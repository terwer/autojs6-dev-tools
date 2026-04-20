using System;
using System.IO;
using System.Text;
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
        var helperSource = LoadReferenceSingleFileTemplateSource();
        var templateName = Path.GetFileNameWithoutExtension(templatePath);
        var templateFileName = NormalizeJsPath(Path.GetFileName(templatePath));
        var orientation = GetGeneratedOrientation(cropRegion);
        var acceptThreshold = FormatJsNumber(ClampGeneratedThreshold(threshold));
        var matchThreshold = FormatJsNumber(0.25);
        var regionRefText = ToJsArray(regionRef);

        var builder = new StringBuilder();
        builder.AppendLine(helperSource.TrimEnd());
        builder.AppendLine();
        builder.Append(
            $$"""
// ------------------------------
// 当前模板调用示例
// GitHub Gist: {{ReferenceSingleFileGistUrl}}
// 模板: {{templateFileName}}
// 原始区域: [{{cropRegion.X}}, {{cropRegion.Y}}, {{cropRegion.Width}}, {{cropRegion.Height}}]
// regionRef: {{regionRefText}}
// 模板默认与当前脚本放在同一目录；若你改成 assets/，请同步调整 templatePath。
// ------------------------------
var templatePath = files.path("./{{templateFileName}}");
var preferLandscape = {{ToJsBoolean(orientation == "landscape")}};
var acceptThreshold = {{acceptThreshold}};
var matchThreshold = {{matchThreshold}};
var regionRef = {{regionRefText}};

if (!requestScreenCapture(preferLandscape)) {
  throw new Error("请求截图权限失败");
}

var screen = captureScreen();
try {
  var detection = matchReferenceTemplate(screen, templatePath, {
    name: "{{EscapeJavaScriptString(templateName)}}",
    orientation: "{{orientation}}",
    regionRef: regionRef,
    matchThreshold: matchThreshold,
    acceptThreshold: acceptThreshold,
    threshold: acceptThreshold,
    max: 1,
    useTransparentMask: true,
    enableMatchFeatures: true,
    ignoreImmersiveSafeArea: true
  });

  if (detection.found) {
    console.log("[reference-single] matched");
    console.log("point=(" + detection.point.x + ", " + detection.point.y + ")");
    console.log("click=(" + detection.clickX + ", " + detection.clickY + ")");
    console.log("similarity=" + detection.similarityText);
    click(detection.clickX, detection.clickY);
  } else {
    console.log("[reference-single] not matched");
    console.log("best similarity=" + detection.similarityText);
  }
} finally {
  if (screen && typeof screen.recycle === "function") {
    screen.recycle();
  }
}
""");

        return NormalizeGeneratedCode(builder.ToString());
    }

}
