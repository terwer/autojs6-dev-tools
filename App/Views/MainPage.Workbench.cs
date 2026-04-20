using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;

namespace App.Views;

public sealed partial class MainPage
{
    private enum WorkbenchMode
    {
        Image,
        Ui
    }

    private enum BottomDockTab
    {
        Code = 0,
        Log = 1
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
    private BottomDockTab _bottomDockTab = BottomDockTab.Code;

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
        _workbenchMode = mode;
        UpdateWorkbenchModeUi();
        UpdateButtonStates();
        UpdateStagePresentation();
    }

    private void UpdateWorkbenchModeUi()
    {
        if (ImageModeButton == null ||
            UiModeButton == null ||
            ImageInspectorPanel == null ||
            UiInspectorPanel == null ||
            DumpUiButton == null ||
            ImageToolbarPanel == null ||
            UiToolbarPanel == null)
        {
            return;
        }

        var isImageMode = _workbenchMode == WorkbenchMode.Image;
        ImageModeButton.IsChecked = isImageMode;
        UiModeButton.IsChecked = !isImageMode;

        ImageInspectorPanel.Visibility = isImageMode ? Visibility.Visible : Visibility.Collapsed;
        UiInspectorPanel.Visibility = isImageMode ? Visibility.Collapsed : Visibility.Visible;
        DumpUiButton.Visibility = isImageMode ? Visibility.Collapsed : Visibility.Visible;
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
            TopFitToWindowButton == null ||
            TopResetViewButton == null ||
            DumpUiButton == null ||
            DumpUiInspectorButton == null ||
            DumpUiStageButton == null ||
            StartCropButton == null ||
            WidgetBoundsCheckBox == null ||
            TestMatchButton == null ||
            SaveTemplateButton == null ||
            CopyCoordinatesButton == null ||
            CopySelectorButton == null ||
            PreviewWidgetSnippetButton == null ||
            TemplateSourceCrop == null ||
            TemplateSourceFile == null ||
            ScreenshotSourceCurrent == null ||
            ScreenshotSourceFile == null ||
            TemplateBrowseButton == null ||
            ScreenshotBrowseButton == null)
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

        CaptureButton.IsEnabled = _currentDevice != null;
        LoadLocalTopButton.IsEnabled = true;

        FitToWindowButton.IsEnabled = hasScreenshot;
        ResetViewButton.IsEnabled = hasScreenshot;
        TopFitToWindowButton.IsEnabled = hasScreenshot;
        TopResetViewButton.IsEnabled = hasScreenshot;

        DumpUiButton.IsEnabled = canDumpUi;
        DumpUiInspectorButton.IsEnabled = canDumpUi;
        DumpUiStageButton.IsEnabled = canDumpUi;

        StartCropButton.IsEnabled = hasScreenshot && !_isFitToWindowMode && _workbenchMode == WorkbenchMode.Image;
        WidgetBoundsCheckBox.IsEnabled = hasScreenshot && _workbenchMode == WorkbenchMode.Ui;

        TemplateBrowseButton.IsEnabled = TemplateSourceFile.IsChecked == true;
        ScreenshotBrowseButton.IsEnabled = ScreenshotSourceFile.IsChecked == true;

        TestMatchButton.IsEnabled = hasTemplateSource && hasScreenshotSource;
        SaveTemplateButton.IsEnabled = _currentCropRegion != null;

        CopyCoordinatesButton.IsEnabled = hasWidget;
        CopySelectorButton.IsEnabled = hasWidget;
        PreviewWidgetSnippetButton.IsEnabled = hasWidget;

