using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private const string ReferenceSingleFileGistUrl = "https://gist.github.com/terwer/74f37cb8b0bd47d3c74c5767434b0b6b";
    private const string ReferenceSingleFileTemplateRelativePath = "CodeTemplates/image/match-reference-template.single.js";
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

        throw new FileNotFoundException($"未找到单文件模板资源：{ReferenceSingleFileTemplateRelativePath}");
    }

    private static string GetGeneratedOrientation(CropRegion cropRegion)
    {
        return (cropRegion.OriginalWidth ?? 1280) >= (cropRegion.OriginalHeight ?? 720)
            ? "landscape"
            : "portrait";
    }

    private static double ClampGeneratedThreshold(double threshold)
    {
        return Math.Max(0.50, Math.Min(0.95, threshold));
    }

    private static string ToJsBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    private static string ToJsArray(IEnumerable<int> values)
    {
        return $"[{string.Join(", ", values)}]";
    }

    private static string BuildGeneratedTemplateReferencePath(string templatePath)
    {
        return $"./assets/{NormalizeJsPath(Path.GetFileName(templatePath))}";
    }

    private static string NormalizeJsPath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string EscapeJavaScriptString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private static string FormatJsNumber(double value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string NormalizeGeneratedCode(string code)
    {
        return code.Replace("\r\n", "\n").TrimEnd() + "\n";
    }
}
