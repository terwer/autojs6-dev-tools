using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
}
