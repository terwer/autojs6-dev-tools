using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Helpers;
using Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace App.Views;

public sealed partial class MainPage
{
    private ImageTemplateSourceKind GetCurrentTemplateSourceKind()
    {
        return TemplateSourceFile?.IsChecked == true
            ? ImageTemplateSourceKind.File
            : ImageTemplateSourceKind.Crop;
    }

    private bool HasExternalScreenshotPreviewSnapshot()
    {
        return _externalScreenshotPreviewSnapshot != null;
    }

    private void UpdateCurrentCanvasSourceSummary(string summary)
    {
        _currentCanvasSourceSummary = summary;
    }

    private void DiscardExternalScreenshotPreviewSnapshot()
    {
        _externalScreenshotPreviewSnapshot = null;
    }

    private void InvalidateSuccessfulMatchContext(bool clearCanvasResults)
    {
        _lastSuccessfulMatchContext = null;

        if (clearCanvasResults)
        {
            Canvas.SetMatchResults([]);
            if (MatchSummaryText != null)
            {
                MatchSummaryText.Text = "匹配：-";
            }
        }

        UpdateRegionRefDisplay();
        UpdateButtonStates();
    }

    private void UpdateSuccessfulMatchContextTemplatePath(string templatePath)
    {
        if (_lastSuccessfulMatchContext == null ||
            _lastSuccessfulMatchContext.TemplateSourceKind != ImageTemplateSourceKind.File)
        {
            return;
        }

        _lastSuccessfulMatchContext.TemplatePath = templatePath;
    }

    private void UpdateRegionRefDisplay()
    {
        if (RegionRefTextBox == null)
        {
            return;
        }

        if (_lastSuccessfulMatchContext != null)
        {
            RegionRefTextBox.Text = $"[{string.Join(", ", _lastSuccessfulMatchContext.RegionRef)}]";
            return;
        }

        if (_currentCropRegion != null && TemplateSourceCrop?.IsChecked == true)
        {
            var regionContext = ImageMatchRegionCalculator.Create(_currentCropRegion, MatchRegionPadding);
            RegionRefTextBox.Text = $"[{string.Join(", ", regionContext.RegionRef)}]";
            return;
        }

        RegionRefTextBox.Text = TemplateSourceFile?.IsChecked == true
            ? "[等待命中...]"
            : "[等待裁剪...]";
    }

    private static bool CropRegionEquals(CropRegion? left, CropRegion? right)
    {
        if (left == null && right == null)
        {
            return true;
        }

        if (left == null || right == null)
        {
            return false;
        }

        return left.X == right.X &&
               left.Y == right.Y &&
               left.Width == right.Width &&
               left.Height == right.Height &&
               left.OriginalWidth == right.OriginalWidth &&
               left.OriginalHeight == right.OriginalHeight;
    }

    private CropImageSourceContext? GetActiveCropImageSourceContext()
    {
        if (_currentCropRegion == null)
        {
            return null;
        }

        if (HasExternalScreenshotPreviewSnapshot())
        {
            var snapshot = _externalScreenshotPreviewSnapshot!;
            return new CropImageSourceContext
            {
                ImageBytes = snapshot.ImageBytes.ToArray(),
                Width = snapshot.Width,
                Height = snapshot.Height,
                CropRegion = snapshot.CropRegion ?? _currentCropRegion
            };
        }

        var currentImageBytes = Canvas.GetCurrentImageBytes();
        var (width, height) = Canvas.GetCurrentImageSize();
        if (currentImageBytes == null || width <= 0 || height <= 0)
        {
            return null;
        }

        return new CropImageSourceContext
        {
            ImageBytes = currentImageBytes,
            Width = width,
            Height = height,
            CropRegion = _currentCropRegion
        };
    }

    private CropRegion CreateReferenceBounds(MatchResult matchResult, int screenshotWidth, int screenshotHeight)
    {
        return new CropRegion
        {
            X = matchResult.X,
            Y = matchResult.Y,
            Width = matchResult.Width,
            Height = matchResult.Height,
            OriginalWidth = screenshotWidth,
            OriginalHeight = screenshotHeight
        };
    }

    private string GetSuggestedTemplateBaseName()
    {
        var requestedName = TemplateNameTextBox?.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            return requestedName;
        }

        if (TemplateSourceFile?.IsChecked == true && !string.IsNullOrWhiteSpace(_templateFilePath))
        {
            return Path.GetFileNameWithoutExtension(_templateFilePath);
        }

        if (!string.IsNullOrWhiteSpace(_savedCropTemplatePath))
        {
            return Path.GetFileNameWithoutExtension(_savedCropTemplatePath);
        }

        return $"template_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    private string GetSuggestedCodeBaseName(string templatePath)
    {
        var requestedName = TemplateNameTextBox?.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            return requestedName;
        }

