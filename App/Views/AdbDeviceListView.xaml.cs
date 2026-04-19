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
        if (string.IsNullOrEmpty(address))
        {
            return;
        }

        try
        {
            // 使用底层 API 连接设备
            await _adbService.ConnectDeviceAsync(address);

            // 刷新设备列表
            await RefreshDevicesAsync();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = $"连接失败：{ex.Message}",
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
