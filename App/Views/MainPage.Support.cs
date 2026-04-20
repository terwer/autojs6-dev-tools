using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private async Task<string> ExportCroppedTemplate(CropRegion cropRegion, string templateName)
    {
        var templatePath = Path.Combine(_saveFolderPath, $"{templateName}.png");
        await Canvas.SaveCropRegionAsync(cropRegion, templatePath);
        return templatePath;
    }

    private int[] GenerateRegionRef(CropRegion cropRegion, int padding)
    {
        var x = Math.Max(0, cropRegion.X - padding);
        var y = Math.Max(0, cropRegion.Y - padding);
        var right = Math.Min(
            cropRegion.OriginalWidth ?? cropRegion.X + cropRegion.Width,
            cropRegion.X + cropRegion.Width + padding);
        var bottom = Math.Min(
            cropRegion.OriginalHeight ?? cropRegion.Y + cropRegion.Height,
            cropRegion.Y + cropRegion.Height + padding);
        var width = right - x;
        var height = bottom - y;

        var screenWidth = cropRegion.OriginalWidth ?? 1280;
        var screenHeight = cropRegion.OriginalHeight ?? 720;
        var isLandscape = screenWidth >= screenHeight;

        var refWidth = isLandscape ? 1280 : 720;
        var refHeight = isLandscape ? 720 : 1280;
        var widthRatio = (double)refWidth / screenWidth;
        var heightRatio = (double)refHeight / screenHeight;

        return
        [
            (int)Math.Round(x * widthRatio),
            (int)Math.Round(y * heightRatio),
            (int)Math.Round(width * widthRatio),
            (int)Math.Round(height * heightRatio)
        ];
    }

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
}
