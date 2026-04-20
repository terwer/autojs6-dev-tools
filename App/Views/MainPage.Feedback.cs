using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

namespace App.Views;

public sealed partial class MainPage
{
    private DispatcherTimer? _actionFeedbackTimer;

    private TimeSpan GetActionTipDuration(StatusTone tone)
    {
        return tone switch
        {
            StatusTone.Success => TimeSpan.FromSeconds(2),
            StatusTone.Warning => TimeSpan.FromSeconds(2.5),
            StatusTone.Error => TimeSpan.FromSeconds(3.5),
            _ => TimeSpan.FromSeconds(2)
        };
    }

    private void ShowActionTip(string message, StatusTone tone, FrameworkElement? target = null, string? title = null)
    {
        if (ActionFeedbackToast == null || ActionFeedbackToastText == null || ActionFeedbackToastIcon == null)
        {
            return;
        }

        EnsureActionFeedbackTimer();
        var (foreground, background, border, glyph) = GetActionToastVisual(tone);

        _actionFeedbackTimer!.Stop();
        ActionFeedbackToastText.Text = message;
        ActionFeedbackToastText.Foreground = new SolidColorBrush(foreground);
        ActionFeedbackToastIcon.Glyph = glyph;
        ActionFeedbackToastIcon.Foreground = new SolidColorBrush(foreground);
        ActionFeedbackToast.Background = new SolidColorBrush(background);
        ActionFeedbackToast.BorderBrush = new SolidColorBrush(border);
        ActionFeedbackToast.Visibility = Visibility.Visible;
        ActionFeedbackToast.Opacity = 1;
        ResetTransientStatusIfNeeded();
        _actionFeedbackTimer.Interval = GetActionTipDuration(tone);
        _actionFeedbackTimer.Start();
    }

    private static (Windows.UI.Color foreground, Windows.UI.Color background, Windows.UI.Color border, string glyph) GetActionToastVisual(StatusTone tone)
    {
        return tone switch
        {
            StatusTone.Success => (
                Colors.ForestGreen,
                Windows.UI.Color.FromArgb(245, 241, 252, 241),
                Windows.UI.Color.FromArgb(255, 181, 227, 181),
                "\uE73E"),
            StatusTone.Warning => (
                Colors.DarkOrange,
                Windows.UI.Color.FromArgb(245, 255, 248, 235),
                Windows.UI.Color.FromArgb(255, 255, 214, 153),
                "\uE7BA"),
            StatusTone.Error => (
                Colors.IndianRed,
                Windows.UI.Color.FromArgb(245, 255, 241, 241),
                Windows.UI.Color.FromArgb(255, 242, 180, 180),
                "\uEA39"),
            _ => (
                Colors.DodgerBlue,
                Windows.UI.Color.FromArgb(245, 240, 247, 255),
                Windows.UI.Color.FromArgb(255, 178, 214, 255),
                "\uE946")
        };
    }

    private void EnsureActionFeedbackTimer()
    {
        if (_actionFeedbackTimer != null)
        {
            return;
        }

        _actionFeedbackTimer = new DispatcherTimer();
        _actionFeedbackTimer.Tick += (_, _) =>
        {
            _actionFeedbackTimer.Stop();
            if (ActionFeedbackToast != null)
            {
                ActionFeedbackToast.Opacity = 0;
                ActionFeedbackToast.Visibility = Visibility.Collapsed;
            }
        };
    }

    private void ResetTransientStatusIfNeeded()
    {
        if (StatusText == null)
        {
            return;
        }

        var current = StatusText.Text?.Trim();
        if (string.IsNullOrWhiteSpace(current))
        {
            return;
        }

        if (current.StartsWith("正在", StringComparison.Ordinal))
        {
            SetStatus("就绪", StatusTone.Info);
        }
    }
}
