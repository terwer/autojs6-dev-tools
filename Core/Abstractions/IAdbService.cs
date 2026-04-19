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
    /// 执行 ADB 命令
    /// </summary>
    /// <param name="device">目标设备</param>
    /// <param name="command">命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令输出</returns>
    Task<string> ExecuteCommandAsync(AdbDevice device, string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行全局 ADB 命令（不需要设备）
    /// </summary>
    /// <param name="command">命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>命令输出</returns>
    Task<string> ExecuteGlobalCommandAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步拉取设备截图
    /// </summary>
    /// <param name="device">目标设备</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>PNG 图像字节流</returns>
    Task<byte[]> CaptureScreenshotAsync(AdbDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步拉取 UI Dump（XML）
    /// </summary>
    /// <param name="device">目标设备</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>UI 层次结构 XML 字符串</returns>
    Task<string> DumpUiHierarchyAsync(AdbDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取设备分辨率
    /// </summary>
    /// <param name="device">目标设备</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>(宽度, 高度)</returns>
    Task<(int Width, int Height)> GetScreenResolutionAsync(AdbDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取设备屏幕旋转角度
    /// </summary>
    /// <param name="device">目标设备</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旋转角度 (0, 90, 180, 270)</returns>
    Task<int> GetScreenRotationAsync(AdbDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查设备连接状态
    /// </summary>
    /// <param name="device">目标设备</param>
    /// <returns>是否在线</returns>
    Task<bool> IsDeviceOnlineAsync(AdbDevice device);
}
