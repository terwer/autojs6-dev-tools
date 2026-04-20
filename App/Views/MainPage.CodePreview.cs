using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace App.Views;

public sealed partial class MainPage
{
    private sealed class CodePreviewTemplateItem
    {
        public CodePreviewTemplateItem(
            string title,
            string description,
            string code,
            ImageCodeTemplateKind? templateKind = null,
            string? externalUrl = null)
        {
            Title = title;
            Description = description;
            Code = code;
            TemplateKind = templateKind;
            ExternalUrl = externalUrl;
        }

        public string Title { get; }

        public string Description { get; }

        public string Code { get; }

        public ImageCodeTemplateKind? TemplateKind { get; }

        public string? ExternalUrl { get; }
    }

    private readonly List<CodePreviewTemplateItem> _latestImageCodePreviewItems = new();
    private ImageCodeTemplateKind _selectedImageCodeTemplateKind = ImageCodeTemplateKind.ReferenceSingleFile;
    private CodePreviewTemplateItem? _currentCodePreviewItem;

    private async void ViewCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var target = sender as FrameworkElement;
        if (_workbenchMode == WorkbenchMode.Image && _latestImageCodePreviewItems.Count > 0)
        {
            await ShowCodePreviewDialogAsync(_latestImageCodePreviewItems, "图像代码调用模板预览");
            return;
        }

        if (string.IsNullOrWhiteSpace(_latestGeneratedCode))
        {
            ShowActionTip("当前没有可查看的代码，请先保存模板或生成代码", StatusTone.Warning, target, "无法查看代码");
            return;
        }

        await ShowCodePreviewDialogAsync(BuildSingleCodePreviewItems(_latestGeneratedCode), "代码预览");
    }

    private async void PreviewWidgetSnippetButton_Click(object sender, RoutedEventArgs e)
    {
        var target = sender as FrameworkElement;
        var snippet = PropertyPanel.GetClickSnippet();
        if (string.IsNullOrWhiteSpace(snippet))
        {
            ShowActionTip("请先在画布或节点树中选择控件", StatusTone.Warning, target, "无法生成代码");
            return;
        }

        _latestGeneratedCode = snippet;
        UpdateButtonStates();
        await ShowCodePreviewDialogAsync(
            BuildSingleCodePreviewItems(
                snippet,
                "控件代码",
                "当前选中控件生成的纯 AutoJS6 控件模式代码片段。"),
            "代码预览");
        ShowActionTip("控件代码已生成", StatusTone.Success, target);
    }

    private void PropertyPanel_CodeGenerated(object? sender, string code)
    {
        _latestGeneratedCode = code;
        UpdateButtonStates();
    }

    private void CodePreviewDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var code = GetCurrentCodePreviewItem()?.Code ?? CodePreviewDialogView?.GetCode();
        if (string.IsNullOrWhiteSpace(code))
        {
            ShowActionTip("当前没有可复制的代码", StatusTone.Warning, null, "无法复制代码");
            args.Cancel = true;
            return;
        }

        CopyToClipboard(code);
        ShowActionTip("代码已复制到剪贴板", StatusTone.Success);
        args.Cancel = true;
    }

    private async void CodePreviewGistButton_Click(object sender, RoutedEventArgs e)
    {
        var url = GetCurrentCodePreviewItem()?.ExternalUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            ShowActionTip("当前模板没有可打开的 Gist 链接", StatusTone.Warning, null, "无法打开链接");
            return;
        }

        var launched = await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
        ShowActionTip(
            launched ? "已打开 GitHub Gist" : "打开 GitHub Gist 失败",
            launched ? StatusTone.Success : StatusTone.Error,
            null,
            launched ? "已打开链接" : "打开失败");
    }

    private void CodePreviewTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplySelectedCodePreviewItem(GetCurrentCodePreviewItem());
    }

    private Task ShowCodePreviewDialogAsync(string code)
    {
        return ShowCodePreviewDialogAsync(BuildSingleCodePreviewItems(code), "代码预览");
    }

    private async Task ShowCodePreviewDialogAsync(IReadOnlyList<CodePreviewTemplateItem> items, string title)
    {
        if (items.Count == 0)
        {
            ShowActionTip("当前没有可查看的代码", StatusTone.Warning, null, "无法查看代码");
            return;
        }

        CodePreviewDialog.Title = title;
        CodePreviewDialog.XamlRoot = XamlRoot;
        ConfigureCodePreviewTabs(items);
        ApplySelectedCodePreviewItem(GetCurrentCodePreviewItem());
        await CodePreviewDialog.ShowAsync();
    }

    private IReadOnlyList<CodePreviewTemplateItem> BuildSingleCodePreviewItems(
        string code,
        string title = "代码预览",
        string description = "当前生成的代码内容。")
    {
        return new[]
        {
            new CodePreviewTemplateItem(title, description, code)
        };
    }

    private void ConfigureCodePreviewTabs(IReadOnlyList<CodePreviewTemplateItem> items)
    {
        CodePreviewTabView.TabItems.Clear();

        TabViewItem? selectedTab = null;
        foreach (var item in items)
        {
            var tab = new TabViewItem
            {
                Header = item.Title,
                Tag = item,
                IsClosable = false
            };
            CodePreviewTabView.TabItems.Add(tab);

            if (selectedTab == null && ShouldSelectCodePreviewItem(item, items.Count))
            {
                selectedTab = tab;
            }
        }

        CodePreviewTabView.Visibility = items.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        CodePreviewTabView.SelectedItem = selectedTab ?? CodePreviewTabView.TabItems[0];
    }

    private bool ShouldSelectCodePreviewItem(CodePreviewTemplateItem item, int itemCount)
    {
        if (item.TemplateKind.HasValue)
        {
            return item.TemplateKind.Value == _selectedImageCodeTemplateKind;
        }

        return itemCount == 1;
    }

    private CodePreviewTemplateItem? GetCurrentCodePreviewItem()
    {
        return CodePreviewTabView.SelectedItem is TabViewItem tab
            ? tab.Tag as CodePreviewTemplateItem
            : _currentCodePreviewItem;
    }

    private void ApplySelectedCodePreviewItem(CodePreviewTemplateItem? item)
    {
        _currentCodePreviewItem = item;

        var code = item?.Code ?? string.Empty;
        CodePreviewDialogView.SetCode(code);
        CodePreviewDialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(code);

        var description = item?.Description ?? string.Empty;
        CodePreviewTemplateDescriptionText.Text = description;
        CodePreviewTemplateDescriptionText.Visibility = string.IsNullOrWhiteSpace(description)
            ? Visibility.Collapsed
            : Visibility.Visible;

        var hasExternalUrl = !string.IsNullOrWhiteSpace(item?.ExternalUrl);
        CodePreviewGistButton.Visibility = hasExternalUrl ? Visibility.Visible : Visibility.Collapsed;
        CodePreviewMetaPanel.Visibility = (CodePreviewTemplateDescriptionText.Visibility == Visibility.Visible || hasExternalUrl)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (item?.TemplateKind is ImageCodeTemplateKind kind)
        {
            _selectedImageCodeTemplateKind = kind;
        }
    }
}
