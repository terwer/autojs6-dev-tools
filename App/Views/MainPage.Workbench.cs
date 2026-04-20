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
            ViewCodeTopButton == null ||
            FitToWindowButton == null ||
            ResetViewButton == null ||
            StartCropButton == null ||
            DumpUiInspectorButton == null ||
            DumpUiStageButton == null ||
            WidgetBoundsCheckBox == null ||
            TestMatchButton == null ||
            SaveTemplateButton == null ||
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

        var hasScreenshot = _hasScreenshot;
        var canDumpUi = hasScreenshot && _currentDevice != null;
        var hasTemplateSource = (TemplateSourceCrop.IsChecked == true && _currentCropRegion != null) ||
                                (TemplateSourceFile.IsChecked == true && !string.IsNullOrWhiteSpace(_templateFilePath));
        var hasScreenshotSource = (ScreenshotSourceCurrent.IsChecked == true && hasScreenshot) ||
                                  (ScreenshotSourceFile.IsChecked == true && !string.IsNullOrWhiteSpace(_screenshotFilePath));
        var hasWidget = _selectedWidget != null;
        var hasGeneratedCode = !string.IsNullOrWhiteSpace(_latestGeneratedCode);

        CaptureButton.IsEnabled = _currentDevice != null;
        LoadLocalTopButton.IsEnabled = true;
        ViewCodeTopButton.IsEnabled = hasGeneratedCode;

        FitToWindowButton.IsEnabled = hasScreenshot;
        ResetViewButton.IsEnabled = hasScreenshot;
        StartCropButton.IsEnabled = hasScreenshot && !_isFitToWindowMode && _workbenchMode == WorkbenchMode.Image;

        WidgetBoundsCheckBox.IsEnabled = hasScreenshot && _workbenchMode == WorkbenchMode.Ui;

        TemplateBrowseButton.IsEnabled = TemplateSourceFile.IsChecked == true;
        ScreenshotBrowseButton.IsEnabled = ScreenshotSourceFile.IsChecked == true;

        TestMatchButton.IsEnabled = hasTemplateSource && hasScreenshotSource;
        SaveTemplateButton.IsEnabled = _currentCropRegion != null;
        ViewCodeRightButton.IsEnabled = hasGeneratedCode;

        CopyCoordinatesButton.IsEnabled = hasWidget;
        CopySelectorButton.IsEnabled = hasWidget;
        PreviewWidgetSnippetButton.IsEnabled = hasWidget;
        ShowLogButton.IsEnabled = true;

        DumpUiInspectorButton.IsEnabled = canDumpUi && !_isDumpUiLoading;
        DumpUiStageButton.IsEnabled = canDumpUi && !_isDumpUiLoading;
        SetDumpUiLoading(_isDumpUiLoading);
        ApplyCropButtonVisualState();

        ToolTipService.SetToolTip(DumpUiInspectorButton, canDumpUi ? null : "请先选择设备并准备一张当前截图");
        ToolTipService.SetToolTip(DumpUiStageButton, canDumpUi ? null : "请先选择设备并准备一张当前截图");
        ToolTipService.SetToolTip(FitToWindowButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(ResetViewButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(ViewCodeTopButton, hasGeneratedCode ? null : "请先执行保存或生成代码");
        ToolTipService.SetToolTip(ViewCodeRightButton, hasGeneratedCode ? null : "请先执行保存或生成代码");
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
            : $"裁剪区域：{_currentCropRegion.Width}x{_currentCropRegion.Height}";
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

        TemplateSourceSummaryText.Text = TemplateSourceCrop.IsChecked == true
            ? _currentCropRegion == null
                ? "当前裁剪：尚未创建区域"
                : $"当前裁剪：{_currentCropRegion.Width}x{_currentCropRegion.Height} 区域"
            : string.IsNullOrWhiteSpace(_templateFilePath)
                ? "模板文件：尚未选择"
                : _templateFilePath!;

        ScreenshotSourceSummaryText.Text = ScreenshotSourceCurrent.IsChecked == true
            ? _hasScreenshot
                ? "当前画布：已就绪"
                : "当前画布：尚未加载截图"
            : string.IsNullOrWhiteSpace(_screenshotFilePath)
                ? "截图文件：尚未选择"
                : _screenshotFilePath!;
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

        _isCroppingMode = false;
        _selectedWidget = null;
        _uiRootNode = null;
        _uiTotalNodes = 0;
        _uiDisplayedNodes = 0;

        if (StartCropButton != null)
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

        _isCroppingMode = false;
        _currentCropRegion = null;
        _templateFilePath = null;
        _screenshotFilePath = null;
        _selectedWidget = null;
        _uiRootNode = null;
        _uiTotalNodes = 0;
        _uiDisplayedNodes = 0;
        _latestGeneratedCode = string.Empty;
        _latestImageCodePreviewItems.Clear();

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

        if (StartCropButton != null)
        {
            ApplyCropButtonVisualState();
        }

        if (RegionRefTextBox != null)
        {
            RegionRefTextBox.Text = "[等待裁剪...]";
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
        UpdateSourceSummaries();
        UpdateButtonStates();
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

            await LoadImageIntoCanvasAsync(bytes, (int)decoder.PixelWidth, (int)decoder.PixelHeight, fitToWindow: false);

            _screenshotFilePath = file.Path;
            ScreenshotSourceCurrent.IsChecked = true;

            SetStatus($"已载入本地图片：{file.Name}", StatusTone.Success);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"载入本地图片失败：{ex.Message}");
            SetStatus("载入本地图片失败", StatusTone.Error);
        }
    }

    private void CopyRegionRefButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RegionRefTextBox.Text) || RegionRefTextBox.Text == "[等待裁剪...]")
        {
            SetStatus("当前没有可复制的 regionRef", StatusTone.Warning);
            return;
        }

        CopyToClipboard(RegionRefTextBox.Text);
        SetStatus("regionRef 已复制到剪贴板", StatusTone.Success);
    }

    private void CopyCoordinatesButton_Click(object sender, RoutedEventArgs e)
    {
        var coordinates = PropertyPanel.GetCoordinatesText();
        if (string.IsNullOrWhiteSpace(coordinates))
        {
            SetStatus("请先在画布或节点树中选择控件", StatusTone.Warning);
            return;
        }

        CopyToClipboard(coordinates);
        SetStatus("控件坐标已复制到剪贴板", StatusTone.Success);
    }

    private void CopySelectorButton_Click(object sender, RoutedEventArgs e)
    {
        var selector = PropertyPanel.GetUiSelectorText();
        if (string.IsNullOrWhiteSpace(selector))
        {
            SetStatus("请先在画布或节点树中选择控件", StatusTone.Warning);
            return;
        }

        CopyToClipboard(selector);
        SetStatus("UiSelector 已复制到剪贴板", StatusTone.Success);
    }

    private async Task LoadImageIntoCanvasAsync(byte[] imageBytes, int width, int height, bool fitToWindow)
    {
        ResetCanvasRelatedState(clearGeneratedCode: false);
        Canvas.LoadImage(imageBytes, width, height);

        _hasScreenshot = true;
        ResolutionText.Text = $"分辨率：{width}x{height}";

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
