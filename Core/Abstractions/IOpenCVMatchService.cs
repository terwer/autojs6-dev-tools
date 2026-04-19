using Core.Models;

namespace Core.Abstractions;

/// <summary>
/// OpenCV 模板匹配服务接口
/// </summary>
public interface IOpenCVMatchService
{
    /// <summary>
    /// 异步模板匹配（单个最佳匹配）
    /// </summary>
    /// <param name="screenshot">截图字节数组</param>
    /// <param name="template">模板字节数组</param>
    /// <param name="threshold">置信度阈值（0.0 - 1.0）</param>
    /// <param name="region">搜索区域（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配结果</returns>
    Task<MatchResult?> MatchTemplateAsync(
        byte[] screenshot,
        byte[] template,
        double threshold = 0.8,
        CropRegion? region = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步多模板匹配（返回所有超过阈值的匹配）
    /// </summary>
    /// <param name="screenshot">截图字节数组</param>
    /// <param name="template">模板字节数组</param>
    /// <param name="threshold">置信度阈值（0.0 - 1.0）</param>
    /// <param name="region">搜索区域（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配结果列表</returns>
    Task<List<MatchResult>> MatchTemplateMultiAsync(
        byte[] screenshot,
        byte[] template,
        double threshold = 0.8,
        CropRegion? region = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算两张图片的相似度
    /// </summary>
    /// <param name="image1">图片1字节数组</param>
    /// <param name="image2">图片2字节数组</param>
    /// <returns>相似度（0.0 - 1.0）</returns>
    Task<double> CalculateSimilarityAsync(byte[] image1, byte[] image2);

    /// <summary>
    /// 验证模板是否有效
    /// </summary>
    /// <param name="template">模板字节数组</param>
    /// <returns>是否有效</returns>
    bool ValidateTemplate(byte[] template);
}
