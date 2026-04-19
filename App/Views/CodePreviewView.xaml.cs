using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace App.Views;

/// <summary>
/// 代码预览视图
/// </summary>
public sealed partial class CodePreviewView : UserControl
{
    public CodePreviewView()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// 设置代码内容
    /// </summary>
    public void SetCode(string code)
    {
        CodeTextBox.Text = code;
    }

    /// <summary>
    /// 获取代码内容
    /// </summary>
    public string GetCode() => CodeTextBox.Text;

    /// <summary>
    /// 复制代码按钮点击
    /// </summary>
    private void CopyCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var code = CodeTextBox.Text;
        if (string.IsNullOrEmpty(code))
        {
            return;
        }

        var dataPackage = new DataPackage();
        dataPackage.SetText(code);
        Clipboard.SetContent(dataPackage);
    }

    /// <summary>
    /// 导出 JS 文件按钮点击
    /// TODO: 实现文件保存对话框（需要窗口句柄）
    /// </summary>
    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var code = CodeTextBox.Text;
        if (string.IsNullOrEmpty(code))
        {
            return;
        }

        try
        {
            // TODO: 实现文件保存功能
            // 当前简化版：仅复制到剪贴板
            var dataPackage = new DataPackage();
            dataPackage.SetText(code);
            Clipboard.SetContent(dataPackage);

            var dialog = new ContentDialog
            {
                Title = "提示",
                Content = "代码已复制到剪贴板。文件保存功能将在后续版本实现。",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = $"操作失败：{ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
