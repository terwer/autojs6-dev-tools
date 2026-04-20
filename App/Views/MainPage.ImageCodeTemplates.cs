using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using App.Models;
using Core.Models;
using Microsoft.UI.Xaml;

namespace App.Views;

public sealed partial class MainPage
{
    private async void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentCropRegion == null)
        {
            ShowActionTip("请先创建裁剪区域", StatusTone.Warning, SaveTemplateButton, "无法保存");
            return;
        }

        try
        {
            SetStatus("正在保存模板和代码...", StatusTone.Info);

            var templateName = TemplateNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(templateName))
            {
                templateName = $"template_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            Directory.CreateDirectory(_saveFolderPath);

            var templatePath = await ExportCroppedTemplate(_currentCropRegion, templateName);
            var regionRef = GenerateRegionRef(_currentCropRegion, padding: MatchRegionPadding);
            var threshold = ThresholdSlider?.Value ?? 0.84;
            var previewItems = GenerateImageModeCodePreviewItems(templatePath, regionRef, _currentCropRegion, threshold);
            var selectedItem = SelectImageCodePreviewItemForSave(previewItems);
            var codePath = Path.Combine(_saveFolderPath, $"{templateName}.js");

            await File.WriteAllTextAsync(codePath, selectedItem.Code, Encoding.UTF8);

            _latestImageCodePreviewItems.Clear();
            _latestImageCodePreviewItems.AddRange(previewItems);
            _latestGeneratedCode = selectedItem.Code;
            UpdateButtonStates();

            Services.LogService.Instance.Log($"[保存] 模板: {templatePath}");
            Services.LogService.Instance.Log($"[保存] 代码: {codePath}");
            Services.LogService.Instance.Log($"[保存] 当前代码模板: {selectedItem.Title}");
            ShowActionTip($"模板与代码已保存（{selectedItem.Title}）", StatusTone.Success, SaveTemplateButton, "保存成功");
        }
        catch (Exception ex)
        {
            ShowActionTip($"保存失败：{ex.Message}", StatusTone.Error, SaveTemplateButton, "保存失败");
        }
    }

    private IReadOnlyList<CodePreviewTemplateItem> GenerateImageModeCodePreviewItems(
        string templatePath,
        int[] regionRef,
        CropRegion cropRegion,
        double threshold)
    {
        return new[]
        {
            new CodePreviewTemplateItem(
                "封装版（gist-ready）",
                "封装版短调用示例；完整 helper 实现请点上方 GitHub Gist。",
                GenerateImageModeCodeByTemplate(ImageCodeTemplateKind.ReferenceSingleFile, templatePath, regionRef, cropRegion, threshold),
                ImageCodeTemplateKind.ReferenceSingleFile,
                ReferenceSingleFileGistUrl),
            new CodePreviewTemplateItem(
                "原生 matchTemplate",
                "原生 matchTemplate 3-5 行调用示例，只展示怎么调用。",
                GenerateImageModeCodeByTemplate(ImageCodeTemplateKind.MatchTemplateNative, templatePath, regionRef, cropRegion, threshold),
                ImageCodeTemplateKind.MatchTemplateNative),
            new CodePreviewTemplateItem(
                "原生 matchFeature",
                "原生 matchFeature 3-5 行调用示例，只展示怎么调用。",
                GenerateImageModeCodeByTemplate(ImageCodeTemplateKind.MatchFeatureNative, templatePath, regionRef, cropRegion, threshold),
                ImageCodeTemplateKind.MatchFeatureNative)
        };
    }

    private CodePreviewTemplateItem SelectImageCodePreviewItemForSave(IReadOnlyList<CodePreviewTemplateItem> items)
    {
        foreach (var item in items)
        {
            if (item.TemplateKind == _selectedImageCodeTemplateKind)
            {
                return item;
            }
        }

        return items[0];
    }
}
