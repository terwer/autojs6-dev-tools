using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.Storage.Streams;
using Core.Models;

namespace App.Views;

/// <summary>
/// Win2D 画布视图
/// 参考 MVP3.Win2DCoordinate 的最佳实践
/// 实现分层渲染：ImageLayer（底层）+ OverlayLayer（上层）
/// </summary>
public sealed partial class CanvasView : Page
{
    // 画布状态
    private float _scale = 1.0f;
    private float _offsetX = 0.0f;
    private float _offsetY = 0.0f;

    // 图像层（底层）
    private CanvasBitmap? _imageBitmap;
    private byte[]? _imageData;
    private int _imageWidth;
    private int _imageHeight;
    private int _imageRotation; // 设备旋转角度 (0, 90, 180, 270)
    private string? _imageHash; // 用于缓存验证

    // CanvasBitmap 缓存池
    private readonly Dictionary<string, CanvasBitmap> _bitmapCache = new();
    private const int MaxCacheSize = 10; // 最多缓存 10 个位图

    // Overlay 层（上层）
    private List<WidgetNode> _widgetNodes = new();
    private List<MatchResult> _matchResults = new();
    private CropRegion? _cropRegion;
    private bool _showWidgetBounds = true;
    private bool _showMatchResults = true;
    private bool _showCropRegion = true;
    private float _overlayOpacity = 0.7f;

    // 交互状态
    private bool _isDragging = false;
    private Point _lastPointerPosition;

    // 惯性滑动
    private Vector2 _velocity = Vector2.Zero;
    private DispatcherTimer? _inertiaTimer;
    private const float InertiaDecay = 0.92f; // 衰减系数
    private const float MinVelocity = 0.5f; // 最小速度阈值

    public CanvasView()
    {
        this.InitializeComponent();

        // 初始化惯性滑动定时器
        _inertiaTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _inertiaTimer.Tick += InertiaTimer_Tick;
    }

    /// <summary>
    /// 惯性滑动定时器回调
    /// </summary>
    private void InertiaTimer_Tick(object? sender, object e)
    {
        // 应用速度
        _offsetX += _velocity.X;
        _offsetY += _velocity.Y;

        // 衰减速度
        _velocity *= InertiaDecay;

        // 如果速度低于阈值，停止定时器
        if (Math.Abs(_velocity.X) < MinVelocity && Math.Abs(_velocity.Y) < MinVelocity)
        {
            _velocity = Vector2.Zero;
            _inertiaTimer?.Stop();
        }

        Canvas?.Invalidate();
    }

    /// <summary>
    /// 设置控件节点列表（用于绘制边界框）
    /// </summary>
    public void SetWidgetNodes(List<WidgetNode> nodes)
    {
        _widgetNodes = nodes;
        Canvas?.Invalidate();
    }

    /// <summary>
    /// 设置匹配结果列表（用于绘制匹配框）
    /// </summary>
    public void SetMatchResults(List<MatchResult> results)
    {
        _matchResults = results;
        Canvas?.Invalidate();
    }

    /// <summary>
    /// 设置裁剪区域（用于绘制裁剪框）
    /// </summary>
    public void SetCropRegion(CropRegion? region)
    {
        _cropRegion = region;
        Canvas?.Invalidate();
    }

    /// <summary>
    /// 设置 Overlay 层透明度
    /// </summary>
    public void SetOverlayOpacity(float opacity)
    {
        _overlayOpacity = Math.Clamp(opacity, 0.0f, 1.0f);
        Canvas?.Invalidate();
    }

    /// <summary>
    /// 切换控件边界框显示
    /// </summary>
    public void ToggleWidgetBounds(bool show)
    {
        _showWidgetBounds = show;
        Canvas?.Invalidate();
    }

    /// <summary>
    /// 切换匹配结果显示
    /// </summary>
    public void ToggleMatchResults(bool show)
    {
        _showMatchResults = show;
        Canvas?.Invalidate();
    }

    /// <summary>
    /// 切换裁剪区域显示
    /// </summary>
    public void ToggleCropRegion(bool show)
    {
        _showCropRegion = show;
        Canvas?.Invalidate();
    }

