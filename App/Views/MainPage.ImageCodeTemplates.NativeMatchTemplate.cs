using Core.Helpers;

namespace App.Views;

public sealed partial class MainPage
{
    private string GenerateNativeMatchTemplateCode(
        string templatePath,
        ImageMatchRegionContext regionContext,
        double threshold,
        string codeOutputDirectory)
    {
        var templateReferencePath = BuildGeneratedTemplateReferencePath(templatePath, codeOutputDirectory);
        var searchRegionText = BuildUsageSearchRegionText(regionContext);
        var matchThreshold = FormatJsNumber(ClampGeneratedThreshold(threshold));

        return NormalizeGeneratedCode(
            $$"""
const screen = captureScreen()
const template = images.read("{{templateReferencePath}}")
var point = images.findImage(screen, template, {
  region: {{searchRegionText}},
  threshold: {{matchThreshold}}
})
if (point) {
  click(
    Math.round(point.x + template.getWidth() / 2),
    Math.round(point.y + template.getHeight() / 2)
  )
}
""");
    }
}
