using System.Text;
using Core.Abstractions;
using Core.Models;

namespace Core.Services;

/// <summary>
/// AutoJS6 代码生成器实现
/// 严格遵循 PHASE0_REFERENCE.md 的 API 约束
/// </summary>
public class AutoJS6CodeGenerator : ICodeGenerator
{
    public string GenerateImageModeCode(AutoJS6CodeOptions options)
    {
        var sb = new StringBuilder();

        // 生成图像模式代码
        sb.AppendLine("// 图像模式：使用 images.findImage 进行模板匹配");
        sb.AppendLine();

        // 请求截图权限
        sb.AppendLine("// 请求截图权限");
        sb.AppendLine("if (!requestScreenCapture()) {");
        sb.AppendLine("    toast(\"请求截图权限失败\");");
        sb.AppendLine("    exit();");
        sb.AppendLine("}");
        sb.AppendLine();

        // 加载模板
        sb.AppendLine("// 加载模板图像");
        sb.AppendLine($"var {options.VariablePrefix}Template = images.read(\"{options.TemplatePath}\");");
        sb.AppendLine($"if (!{options.VariablePrefix}Template) {{");
        sb.AppendLine("    toast(\"模板图像加载失败\");");
        sb.AppendLine("    exit();");
        sb.AppendLine("}");
        sb.AppendLine();

        if (options.GenerateRetryLogic)
        {
            // 生成重试机制
            sb.AppendLine("// 重试机制");
            sb.AppendLine($"var {options.VariablePrefix}Found = false;");
            sb.AppendLine($"for (var i = 0; i < {options.RetryCount}; i++) {{");
            sb.AppendLine("    var screen = captureScreen();");
            sb.AppendLine("    if (!screen) {");
            sb.AppendLine("        sleep(1000);");
            sb.AppendLine("        continue;");
            sb.AppendLine("    }");
            sb.AppendLine();

            // 生成匹配代码
            GenerateMatchCode(sb, options, "    ");

            sb.AppendLine("    if (result) {");
            sb.AppendLine($"        {options.VariablePrefix}Found = true;");
            sb.AppendLine("        break;");
            sb.AppendLine("    }");
            sb.AppendLine("    sleep(1000);");
            sb.AppendLine("}");
            sb.AppendLine();

            // 回收模板图像
            if (options.GenerateImageRecycle)
            {
                sb.AppendLine("// 回收模板图像");
                sb.AppendLine($"{options.VariablePrefix}Template.recycle();");
                sb.AppendLine();
            }

            sb.AppendLine($"if (!{options.VariablePrefix}Found) {{");
            sb.AppendLine("    toast(\"未找到目标\");");
            sb.AppendLine("    exit();");
            sb.AppendLine("}");
        }
        else
        {
            // 单次匹配
            sb.AppendLine("// 截图并匹配");
            sb.AppendLine("var screen = captureScreen();");
            sb.AppendLine("if (!screen) {");
            sb.AppendLine("    toast(\"截图失败\");");
            sb.AppendLine("    exit();");
            sb.AppendLine("}");
            sb.AppendLine();

            GenerateMatchCode(sb, options, "");

            // 回收模板图像
            if (options.GenerateImageRecycle)
            {
                sb.AppendLine($"{options.VariablePrefix}Template.recycle();");
                sb.AppendLine();
            }

            sb.AppendLine("if (!result) {");
            sb.AppendLine("    toast(\"未找到目标\");");
            sb.AppendLine("    exit();");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public string GenerateWidgetModeCode(AutoJS6CodeOptions options)
    {
        if (options.Widget == null)
        {
            throw new ArgumentException("Widget mode requires a WidgetNode");
        }

        var sb = new StringBuilder();

        sb.AppendLine("// 控件模式：使用 UiSelector 进行控件查找");
        sb.AppendLine();

        var primarySelector = BuildPrimaryWidgetSelector(options.Widget);
        var fallbackSelectors = BuildFallbackWidgetSelectors(options.Widget);

        if (options.GenerateRetryLogic)
        {
            sb.AppendLine("// 重试机制");
            sb.AppendLine($"var {options.VariablePrefix} = null;");
            sb.AppendLine($"for (var i = 0; i < {options.RetryCount}; i++) {{");
            sb.AppendLine($"    {options.VariablePrefix} = {primarySelector};");
            foreach (var fallbackSelector in fallbackSelectors)
            {
                sb.AppendLine($"    if (!{options.VariablePrefix}) {{");
                sb.AppendLine($"        {options.VariablePrefix} = {fallbackSelector};");
                sb.AppendLine("    }");
            }
            sb.AppendLine($"    if ({options.VariablePrefix}) {{");
            sb.AppendLine("        break;");
            sb.AppendLine("    }");
            sb.AppendLine("    sleep(1000);");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"if (!{options.VariablePrefix}) {{");
            sb.AppendLine("    toast(\"未找到控件\");");
            sb.AppendLine("    exit();");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("// 点击控件");
            sb.AppendLine($"{options.VariablePrefix}.click();");
        }
        else
        {
            sb.AppendLine($"var {options.VariablePrefix} = {primarySelector};");
            foreach (var fallbackSelector in fallbackSelectors)
            {
                sb.AppendLine($"if (!{options.VariablePrefix}) {{");
                sb.AppendLine($"    {options.VariablePrefix} = {fallbackSelector};");
                sb.AppendLine("}");
            }
            sb.AppendLine($"if ({options.VariablePrefix}) {{");
            sb.AppendLine($"    {options.VariablePrefix}.click();");
            sb.AppendLine("} else {");
            sb.AppendLine("    toast(\"未找到控件\");");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public string GenerateFullScript(AutoJS6CodeOptions options)
    {
        var sb = new StringBuilder();

        // 脚本头部
        sb.AppendLine("\"ui\";");
        sb.AppendLine();
        sb.AppendLine("// AutoJS6 自动生成脚本");
        sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"// 模式: {options.Mode}");
        sb.AppendLine();

        // 生成主要代码
        if (options.Mode == CodeGenerationMode.Image)
        {
            sb.Append(GenerateImageModeCode(options));
        }
        else
        {
            sb.Append(GenerateWidgetModeCode(options));
        }

        return sb.ToString();
    }

    public string FormatCode(string code, int indentSize = 4)
    {
        // 简单的代码格式化
        var lines = code.Split('\n');
        var sb = new StringBuilder();
        int indentLevel = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("}"))
            {
                indentLevel--;
            }

            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                sb.Append(new string(' ', indentLevel * indentSize));
                sb.AppendLine(trimmed);
            }
            else
            {
                sb.AppendLine();
            }

            if (trimmed.EndsWith("{"))
            {
                indentLevel++;
            }
        }

