namespace Core.Models;

/// <summary>
/// 模板匹配结果
/// </summary>
public class MatchResult
{
    /// <summary>
    /// 匹配位置 X 坐标（左上角）
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// 匹配位置 Y 坐标（左上角）
    /// </summary>
    public required int Y { get; init; }

    /// <summary>
    /// 模板宽度
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// 模板高度
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// 置信度（0.0 - 1.0）
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// 匹配耗时（毫秒）
    /// </summary>
    public required long ElapsedMilliseconds { get; init; }

    /// <summary>
    /// 点击坐标 X（中心点）
    /// </summary>
    public int ClickX => X + Width / 2;

    /// <summary>
    /// 点击坐标 Y（中心点）
    /// </summary>
    public int ClickY => Y + Height / 2;

    /// <summary>
    /// 是否匹配成功（基于阈值）
    /// </summary>
    public bool IsMatch { get; init; }

    /// <summary>
    /// 匹配算法（如 TM_CCOEFF_NORMED）
    /// </summary>
    public string? Algorithm { get; init; }

    /// <summary>
    /// 使用的阈值
    /// </summary>
    public double? Threshold { get; init; }
}
