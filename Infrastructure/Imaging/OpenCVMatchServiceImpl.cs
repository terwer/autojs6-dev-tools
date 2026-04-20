using System.Diagnostics;
using Core.Abstractions;
using Core.Models;
using OpenCvSharp;

namespace Infrastructure.Imaging;

/// <summary>
/// OpenCV 模板匹配服务实现。
/// </summary>
public sealed class OpenCVMatchServiceImpl : IOpenCVMatchService
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

                using var screenshotMat = Mat.FromImageData(screenshot);
                using var templateMat = Mat.FromImageData(template);

                if (screenshotMat.Empty() || templateMat.Empty())
                {
                    return null;
                }

                using var searchContext = CreateSearchContext(screenshotMat, region);
                using var result = new Mat();

                Cv2.MatchTemplate(searchContext.SearchMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out var maxLoc);

                stopwatch.Stop();

                return new MatchResult
                {
                    X = maxLoc.X + searchContext.OffsetX,
                    Y = maxLoc.Y + searchContext.OffsetY,
                    Width = templateMat.Width,
                    Height = templateMat.Height,
                    Confidence = maxVal,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    IsMatch = maxVal >= threshold,
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
            var matches = new List<MatchResult>();

            try
            {
                var stopwatch = Stopwatch.StartNew();

                using var screenshotMat = Mat.FromImageData(screenshot);
                using var templateMat = Mat.FromImageData(template);

                if (screenshotMat.Empty() || templateMat.Empty())
                {
                    return matches;
                }

                using var searchContext = CreateSearchContext(screenshotMat, region);
                using var result = new Mat();

                Cv2.MatchTemplate(searchContext.SearchMat, templateMat, result, TemplateMatchModes.CCoeffNormed);

                for (var y = 0; y < result.Rows; y++)
                {
                    for (var x = 0; x < result.Cols; x++)
                    {
                        var confidence = result.At<float>(y, x);
                        if (confidence < threshold)
                        {
                            continue;
                        }

                        matches.Add(new MatchResult
                        {
                            X = x + searchContext.OffsetX,
                            Y = y + searchContext.OffsetY,
                            Width = templateMat.Width,
                            Height = templateMat.Height,
                            Confidence = confidence,
                            ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                            IsMatch = true,
                            Algorithm = "TM_CCOEFF_NORMED",
                            Threshold = threshold
                        });
                    }
                }
            }
            catch
            {
                // 返回空列表
            }

            return matches;
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

                if (mat1.Empty() || mat2.Empty() || mat1.Width != mat2.Width || mat1.Height != mat2.Height)
                {
                    return 0.0;
                }

                using var result = new Mat();
                Cv2.MatchTemplate(mat1, mat2, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out _);
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
            return !mat.Empty() && mat.Width > 0 && mat.Height > 0;
        }
        catch
        {
            return false;
        }
    }

    private static SearchContext CreateSearchContext(Mat screenshotMat, CropRegion? region)
    {
        if (region == null)
        {
            return new SearchContext(screenshotMat, 0, 0, ownsMat: false);
        }

        var safeX = Math.Clamp(region.X, 0, Math.Max(0, screenshotMat.Width - 1));
        var safeY = Math.Clamp(region.Y, 0, Math.Max(0, screenshotMat.Height - 1));
        var safeWidth = Math.Clamp(region.Width, 1, screenshotMat.Width - safeX);
        var safeHeight = Math.Clamp(region.Height, 1, screenshotMat.Height - safeY);

        var rect = new Rect(safeX, safeY, safeWidth, safeHeight);
        return new SearchContext(new Mat(screenshotMat, rect), safeX, safeY, ownsMat: true);
    }

    private sealed class SearchContext : IDisposable
    {
        public SearchContext(Mat searchMat, int offsetX, int offsetY, bool ownsMat)
        {
            SearchMat = searchMat;
            OffsetX = offsetX;
            OffsetY = offsetY;
            _ownsMat = ownsMat;
        }

        public Mat SearchMat { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }

        private readonly bool _ownsMat;

        public void Dispose()
        {
            if (_ownsMat)
            {
                SearchMat.Dispose();
            }
        }
    }
}
