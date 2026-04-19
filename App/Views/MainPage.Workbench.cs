using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        var isImageMode = _workbenchMode == WorkbenchMode.Image;

        ImageModeButton.IsChecked = isImageMode;
        UiModeButton.IsChecked = !isImageMode;

        ImageInspectorPanel.Visibility = isImageMode ? Visibility.Visible : Visibility.Collapsed;
        UiInspectorPanel.Visibility = isImageMode ? Visibility.Collapsed : Visibility.Visible;
        DumpUiButton.Visibility = isImageMode ? Visibility.Collapsed : Visibility.Visible;
        StartCropButton.Visibility = isImageMode ? Visibility.Visible : Visibility.Collapsed;
        WidgetBoundsCheckBox.Visibility = isImageMode ? Visibility.Collapsed : Visibility.Visible;

        var modeText = isImageMode ? "图像模式" : "控件模式";
        ModeStatusText.Text = $"当前模式：{modeText}";
        HudModeText.Text = $"模式：{modeText}";
    }

    private void UpdateButtonStates()
    {
        var hasScreenshot = _hasScreenshot;
        var canDumpUi = hasScreenshot && _isFitToWindowMode && _currentDevice != null;
        var hasTemplateSource = (TemplateSourceCrop.IsChecked == true && _currentCropRegion != null) ||
                                (TemplateSourceFile.IsChecked == true && !string.IsNullOrWhiteSpace(_templateFilePath));
        var hasScreenshotSource = (ScreenshotSourceCurrent.IsChecked == true && hasScreenshot) ||
                                  (ScreenshotSourceFile.IsChecked == true && !string.IsNullOrWhiteSpace(_screenshotFilePath));
        var hasWidget = _selectedWidget != null;

        CaptureButton.IsEnabled = _currentDevice != null;

        FitToWindowButton.IsEnabled = hasScreenshot;
        ResetViewButton.IsEnabled = hasScreenshot;
        TopFitToWindowButton.IsEnabled = hasScreenshot;
        TopResetViewButton.IsEnabled = hasScreenshot;

        DumpUiButton.IsEnabled = canDumpUi;
        DumpUiInspectorButton.IsEnabled = canDumpUi;

        StartCropButton.IsEnabled = hasScreenshot && !_isFitToWindowMode && _workbenchMode == WorkbenchMode.Image;
        WidgetBoundsCheckBox.IsEnabled = hasScreenshot && _workbenchMode == WorkbenchMode.Ui;

        TestMatchButton.IsEnabled = hasTemplateSource && hasScreenshotSource;
        SaveTemplateButton.IsEnabled = _currentCropRegion != null;

        CopyCoordinatesButton.IsEnabled = hasWidget;
        CopySelectorButton.IsEnabled = hasWidget;
        PreviewWidgetSnippetButton.IsEnabled = hasWidget;

        ToolTipService.SetToolTip(DumpUiButton, !hasScreenshot
            ? "请先截图或载入当前画布内容"
            : !_isFitToWindowMode
                ? "请先切换到适应窗口模式"
                : _currentDevice == null
                    ? "请先选择设备"
                    : null);

        ToolTipService.SetToolTip(TopFitToWindowButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(TopResetViewButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(FitToWindowButton, hasScreenshot ? null : "请先截图或载入本地图片");
        ToolTipService.SetToolTip(ResetViewButton, hasScreenshot ? null : "请先截图或载入本地图片");
    }

    private void UpdateStagePresentation()
    {
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
        CurrentDeviceSummaryText.Text = _currentDevice == null
            ? "当前设备：尚未选择"
            : $"当前设备：{_currentDevice.Serial} · {(_currentDevice.Model ?? "未知型号")}";
    }

    private void UpdateSourceSummaries()
    {
        TemplateSourceSummaryText.Text = TemplateSourceCrop.IsChecked == true
            ? _currentCropRegion == null
                ? "当前裁剪：尚未创建区域"
                : $"当前裁剪：{_currentCropRegion.Width}x{_currentCropRegion.Height} 区域"
            : string.IsNullOrWhiteSpace(_templateFilePath)
                ? "模板文件：尚未选择"
                : $"模板文件：{_templateFilePath}";

        ScreenshotSourceSummaryText.Text = ScreenshotSourceCurrent.IsChecked == true
            ? _hasScreenshot
                ? "当前画布：已加载截图，可直接作为测试目标"
                : "当前画布：尚未加载截图"
            : string.IsNullOrWhiteSpace(_screenshotFilePath)
                ? "截图文件：尚未选择"
                : $"截图文件：{_screenshotFilePath}";
    }

    private void UpdateUiTreeSummary()
    {
        UiTreeSummaryText.Text = _uiDisplayedNodes <= 0
            ? "尚未拉取 UI 树"
            : $"已显示 {_uiDisplayedNodes} 个控件（原始节点 {_uiTotalNodes}）";
    }

    private void UpdateSelectedWidgetSummary()
    {
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
        Canvas.SetCropRegion(null);
        Canvas.DisableCroppingMode();

        _isCroppingMode = false;
        StartCropButton.Content = "开启裁剪";

        _selectedWidget = null;
        PropertyPanel.SetWidget(null);
        UpdateSelectedWidgetSummary();

        _uiTotalNodes = 0;
        _uiDisplayedNodes = 0;
        UpdateUiTreeSummary();

        MatchResultText.Text = "结果：匹配执行未接入，当前仅完成界面占位";

        if (clearCodePreview)
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
        _bottomDockTab = tab;
        _isBottomDockOpen = true;

        BottomDockPanel.Visibility = Visibility.Visible;
        BottomDockTabView.SelectedIndex = (int)tab;

        ToggleDockButton.IsChecked = true;
        ToggleDockButton.Content = "收起输出";
    }

    private void CloseBottomDock()
    {
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

            ResetCanvasRelatedState(clearCodePreview: false);
            StatusText.Text = "正在载入本地图片...";

            var bytes = await File.ReadAllBytesAsync(file.Path);
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);

            Canvas.LoadImage(bytes, (int)decoder.PixelWidth, (int)decoder.PixelHeight);

            _hasScreenshot = true;
            _isFitToWindowMode = false;
            _screenshotFilePath = file.Path;
            ScreenshotSourceCurrent.IsChecked = true;

            ResolutionText.Text = $"分辨率：{decoder.PixelWidth}x{decoder.PixelHeight}";
            StatusText.Text = $"已载入本地图片：{file.Name}";

            UpdateSourceSummaries();
            UpdateStagePresentation();
            UpdateButtonStates();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"载入本地图片失败：{ex.Message}");
            StatusText.Text = "载入本地图片失败";
        }
    }

    private void CopyRegionRefButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RegionRefTextBox.Text) || RegionRefTextBox.Text == "[等待裁剪...]")
        {
            StatusText.Text = "当前没有可复制的 regionRef";
            return;
        }

        CopyToClipboard(RegionRefTextBox.Text);
        StatusText.Text = "regionRef 已复制到剪贴板";
    }

    private void CopyCoordinatesButton_Click(object sender, RoutedEventArgs e)
    {
        var coordinates = PropertyPanel.GetCoordinatesText();
        if (string.IsNullOrWhiteSpace(coordinates))
        {
            StatusText.Text = "请先在画布中选择控件";
            return;
        }

        CopyToClipboard(coordinates);
        StatusText.Text = "控件坐标已复制到剪贴板";
    }

    private void CopySelectorButton_Click(object sender, RoutedEventArgs e)
    {
        var selector = PropertyPanel.GetUiSelectorText();
        if (string.IsNullOrWhiteSpace(selector))
        {
            StatusText.Text = "请先在画布中选择控件";
            return;
        }

        CopyToClipboard(selector);
        StatusText.Text = "UiSelector 已复制到剪贴板";
    }

    private void PreviewWidgetSnippetButton_Click(object sender, RoutedEventArgs e)
    {
        var snippet = PropertyPanel.GetClickSnippet();
        if (string.IsNullOrWhiteSpace(snippet))
        {
            StatusText.Text = "请先在画布中选择控件";
            return;
        }

        CodePreviewTextBox.Text = snippet;
        OpenBottomDock(BottomDockTab.Code);
        StatusText.Text = "控件代码片段已写入代码预览";
    }

    private void PropertyPanel_CodeGenerated(object? sender, string code)
    {
        CodePreviewTextBox.Text = code;
        OpenBottomDock(BottomDockTab.Code);
    }

    private void CopyToClipboard(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }
}
