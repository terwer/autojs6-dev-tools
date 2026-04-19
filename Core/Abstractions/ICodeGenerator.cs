using Core.Models;

namespace Core.Abstractions;

/// <summary>
/// AutoJS6 代码生成器接口
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// 生成图像模式代码（images.findImage）
    /// </summary>
    /// <param name="options">代码生成选项</param>
    /// <returns>JavaScript 代码</returns>
    string GenerateImageModeCode(AutoJS6CodeOptions options);

    /// <summary>
    /// 生成控件模式代码（UiSelector）
    /// </summary>
    /// <param name="options">代码生成选项</param>
    /// <returns>JavaScript 代码</returns>
    string GenerateWidgetModeCode(AutoJS6CodeOptions options);

    /// <summary>
    /// 生成完整脚本（包含初始化和清理代码）
    /// </summary>
    /// <param name="options">代码生成选项</param>
    /// <returns>完整的 JavaScript 脚本</returns>
    string GenerateFullScript(AutoJS6CodeOptions options);

    /// <summary>
    /// 格式化 JavaScript 代码
    /// </summary>
    /// <param name="code">原始代码</param>
    /// <param name="indentSize">缩进大小</param>
    /// <returns>格式化后的代码</returns>
    string FormatCode(string code, int indentSize = 4);

    /// <summary>
    /// 验证生成的代码是否符合 AutoJS6 约束
    /// </summary>
    /// <param name="code">代码</param>
    /// <returns>(是否有效, 错误信息列表)</returns>
    (bool IsValid, List<string> Errors) ValidateCode(string code);
}
