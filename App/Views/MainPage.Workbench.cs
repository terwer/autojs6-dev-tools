using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace App.Views;

public sealed partial class MainPage
{
    private enum WorkbenchMode
    {
        Image,
        Ui
    }

    private enum StatusTone
    {
        Info,
        Success,
        Warning,
        Error
    }

    private enum MatchSearchScope
    {
        Region,
        FullImage
    }

    private WorkbenchMode _workbenchMode = WorkbenchMode.Image;
    private string _latestGeneratedCode = string.Empty;

    private void ImageModeButton_Click(object sender, RoutedEventArgs e)
    {
        SetWorkbenchMode(WorkbenchMode.Image);
    }

    private void UiModeButton_Click(object sender, RoutedEventArgs e)
    {
        SetWorkbenchMode(WorkbenchMode.Ui);
    }

    private void SetWorkbenchMode(WorkbenchMode mode)
    {
        if (_workbenchMode == mode)
        {
            return;
        }

        ClearModeSpecificStateForSwitch();
        _workbenchMode = mode;
        UpdateWorkbenchModeUi();
        UpdateButtonStates();
        UpdateStagePresentation();
        SetStatus($"已切换到{(_workbenchMode == WorkbenchMode.Image ? "图像模式" : "控件模式")}，已清空上一模式状态", StatusTone.Info);
    }

    private void UpdateWorkbenchModeUi()
    {
        if (ImageModeButton == null ||
            UiModeButton == null ||
            ImageInspectorPanel == null ||
            UiInspectorPanel == null ||
            ImageToolbarPanel == null ||
            UiToolbarPanel == null ||
            ModeStatusText == null ||
            HudModeText == null)
        {
            return;
        }

        var isImageMode = _workbenchMode == WorkbenchMode.Image;

        ImageModeButton.IsChecked = isImageMode;
        UiModeButton.IsChecked = !isImageMode;
        ImageInspectorPanel.Visibility = isImageMode ? Visibility.Visible : Visibility.Collapsed;
        UiInspectorPanel.Visibility = isImageMode ? Visibility.Collapsed : Visibility.Visible;
        ImageToolbarPanel.Visibility = isImageMode ? Visibility.Visible : Visibility.Collapsed;
        UiToolbarPanel.Visibility = isImageMode ? Visibility.Collapsed : Visibility.Visible;

        var modeText = isImageMode ? "图像模式" : "控件模式";
        ModeStatusText.Text = $"当前模式：{modeText}";
        HudModeText.Text = $"模式：{modeText}";
    }

