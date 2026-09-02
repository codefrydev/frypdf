using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PdfEditorApp.Core.Models;

/// <summary>
/// Type of OCR engine used for text recognition.
/// </summary>
public enum OcrEngineType
{
    Auto,
    OsNative,
    Tesseract,
    None
}

/// <summary>
/// Lightweight normalized or point bounding box independent of UI framework.
/// </summary>
public readonly record struct OcrBoundingBox(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;

    public static OcrBoundingBox Empty => new(0, 0, 0, 0);

    public bool Contains(double px, double py) =>
        px >= X && px <= Right && py >= Y && py <= Bottom;
}

/// <summary>
/// A single recognized word with bounds normalized between 0.0 and 1.0.
/// </summary>
public class OcrWordItem
{
    public string Text { get; set; } = string.Empty;
    public OcrBoundingBox NormalizedBounds { get; set; }
    public float Confidence { get; set; } = 1.0f;
}

/// <summary>
/// A line of recognized words.
/// </summary>
public class OcrLineItem
{
    public string Text { get; set; } = string.Empty;
    public OcrBoundingBox NormalizedBounds { get; set; }
    public List<OcrWordItem> Words { get; set; } = new();
}

/// <summary>
/// Output of an OCR recognition operation on an image or page.
/// </summary>
public class OcrResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string FullText { get; set; } = string.Empty;
    public List<OcrLineItem> Lines { get; set; } = new();
    public List<OcrWordItem> Words { get; set; } = new();
    public string EngineUsed { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}

/// <summary>
/// Metadata for an on-demand downloadable Tesseract language model.
/// </summary>
public class TesseractLanguagePackageInfo
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = "🌐";
    public string Category { get; set; } = "Latin & European";
    public string Description { get; set; } = string.Empty;
    public string SampleText { get; set; } = string.Empty;
    public long EstimatedSizeBytes { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileName => $"{Code}.traineddata";
    public bool IsInstalled { get; set; }

    public string FormattedSize => FormatBytes(EstimatedSizeBytes);

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 KB";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}

/// <summary>
/// Unified contract for optical character recognition providers.
/// </summary>
public interface IOcrEngine
{
    string EngineName { get; }
    OcrEngineType EngineType { get; }
    bool IsAvailable { get; }
    Task<OcrResult> RecognizeTextAsync(byte[] imageBytes, string language = "eng", CancellationToken ct = default);
}
