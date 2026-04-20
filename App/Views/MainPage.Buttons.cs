using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace App.Views;

public sealed partial class MainPage
{
    private void CopyToClipboard(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }

    private void ApplyCropButtonVisualState()
    {
        if (StartCropButton == null)
        {
            return;
        }

        var isDanger = _isCroppingMode;
        StartCropButton.Style = (Style)Application.Current.Resources[
            isDanger ? "WorkbenchDangerButtonStyle" : "WorkbenchPrimaryButtonStyle"];
        StartCropButton.ClearValue(Control.ForegroundProperty);

        var content = BuildButtonContent(
            isDanger ? "\uE10A" : "\uE16E",
            isDanger ? "退出裁剪" : "开始裁剪");

        if (isDanger)
        {
            var foreground = new SolidColorBrush(Colors.White);
            StartCropButton.Foreground = foreground;
            ApplyButtonContentForeground(content, foreground);
        }

        StartCropButton.Content = content;
    }

    private void SetDumpUiLoading(bool isLoading)
    {
        if (DumpUiStageButton != null)
        {
            DumpUiStageButton.Content = isLoading
                ? BuildLoadingButtonContent("拉取中...")
                : BuildButtonContent(Symbol.ViewAll, "拉取 UI 树");
        }

        if (DumpUiInspectorButton != null)
        {
            DumpUiInspectorButton.Content = isLoading
                ? BuildLoadingButtonContent("拉取中...")
                : BuildButtonContent(Symbol.ViewAll, "拉取 UI 树");
        }
    }

    private static void ApplyButtonContentForeground(StackPanel panel, Brush foreground)
    {
        foreach (var child in panel.Children)
        {
            switch (child)
            {
                case FontIcon fontIcon:
                    fontIcon.Foreground = foreground;
                    break;
                case SymbolIcon symbolIcon:
                    symbolIcon.Foreground = foreground;
                    break;
                case TextBlock textBlock:
                    textBlock.Foreground = foreground;
                    break;
            }
        }
    }

    private static StackPanel BuildButtonContent(string glyph, string text)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        panel.Children.Add(new FontIcon { Glyph = glyph });
        panel.Children.Add(new TextBlock { Text = text });
        return panel;
    }

    private static StackPanel BuildButtonContent(Symbol symbol, string text)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        panel.Children.Add(new SymbolIcon(symbol));
        panel.Children.Add(new TextBlock { Text = text });
        return panel;
    }

    private static StackPanel BuildLoadingButtonContent(string text)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        panel.Children.Add(new ProgressRing
        {
            IsActive = true,
            Width = 14,
            Height = 14
        });
        panel.Children.Add(new TextBlock { Text = text });
        return panel;
    }
}
