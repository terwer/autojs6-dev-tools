using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Core.Models;
using Core.Services;
using System;

namespace App.Views;

/// <summary>
/// 属性面板视图
/// </summary>
public sealed partial class PropertyPanelView : UserControl
{
    private WidgetNode? _currentWidget;
    private readonly UiDumpParser _uiDumpParser = new();

    public event EventHandler<string>? CodeGenerated;

    public PropertyPanelView()
    {
        this.InitializeComponent();
    }

    public void SetWidget(WidgetNode? widget)
    {
        _currentWidget = widget;
        UpdateProperties();
    }

    public WidgetNode? GetCurrentWidget() => _currentWidget;

    public string? GetCoordinatesText()
    {
        if (_currentWidget == null)
        {
            return null;
        }

        var (x, y, w, h) = _currentWidget.BoundsRect;
        return $"[{x}, {y}, {w}, {h}]";
    }

    public string? GetUiSelectorText()
    {
        if (_currentWidget == null)
        {
            return null;
        }

        return _uiDumpParser.GenerateUiSelector(_currentWidget);
    }

    public string? GetClickSnippet()
    {
        var selector = GetUiSelectorText();
        if (string.IsNullOrEmpty(selector))
        {
            return null;
        }

return
$@"// 控件模式：点击当前选中控件
var target = {selector};
if (target) {{
  target.click();
}} else {{
  console.log(""未找到控件"");
}}";
    }

    public void EmitSnippetPreview()
    {
        var snippet = GetClickSnippet();
        if (!string.IsNullOrEmpty(snippet))
        {
            CodeGenerated?.Invoke(this, snippet);
        }
    }

    private void UpdateProperties()
    {
        PropertiesPanel.Children.Clear();

        if (_currentWidget == null)
        {
            PropertiesPanel.Children.Add(new Border
            {
                Style = (Style)Application.Current.Resources["WorkbenchCardBorderStyle"],
                Padding = new Thickness(12),
                Child = new TextBlock
                {
                    Text = "尚未选中控件。进入控件模式后，可在中央画布中点击控件边界框查看属性。",
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.72
                }
            });
            return;
        }

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
        AddProperty("坐标", $"({x}, {y}, {w}, {h})");
        AddProperty("尺寸", $"{w} x {h}");
        AddProperty("UiSelector", GetUiSelectorText());
    }

    private void AddProperty(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var container = new Border
        {
            Style = (Style)Application.Current.Resources["WorkbenchCardBorderStyle"],
            Padding = new Thickness(12, 10, 12, 10)
        };

        var stack = new StackPanel
        {
            Spacing = 6
        };

        stack.Children.Add(new TextBlock
        {
            Text = name,
            FontSize = 12,
            Opacity = 0.72,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        stack.Children.Add(new TextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14
        });

        container.Child = stack;
        PropertiesPanel.Children.Add(container);
    }
}
