using System.IO;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private string GenerateNativeMatchTemplateCode(string templatePath, int[] regionRef, CropRegion cropRegion, double threshold)
    {
        var templateName = Path.GetFileNameWithoutExtension(templatePath);
        var templateFileName = NormalizeJsPath(Path.GetFileName(templatePath));
        var orientation = GetGeneratedOrientation(cropRegion);
        var acceptThreshold = FormatJsNumber(ClampGeneratedThreshold(threshold));
        var matchThreshold = FormatJsNumber(0.25);
        var regionRefText = ToJsArray(regionRef);

        return NormalizeGeneratedCode(
            $$"""
// 纯原生 matchTemplate 版本
// 模板: {{templateFileName}}
// 原始区域: [{{cropRegion.X}}, {{cropRegion.Y}}, {{cropRegion.Width}}, {{cropRegion.Height}}]
// regionRef: {{regionRefText}}
// 模板默认与当前脚本放在同一目录；若你改成 assets/，请同步修改 templatePath。
var templatePath = files.path("./{{templateFileName}}");
var preferLandscape = {{ToJsBoolean(orientation == "landscape")}};
var orientation = "{{orientation}}";
var regionRef = {{regionRefText}};
var matchThreshold = {{matchThreshold}};
var acceptThreshold = {{acceptThreshold}};

if (!requestScreenCapture(preferLandscape)) {
  throw new Error("请求截图权限失败");
}

var screen = captureScreen();
var template = images.read(templatePath);
if (!template) {
  throw new Error("模板读取失败: " + templatePath);
}

try {
  var detection = runNativeReferenceMatchTemplate(screen, template, {
    name: "{{EscapeJavaScriptString(templateName)}}",
    orientation: orientation,
    regionRef: regionRef,
    matchThreshold: matchThreshold,
    acceptThreshold: acceptThreshold,
    max: 1,
    useTransparentMask: true
  });

  if (detection.found) {
    console.log("[native-matchTemplate] matched");
    console.log("point=(" + detection.point.x + ", " + detection.point.y + ")");
    console.log("click=(" + detection.clickX + ", " + detection.clickY + ")");
    console.log("similarity=" + detection.similarityText);
    click(detection.clickX, detection.clickY);
  } else {
    console.log("[native-matchTemplate] not matched");
    console.log("best similarity=" + detection.similarityText);
  }
} finally {
  safeRecycleImage(template);
  safeRecycleImage(screen);
}

function runNativeReferenceMatchTemplate(screenImage, templateImage, options) {
  options = options || {};
  var normalizedOrientation = normalizeOrientation(options.orientation);
  var refWidth = normalizedOrientation === "portrait" ? 720 : 1280;
  var refHeight = normalizedOrientation === "portrait" ? 1280 : 720;
  var matchThreshold = typeof options.matchThreshold === "number"
    ? options.matchThreshold
    : (typeof options.threshold === "number" ? options.threshold : 0.25);
  var acceptThreshold = typeof options.acceptThreshold === "number"
    ? options.acceptThreshold
    : (typeof options.threshold === "number" ? options.threshold : 0.80);
  var best = createNativeMatchResult(options.name || "[template]", normalizedOrientation);
  var screenCandidates = buildScreenCandidates(screenImage, normalizedOrientation);

  try {
    for (var candidateIndex = 0; candidateIndex < screenCandidates.length; candidateIndex++) {
      var candidate = screenCandidates[candidateIndex];
      var candidateWidth = candidate.image.getWidth();
      var candidateHeight = candidate.image.getHeight();
      if (candidateWidth <= 0 || candidateHeight <= 0) {
        continue;
      }

      var widthRatio = candidateWidth / refWidth;
      var heightRatio = candidateHeight / refHeight;
      var scaleCandidates = buildScaleCandidates(widthRatio, heightRatio);
      var baseRegions = Array.isArray(options.regionRef) && options.regionRef.length === 4
        ? buildReferenceRegions(options.regionRef, widthRatio, heightRatio, candidateWidth, candidateHeight, refWidth, refHeight)
        : [{ mode: "full", rect: [0, 0, candidateWidth, candidateHeight] }];

      for (var regionIndex = 0; regionIndex < baseRegions.length; regionIndex++) {
        var regionInfo = baseRegions[regionIndex];
        for (var scaleIndex = 0; scaleIndex < scaleCandidates.length; scaleIndex++) {
          var scale = scaleCandidates[scaleIndex];
          var scaledTemplate = templateImage;
          var needScaled = Math.abs(scale - 1) > 0.001;

          try {
            if (needScaled) {
              var targetWidth = Math.max(1, Math.round(templateImage.getWidth() * scale));
              var targetHeight = Math.max(1, Math.round(templateImage.getHeight() * scale));
              scaledTemplate = images.resize(templateImage, [targetWidth, targetHeight], "LINEAR");
            }

            var templateWidth = scaledTemplate.getWidth();
            var templateHeight = scaledTemplate.getHeight();
            if (templateWidth <= 0 || templateHeight <= 0) {
              continue;
            }

            if (!canUseRegion(regionInfo.rect, candidateWidth, candidateHeight, templateWidth, templateHeight)) {
              continue;
            }

            var matchOptions = {
              threshold: matchThreshold,
              max: typeof options.max === "number" ? options.max : 1,
              region: regionInfo.rect
            };
            if (options.useTransparentMask === true) {
              matchOptions.useTransparentMask = true;
            }

            var result = images.matchTemplate(candidate.image, scaledTemplate, matchOptions);
            if (!result || !result.matches || result.matches.length === 0) {
              continue;
            }

            var match = result.matches[0];
            if (match && match.similarity > best.similarity) {
              best = createNativeMatchResult(
                options.name || "[template]",
                normalizedOrientation,
                match.point,
                match.similarity,
                scale,
                templateWidth,
                templateHeight,
                candidate.mode + ":" + regionInfo.mode,
                regionInfo.rect,
                acceptThreshold
              );
            }
          } finally {
            if (needScaled && scaledTemplate) {
              safeRecycleImage(scaledTemplate);
            }
          }
        }
      }
    }
  } finally {
    for (var recycleIndex = 0; recycleIndex < screenCandidates.length; recycleIndex++) {
      if (screenCandidates[recycleIndex].ownImage) {
        safeRecycleImage(screenCandidates[recycleIndex].image);
      }
    }
  }

  if (best.similarity < acceptThreshold) {
    best.found = false;
  }
  return best;
}

function createNativeMatchResult(name, orientation, point, similarity, scale, templateWidth, templateHeight, regionMode, regionRect, acceptThreshold) {
  point = point || null;
  similarity = typeof similarity === "number" ? similarity : 0;
  scale = typeof scale === "number" && scale > 0 ? scale : 0;
  templateWidth = Math.max(0, Math.round(templateWidth || 0));
  templateHeight = Math.max(0, Math.round(templateHeight || 0));
  acceptThreshold = typeof acceptThreshold === "number" ? acceptThreshold : 0;

  var clickX = 0;
  var clickY = 0;
  var rect = null;
  if (point) {
    clickX = Math.round(point.x + (templateWidth > 0 ? templateWidth / 2 : 0));
    clickY = Math.round(point.y + (templateHeight > 0 ? templateHeight / 2 : 0));
    rect = {
      x: Math.round(point.x),
      y: Math.round(point.y),
      width: templateWidth,
      height: templateHeight,
      right: Math.round(point.x + templateWidth),
      bottom: Math.round(point.y + templateHeight)
    };
  }

  return {
    name: name,
    found: !!point && similarity >= acceptThreshold,
    similarity: similarity,
    similarityText: similarity.toFixed(3),
    scale: scale,
    scaleText: scale > 0 ? scale.toFixed(3) : "N/A",
    orientation: orientation,
    regionMode: regionMode || "full",
    point: point,
    clickX: clickX,
    clickY: clickY,
    rect: rect,
    regionRect: regionRect || null,
    acceptThreshold: acceptThreshold
  };
}

function normalizeOrientation(value) {
  var text = String(value || "landscape").toLowerCase();
  return text === "portrait" || text === "vertical" ? "portrait" : "landscape";
}

function buildScaleCandidates(widthRatio, heightRatio) {
  var values = [];

  function pushScale(scale) {
    var rounded = Number(scale.toFixed(3));
    if (rounded > 0 && values.indexOf(rounded) < 0) {
      values.push(rounded);
    }
  }

  if (Math.abs(widthRatio - 1) <= 0.02 && Math.abs(heightRatio - 1) <= 0.02) {
    pushScale(1);
    return values;
  }

  pushScale(heightRatio);
  pushScale(widthRatio);
  pushScale(1);
  return values;
}

function buildReferenceRegions(regionRef, widthRatio, heightRatio, screenWidth, screenHeight, refWidth, refHeight) {
  var regions = [];

  function pushRegion(mode, x, y, width, height) {
    var rect = clampRegionRect([x, y, width, height], screenWidth, screenHeight);
    if (!rect) {
      return;
    }

    var key = mode + ":" + rect.join(",");
    for (var i = 0; i < regions.length; i++) {
      if (regions[i].mode + ":" + regions[i].rect.join(",") === key) {
        return;
      }
    }

    regions.push({ mode: mode, rect: rect });
  }

  pushRegion(
    "stretch",
    regionRef[0] * widthRatio,
    regionRef[1] * heightRatio,
    regionRef[2] * widthRatio,
    regionRef[3] * heightRatio
  );

  var fitHeightWidth = refWidth * heightRatio;
  var offsetX = (screenWidth - fitHeightWidth) / 2;
  pushRegion(
    "fitHeight",
    offsetX + regionRef[0] * heightRatio,
    regionRef[1] * heightRatio,
    regionRef[2] * heightRatio,
    regionRef[3] * heightRatio
  );

  var fitWidthHeight = refHeight * widthRatio;
  var offsetY = (screenHeight - fitWidthHeight) / 2;
  pushRegion(
    "fitWidth",
    regionRef[0] * widthRatio,
    offsetY + regionRef[1] * widthRatio,
    regionRef[2] * widthRatio,
    regionRef[3] * widthRatio
  );

  return regions;
}

function buildScreenCandidates(screenImage, orientation) {
  var candidates = [{ image: screenImage, ownImage: false, mode: "raw" }];
  var screenWidth = screenImage.getWidth();
  var screenHeight = screenImage.getHeight();

  if (orientation === "landscape" && screenWidth < screenHeight) {
    try {
      candidates.push({ image: images.rotate(screenImage, 90), ownImage: true, mode: "rot90" });
    } catch (_) {}
    try {
      candidates.push({ image: images.rotate(screenImage, 270), ownImage: true, mode: "rot270" });
    } catch (_) {}
  }

  if (orientation === "portrait" && screenWidth > screenHeight) {
    try {
      candidates.push({ image: images.rotate(screenImage, 90), ownImage: true, mode: "rot90" });
    } catch (_) {}
    try {
      candidates.push({ image: images.rotate(screenImage, 270), ownImage: true, mode: "rot270" });
    } catch (_) {}
  }

  return candidates;
}

function canUseRegion(regionRect, screenWidth, screenHeight, templateWidth, templateHeight) {
  if (!regionRect || regionRect.length !== 4) {
    return false;
  }

  var x = regionRect[0];
  var y = regionRect[1];
  var width = regionRect[2];
  var height = regionRect[3];
  if (x < 0 || y < 0 || width <= 0 || height <= 0) {
    return false;
  }
  if (x + width > screenWidth || y + height > screenHeight) {
    return false;
  }
  return templateWidth <= width && templateHeight <= height;
}

function clampRegionRect(rect, screenWidth, screenHeight) {
  var x = Math.max(0, Math.round(rect[0] || 0));
  var y = Math.max(0, Math.round(rect[1] || 0));
  var width = Math.max(0, Math.round(rect[2] || 0));
  var height = Math.max(0, Math.round(rect[3] || 0));
  if (width <= 0 || height <= 0 || x >= screenWidth || y >= screenHeight) {
    return null;
  }
  if (x + width > screenWidth) {
    width = Math.max(0, screenWidth - x);
  }
  if (y + height > screenHeight) {
    height = Math.max(0, screenHeight - y);
  }
  if (width <= 0 || height <= 0) {
    return null;
  }
  return [x, y, width, height];
}

function safeRecycleImage(image) {
  if (!image || typeof image.recycle !== "function") {
    return;
  }
  try {
    if (typeof image.isRecycled === "function" && image.isRecycled()) {
      return;
    }
    image.recycle();
  } catch (_) {}
}
""");
    }

}
