using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace App.Views;

public sealed partial class MainPage
{
    private string GetActionTipTitle(StatusTone tone)
    {
        return tone switch
        {
            StatusTone.Success => "操作成功",
            StatusTone.Warning => "请注意",
            StatusTone.Error => "操作失败",
            _ => "提示"
        };
    }

    private void ShowActionTip(string message, StatusTone tone, FrameworkElement? target = null, string? title = null)
    {
        SetStatus(message, tone);

        if (ActionFeedbackTip == null)
        {
            return;
        }

        var resolvedTarget = target ?? ActionFeedbackAnchor ?? StatusPillBorder;
        ActionFeedbackTip.IsOpen = false;
        ActionFeedbackTip.Target = resolvedTarget;
        ActionFeedbackTip.Title = title ?? GetActionTipTitle(tone);
        ActionFeedbackTip.Subtitle = message;

        if (ActionFeedbackTip.DispatcherQueue != null)
        {
            _ = ActionFeedbackTip.DispatcherQueue.TryEnqueue(() =>
            {
                ActionFeedbackTip.IsOpen = true;
            });
            return;
        }

        ActionFeedbackTip.IsOpen = true;
    }
}