    /// <summary>
    /// 加载图像数据到画布
    /// 参考 MVP3: CanvasBitmap.CreateFromBytes
    /// 使用缓存池避免重复创建纹理
    /// </summary>
    public async void LoadImage(byte[] imageData, int width, int height, int rotation = 0)
    {
        _imageData = imageData;
        _imageWidth = width;
        _imageHeight = height;
        _imageRotation = rotation;

        // 计算图像哈希（用于缓存键）
        _imageHash = ComputeImageHash(imageData, width, height);

        // 检查缓存
        if (_bitmapCache.TryGetValue(_imageHash, out var cachedBitmap))
        {
            _imageBitmap = cachedBitmap;
        }
        else
        {
            // 保存旧位图引用
            var oldBitmap = _imageBitmap;

            // 先清空 _imageBitmap，避免 Canvas_Draw 访问即将释放的对象
            _imageBitmap = null;

            // 立即加载位图（在这里完成，而不是在 Canvas_Draw 中）
            if (Canvas != null)
            {
                try
                {
                    using (var stream = new System.IO.MemoryStream(imageData))
                    {
                        _imageBitmap = await CanvasBitmap.LoadAsync(Canvas, stream.AsRandomAccessStream());

                        // 加入缓存
                        _bitmapCache[_imageHash] = _imageBitmap;

                        // 如果缓存超过限制，移除最旧的
                        if (_bitmapCache.Count > MaxCacheSize)
                        {
                            var oldestKey = _bitmapCache.Keys.First();
                            var oldestBitmap = _bitmapCache[oldestKey];
                            oldestBitmap.Dispose();
                            _bitmapCache.Remove(oldestKey);
                        }
                    } // stream 在这里释放，确保所有操作完成
                }
                catch
                {
                    // 加载失败，清空数据
                    _imageData = null;
                    _imageBitmap = null;
                    return;
                }
            }

            // 释放旧位图（如果不在缓存中）
            if (oldBitmap != null && !_bitmapCache.ContainsValue(oldBitmap))
            {
                oldBitmap.Dispose();
            }
        }

        // 默认使用原图模式（1:1 显示，左上角对齐）
        _scale = 1.0f;
        _offsetX = 0.0f;
        _offsetY = 0.0f;

        // 触发重绘
        Canvas?.Invalidate();
    }

    /// <summary>
    /// 计算图像哈希（用于缓存键）
    /// </summary>
    private string ComputeImageHash(byte[] data, int width, int height)
    {
        // 简单哈希：宽度 + 高度 + 数据长度 + 前 16 字节
        var hash = $"{width}x{height}_{data.Length}";
        if (data.Length >= 16)
        {
            for (int i = 0; i < 16; i++)
            {
                hash += data[i].ToString("X2");
            }
        }
        return hash;
    }

    /// <summary>
    /// 清理缓存池（释放所有位图）
    /// </summary>
    public void ClearBitmapCache()
    {
        foreach (var bitmap in _bitmapCache.Values)
        {
            bitmap.Dispose();
        }
        _bitmapCache.Clear();
        _imageBitmap = null;
    }

    /// <summary>
    /// 重置视图（缩放、平移、旋转）
    /// </summary>
    public void ResetView()
    {
        _scale = 1.0f;
        _offsetX = 0.0f;
        _offsetY = 0.0f;
        Canvas?.Invalidate();
    }

