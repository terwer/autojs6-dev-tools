namespace Core.Models;

/// <summary>
/// ADB 设备信息
/// </summary>
public class AdbDevice
{
    /// <summary>
    /// 设备序列号
    /// </summary>
    public required string Serial { get; init; }

    /// <summary>
    /// 设备型号
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// 设备状态（device, offline, unauthorized 等）
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// 连接类型（usb, tcpip）
    /// </summary>
    public string? ConnectionType { get; init; }

    /// <summary>
    /// 产品名称
    /// </summary>
    public string? Product { get; init; }

    /// <summary>
    /// 传输 ID
    /// </summary>
    public string? TransportId { get; init; }
}
