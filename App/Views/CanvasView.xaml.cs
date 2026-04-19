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
    // 缩放变化事件
    public event EventHandler<float>? ScaleChanged;

    // 控件选择事件
    public event EventHandler<WidgetNode>? WidgetSelected;

    // 裁剪区域变化事件
    public event EventHandler<CropRegion?>? CropRegionChanged;

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
    private Point _pointerPressedPosition; // 记录按下位置，用于区分点击和拖拽
    private const double ClickThreshold = 5.0; // 移动距离小于此值视为点击

    // 裁剪交互状态
    private bool _isCroppingMode = false; // 是否处于裁剪模式
    private bool _isCreatingCrop = false; // 是否正在创建裁剪区域
    private bool _isResizingCrop = false; // 是否正在调整裁剪区域
    private Point _cropStartPoint; // 裁剪起始点（图像坐标）
    private Point _cropCurrentPoint; // 裁剪当前点（图像坐标）
    private ResizeHandle _activeResizeHandle = ResizeHandle.None; // 当前激活的调整手柄
    private bool _isShiftPressed = false; // Shift 键是否按下（锁定宽高比）
    private double _lockedAspectRatio = 1.0; // 锁定的宽高比

    // 调整手柄枚举
    private enum ResizeHandle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Top,
        Bottom,
        Left,
        Right
    }

    // 手柄大小（画布坐标）
    private const double HandleSize = 8.0;

    // 惯性滑动
    private Vector2 _velocity = Vector2.Zero;
    private DispatcherTimer? _inertiaTimer;
    private const float InertiaDecay = 0.80f; // 衰减系数（降低以加快停止）
    private const float MinVelocity = 2.0f; // 最小速度阈值（提高以更快停止）
    private const float VelocityScale = 0.3f; // 速度缩放系数（降低初始速度）

    public CanvasView()
    {
        this.InitializeComponent();

        // 初始化惯性滑动定时器
        _inertiaTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _inertiaTimer.Tick += InertiaTimer_Tick;

        // 监听键盘事件（Shift 键）
        this.KeyDown += CanvasView_KeyDown;
        this.KeyUp += CanvasView_KeyUp;
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
        CropRegionChanged?.Invoke(this, region);
    }

    /// <summary>
    /// 获取当前裁剪区域
    /// </summary>
    public CropRegion? GetCropRegion() => _cropRegion;

    /// <summary>
    /// 启用裁剪模式（仅在 1:1 模式下允许）
    /// </summary>
    public bool EnableCroppingMode()
    {
        // 检查是否为 1:1 模式
        if (Math.Abs(_scale - 1.0f) > 0.001f)
        {
            Services.LogService.Instance.Log($"[Crop] 裁剪模式仅在 1:1 模式下可用，当前缩放: {_scale:F3}");
            return false;
        }

        _isCroppingMode = true;
        Services.LogService.Instance.Log($"[Crop] 裁剪模式已启用");
        return true;
    }

    /// <summary>
    /// 禁用裁剪模式
    /// </summary>
    public void DisableCroppingMode()
    {
        _isCroppingMode = false;
        _isCreatingCrop = false;
        _isResizingCrop = false;
        _activeResizeHandle = ResizeHandle.None;
        Services.LogService.Instance.Log($"[Crop] 裁剪模式已禁用");
    }

    /// <summary>
    /// 清除裁剪区域
    /// </summary>
    public void ClearCropRegion()
    {
        _cropRegion = null;
        Canvas?.Invalidate();
        CropRegionChanged?.Invoke(this, null);
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
    public async void LoadImage(byte[] imageData, int width, int height)
    {
        _imageData = imageData;
        _imageWidth = width;
        _imageHeight = height;
        _imageRotation = 0; // 不再使用旋转角度

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
                // 直接使用图像的实际宽高
                int displayWidth = _imageWidth;
                int displayHeight = _imageHeight;

                // 计算缩放比例
                float scaleX = (float)(canvasWidth / displayWidth);
                float scaleY = (float)(canvasHeight / displayHeight);

                // 取较小的缩放比例，确保图像完整显示在窗口内
                _scale = Math.Min(scaleX, scaleY);

                // 限制缩放范围 10%-500%
                _scale = Math.Clamp(_scale, 0.1f, 5.0f);

                // 计算缩放后的图像尺寸
                float scaledWidth = displayWidth * _scale;
                float scaledHeight = displayHeight * _scale;

                // 居中显示：计算偏移量
                _offsetX = (float)((canvasWidth - scaledWidth) / 2);
                _offsetY = (float)((canvasHeight - scaledHeight) / 2);

                Services.LogService.Instance.Log($"[FitToWindow] Canvas=({canvasWidth:F1}x{canvasHeight:F1}), Image=({_imageWidth}x{_imageHeight})");
                Services.LogService.Instance.Log($"[FitToWindow] ScaleX={scaleX:F3}, ScaleY={scaleY:F3}, Final={_scale:F3}");
                Services.LogService.Instance.Log($"[FitToWindow] Scaled=({scaledWidth:F1}x{scaledHeight:F1}), Offset=({_offsetX:F1}, {_offsetY:F1})");

                Canvas.Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取当前旋转角度
    /// </summary>
    public int GetRotation() => _imageRotation;

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

        Services.LogService.Instance.Log($"[Canvas_Draw] 画布尺寸: {sender.ActualWidth:F1}x{sender.ActualHeight:F1}, 缩放: {_scale:F3}, 偏移: ({_offsetX:F1}, {_offsetY:F1})");

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

        Services.LogService.Instance.Log($"[Canvas_Draw] Overlay 层状态: WidgetBounds={_showWidgetBounds}, MatchResults={_showMatchResults}, CropRegion={_showCropRegion}");

        // 绘制控件边界框
        if (_showWidgetBounds)
        {
            DrawWidgetBounds(ds);
        }
        else
        {
            Services.LogService.Instance.Log($"[Canvas_Draw] 跳过 WidgetBounds 绘制（_showWidgetBounds=false）");
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
        Services.LogService.Instance.Log($"[DrawWidgetBounds] 开始绘制，节点数: {_widgetNodes.Count}, 显示状态: {_showWidgetBounds}");

        int drawnCount = 0;
        foreach (var node in _widgetNodes)
        {
            var (x, y, w, h) = node.BoundsRect;
            if (w <= 0 || h <= 0)
            {
                Services.LogService.Instance.Log($"[DrawWidgetBounds] 跳过无效节点: {node.ClassName}, Bounds=({x}, {y}, {w}, {h})");
                continue;
            }

            // 按类型着色
            var color = GetWidgetColor(node.ClassName);
            var colorWithAlpha = Windows.UI.Color.FromArgb(
                (byte)(_overlayOpacity * 255),
                color.R,
                color.G,
                color.B
            );

            ds.DrawRectangle(x, y, w, h, colorWithAlpha, 2);
            drawnCount++;

            if (drawnCount <= 3)
            {
                Services.LogService.Instance.Log($"[DrawWidgetBounds] 绘制节点 {drawnCount}: {node.ClassName}, Bounds=({x}, {y}, {w}, {h}), Color=({colorWithAlpha.R}, {colorWithAlpha.G}, {colorWithAlpha.B}, {colorWithAlpha.A})");
            }
        }

        Services.LogService.Instance.Log($"[DrawWidgetBounds] 完成绘制，共绘制 {drawnCount} 个节点");
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
    /// 绘制裁剪区域框（虚线 + 调整手柄）
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

        // 如果处于裁剪模式，绘制调整手柄
        if (_isCroppingMode)
        {
            DrawResizeHandles(ds, _cropRegion);
        }
    }

    /// <summary>
    /// 绘制调整手柄（8个：4个顶点 + 4个边中点）
    /// </summary>
    private void DrawResizeHandles(Microsoft.Graphics.Canvas.CanvasDrawingSession ds, CropRegion region)
    {
        var handleColor = Windows.UI.Color.FromArgb(255, 255, 255, 255); // 白色
        var handleBorderColor = Windows.UI.Color.FromArgb(255, 0, 0, 0); // 黑色边框

        // 计算手柄在图像坐标系中的大小
        float handleSizeInImage = (float)(HandleSize / _scale);

        // 8个手柄位置（图像坐标）
        var handles = new[]
        {
            new { X = region.X, Y = region.Y, Handle = ResizeHandle.TopLeft }, // 左上
            new { X = region.X + region.Width, Y = region.Y, Handle = ResizeHandle.TopRight }, // 右上
            new { X = region.X, Y = region.Y + region.Height, Handle = ResizeHandle.BottomLeft }, // 左下
            new { X = region.X + region.Width, Y = region.Y + region.Height, Handle = ResizeHandle.BottomRight }, // 右下
            new { X = region.X + region.Width / 2, Y = region.Y, Handle = ResizeHandle.Top }, // 上中
            new { X = region.X + region.Width / 2, Y = region.Y + region.Height, Handle = ResizeHandle.Bottom }, // 下中
            new { X = region.X, Y = region.Y + region.Height / 2, Handle = ResizeHandle.Left }, // 左中
            new { X = region.X + region.Width, Y = region.Y + region.Height / 2, Handle = ResizeHandle.Right } // 右中
        };

        foreach (var handle in handles)
        {
            // 绘制手柄（白色填充 + 黑色边框）
            float halfSize = handleSizeInImage / 2;
            ds.FillRectangle(
                handle.X - halfSize,
                handle.Y - halfSize,
                handleSizeInImage,
                handleSizeInImage,
                handleColor
            );
            ds.DrawRectangle(
                handle.X - halfSize,
                handle.Y - halfSize,
                handleSizeInImage,
                handleSizeInImage,
                handleBorderColor,
                1.0f / _scale // 边框宽度适应缩放
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

        // 触发缩放变化事件
        ScaleChanged?.Invoke(this, _scale);

        Canvas.Invalidate();
        e.Handled = true;
    }

    /// <summary>
    /// 鼠标按下（开始拖拽或裁剪）
    /// 参考 MVP3: PointerPressed
    /// </summary>
    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas);

        // 记录按下位置
        _pointerPressedPosition = point.Position;

        // 如果处于裁剪模式且按下左键
        if (_isCroppingMode && point.Properties.IsLeftButtonPressed)
        {
            // 转换为图像坐标
            var (imageX, imageY) = CanvasToImage((float)point.Position.X, (float)point.Position.Y);

            // 检查是否点击了调整手柄
            if (_cropRegion != null)
            {
                var handle = GetResizeHandleAt(imageX, imageY);
                if (handle != ResizeHandle.None)
                {
                    // 开始调整裁剪区域
                    _isResizingCrop = true;
                    _activeResizeHandle = handle;
                    _cropStartPoint = new Point(imageX, imageY);

                    // 如果按下 Shift，锁定宽高比
                    if (_isShiftPressed && _cropRegion.Width > 0 && _cropRegion.Height > 0)
                    {
                        _lockedAspectRatio = (double)_cropRegion.Width / _cropRegion.Height;
                    }

                    Canvas.CapturePointer(e.Pointer);
                    e.Handled = true;
                    return;
                }
            }

            // 开始创建新的裁剪区域
            _isCreatingCrop = true;
            _cropStartPoint = new Point(imageX, imageY);
            _cropCurrentPoint = new Point(imageX, imageY);

            // 如果按下 Shift，锁定宽高比为 1:1
            if (_isShiftPressed)
            {
                _lockedAspectRatio = 1.0;
            }

            Canvas.CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        // 左键或右键拖拽平移
        if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
        {
            _isDragging = true;
            _lastPointerPosition = point.Position;
            Canvas.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 鼠标移动（拖拽平移或裁剪）
    /// 参考 MVP3: PointerMoved
    /// </summary>
    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas);

        // 如果正在创建裁剪区域
        if (_isCreatingCrop && point.Properties.IsLeftButtonPressed)
        {
            var (imageX, imageY) = CanvasToImage((float)point.Position.X, (float)point.Position.Y);
            _cropCurrentPoint = new Point(imageX, imageY);

            // 计算裁剪区域
            UpdateCropRegionFromPoints(_cropStartPoint, _cropCurrentPoint);

            Canvas.Invalidate();
            e.Handled = true;
            return;
        }

        // 如果正在调整裁剪区域
        if (_isResizingCrop && point.Properties.IsLeftButtonPressed)
        {
            var (imageX, imageY) = CanvasToImage((float)point.Position.X, (float)point.Position.Y);
            _cropCurrentPoint = new Point(imageX, imageY);

            // 根据手柄调整裁剪区域
            ResizeCropRegion(_activeResizeHandle, imageX, imageY);

            Canvas.Invalidate();
            e.Handled = true;
            return;
        }

        if (_isDragging && (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed))
        {
            // 计算偏移量
            double deltaX = point.Position.X - _lastPointerPosition.X;
            double deltaY = point.Position.Y - _lastPointerPosition.Y;

            _offsetX += (float)deltaX;
            _offsetY += (float)deltaY;

            // 更新速度（用于惯性滑动，应用速度缩放）
            _velocity = new Vector2((float)deltaX * VelocityScale, (float)deltaY * VelocityScale);

            _lastPointerPosition = point.Position;
            Canvas.Invalidate();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 鼠标释放（结束拖拽、裁剪或调整）
    /// </summary>
    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas);

        // 如果正在创建裁剪区域
        if (_isCreatingCrop)
        {
            _isCreatingCrop = false;
            Canvas.ReleasePointerCapture(e.Pointer);

            // 确保裁剪区域有效（宽高至少 5 像素）
            if (_cropRegion != null && (_cropRegion.Width < 5 || _cropRegion.Height < 5))
            {
                _cropRegion = null;
                Services.LogService.Instance.Log($"[Crop] 裁剪区域太小，已取消");
            }
            else if (_cropRegion != null)
            {
                Services.LogService.Instance.Log($"[Crop] 裁剪区域创建完成: ({_cropRegion.X}, {_cropRegion.Y}, {_cropRegion.Width}, {_cropRegion.Height})");
            }

            Canvas.Invalidate();
            e.Handled = true;
            return;
        }

        // 如果正在调整裁剪区域
        if (_isResizingCrop)
        {
            _isResizingCrop = false;
            _activeResizeHandle = ResizeHandle.None;
            Canvas.ReleasePointerCapture(e.Pointer);

            Services.LogService.Instance.Log($"[Crop] 裁剪区域调整完成: ({_cropRegion?.X}, {_cropRegion?.Y}, {_cropRegion?.Width}, {_cropRegion?.Height})");

            Canvas.Invalidate();
            e.Handled = true;
            return;
        }

        if (_isDragging)
        {
            _isDragging = false;
            Canvas.ReleasePointerCapture(e.Pointer);

            // 计算移动距离
            double distance = Math.Sqrt(
                Math.Pow(point.Position.X - _pointerPressedPosition.X, 2) +
                Math.Pow(point.Position.Y - _pointerPressedPosition.Y, 2)
            );

            // 如果移动距离小于阈值，视为点击
            if (distance < ClickThreshold)
            {
                // 左键点击选择控件
                if (point.Properties.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.LeftButtonReleased)
                {
                    HandleWidgetClick(point.Position);
                }
            }
            else
            {
                // 如果有速度，启动惯性滑动
                if (Math.Abs(_velocity.X) > MinVelocity || Math.Abs(_velocity.Y) > MinVelocity)
                {
                    _inertiaTimer?.Start();
                }
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// 处理控件点击（选择控件）
    /// </summary>
    private void HandleWidgetClick(Point canvasPosition)
    {
        // 转换为图像坐标
        var (imageX, imageY) = CanvasToImage((float)canvasPosition.X, (float)canvasPosition.Y);

        // 查找点击位置的控件（从最小的开始，优先选择子控件）
        WidgetNode? selectedWidget = null;
        double minArea = double.MaxValue;

        foreach (var widget in _widgetNodes)
        {
            if (IsPointInWidget(imageX, imageY, widget))
            {
                double area = widget.BoundsRect.Width * widget.BoundsRect.Height;
                if (area < minArea)
                {
                    minArea = area;
                    selectedWidget = widget;
                }
            }
        }

        // 触发控件选择事件
        if (selectedWidget != null)
        {
            WidgetSelected?.Invoke(this, selectedWidget);
        }
    }

    /// <summary>
    /// 判断点是否在控件内
    /// </summary>
    private bool IsPointInWidget(float x, float y, WidgetNode widget)
    {
        return x >= widget.BoundsRect.X &&
               x <= widget.BoundsRect.X + widget.BoundsRect.Width &&
               y >= widget.BoundsRect.Y &&
               y <= widget.BoundsRect.Y + widget.BoundsRect.Height;
    }

    /// <summary>
    /// 键盘按下事件（监听 Shift 键）
    /// </summary>
    private void CanvasView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Shift)
        {
            _isShiftPressed = true;
            Services.LogService.Instance.Log($"[Crop] Shift 键按下，宽高比锁定");
        }
    }

    /// <summary>
    /// 键盘释放事件（监听 Shift 键）
    /// </summary>
    private void CanvasView_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Shift)
        {
            _isShiftPressed = false;
            Services.LogService.Instance.Log($"[Crop] Shift 键释放，宽高比解锁");
        }
    }

    /// <summary>
    /// 根据两点更新裁剪区域
    /// </summary>
    private void UpdateCropRegionFromPoints(Point start, Point current)
    {
        // 计算矩形（确保左上角为起点）
        int x = (int)Math.Min(start.X, current.X);
        int y = (int)Math.Min(start.Y, current.Y);
        int width = (int)Math.Abs(current.X - start.X);
        int height = (int)Math.Abs(current.Y - start.Y);

        // 如果按下 Shift，锁定宽高比
        if (_isShiftPressed)
        {
            // 以较小的边为基准，调整另一边
            if (width < height)
            {
                height = (int)(width / _lockedAspectRatio);
            }
            else
            {
                width = (int)(height * _lockedAspectRatio);
            }
        }

        // 限制在图像范围内
        x = Math.Clamp(x, 0, _imageWidth);
        y = Math.Clamp(y, 0, _imageHeight);
        width = Math.Clamp(width, 0, _imageWidth - x);
        height = Math.Clamp(height, 0, _imageHeight - y);

        _cropRegion = new CropRegion
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            OriginalWidth = _imageWidth,
            OriginalHeight = _imageHeight
        };

        // 触发裁剪区域变化事件
        CropRegionChanged?.Invoke(this, _cropRegion);
    }

    /// <summary>
    /// 获取指定位置的调整手柄
    /// </summary>
    private ResizeHandle GetResizeHandleAt(float imageX, float imageY)
    {
        if (_cropRegion == null) return ResizeHandle.None;

        // 计算手柄在图像坐标系中的大小
        float handleSizeInImage = (float)(HandleSize / _scale);
        float halfSize = handleSizeInImage / 2;

        // 检查 8 个手柄
        var handles = new[]
        {
            new { X = _cropRegion.X, Y = _cropRegion.Y, Handle = ResizeHandle.TopLeft },
            new { X = _cropRegion.X + _cropRegion.Width, Y = _cropRegion.Y, Handle = ResizeHandle.TopRight },
            new { X = _cropRegion.X, Y = _cropRegion.Y + _cropRegion.Height, Handle = ResizeHandle.BottomLeft },
            new { X = _cropRegion.X + _cropRegion.Width, Y = _cropRegion.Y + _cropRegion.Height, Handle = ResizeHandle.BottomRight },
            new { X = _cropRegion.X + _cropRegion.Width / 2, Y = _cropRegion.Y, Handle = ResizeHandle.Top },
            new { X = _cropRegion.X + _cropRegion.Width / 2, Y = _cropRegion.Y + _cropRegion.Height, Handle = ResizeHandle.Bottom },
            new { X = _cropRegion.X, Y = _cropRegion.Y + _cropRegion.Height / 2, Handle = ResizeHandle.Left },
            new { X = _cropRegion.X + _cropRegion.Width, Y = _cropRegion.Y + _cropRegion.Height / 2, Handle = ResizeHandle.Right }
        };

        foreach (var handle in handles)
        {
            if (Math.Abs(imageX - handle.X) <= halfSize && Math.Abs(imageY - handle.Y) <= halfSize)
            {
                return handle.Handle;
            }
        }

        return ResizeHandle.None;
    }

    /// <summary>
    /// 根据手柄调整裁剪区域
    /// </summary>
    private void ResizeCropRegion(ResizeHandle handle, float imageX, float imageY)
    {
        if (_cropRegion == null) return;

        int x = _cropRegion.X;
        int y = _cropRegion.Y;
        int width = _cropRegion.Width;
        int height = _cropRegion.Height;

        // 根据手柄类型调整
        switch (handle)
        {
            case ResizeHandle.TopLeft:
                int newX = (int)imageX;
                int newY = (int)imageY;
                width = x + width - newX;
                height = y + height - newY;
                x = newX;
                y = newY;
                break;

            case ResizeHandle.TopRight:
                width = (int)imageX - x;
                int newY2 = (int)imageY;
                height = y + height - newY2;
                y = newY2;
                break;

            case ResizeHandle.BottomLeft:
                int newX3 = (int)imageX;
                width = x + width - newX3;
                x = newX3;
                height = (int)imageY - y;
                break;

            case ResizeHandle.BottomRight:
                width = (int)imageX - x;
                height = (int)imageY - y;
                break;

            case ResizeHandle.Top:
                int newY5 = (int)imageY;
                height = y + height - newY5;
                y = newY5;
                break;

            case ResizeHandle.Bottom:
                height = (int)imageY - y;
                break;

            case ResizeHandle.Left:
                int newX7 = (int)imageX;
                width = x + width - newX7;
                x = newX7;
                break;

            case ResizeHandle.Right:
                width = (int)imageX - x;
                break;
        }

        // 如果按下 Shift，锁定宽高比
        if (_isShiftPressed && _lockedAspectRatio > 0)
        {
            // 根据手柄类型决定以哪个边为基准
            if (handle == ResizeHandle.Top || handle == ResizeHandle.Bottom)
            {
                // 调整高度时，以高度为基准调整宽度
                int newWidth = (int)(height * _lockedAspectRatio);
                int widthDiff = newWidth - width;
                width = newWidth;
                x -= widthDiff / 2; // 居中调整
            }
            else if (handle == ResizeHandle.Left || handle == ResizeHandle.Right)
            {
                // 调整宽度时，以宽度为基准调整高度
                int newHeight = (int)(width / _lockedAspectRatio);
                int heightDiff = newHeight - height;
                height = newHeight;
                y -= heightDiff / 2; // 居中调整
            }
            else
            {
                // 顶点调整时，以较大的变化为基准
                double widthRatio = Math.Abs(width) / (double)_cropRegion.Width;
                double heightRatio = Math.Abs(height) / (double)_cropRegion.Height;

                if (widthRatio > heightRatio)
                {
                    height = (int)(width / _lockedAspectRatio);
                }
                else
                {
                    width = (int)(height * _lockedAspectRatio);
                }
            }
        }

        // 确保宽高为正
        if (width < 0)
        {
            x += width;
            width = -width;
        }
        if (height < 0)
        {
            y += height;
            height = -height;
        }

        // 限制在图像范围内
        x = Math.Clamp(x, 0, _imageWidth);
        y = Math.Clamp(y, 0, _imageHeight);
        width = Math.Clamp(width, 1, _imageWidth - x);
        height = Math.Clamp(height, 1, _imageHeight - y);

        _cropRegion = new CropRegion
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            OriginalWidth = _imageWidth,
            OriginalHeight = _imageHeight
        };

        // 触发裁剪区域变化事件
        CropRegionChanged?.Invoke(this, _cropRegion);
    }
}