    /// <summary>
    /// 适应窗口（自动缩放使图像完整显示）
    /// </summary>
    public void FitToWindow()
    {
        if (Canvas != null && _imageWidth > 0 && _imageHeight > 0)
        {
            double canvasWidth = Canvas.ActualWidth;
            double canvasHeight = Canvas.ActualHeight;

            if (canvasWidth > 0 && canvasHeight > 0)
            {
                // 计算缩放比例
                float scaleX = (float)(canvasWidth / _imageWidth);
                float scaleY = (float)(canvasHeight / _imageHeight);

                // 根据设备旋转角度判断实际显示方向
                // 0° 或 180°: 保持原方向
                // 90° 或 270°: 旋转 90 度
                bool isRotated = (_imageRotation == 90 || _imageRotation == 270);

                // 判断实际显示方向
                bool imageIsLandscape;
                if (isRotated)
                {
                    // 旋转后，宽高互换
                    imageIsLandscape = _imageHeight > _imageWidth;
                }
                else
                {
                    imageIsLandscape = _imageWidth > _imageHeight;
                }

                // 根据图像实际显示方向选择适应策略
                if (imageIsLandscape)
                {
                    // 横屏：适应宽度
                    _scale = scaleX;
                }
                else
                {
                    // 竖屏：适应高度
                    _scale = scaleY;
                }

                // 限制缩放范围 10%-500%
                _scale = Math.Clamp(_scale, 0.1f, 5.0f);

                // 计算缩放后的图像尺寸
                float scaledWidth = _imageWidth * _scale;
                float scaledHeight = _imageHeight * _scale;

                // 居中显示：计算偏移量
                _offsetX = (float)((canvasWidth - scaledWidth) / 2);
                _offsetY = (float)((canvasHeight - scaledHeight) / 2);

                // 调试日志
                Services.LogService.Instance.Log($"[FitToWindow] Canvas=({canvasWidth:F1}x{canvasHeight:F1}), Image=({_imageWidth}x{_imageHeight}), Rotation={_imageRotation}°");
                Services.LogService.Instance.Log($"[FitToWindow] 实际方向={(imageIsLandscape ? "横屏" : "竖屏")}, 适应={(imageIsLandscape ? "宽度" : "高度")}, ScaleX={scaleX:F3}, ScaleY={scaleY:F3}, Final={_scale:F3}");
                Services.LogService.Instance.Log($"[FitToWindow] Scaled=({scaledWidth:F1}x{scaledHeight:F1}), Offset=({_offsetX:F1}, {_offsetY:F1})");

                Canvas.Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取当前缩放比例
    /// </summary>
    public float GetScale() => _scale;

    /// <summary>
    /// 获取当前偏移量
    /// </summary>
    public (float X, float Y) GetOffset() => (_offsetX, _offsetY);

    /// <summary>
    /// 画布坐标转图像坐标
    /// 参考 MVP3 的坐标系转换公式
    /// </summary>
    public (float X, float Y) CanvasToImage(float canvasX, float canvasY)
    {
        float imageX = (canvasX - _offsetX) / _scale;
        float imageY = (canvasY - _offsetY) / _scale;
        return (imageX, imageY);
    }

    /// <summary>
    /// 图像坐标转画布坐标
    /// </summary>
    public (float X, float Y) ImageToCanvas(float imageX, float imageY)
    {
        float canvasX = imageX * _scale + _offsetX;
        float canvasY = imageY * _scale + _offsetY;
        return (canvasX, canvasY);
    }

    /// <summary>
    /// Win2D 绘制
    /// 实现分层渲染：ImageLayer（底层）+ OverlayLayer（上层）
    /// </summary>
    private void Canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        var ds = args.DrawingSession;

        // === 图像层（底层）===
        var bitmap = _imageBitmap; // 本地副本，避免多线程竞争
        if (bitmap != null)
        {
            try
            {
                // 应用变换矩阵：先缩放，再平移
                var transform = Matrix3x2.CreateScale(_scale) * Matrix3x2.CreateTranslation(_offsetX, _offsetY);
                ds.Transform = transform;
                ds.DrawImage(bitmap);
                ds.Transform = Matrix3x2.Identity;
            }
            catch (ObjectDisposedException)
            {
                // 位图已被释放，跳过本次绘制
            }
        }

        // === Overlay 层（上层）===
        // 应用变换矩阵（与图像层相同）
        var overlayTransform = Matrix3x2.CreateScale(_scale) * Matrix3x2.CreateTranslation(_offsetX, _offsetY);
        ds.Transform = overlayTransform;

        // 绘制控件边界框
        if (_showWidgetBounds)
        {
            DrawWidgetBounds(ds);
        }

        // 绘制匹配结果框
        if (_showMatchResults)
        {
            DrawMatchResults(ds);
        }

        // 绘制裁剪区域框
        if (_showCropRegion && _cropRegion != null)
        {
            DrawCropRegion(ds);
        }

        // 重置变换
        ds.Transform = Matrix3x2.Identity;
    }

    /// <summary>
    /// 绘制控件边界框（按类型着色）
    /// </summary>
    private void DrawWidgetBounds(Microsoft.Graphics.Canvas.CanvasDrawingSession ds)
    {
        foreach (var node in _widgetNodes)
        {
            var (x, y, w, h) = node.BoundsRect;
            if (w <= 0 || h <= 0) continue;

            // 按类型着色
            var color = GetWidgetColor(node.ClassName);
            var colorWithAlpha = Windows.UI.Color.FromArgb(
                (byte)(_overlayOpacity * 255),
                color.R,
                color.G,
                color.B
            );

            ds.DrawRectangle(x, y, w, h, colorWithAlpha, 2);
        }
    }

    /// <summary>
    /// 绘制匹配结果框（按置信度着色）
    /// </summary>
    private void DrawMatchResults(Microsoft.Graphics.Canvas.CanvasDrawingSession ds)
    {
        foreach (var result in _matchResults)
        {
            // 按置信度着色：绿色（高）、黄色（中）、橙色（低）
            var color = result.Confidence >= 0.9 ? Microsoft.UI.Colors.Green :
                       result.Confidence >= 0.8 ? Microsoft.UI.Colors.Yellow :
                       Microsoft.UI.Colors.Orange;

            var colorWithAlpha = Windows.UI.Color.FromArgb(
                (byte)(_overlayOpacity * 255),
                color.R,
                color.G,
                color.B
            );

            ds.DrawRectangle(result.X, result.Y, result.Width, result.Height, colorWithAlpha, 3);

            // 绘制置信度文本
            var text = $"{result.Confidence:F2}";
            ds.DrawText(text, result.X, result.Y - 20, colorWithAlpha);
        }
    }

    /// <summary>
    /// 绘制裁剪区域框（虚线）
    /// </summary>
    private void DrawCropRegion(Microsoft.Graphics.Canvas.CanvasDrawingSession ds)
    {
        if (_cropRegion == null) return;

        var color = Windows.UI.Color.FromArgb(
            (byte)(_overlayOpacity * 255),
            255, 0, 0 // 红色
        );

        // 绘制虚线矩形
        using (var strokeStyle = new Microsoft.Graphics.Canvas.Geometry.CanvasStrokeStyle())
        {
            strokeStyle.DashStyle = Microsoft.Graphics.Canvas.Geometry.CanvasDashStyle.Dash;
            ds.DrawRectangle(
                _cropRegion.X,
                _cropRegion.Y,
                _cropRegion.Width,
                _cropRegion.Height,
                color,
                2,
                strokeStyle
            );
        }
    }

    /// <summary>
    /// 根据控件类型返回颜色
    /// 蓝色=Text、绿色=Button、橙色=Image、灰色=其他
    /// </summary>
    private Windows.UI.Color GetWidgetColor(string className)
    {
        if (className.Contains("Text")) return Microsoft.UI.Colors.Blue;
        if (className.Contains("Button")) return Microsoft.UI.Colors.Green;
        if (className.Contains("Image")) return Microsoft.UI.Colors.Orange;
        return Microsoft.UI.Colors.Gray;
    }

    /// <summary>
    /// 滚轮缩放（以光标为中心）
    /// 参考 MVP3: Canvas_PointerWheelChanged
    /// </summary>
    private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas);
        float delta = point.Properties.MouseWheelDelta;
        float scaleFactor = delta > 0 ? 1.1f : 0.9f;

        // 计算缩放前光标在图像中的位置
        float mouseX = (float)(point.Position.X - _offsetX) / _scale;
        float mouseY = (float)(point.Position.Y - _offsetY) / _scale;

        // 应用缩放
        _scale *= scaleFactor;

        // 限制缩放范围 10%-500%
        _scale = Math.Clamp(_scale, 0.1f, 5.0f);

        // 调整偏移量，使光标位置保持不变
        _offsetX = (float)point.Position.X - mouseX * _scale;
        _offsetY = (float)point.Position.Y - mouseY * _scale;

        Canvas.Invalidate();
        e.Handled = true;
    }

