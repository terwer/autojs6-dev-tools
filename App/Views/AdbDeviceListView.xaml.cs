using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Models;

namespace App.Views;

/// <summary>
/// ADB 设备列表视图。
/// </summary>
public sealed partial class AdbDeviceListView : UserControl
{
    private readonly IAdbService _adbService;
    private readonly ObservableCollection<AdbDevice> _devices = [];
    private AdbDevice? _selectedDevice;

    public event EventHandler<AdbDevice>? DeviceSelected;

    public AdbDeviceListView()
    {
        InitializeComponent();

        _adbService = new Infrastructure.Adb.AdbServiceImpl();
        DeviceListView.ItemsSource = _devices;

        _ = RefreshDevicesAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDevicesAsync();
    }

    private async void DiscoverButton_Click(object sender, RoutedEventArgs e)
    {
        Services.LogService.Instance.Log("[mDNS] 开始扫描局域网设备（超时 5 秒）...");
        DiscoverButton.IsEnabled = false;
        DiscoverButton.Content = "扫描中...";

        try
        {
            var discoveredDevices = await _adbService.DiscoverDevicesAsync(timeoutSeconds: 5);
            Services.LogService.Instance.Log($"[mDNS] 扫描完成，发现 {discoveredDevices.Count} 个设备");

            await RefreshDevicesAsync();

            foreach (var device in discoveredDevices.Where(device => _devices.All(item => item.Serial != device.Address)))
            {
                _devices.Add(new AdbDevice
                {
                    Serial = device.Address,
                    Model = device.DeviceName,
                    State = "未连接",
                    ConnectionType = "tcpip",
                    Product = "无线设备",
                    TransportId = null
                });
            }

            UpdateDeviceCountText();
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[mDNS] 扫描异常: {ex.Message}");
        }
        finally
        {
            DiscoverButton.IsEnabled = true;
            DiscoverButton.Content = "扫描";
        }
    }

    private async void WirelessButton_Click(object sender, RoutedEventArgs e)
    {
        WirelessConnectionDialog.XamlRoot = XamlRoot;
        await WirelessConnectionDialog.ShowAsync();
    }

    private async void DeviceListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceListView.SelectedItem is not AdbDevice device)
        {
            _selectedDevice = null;
            return;
        }

        _selectedDevice = device;

        if (device.State == "未连接" && string.Equals(device.ConnectionType, "tcpip", StringComparison.OrdinalIgnoreCase))
        {
            Services.LogService.Instance.Log($"[连接] 尝试连接无线设备: {device.Model} ({device.Serial})");

            try
            {
                var connectResult = await _adbService.ConnectDeviceAsync(device.Serial);
                Services.LogService.Instance.Log($"[连接] 连接结果: {connectResult}");

                await RefreshDevicesAsync(device.Serial);

                var connectedDevice = _devices.FirstOrDefault(item => item.Serial == device.Serial);
                if (connectedDevice != null)
                {
                    DeviceListView.SelectedItem = connectedDevice;
                    _selectedDevice = connectedDevice;
                    DeviceSelected?.Invoke(this, connectedDevice);
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Instance.Log($"[连接] 连接失败: {ex.Message}");
            }

            return;
        }

        DeviceSelected?.Invoke(this, device);
    }

    private async void PairButton_Click(object sender, RoutedEventArgs e)
    {
        var ipAddress = PairIpTextBox.Text.Trim();
        var port = PairPortTextBox.Text.Trim();
        var pairingCode = PairingCodeTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(ipAddress) ||
            string.IsNullOrWhiteSpace(port) ||
            string.IsNullOrWhiteSpace(pairingCode))
        {
            Services.LogService.Instance.Log("[配对] IP、端口和配对码不能为空");
            return;
        }

        try
        {
            var result = await _adbService.PairDeviceAsync($"{ipAddress}:{port}", pairingCode);
            Services.LogService.Instance.Log($"[配对] 配对成功: {result}");
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[配对] 配对失败: {ex.Message}");
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var ipAddress = ConnectIpTextBox.Text.Trim();
        var port = ConnectPortTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(port))
        {
            Services.LogService.Instance.Log("[连接] IP 地址和端口不能为空");
            return;
        }

        var address = $"{ipAddress}:{port}";
        try
        {
            var connectResult = await _adbService.ConnectDeviceAsync(address);
            Services.LogService.Instance.Log($"[连接] 连接结果: {connectResult}");

            var isSuccess = connectResult.Contains("connected", StringComparison.OrdinalIgnoreCase) &&
                            !connectResult.Contains("failed", StringComparison.OrdinalIgnoreCase);

            if (!isSuccess)
            {
                Services.LogService.Instance.Log($"[连接] 连接失败: {connectResult}");
                return;
            }

            await RefreshDevicesAsync(address);
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[连接] 异常: {ex.Message}");
        }
    }

    private async Task RefreshDevicesAsync(string? preferredSelectedSerial = null)
    {
        try
        {
            var devices = await _adbService.ScanDevicesAsync();
            var selectedSerial = preferredSelectedSerial ?? _selectedDevice?.Serial;

            _devices.Clear();

            var uniqueDevices = new Dictionary<string, AdbDevice>();
            foreach (var device in devices)
            {
                if (!uniqueDevices.TryGetValue(device.Serial, out var existing))
                {
                    uniqueDevices[device.Serial] = device;
                    continue;
                }

                if (!existing.State.Contains("Online", StringComparison.OrdinalIgnoreCase) &&
                    device.State.Contains("Online", StringComparison.OrdinalIgnoreCase))
                {
                    uniqueDevices[device.Serial] = device;
                }
            }

            foreach (var device in uniqueDevices.Values)
            {
                _devices.Add(device);
            }

            UpdateDeviceCountText();

            if (string.IsNullOrWhiteSpace(selectedSerial))
            {
                return;
            }

            var selectedDevice = _devices.FirstOrDefault(device => device.Serial == selectedSerial);
            if (selectedDevice == null)
            {
                return;
            }

            DeviceListView.SelectedItem = selectedDevice;
            _selectedDevice = selectedDevice;
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[刷新] 刷新设备失败：{ex.Message}");
        }
    }

    private void UpdateDeviceCountText()
    {
        DeviceCountText.Text = _devices.Count == 0
            ? "当前未发现在线设备，可先点击“扫描”或使用无线连接。"
            : $"已发现 {_devices.Count} 台设备，点击即可设为当前工作目标。";
    }

    public AdbDevice? GetSelectedDevice() => _selectedDevice;
}
