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
        if (EnterCropButton == null || ExitCropButton == null)
        {
            return;
        }

        EnterCropButton.Visibility = _isCroppingMode ? Visibility.Collapsed : Visibility.Visible;
        ExitCropButton.Visibility = _isCroppingMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetDumpUiLoading(bool isLoading)
    {
        if (DumpUiStageButton != null)
        {
            DumpUiStageButton.Content = isLoading
                ? BuildLoadingButtonContent("拉取中...")
                : BuildButtonContent(Symbol.ViewAll, "拉取 UI 树");
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
