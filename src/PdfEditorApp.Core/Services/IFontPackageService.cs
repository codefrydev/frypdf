using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Core.Services;

/// <summary>
/// Service managing on-demand language and font packs for international and creative PDF rendering.
/// Keeps the application lightweight while enabling global script support through local caching.
/// </summary>
public interface IFontPackageService
{
    /// <summary>
    /// Event fired whenever fonts are downloaded, imported, or deleted.
    /// </summary>
    event Action? FontLibraryChanged;

    /// <summary>
    /// Returns the catalog of all available font packages.
    /// </summary>
    IReadOnlyList<FontPackageInfo> GetAllPackages();

    /// <summary>
    /// Returns the local user directory where downloaded/custom fonts are stored.
    /// </summary>
    string GetUserFontDirectory();

    /// <summary>
    /// Checks if a package's font files are present on disk.
    /// </summary>
    bool IsPackageInstalled(FontPackageInfo package);

    /// <summary>
    /// Downloads all font files in a package asynchronously with progress reporting.
    /// </summary>
    Task<bool> DownloadPackageAsync(
        FontPackageInfo package,
        IProgress<double>? progress = null,
        Action<string>? statusCallback = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a downloaded font package from local disk to reclaim space.
    /// </summary>
    Task<bool> DeletePackageAsync(FontPackageInfo package);

    /// <summary>
    /// Calculates the total disk space used by downloaded and custom fonts.
    /// </summary>
    Task<long> GetTotalCacheSizeBytesAsync();

    /// <summary>
    /// Deletes all downloaded font cache files.
    /// </summary>
    Task ClearAllCacheAsync();

    /// <summary>
    /// Imports a user-supplied custom .ttf / .otf font file into the local font directory.
    /// </summary>
    Task<bool> ImportCustomFontAsync(string sourceFilePath);

    /// <summary>
    /// Scans text for non-Latin script characters and returns the corresponding font package if not yet installed.
    /// </summary>
    FontPackageInfo? DetectMissingPackageForText(string text);

    /// <summary>
    /// Returns all font file paths available across embedded and downloaded directories.
    /// </summary>
    IEnumerable<string> GetAllAvailableFontFilePaths();
}
