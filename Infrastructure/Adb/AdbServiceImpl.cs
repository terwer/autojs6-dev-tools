using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using Core.Abstractions;
using Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using Zeroconf;

namespace Infrastructure.Adb;

/// <summary>
/// ADB 服务实现
/// 参考 MVP1.AdbScreencap 和 MVP2.UiDumpParser 的最佳实践
/// </summary>
public class AdbServiceImpl : IAdbService
{
    private readonly AdbClient _adbClient;
    private readonly AdbServer _adbServer;
    private readonly string? _adbPath;

    public AdbServiceImpl(string? adbPath = null)
    {
        _adbPath = adbPath ?? FindAdbPath();
        _adbServer = new AdbServer();
        _adbClient = new AdbClient();
    }

    /// <summary>
    /// 初始化 ADB 服务器
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        if (_adbPath == null)
        {
            return false;
        }

        try
        {
            var result = _adbServer.StartServer(_adbPath, restartServerIfNewer: false);
            return result == StartServerResult.Started || result == StartServerResult.AlreadyRunning;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Core.Models.AdbDevice>> ScanDevicesAsync()
    {
        var devices = _adbClient.GetDevices();
        var result = new List<Core.Models.AdbDevice>();

        foreach (var device in devices)
        {
            result.Add(new Core.Models.AdbDevice
            {
                Serial = device.Serial,
                Model = device.Model,
                State = device.State.ToString(),
                ConnectionType = device.Serial.Contains(":") ? "tcpip" : "usb",
                Product = device.Product,
                TransportId = device.TransportId
            });
        }

        return await Task.FromResult(result);
    }

    public async Task<(byte[] Data, int Width, int Height)> CaptureScreenshotAsync(Core.Models.AdbDevice device, CancellationToken cancellationToken = default)
    {
        var adbDevice = FindDeviceBySerial(device.Serial);
        if (adbDevice == null)
        {
            throw new InvalidOperationException($"Device {device.Serial} not found");
        }

        // 参考 MVP1: 使用 GetFrameBufferAsync 流式读取截图
        var framebuffer = await _adbClient.GetFrameBufferAsync(adbDevice, cancellationToken);

        // 参考 MVP1: 使用 uint 类型安全
        uint width = framebuffer.Header.Width;
        uint height = framebuffer.Header.Height;
        uint bpp = framebuffer.Header.Bpp;
        byte[] data = framebuffer.Data ?? throw new InvalidOperationException("Framebuffer.Data 为 null");

        // 参考 MVP1: 处理 Framebuffer stride（行填充）
        uint bytesPerPixel = bpp / 8;
        uint expectedSize = width * height * bytesPerPixel;

        if (data.Length != expectedSize)
        {
            // 检测行填充
            uint stride = (uint)data.Length / height;
            if (stride * height == data.Length && stride >= width * bytesPerPixel)
            {
                // 去除填充，提取有效像素数据
                var validData = new byte[expectedSize];
                uint validRowBytes = width * bytesPerPixel;

                for (uint y = 0; y < height; y++)
                {
                    Array.Copy(data, (int)(y * stride), validData, (int)(y * validRowBytes), (int)validRowBytes);
                }

                data = validData;
            }
        }

        // 使用 SixLabors.ImageSharp 转换为 PNG
        using var image = Image.LoadPixelData<Rgba32>(data, (int)width, (int)height);

        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new PngEncoder(), cancellationToken);
        return (ms.ToArray(), (int)width, (int)height);
    }

    public async Task<string> DumpUiHierarchyAsync(Core.Models.AdbDevice device, CancellationToken cancellationToken = default)
    {
        var adbDevice = FindDeviceBySerial(device.Serial);
        if (adbDevice == null)
        {
            throw new InvalidOperationException($"Device {device.Serial} not found");
        }

        // 参考 MVP2: 使用 DeviceClient.DumpScreenAsync 异步获取 UI 层次结构
        var deviceClient = new DeviceClient(_adbClient, adbDevice);
        var xmlDoc = await deviceClient.DumpScreenAsync(cancellationToken);

        if (xmlDoc == null)
        {
            throw new InvalidOperationException("Failed to dump UI hierarchy");
        }

        return xmlDoc.OuterXml;
    }

    public async Task<bool> IsDeviceOnlineAsync(Core.Models.AdbDevice device)
    {
        var adbDevice = FindDeviceBySerial(device.Serial);
        return await Task.FromResult(adbDevice?.State == DeviceState.Online);
    }

