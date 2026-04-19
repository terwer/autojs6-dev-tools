namespace Core.Models;

/// <summary>
/// UI 控件节点信息
/// </summary>
public class WidgetNode
{
    /// <summary>
    /// 控件类名（如 android.widget.TextView）
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// 资源 ID（如 com.example:id/button）
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// 文本内容
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// 内容描述（content-desc）
    /// </summary>
    public string? ContentDesc { get; init; }

    /// <summary>
    /// 是否可点击
    /// </summary>
    public bool Clickable { get; init; }

    /// <summary>
    /// 边界框字符串（如 "[0,0][100,50]"）
    /// </summary>
    public required string Bounds { get; init; }

    /// <summary>
    /// 边界框矩形（x, y, width, height）
    /// </summary>
    public (int X, int Y, int Width, int Height) BoundsRect { get; set; }

    /// <summary>
    /// 包名
    /// </summary>
    public string? Package { get; init; }

    /// <summary>
    /// 是否可选中
    /// </summary>
    public bool Checkable { get; init; }

    /// <summary>
    /// 是否已选中
    /// </summary>
    public bool Checked { get; init; }

    /// <summary>
    /// 是否可聚焦
    /// </summary>
    public bool Focusable { get; init; }

    /// <summary>
    /// 是否已聚焦
    /// </summary>
    public bool Focused { get; init; }

    /// <summary>
    /// 是否可滚动
    /// </summary>
    public bool Scrollable { get; init; }

    /// <summary>
    /// 是否可长按
    /// </summary>
    public bool LongClickable { get; init; }

    /// <summary>
    /// 是否已启用
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// 节点深度（用于 TreeView 渲染）
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// 子节点列表
    /// </summary>
    public List<WidgetNode> Children { get; init; } = new();
}
