using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Models;
using Core.Services;
using Infrastructure.Imaging;

namespace App.Views;

/// <summary>
/// 主页面。
/// </summary>
public sealed partial class MainPage : Page
{
    private readonly IAdbService _adbService;
    private readonly IOpenCVMatchService _openCvMatchService;
    private readonly ImageProcessor _imageProcessor;

    private AdbDevice? _currentDevice;
    private WidgetNode? _selectedWidget;

    private bool _hasScreenshot;
    private bool _isFitToWindowMode;
    private bool _isCroppingMode;
    private bool _isDumpUiLoading;

    private int _uiTotalNodes;
    private int _uiDisplayedNodes;

    private string? _templateFilePath;
    private string? _screenshotFilePath;
    private CropRegion? _currentCropRegion;
    private string _saveFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "autojs6-templates");

    private const int MatchRegionPadding = 20;

    public MainPage()
    {
        InitializeComponent();
        Loaded += MainPage_Loaded;

        _adbService = new Infrastructure.Adb.AdbServiceImpl();
        _openCvMatchService = new OpenCVMatchServiceImpl();
        _imageProcessor = new ImageProcessor();

        DeviceList.DeviceSelected += DeviceList_DeviceSelected;
        DeviceList.RefreshStatusChanged += DeviceList_RefreshStatusChanged;
        Services.LogService.Instance.LogMessageReceived += OnLogMessageReceived;

        Canvas.ScaleChanged += Canvas_ScaleChanged;
        Canvas.WidgetSelected += Canvas_WidgetSelected;
        Canvas.CropRegionChanged += Canvas_CropRegionChanged;
        PropertyPanel.CodeGenerated += PropertyPanel_CodeGenerated;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateSaveFolderDisplay();
        UpdateSourceSummaries();
        UpdateRegionRefDisplay();
        UpdateUiTreeSummary();
        UpdateSelectedWidgetSummary();
        UpdateWorkbenchModeUi();
        UpdateStagePresentation();
        UpdateButtonStates();
        SetStatus("就绪", StatusTone.Info);
    }

    private void Canvas_ScaleChanged(object? sender, float scale)
    {
        ScaleText.Text = $"缩放：{scale * 100:F0}%";
        HudScaleText.Text = $"缩放：{scale * 100:F0}%";
    }

    private void Canvas_WidgetSelected(object? sender, WidgetNode widget)
    {
        SelectWidget(widget, syncTreeSelection: true);
        SetStatus($"已选择控件：{widget.ClassName}", StatusTone.Info);
    }

    private void Canvas_CropRegionChanged(object? sender, CropRegion? cropRegion)
    {
        var previousCropRegion = _currentCropRegion;
        _currentCropRegion = cropRegion;

        if (!_suspendCropStateTracking && !CropRegionEquals(previousCropRegion, cropRegion))
        {
            _savedCropTemplatePath = null;
            if (TemplateSourceCrop?.IsChecked == true)
            {
                InvalidateSuccessfulMatchContext(clearCanvasResults: true);
            }
        }

        if (cropRegion != null)
        {
            SetStatus($"裁剪区域已更新：{cropRegion.Width}x{cropRegion.Height}", StatusTone.Info);
        }

        UpdateRegionRefDisplay();
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
        UpdateCurrentDeviceSummary();
        UpdateButtonStates();
        SetStatus($"已连接设备：{device.Serial}", StatusTone.Success);
    }

    private void DeviceList_RefreshStatusChanged(object? sender, AdbDeviceListView.DeviceRefreshStatusChangedEventArgs e)
    {
        var tone = e.Tone switch
        {
            AdbDeviceListView.RefreshFeedbackTone.Success => StatusTone.Success,
            AdbDeviceListView.RefreshFeedbackTone.Warning => StatusTone.Warning,
            AdbDeviceListView.RefreshFeedbackTone.Error => StatusTone.Error,
            _ => StatusTone.Info
        };

        if (tone == StatusTone.Info)
        {
            SetStatus(e.Message, tone);
            return;
        }

        ShowActionTip(e.Message, tone);
    }

    private async void AboutAuthorButton_Click(object sender, RoutedEventArgs e)
    {
        var launched = await Windows.System.Launcher.LaunchUriAsync(new Uri("https://terwer.space"));
        ShowActionTip(
            launched ? "已打开作者主页" : "打开作者主页失败",
            launched ? StatusTone.Success : StatusTone.Error,
            AboutAuthorButton);
    }

    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentDevice == null)
        {
            ShowActionTip("请先选择设备", StatusTone.Warning, CaptureButton, "无法截图");
            return;
        }

        try
        {
            SetStatus("正在截图...", StatusTone.Info);

            var (screenshot, width, height) = await _adbService.CaptureScreenshotAsync(_currentDevice);
            Services.LogService.Instance.Log($"[Capture] Framebuffer 实际尺寸: {width}x{height}");

            DiscardExternalScreenshotPreviewSnapshot();
            await LoadImageIntoCanvasAsync(
                screenshot,
                width,
                height,
                fitToWindow: false,
                currentCanvasSummary: $"当前画布：设备截图 {width}x{height}");
            _templateFilePath = null;
            _screenshotFilePath = null;

            ShowActionTip("截图完成", StatusTone.Success, CaptureButton);
        }
        catch (Exception ex)
        {
            ShowActionTip($"截图失败：{ex.Message}", StatusTone.Error, CaptureButton, "截图失败");
        }
    }

    private async void DumpUiButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isDumpUiLoading)
        {
            return;
        }

        if (_currentDevice == null)
        {
            ShowActionTip("请先选择设备", StatusTone.Warning, DumpUiStageButton, "无法拉取 UI 树");
            return;
        }

        if (!_hasScreenshot)
        {
            ShowActionTip("请先准备当前截图", StatusTone.Warning, DumpUiStageButton, "无法拉取 UI 树");
            return;
        }

        try
        {
            _isDumpUiLoading = true;
            SetDumpUiLoading(true);
            SetStatus("正在拉取 UI 树...", StatusTone.Info);

            var xmlContent = await _adbService.DumpUiHierarchyAsync(_currentDevice);
            Services.LogService.Instance.Log($"[DumpUI] XML 长度: {xmlContent.Length} 字符");

            var parser = new UiDumpParser();
            var root = await parser.ParseAsync(xmlContent);
            if (root == null)
            {
                _uiRootNode = null;
                _uiTotalNodes = 0;
                _uiDisplayedNodes = 0;
                Canvas.SetWidgetNodes([]);
                RebuildUiNodeTree();
                ShowActionTip("UI 树解析失败", StatusTone.Error, DumpUiStageButton, "拉取失败");
                return;
            }

            _uiRootNode = root;
            _uiTotalNodes = CountAllNodes(root);

            var nodes = parser.FilterNodes(root);
            if (nodes.Count < 5)
            {
                nodes = GetAllNodes(root);
            }

            _uiDisplayedNodes = nodes.Count;
            Canvas.SetWidgetNodes(nodes);
            Canvas.SetSelectedWidget(null);
            RebuildUiNodeTree();

            ShowActionTip($"UI 树拉取完成，共 {nodes.Count} 个控件", StatusTone.Success, DumpUiStageButton);
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[DumpUI] 异常: {ex.Message}");
            ShowActionTip($"拉取 UI 树失败：{ex.Message}", StatusTone.Error, DumpUiStageButton, "拉取失败");
        }
        finally
        {
            _isDumpUiLoading = false;
            SetDumpUiLoading(false);
            UpdateButtonStates();
        }
    }

    private void EnterCropButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasScreenshot)
        {
            ShowActionTip("请先截图或载入本地图片", StatusTone.Warning, EnterCropButton, "无法进入裁剪");
            return;
        }

        if (_isFitToWindowMode)
        {
            ShowActionTip("裁剪模式仅在 1:1 视图下可用", StatusTone.Warning, EnterCropButton, "无法进入裁剪");
            return;
        }

        if (!Canvas.EnableCroppingMode())
        {
            _isCroppingMode = false;
            ApplyCropButtonVisualState();
            ShowActionTip("裁剪模式启用失败，请确保处于 1:1 模式", StatusTone.Warning, EnterCropButton, "无法进入裁剪");
            return;
        }

        _isCroppingMode = true;
        ApplyCropButtonVisualState();
        UpdateButtonStates();
        ShowActionTip("裁剪模式已启用，可拖拽创建区域", StatusTone.Success, ExitCropButton, "已进入裁剪");
    }

    private void ExitCropButton_Click(object sender, RoutedEventArgs e)
    {
        Canvas.DisableCroppingMode();
        _isCroppingMode = false;
        ApplyCropButtonVisualState();
        UpdateButtonStates();
        ShowActionTip("裁剪模式已禁用", StatusTone.Info, EnterCropButton, "已退出裁剪");
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
        InvalidateSuccessfulMatchContext(clearCanvasResults: true);
        UpdateSourceSummaries();
        UpdateButtonStates();
        ShowActionTip($"已选择模板：{file.Name}", StatusTone.Success, TemplateBrowseButton, "模板已更新");
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
        InvalidateSuccessfulMatchContext(clearCanvasResults: true);
        UpdateSourceSummaries();
        UpdateButtonStates();
        ShowActionTip($"已选择测试截图：{file.Name}", StatusTone.Success, ScreenshotBrowseButton, "截图源已更新");
    }

    private void TemplateSource_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSourceSummaries();
        UpdateRegionRefDisplay();
        UpdateButtonStates();
    }

    private void ScreenshotSource_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSourceSummaries();
        UpdateRegionRefDisplay();
        UpdateButtonStates();
    }

    private void ThresholdSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (ThresholdValueText != null)
        {
            ThresholdValueText.Text = e.NewValue.ToString("F2");
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
        ShowActionTip("代码目录已更改", StatusTone.Success, GenerateCodeButton, "目录已更新");
    }

    private void UpdateSaveFolderDisplay()
    {
        if (SaveFolderText != null)
        {
            SaveFolderText.Text = $"代码目录：{_saveFolderPath}";
        }
    }

    private void FitToWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Canvas.FitToWindow();
        _isFitToWindowMode = true;
        UpdateStagePresentation();
        UpdateButtonStates();
        SetStatus($"已切换到适应窗口（{Canvas.GetScale() * 100:F0}%）", StatusTone.Info);
    }

    private void ResetViewButton_Click(object sender, RoutedEventArgs e)
    {
        Canvas.ResetView();
        _isFitToWindowMode = false;
        UpdateStagePresentation();
        UpdateButtonStates();
        SetStatus("已切换到原图模式（1:1）", StatusTone.Info);
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        DebugLogText.Text = string.Empty;
        SetStatus("日志已清空", StatusTone.Info);
    }

    private void SelectAllLogButton_Click(object sender, RoutedEventArgs e)
    {
        DebugLogText.SelectAll();
    }

    private void CopyLogButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedText = DebugLogText.SelectedText;
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            return;
        }

        CopyToClipboard(selectedText);
        SetStatus("日志已复制到剪贴板", StatusTone.Success);
    }
}
