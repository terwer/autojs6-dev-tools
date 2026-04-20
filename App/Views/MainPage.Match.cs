using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private async void TestMatchButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var templateBytes = await ResolveTemplateBytesAsync();
            var screenshotBytes = await ResolveScreenshotBytesAsync();

            if (templateBytes == null || screenshotBytes == null)
            {
                return;
            }

            SetStatus("正在执行真实匹配测试...", StatusTone.Info);

            var threshold = ThresholdSlider.Value;
            var searchScope = GetMatchSearchScope();
            var region = BuildMatchSearchRegion(searchScope, screenshotBytes.Value.IsExternalFile);

            var matchResult = await _openCvMatchService.MatchTemplateAsync(
                screenshotBytes.Value.Bytes,
                templateBytes.Value.Bytes,
                threshold,
                region);

            await EnsureScreenshotPreviewAsync(screenshotBytes.Value);

            var overlayResults = matchResult == null ? [] : new List<MatchResult> { matchResult };
            Canvas.SetMatchResults(overlayResults);

            MatchResultText.Text = BuildMatchSummary(matchResult, searchScope, region, threshold);

            if (matchResult == null)
            {
                SetStatus("匹配执行失败，未返回结果", StatusTone.Error);
                return;
            }

            SetStatus(
                matchResult.IsMatch
                    ? $"匹配成功：({matchResult.ClickX}, {matchResult.ClickY})"
                    : $"未达到阈值，最佳置信度 {matchResult.Confidence:F3}",
                matchResult.IsMatch ? StatusTone.Success : StatusTone.Warning);
        }
        catch (Exception ex)
        {
            MatchResultText.Text = $"结果：匹配测试失败\n原因：{ex.Message}";
            Canvas.SetMatchResults([]);
            await ShowErrorAsync($"匹配测试失败：{ex.Message}");
            SetStatus("匹配测试失败", StatusTone.Error);
        }
    }

    private async Task<(byte[] Bytes, string Label)?> ResolveTemplateBytesAsync()
    {
        if (TemplateSourceCrop.IsChecked == true)
        {
            if (_currentCropRegion == null)
            {
                SetStatus("请先在当前画布中创建裁剪区域", StatusTone.Warning);
                return null;
            }

            var screenshotBytes = Canvas.GetCurrentImageBytes();
            if (screenshotBytes == null)
            {
                SetStatus("当前画布没有可用图像，无法裁剪模板", StatusTone.Warning);
                return null;
            }

            var croppedBytes = await _imageProcessor.CropAsync(
                screenshotBytes,
                _currentCropRegion.X,
                _currentCropRegion.Y,
                _currentCropRegion.Width,
                _currentCropRegion.Height);

            return (croppedBytes, "当前裁剪");
        }

        if (string.IsNullOrWhiteSpace(_templateFilePath) || !File.Exists(_templateFilePath))
        {
            SetStatus("请选择模板文件", StatusTone.Warning);
            return null;
        }

        var templateBytes = await File.ReadAllBytesAsync(_templateFilePath);
        if (!_openCvMatchService.ValidateTemplate(templateBytes))
        {
            SetStatus("模板文件无效，无法执行匹配", StatusTone.Error);
            return null;
        }

        return (templateBytes, _templateFilePath);
    }

    private async Task<(byte[] Bytes, int Width, int Height, bool IsExternalFile, string Label)?> ResolveScreenshotBytesAsync()
    {
        if (ScreenshotSourceCurrent.IsChecked == true)
        {
            var currentBytes = Canvas.GetCurrentImageBytes();
            if (currentBytes == null)
            {
                SetStatus("当前画布没有可用截图", StatusTone.Warning);
                return null;
            }

            var (currentWidth, currentHeight) = Canvas.GetCurrentImageSize();
            return (currentBytes, currentWidth, currentHeight, false, "当前画布");
        }

        if (string.IsNullOrWhiteSpace(_screenshotFilePath) || !File.Exists(_screenshotFilePath))
        {
            SetStatus("请选择测试截图文件", StatusTone.Warning);
            return null;
        }

        var screenshotBytes = await File.ReadAllBytesAsync(_screenshotFilePath);
        var (fileWidth, fileHeight) = await _imageProcessor.GetImageSizeAsync(screenshotBytes);
        return (screenshotBytes, fileWidth, fileHeight, true, _screenshotFilePath);
    }

    private CropRegion? BuildMatchSearchRegion(MatchSearchScope searchScope, bool isExternalScreenshot)
    {
        if (searchScope == MatchSearchScope.FullImage || isExternalScreenshot)
        {
            return null;
        }

        if (_currentCropRegion == null)
        {
            return null;
        }

        var (imageWidth, imageHeight) = Canvas.GetCurrentImageSize();
        var x = Math.Max(0, _currentCropRegion.X - MatchRegionPadding);
        var y = Math.Max(0, _currentCropRegion.Y - MatchRegionPadding);
        var right = Math.Min(imageWidth, _currentCropRegion.X + _currentCropRegion.Width + MatchRegionPadding);
        var bottom = Math.Min(imageHeight, _currentCropRegion.Y + _currentCropRegion.Height + MatchRegionPadding);

        return new CropRegion
        {
            X = x,
            Y = y,
            Width = Math.Max(1, right - x),
            Height = Math.Max(1, bottom - y),
            OriginalWidth = imageWidth,
            OriginalHeight = imageHeight
        };
    }

    private async Task EnsureScreenshotPreviewAsync((byte[] Bytes, int Width, int Height, bool IsExternalFile, string Label) screenshot)
    {
        if (!screenshot.IsExternalFile)
        {
            return;
        }

        await LoadImageIntoCanvasAsync(screenshot.Bytes, screenshot.Width, screenshot.Height, fitToWindow: true);
        SetStatus($"已切换到测试截图预览：{Path.GetFileName(screenshot.Label)}", StatusTone.Info);
    }

    private string BuildMatchSummary(
        MatchResult? matchResult,
        MatchSearchScope scope,
        CropRegion? region,
        double threshold)
    {
        var scopeText = scope == MatchSearchScope.FullImage
            ? "全图搜索"
            : region == null
                ? "区域搜索（回退全图）"
                : $"区域搜索 [{region.X}, {region.Y}, {region.Width}, {region.Height}]";

        if (matchResult == null)
        {
            return $"结果：执行失败，未返回匹配结果\n阈值：{threshold:F2}\n搜索范围：{scopeText}";
        }

        var state = matchResult.IsMatch ? "命中" : "未命中";
        return
$@"结果：{state}
置信度：{matchResult.Confidence:F4}
点击坐标：({matchResult.ClickX}, {matchResult.ClickY})
匹配区域：({matchResult.X}, {matchResult.Y}, {matchResult.Width}, {matchResult.Height})
耗时：{matchResult.ElapsedMilliseconds} ms
阈值：{threshold:F2}
搜索范围：{scopeText}";
    }
}
