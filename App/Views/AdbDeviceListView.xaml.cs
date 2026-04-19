using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Models;

namespace App.Views;

/// <summary>
/// ADB 设备列表视图
/// </summary>
public sealed partial class AdbDeviceListView : UserControl
{
    private readonly IAdbService _adbService;
    private readonly ObservableCollection<AdbDevice> _devices = new();
    private AdbDevice? _selectedDevice;

    public event EventHandler<AdbDevice>? DeviceSelected;

    public AdbDeviceListView()
    {
        this.InitializeComponent();

        // 注入 AdbService（临时使用，后续通过 DI 容器）
        _adbService = new Infrastructure.Adb.AdbServiceImpl();

        DeviceListView.ItemsSource = _devices;

        // 自动刷新设备列表
        _ = RefreshDevicesAsync();
    }

    /// <summary>
    /// 刷新设备列表
    /// </summary>
    private async Task RefreshDevicesAsync()
    {
        try
        {
            var devices = await _adbService.ScanDevicesAsync();

            _devices.Clear();
            foreach (var device in devices)
            {
                _devices.Add(device);
            }
        }
        catch (Exception ex)
        {
            // 显示错误提示
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = $"刷新设备失败：{ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    /// <summary>
    /// 刷新按钮点击
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDevicesAsync();
    }

    /// <summary>
    /// 连接按钮点击
    /// </summary>
    private void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDevice != null)
        {
            DeviceSelected?.Invoke(this, _selectedDevice);
        }
    }

    /// <summary>
    /// 设备选择变化
    /// </summary>
    private void DeviceListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceListView.SelectedItem is AdbDevice device)
        {
            _selectedDevice = device;
        }
    }

    /// <summary>
    /// TCP/IP 连接按钮点击
    /// </summary>
    private async void TcpIpConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var address = TcpIpTextBox.Text.Trim();

        Services.LogService.Instance.Log($"[TCP/IP] 尝试连接: {address}");

        if (string.IsNullOrEmpty(address))
        {
            Services.LogService.Instance.Log($"[TCP/IP] 地址为空，取消连接");
            return;
        }

        try
        {
            Services.LogService.Instance.Log($"[TCP/IP] 调用 ConnectDeviceAsync...");

            // 使用底层 API 连接设备
            var result = await _adbService.ConnectDeviceAsync(address);

            Services.LogService.Instance.Log($"[TCP/IP] 连接结果: {result}");

            // 判断连接是否成功（ADB 返回结果包含 "connected" 或不包含 "failed"）
            bool isSuccess = result.Contains("connected", StringComparison.OrdinalIgnoreCase) &&
                           !result.Contains("failed", StringComparison.OrdinalIgnoreCase);

            if (isSuccess)
            {
                // 刷新设备列表
                await RefreshDevicesAsync();

                // 显示成功提示
                var successDialog = new ContentDialog
                {
                    Title = "成功",
                    Content = $"连接成功：{result}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            else
            {
                // 连接失败
                var dialog = new ContentDialog
                {
                    Title = "连接失败",
                    Content = $"{result}\n\n请检查：\n1. 设备是否开启无线调试\n2. IP 地址和端口是否正确\n3. 设备和电脑是否在同一网络",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[TCP/IP] 连接异常: {ex.Message}");

            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = $"连接异常：{ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    /// <summary>
    /// 获取当前选中的设备
    /// </summary>
    public AdbDevice? GetSelectedDevice() => _selectedDevice;
}
