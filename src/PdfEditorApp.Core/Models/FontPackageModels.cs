using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Models;

/// <summary>
/// Categories for grouping language and font packages.
/// </summary>
public enum FontPackageCategory
{
    All,
    EastAsia,
    SouthAsia,
    MiddleEast,
    SoutheastAsia,
    EuropeAndEurasia,
    DesignAndTypography
}

/// <summary>
/// Status of an on-demand font package.
/// </summary>
public enum FontPackageStatus
{
    NotInstalled,
    Downloading,
    Installed,
    UpdateAvailable,
    Error
}

/// <summary>
/// Metadata for an individual font file inside a package.
/// </summary>
public class FontFileItem
{
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FontFamilyName { get; set; } = string.Empty;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
}

/// <summary>
/// Representation of a language or font pack available for on-demand installation.
/// </summary>
public class FontPackageInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string FlagEmoji { get; set; } = "🌐";
    public string Region { get; set; } = string.Empty;
    public FontPackageCategory Category { get; set; } = FontPackageCategory.EastAsia;
    public string Description { get; set; } = string.Empty;
    public string SampleText { get; set; } = string.Empty;
    public List<string> SupportedLanguages { get; set; } = new();
    public List<string> IncludedFontFamilies { get; set; } = new();
    public List<FontFileItem> Files { get; set; } = new();
    public long TotalEstimatedSizeBytes { get; set; }
    public string FormattedSize => FormatBytes(TotalEstimatedSizeBytes);
    public bool IsBundledByDefault { get; set; }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 KB";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
