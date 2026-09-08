using System;
using System.IO;

namespace PdfEditorApp.Services;

/// <summary>
/// Serializes access to QuestPDF's process-wide font registry.
/// </summary>
/// <remarks>
/// <c>QuestPDF.Drawing.FontManager</c> is global mutable state. Fonts are registered from the
/// export service's static initializer (on whichever thread exports first) and from the font
/// package service's download and import paths (on thread-pool threads), so a font download
/// completing mid-export mutated the registry while the export was reading it.
/// Every registration in the app goes through here.
/// </remarks>
public static class QuestPdfFontRegistry
{
    private static readonly object Gate = new();

    /// <summary>
    /// Registers a font stream with QuestPDF under the shared lock.
    /// </summary>
    public static void Register(Stream fontStream)
    {
        ArgumentNullException.ThrowIfNull(fontStream);

        lock (Gate)
        {
            QuestPDF.Drawing.FontManager.RegisterFont(fontStream);
        }
    }

    /// <summary>
    /// Registers the font file at <paramref name="path"/>, returning false if it cannot be read
    /// or parsed. Failures are logged rather than thrown: a single unusable font file should not
    /// abort a bulk registration pass.
    /// </summary>
    public static bool TryRegisterFile(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Register(stream);
            return true;
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogWarning("Fonts", $"Could not register font '{path}' with QuestPDF", ex);
            return false;
        }
    }
}