        return sb.ToString();
    }

    public (bool IsValid, List<string> Errors) ValidateCode(string code)
    {
        var errors = new List<string>();

        // 检查 Rhino 引擎约束：循环体内禁止 const/let
        var lines = code.Split('\n');
        bool inLoop = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // 检测循环开始
            if (line.Contains("for") || line.Contains("while"))
            {
                inLoop = true;
            }

            // 检测循环结束
            if (inLoop && line.Contains("}"))
            {
                inLoop = false;
            }

            // 检查循环体内的 const/let
            if (inLoop && (line.Contains("const ") || line.Contains("let ")))
            {
                errors.Add($"第 {i + 1} 行: Rhino 引擎禁止在循环体内使用 const/let，请使用 var");
            }
        }

        return (errors.Count == 0, errors);
    }

    private void GenerateMatchCode(StringBuilder sb, AutoJS6CodeOptions options, string indent)
    {
        sb.AppendLine($"{indent}// 执行模板匹配");

        if (options.Region != null)
        {
            var r = options.Region;
            sb.AppendLine($"{indent}var result = images.findImage(screen, {options.VariablePrefix}Template, {{");
            sb.AppendLine($"{indent}    threshold: {options.Threshold:F2},");
            sb.AppendLine($"{indent}    region: [{r.X}, {r.Y}, {r.Width}, {r.Height}]");
            sb.AppendLine($"{indent}}});");
        }
        else
        {
            sb.AppendLine($"{indent}var result = images.findImage(screen, {options.VariablePrefix}Template, {{");
            sb.AppendLine($"{indent}    threshold: {options.Threshold:F2}");
            sb.AppendLine($"{indent}}});");
        }

        sb.AppendLine();
        sb.AppendLine($"{indent}if (result) {{");
        sb.AppendLine($"{indent}    // 计算点击坐标（中心点）");
        sb.AppendLine($"{indent}    var clickX = result.x + {options.VariablePrefix}Template.width / 2;");
        sb.AppendLine($"{indent}    var clickY = result.y + {options.VariablePrefix}Template.height / 2;");
        sb.AppendLine($"{indent}    click(clickX, clickY);");
        sb.AppendLine($"{indent}    toast(\"已点击目标\");");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    private static string BuildPrimaryWidgetSelector(WidgetNode widget)
    {
        if (!string.IsNullOrWhiteSpace(widget.ResourceId))
        {
            return BuildSelectorWithOptionalBounds($"id(\"{widget.ResourceId}\")", widget);
        }

        if (!string.IsNullOrWhiteSpace(widget.Text))
        {
            return BuildSelectorWithOptionalBounds($"text(\"{EscapeJavaScript(widget.Text)}\")", widget);
        }

        if (!string.IsNullOrWhiteSpace(widget.ContentDesc))
        {
            return BuildSelectorWithOptionalBounds($"desc(\"{EscapeJavaScript(widget.ContentDesc)}\")", widget);
        }

        var selectorBase = !string.IsNullOrWhiteSpace(widget.ClassName)
            ? $"className(\"{widget.ClassName}\")"
            : "selector()";

        return BuildSelectorWithOptionalBounds(selectorBase, widget);
    }

    private static IEnumerable<string> BuildFallbackWidgetSelectors(WidgetNode widget)
    {
        var selectors = new List<string>();

        if (!string.IsNullOrWhiteSpace(widget.ResourceId) && !string.IsNullOrWhiteSpace(widget.Text))
        {
            selectors.Add(BuildSelectorWithOptionalBounds($"text(\"{EscapeJavaScript(widget.Text)}\")", widget));
        }

        if (!string.IsNullOrWhiteSpace(widget.ContentDesc))
        {
            selectors.Add(BuildSelectorWithOptionalBounds($"desc(\"{EscapeJavaScript(widget.ContentDesc)}\")", widget));
        }

        if (selectors.Count == 0 && !string.IsNullOrWhiteSpace(widget.Text))
        {
            selectors.Add(BuildSelectorWithOptionalBounds($"text(\"{EscapeJavaScript(widget.Text)}\")", widget));
        }

        return selectors.Distinct(StringComparer.Ordinal);
    }

    private static string BuildSelectorWithOptionalBounds(string selectorBase, WidgetNode widget)
    {
        if (widget.BoundsRect.Width > 0 && widget.BoundsRect.Height > 0)
        {
            var (x, y, width, height) = widget.BoundsRect;
            return $"{selectorBase}.boundsInside({x}, {y}, {x + width}, {y + height}).findOne()";
        }

        return $"{selectorBase}.findOne()";
    }

    private static string EscapeJavaScript(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
