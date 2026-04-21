using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Helpers;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private async void TestMatchButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var target = sender as Microsoft.UI.Xaml.FrameworkElement;
        try
        {
            var templateBytes = await ResolveTemplateBytesAsync(target);
            var screenshotBytes = await ResolveScreenshotBytesAsync(target);

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

            var overlayResults = matchResult == null ? [] : new List<MatchResult> { matchResult };

            if (screenshotBytes.Value.IsExternalFile)
            {
                await ShowExternalScreenshotPreviewAsync(
                    screenshotBytes.Value.Bytes,
                    screenshotBytes.Value.Width,
                    screenshotBytes.Value.Height,
                    screenshotBytes.Value.Label,
                    overlayResults);
            }
            else
            {
                Canvas.ToggleCropRegion(true);
                Canvas.SetMatchResults(overlayResults);
            }

            MatchSummaryText.Text = BuildMatchSummary(matchResult);
            UpdateMatchContext(matchResult, templateBytes.Value.SourceKind, screenshotBytes.Value.Width, screenshotBytes.Value.Height);
            UpdateRegionRefDisplay();
            LogMatchDetails(matchResult, searchScope, region, threshold, templateBytes.Value.Label, screenshotBytes.Value.Label);

            if (matchResult == null)
            {
                ShowActionTip("匹配执行失败，未返回结果", StatusTone.Error, target, "执行失败");
                return;
            }

            var successMessage = screenshotBytes.Value.IsExternalFile
                ? $"匹配成功：({matchResult.ClickX}, {matchResult.ClickY})，已切换到测试截图，可随时恢复现场"
                : $"匹配成功：({matchResult.ClickX}, {matchResult.ClickY})";

            ShowActionTip(
                matchResult.IsMatch ? successMessage : $"未达到阈值，最佳置信度 {matchResult.Confidence:F3}",
                matchResult.IsMatch ? StatusTone.Success : StatusTone.Warning,
                target,
                matchResult.IsMatch ? "匹配成功" : "匹配未命中");
        }
        catch (Exception ex)
        {
            InvalidateSuccessfulMatchContext(clearCanvasResults: true);
            MatchSummaryText.Text = "匹配：失败";
            Services.LogService.Instance.Log($"[Match] 执行失败: {ex.Message}");
            Canvas.SetMatchResults([]);
            ShowActionTip($"匹配测试失败：{ex.Message}", StatusTone.Error, target, "执行失败");
        }
    }

    private async Task<(byte[] Bytes, string Label, ImageTemplateSourceKind SourceKind)?> ResolveTemplateBytesAsync(Microsoft.UI.Xaml.FrameworkElement? target)
    {
        if (TemplateSourceCrop.IsChecked == true)
        {
            var cropSourceContext = GetActiveCropImageSourceContext();
            if (cropSourceContext == null)
            {
                ShowActionTip("请先在当前画布中创建裁剪区域", StatusTone.Warning, target, "无法执行测试");
                return null;
            }

            var croppedBytes = await _imageProcessor.CropAsync(
                cropSourceContext.ImageBytes,
                cropSourceContext.CropRegion.X,
                cropSourceContext.CropRegion.Y,
                cropSourceContext.CropRegion.Width,
                cropSourceContext.CropRegion.Height);

            return (croppedBytes, "当前裁剪", ImageTemplateSourceKind.Crop);
        }

        if (string.IsNullOrWhiteSpace(_templateFilePath) || !File.Exists(_templateFilePath))
        {
            ShowActionTip("请选择模板文件", StatusTone.Warning, target, "无法执行测试");
            return null;
        }

        var templateBytes = await File.ReadAllBytesAsync(_templateFilePath);
        if (!_openCvMatchService.ValidateTemplate(templateBytes))
        {
            ShowActionTip("模板文件无效，无法执行匹配", StatusTone.Error, target, "无法执行测试");
            return null;
        }

        return (templateBytes, _templateFilePath, ImageTemplateSourceKind.File);
    }

    private async Task<(byte[] Bytes, int Width, int Height, bool IsExternalFile, string Label)?> ResolveScreenshotBytesAsync(Microsoft.UI.Xaml.FrameworkElement? target)
    {
        if (ScreenshotSourceCurrent.IsChecked == true)
        {
            var currentBytes = Canvas.GetCurrentImageBytes();
            if (currentBytes == null)
            {
                ShowActionTip("当前画布没有可用截图", StatusTone.Warning, target, "无法执行测试");
                return null;
            }

            var (currentWidth, currentHeight) = Canvas.GetCurrentImageSize();
            return (currentBytes, currentWidth, currentHeight, false, _currentCanvasSourceSummary);
        }

        if (string.IsNullOrWhiteSpace(_screenshotFilePath) || !File.Exists(_screenshotFilePath))
        {
            ShowActionTip("请选择测试截图文件", StatusTone.Warning, target, "无法执行测试");
            return null;
        }

        var screenshotBytes = await File.ReadAllBytesAsync(_screenshotFilePath);
        var (fileWidth, fileHeight) = await _imageProcessor.GetImageSizeAsync(screenshotBytes);
        return (screenshotBytes, fileWidth, fileHeight, true, _screenshotFilePath);
    }

    private CropRegion? BuildMatchSearchRegion(MatchSearchScope searchScope, bool isExternalScreenshot)
    {
        if (searchScope == MatchSearchScope.FullImage || isExternalScreenshot || _currentCropRegion == null)
        {
            return null;
        }

        return ImageMatchRegionCalculator.Create(_currentCropRegion, MatchRegionPadding).SearchRegion;
    }

    private void UpdateMatchContext(
        MatchResult? matchResult,
        ImageTemplateSourceKind templateSourceKind,
        int screenshotWidth,
        int screenshotHeight)
    {
        if (matchResult == null || !matchResult.IsMatch)
        {
            _lastSuccessfulMatchContext = null;
            UpdateButtonStates();
            return;
        }

        var referenceBounds = CreateReferenceBounds(matchResult, screenshotWidth, screenshotHeight);
        var regionContext = CreateRegionContext(referenceBounds);

        _lastSuccessfulMatchContext = new SuccessfulImageMatchContext
        {
            TemplateSourceKind = templateSourceKind,
            TemplatePath = templateSourceKind == ImageTemplateSourceKind.File ? _templateFilePath : _savedCropTemplatePath,
            ReferenceBounds = regionContext.ReferenceBounds,
            SearchRegion = regionContext.SearchRegion,
            RegionRef = regionContext.RegionRef,
            Orientation = regionContext.Orientation,
            MatchResult = matchResult
        };

        UpdateButtonStates();
    }

    private string BuildMatchSummary(MatchResult? matchResult)
    {
        if (matchResult == null)
        {
            return "匹配：失败";
        }

        return matchResult.IsMatch
            ? $"匹配：命中 · {matchResult.Confidence:F3} · {matchResult.ElapsedMilliseconds}ms · ({matchResult.ClickX}, {matchResult.ClickY})"
            : $"匹配：未命中 · {matchResult.Confidence:F3} · {matchResult.ElapsedMilliseconds}ms";
    }

    private void LogMatchDetails(
        MatchResult? matchResult,
        MatchSearchScope scope,
        CropRegion? region,
        double threshold,
        string templateLabel,
        string screenshotLabel)
    {
        var scopeText = scope == MatchSearchScope.FullImage
            ? "全图搜索"
            : region == null
                ? "区域搜索（回退全图）"
                : $"区域搜索 [{region.X}, {region.Y}, {region.Width}, {region.Height}]";

        Services.LogService.Instance.Log($"[Match] 模板源={templateLabel}");
        Services.LogService.Instance.Log($"[Match] 截图源={screenshotLabel}");

        if (matchResult == null)
        {
            Services.LogService.Instance.Log($"[Match] 未返回结果 · 阈值={threshold:F2} · 搜索={scopeText}");
            return;
        }

        Services.LogService.Instance.Log(
            $"[Match] {(matchResult.IsMatch ? "命中" : "未命中")} · 置信度={matchResult.Confidence:F4} · 点击=({matchResult.ClickX}, {matchResult.ClickY})");
        Services.LogService.Instance.Log(
            $"[Match] 匹配区域=({matchResult.X}, {matchResult.Y}, {matchResult.Width}, {matchResult.Height}) · 耗时={matchResult.ElapsedMilliseconds}ms · 阈值={threshold:F2} · 搜索={scopeText}");
    }
}
