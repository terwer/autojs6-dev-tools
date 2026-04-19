using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Models;

namespace App.Views;

/// <summary>
/// 主页面
/// 整合设备中心、中央画布和模式化检查器。
/// </summary>
public sealed partial class MainPage : Page
{
    private readonly IAdbService _adbService;

    private AdbDevice? _currentDevice;
    private WidgetNode? _selectedWidget;

    private bool _hasScreenshot;
    private bool _isFitToWindowMode;
    private bool _isCroppingMode;
    private bool _isBottomDockOpen;

    private int _uiTotalNodes;
    private int _uiDisplayedNodes;

    private string? _templateFilePath;
    private string? _screenshotFilePath;
    private CropRegion? _currentCropRegion;
    private string _saveFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "autojs6-templates");

    public MainPage()
    {
        InitializeComponent();
        Loaded += MainPage_Loaded;

        _adbService = new Infrastructure.Adb.AdbServiceImpl();

        DeviceList.DeviceSelected += DeviceList_DeviceSelected;
        Services.LogService.Instance.LogMessageReceived += OnLogMessageReceived;

        Canvas.ScaleChanged += Canvas_ScaleChanged;
        Canvas.WidgetSelected += Canvas_WidgetSelected;
        Canvas.CropRegionChanged += Canvas_CropRegionChanged;

        if (PropertyPanel != null)
        {
            PropertyPanel.CodeGenerated += PropertyPanel_CodeGenerated;
            PropertyPanel.SetWidget(null);
        }
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateSaveFolderDisplay();
        UpdateSourceSummaries();
        UpdateUiTreeSummary();
        UpdateSelectedWidgetSummary();
        UpdateWorkbenchModeUi();
        UpdateStagePresentation();
        UpdateButtonStates();
    }

    private void Canvas_ScaleChanged(object? sender, float scale)
    {
        ScaleText.Text = $"缩放：{scale * 100:F0}%";
        HudScaleText.Text = $"缩放：{scale * 100:F0}%";
    }

    private void Canvas_WidgetSelected(object? sender, WidgetNode widget)
    {
        _selectedWidget = widget;
        PropertyPanel?.SetWidget(widget);
        UpdateSelectedWidgetSummary();
        StatusText.Text = $"已选择控件：{widget.ClassName}";
    }

    private void Canvas_CropRegionChanged(object? sender, CropRegion? cropRegion)
    {
        _currentCropRegion = cropRegion;

        if (cropRegion != null)
        {
            var regionRef = GenerateRegionRef(cropRegion, padding: 20);
            RegionRefTextBox.Text = $"[{string.Join(", ", regionRef)}]";
            StatusText.Text = $"裁剪区域：{cropRegion.Width}x{cropRegion.Height}";
        }
        else
        {
            RegionRefTextBox.Text = "[等待裁剪...]";
        }

        UpdateSourceSummaries();
        UpdateStagePresentation();
        UpdateButtonStates();
    }

    private void OnLogMessageReceived(string logLine)
    {
        if (DebugLogText != null)
        {
            DebugLogText.Text += logLine + "\n";
        }
    }

    private void DeviceList_DeviceSelected(object? sender, AdbDevice device)
    {
        _currentDevice = device;
        StatusText.Text = $"已连接设备：{device.Serial}";
        UpdateCurrentDeviceSummary();
        UpdateButtonStates();
    }

    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentDevice == null)
        {
            await ShowErrorAsync("请先选择设备");
            return;
        }

        try
        {
            StatusText.Text = "正在截图...";
            ResetCanvasRelatedState(clearCodePreview: true);

            var (screenshot, width, height) = await _adbService.CaptureScreenshotAsync(_currentDevice);
            Services.LogService.Instance.Log($"[Capture] Framebuffer 实际尺寸: {width}x{height}");

            Canvas.LoadImage(screenshot, width, height);

            _hasScreenshot = true;
            _isFitToWindowMode = false;

            StatusText.Text = "截图完成";
            ResolutionText.Text = $"分辨率：{width}x{height}";

            UpdateSourceSummaries();
            UpdateStagePresentation();
            UpdateButtonStates();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"截图失败：{ex.Message}");
            StatusText.Text = "截图失败";
        }
    }

    private async void DumpUiButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentDevice == null)
        {
            await ShowErrorAsync("请先选择设备");
            return;
        }

        try
        {
            StatusText.Text = "正在拉取 UI 树...";

            var xmlContent = await _adbService.DumpUiHierarchyAsync(_currentDevice);
            Services.LogService.Instance.Log($"[DumpUI] XML 长度: {xmlContent.Length} 字符");

            var parser = new Core.Services.UiDumpParser();
            var root = await parser.ParseAsync(xmlContent);

            if (root == null)
            {
                Services.LogService.Instance.Log("[DumpUI] 解析失败：root 为 null");
                StatusText.Text = "UI 树解析失败";
                _uiTotalNodes = 0;
                _uiDisplayedNodes = 0;
                UpdateUiTreeSummary();
                return;
            }

            Services.LogService.Instance.Log($"[DumpUI] 根节点: {root.ClassName}, Bounds={root.BoundsRect}");

            _uiTotalNodes = CountAllNodes(root);
            Services.LogService.Instance.Log($"[DumpUI] 总节点数（含子节点）: {_uiTotalNodes}");

            var nodes = parser.FilterNodes(root);
            Services.LogService.Instance.Log($"[DumpUI] 过滤后节点数: {nodes.Count}");

            if (nodes.Count < 5)
            {
                Services.LogService.Instance.Log("[DumpUI] 过滤后节点太少，使用所有节点");
                nodes = GetAllNodes(root);
                Services.LogService.Instance.Log($"[DumpUI] 所有节点数: {nodes.Count}");
            }

            _uiDisplayedNodes = nodes.Count;

            Services.LogService.Instance.Log($"[DumpUI] UI Dump 坐标系统: {root.BoundsRect.Width}x{root.BoundsRect.Height}");
            for (var index = 0; index < Math.Min(5, nodes.Count); index++)
            {
                var node = nodes[index];
                Services.LogService.Instance.Log(
                    $"[DumpUI] 节点 {index}: {node.ClassName}, Bounds=({node.BoundsRect.X}, {node.BoundsRect.Y}, {node.BoundsRect.Width}, {node.BoundsRect.Height})");
            }

            Canvas.SetWidgetNodes(nodes);
            StatusText.Text = $"UI 树拉取完成，共 {nodes.Count} 个控件";

            UpdateUiTreeSummary();
            UpdateButtonStates();
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[DumpUI] 异常: {ex.Message}");
            await ShowErrorAsync($"拉取 UI 树失败：{ex.Message}");
            StatusText.Text = "拉取 UI 树失败";
        }
    }

    private void StartCropButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasScreenshot)
        {
            StatusText.Text = "请先截图";
            return;
        }

        if (_isFitToWindowMode)
        {
            StatusText.Text = "裁剪模式仅在原图模式（1:1）下可用，请点击“原图 1:1”按钮";
            return;
        }

        _isCroppingMode = !_isCroppingMode;

        if (_isCroppingMode)
        {
            var success = Canvas.EnableCroppingMode();
            if (!success)
            {
                _isCroppingMode = false;
                StatusText.Text = "裁剪模式启用失败，请确保处于 1:1 模式";
                UpdateButtonStates();
                return;
            }

            StartCropButton.Content = "退出裁剪";
            StatusText.Text = "裁剪模式已启用，可拖拽创建矩形区域";
        }
        else
        {
            Canvas.DisableCroppingMode();
            StartCropButton.Content = "开启裁剪";
            StatusText.Text = "裁剪模式已禁用";
        }

        UpdateButtonStates();
    }

    private async void BrowseTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = CreateImageFilePicker();
        var file = await picker.PickSingleFileAsync();
        if (file == null)
        {
            return;
        }

        _templateFilePath = file.Path;
        StatusText.Text = $"已选择模板：{file.Name}";
        UpdateSourceSummaries();
        UpdateButtonStates();
    }

    private async void BrowseScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = CreateImageFilePicker();
        var file = await picker.PickSingleFileAsync();
        if (file == null)
        {
            return;
        }

        _screenshotFilePath = file.Path;
        StatusText.Text = $"已选择截图：{file.Name}";
        UpdateSourceSummaries();
        UpdateButtonStates();
    }

    private void TemplateSource_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSourceSummaries();
        UpdateButtonStates();
    }

    private void ScreenshotSource_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSourceSummaries();
        UpdateButtonStates();
    }

    private void ThresholdSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (ThresholdValueText != null)
        {
            ThresholdValueText.Text = e.NewValue.ToString("F2");
        }
    }

    private Task ShowUnimplementedMatchResultAsync()
    {
        MatchResultText.Text = "结果：匹配执行未接入，当前仅完成界面占位";
        StatusText.Text = "匹配测试尚未接入真实执行逻辑";
        return Task.CompletedTask;
    }

    private async void TestMatchButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ShowUnimplementedMatchResultAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"匹配测试失败：{ex.Message}");
            MatchResultText.Text = "结果：匹配执行未接入，当前仅完成界面占位";
            StatusText.Text = "匹配测试失败";
        }
    }

    private async void SelectSaveFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder == null)
        {
            return;
        }

        _saveFolderPath = folder.Path;
        UpdateSaveFolderDisplay();
        StatusText.Text = "保存位置已更改";
    }

    private void UpdateSaveFolderDisplay()
    {
        if (SaveFolderText != null)
        {
            SaveFolderText.Text = _saveFolderPath;
        }
    }

    private async void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentCropRegion == null)
        {
            await ShowErrorAsync("请先创建裁剪区域");
            return;
        }

        try
        {
            StatusText.Text = "正在保存模板和代码...";

            var templateName = TemplateNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(templateName))
            {
                templateName = $"template_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            Directory.CreateDirectory(_saveFolderPath);

            var templatePath = await ExportCroppedTemplate(_currentCropRegion, templateName);
            var regionRef = GenerateRegionRef(_currentCropRegion, padding: 20);
            var code = GenerateMatchTemplateCode(templatePath, regionRef, _currentCropRegion);

            var codePath = Path.ChangeExtension(templatePath, ".js");
            File.WriteAllText(codePath, code);

            CodePreviewTextBox.Text = code;
            OpenBottomDock(BottomDockTab.Code);

            StatusText.Text = $"已保存到：{_saveFolderPath}";

            Services.LogService.Instance.Log($"[保存] 模板: {templatePath}");
            Services.LogService.Instance.Log($"[保存] 代码: {codePath}");
            Services.LogService.Instance.Log($"[保存] 位置: {_saveFolderPath}");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"保存失败：{ex.Message}");
            StatusText.Text = "保存失败";
        }
    }

    private void FitToWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Canvas.FitToWindow();
        var scale = Canvas.GetScale();

        _isFitToWindowMode = true;
        StatusText.Text = $"已切换到适应窗口模式（缩放：{scale * 100:F0}%）";

        UpdateStagePresentation();
        UpdateButtonStates();
    }

    private void ResetViewButton_Click(object sender, RoutedEventArgs e)
    {
        Canvas.ResetView();

        _isFitToWindowMode = false;
        StatusText.Text = "已切换到原图模式（1:1）";

        UpdateStagePresentation();
        UpdateButtonStates();
    }

    private void CopyCodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(CodePreviewTextBox.Text))
        {
            return;
        }

        CopyToClipboard(CodePreviewTextBox.Text);
        StatusText.Text = "代码已复制到剪贴板";
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        DebugLogText.Text = string.Empty;
    }

    private void SelectAllLogButton_Click(object sender, RoutedEventArgs e)
    {
        DebugLogText.SelectAll();
    }

    private void CopyLogButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedText = DebugLogText.SelectedText;
        if (string.IsNullOrEmpty(selectedText))
        {
            return;
        }

        CopyToClipboard(selectedText);
        StatusText.Text = "日志已复制到剪贴板";
    }
}
