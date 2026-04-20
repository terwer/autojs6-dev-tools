/**
 * @auther terwer
 * @date 2026/4/21 17:30
 * @description AutoJS6 Image Matching Helper
 *
 * GitHub Gist: https://gist.github.com/terwer/74f37cb8b0bd47d3c74c5767434b0b6b
 *
 * AutoJS6 image match helper for matching a reference template image within a screen image, with support for orientation, scaling, and region mapping.
 * - matchReferenceTemplate(screenImage, template, options)
 * - regionRef reference region mapping [x, y, width, height] in reference template coordinates
 * - scaleCandidates finite scale candidates (retry with proportional scaling)
 * - matchFeatures native matchFeatures fallback
 *
 * parameters：
 * - orientation / regionRef / matchThreshold / acceptThreshold / threshold / max
 * - useTransparentMask / enableMatchFeatures / ignoreImmersiveSafeArea / name
 */
function matchReferenceTemplate(screenImage, template, options) {
  options = options || {};

  var detectionName = options.name || (typeof template === "string" ? template : "[ImageWrapper]");
  var orientation = normalizeReferenceOrientation(options.orientation);
  var refWidth = orientation === "portrait" ? 720 : 1280;
  var refHeight = orientation === "portrait" ? 1280 : 720;
  var matchThreshold = typeof options.matchThreshold === "number"
    ? options.matchThreshold
    : (typeof options.threshold === "number" ? options.threshold : 0.25);
  var acceptThreshold = typeof options.acceptThreshold === "number"
    ? options.acceptThreshold
    : (typeof options.threshold === "number" ? options.threshold : 0.80);

  var best = createEmptyDetection(detectionName, orientation);
  var templateImage = null;
  var ownTemplate = false;
  var screenCandidates = [];

  if (!screenImage) {
    return best;
  }

  try {
    if (typeof template === "string") {
      templateImage = images.read(template);
      ownTemplate = true;
    } else {
      templateImage = template;
    }

    if (!templateImage) {
      return best;
    }

    screenCandidates = buildReferenceScreenCandidates(screenImage, orientation);

    for (var candidateIndex = 0; candidateIndex < screenCandidates.length; candidateIndex++) {
      var candidate = screenCandidates[candidateIndex];
      var candidateWidth = candidate.image.getWidth();
      var candidateHeight = candidate.image.getHeight();
      if (candidateWidth <= 0 || candidateHeight <= 0) {
        continue;
      }

      var widthRatio = candidateWidth / refWidth;
      var heightRatio = candidateHeight / refHeight;
      var scaleCandidates = buildReferenceScaleCandidates(widthRatio, heightRatio);
      var regions = Array.isArray(options.regionRef) && options.regionRef.length === 4
        ? buildReferenceRegions(options.regionRef, widthRatio, heightRatio, candidateWidth, candidateHeight, refWidth, refHeight)
        : [{ mode: "full", rect: [0, 0, candidateWidth, candidateHeight] }];

      for (var regionIndex = 0; regionIndex < regions.length; regionIndex++) {
        var regionInfo = regions[regionIndex];
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
              best = createDetectionResult({
                name: detectionName,
                orientation: orientation,
                regionMode: candidate.mode + ":" + regionInfo.mode,
                regionRect: regionInfo.rect,
                point: match.point,
                similarity: match.similarity,
                scale: scale,
                templateWidth: templateWidth,
                templateHeight: templateHeight,
                acceptThreshold: acceptThreshold
              });
            }
          } finally {
            if (needScaled && scaledTemplate) {
              safeRecycleImage(scaledTemplate);
            }
          }
        }
      }
    }

    if (best.similarity < acceptThreshold) {
      best.found = false;
    }
    if (best.found || !options.enableMatchFeatures) {
      return best;
    }

    var featureDetection = runMatchFeaturesFallback(screenCandidates, templateImage, {
      name: detectionName,
      orientation: orientation,
      regionRef: options.regionRef,
      acceptThreshold: acceptThreshold,
      refWidth: refWidth,
      refHeight: refHeight
    });
    if (featureDetection && featureDetection.found) {
      return featureDetection;
    }

    return best;
  } catch (_) {
    return best;
  } finally {
    if (ownTemplate && templateImage) {
      safeRecycleImage(templateImage);
    }
    for (var recycleIndex = 0; recycleIndex < screenCandidates.length; recycleIndex++) {
      if (screenCandidates[recycleIndex].ownImage) {
        safeRecycleImage(screenCandidates[recycleIndex].image);
      }
    }
  }
}

