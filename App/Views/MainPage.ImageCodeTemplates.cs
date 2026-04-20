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
            SetStatus("请先创建裁剪区域", StatusTone.Warning);
            await ShowErrorAsync("请先创建裁剪区域");
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
            var codePath = Path.ChangeExtension(templatePath, ".js");

            await File.WriteAllTextAsync(codePath, selectedItem.Code, Encoding.UTF8);

            _latestImageCodePreviewItems.Clear();
            _latestImageCodePreviewItems.AddRange(previewItems);
            _latestGeneratedCode = selectedItem.Code;
            UpdateButtonStates();

            Services.LogService.Instance.Log($"[保存] 模板: {templatePath}");
            Services.LogService.Instance.Log($"[保存] 代码: {codePath}");
            Services.LogService.Instance.Log($"[保存] 当前代码模板: {selectedItem.Title}");
            SetStatus($"模板与代码已保存（{selectedItem.Title}）", StatusTone.Success);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"保存失败：{ex.Message}");
            SetStatus("保存失败", StatusTone.Error);
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
                "单文件最小可用封装版，包含 regionRef、等比缩放与 matchFeatures 兜底，并可直接打开 GitHub Gist。",
                GenerateImageModeCodeByTemplate(ImageCodeTemplateKind.ReferenceSingleFile, templatePath, regionRef, cropRegion, threshold),
                ImageCodeTemplateKind.ReferenceSingleFile,
                ReferenceSingleFileGistUrl),
            new CodePreviewTemplateItem(
                "原生 matchTemplate",
                "纯 AutoJS 原生 images.matchTemplate 路径，保留 regionRef 与等比缩放重试，不依赖任何项目封装。",
                GenerateImageModeCodeByTemplate(ImageCodeTemplateKind.MatchTemplateNative, templatePath, regionRef, cropRegion, threshold),
                ImageCodeTemplateKind.MatchTemplateNative),
            new CodePreviewTemplateItem(
                "原生 matchFeature",
                "纯 AutoJS 原生 detectAndComputeFeatures / matchFeatures 路径，适合作为特征匹配兜底脚本。",
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
