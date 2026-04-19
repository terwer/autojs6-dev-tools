using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Core.Models;
using System;

namespace App.Views;

/// <summary>
/// 属性面板视图
/// </summary>
public sealed partial class PropertyPanelView : UserControl
{
    private WidgetNode? _currentWidget;

    // 代码生成事件
    public event EventHandler<string>? CodeGenerated;

    public PropertyPanelView()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// 设置当前控件
    /// </summary>
    public void SetWidget(WidgetNode? widget)
    {
        _currentWidget = widget;
        UpdateProperties();
    }

    /// <summary>
    /// 更新属性显示
    /// </summary>
    private void UpdateProperties()
    {
        PropertiesPanel.Children.Clear();

        if (_currentWidget == null)
        {
            PropertiesPanel.Children.Add(new TextBlock { Text = "未选中控件", Opacity = 0.6 });
            return;
        }

        // 显示所有属性
        AddProperty("ClassName", _currentWidget.ClassName);
        AddProperty("ResourceId", _currentWidget.ResourceId);
        AddProperty("Text", _currentWidget.Text);
        AddProperty("ContentDesc", _currentWidget.ContentDesc);
        AddProperty("Package", _currentWidget.Package);
        AddProperty("Bounds", _currentWidget.Bounds);
        AddProperty("Clickable", _currentWidget.Clickable.ToString());
        AddProperty("Checkable", _currentWidget.Checkable.ToString());
        AddProperty("Focusable", _currentWidget.Focusable.ToString());
        AddProperty("Scrollable", _currentWidget.Scrollable.ToString());
        AddProperty("Enabled", _currentWidget.Enabled.ToString());

        var (x, y, w, h) = _currentWidget.BoundsRect;
        AddProperty("坐标", $"({x}, {y}) - ({x + w}, {y + h})");
        AddProperty("尺寸", $"{w} x {h}");
    }

    /// <summary>
    /// 添加属性行
    /// </summary>
    private void AddProperty(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var grid = new Grid
        {
            ColumnSpacing = 8,
            Margin = new Thickness(0, 4, 0, 4)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var nameText = new TextBlock
        {
            Text = name + ":",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Opacity = 0.7
        };
        Grid.SetColumn(nameText, 0);

        var valueText = new TextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(valueText, 1);

        grid.Children.Add(nameText);
        grid.Children.Add(valueText);

        PropertiesPanel.Children.Add(grid);
    }

    /// <summary>
    /// 复制坐标按钮点击
    /// </summary>
    private void CopyCoordinatesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWidget == null) return;

        var (x, y, w, h) = _currentWidget.BoundsRect;
        var coordinates = $"[{x}, {y}, {w}, {h}]";

        var dataPackage = new DataPackage();
        dataPackage.SetText(coordinates);
        Clipboard.SetContent(dataPackage);

        // 触发代码生成事件，显示在代码预览框
        CodeGenerated?.Invoke(this, $"// 控件坐标\nvar bounds = {coordinates};\n");
    }

    /// <summary>
    /// 复制 XPath 按钮点击
    /// TODO: 实现完整的 XPath 生成逻辑
    /// </summary>
    private void CopyXPathButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentWidget == null) return;

        // 简化版 XPath（仅使用 resource-id）
        var xpath = !string.IsNullOrEmpty(_currentWidget.ResourceId)
            ? $"//*[@resource-id='{_currentWidget.ResourceId}']"
            : $"//*[@class='{_currentWidget.ClassName}']";

        var dataPackage = new DataPackage();
        dataPackage.SetText(xpath);
        Clipboard.SetContent(dataPackage);

        // 触发代码生成事件，显示在代码预览框
        CodeGenerated?.Invoke(this, $"// 控件 XPath\nvar xpath = \"{xpath}\";\n");
    }
}