    /// <summary>
    /// 鼠标按下（开始拖拽）
    /// 参考 MVP3: PointerPressed
    /// </summary>
    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas);

        // 右键拖拽平移
        if (point.Properties.IsRightButtonPressed)
        {
            _isDragging = true;
            _lastPointerPosition = point.Position;
            Canvas.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 鼠标移动（拖拽平移）
    /// 参考 MVP3: PointerMoved
    /// </summary>
    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas);

        if (_isDragging && point.Properties.IsRightButtonPressed)
        {
            // 计算偏移量
            double deltaX = point.Position.X - _lastPointerPosition.X;
            double deltaY = point.Position.Y - _lastPointerPosition.Y;

            _offsetX += (float)deltaX;
            _offsetY += (float)deltaY;

            // 更新速度（用于惯性滑动）
            _velocity = new Vector2((float)deltaX, (float)deltaY);

            _lastPointerPosition = point.Position;
            Canvas.Invalidate();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 鼠标释放（结束拖拽，启动惯性滑动）
    /// </summary>
    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            Canvas.ReleasePointerCapture(e.Pointer);

            // 如果有速度，启动惯性滑动
            if (Math.Abs(_velocity.X) > MinVelocity || Math.Abs(_velocity.Y) > MinVelocity)
            {
                _inertiaTimer?.Start();
            }

            e.Handled = true;
        }
    }
}
