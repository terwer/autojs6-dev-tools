using Core.Helpers;

namespace App.Views;

public sealed partial class MainPage
{
    private string GenerateNativeMatchFeatureCode(
        string templatePath,
        ImageMatchRegionContext regionContext,
        string codeOutputDirectory)
    {
        var templateReferencePath = BuildGeneratedTemplateReferencePath(templatePath, codeOutputDirectory);
        var searchRegionText = BuildUsageSearchRegionText(regionContext);

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
