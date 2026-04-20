using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private WidgetNode? _uiRootNode;
    private readonly Dictionary<TreeViewNode, WidgetNode> _treeToWidgetMap = [];
    private readonly Dictionary<WidgetNode, TreeViewNode> _widgetToTreeMap = new(ReferenceEqualityComparer.Instance);

    private void UiSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RebuildUiNodeTree();
    }

    private void UiNodeTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (sender.SelectedNode == null ||
            !_treeToWidgetMap.TryGetValue(sender.SelectedNode, out var widget))
        {
            return;
        }

        SelectWidget(widget, syncTreeSelection: false);
        SetStatus($"已从节点树选择控件：{widget.ClassName}", StatusTone.Info);
    }

    private void UpdateUiTreeSummary()
    {
        if (UiTreeSummaryText == null)
        {
            return;
        }

        var searchKeyword = UiSearchTextBox?.Text?.Trim();
        var searchSuffix = string.IsNullOrWhiteSpace(searchKeyword)
            ? string.Empty
            : $" · 搜索：{searchKeyword}";

        UiTreeSummaryText.Text = _uiDisplayedNodes <= 0
            ? "尚未拉取 UI 树"
            : $"已显示 {_uiDisplayedNodes} 个业务节点（原始 {_uiTotalNodes}）{searchSuffix}";
    }

    private void RebuildUiNodeTree()
    {
        if (UiNodeTreeView == null)
        {
            return;
        }

        UiNodeTreeView.RootNodes.Clear();
        _treeToWidgetMap.Clear();
        _widgetToTreeMap.Clear();

        if (_uiRootNode == null)
        {
            UpdateUiTreeSummary();
            return;
        }

        var query = UiSearchTextBox?.Text?.Trim();
        var rootNodes = BuildTreeNodes(_uiRootNode, query);

        foreach (var rootNode in rootNodes)
        {
            UiNodeTreeView.RootNodes.Add(rootNode);
        }

        UpdateUiTreeSummary();

        if (_selectedWidget != null)
        {
            SelectTreeNodeForWidget(_selectedWidget);
        }
    }

    private List<TreeViewNode> BuildTreeNodes(WidgetNode node, string? query)
    {
        var childNodes = new List<TreeViewNode>();
        foreach (var child in node.Children)
        {
            childNodes.AddRange(BuildTreeNodes(child, query));
        }

        var isBusinessNode = IsBusinessNode(node);
        var matchesQuery = MatchesTreeQuery(node, query);
        var shouldCreateNode = isBusinessNode && (string.IsNullOrWhiteSpace(query) || matchesQuery || childNodes.Count > 0);

        if (!shouldCreateNode)
        {
            return childNodes;
        }

        var treeNode = new TreeViewNode
        {
            Content = BuildTreeLabel(node),
            IsExpanded = !string.IsNullOrWhiteSpace(query) || node.Depth < 2
        };

        _treeToWidgetMap[treeNode] = node;
        _widgetToTreeMap[node] = treeNode;

        foreach (var childNode in childNodes)
        {
            treeNode.Children.Add(childNode);
        }

        return [treeNode];
    }

    private static bool IsBusinessNode(WidgetNode node)
    {
        var hasMeaningfulContent = !string.IsNullOrWhiteSpace(node.ResourceId) ||
                                   !string.IsNullOrWhiteSpace(node.Text) ||
                                   !string.IsNullOrWhiteSpace(node.ContentDesc) ||
                                   node.Clickable;

        var isLayoutContainer = node.ClassName.Contains("Layout", StringComparison.OrdinalIgnoreCase) &&
                                !hasMeaningfulContent;

        return !isLayoutContainer;
    }

    private static bool MatchesTreeQuery(WidgetNode node, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return node.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               (!string.IsNullOrWhiteSpace(node.Text) && node.Text.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
               (!string.IsNullOrWhiteSpace(node.ContentDesc) && node.ContentDesc.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
               (!string.IsNullOrWhiteSpace(node.ResourceId) && node.ResourceId.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildTreeLabel(WidgetNode node)
    {
        var className = node.ClassName.Split('.').LastOrDefault() ?? node.ClassName;
        var summary = !string.IsNullOrWhiteSpace(node.Text)
            ? node.Text
            : !string.IsNullOrWhiteSpace(node.ContentDesc)
                ? node.ContentDesc
                : !string.IsNullOrWhiteSpace(node.ResourceId)
                    ? node.ResourceId
                    : node.Bounds;

        return $"{className} · {summary}";
    }

    private void SelectWidget(WidgetNode widget, bool syncTreeSelection)
    {
        _selectedWidget = widget;
        PropertyPanel?.SetWidget(widget);
        Canvas.SetSelectedWidget(widget);
        UpdateSelectedWidgetSummary();
        UpdateButtonStates();

        if (syncTreeSelection)
        {
            SelectTreeNodeForWidget(widget);
        }
    }

    private void SelectTreeNodeForWidget(WidgetNode widget)
    {
        if (UiNodeTreeView == null || !_widgetToTreeMap.TryGetValue(widget, out var treeNode))
        {
            return;
        }

        ExpandTreeNodeAncestors(treeNode);
        UiNodeTreeView.SelectedNode = treeNode;
    }

    private static void ExpandTreeNodeAncestors(TreeViewNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }
}
