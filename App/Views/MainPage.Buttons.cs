using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace App.Views;

public sealed partial class MainPage
{
    private bool _isCropDangerPointerOver;
    private bool _isCropDangerPressed;

    private void AttachCropButtonInteractionHandlers()
    {
        if (StartCropButton == null)
        {
            return;
        }

        StartCropButton.PointerEntered -= StartCropButton_PointerEntered;
        StartCropButton.PointerExited -= StartCropButton_PointerExited;
        StartCropButton.PointerPressed -= StartCropButton_PointerPressed;
        StartCropButton.PointerReleased -= StartCropButton_PointerReleased;
        StartCropButton.PointerCaptureLost -= StartCropButton_PointerCaptureLost;

        StartCropButton.PointerEntered += StartCropButton_PointerEntered;
        StartCropButton.PointerExited += StartCropButton_PointerExited;
        StartCropButton.PointerPressed += StartCropButton_PointerPressed;
        StartCropButton.PointerReleased += StartCropButton_PointerReleased;
        StartCropButton.PointerCaptureLost += StartCropButton_PointerCaptureLost;
    }

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
        StartCropButton.ClearValue(Control.BackgroundProperty);
        StartCropButton.ClearValue(Control.BorderBrushProperty);

        var content = BuildButtonContent(
            isDanger ? "\uE10A" : "\uE16E",
            isDanger ? "退出裁剪" : "开始裁剪");

        if (isDanger)
        {
            var (foreground, background, borderBrush) = ResolveCropDangerVisuals();
            StartCropButton.Foreground = foreground;
            StartCropButton.Background = background;
            StartCropButton.BorderBrush = borderBrush;
            ApplyButtonContentForeground(content, foreground);
        }
        else
        {
            _isCropDangerPointerOver = false;
            _isCropDangerPressed = false;
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
    }

    private (Brush Foreground, Brush Background, Brush BorderBrush) ResolveCropDangerVisuals()
    {
        if (StartCropButton?.IsEnabled != true)
        {
            return (
                new SolidColorBrush(Windows.UI.Color.FromArgb(204, 255, 255, 255)),
                new SolidColorBrush(Windows.UI.Color.FromArgb(102, 209, 67, 67)),
                new SolidColorBrush(Windows.UI.Color.FromArgb(102, 209, 67, 67)));
        }

        var color = _isCropDangerPressed
            ? Windows.UI.Color.FromArgb(255, 169, 49, 49)
            : _isCropDangerPointerOver
                ? Windows.UI.Color.FromArgb(255, 197, 58, 58)
                : Windows.UI.Color.FromArgb(255, 209, 67, 67);

        var brush = new SolidColorBrush(color);
        return (new SolidColorBrush(Colors.White), brush, brush);
    }

    private void StartCropButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isCropDangerPointerOver = true;
        RequestCropButtonVisualStateRefresh();
    }

    private void StartCropButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isCropDangerPointerOver = false;
        _isCropDangerPressed = false;
        RequestCropButtonVisualStateRefresh();
    }

    private void StartCropButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isCropDangerPressed = true;
        RequestCropButtonVisualStateRefresh();
    }

    private void StartCropButton_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isCropDangerPressed = false;
        RequestCropButtonVisualStateRefresh();
    }

    private void StartCropButton_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isCropDangerPressed = false;
        RequestCropButtonVisualStateRefresh();
    }

    private void RequestCropButtonVisualStateRefresh()
    {
        if (StartCropButton?.DispatcherQueue == null)
        {
            ApplyCropButtonVisualState();
            return;
        }

        _ = StartCropButton.DispatcherQueue.TryEnqueue(() =>
        {
            ApplyCropButtonVisualState();
        });
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
