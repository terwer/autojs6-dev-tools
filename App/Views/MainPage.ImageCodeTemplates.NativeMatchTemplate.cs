using System.IO;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private string GenerateNativeMatchTemplateCode(string templatePath, int[] regionRef, CropRegion cropRegion, double threshold)
    {
        var templateReferencePath = BuildGeneratedTemplateReferencePath(templatePath);
        var searchRegionText = BuildUsageSearchRegionText(cropRegion);
        var matchThreshold = FormatJsNumber(ClampGeneratedThreshold(threshold));

        return NormalizeGeneratedCode(
            $$"""
var screen = captureScreen();
var template = images.read("{{templateReferencePath}}");
var match = images.matchTemplate(screen, template, { threshold: {{matchThreshold}}, region: {{searchRegionText}}, max: 1 });
if (match.matches.length > 0) click(Math.round(match.matches[0].point.x + template.getWidth() / 2), Math.round(match.matches[0].point.y + template.getHeight() / 2));
template.recycle(); screen.recycle();
""");
    }
}
