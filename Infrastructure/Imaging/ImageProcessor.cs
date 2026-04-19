using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using System.Text.Json;

namespace Infrastructure.Imaging;

/// <summary>
/// 图像处理器
/// 提供 PNG 解码、降采样、裁剪、导出功能
/// </summary>
public class ImageProcessor
{
    private const int MaxWidth = 1920;
    private const int MaxHeight = 1080;

    /// <summary>
    /// 解码 PNG 为 byte[]
    /// </summary>
    public async Task<byte[]> DecodePngAsync(byte[] pngData, CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync<Rgba32>(new MemoryStream(pngData), cancellationToken);

        var width = image.Width;
        var height = image.Height;
        var pixelData = new byte[width * height * 4];

        image.CopyPixelDataTo(pixelData);
        return pixelData;
    }

    /// <summary>
    /// 解码 PNG 为 Stream
    /// </summary>
    public async Task<Stream> DecodePngToStreamAsync(byte[] pngData, CancellationToken cancellationToken = default)
    {
        var ms = new MemoryStream();
        await ms.WriteAsync(pngData, cancellationToken);
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// 高分辨率图像降采样（最大 1920x1080，保持宽高比）
    /// </summary>
    public async Task<byte[]> DownsampleAsync(byte[] pngData, CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync<Rgba32>(new MemoryStream(pngData), cancellationToken);

        // 检查是否需要降采样
        if (image.Width <= MaxWidth && image.Height <= MaxHeight)
        {
            return pngData; // 无需降采样
        }

        // 计算缩放比例（保持宽高比）
        var scaleX = (double)MaxWidth / image.Width;
        var scaleY = (double)MaxHeight / image.Height;
        var scale = Math.Min(scaleX, scaleY);

        var newWidth = (int)(image.Width * scale);
        var newHeight = (int)(image.Height * scale);

        // 降采样
        image.Mutate(x => x.Resize(newWidth, newHeight));

        // 转换为 PNG
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new PngEncoder(), cancellationToken);
        return ms.ToArray();
    }

    /// <summary>
    /// 裁剪区域导出为独立 PNG 文件
    /// </summary>
    public async Task<byte[]> CropAsync(
        byte[] pngData,
        int x,
        int y,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync<Rgba32>(new MemoryStream(pngData), cancellationToken);

        // 验证裁剪区域
        if (x < 0 || y < 0 || x + width > image.Width || y + height > image.Height)
        {
            throw new ArgumentException("裁剪区域超出图像边界");
        }

        // 裁剪
        image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));

        // 转换为 PNG
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new PngEncoder(), cancellationToken);
        return ms.ToArray();
    }

    /// <summary>
    /// 生成 JSON 元数据（记录偏移量与原图尺寸）
    /// </summary>
    public string GenerateMetadata(
        int originalWidth,
        int originalHeight,
        int cropX,
        int cropY,
        int cropWidth,
        int cropHeight,
        string? templateName = null)
    {
        var metadata = new
        {
            originalWidth,
            originalHeight,
            crop = new
            {
                x = cropX,
                y = cropY,
                width = cropWidth,
                height = cropHeight
            },
            templateName,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// 获取图像尺寸
    /// </summary>
    public async Task<(int Width, int Height)> GetImageSizeAsync(
        byte[] pngData,
        CancellationToken cancellationToken = default)
    {
        using var image = await Image.LoadAsync<Rgba32>(new MemoryStream(pngData), cancellationToken);
        return (image.Width, image.Height);
    }

    /// <summary>
    /// 验证图像是否有效
    /// </summary>
    public async Task<bool> ValidateImageAsync(byte[] pngData)
    {
        try
        {
            using var image = await Image.LoadAsync<Rgba32>(new MemoryStream(pngData));
            return image.Width > 0 && image.Height > 0;
        }
        catch
        {
            return false;
        }
    }
}
