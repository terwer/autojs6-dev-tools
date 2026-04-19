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

        _adbService = new Infrastructure.Adb.AdbServiceImpl();
        DeviceListView.ItemsSource = _devices;

        UpdateCurrentDeviceSummary(null);

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
            Services.LogService.Instance.Log("[mDNS] 调用 DiscoverDevicesAsync...");
            var discoveredDevices = await _adbService.DiscoverDevicesAsync(timeoutSeconds: 5);

            Services.LogService.Instance.Log($"[mDNS] 扫描完成，发现 {discoveredDevices.Count} 个设备");

            if (discoveredDevices.Count == 0)
            {
                Services.LogService.Instance.Log("[mDNS] 未发现设备");
                Services.LogService.Instance.Log("[mDNS] 请检查：");
                Services.LogService.Instance.Log("[mDNS]   1. 设备是否开启无线调试（设置 → 开发者选项 → 无线调试）");
                Services.LogService.Instance.Log("[mDNS]   2. 设备和电脑是否在同一网络");
                Services.LogService.Instance.Log("[mDNS]   3. 防火墙是否阻止 mDNS（端口 5353）");
                return;
            }

            await RefreshDevicesAsync();

            foreach (var device in discoveredDevices)
            {
                Services.LogService.Instance.Log($"[mDNS] 发现设备: {device.DeviceName} ({device.Address})");

                var alreadyExists = _devices.Any(d => d.Serial == device.Address);
                if (alreadyExists)
                {
                    Services.LogService.Instance.Log($"[mDNS] 设备已存在: {device.DeviceName}");
                    continue;
                }

                _devices.Add(new AdbDevice
                {
                    Serial = device.Address,
                    Model = device.DeviceName,
                    State = "未连接",
                    ConnectionType = "tcpip",
                    Product = "无线设备",
                    TransportId = null
                });

                Services.LogService.Instance.Log($"[mDNS] 已添加到列表: {device.DeviceName}");
            }
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[mDNS] 扫描异常: {ex.GetType().Name}");
            Services.LogService.Instance.Log($"[mDNS] 错误信息: {ex.Message}");
        }
        finally
        {
            DiscoverButton.IsEnabled = true;
            DiscoverButton.Content = "扫描无线设备";
            Services.LogService.Instance.Log("[mDNS] 扫描流程结束");
        }
    }

    private async void DeviceListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceListView.SelectedItem is not AdbDevice device)
        {
            _selectedDevice = null;
            UpdateCurrentDeviceSummary(null);
            return;
        }

        _selectedDevice = device;
        UpdateCurrentDeviceSummary(device);

        if (device.State == "未连接" && string.Equals(device.ConnectionType, "tcpip", StringComparison.OrdinalIgnoreCase))
        {
            Services.LogService.Instance.Log($"[连接] 尝试连接无线设备: {device.Model} ({device.Serial})");

            try
            {
                var connectResult = await _adbService.ConnectDeviceAsync(device.Serial);
                Services.LogService.Instance.Log($"[连接] 连接结果: {connectResult}");

                await RefreshDevicesAsync(device.Serial);

                var connectedDevice = _devices.FirstOrDefault(d => d.Serial == device.Serial);
                if (connectedDevice != null)
                {
                    DeviceListView.SelectedItem = connectedDevice;
                    _selectedDevice = connectedDevice;
                    UpdateCurrentDeviceSummary(connectedDevice);
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

        if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(pairingCode))
        {
            Services.LogService.Instance.Log("[配对] IP、端口和配对码不能为空");
            return;
        }

        var address = $"{ipAddress}:{port}";
        Services.LogService.Instance.Log($"[配对] 开始配对: {address}");

        try
        {
            var pairResult = await _adbService.PairDeviceAsync(address, pairingCode);
            Services.LogService.Instance.Log($"[配对] 配对成功: {pairResult}");
            Services.LogService.Instance.Log("[配对] 现在可以使用步骤 2 连接设备了");
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

        if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(port))
        {
            Services.LogService.Instance.Log("[连接] IP 地址和端口不能为空");
            return;
        }

        var address = $"{ipAddress}:{port}";
        Services.LogService.Instance.Log($"[连接] 尝试连接: {address}");

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

            Services.LogService.Instance.Log("[连接] 连接成功！");
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

            if (string.IsNullOrEmpty(selectedSerial))
            {
                return;
            }

            var selectedDevice = _devices.FirstOrDefault(d => d.Serial == selectedSerial);
            if (selectedDevice == null)
            {
                return;
            }

            DeviceListView.SelectedItem = selectedDevice;
            _selectedDevice = selectedDevice;
            UpdateCurrentDeviceSummary(selectedDevice);
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[刷新] 刷新设备失败：{ex.Message}");
        }
    }

    private void UpdateCurrentDeviceSummary(AdbDevice? device)
    {
        if (device == null)
        {
            CurrentDeviceSerialText.Text = "尚未选择设备";
            CurrentDeviceModelText.Text = "从设备列表中选择一个 ADB 设备后，这里会显示详细信息。";
            CurrentDeviceStateText.Text = "-";
            CurrentDeviceConnectionText.Text = "-";
            return;
        }

        CurrentDeviceSerialText.Text = device.Serial;
        CurrentDeviceModelText.Text = string.IsNullOrWhiteSpace(device.Model)
            ? "未提供型号信息"
            : device.Model;
        CurrentDeviceStateText.Text = device.State;
        CurrentDeviceConnectionText.Text = string.IsNullOrWhiteSpace(device.ConnectionType)
            ? "未知"
            : device.ConnectionType;
    }

    public AdbDevice? GetSelectedDevice() => _selectedDevice;
}
