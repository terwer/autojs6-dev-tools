using Core.Models;

namespace Core.Abstractions;

/// <summary>
/// UI Dump 解析器接口
/// </summary>
public interface IUiDumpParser
{
    /// <summary>
    /// 解析 UI Dump XML
    /// </summary>
    /// <param name="xmlContent">XML 字符串</param>
    /// <returns>控件节点树根节点</returns>
    Task<WidgetNode?> ParseAsync(string xmlContent);

    /// <summary>
    /// 过滤控件节点（应用布局容器过滤规则）
    /// </summary>
    /// <param name="root">根节点</param>
    /// <returns>过滤后的控件列表（扁平化）</returns>
    List<WidgetNode> FilterNodes(WidgetNode root);

    /// <summary>
    /// 查找匹配的控件节点
    /// </summary>
    /// <param name="root">根节点</param>
    /// <param name="resourceId">资源 ID（可选）</param>
    /// <param name="text">文本（可选）</param>
    /// <param name="contentDesc">内容描述（可选）</param>
    /// <param name="className">类名（可选）</param>
    /// <returns>匹配的控件列表</returns>
    List<WidgetNode> FindNodes(
        WidgetNode root,
        string? resourceId = null,
        string? text = null,
        string? contentDesc = null,
        string? className = null);

    /// <summary>
    /// 根据坐标查找控件节点
    /// </summary>
    /// <param name="root">根节点</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <returns>匹配的控件（最深层）</returns>
    WidgetNode? FindNodeByCoordinate(WidgetNode root, int x, int y);

    /// <summary>
    /// 生成 UiSelector 代码
    /// </summary>
    /// <param name="node">控件节点</param>
    /// <returns>UiSelector 代码字符串</returns>
    string GenerateUiSelector(WidgetNode node);
}
