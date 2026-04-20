using System;
using System.IO;
using App.Models;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private string GenerateImageModeCodeByTemplate(
        ImageCodeTemplateKind kind,
        string templatePath,
        int[] regionRef,
        CropRegion cropRegion,
        double threshold)
    {
        return kind switch
        {
            ImageCodeTemplateKind.ReferenceSingleFile => GenerateReferenceSingleFileCode(templatePath, regionRef, cropRegion, threshold),
            ImageCodeTemplateKind.MatchTemplateNative => GenerateNativeMatchTemplateCode(templatePath, regionRef, cropRegion, threshold),
            ImageCodeTemplateKind.MatchFeatureNative => GenerateNativeMatchFeatureCode(templatePath, regionRef, cropRegion, threshold),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private string GenerateReferenceSingleFileCode(string templatePath, int[] regionRef, CropRegion cropRegion, double threshold)
    {
        _ = LoadReferenceSingleFileTemplateSource();

        var templateReferencePath = BuildGeneratedTemplateReferencePath(templatePath);
        var orientation = GetGeneratedOrientation(cropRegion);
        var acceptThreshold = FormatJsNumber(ClampGeneratedThreshold(threshold));
        var matchThreshold = FormatJsNumber(0.25);
        var regionRefText = ToJsArray(regionRef);

        return NormalizeGeneratedCode(
            $$"""
var matchReferenceTemplate = require("./autojs6-image-match-helper.js");
var screen = captureScreen();
var result = matchReferenceTemplate(screen, "{{templateReferencePath}}", { orientation: "{{orientation}}", regionRef: {{regionRefText}}, matchThreshold: {{matchThreshold}}, acceptThreshold: {{acceptThreshold}}, useTransparentMask: true, enableMatchFeatures: true });
if (result.found) click(result.clickX, result.clickY);
screen.recycle();
""");
    }
}