    private void UpdateButtonStates()
    {
        if (CaptureButton == null ||
            LoadLocalTopButton == null ||
            FitToWindowButton == null ||
            ResetViewButton == null ||
            EnterCropButton == null ||
            ExitCropButton == null ||
            ClearStageButton == null ||
            RestoreStageButton == null ||
            DumpUiStageButton == null ||
            WidgetBoundsCheckBox == null ||
            TestMatchButton == null ||
            SaveTemplateButton == null ||
            GenerateCodeButton == null ||
            ViewCodeRightButton == null ||
            CopyCoordinatesButton == null ||
            CopySelectorButton == null ||
            PreviewWidgetSnippetButton == null ||
            TemplateSourceCrop == null ||
            TemplateSourceFile == null ||
            ScreenshotSourceCurrent == null ||
            ScreenshotSourceFile == null ||
            TemplateBrowseButton == null ||
            ScreenshotBrowseButton == null ||
            ShowLogButton == null)
        {
            return;
        }

        var isImageMode = _workbenchMode == WorkbenchMode.Image;
        var hasScreenshot = _hasScreenshot;
        var canDumpUi = hasScreenshot && _currentDevice != null;
        var hasCropTemplateSource = TemplateSourceCrop.IsChecked == true && _currentCropRegion != null;
        var hasFileTemplateSource = TemplateSourceFile.IsChecked == true && !string.IsNullOrWhiteSpace(_templateFilePath);
        var hasTemplateSource = hasCropTemplateSource || hasFileTemplateSource;
        var hasScreenshotSource = (ScreenshotSourceCurrent.IsChecked == true && hasScreenshot) ||
                                  (ScreenshotSourceFile.IsChecked == true && !string.IsNullOrWhiteSpace(_screenshotFilePath));
        var hasWidget = _selectedWidget != null;
        var hasGeneratedCode = !string.IsNullOrWhiteSpace(_latestGeneratedCode);
        var hasSavedCropTemplate = !string.IsNullOrWhiteSpace(_savedCropTemplatePath) && File.Exists(_savedCropTemplatePath);
        var hasRestoreSnapshot = HasExternalScreenshotPreviewSnapshot();

        var canGenerateCode = isImageMode && (TemplateSourceCrop.IsChecked == true
            ? hasSavedCropTemplate
            : hasFileTemplateSource && _lastSuccessfulMatchContext?.TemplateSourceKind == ImageTemplateSourceKind.File);

        CaptureButton.IsEnabled = _currentDevice != null;
        LoadLocalTopButton.IsEnabled = true;

        FitToWindowButton.IsEnabled = hasScreenshot;
        ResetViewButton.IsEnabled = hasScreenshot;
        EnterCropButton.IsEnabled = hasScreenshot && !_isFitToWindowMode && isImageMode && !_isCroppingMode;
        ExitCropButton.IsEnabled = hasScreenshot && isImageMode && _isCroppingMode;
        ClearStageButton.IsEnabled = hasScreenshot;
        RestoreStageButton.IsEnabled = hasRestoreSnapshot;
        RestoreStageButton.Visibility = hasRestoreSnapshot ? Visibility.Visible : Visibility.Collapsed;

        WidgetBoundsCheckBox.IsEnabled = hasScreenshot && _workbenchMode == WorkbenchMode.Ui;

        TemplateBrowseButton.IsEnabled = TemplateSourceFile.IsChecked == true;
        ScreenshotBrowseButton.IsEnabled = ScreenshotSourceFile.IsChecked == true;

        TestMatchButton.IsEnabled = isImageMode && hasTemplateSource && hasScreenshotSource;
        SaveTemplateButton.IsEnabled = isImageMode && hasTemplateSource;
        GenerateCodeButton.IsEnabled = canGenerateCode;
        ViewCodeRightButton.IsEnabled = hasGeneratedCode;

        CopyCoordinatesButton.IsEnabled = hasWidget;
        CopySelectorButton.IsEnabled = hasWidget;
        PreviewWidgetSnippetButton.IsEnabled = hasWidget;
        ShowLogButton.IsEnabled = true;

        DumpUiStageButton.IsEnabled = canDumpUi && !_isDumpUiLoading;
        SetDumpUiLoading(_isDumpUiLoading);
        ApplyCropButtonVisualState();

        ToolTipService.SetToolTip(DumpUiStageButton, canDumpUi ? null : "请先选择设备并准备一张当前截图");
        ToolTipService.SetToolTip(FitToWindowButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(ResetViewButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(EnterCropButton, EnterCropButton.IsEnabled ? null : "请先截图并切回 1:1 视图");
        ToolTipService.SetToolTip(ExitCropButton, ExitCropButton.IsEnabled ? null : "当前未处于裁剪模式");
        ToolTipService.SetToolTip(RestoreStageButton, hasRestoreSnapshot ? "恢复切换到测试截图前的现场" : "当前没有可恢复的现场");
        ToolTipService.SetToolTip(ClearStageButton, hasScreenshot ? "保留当前图片，清空当前模式里的裁剪、匹配和选择状态" : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(SaveTemplateButton, SaveTemplateButton.IsEnabled
            ? "每次保存都会让你明确选择目标 PNG 文件"
            : TemplateSourceCrop.IsChecked == true
                ? "请先创建裁剪区域，或切换到模板文件来源"
                : "请先选择模板文件");
        ToolTipService.SetToolTip(GenerateCodeButton, GenerateCodeButton.IsEnabled
            ? "代码会直接写入当前代码目录，同名文件会覆盖"
            : TemplateSourceCrop.IsChecked == true
                ? "请先保存模板，再生成代码"
                : "外部模板请先完成一次命中测试");
        ToolTipService.SetToolTip(ViewCodeRightButton, hasGeneratedCode ? null : "请先生成代码");
        ToolTipService.SetToolTip(CopyCoordinatesButton, hasWidget ? null : "请先在画布或节点树中选择控件");
        ToolTipService.SetToolTip(CopySelectorButton, hasWidget ? null : "请先在画布或节点树中选择控件");
        ToolTipService.SetToolTip(PreviewWidgetSnippetButton, hasWidget ? null : "请先在画布或节点树中选择控件");
    }

    private void UpdateStagePresentation()
    {
        if (StageEmptyStateOverlay == null ||
            ScaleText == null ||
            HudScaleText == null ||
            HudResolutionText == null ||
            ResolutionText == null ||
            HudCropText == null ||
            MatchSummaryText == null)
        {
            return;
        }

        StageEmptyStateOverlay.Visibility = _hasScreenshot ? Visibility.Collapsed : Visibility.Visible;

        var scale = Canvas.GetScale();
        ScaleText.Text = $"缩放：{scale * 100:F0}%";
        HudScaleText.Text = $"缩放：{scale * 100:F0}%";
        HudResolutionText.Text = ResolutionText.Text;
        HudCropText.Text = _currentCropRegion == null
            ? "裁剪区域：-"
            : $"裁剪区域：{_currentCropRegion.Width}x{_currentCropRegion.Height}{(HasExternalScreenshotPreviewSnapshot() ? "（暂存）" : string.Empty)}";
        MatchSummaryText.Text = string.IsNullOrWhiteSpace(MatchSummaryText.Text)
            ? "匹配：-"
            : MatchSummaryText.Text;
    }

    private void UpdateCurrentDeviceSummary()
    {
        if (CurrentDeviceSummaryText == null)
        {
            return;
        }

        CurrentDeviceSummaryText.Text = _currentDevice == null
            ? "当前设备：尚未选择"
            : $"当前设备：{_currentDevice.Serial} · {(_currentDevice.Model ?? "未知型号")}";
    }

    private void UpdateSourceSummaries()
    {
        if (TemplateSourceSummaryText == null ||
            TemplateSourceCrop == null ||
            ScreenshotSourceSummaryText == null ||
            ScreenshotSourceCurrent == null)
        {
            return;
        }

        if (TemplateSourceCrop.IsChecked == true)
        {
            if (_currentCropRegion == null)
            {
                TemplateSourceSummaryText.Text = "当前裁剪：尚未创建区域";
            }
            else
            {
                var savedState = !string.IsNullOrWhiteSpace(_savedCropTemplatePath) && File.Exists(_savedCropTemplatePath)
                    ? $"已保存：{_savedCropTemplatePath}"
                    : "未保存：请先保存模板再生成代码";
                var previewState = HasExternalScreenshotPreviewSnapshot()
                    ? " · 已暂存预览前现场"
                    : string.Empty;
                TemplateSourceSummaryText.Text =
                    $"当前裁剪：{_currentCropRegion.Width}x{_currentCropRegion.Height} 区域{previewState}\n{savedState}";
            }
        }
        else
        {
            TemplateSourceSummaryText.Text = string.IsNullOrWhiteSpace(_templateFilePath)
                ? "模板文件：尚未选择"
                : $"模板文件：{Path.GetFileName(_templateFilePath)}\n{_templateFilePath}";
        }

        ScreenshotSourceSummaryText.Text = ScreenshotSourceCurrent.IsChecked == true
            ? _currentCanvasSourceSummary
            : string.IsNullOrWhiteSpace(_screenshotFilePath)
                ? "测试截图：尚未选择"
                : $"测试截图：{Path.GetFileName(_screenshotFilePath)}\n{_screenshotFilePath}";
    }

    private void UpdateSelectedWidgetSummary()
    {
        if (SelectedWidgetClassText == null ||
            SelectedWidgetTextText == null ||
            SelectedWidgetResourceIdText == null)
        {
            return;
        }

        SelectedWidgetClassText.Text = _selectedWidget?.ClassName ?? "-";
        SelectedWidgetTextText.Text = string.IsNullOrWhiteSpace(_selectedWidget?.Text)
            ? "-"
            : _selectedWidget!.Text!;
        SelectedWidgetResourceIdText.Text = string.IsNullOrWhiteSpace(_selectedWidget?.ResourceId)
            ? "-"
            : _selectedWidget!.ResourceId!;
    }

    private void ResetCanvasRelatedState(bool clearGeneratedCode)
    {
        Canvas.SetWidgetNodes([]);
        Canvas.SetMatchResults([]);
        Canvas.SetSelectedWidget(null);
        Canvas.SetCropRegion(null);
        Canvas.DisableCroppingMode();
        Canvas.ToggleCropRegion(true);

        _isCroppingMode = false;
        _selectedWidget = null;
        _uiRootNode = null;
        _uiTotalNodes = 0;
        _uiDisplayedNodes = 0;
        _savedCropTemplatePath = null;
        _lastSuccessfulMatchContext = null;

        if (EnterCropButton != null && ExitCropButton != null)
        {
            ApplyCropButtonVisualState();
        }

        PropertyPanel?.SetWidget(null);
        UpdateSelectedWidgetSummary();
        UpdateUiTreeSummary();
        RebuildUiNodeTree();

        if (MatchSummaryText != null)
        {
            MatchSummaryText.Text = "匹配：-";
        }

        if (clearGeneratedCode)
        {
            _latestGeneratedCode = string.Empty;
            _latestImageCodePreviewItems.Clear();
        }

        UpdateRegionRefDisplay();
        UpdateButtonStates();
    }

    private void ClearModeSpecificStateForSwitch()
    {
        Canvas.SetWidgetNodes([]);
        Canvas.SetMatchResults([]);
        Canvas.SetSelectedWidget(null);
        Canvas.SetCropRegion(null);
        Canvas.DisableCroppingMode();
        Canvas.ToggleWidgetBounds(true);
        Canvas.ToggleCropRegion(true);

        _isCroppingMode = false;
        _currentCropRegion = null;
        _templateFilePath = null;
        _screenshotFilePath = null;
        _selectedWidget = null;
        _uiRootNode = null;
        _uiTotalNodes = 0;
        _uiDisplayedNodes = 0;
        _savedCropTemplatePath = null;
        _lastSuccessfulMatchContext = null;
        _latestGeneratedCode = string.Empty;
        _latestImageCodePreviewItems.Clear();
        DiscardExternalScreenshotPreviewSnapshot();

        if (TemplateSourceCrop != null)
        {
            TemplateSourceCrop.IsChecked = true;
        }

        if (ScreenshotSourceCurrent != null)
        {
            ScreenshotSourceCurrent.IsChecked = true;
        }

        if (FullImageSearchCheckBox != null)
        {
            FullImageSearchCheckBox.IsChecked = false;
        }

        if (ThresholdSlider != null)
        {
            ThresholdSlider.Value = 0.84;
        }

        if (EnterCropButton != null && ExitCropButton != null)
        {
            ApplyCropButtonVisualState();
        }

        if (MatchSummaryText != null)
        {
            MatchSummaryText.Text = "匹配：-";
        }

        if (UiSearchTextBox != null)
        {
            UiSearchTextBox.Text = string.Empty;
        }

        PropertyPanel?.SetWidget(null);
        UpdateSelectedWidgetSummary();
        UpdateUiTreeSummary();
        RebuildUiNodeTree();
        UpdateRegionRefDisplay();
        UpdateSourceSummaries();
        UpdateButtonStates();
    }

    private void ClearStageButton_Click(object sender, RoutedEventArgs e)
    {
        var target = sender as FrameworkElement;
        if (!_hasScreenshot)
        {
            ShowActionTip("请先截图或载入本地图片", StatusTone.Warning, target, "无法清屏");
            return;
        }

        ClearModeSpecificStateForSwitch();
        UpdateStagePresentation();

        var modeText = _workbenchMode == WorkbenchMode.Image ? "图像模式" : "控件模式";
        ShowActionTip($"已清空{modeText}现场，底图已保留", StatusTone.Success, target);
    }

    private async void RestoreStageButton_Click(object sender, RoutedEventArgs e)
    {
        await RestoreExternalScreenshotPreviewAsync(sender as FrameworkElement);
    }

    private void ShowLogDockButton_Click(object sender, RoutedEventArgs e)
    {
        BottomDockPanel.Visibility = BottomDockPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void WidgetBoundsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (Canvas == null || WidgetBoundsCheckBox == null)
        {
            return;
        }

        Canvas.ToggleWidgetBounds(WidgetBoundsCheckBox.IsChecked == true);
    }

    private async void LoadLocalImageButton_Click(object sender, RoutedEventArgs e)
    {
        var target = sender as FrameworkElement;
        try
        {
            var picker = CreateImageFilePicker();
            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            SetStatus("正在载入本地图片...", StatusTone.Info);

            var bytes = await File.ReadAllBytesAsync(file.Path);
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);

            DiscardExternalScreenshotPreviewSnapshot();
            await LoadImageIntoCanvasAsync(
                bytes,
                (int)decoder.PixelWidth,
                (int)decoder.PixelHeight,
                fitToWindow: false,
                currentCanvasSummary: $"当前画布：本地图片 {file.Name} · {decoder.PixelWidth}x{decoder.PixelHeight}");

            _screenshotFilePath = file.Path;
            ScreenshotSourceCurrent.IsChecked = true;

            ShowActionTip($"已载入本地图片：{file.Name}", StatusTone.Success, target);
        }
        catch (Exception ex)
        {
            ShowActionTip($"载入本地图片失败：{ex.Message}", StatusTone.Error, target, "载入失败");
        }
    }

    private void CopyRegionRefButton_Click(object sender, RoutedEventArgs e)
    {
        var target = sender as FrameworkElement;
        if (string.IsNullOrWhiteSpace(RegionRefTextBox.Text) || RegionRefTextBox.Text.StartsWith("[等待", StringComparison.Ordinal))
        {
            ShowActionTip("当前没有可复制的 regionRef", StatusTone.Warning, target, "无法复制");
            return;
        }

        CopyToClipboard(RegionRefTextBox.Text);
        ShowActionTip("regionRef 已复制到剪贴板", StatusTone.Success, target);
    }

    private void CopyCoordinatesButton_Click(object sender, RoutedEventArgs e)
    {
        var target = sender as FrameworkElement;
        var coordinates = PropertyPanel.GetCoordinatesText();
        if (string.IsNullOrWhiteSpace(coordinates))
        {
            ShowActionTip("请先在画布或节点树中选择控件", StatusTone.Warning, target, "无法复制坐标");
            return;
        }

        CopyToClipboard(coordinates);
        ShowActionTip("控件坐标已复制到剪贴板", StatusTone.Success, target);
    }

    private void CopySelectorButton_Click(object sender, RoutedEventArgs e)
    {
        var target = sender as FrameworkElement;
        var selector = PropertyPanel.GetUiSelectorText();
        if (string.IsNullOrWhiteSpace(selector))
        {
            ShowActionTip("请先在画布或节点树中选择控件", StatusTone.Warning, target, "无法复制选择器");
            return;
        }

        CopyToClipboard(selector);
        ShowActionTip("选择器已复制到剪贴板", StatusTone.Success, target);
    }

    private async Task LoadImageIntoCanvasAsync(
        byte[] imageBytes,
        int width,
        int height,
        bool fitToWindow,
        string? currentCanvasSummary = null)
    {
        ResetCanvasRelatedState(clearGeneratedCode: false);
        Canvas.LoadImage(imageBytes, width, height);

        _hasScreenshot = true;
        ResolutionText.Text = $"分辨率：{width}x{height}";
        UpdateCurrentCanvasSourceSummary(currentCanvasSummary ?? $"当前画布：已就绪 · {width}x{height}");

        if (fitToWindow)
        {
            Canvas.FitToWindow();
            _isFitToWindowMode = true;
        }
        else
        {
            _isFitToWindowMode = false;
        }

        UpdateSourceSummaries();
        UpdateStagePresentation();
        UpdateButtonStates();

        await Task.CompletedTask;
    }

    private MatchSearchScope GetMatchSearchScope()
    {
        return FullImageSearchCheckBox?.IsChecked == true
            ? MatchSearchScope.FullImage
            : MatchSearchScope.Region;
    }

    private void SetStatus(string message, StatusTone tone)
    {
        if (StatusText == null || StatusPillBorder == null || StatusIcon == null)
        {
            return;
        }

        var (foreground, background, glyph) = tone switch
        {
            StatusTone.Success => (Colors.ForestGreen, Windows.UI.Color.FromArgb(20, 34, 139, 34), "\uE73E"),
            StatusTone.Warning => (Colors.DarkOrange, Windows.UI.Color.FromArgb(24, 255, 140, 0), "\uE7BA"),
            StatusTone.Error => (Colors.IndianRed, Windows.UI.Color.FromArgb(24, 205, 92, 92), "\uEA39"),
            _ => (Colors.DodgerBlue, Windows.UI.Color.FromArgb(20, 30, 144, 255), "\uE946")
        };

        var brush = new SolidColorBrush(foreground);
        StatusText.Text = message;
        StatusText.Foreground = brush;
        StatusIcon.Glyph = glyph;
        StatusIcon.Foreground = brush;
        StatusPillBorder.Background = new SolidColorBrush(background);
        StatusPillBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(36, foreground.R, foreground.G, foreground.B));
    }
}
