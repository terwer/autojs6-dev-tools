using System;
using App.Models;
using Core.Helpers;

namespace App.Views;

public sealed partial class MainPage
{
    private string GenerateImageModeCodeByTemplate(
        ImageCodeTemplateKind kind,
        string templatePath,
        ImageMatchRegionContext regionContext,
        double threshold,
        string codeOutputDirectory)
    {
        return kind switch
        {
            ImageCodeTemplateKind.ReferenceSingleFile => GenerateReferenceSingleFileCode(templatePath, regionContext, threshold, codeOutputDirectory),
            ImageCodeTemplateKind.MatchTemplateNative => GenerateNativeMatchTemplateCode(templatePath, regionContext, threshold, codeOutputDirectory),
            ImageCodeTemplateKind.MatchFeatureNative => GenerateNativeMatchFeatureCode(templatePath, regionContext, codeOutputDirectory),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private string GenerateReferenceSingleFileCode(
        string templatePath,
        ImageMatchRegionContext regionContext,
        double threshold,
        string codeOutputDirectory)
    {
        _ = LoadReferenceSingleFileTemplateSource();

        var templateReferencePath = BuildGeneratedTemplateReferencePath(templatePath, codeOutputDirectory);
        var acceptThreshold = FormatJsNumber(ClampGeneratedThreshold(threshold));
        var matchThreshold = FormatJsNumber(0.25);
        var regionRefText = ToJsArray(regionContext.RegionRef);

        return NormalizeGeneratedCode(
            $$"""
const matchReferenceTemplate = require("./autojs6-image-match-helper.js")
const screen = captureScreen()
var result = matchReferenceTemplate(screen, "{{templateReferencePath}}", {
  orientation: "{{regionContext.Orientation}}",
  regionRef: {{regionRefText}},
  matchThreshold: {{matchThreshold}},
  acceptThreshold: {{acceptThreshold}},
  useTransparentMask: false
})
if (result && result.found) {
  click(result.clickX, result.clickY)
}
""");
    }
}
