using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
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

            // 拉取截图
            var screenshot = await _adbService.CaptureScreenshotAsync(_currentDevice);

            // 获取分辨率和旋转角度
            var (width, height) = await _adbService.GetScreenResolutionAsync(_currentDevice);
            var rotation = await _adbService.GetScreenRotationAsync(_currentDevice);

            // 加载到画布
            Canvas.LoadImage(screenshot, width, height, rotation);

            StatusText.Text = "截图完成";
            ResolutionText.Text = $"分辨率: {width}x{height}, 旋转: {rotation}°";
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

            // 解析 UI 树
            var parser = new Core.Services.UiDumpParser();
            var root = await parser.ParseAsync(xmlContent);

            if (root != null)
            {
                // 过滤节点
                var nodes = parser.FilterNodes(root);

                // 显示到画布
                Canvas.SetWidgetNodes(nodes);

                StatusText.Text = $"UI 树拉取完成，共 {nodes.Count} 个控件";
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"拉取 UI 树失败：{ex.Message}");
            StatusText.Text = "拉取 UI 树失败";
        }
    }

    /// <summary>
    /// 生成代码按钮点击
    /// TODO: 实现完整的代码生成逻辑
    /// </summary>
    private void GenerateCodeButton_Click(object sender, RoutedEventArgs e)
    {
        // 生成示例代码
        var options = new AutoJS6CodeOptions
        {
            Mode = CodeGenerationMode.Image,
            TemplatePath = "./template.png",
            Threshold = 0.8,
            VariablePrefix = "target",
            GenerateRetryLogic = true,
            RetryCount = 3
        };

        var code = _codeGenerator.GenerateImageModeCode(options);
        CodePreview.SetCode(code);

        StatusText.Text = "代码生成完成";
    }

    /// <summary>
    /// 匹配测试按钮点击
    /// TODO: 实现匹配测试功能
    /// </summary>
    private async void MatchTestButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowErrorAsync("匹配测试功能将在后续版本实现");
    }

    /// <summary>
    /// 适应窗口按钮点击
    /// </summary>
    private void FitToWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Canvas.FitToWindow();
        var scale = Canvas.GetScale();
        var (offsetX, offsetY) = Canvas.GetOffset();
        StatusText.Text = $"已切换到适应窗口模式 (缩放: {scale * 100:F0}%)";
        ScaleText.Text = $"缩放: {scale * 100:F0}%";
    }

    /// <summary>
    /// 原图按钮点击
    /// </summary>
    private void ResetViewButton_Click(object sender, RoutedEventArgs e)
    {
        Canvas.ResetView();
        StatusText.Text = "已切换到原图模式 (1:1)";
        ScaleText.Text = "缩放: 100%";
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

}
