using System.IO;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private string GenerateNativeMatchFeatureCode(string templatePath, int[] regionRef, CropRegion cropRegion, double threshold)
    {
        var templateName = Path.GetFileNameWithoutExtension(templatePath);
        var templateFileName = NormalizeJsPath(Path.GetFileName(templatePath));
        var templateReferencePath = BuildGeneratedTemplateReferencePath(templatePath);
        var orientation = GetGeneratedOrientation(cropRegion);
        var acceptThreshold = FormatJsNumber(ClampGeneratedThreshold(threshold));
        var regionRefText = ToJsArray(regionRef);

        return NormalizeGeneratedCode(
            $$"""
// 纯原生 matchFeature 版本
// 模板: {{templateFileName}}
// 原始区域: [{{cropRegion.X}}, {{cropRegion.Y}}, {{cropRegion.Width}}, {{cropRegion.Height}}]
// regionRef: {{regionRefText}}
// 这个版本只使用 AutoJS 原生 detectAndComputeFeatures / matchFeatures。
var templatePath = files.path("{{templateReferencePath}}");
var preferLandscape = {{ToJsBoolean(orientation == "landscape")}};
var orientation = "{{orientation}}";
var regionRef = {{regionRefText}};
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
  var detection = runNativeReferenceMatchFeature(screen, template, {
    name: "{{EscapeJavaScriptString(templateName)}}",
    orientation: orientation,
    regionRef: regionRef,
    acceptThreshold: acceptThreshold
  });

  if (detection.found) {
    console.log("[native-matchFeature] matched");
    console.log("point=(" + detection.point.x + ", " + detection.point.y + ")");
    console.log("click=(" + detection.clickX + ", " + detection.clickY + ")");
    console.log("confidence=" + detection.similarityText);
    click(detection.clickX, detection.clickY);
  } else {
    console.log("[native-matchFeature] not matched");
    console.log("当前环境不支持或未命中特征匹配");
  }
} finally {
  safeRecycleImage(template);
  safeRecycleImage(screen);
}

function runNativeReferenceMatchFeature(screenImage, templateImage, options) {
  options = options || {};
  var normalizedOrientation = normalizeOrientation(options.orientation);
  var result = createFeatureDetection(options.name || "[template]", normalizedOrientation, null, null, 0, options.acceptThreshold);
  if (!supportsMatchFeaturesApi()) {
    return result;
  }

  var refWidth = normalizedOrientation === "portrait" ? 720 : 1280;
  var refHeight = normalizedOrientation === "portrait" ? 1280 : 720;
  var screenCandidates = buildScreenCandidates(screenImage, normalizedOrientation);
  var objectFeatures = null;

  try {
    objectFeatures = images.detectAndComputeFeatures(templateImage);
    if (!objectFeatures) {
      return result;
    }

    for (var candidateIndex = 0; candidateIndex < screenCandidates.length; candidateIndex++) {
      var candidate = screenCandidates[candidateIndex];
      var candidateWidth = candidate.image.getWidth();
      var candidateHeight = candidate.image.getHeight();
      if (candidateWidth <= 0 || candidateHeight <= 0) {
        continue;
      }

      var widthRatio = candidateWidth / refWidth;
      var heightRatio = candidateHeight / refHeight;
      var regions = Array.isArray(options.regionRef) && options.regionRef.length === 4
        ? buildReferenceRegions(options.regionRef, widthRatio, heightRatio, candidateWidth, candidateHeight, refWidth, refHeight)
        : [{ mode: "full", rect: [0, 0, candidateWidth, candidateHeight] }];

      for (var regionIndex = 0; regionIndex < regions.length; regionIndex++) {
        var regionInfo = regions[regionIndex];
        var sceneFeatures = null;
        try {
          sceneFeatures = images.detectAndComputeFeatures(candidate.image, {
            region: regionInfo.rect
          });
          if (!sceneFeatures) {
            continue;
          }

          var objectFrame = images.matchFeatures(sceneFeatures, objectFeatures);
          if (!objectFrame) {
            continue;
          }

          return createFeatureDetection(
            options.name || "[template]",
            normalizedOrientation,
            objectFrame,
            regionInfo.rect,
            typeof sceneFeatures.scale === "number" ? sceneFeatures.scale : 1,
            options.acceptThreshold,
            candidate.mode + ":" + regionInfo.mode + ":feature"
          );
        } finally {
          safeRecycleFeatures(sceneFeatures);
        }
      }
    }
  } catch (_) {
    return result;
  } finally {
    safeRecycleFeatures(objectFeatures);
    for (var recycleIndex = 0; recycleIndex < screenCandidates.length; recycleIndex++) {
      if (screenCandidates[recycleIndex].ownImage) {
        safeRecycleImage(screenCandidates[recycleIndex].image);
      }
    }
  }

  return result;
}

function createFeatureDetection(name, orientation, objectFrame, regionRect, scale, acceptThreshold, regionMode) {
  var rect = buildRectFromObjectFrame(objectFrame);
  if (!rect) {
    return {
      name: name,
      found: false,
      similarity: 0,
      similarityText: "0.000",
      scale: 0,
      scaleText: "N/A",
      orientation: orientation,
      regionMode: regionMode || "full",
      point: null,
      clickX: 0,
      clickY: 0,
      rect: null,
      regionRect: regionRect || null,
      acceptThreshold: typeof acceptThreshold === "number" ? acceptThreshold : 0
    };
  }

  var clickX = typeof objectFrame.centerX === "number"
    ? Math.round(objectFrame.centerX)
    : Math.round(rect.x + rect.width / 2);
  var clickY = typeof objectFrame.centerY === "number"
    ? Math.round(objectFrame.centerY)
    : Math.round(rect.y + rect.height / 2);

  return {
    name: name,
    found: true,
    similarity: 1,
    similarityText: "1.000",
    scale: typeof scale === "number" && scale > 0 ? scale : 1,
    scaleText: typeof scale === "number" && scale > 0 ? scale.toFixed(3) : "1.000",
    orientation: orientation,
    regionMode: regionMode || "full",
    point: { x: rect.x, y: rect.y },
    clickX: clickX,
    clickY: clickY,
    rect: rect,
    regionRect: regionRect || null,
    acceptThreshold: typeof acceptThreshold === "number" ? acceptThreshold : 0
  };
}

function buildRectFromObjectFrame(objectFrame) {
  var points = [
    objectFrame && objectFrame.topLeft,
    objectFrame && objectFrame.topRight,
    objectFrame && objectFrame.bottomLeft,
    objectFrame && objectFrame.bottomRight
  ].filter(function (point) {
    return point && typeof point.x === "number" && typeof point.y === "number";
  });

  if (points.length === 0) {
    return null;
  }

  var xs = points.map(function (point) { return point.x; });
  var ys = points.map(function (point) { return point.y; });
  var minX = Math.round(Math.min.apply(null, xs));
  var minY = Math.round(Math.min.apply(null, ys));
  var maxX = Math.round(Math.max.apply(null, xs));
  var maxY = Math.round(Math.max.apply(null, ys));

  return {
    x: minX,
    y: minY,
    width: Math.max(1, maxX - minX),
    height: Math.max(1, maxY - minY),
    right: maxX,
    bottom: maxY
  };
}

function normalizeOrientation(value) {
  var text = String(value || "landscape").toLowerCase();
  return text === "portrait" || text === "vertical" ? "portrait" : "landscape";
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

function supportsMatchFeaturesApi() {
  return typeof images !== "undefined"
    && typeof images.detectAndComputeFeatures === "function"
    && typeof images.matchFeatures === "function";
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

function safeRecycleFeatures(imageFeatures) {
  if (!imageFeatures || typeof imageFeatures.recycle !== "function") {
    return;
  }
  try {
    if (typeof imageFeatures.isRecycled === "function" && imageFeatures.isRecycled()) {
      return;
    }
    imageFeatures.recycle();
  } catch (_) {}
}
""");
    }

}
