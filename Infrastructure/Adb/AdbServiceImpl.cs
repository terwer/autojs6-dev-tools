using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using Core.Abstractions;
using Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

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

    public async Task<string> ExecuteCommandAsync(Core.Models.AdbDevice device, string command, CancellationToken cancellationToken = default)
    {
        var adbDevice = FindDeviceBySerial(device.Serial);
        if (adbDevice == null)
        {
            throw new InvalidOperationException($"Device {device.Serial} not found");
        }

        var receiver = new ConsoleOutputReceiver();
        await _adbClient.ExecuteRemoteCommandAsync(command, adbDevice, receiver, cancellationToken);
        return receiver.ToString();
    }

    public async Task<byte[]> CaptureScreenshotAsync(Core.Models.AdbDevice device, CancellationToken cancellationToken = default)
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
        return ms.ToArray();
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

    public async Task<(int Width, int Height)> GetScreenResolutionAsync(Core.Models.AdbDevice device, CancellationToken cancellationToken = default)
    {
        var adbDevice = FindDeviceBySerial(device.Serial);
        if (adbDevice == null)
        {
            throw new InvalidOperationException($"Device {device.Serial} not found");
        }

        var receiver = new ConsoleOutputReceiver();
        await _adbClient.ExecuteRemoteCommandAsync("wm size", adbDevice, receiver, cancellationToken);
        var output = receiver.ToString();

        // 解析输出: "Physical size: 1080x1920"
        var match = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)x(\d+)");
        if (match.Success)
        {
            return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }

        throw new InvalidOperationException($"Failed to parse screen resolution: {output}");
    }

    public async Task<bool> IsDeviceOnlineAsync(Core.Models.AdbDevice device)
    {
        var adbDevice = FindDeviceBySerial(device.Serial);
        return await Task.FromResult(adbDevice?.State == DeviceState.Online);
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

    /// <summary>
    /// 执行全局 ADB 命令（不需要设备）
    /// 例如：adb connect、adb disconnect
    /// </summary>
    public async Task<string> ExecuteGlobalCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (_adbPath == null)
            {
                throw new InvalidOperationException("ADB 路径未找到");
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _adbPath,
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                throw new Exception($"ADB 命令执行失败：{error}");
            }

            return output;
        }, cancellationToken);
    }
}
