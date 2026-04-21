using System.Collections.Generic;
using Core.Models;

namespace App.Views;

public sealed partial class MainPage
{
    private enum ImageTemplateSourceKind
    {
        Crop,
        File
    }

    private sealed class SuccessfulImageMatchContext
    {
        public required ImageTemplateSourceKind TemplateSourceKind { get; init; }

        public string? TemplatePath { get; set; }

        public required CropRegion ReferenceBounds { get; init; }

        public required CropRegion SearchRegion { get; init; }

        public required int[] RegionRef { get; init; }

        public required string Orientation { get; init; }

        public required MatchResult MatchResult { get; init; }
    }

    private sealed class ExternalScreenshotPreviewSnapshot
    {
        public required byte[] ImageBytes { get; init; }

        public required int Width { get; init; }

        public required int Height { get; init; }

        public required float Scale { get; init; }

        public required float OffsetX { get; init; }

        public required float OffsetY { get; init; }

        public required bool IsFitToWindowMode { get; init; }

        public required CropRegion? CropRegion { get; init; }

        public required string MatchSummaryText { get; init; }

        public required string RegionRefText { get; init; }

        public required bool TemplateSourceCropChecked { get; init; }

        public required bool ScreenshotSourceCurrentChecked { get; init; }

        public required string? ScreenshotFilePath { get; init; }

        public required string CanvasSourceSummary { get; init; }

        public required List<MatchResult> MatchResults { get; init; }
    }

    private sealed class CropImageSourceContext
    {
        public required byte[] ImageBytes { get; init; }

        public required int Width { get; init; }

        public required int Height { get; init; }

        public required CropRegion CropRegion { get; init; }
    }

    private SuccessfulImageMatchContext? _lastSuccessfulMatchContext;
    private ExternalScreenshotPreviewSnapshot? _externalScreenshotPreviewSnapshot;
    private string? _savedCropTemplatePath;
    private string _currentCanvasSourceSummary = "当前画布：尚未加载截图";
    private bool _suspendCropStateTracking;
}
