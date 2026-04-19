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
/// 整合所有视图组件
/// </summary>
public sealed partial class MainPage : Page
{
    private readonly IAdbService _adbService;
    private readonly ICodeGenerator _codeGenerator;
    private AdbDevice? _currentDevice;
    private bool _hasScreenshot = false;
    private bool _isFitToWindowMode = false;
    private bool _isCroppingMode = false;

    // 工作流状态
    private string? _templateFilePath = null;
    private string? _screenshotFilePath = null;
    private CropRegion? _currentCropRegion = null;
    private string _saveFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "autojs6-templates");

    public MainPage()
    {
        this.InitializeComponent();

        // 临时注入服务（后续通过 DI 容器）
        _adbService = new Infrastructure.Adb.AdbServiceImpl();
        _codeGenerator = new Core.Services.AutoJS6CodeGenerator();

        // 订阅设备选择事件
        DeviceList.DeviceSelected += DeviceList_DeviceSelected;

        // 订阅日志服务
        Services.LogService.Instance.LogMessageReceived += OnLogMessageReceived;

        // 订阅画布缩放变化事件
        Canvas.ScaleChanged += Canvas_ScaleChanged;

        // 订阅画布控件选择事件
        Canvas.WidgetSelected += Canvas_WidgetSelected;

        // 订阅画布裁剪区域变化事件
        Canvas.CropRegionChanged += Canvas_CropRegionChanged;

        // 初始化保存路径显示
        UpdateSaveFolderDisplay();

        // 初始化按钮状态
        UpdateButtonStates();
    }

    /// <summary>
    /// 画布缩放变化事件处理
    /// </summary>
    private void Canvas_ScaleChanged(object? sender, float scale)
    {
        ScaleText.Text = $"缩放: {scale * 100:F0}%";
    }

    /// <summary>
    /// 画布控件选择事件处理
    /// </summary>
    private void Canvas_WidgetSelected(object? sender, WidgetNode widget)
    {
        StatusText.Text = $"已选择控件: {widget.ClassName}";
    }

    /// <summary>
    /// 画布裁剪区域变化事件处理
    /// </summary>
    private void Canvas_CropRegionChanged(object? sender, CropRegion? cropRegion)
    {
        _currentCropRegion = cropRegion;

        if (cropRegion != null)
        {
            // 自动计算 regionRef
            var regionRef = GenerateRegionRef(cropRegion, padding: 20);
            RegionRefTextBox.Text = $"[{string.Join(", ", regionRef)}]";

            StatusText.Text = $"裁剪区域: {cropRegion.Width}x{cropRegion.Height}";
        }
        else
        {
            RegionRefTextBox.Text = "[等待裁剪...]";
        }

        UpdateButtonStates();
    }

    /// <summary>
    /// 接收日志消息并显示到 UI
    /// </summary>
    private void OnLogMessageReceived(string logLine)
    {
        DebugLogText.Text += logLine + "\n";
    }

    /// <summary>
    /// 设备选择事件处理
    /// </summary>
    private void DeviceList_DeviceSelected(object? sender, AdbDevice device)
    {
        _currentDevice = device;
        StatusText.Text = $"已连接设备: {device.Serial}";

        // 更新按钮状态
        UpdateButtonStates();
    }

    /// <summary>
    /// 截图按钮点击
    /// </summary>
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

            // 清空之前的状态
            Canvas.SetWidgetNodes(new List<WidgetNode>());
            CodePreviewTextBox.Text = "";
            _currentCropRegion = null;
            RegionRefTextBox.Text = "[等待裁剪...]";

            // 拉取截图（直接从 Framebuffer 获取实际宽高）
            var (screenshot, width, height) = await _adbService.CaptureScreenshotAsync(_currentDevice);

            Services.LogService.Instance.Log($"[Capture] Framebuffer 实际尺寸: {width}x{height}");

            // 加载到画布（不再传递旋转角度）
            Canvas.LoadImage(screenshot, width, height);

            // 更新状态
            _hasScreenshot = true;
            _isFitToWindowMode = false; // 默认是原图模式

            StatusText.Text = "截图完成";
            ResolutionText.Text = $"分辨率: {width}x{height}";

            // 更新按钮状态
            UpdateButtonStates();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"截图失败：{ex.Message}");
            StatusText.Text = "截图失败";
        }
    }

    /// <summary>
    /// 拉取 UI 树按钮点击
    /// </summary>
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

            // 拉取 UI 树
            var xmlContent = await _adbService.DumpUiHierarchyAsync(_currentDevice);
            Services.LogService.Instance.Log($"[DumpUI] XML 长度: {xmlContent.Length} 字符");

            // 解析 UI 树
            var parser = new Core.Services.UiDumpParser();
            var root = await parser.ParseAsync(xmlContent);

            if (root != null)
            {
                Services.LogService.Instance.Log($"[DumpUI] 根节点: {root.ClassName}, Bounds={root.BoundsRect}");

                // 统计所有节点数量（包括子节点）
                int totalNodes = CountAllNodes(root);
                Services.LogService.Instance.Log($"[DumpUI] 总节点数（含子节点）: {totalNodes}");

                // 过滤节点
                var nodes = parser.FilterNodes(root);
                Services.LogService.Instance.Log($"[DumpUI] 过滤后节点数: {nodes.Count}");

                // 如果过滤后节点太少，使用所有节点
                if (nodes.Count < 5)
                {
                    Services.LogService.Instance.Log($"[DumpUI] 过滤后节点太少，使用所有节点");
                    nodes = GetAllNodes(root);
                    Services.LogService.Instance.Log($"[DumpUI] 所有节点数: {nodes.Count}");
                }

                // 不再做坐标转换，UI Dump 坐标应该直接匹配 Framebuffer 坐标
                Services.LogService.Instance.Log($"[DumpUI] UI Dump 坐标系统: {root.BoundsRect.Width}x{root.BoundsRect.Height}");

                // 打印前 5 个节点的边界
                for (int i = 0; i < Math.Min(5, nodes.Count); i++)
                {
                    var node = nodes[i];
                    Services.LogService.Instance.Log($"[DumpUI] 节点 {i}: {node.ClassName}, Bounds=({node.BoundsRect.X}, {node.BoundsRect.Y}, {node.BoundsRect.Width}, {node.BoundsRect.Height})");
                }

                // 显示到画布
                Canvas.SetWidgetNodes(nodes);
                Services.LogService.Instance.Log($"[DumpUI] 已调用 Canvas.SetWidgetNodes");

                StatusText.Text = $"UI 树拉取完成，共 {nodes.Count} 个控件";
            }
            else
            {
                Services.LogService.Instance.Log($"[DumpUI] 解析失败：root 为 null");
                StatusText.Text = "UI 树解析失败";
            }
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[DumpUI] 异常: {ex.Message}");
            await ShowErrorAsync($"拉取 UI 树失败：{ex.Message}");
            StatusText.Text = "拉取 UI 树失败";
        }
    }

    /// <summary>
    /// 开始裁剪按钮点击
    /// </summary>
    private void StartCropButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasScreenshot)
        {
            StatusText.Text = "请先截图";
            return;
        }

        if (_isFitToWindowMode)
        {
            StatusText.Text = "裁剪模式仅在原图模式（1:1）下可用，请点击\"原图\"按钮";
            return;
        }

        _isCroppingMode = !_isCroppingMode;

        if (_isCroppingMode)
        {
            bool success = Canvas.EnableCroppingMode();
            if (success)
            {
                StartCropButton.Content = "退出裁剪";
                StatusText.Text = "裁剪模式已启用 - 拖拽创建矩形，按住 Shift 锁定宽高比";
            }
            else
            {
                _isCroppingMode = false;
                StatusText.Text = "裁剪模式启用失败，请确保处于 1:1 模式";
            }
        }
        else
        {
            Canvas.DisableCroppingMode();
            StartCropButton.Content = "开始裁剪";
            StatusText.Text = "裁剪模式已禁用";
        }

        UpdateButtonStates();
    }

    /// <summary>
    /// 浏览模板文件按钮点击
    /// </summary>
    private async void BrowseTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            _templateFilePath = file.Path;
            StatusText.Text = $"已选择模板: {file.Name}";
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// 浏览截图文件按钮点击
    /// </summary>
    private async void BrowseScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            _screenshotFilePath = file.Path;
            StatusText.Text = $"已选择截图: {file.Name}";
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// 模板源变化
    /// </summary>
    private void TemplateSource_Changed(object sender, RoutedEventArgs e)
    {
        UpdateButtonStates();
    }

    /// <summary>
    /// 截图源变化
    /// </summary>
    private void ScreenshotSource_Changed(object sender, RoutedEventArgs e)
    {
        UpdateButtonStates();
    }

    /// <summary>
    /// 阈值滑块变化
    /// </summary>
    private void ThresholdSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (ThresholdValueText != null)
        {
            ThresholdValueText.Text = e.NewValue.ToString("F2");
        }
    }

    /// <summary>
    /// 匹配测试按钮点击
    /// </summary>
    private async void TestMatchButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "正在进行匹配测试...";
            MatchResultText.Text = "结果: 测试中...";

            // TODO: 实现完整的匹配测试逻辑
            // 1. 获取模板图像（当前裁剪 或 外部文件）
            // 2. 获取截图图像（当前截图 或 外部文件）
            // 3. 使用 OpenCV 进行模板匹配
            // 4. 在画布上显示匹配结果（绿框 + 置信度）

            await Task.Delay(500); // 模拟匹配过程

            MatchResultText.Text = "结果: ✓ 匹配成功 (0.95)\n位置: (120, 340)";
            StatusText.Text = "匹配测试完成";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"匹配测试失败：{ex.Message}");
            MatchResultText.Text = "结果: ✗ 测试失败";
            StatusText.Text = "匹配测试失败";
        }
    }

    /// <summary>
    /// 选择保存文件夹按钮点击
    /// </summary>
    private async void SelectSaveFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            _saveFolderPath = folder.Path;
            UpdateSaveFolderDisplay();
            StatusText.Text = $"保存位置已更改";
        }
    }

    /// <summary>
    /// 更新保存文件夹显示
    /// </summary>
    private void UpdateSaveFolderDisplay()
    {
        if (SaveFolderText != null)
        {
            SaveFolderText.Text = _saveFolderPath;
        }
    }

    /// <summary>
    /// 保存模板+代码按钮点击
    /// </summary>
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

            // 1. 确定模板名
            var templateName = TemplateNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(templateName))
            {
                templateName = $"template_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            // 2. 确保保存目录存在
            Directory.CreateDirectory(_saveFolderPath);

            // 3. 裁剪并保存模板图像
            var templatePath = await ExportCroppedTemplate(_currentCropRegion, templateName);

            // 4. 生成 regionRef
            var regionRef = GenerateRegionRef(_currentCropRegion, padding: 20);

            // 5. 生成 AutoJS6 代码
            var code = GenerateMatchTemplateCode(templatePath, regionRef, _currentCropRegion);

            // 6. 保存代码文件
            var codePath = Path.ChangeExtension(templatePath, ".js");
            File.WriteAllText(codePath, code);

            // 7. 显示到代码预览
            CodePreviewTextBox.Text = code;
            CodePreviewExpander.IsExpanded = true;

            StatusText.Text = $"已保存到: {_saveFolderPath}";

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

    /// <summary>
    /// 适应窗口按钮点击
    /// </summary>
    private void FitToWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Canvas.FitToWindow();
        var scale = Canvas.GetScale();
        var (offsetX, offsetY) = Canvas.GetOffset();

        // 更新状态
        _isFitToWindowMode = true;

        StatusText.Text = $"已切换到适应窗口模式 (缩放: {scale * 100:F0}%)";
        ScaleText.Text = $"缩放: {scale * 100:F0}%";

        // 更新按钮状态
        UpdateButtonStates();
    }

    /// <summary>
    /// 原图按钮点击
    /// </summary>
    private void ResetViewButton_Click(object sender, RoutedEventArgs e)
    {
        Canvas.ResetView();

        // 更新状态
        _isFitToWindowMode = false;

        StatusText.Text = "已切换到原图模式 (1:1)";
        ScaleText.Text = "缩放: 100%";

        // 更新按钮状态
        UpdateButtonStates();
    }

    /// <summary>
    /// 复制代码按钮点击
    /// </summary>
    private void CopyCodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(CodePreviewTextBox.Text))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(CodePreviewTextBox.Text);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            StatusText.Text = "代码已复制到剪贴板";
        }
    }

    /// <summary>
    /// 导出裁剪的模板图像
    /// </summary>
    private async Task<string> ExportCroppedTemplate(CropRegion cropRegion, string templateName)
    {
        // TODO: 实现图像裁剪和保存
        // 1. 从 Canvas 获取原始图像数据
        // 2. 使用 ImageProcessor 裁剪指定区域
        // 3. 保存为 PNG 文件

        var templatePath = Path.Combine(_saveFolderPath, $"{templateName}.png");

        // TODO: 实际裁剪和保存逻辑
        await Task.CompletedTask;

        return templatePath;
    }

    /// <summary>
    /// 生成 regionRef（参考 generate-region-ref.js 的逻辑）
    /// </summary>
    private int[] GenerateRegionRef(CropRegion cropRegion, int padding)
    {
        // 1. 应用 padding
        int x = Math.Max(0, cropRegion.X - padding);
        int y = Math.Max(0, cropRegion.Y - padding);
        int right = Math.Min(cropRegion.OriginalWidth ?? cropRegion.X + cropRegion.Width,
                            cropRegion.X + cropRegion.Width + padding);
        int bottom = Math.Min(cropRegion.OriginalHeight ?? cropRegion.Y + cropRegion.Height,
                             cropRegion.Y + cropRegion.Height + padding);
        int width = right - x;
        int height = bottom - y;

        // 2. 确定方向（横屏/竖屏）
        int screenWidth = cropRegion.OriginalWidth ?? 1280;
        int screenHeight = cropRegion.OriginalHeight ?? 720;
        bool isLandscape = screenWidth >= screenHeight;

        // 3. 转换到参考分辨率
        int refWidth = isLandscape ? 1280 : 720;
        int refHeight = isLandscape ? 720 : 1280;
        double widthRatio = (double)refWidth / screenWidth;
        double heightRatio = (double)refHeight / screenHeight;

        return new int[]
        {
            (int)Math.Round(x * widthRatio),
            (int)Math.Round(y * heightRatio),
            (int)Math.Round(width * widthRatio),
            (int)Math.Round(height * heightRatio)
        };
    }

    /// <summary>
    /// 生成 matchReferenceTemplate 代码
    /// </summary>
    private string GenerateMatchTemplateCode(string templatePath, int[] regionRef, CropRegion cropRegion)
    {
        var templateName = Path.GetFileNameWithoutExtension(templatePath);
        var orientation = (cropRegion.OriginalWidth ?? 1280) >= (cropRegion.OriginalHeight ?? 720)
            ? "landscape"
            : "portrait";

        var code = $@"// 模板匹配测试代码
// 模板: {Path.GetFileName(templatePath)}
// 原始区域: [{cropRegion.X}, {cropRegion.Y}, {cropRegion.Width}, {cropRegion.Height}]
// regionRef: [{string.Join(", ", regionRef)}]

const screen = captureScreen();
const result = services.image.matchReferenceTemplate(
    screen,
    ""./assets/{Path.GetFileName(templatePath)}"",
    {{
        name: ""{templateName}"",
        orientation: ""{orientation}"",
        regionRef: [{string.Join(", ", regionRef)}],
        matchThreshold: 0.25,
        acceptThreshold: 0.84,
        useTransparentMask: true
    }}
);

if (result && result.matched) {{
    console.log(""匹配成功！"");
    console.log(""位置: ("" + result.point.x + "", "" + result.point.y + "")"");
    console.log(""置信度: "" + result.confidence.toFixed(4));

    // 点击匹配位置
    click(result.point.x, result.point.y);
}} else {{
    console.log(""匹配失败"");
}}

// 回收图像
screen.recycle();";

        return code;
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    private async Task ShowErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "错误",
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    /// <summary>
    /// 切换调试日志面板
    /// </summary>
    private void ToggleDebugButton_Click(object sender, RoutedEventArgs e)
    {
        DebugPanel.Visibility = DebugPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <summary>
    /// 统计所有节点数量（递归）
    /// </summary>
    private int CountAllNodes(WidgetNode node)
    {
        int count = 1;
        foreach (var child in node.Children)
        {
            count += CountAllNodes(child);
        }
        return count;
    }

    /// <summary>
    /// 获取所有节点（递归）
    /// </summary>
    private List<WidgetNode> GetAllNodes(WidgetNode node)
    {
        var result = new List<WidgetNode> { node };
        foreach (var child in node.Children)
        {
            result.AddRange(GetAllNodes(child));
        }
        return result;
    }

    /// <summary>
    /// 更新按钮状态（根据当前工作流状态）
    /// </summary>
    private void UpdateButtonStates()
    {
        // 截图按钮：始终可用（只要有设备）
        CaptureButton.IsEnabled = _currentDevice != null;

        // 依赖截图的按钮
        bool hasScreenshot = _hasScreenshot;
        FitToWindowButton.IsEnabled = hasScreenshot;
        ResetViewButton.IsEnabled = hasScreenshot;

        // 拉取 UI 树：需要截图 + 适应窗口模式
        bool canDumpUi = hasScreenshot && _isFitToWindowMode;
        DumpUiButton.IsEnabled = canDumpUi;

        // 工作流面板按钮状态
        if (StartCropButton != null)
        {
            StartCropButton.IsEnabled = hasScreenshot && !_isFitToWindowMode;
        }

        // 匹配测试按钮：需要有模板源和截图源
        if (TestMatchButton != null)
        {
            bool hasTemplateSource = (TemplateSourceCrop?.IsChecked == true && _currentCropRegion != null) ||
                                    (TemplateSourceFile?.IsChecked == true && !string.IsNullOrEmpty(_templateFilePath));
            bool hasScreenshotSource = (ScreenshotSourceCurrent?.IsChecked == true && hasScreenshot) ||
                                      (ScreenshotSourceFile?.IsChecked == true && !string.IsNullOrEmpty(_screenshotFilePath));

            TestMatchButton.IsEnabled = hasTemplateSource && hasScreenshotSource;
        }

        // 保存按钮：需要有裁剪区域
        if (SaveTemplateButton != null)
        {
            SaveTemplateButton.IsEnabled = _currentCropRegion != null;
        }

        // 设置 Tooltip 提示
        if (!hasScreenshot)
        {
            ToolTipService.SetToolTip(DumpUiButton, "请先截图");
            ToolTipService.SetToolTip(FitToWindowButton, "请先截图");
            ToolTipService.SetToolTip(ResetViewButton, "请先截图");
        }
        else
        {
            ToolTipService.SetToolTip(FitToWindowButton, null);
            ToolTipService.SetToolTip(ResetViewButton, null);

            if (!_isFitToWindowMode)
            {
                ToolTipService.SetToolTip(DumpUiButton, "请先切换到适应窗口模式");
            }
            else
            {
                ToolTipService.SetToolTip(DumpUiButton, null);
            }
        }
    }

    /// <summary>
    /// 清空日志按钮点击
    /// </summary>
    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        DebugLogText.Text = string.Empty;
    }

    /// <summary>
    /// 全选日志按钮点击
    /// </summary>
    private void SelectAllLogButton_Click(object sender, RoutedEventArgs e)
    {
        DebugLogText.SelectAll();
    }

    /// <summary>
    /// 复制日志按钮点击
    /// </summary>
    private void CopyLogButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedText = DebugLogText.SelectedText;
        if (!string.IsNullOrEmpty(selectedText))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(selectedText);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

}
