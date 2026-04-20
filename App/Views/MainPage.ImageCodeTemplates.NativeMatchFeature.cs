using System.IO;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private string GenerateNativeMatchFeatureCode(string templatePath, int[] regionRef, CropRegion cropRegion, double threshold)
    {
        var templateReferencePath = BuildGeneratedTemplateReferencePath(templatePath);
        var searchRegionText = BuildUsageSearchRegionText(cropRegion);

        return NormalizeGeneratedCode(
            $$"""
const screen = captureScreen()
const template = images.read("{{templateReferencePath}}")
var frame = images.matchFeatures(
  images.detectAndComputeFeatures(screen, { region: {{searchRegionText}} }),
  images.detectAndComputeFeatures(template)
)
if (frame) {
  click(Math.round(frame.centerX), Math.round(frame.centerY))
}
""");
    }
}