function createEmptyDetection(name, orientation) {
  return {
    name: name,
    found: false,
    similarity: 0,
    similarityText: "0.000",
    scale: 0,
    scaleText: "N/A",
    orientation: normalizeReferenceOrientation(orientation),
    regionMode: "N/A",
    point: null,
    clickX: 0,
    clickY: 0,
    rect: null,
    regionRect: null,
    acceptThreshold: 0
  };
}

function createDetectionResult(options) {
  options = options || {};

  var name = options.name || "[template]";
  var orientation = normalizeReferenceOrientation(options.orientation);
  var point = options.point || null;
  var similarity = typeof options.similarity === "number" ? options.similarity : 0;
  var scale = typeof options.scale === "number" && options.scale > 0 ? options.scale : 0;
  var templateWidth = Math.max(0, Math.round(options.templateWidth || 0));
  var templateHeight = Math.max(0, Math.round(options.templateHeight || 0));
  var acceptThreshold = typeof options.acceptThreshold === "number" ? options.acceptThreshold : 0;
  var clickX = 0;
  var clickY = 0;
  var rect = null;

  if (point) {
    clickX = typeof options.clickX === "number"
      ? Math.round(options.clickX)
      : Math.round(point.x + (templateWidth > 0 ? templateWidth / 2 : 0));
    clickY = typeof options.clickY === "number"
      ? Math.round(options.clickY)
      : Math.round(point.y + (templateHeight > 0 ? templateHeight / 2 : 0));
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
    regionMode: options.regionMode || "full",
    point: point,
    clickX: clickX,
    clickY: clickY,
    rect: rect,
    regionRect: options.regionRect || null,
    acceptThreshold: acceptThreshold
  };
}

function normalizeReferenceOrientation(orientation) {
  var value = String(orientation || "landscape").toLowerCase();
  return value === "portrait" || value === "vertical" ? "portrait" : "landscape";
}

function buildReferenceScaleCandidates(widthRatio, heightRatio) {
  var candidates = [];

  function pushScale(value) {
    var rounded = Number(value.toFixed(3));
    if (rounded > 0 && candidates.indexOf(rounded) < 0) {
      candidates.push(rounded);
    }
  }

  if (Math.abs(widthRatio - 1) <= 0.02 && Math.abs(heightRatio - 1) <= 0.02) {
    pushScale(1);
    return candidates;
  }

  pushScale(heightRatio);
  pushScale(widthRatio);
  pushScale(1);
  return candidates;
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

function buildReferenceScreenCandidates(screenImage, orientation) {
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

function runMatchFeaturesFallback(screenCandidates, templateImage, options) {
  options = options || {};
  if (!supportsMatchFeaturesApi()) {
    return null;
  }

  var objectFeatures = null;
  try {
    objectFeatures = images.detectAndComputeFeatures(templateImage);
    if (!objectFeatures) {
      return null;
    }

    for (var candidateIndex = 0; candidateIndex < screenCandidates.length; candidateIndex++) {
      var candidate = screenCandidates[candidateIndex];
      var candidateWidth = candidate.image.getWidth();
      var candidateHeight = candidate.image.getHeight();
      if (candidateWidth <= 0 || candidateHeight <= 0) {
        continue;
      }

      var widthRatio = candidateWidth / options.refWidth;
      var heightRatio = candidateHeight / options.refHeight;
      var regions = Array.isArray(options.regionRef) && options.regionRef.length === 4
        ? buildReferenceRegions(options.regionRef, widthRatio, heightRatio, candidateWidth, candidateHeight, options.refWidth, options.refHeight)
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
            options.orientation,
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
  } finally {
    safeRecycleFeatures(objectFeatures);
  }

  return null;
}

function createFeatureDetection(name, orientation, objectFrame, regionRect, scale, acceptThreshold, regionMode) {
  var rect = buildRectFromObjectFrame(objectFrame);
  if (!rect) {
    return createEmptyDetection(name, orientation);
  }

  var clickX = typeof objectFrame.centerX === "number"
    ? Math.round(objectFrame.centerX)
    : Math.round(rect.x + rect.width / 2);
  var clickY = typeof objectFrame.centerY === "number"
    ? Math.round(objectFrame.centerY)
    : Math.round(rect.y + rect.height / 2);

  return createDetectionResult({
    name: name,
    orientation: orientation,
    regionMode: regionMode || "full:feature",
    regionRect: regionRect || null,
    point: { x: rect.x, y: rect.y },
    clickX: clickX,
    clickY: clickY,
    similarity: 1,
    scale: typeof scale === "number" && scale > 0 ? scale : 1,
    templateWidth: rect.width,
    templateHeight: rect.height,
    acceptThreshold: typeof acceptThreshold === "number" ? acceptThreshold : 1
  });
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

if (typeof module !== "undefined" && module.exports) {
  module.exports = matchReferenceTemplate;
}
