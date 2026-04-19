using Core.Models;

namespace Core.Abstractions;

/// <summary>
/// ADB 服务接口
/// </summary>
public interface IAdbService
{
    /// <summary>
    /// 扫描连接的设备
    /// </summary>
    /// <returns>设备列表</returns>
    Task<List<AdbDevice>> ScanDevicesAsync();

    /// <summary>
    /// 异步拉取设备截图
    /// </summary>
    /// <param name="device">目标设备</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>(PNG 图像字节流, 宽度, 高度)</returns>
    Task<(byte[] Data, int Width, int Height)> CaptureScreenshotAsync(AdbDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步拉取 UI Dump（XML）
    /// </summary>
    /// <param name="device">目标设备</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>UI 层次结构 XML 字符串</returns>
    Task<string> DumpUiHierarchyAsync(AdbDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查设备连接状态
    /// </summary>
    /// <param name="device">目标设备</param>
    /// <returns>是否在线</returns>
    Task<bool> IsDeviceOnlineAsync(AdbDevice device);

    /// <summary>
    /// 连接到网络设备（TCP/IP）
    /// </summary>
    /// <param name="address">设备地址（如 192.168.1.100:5555）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接结果</returns>
    Task<string> ConnectDeviceAsync(string address, CancellationToken cancellationToken = default);
}
