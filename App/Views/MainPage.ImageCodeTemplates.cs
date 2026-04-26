using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using App.Models;
using Core.Helpers;
using Microsoft.UI.Xaml;

namespace App.Views;

public sealed partial class MainPage
{
    private async void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var target = sender as FrameworkElement;
        try
        {
            var savedTemplatePath = await SaveSelectedTemplateAsync(target);
            if (string.IsNullOrWhiteSpace(savedTemplatePath))
            {
                return;
            }

            UpdateTemplateNameFromPath(savedTemplatePath);
            UpdateSourceSummaries();
            UpdateButtonStates();
        }
        catch (Exception ex)
        {
            ShowActionTip($"保存模板失败：{ex.Message}", StatusTone.Error, target, "保存失败");
        }
    }

    private async void GenerateCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var target = sender as FrameworkElement;
        try
        {
            if (!TryResolveCodeGenerationTemplatePath(target, out var templatePath) ||
                !TryResolveCodeGenerationRegionContext(target, out var regionContext))
            {
                return;
            }

            SetStatus("正在生成代码...", StatusTone.Info);

            Directory.CreateDirectory(_saveFolderPath);

            var threshold = ThresholdSlider?.Value ?? 0.84;
            var previewItems = GenerateImageModeCodePreviewItems(
                templatePath,
                regionContext,
                threshold,
                _saveFolderPath);
            var selectedItem = SelectImageCodePreviewItemForSave(previewItems);
            var codeBaseName = GetSuggestedCodeBaseName(templatePath);
            var codePath = Path.Combine(_saveFolderPath, $"{codeBaseName}.js");

            await File.WriteAllTextAsync(codePath, selectedItem.Code, Encoding.UTF8);

            _latestImageCodePreviewItems.Clear();
            _latestImageCodePreviewItems.AddRange(previewItems);
            _latestGeneratedCode = selectedItem.Code;

            Services.LogService.Instance.Log($"[代码] 模板: {templatePath}");
            Services.LogService.Instance.Log($"[代码] 输出: {codePath}");
            Services.LogService.Instance.Log($"[代码] 当前代码模板: {selectedItem.Title}");

            UpdateButtonStates();
            ShowActionTip("已生成代码", StatusTone.Success, target);
        }
        catch (Exception ex)
        {
            ShowActionTip($"生成代码失败：{ex.Message}", StatusTone.Error, target, "生成失败");
        }
    }

    private async Task<string?> SaveSelectedTemplateAsync(FrameworkElement? target)
    {
        var suggestedBaseName = GetSuggestedTemplateBaseName();
        var outputPath = await PickTemplateOutputPathAsync(suggestedBaseName);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return null;
        }

        var normalizedOutputPath = Path.GetFullPath(outputPath);
        if (GetCurrentTemplateSourceKind() == ImageTemplateSourceKind.Crop)
        {
            if (!await ConfirmOverwriteAsync(normalizedOutputPath, "模板"))
            {
                ShowActionTip("已取消覆盖模板", StatusTone.Info, target);
                return null;
            }

            var cropSourceContext = GetActiveCropImageSourceContext();
            if (cropSourceContext == null)
            {
                ShowActionTip("请先创建裁剪区域", StatusTone.Warning, target, "无法保存");
                return null;
            }

            SetStatus("正在保存模板...", StatusTone.Info);
            await SaveCropSourceToPathAsync(cropSourceContext, normalizedOutputPath);
            _savedCropTemplatePath = normalizedOutputPath;

            Services.LogService.Instance.Log($"[模板] 当前裁剪已保存: {normalizedOutputPath}");
            ShowActionTip($"模板已保存：{Path.GetFileName(normalizedOutputPath)}", StatusTone.Success, target);
            return normalizedOutputPath;
        }

        if (string.IsNullOrWhiteSpace(_templateFilePath) || !File.Exists(_templateFilePath))
        {
            ShowActionTip("请选择模板文件", StatusTone.Warning, target, "无法保存");
            return null;
        }

        var normalizedSourcePath = Path.GetFullPath(_templateFilePath);
        if (string.Equals(normalizedSourcePath, normalizedOutputPath, StringComparison.OrdinalIgnoreCase))
        {
            _templateFilePath = normalizedOutputPath;
            UpdateSuccessfulMatchContextTemplatePath(normalizedOutputPath);
            ShowActionTip("模板已在目标位置，已忽略重复复制", StatusTone.Info, target);
            return normalizedOutputPath;
        }

        if (!await ConfirmOverwriteAsync(normalizedOutputPath, "模板"))
        {
            ShowActionTip("已取消覆盖模板", StatusTone.Info, target);
            return null;
        }

        SetStatus("正在保存模板...", StatusTone.Info);
        var templateBytes = await File.ReadAllBytesAsync(_templateFilePath);
        await SaveBytesToPathAsync(templateBytes, normalizedOutputPath);

        _templateFilePath = normalizedOutputPath;
        UpdateSuccessfulMatchContextTemplatePath(normalizedOutputPath);

        Services.LogService.Instance.Log($"[模板] 外部模板已另存: {normalizedOutputPath}");
        ShowActionTip($"模板已保存：{Path.GetFileName(normalizedOutputPath)}", StatusTone.Success, target);
        return normalizedOutputPath;
    }

    private bool TryResolveCodeGenerationTemplatePath(FrameworkElement? target, out string templatePath)
    {
        templatePath = string.Empty;

        if (GetCurrentTemplateSourceKind() == ImageTemplateSourceKind.Crop)
        {
            if (string.IsNullOrWhiteSpace(_savedCropTemplatePath) || !File.Exists(_savedCropTemplatePath))
            {
                ShowActionTip("请先保存模板，再生成代码", StatusTone.Warning, target, "无法生成代码");
                return false;
            }

            templatePath = _savedCropTemplatePath;
            return true;
        }

        if (string.IsNullOrWhiteSpace(_templateFilePath) || !File.Exists(_templateFilePath))
        {
            ShowActionTip("请选择模板文件", StatusTone.Warning, target, "无法生成代码");
            return false;
        }

        templatePath = _templateFilePath;
        return true;
    }

    private bool TryResolveCodeGenerationRegionContext(
        FrameworkElement? target,
        out ImageMatchRegionContext regionContext)
    {
        if (_lastSuccessfulMatchContext != null &&
            _lastSuccessfulMatchContext.TemplateSourceKind == GetCurrentTemplateSourceKind())
        {
            regionContext = new ImageMatchRegionContext
            {
                ReferenceBounds = _lastSuccessfulMatchContext.ReferenceBounds,
                SearchRegion = _lastSuccessfulMatchContext.SearchRegion,
                RegionRef = _lastSuccessfulMatchContext.RegionRef,
                Orientation = _lastSuccessfulMatchContext.Orientation
            };
            return true;
        }

        if (GetCurrentTemplateSourceKind() == ImageTemplateSourceKind.Crop && _currentCropRegion != null)
        {
            regionContext = CreateRegionContext(_currentCropRegion);
            return true;
        }

        regionContext = default!;
        ShowActionTip("外部模板请先完成一次命中测试，再生成代码", StatusTone.Warning, target, "无法生成代码");
        return false;
    }

    private IReadOnlyList<CodePreviewTemplateItem> GenerateImageModeCodePreviewItems(
        string templatePath,
        ImageMatchRegionContext regionContext,
        double threshold,
        string codeOutputDirectory)
    {
        return
        [
            new CodePreviewTemplateItem(
                "封装版",
                "已经放好 helper 文件时，直接复制这段最省事。",
                GenerateImageModeCodeByTemplate(
                    ImageCodeTemplateKind.ReferenceSingleFile,
                    templatePath,
                    regionContext,
                    threshold,
                    codeOutputDirectory),
                ImageCodeTemplateKind.ReferenceSingleFile,
                ReferenceSingleFileGistUrl),
            new CodePreviewTemplateItem(
                "图像匹配",
                "先用最直接的图像匹配，快速验证能不能点中。",
                GenerateImageModeCodeByTemplate(
                    ImageCodeTemplateKind.MatchTemplateNative,
                    templatePath,
                    regionContext,
                    threshold,
                    codeOutputDirectory),
                ImageCodeTemplateKind.MatchTemplateNative),
            new CodePreviewTemplateItem(
                "特征匹配",
                "模板会缩放或轻微变形时，再切到这一种。",
                GenerateImageModeCodeByTemplate(
                    ImageCodeTemplateKind.MatchFeatureNative,
                    templatePath,
                    regionContext,
                    threshold,
                    codeOutputDirectory),
                ImageCodeTemplateKind.MatchFeatureNative)
        ];
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
