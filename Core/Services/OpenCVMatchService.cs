using System.Diagnostics;
using OpenCvSharp;
using Core.Abstractions;
using Core.Models;

namespace Core.Services;

/// <summary>
/// OpenCV 模板匹配服务实现
/// 参考 MVP4.OpenCVBenchmark 的最佳实践
/// </summary>
public class OpenCVMatchService : IOpenCVMatchService
{
    public async Task<MatchResult?> MatchTemplateAsync(
        byte[] screenshot,
        byte[] template,
        double threshold = 0.8,
        CropRegion? region = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // 加载图像
                using var screenshotMat = Mat.FromImageData(screenshot);
                using var templateMat = Mat.FromImageData(template);

                // 如果指定了区域，裁剪截图
                Mat searchMat = screenshotMat;
                int offsetX = 0;
                int offsetY = 0;

                if (region != null)
                {
                    var rect = new Rect(region.X, region.Y, region.Width, region.Height);
                    searchMat = new Mat(screenshotMat, rect);
                    offsetX = region.X;
                    offsetY = region.Y;
                }

                // 参考 MVP4: 执行模板匹配 (TM_CCOEFF_NORMED)
                using var result = new Mat();
                Cv2.MatchTemplate(searchMat, templateMat, result, TemplateMatchModes.CCoeffNormed);

                // 参考 MVP4: 查找最佳匹配位置
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point maxLoc);

                stopwatch.Stop();

                // 判断是否超过阈值
                bool isMatch = maxVal >= threshold;

                // 调整坐标（如果使用了区域裁剪）
                int finalX = maxLoc.X + offsetX;
                int finalY = maxLoc.Y + offsetY;

                if (region != null && searchMat != screenshotMat)
                {
                    searchMat.Dispose();
                }

                return new MatchResult
                {
                    X = finalX,
                    Y = finalY,
                    Width = templateMat.Width,
                    Height = templateMat.Height,
                    Confidence = maxVal,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    IsMatch = isMatch,
                    Algorithm = "TM_CCOEFF_NORMED",
                    Threshold = threshold
                };
            }
            catch
            {
                return null;
            }
        }, cancellationToken);
    }

    public async Task<List<MatchResult>> MatchTemplateMultiAsync(
        byte[] screenshot,
        byte[] template,
        double threshold = 0.8,
        CropRegion? region = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var results = new List<MatchResult>();

            try
            {
                var stopwatch = Stopwatch.StartNew();

                using var screenshotMat = Mat.FromImageData(screenshot);
                using var templateMat = Mat.FromImageData(template);

                Mat searchMat = screenshotMat;
                int offsetX = 0;
                int offsetY = 0;

                if (region != null)
                {
                    var rect = new Rect(region.X, region.Y, region.Width, region.Height);
                    searchMat = new Mat(screenshotMat, rect);
                    offsetX = region.X;
                    offsetY = region.Y;
                }

                using var result = new Mat();
                Cv2.MatchTemplate(searchMat, templateMat, result, TemplateMatchModes.CCoeffNormed);

                // 查找所有超过阈值的匹配
                for (int y = 0; y < result.Rows; y++)
                {
                    for (int x = 0; x < result.Cols; x++)
                    {
                        double value = result.At<float>(y, x);
                        if (value >= threshold)
                        {
                            results.Add(new MatchResult
                            {
                                X = x + offsetX,
                                Y = y + offsetY,
                                Width = templateMat.Width,
                                Height = templateMat.Height,
                                Confidence = value,
                                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                                IsMatch = true,
                                Algorithm = "TM_CCOEFF_NORMED",
                                Threshold = threshold
                            });
                        }
                    }
                }

                stopwatch.Stop();

                if (region != null && searchMat != screenshotMat)
                {
                    searchMat.Dispose();
                }
            }
            catch
            {
                // 返回空列表
            }

            return results;
        }, cancellationToken);
    }

    public async Task<double> CalculateSimilarityAsync(byte[] image1, byte[] image2)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var mat1 = Mat.FromImageData(image1);
                using var mat2 = Mat.FromImageData(image2);

                // 确保尺寸相同
                if (mat1.Width != mat2.Width || mat1.Height != mat2.Height)
                {
                    return 0.0;
                }

                // 使用 TM_CCOEFF_NORMED 计算相似度
                using var result = new Mat();
                Cv2.MatchTemplate(mat1, mat2, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);

                return maxVal;
            }
            catch
            {
                return 0.0;
            }
        });
    }

    public bool ValidateTemplate(byte[] template)
    {
        try
        {
            using var mat = Mat.FromImageData(template);
            return mat.Width > 0 && mat.Height > 0 && !mat.Empty();
        }
        catch
        {
            return false;
        }
    }
}