    /// <summary>
    /// 连接到网络设备（TCP/IP）
    /// 使用底层 API：AdbClient.Connect
    /// </summary>
    public async Task<string> ConnectDeviceAsync(string address, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Task.Run(() => _adbClient.Connect(address), cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"连接设备失败：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 配对网络设备（使用配对码）
    /// 使用底层 API：AdbClient.Pair
    /// </summary>
    public async Task<string> PairDeviceAsync(string address, string pairingCode, CancellationToken cancellationToken = default)
    {
        try
        {
            // 直接调用 Pair 方法
            var result = await Task.Run(() => _adbClient.Pair(address, pairingCode), cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"配对设备失败：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 通过 mDNS 自动发现局域网内的 ADB 设备
    /// </summary>
    public async Task<List<(string DeviceName, string Address)>> DiscoverDevicesAsync(int timeoutSeconds = 5, CancellationToken cancellationToken = default)
    {
        var devices = new List<(string DeviceName, string Address)>();

        try
        {
            // Android 无线调试可能使用多种服务类型，尝试所有可能的类型
            var serviceTypes = new[]
            {
                "_adb-tls-connect._tcp.local.",  // Android 11+ 无线调试
                "_adb._tcp.local.",              // 旧版 ADB
                "_adb-tls-pairing._tcp.local."   // 配对服务
            };

            foreach (var serviceType in serviceTypes)
            {
                System.Diagnostics.Debug.WriteLine($"[mDNS] 尝试解析服务: {serviceType}");

                try
                {
                    var responses = await ZeroconfResolver.ResolveAsync(serviceType, TimeSpan.FromSeconds(timeoutSeconds), cancellationToken: cancellationToken);

                    System.Diagnostics.Debug.WriteLine($"[mDNS] {serviceType} 收到 {responses.Count()} 个响应");

                    foreach (var response in responses)
                    {
                        System.Diagnostics.Debug.WriteLine($"[mDNS] 响应: {response.Id}, DisplayName: {response.DisplayName}, IP: {response.IPAddress}");

                        foreach (var service in response.Services)
                        {
                            System.Diagnostics.Debug.WriteLine($"[mDNS] 服务: {service.Key}, Port: {service.Value.Port}");

                            if (service.Value.Port > 0)
                            {
                                // 获取设备名称
                                string deviceName = response.DisplayName;
                                if (string.IsNullOrEmpty(deviceName))
                                {
                                    deviceName = response.Id.Replace(".local.", "");
                                }

                                // 获取 IP 地址
                                string ipAddress = response.IPAddress;
                                int port = service.Value.Port;

                                var deviceKey = $"{ipAddress}:{port}";

                                // 避免重复添加
                                if (!devices.Any(d => d.Address == deviceKey))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[mDNS] 添加设备: {deviceName} ({deviceKey})");
                                    devices.Add((deviceName, deviceKey));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[mDNS] {serviceType} 解析失败: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[mDNS] 最终发现 {devices.Count} 个设备");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[mDNS] 发现异常: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[mDNS] 堆栈: {ex.StackTrace}");
        }

        return devices;
    }

    private DeviceData? FindDeviceBySerial(string serial)
    {
        return _adbClient.GetDevices().FirstOrDefault(d => d.Serial == serial);
    }

    /// <summary>
    /// 查找 ADB 可执行文件路径
    /// 参考 MVP1 和 MVP2 的实现
    /// </summary>
    private static string? FindAdbPath()
    {
        // 1. 尝试从 PATH 环境变量查找
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv != null)
        {
            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var adbPath = Path.Combine(path, "adb.exe");
                if (File.Exists(adbPath))
                {
                    return adbPath;
                }
            }
        }

        // 2. 尝试常见的 Android SDK 安装位置
        var commonPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", "adb.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "Sdk", "platform-tools", "adb.exe"),
            @"C:\Android\Sdk\platform-tools\adb.exe",
            @"D:\Android\Sdk\platform-tools\adb.exe"
        };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        // 3. 尝试从 ANDROID_HOME 环境变量
        var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
        if (androidHome != null)
        {
            var adbPath = Path.Combine(androidHome, "platform-tools", "adb.exe");
            if (File.Exists(adbPath))
            {
                return adbPath;
            }
        }

        return null;
    }
}