        ToolTipService.SetToolTip(DumpUiButton, canDumpUi ? null : "请先选择设备并准备一张当前截图");
        ToolTipService.SetToolTip(DumpUiInspectorButton, canDumpUi ? null : "请先选择设备并准备一张当前截图");
        ToolTipService.SetToolTip(DumpUiStageButton, canDumpUi ? null : "请先选择设备并准备一张当前截图");
        ToolTipService.SetToolTip(FitToWindowButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(ResetViewButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(TopFitToWindowButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(TopResetViewButton, hasScreenshot ? null : "请先截图或载入本地图片");
    }

    private void UpdateStagePresentation()
    {
        if (StageEmptyStateOverlay == null ||
            ScaleText == null ||
            HudScaleText == null ||
            HudResolutionText == null ||
            ResolutionText == null ||
            HudCropText == null)
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

    private void ResetCanvasRelatedState(bool clearCodePreview)
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
            StartCropButton.Content = "开启裁剪";
        }

        PropertyPanel?.SetWidget(null);
        UpdateSelectedWidgetSummary();
        UpdateUiTreeSummary();
        RebuildUiNodeTree();

        if (MatchResultText != null)
        {
            MatchResultText.Text = "结果：等待执行匹配测试";
        }

        if (clearCodePreview && CodePreviewTextBox != null)
        {
            CodePreviewTextBox.Text = string.Empty;
        }
    }

    private void ToggleDockButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBottomDockOpen)
        {
            CloseBottomDock();
            return;
        }

        OpenBottomDock(_bottomDockTab);
    }

    private void ShowCodeDockButton_Click(object sender, RoutedEventArgs e)
    {
        OpenBottomDock(BottomDockTab.Code);
    }

    private void ShowLogDockButton_Click(object sender, RoutedEventArgs e)
    {
        OpenBottomDock(BottomDockTab.Log);
    }

    private void OpenBottomDock(BottomDockTab tab)
    {
        if (BottomDockPanel == null || BottomDockTabView == null || ToggleDockButton == null)
        {
            return;
        }

        _bottomDockTab = tab;
        _isBottomDockOpen = true;

        BottomDockPanel.Visibility = Visibility.Visible;
        BottomDockTabView.SelectedIndex = (int)tab;
        ToggleDockButton.IsChecked = true;
        ToggleDockButton.Content = "收起输出";
    }

    private void CloseBottomDock()
    {
        if (BottomDockPanel == null || ToggleDockButton == null)
        {
            return;
        }

        _isBottomDockOpen = false;
        BottomDockPanel.Visibility = Visibility.Collapsed;
        ToggleDockButton.IsChecked = false;
        ToggleDockButton.Content = "展开输出";
    }

    private void BottomDockTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _bottomDockTab = BottomDockTabView.SelectedIndex == (int)BottomDockTab.Log
            ? BottomDockTab.Log
            : BottomDockTab.Code;
    }

    private void WidgetBoundsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
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

    private void PreviewWidgetSnippetButton_Click(object sender, RoutedEventArgs e)
    {
        var snippet = PropertyPanel.GetClickSnippet();
        if (string.IsNullOrWhiteSpace(snippet))
        {
            SetStatus("请先在画布或节点树中选择控件", StatusTone.Warning);
            return;
        }

        CodePreviewTextBox.Text = snippet;
        OpenBottomDock(BottomDockTab.Code);
        SetStatus("控件代码片段已写入代码预览", StatusTone.Success);
    }

    private void PropertyPanel_CodeGenerated(object? sender, string code)
    {
        CodePreviewTextBox.Text = code;
        OpenBottomDock(BottomDockTab.Code);
    }

    private async Task LoadImageIntoCanvasAsync(byte[] imageBytes, int width, int height, bool fitToWindow)
    {
        ResetCanvasRelatedState(clearCodePreview: false);
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

    private void CopyToClipboard(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }

    private void SetStatus(string message, StatusTone tone)
    {
        if (StatusText == null || StatusPillBorder == null || StatusIcon == null)
        {
            return;
        }

        var (foreground, background, glyph) = tone switch
        {
            StatusTone.Success => (Colors.ForestGreen, Windows.UI.Color.FromArgb(24, 34, 139, 34), "\uE73E"),
            StatusTone.Warning => (Colors.DarkOrange, Windows.UI.Color.FromArgb(28, 255, 140, 0), "\uE7BA"),
            StatusTone.Error => (Colors.IndianRed, Windows.UI.Color.FromArgb(28, 205, 92, 92), "\uEA39"),
            _ => (Colors.DodgerBlue, Windows.UI.Color.FromArgb(24, 30, 144, 255), "\uE946")
        };

        var brush = new SolidColorBrush(foreground);
        StatusText.Text = message;
        StatusText.Foreground = brush;
        StatusIcon.Glyph = glyph;
        StatusIcon.Foreground = brush;
        StatusPillBorder.Background = new SolidColorBrush(background);
        StatusPillBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(42, foreground.R, foreground.G, foreground.B));
    }
}
