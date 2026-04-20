using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

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
            await ShowCodePreviewDialogAsync(_latestImageCodePreviewItems.ToArray(), "图像调用模板");
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
                "直接复制这段代码，就能测试当前选中的控件。"),
            "代码预览");
        ShowActionTip("已生成控件代码", StatusTone.Success, target);
    }

    private void PropertyPanel_CodeGenerated(object? sender, string code)
    {
        _latestGeneratedCode = code;
        UpdateButtonStates();
    }

    private void CodePreviewDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var code = _currentCodePreviewItem?.Code ?? CodePreviewDialogView?.GetCode();
        if (string.IsNullOrWhiteSpace(code))
        {
            ShowActionTip("当前没有可复制的代码", StatusTone.Warning, CodePreviewDialogView);
            args.Cancel = true;
            return;
        }

        CopyToClipboard(code);
        ShowActionTip("已复制代码", StatusTone.Success, CodePreviewDialogView);
        args.Cancel = true;
    }

    private async void CodePreviewGistButton_Click(object sender, RoutedEventArgs e)
    {
        var url = _currentCodePreviewItem?.ExternalUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            ShowActionTip("当前模板没有可打开的完整实现链接", StatusTone.Warning, CodePreviewGistButton);
            return;
        }

        var launched = await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
        ShowActionTip(
            launched ? "已打开 GitHub Gist" : "打开 GitHub Gist 失败",
            launched ? StatusTone.Success : StatusTone.Error,
            CodePreviewGistButton,
            launched ? null : null);
    }

    private void CodePreviewReferenceTabButton_Click(object sender, RoutedEventArgs e)
    {
        SelectCodePreviewTemplate(ImageCodeTemplateKind.ReferenceSingleFile);
    }

    private void CodePreviewMatchTemplateTabButton_Click(object sender, RoutedEventArgs e)
    {
        SelectCodePreviewTemplate(ImageCodeTemplateKind.MatchTemplateNative);
    }

    private void CodePreviewMatchFeatureTabButton_Click(object sender, RoutedEventArgs e)
    {
        SelectCodePreviewTemplate(ImageCodeTemplateKind.MatchFeatureNative);
    }

    private async Task ShowCodePreviewDialogAsync(IReadOnlyList<CodePreviewTemplateItem> items, string title)
    {
        if (items.Count == 0)
        {
            ShowActionTip("当前没有可查看的代码", StatusTone.Warning, ViewCodeRightButton);
            return;
        }

        CodePreviewDialog.Title = title;
        CodePreviewDialog.XamlRoot = XamlRoot;
        ConfigureCodePreviewTemplates(items);
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

    private void ConfigureCodePreviewTemplates(IReadOnlyList<CodePreviewTemplateItem> items)
    {
        var snapshot = items.ToList();
        if (snapshot.Count == 0)
        {
            _currentCodePreviewItem = null;
            CodePreviewTemplateStrip.Visibility = Visibility.Collapsed;
            CodePreviewDialogView.SetCode(string.Empty);
            CodePreviewDialog.IsPrimaryButtonEnabled = false;
            CodePreviewTemplateDescriptionText.Text = string.Empty;
            CodePreviewTemplateDescriptionText.Visibility = Visibility.Collapsed;
            CodePreviewGistButton.Visibility = Visibility.Collapsed;
            CodePreviewMetaPanel.Visibility = Visibility.Collapsed;
            UpdateCodePreviewTemplateButtons();
            return;
        }

        var isImageTemplates = snapshot.Count == 3 && snapshot.All(item => item.TemplateKind.HasValue);
        CodePreviewTemplateStrip.Visibility = isImageTemplates ? Visibility.Visible : Visibility.Collapsed;

        if (isImageTemplates)
        {
            _latestImageCodePreviewItems.Clear();
            _latestImageCodePreviewItems.AddRange(snapshot);

            var selectedItem = snapshot.FirstOrDefault(item => item.TemplateKind == _selectedImageCodeTemplateKind)
                               ?? snapshot[0];
            _selectedImageCodeTemplateKind = selectedItem.TemplateKind ?? ImageCodeTemplateKind.ReferenceSingleFile;
            ApplySelectedCodePreviewItem(selectedItem);
            UpdateCodePreviewTemplateButtons();
            return;
        }

        ApplySelectedCodePreviewItem(snapshot[0]);
        UpdateCodePreviewTemplateButtons();
    }

    private CodePreviewTemplateItem? FindImageCodePreviewItem(ImageCodeTemplateKind kind)
    {
        return _latestImageCodePreviewItems.FirstOrDefault(item => item.TemplateKind == kind);
    }

    private void SelectCodePreviewTemplate(ImageCodeTemplateKind kind)
    {
        var item = FindImageCodePreviewItem(kind);
        if (item == null)
        {
            return;
        }

        _selectedImageCodeTemplateKind = kind;
        ApplySelectedCodePreviewItem(item);
        UpdateCodePreviewTemplateButtons();
    }

    private void UpdateCodePreviewTemplateButtons()
    {
        if (CodePreviewReferenceTabButton == null ||
            CodePreviewMatchTemplateTabButton == null ||
            CodePreviewMatchFeatureTabButton == null)
        {
            return;
        }

        var isImageTemplates = CodePreviewTemplateStrip.Visibility == Visibility.Visible;
        if (!isImageTemplates)
        {
            CodePreviewReferenceTabButton.IsChecked = false;
            CodePreviewMatchTemplateTabButton.IsChecked = false;
            CodePreviewMatchFeatureTabButton.IsChecked = false;
            return;
        }

        CodePreviewReferenceTabButton.IsChecked = _selectedImageCodeTemplateKind == ImageCodeTemplateKind.ReferenceSingleFile;
        CodePreviewMatchTemplateTabButton.IsChecked = _selectedImageCodeTemplateKind == ImageCodeTemplateKind.MatchTemplateNative;
        CodePreviewMatchFeatureTabButton.IsChecked = _selectedImageCodeTemplateKind == ImageCodeTemplateKind.MatchFeatureNative;
    }

    private static void SetCodePreviewMetaLinkVisible(HyperlinkButton button, bool visible)
    {
        button.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        button.Opacity = visible ? 1 : 0;
        button.IsHitTestVisible = visible;
    }

    private void ApplySelectedCodePreviewItem(CodePreviewTemplateItem item)
    {
        _currentCodePreviewItem = item;

        var code = item.Code ?? string.Empty;
        CodePreviewDialogView.SetCode(code);
        CodePreviewDialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(code);

        CodePreviewTemplateDescriptionText.Text = item.Description ?? string.Empty;
        var hasDescription = !string.IsNullOrWhiteSpace(item.Description);
        CodePreviewTemplateDescriptionText.Visibility = hasDescription ? Visibility.Visible : Visibility.Collapsed;

        var hasExternalUrl = !string.IsNullOrWhiteSpace(item.ExternalUrl);
        SetCodePreviewMetaLinkVisible(CodePreviewGistButton, hasExternalUrl);

        var shouldKeepMetaStable = CodePreviewTemplateStrip.Visibility == Visibility.Visible;
        CodePreviewMetaPanel.Visibility = shouldKeepMetaStable || hasDescription || hasExternalUrl
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
