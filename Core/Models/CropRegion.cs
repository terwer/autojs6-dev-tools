namespace Core.Models;

/// <summary>
/// 裁剪区域信息
/// </summary>
public class CropRegion
{
    /// <summary>
    /// X 坐标（左上角）
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// Y 坐标（左上角）
    /// </summary>
    public required int Y { get; init; }

    /// <summary>
    /// 宽度
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// 高度
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// 区域名称（可选）
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// 原始图像宽度（用于坐标转换）
    /// </summary>
    public int? OriginalWidth { get; init; }

    /// <summary>
    /// 原始图像高度（用于坐标转换）
    /// </summary>
    public int? OriginalHeight { get; init; }

    /// <summary>
    /// 参考分辨率宽度（用于 regionRef 生成）
    /// </summary>
    public int? ReferenceWidth { get; init; }

    /// <summary>
    /// 参考分辨率高度（用于 regionRef 生成）
    /// </summary>
    public int? ReferenceHeight { get; init; }
}
