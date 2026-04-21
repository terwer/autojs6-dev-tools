using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private async Task ShowErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "错误",
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = XamlRoot
        };
        await dialog.ShowAsync();
    }

    private int CountAllNodes(WidgetNode node)
    {
        var count = 1;
        foreach (var child in node.Children)
        {
            count += CountAllNodes(child);
        }

        return count;
    }

    private List<WidgetNode> GetAllNodes(WidgetNode node)
    {
        var result = new List<WidgetNode> { node };
        foreach (var child in node.Children)
        {
            result.AddRange(GetAllNodes(child));
        }

        return result;
    }

    private Windows.Storage.Pickers.FileOpenPicker CreateImageFilePicker()
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        return picker;
    }

    private void UpdateTemplateNameFromPath(string path)
    {
        if (TemplateNameTextBox == null || !string.IsNullOrWhiteSpace(TemplateNameTextBox.Text))
        {
            return;
        }

        TemplateNameTextBox.Text = Path.GetFileNameWithoutExtension(path);
    }
}
