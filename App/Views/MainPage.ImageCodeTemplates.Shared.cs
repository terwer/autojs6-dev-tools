using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Core.Helpers;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private const string ReferenceSingleFileGistUrl = "https://gist.github.com/terwer/74f37cb8b0bd47d3c74c5767434b0b6b";
    private const string ReferenceSingleFileTemplateRelativePath = "CodeTemplates/image/autojs6-image-match-helper.js";

    private static string LoadReferenceSingleFileTemplateSource()
    {
        var relativePath = ReferenceSingleFileTemplateRelativePath.Replace('/', Path.DirectorySeparatorChar);
        var outputPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (File.Exists(outputPath))
        {
            return File.ReadAllText(outputPath, Encoding.UTF8);
        }

        var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
        if (File.Exists(sourcePath))
        {
            return File.ReadAllText(sourcePath, Encoding.UTF8);
        }

        throw new FileNotFoundException(
            $"未找到单文件模板资源。输出目录路径：{outputPath}；源码目录路径：{sourcePath}");
    }

    private static double ClampGeneratedThreshold(double threshold)
    {
        return Math.Max(0.50, Math.Min(0.95, threshold));
    }

    private static string ToJsArray(IEnumerable<int> values)
    {
        return $"[{string.Join(", ", values)}]";
    }

    private static string BuildGeneratedTemplateReferencePath(string templatePath, string codeOutputDirectory)
    {
        //var relativePath = NormalizeJsPath(Path.GetRelativePath(codeOutputDirectory, templatePath));
        //return relativePath.StartsWith(".") || Path.IsPathRooted(relativePath)
        //    ? relativePath
        //    : $"./{relativePath}";
        var templateName = Path.GetFileName(templatePath);
        return $"./assets/{templateName}";
    }

    private static string BuildUsageSearchRegionText(ImageMatchRegionContext regionContext)
    {
        return ToJsArray(
        [
            regionContext.SearchRegion.X,
            regionContext.SearchRegion.Y,
            regionContext.SearchRegion.Width,
            regionContext.SearchRegion.Height
        ]);
    }

    private static string NormalizeJsPath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string FormatJsNumber(double value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string NormalizeGeneratedCode(string code)
    {
        return code.Replace("\r\n", "\n").TrimEnd() + "\n";
    }

    private static ImageMatchRegionContext CreateRegionContext(CropRegion referenceBounds)
    {
        return ImageMatchRegionCalculator.Create(referenceBounds, MatchRegionPadding);
    }
}
