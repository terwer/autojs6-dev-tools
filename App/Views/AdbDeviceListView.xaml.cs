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

        // 注入 AdbService（临时使用，后续通过 DI 容器）
        _adbService = new Infrastructure.Adb.AdbServiceImpl();

        DeviceListView.ItemsSource = _devices;

        // 自动刷新设备列表
        _ = RefreshDevicesAsync();
    }

    /// <summary>
    /// 刷新按钮点击
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDevicesAsync();
    }

    /// <summary>
    /// 扫描无线设备按钮点击
    /// </summary>
    private async void DiscoverButton_Click(object sender, RoutedEventArgs e)
    {
        Services.LogService.Instance.Log($"[mDNS] 开始扫描局域网设备（超时 5 秒）...");

        // 禁用按钮，防止重复点击
        DiscoverButton.IsEnabled = false;
        DiscoverButton.Content = "扫描中...";

        try
        {
            // 扫描设备（5秒超时）
            Services.LogService.Instance.Log($"[mDNS] 调用 DiscoverDevicesAsync...");
            var discoveredDevices = await _adbService.DiscoverDevicesAsync(timeoutSeconds: 5);

            Services.LogService.Instance.Log($"[mDNS] 扫描完成，发现 {discoveredDevices.Count} 个设备");

            if (discoveredDevices.Count == 0)
            {
                Services.LogService.Instance.Log($"[mDNS] 未发现设备");
                Services.LogService.Instance.Log($"[mDNS] 请检查：");
                Services.LogService.Instance.Log($"[mDNS]   1. 设备是否开启无线调试（设置 → 开发者选项 → 无线调试）");
                Services.LogService.Instance.Log($"[mDNS]   2. 设备和电脑是否在同一网络");
                Services.LogService.Instance.Log($"[mDNS]   3. 防火墙是否阻止 mDNS（端口 5353）");
            }
            else
            {
                // 先刷新现有设备列表
                await RefreshDevicesAsync();

                // 将扫描到的设备添加到列表（标记为未连接）
                foreach (var device in discoveredDevices)
                {
                    Services.LogService.Instance.Log($"[mDNS] 发现设备: {device.DeviceName} ({device.Address})");

                    // 检查是否已经在列表中
                    bool alreadyExists = _devices.Any(d => d.Serial == device.Address);

                    if (!alreadyExists)
                    {
                        // 添加到列表，标记为"未连接"
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
                    else
                    {
                        Services.LogService.Instance.Log($"[mDNS] 设备已存在: {device.DeviceName}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[mDNS] 扫描异常: {ex.GetType().Name}");
            Services.LogService.Instance.Log($"[mDNS] 错误信息: {ex.Message}");
        }
        finally
        {
            // 恢复按钮状态
            DiscoverButton.IsEnabled = true;
            DiscoverButton.Content = "扫描";
            Services.LogService.Instance.Log($"[mDNS] 扫描流程结束");
        }
    }

    /// <summary>
    /// 设备选择变化
    /// </summary>
    private async void DeviceListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceListView.SelectedItem is AdbDevice device)
        {
            _selectedDevice = device;

            // 如果是未连接的无线设备，尝试连接
            if (device.State == "未连接" && device.ConnectionType == "tcpip")
            {
                Services.LogService.Instance.Log($"[连接] 尝试连接无线设备: {device.Model} ({device.Serial})");

                try
                {
                    var connectResult = await _adbService.ConnectDeviceAsync(device.Serial);
                    Services.LogService.Instance.Log($"[连接] 连接结果: {connectResult}");

                    // 刷新设备列表
                    await RefreshDevicesAsync();

                    // 重新选择设备
                    var connectedDevice = _devices.FirstOrDefault(d => d.Serial == device.Serial);
                    if (connectedDevice != null)
                    {
                        DeviceListView.SelectedItem = connectedDevice;
                        DeviceSelected?.Invoke(this, connectedDevice);
                    }
                }
                catch (Exception ex)
                {
                    Services.LogService.Instance.Log($"[连接] 连接失败: {ex.Message}");
                }
            }
            else
            {
                // 已连接的设备，直接触发选择事件
                DeviceSelected?.Invoke(this, device);
            }
        }
    }

    /// <summary>
    /// 配对按钮点击
    /// </summary>
    private async void PairButton_Click(object sender, RoutedEventArgs e)
    {
        var ipAddress = PairIpTextBox.Text.Trim();
        var port = PairPortTextBox.Text.Trim();
        var pairingCode = PairingCodeTextBox.Text.Trim();

        if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(pairingCode))
        {
            Services.LogService.Instance.Log($"[配对] IP、端口和配对码不能为空");
            return;
        }

        var address = $"{ipAddress}:{port}";

        Services.LogService.Instance.Log($"[配对] 开始配对: {address}");

        try
        {
            var pairResult = await _adbService.PairDeviceAsync(address, pairingCode);
            Services.LogService.Instance.Log($"[配对] 配对成功: {pairResult}");
            Services.LogService.Instance.Log($"[配对] 现在可以使用步骤 2 连接设备了");
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[配对] 配对失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 连接按钮点击
    /// </summary>
    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var ipAddress = ConnectIpTextBox.Text.Trim();
        var port = ConnectPortTextBox.Text.Trim();

        if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(port))
        {
            Services.LogService.Instance.Log($"[连接] IP 地址和端口不能为空");
            return;
        }

        var address = $"{ipAddress}:{port}";

        Services.LogService.Instance.Log($"[连接] 尝试连接: {address}");

        try
        {
            var connectResult = await _adbService.ConnectDeviceAsync(address);
            Services.LogService.Instance.Log($"[连接] 连接结果: {connectResult}");

            bool isSuccess = connectResult.Contains("connected", StringComparison.OrdinalIgnoreCase) &&
                           !connectResult.Contains("failed", StringComparison.OrdinalIgnoreCase);

            if (isSuccess)
            {
                Services.LogService.Instance.Log($"[连接] 连接成功！");
                // 刷新设备列表
                await RefreshDevicesAsync();
            }
            else
            {
                Services.LogService.Instance.Log($"[连接] 连接失败: {connectResult}");
            }
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[连接] 异常: {ex.Message}");
        }
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

            // 去重：使用 Dictionary 按 Serial 去重，保留 Online 状态的设备
            var uniqueDevices = new Dictionary<string, AdbDevice>();

            foreach (var device in devices)
            {
                if (!uniqueDevices.ContainsKey(device.Serial))
                {
                    uniqueDevices[device.Serial] = device;
                }
                else
                {
                    // 如果已存在，优先保留 Online 状态的
                    if (device.State.Contains("Online", StringComparison.OrdinalIgnoreCase))
                    {
                        uniqueDevices[device.Serial] = device;
                    }
                }
            }

            foreach (var device in uniqueDevices.Values)
            {
                _devices.Add(device);
            }
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[刷新] 刷新设备失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取当前选中的设备
    /// </summary>
    public AdbDevice? GetSelectedDevice() => _selectedDevice;
}