        return Path.GetFileNameWithoutExtension(templatePath);
    }

    private async Task<string?> PickTemplateOutputPathAsync(string suggestedBaseName)
    {
        var picker = CreateTemplateSaveFilePicker(suggestedBaseName);
        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    private Windows.Storage.Pickers.FileSavePicker CreateTemplateSaveFilePicker(string suggestedBaseName)
    {
        var picker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedFileName = suggestedBaseName
        };
        picker.FileTypeChoices.Add("PNG 图片", [".png"]);

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        return picker;
    }

    private async Task<bool> ConfirmOverwriteAsync(string targetPath, string itemLabel)
    {
        if (!File.Exists(targetPath))
        {
            return true;
        }

        var dialog = new ContentDialog
        {
            Title = $"确认覆盖{itemLabel}",
            Content = $"目标文件已存在：\n{targetPath}\n\n继续后会覆盖旧文件。",
            PrimaryButtonText = "覆盖",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async Task SaveBytesToPathAsync(byte[] bytes, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(outputPath, bytes);
    }

    private async Task SaveCropSourceToPathAsync(CropImageSourceContext sourceContext, string outputPath)
    {
        var croppedBytes = await _imageProcessor.CropAsync(
            sourceContext.ImageBytes,
            sourceContext.CropRegion.X,
            sourceContext.CropRegion.Y,
            sourceContext.CropRegion.Width,
            sourceContext.CropRegion.Height);

        await SaveBytesToPathAsync(croppedBytes, outputPath);
    }

    private async Task<bool> RestoreExternalScreenshotPreviewAsync(FrameworkElement? target)
    {
        if (!HasExternalScreenshotPreviewSnapshot())
        {
            ShowActionTip("当前没有可恢复的现场", StatusTone.Warning, target, "无法恢复");
            return false;
        }

        var snapshot = _externalScreenshotPreviewSnapshot!;
        Canvas.LoadImage(snapshot.ImageBytes, snapshot.Width, snapshot.Height);
        Canvas.SetViewState(snapshot.Scale, snapshot.OffsetX, snapshot.OffsetY);
        Canvas.SetMatchResults(snapshot.MatchResults.ToList());
        Canvas.ToggleCropRegion(snapshot.CropRegion != null);

        _suspendCropStateTracking = true;
        try
        {
            Canvas.SetCropRegion(snapshot.CropRegion);
            _currentCropRegion = snapshot.CropRegion;
        }
        finally
        {
            _suspendCropStateTracking = false;
        }

        _isFitToWindowMode = snapshot.IsFitToWindowMode;
        _hasScreenshot = true;
        _screenshotFilePath = snapshot.ScreenshotFilePath;
        _currentCanvasSourceSummary = snapshot.CanvasSourceSummary;

        if (TemplateSourceCrop != null)
        {
            TemplateSourceCrop.IsChecked = snapshot.TemplateSourceCropChecked;
        }

        if (TemplateSourceFile != null)
        {
            TemplateSourceFile.IsChecked = !snapshot.TemplateSourceCropChecked;
        }

        if (ScreenshotSourceCurrent != null)
        {
            ScreenshotSourceCurrent.IsChecked = snapshot.ScreenshotSourceCurrentChecked;
        }

        if (ScreenshotSourceFile != null)
        {
            ScreenshotSourceFile.IsChecked = !snapshot.ScreenshotSourceCurrentChecked;
        }

        ResolutionText.Text = $"分辨率：{snapshot.Width}x{snapshot.Height}";
        MatchSummaryText.Text = snapshot.MatchSummaryText;
        RegionRefTextBox.Text = snapshot.RegionRefText;

        DiscardExternalScreenshotPreviewSnapshot();
        UpdateSourceSummaries();
        UpdateStagePresentation();
        UpdateButtonStates();
        ShowActionTip("已恢复测试前现场", StatusTone.Success, target);
        return true;
    }

    private async Task ShowExternalScreenshotPreviewAsync(
        byte[] imageBytes,
        int width,
        int height,
        string screenshotLabel,
        IReadOnlyList<MatchResult> overlayResults)
    {
        if (!HasExternalScreenshotPreviewSnapshot())
        {
            var currentImageBytes = Canvas.GetCurrentImageBytes();
            var (currentWidth, currentHeight) = Canvas.GetCurrentImageSize();
            var (currentScale, currentOffsetX, currentOffsetY) = Canvas.GetViewState();

            if (currentImageBytes != null && currentWidth > 0 && currentHeight > 0)
            {
                _externalScreenshotPreviewSnapshot = new ExternalScreenshotPreviewSnapshot
                {
                    ImageBytes = currentImageBytes.ToArray(),
                    Width = currentWidth,
                    Height = currentHeight,
                    Scale = currentScale,
                    OffsetX = currentOffsetX,
                    OffsetY = currentOffsetY,
                    IsFitToWindowMode = _isFitToWindowMode,
                    CropRegion = _currentCropRegion,
                    MatchSummaryText = MatchSummaryText?.Text ?? "匹配：-",
                    RegionRefText = RegionRefTextBox?.Text ?? "[等待裁剪...]",
                    TemplateSourceCropChecked = TemplateSourceCrop?.IsChecked == true,
                    ScreenshotSourceCurrentChecked = ScreenshotSourceCurrent?.IsChecked == true,
                    ScreenshotFilePath = _screenshotFilePath,
                    CanvasSourceSummary = _currentCanvasSourceSummary,
                    MatchResults = Canvas.GetMatchResults()
                };
            }
        }

        Canvas.LoadImage(imageBytes, width, height);
        Canvas.FitToWindow();
        Canvas.ToggleCropRegion(false);
        Canvas.SetMatchResults(overlayResults.ToList());

        _isFitToWindowMode = true;
        _hasScreenshot = true;
        ResolutionText.Text = $"分辨率：{width}x{height}";
        UpdateCurrentCanvasSourceSummary(
            $"当前画布：测试预览 {Path.GetFileName(screenshotLabel)} · {width}x{height}");
        UpdateSourceSummaries();
        UpdateStagePresentation();
        UpdateButtonStates();
        await Task.CompletedTask;
    }
}
