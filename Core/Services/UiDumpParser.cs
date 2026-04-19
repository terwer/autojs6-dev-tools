using System.Xml.Linq;
using System.Text.RegularExpressions;
using Core.Abstractions;
using Core.Models;

namespace Core.Services;

/// <summary>
/// UI Dump 解析器实现
/// 参考 MVP2.UiDumpParser 的最佳实践
/// </summary>
public class UiDumpParser : IUiDumpParser
{
    public async Task<WidgetNode?> ParseAsync(string xmlContent)
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var root = doc.Root?.Element("node");

                if (root == null)
                {
                    return null;
                }

                return ParseNode(root, 0);
            }
            catch
            {
                return null;
            }
        });
    }

    public List<WidgetNode> FilterNodes(WidgetNode root)
    {
        var result = new List<WidgetNode>();
        FilterNodesRecursive(root, result);
        return result;
    }

    public List<WidgetNode> FindNodes(
        WidgetNode root,
        string? resourceId = null,
        string? text = null,
        string? contentDesc = null,
        string? className = null)
    {
        var result = new List<WidgetNode>();
        FindNodesRecursive(root, result, resourceId, text, contentDesc, className);
        return result;
    }

    public WidgetNode? FindNodeByCoordinate(WidgetNode root, int x, int y)
    {
        return FindNodeByCoordinateRecursive(root, x, y);
    }

    public string GenerateUiSelector(WidgetNode node)
    {
        var parts = new List<string>();

        // 优先使用 resource-id
        if (!string.IsNullOrEmpty(node.ResourceId))
        {
            parts.Add($"id(\"{node.ResourceId}\")");
        }

        // 降级使用 text
        if (!string.IsNullOrEmpty(node.Text))
        {
            parts.Add($"text(\"{EscapeJavaScript(node.Text)}\")");
        }

        // 降级使用 content-desc
        if (!string.IsNullOrEmpty(node.ContentDesc))
        {
            parts.Add($"desc(\"{EscapeJavaScript(node.ContentDesc)}\")");
        }

        // 补充 className
        if (!string.IsNullOrEmpty(node.ClassName))
        {
            parts.Add($"className(\"{node.ClassName}\")");
        }

        // 补充 boundsInside（如果有边界框）
        if (node.BoundsRect.Width > 0 && node.BoundsRect.Height > 0)
        {
            var (x, y, w, h) = node.BoundsRect;
            parts.Add($"boundsInside({x}, {y}, {x + w}, {y + h})");
        }

        return string.Join(".", parts) + ".findOne()";
    }

    /// <summary>
    /// 递归解析 XML 节点
    /// 参考 MVP2 的实现
    /// </summary>
    private WidgetNode? ParseNode(XElement element, int depth)
    {
        var className = element.Attribute("class")?.Value ?? "";
        var resourceId = element.Attribute("resource-id")?.Value ?? "";
        var text = element.Attribute("text")?.Value ?? "";
        var contentDesc = element.Attribute("content-desc")?.Value ?? "";
        var clickable = element.Attribute("clickable")?.Value == "true";
        var bounds = element.Attribute("bounds")?.Value ?? "";
        var package = element.Attribute("package")?.Value ?? "";
        var checkable = element.Attribute("checkable")?.Value == "true";
        var checked_ = element.Attribute("checked")?.Value == "true";
        var focusable = element.Attribute("focusable")?.Value == "true";
        var focused = element.Attribute("focused")?.Value == "true";
        var scrollable = element.Attribute("scrollable")?.Value == "true";
        var longClickable = element.Attribute("long-clickable")?.Value == "true";
        var enabled = element.Attribute("enabled")?.Value == "true";

        // 解析 bounds
        var boundsRect = ParseBounds(bounds);

        var node = new WidgetNode
        {
            ClassName = className,
            ResourceId = resourceId,
            Text = text,
            ContentDesc = contentDesc,
            Clickable = clickable,
            Bounds = bounds,
            BoundsRect = boundsRect,
            Package = package,
            Checkable = checkable,
            Checked = checked_,
            Focusable = focusable,
            Focused = focused,
            Scrollable = scrollable,
            LongClickable = longClickable,
            Enabled = enabled,
            Depth = depth
        };

        // 递归解析子节点
        foreach (var child in element.Elements("node"))
        {
            var childNode = ParseNode(child, depth + 1);
            if (childNode != null)
            {
                node.Children.Add(childNode);
            }
        }

        return node;
    }

    /// <summary>
    /// 解析 bounds 字符串为矩形坐标
    /// 参考 MVP2 的实现
    /// </summary>
    private (int X, int Y, int Width, int Height) ParseBounds(string bounds)
    {
        var match = Regex.Match(bounds, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
        if (match.Success)
        {
            int x1 = int.Parse(match.Groups[1].Value);
            int y1 = int.Parse(match.Groups[2].Value);
            int x2 = int.Parse(match.Groups[3].Value);
            int y2 = int.Parse(match.Groups[4].Value);
            return (x1, y1, x2 - x1, y2 - y1);
        }
        return (0, 0, 0, 0);
    }

    /// <summary>
    /// 递归过滤节点（应用布局容器过滤规则）
    /// 参考 MVP2 的实现
    /// </summary>
    private void FilterNodesRecursive(WidgetNode node, List<WidgetNode> result)
    {
        // 参考 MVP2: 布局容器过滤规则
        bool isLayoutContainer = node.ClassName.Contains("Layout") &&
                                 string.IsNullOrEmpty(node.ResourceId) &&
                                 string.IsNullOrEmpty(node.Text) &&
                                 string.IsNullOrEmpty(node.ContentDesc) &&
                                 !node.Clickable;

        if (!isLayoutContainer)
        {
            result.Add(node);
        }

        // 递归处理子节点
        foreach (var child in node.Children)
        {
            FilterNodesRecursive(child, result);
        }
    }

    private void FindNodesRecursive(
        WidgetNode node,
        List<WidgetNode> result,
        string? resourceId,
        string? text,
        string? contentDesc,
        string? className)
    {
        bool matches = true;

        if (resourceId != null && node.ResourceId != resourceId)
            matches = false;
        if (text != null && node.Text != text)
            matches = false;
        if (contentDesc != null && node.ContentDesc != contentDesc)
            matches = false;
        if (className != null && node.ClassName != className)
            matches = false;

        if (matches)
        {
            result.Add(node);
        }

        foreach (var child in node.Children)
        {
            FindNodesRecursive(child, result, resourceId, text, contentDesc, className);
        }
    }

    private WidgetNode? FindNodeByCoordinateRecursive(WidgetNode node, int x, int y)
    {
        var (nx, ny, nw, nh) = node.BoundsRect;

        // 检查坐标是否在当前节点内
        if (x >= nx && x < nx + nw && y >= ny && y < ny + nh)
        {
            // 优先查找子节点（最深层）
            foreach (var child in node.Children)
            {
                var found = FindNodeByCoordinateRecursive(child, x, y);
                if (found != null)
                {
                    return found;
                }
            }

            // 如果没有子节点匹配，返回当前节点
            return node;
        }

        return null;
    }

    private string EscapeJavaScript(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
