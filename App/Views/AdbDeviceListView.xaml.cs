using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
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
    public enum RefreshFeedbackTone
    {
        Info,
        Success,
        Warning,
        Error
    }

    public sealed class DeviceRefreshStatusChangedEventArgs : EventArgs
    {
        public DeviceRefreshStatusChangedEventArgs(string message, RefreshFeedbackTone tone)
        {
            Message = message;
            Tone = tone;
        }

        public string Message { get; }

        public RefreshFeedbackTone Tone { get; }
    }

    private readonly IAdbService _adbService;
    private readonly List<AdbDevice> _devices = [];
    private AdbDevice? _selectedDevice;

    public event EventHandler<AdbDevice>? DeviceSelected;
    public event EventHandler<DeviceRefreshStatusChangedEventArgs>? RefreshStatusChanged;

    public AdbDeviceListView()
    {
        InitializeComponent();

        _adbService = new Infrastructure.Adb.AdbServiceImpl();
        _ = RefreshDevicesAsync(reportStatus: false);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDevicesAsync(reportStatus: true);
    }

    private async void WirelessButton_Click(object sender, RoutedEventArgs e)
    {
        WirelessConnectionDialog.XamlRoot = XamlRoot;
        await WirelessConnectionDialog.ShowAsync();
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

            await RefreshDevicesAsync(address, reportStatus: true);
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[连接] 异常: {ex.Message}");
        }
    }

    private async Task RefreshDevicesAsync(string? preferredSelectedSerial = null, bool reportStatus = false)
    {
        if (reportStatus)
        {
            RaiseRefreshStatus("正在刷新设备...", RefreshFeedbackTone.Info);
        }

        RefreshButton.IsEnabled = false;

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

            _selectedDevice = string.IsNullOrWhiteSpace(selectedSerial)
                ? null
                : _devices.FirstOrDefault(device => device.Serial == selectedSerial);

            RenderDevices();
            Services.LogService.Instance.Log($"[刷新] 刷新完成，共 {_devices.Count} 台在线设备");

            if (reportStatus)
            {
                RaiseRefreshStatus(
                    _devices.Count == 0
                        ? "未发现在线设备"
                        : $"设备刷新完成，共 {_devices.Count} 台在线设备",
                    _devices.Count == 0 ? RefreshFeedbackTone.Warning : RefreshFeedbackTone.Success);
            }
        }
        catch (Exception ex)
        {
            Services.LogService.Instance.Log($"[刷新] 刷新设备失败：{ex.Message}");
            if (reportStatus)
            {
                RaiseRefreshStatus($"刷新设备失败：{ex.Message}", RefreshFeedbackTone.Error);
            }
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void RaiseRefreshStatus(string message, RefreshFeedbackTone tone)
    {
        RefreshStatusChanged?.Invoke(this, new DeviceRefreshStatusChangedEventArgs(message, tone));
    }

    private void RenderDevices()
    {
        DeviceItemsPanel.Children.Clear();

        foreach (var device in _devices)
        {
            DeviceItemsPanel.Children.Add(CreateDeviceCard(device));
        }

        UpdateDeviceCountText();
    }

    private FrameworkElement CreateDeviceCard(AdbDevice device)
    {
        var isSelected = _selectedDevice?.Serial == device.Serial;

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var accent = new Border
        {
            Background = new SolidColorBrush(isSelected
                ? Windows.UI.Color.FromArgb(255, 0, 120, 215)
                : Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 8, 0, 8)
        };
        Grid.SetColumn(accent, 0);

        var button = new Button
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0),
            Margin = new Thickness(8, 0, 0, 0),
            Tag = device
        };
        button.Click += DeviceButton_Click;
        Grid.SetColumn(button, 1);

        var card = new Border
        {
            Padding = new Thickness(12, 9, 12, 9),
            Background = new SolidColorBrush(isSelected
                ? Windows.UI.Color.FromArgb(255, 236, 244, 255)
                : Windows.UI.Color.FromArgb(255, 248, 248, 248)),
            BorderBrush = new SolidColorBrush(isSelected
                ? Windows.UI.Color.FromArgb(255, 62, 138, 255)
                : Windows.UI.Color.FromArgb(255, 228, 228, 228)),
            BorderThickness = new Thickness(isSelected ? 1.5 : 1),
            CornerRadius = new CornerRadius(10)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel { Spacing = 4 };
        left.Children.Add(new TextBlock
        {
            Text = device.Serial,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        left.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(device.Model) ? "未提供型号信息" : device.Model,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
        });
        Grid.SetColumn(left, 0);

        var right = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2
        };
        right.Children.Add(new TextBlock
        {
            Text = device.State,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Right
        });
        right.Children.Add(new TextBlock
        {
            Text = device.ConnectionType ?? "未知",
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
        });
        Grid.SetColumn(right, 1);

        grid.Children.Add(left);
        grid.Children.Add(right);
        card.Child = grid;
        button.Content = card;

        root.Children.Add(accent);
        root.Children.Add(button);
        return root;
    }

    private async void DeviceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not AdbDevice device)
        {
            return;
        }

        _selectedDevice = device;
        RenderDevices();

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
                    _selectedDevice = connectedDevice;
                    RenderDevices();
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

    private void UpdateDeviceCountText()
    {
        DeviceCountText.Text = _devices.Count == 0
            ? "当前未发现在线设备。"
            : $"已发现 {_devices.Count} 台设备，点击即可设为当前工作目标。";
    }

    public AdbDevice? GetSelectedDevice() => _selectedDevice;
}
