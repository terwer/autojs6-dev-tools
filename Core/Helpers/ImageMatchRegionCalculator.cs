using System;
using Core.Models;

namespace Core.Helpers;

/// <summary>
/// 图像匹配区域上下文。
/// </summary>
public sealed class ImageMatchRegionContext
{
    /// <summary>
    /// 参考命中框或裁剪框。
    /// </summary>
    public required CropRegion ReferenceBounds { get; init; }

    /// <summary>
    /// 实际搜索区域。
    /// </summary>
    public required CropRegion SearchRegion { get; init; }

    /// <summary>
    /// 归一化后的 regionRef。
    /// </summary>
    public required int[] RegionRef { get; init; }

    /// <summary>
    /// 方向约定。
    /// </summary>
    public required string Orientation { get; init; }
}

/// <summary>
/// 图像匹配区域计算器。
/// </summary>
public static class ImageMatchRegionCalculator
{
    /// <summary>
    /// 基于参考矩形构建代码生成与匹配所需上下文。
    /// </summary>
    public static ImageMatchRegionContext Create(CropRegion referenceBounds, int padding)
    {
        var screenWidth = Math.Max(
            1,
            referenceBounds.OriginalWidth ?? (referenceBounds.X + referenceBounds.Width));
        var screenHeight = Math.Max(
            1,
            referenceBounds.OriginalHeight ?? (referenceBounds.Y + referenceBounds.Height));

        var safeReferenceBounds = new CropRegion
        {
            X = Math.Clamp(referenceBounds.X, 0, Math.Max(0, screenWidth - 1)),
            Y = Math.Clamp(referenceBounds.Y, 0, Math.Max(0, screenHeight - 1)),
            Width = Math.Clamp(referenceBounds.Width, 1, screenWidth),
            Height = Math.Clamp(referenceBounds.Height, 1, screenHeight),
            OriginalWidth = screenWidth,
            OriginalHeight = screenHeight
        };

        var searchX = Math.Max(0, safeReferenceBounds.X - padding);
        var searchY = Math.Max(0, safeReferenceBounds.Y - padding);
        var searchRight = Math.Min(
            screenWidth,
            safeReferenceBounds.X + safeReferenceBounds.Width + padding);
        var searchBottom = Math.Min(
            screenHeight,
            safeReferenceBounds.Y + safeReferenceBounds.Height + padding);

        var searchRegion = new CropRegion
        {
            X = searchX,
            Y = searchY,
            Width = Math.Max(1, searchRight - searchX),
            Height = Math.Max(1, searchBottom - searchY),
            OriginalWidth = screenWidth,
            OriginalHeight = screenHeight
        };

        var isLandscape = screenWidth >= screenHeight;
        var referenceWidth = isLandscape ? 1280 : 720;
        var referenceHeight = isLandscape ? 720 : 1280;
        var widthRatio = (double)referenceWidth / screenWidth;
        var heightRatio = (double)referenceHeight / screenHeight;

        return new ImageMatchRegionContext
        {
            ReferenceBounds = safeReferenceBounds,
            SearchRegion = searchRegion,
            Orientation = isLandscape ? "landscape" : "portrait",
            RegionRef =
            [
                (int)Math.Round(searchRegion.X * widthRatio),
                (int)Math.Round(searchRegion.Y * heightRatio),
                (int)Math.Round(searchRegion.Width * widthRatio),
                (int)Math.Round(searchRegion.Height * heightRatio)
            ]
        };
    }
}
