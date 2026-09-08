using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using UglyToad.PdfPig.Writer;
using PdfEditorApp.Core.Utils;
using PdfEditorApp.Services; // AppLogService — diagnostic logging

namespace PdfEditorApp.Services.Tools.Core;

/// <summary>
/// Robust PDF file reader and sanitizer.
/// Gracefully opens modern, web-exported, linearized, or non-standard PDF files.
/// </summary>
public static class PdfFileHelper
{
    public static void SetFryPdfMetadata(
        PdfSharpCore.Pdf.PdfDocument doc,
        string? title = null,
        string? author = null,
        string? subject = null,
        string? keywords = null,
        string? creator = null,
        string? producer = null)
    {
        if (doc == null) return;
        if (!string.IsNullOrEmpty(title) && string.IsNullOrEmpty(doc.Info.Title)) doc.Info.Title = title;
        if (!string.IsNullOrEmpty(author) && string.IsNullOrEmpty(doc.Info.Author)) doc.Info.Author = author;
        if (!string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(doc.Info.Subject)) doc.Info.Subject = subject;
        if (!string.IsNullOrEmpty(keywords) && string.IsNullOrEmpty(doc.Info.Keywords)) doc.Info.Keywords = keywords;
        doc.Info.Creator = !string.IsNullOrWhiteSpace(creator) ? creator : "FryPDF";
        try
        {
            doc.Info.Elements.SetString("/Producer", !string.IsNullOrWhiteSpace(producer) ? producer : "codefrydev.in");
        }
        catch
        {
            try { doc.Info.Elements["/Producer"] = new PdfString(!string.IsNullOrWhiteSpace(producer) ? producer : "codefrydev.in"); } catch { }
        }
    }

    public static void SaveDocumentWithFryPdfMetadata(
        PdfSharpCore.Pdf.PdfDocument doc,
        string filePath,
        string? title = null,
        string? author = null,
        string? subject = null,
        string? keywords = null,
        string? creator = "FryPDF",
        string? producer = "codefrydev.in")
    {
        SetFryPdfMetadata(doc, title, author, subject, keywords, creator, producer);

        // Write through a temp file and swap it in atomically. Previously the document was
        // saved directly to filePath and *then* rewritten in place to patch a ~20-byte
        // /Producer string; a failure partway through that rewrite (disk full, AV lock,
        // cancellation) left a truncated PDF at the exact path just reported as saved.
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempPath = filePath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            doc.Save(tempPath);
            PatchProducerInFile(tempPath, producer ?? "codefrydev.in");

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }
        }
        catch
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* best effort */ }
            throw;
        }
    }

    /// <summary>
    /// Rewrites the /Producer string inside an already-written PDF.
    /// </summary>
    /// <remarks>
    /// Failures are logged and swallowed deliberately: the producer string is cosmetic, and
    /// callers reach this only via <see cref="SaveDocumentWithFryPdfMetadata"/>, which writes
    /// to a temp file — so a failure here cannot corrupt the caller's destination file.
    /// </remarks>
    public static void PatchProducerInFile(string filePath, string producer = "codefrydev.in")
    {
        if (!File.Exists(filePath)) return;
        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            byte[] updated = PatchProducerInBytes(bytes, producer);
            if (updated != null && updated.Length > 0)
            {
                File.WriteAllBytes(filePath, updated);
            }
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogWarning("PdfSave",
                $"Could not patch /Producer in '{filePath}'; the PDF itself is unaffected", ex);
        }
    }

    /// <summary>
    /// Rewrites the /Producer string inside a PDF byte buffer.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="PdfDocumentSanitizer"/>. This was a verbatim copy of that
    /// implementation, so the same defect had to be fixed twice.
    /// </remarks>
    public static byte[] PatchProducerInBytes(byte[] bytes, string producer = "codefrydev.in")
        => PdfDocumentSanitizer.PatchProducerInBytes(bytes, producer);

    public static PdfSharpCore.Pdf.PdfDocument OpenDocumentSafely(string filePath, PdfDocumentOpenMode mode = PdfDocumentOpenMode.Import, string? password = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        // Four-stage salvage ladder. Each stage used to discard its exception, so when all
        // four failed the user was shown stage four's error — almost never the real cause.
        // Every stage is now logged, and the buffer each stage allocated is released when
        // that stage fails (on success PdfSharpCore keeps reading from the stream, so it is
        // deliberately left open and owned by the returned document).
        string fileName = Path.GetFileName(filePath);

        // 1. Direct open attempt
        try
        {
            if (string.IsNullOrEmpty(password))
                return PdfReader.Open(filePath, mode);
            else
                return PdfReader.Open(filePath, password, mode);
        }
        catch (Exception directEx)
        {
            AppLogService.Instance.LogWarning("PdfOpen",
                $"Stage 1 (direct open) failed for '{fileName}'; sanitizing and retrying", directEx);
        }

        // 2. Read bytes and sanitize trailing garbage or whitespace
        byte[] sanitized = SanitizePdfBytes(File.ReadAllBytes(filePath));

        var sanitizedStream = new MemoryStream(sanitized);
        try
        {
            return string.IsNullOrEmpty(password)
                ? PdfReader.Open(sanitizedStream, mode)
                : PdfReader.Open(sanitizedStream, password, mode);
        }
        catch (Exception sanitizeEx)
        {
            sanitizedStream.Dispose();
            AppLogService.Instance.LogWarning("PdfOpen",
                $"Stage 2 (sanitized bytes) failed for '{fileName}'; rebuilding with PdfPig", sanitizeEx);
        }

        // 3. Reconstruct using the PdfPig builder (handles modern cross-reference streams
        //    and non-standard xrefs)
        MemoryStream? rebuiltStream = null;
        try
        {
            byte[] rebuilt = ReconstructCleanPdfWithPdfPig(filePath);
            if (rebuilt.Length > 0)
            {
                rebuiltStream = new MemoryStream(rebuilt);
                return string.IsNullOrEmpty(password)
                    ? PdfReader.Open(rebuiltStream, mode)
                    : PdfReader.Open(rebuiltStream, password, mode);
            }
        }
        catch (Exception rebuildEx)
        {
            rebuiltStream?.Dispose();
            AppLogService.Instance.LogWarning("PdfOpen",
                $"Stage 3 (PdfPig rebuild) failed for '{fileName}'; synthesizing a trailer", rebuildEx);
        }

        // 4. Synthesize a trailer. This stage's exception is the one that reaches the caller,
        //    as before — but stages 1-3 are now in the diagnostic log alongside it.
        var repairedStream = new MemoryStream(SalvageAndRepairPdfBytes(sanitized));
        try
        {
            return string.IsNullOrEmpty(password)
                ? PdfReader.Open(repairedStream, mode)
                : PdfReader.Open(repairedStream, password, mode);
        }
        catch
        {
            repairedStream.Dispose();
            throw;
        }
    }

    public static int InspectPageCountSafely(string filePath)
    {
        if (!File.Exists(filePath)) return 0;

        // 1. Try modern PdfPig parser (handles all PDF versions & formats)
        try
        {
            using var pig = UglyToad.PdfPig.PdfDocument.Open(filePath);
            return pig.NumberOfPages;
        }
        catch
        {
            // 2. Try OpenDocumentSafely
            try
            {
                using var doc = OpenDocumentSafely(filePath, PdfDocumentOpenMode.Import);
                return doc.PageCount;
            }
            catch
            {
                // 3. Regex token counting fallback
                try
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    string text = Encoding.ASCII.GetString(bytes);
                    int count = 0;
                    int idx = 0;
                    while ((idx = text.IndexOf("/Type /Page", idx, StringComparison.Ordinal)) != -1)
                    {
                        if (idx + 11 >= text.Length || text[idx + 11] != 's')
                        {
                            count++;
                        }
                        idx += 11;
                    }
                    return Math.Max(1, count);
                }
                catch
                {
                    return 1;
                }
            }
        }
    }

    /// <summary>
    /// Rebuilds a clean PDF by re-emitting every page through PdfPig's document builder.
    /// </summary>
    public static byte[] ReconstructCleanPdfWithPdfPig(string filePath)
        => PdfDocumentSanitizer.ReconstructCleanPdfWithPdfPig(filePath);
    /// <summary>
    /// Trims trailing garbage after the last %%EOF, or appends a terminator when none exists.
    /// </summary>
    public static byte[] SanitizePdfBytes(byte[] rawBytes)
        => PdfDocumentSanitizer.SanitizePdfBytes(rawBytes);
    /// <summary>
    /// Synthesizes a trailer for a PDF whose own trailer is missing.
    /// </summary>
    /// <remarks>
    /// The copy that lived here was missing the null/length guard the Core implementation has,
    /// so it threw a NullReferenceException on a null buffer instead of returning safely.
    /// </remarks>
    public static byte[] SalvageAndRepairPdfBytes(byte[] rawBytes)
        => PdfDocumentSanitizer.SalvageAndRepairPdfBytes(rawBytes);
    /// <summary>
    /// Checks whether an existing file is currently locked or cannot be opened for writing by the current process.
    /// Returns false if the file does not exist.
    /// </summary>
    public static bool IsFileLocked(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves a safe, non-conflicting, and writable destination file path.
    /// If requestedPath is supplied and non-empty, it is returned directly.
    /// Otherwise, checks defaultDirectory for baseFileName.extension.
    /// If that file already exists and is locked or matches any path in avoidPaths,
    /// appends an incrementing index (_1, _2, ...) until an available and unlocked path is found.
    /// </summary>
    public static string ResolveSafeOutputPath(
        string? requestedPath,
        string defaultDirectory,
        string baseFileName,
        string extension = ".pdf",
        IEnumerable<string>? avoidPaths = null)
    {
        if (!string.IsNullOrWhiteSpace(requestedPath))
        {
            return requestedPath;
        }

        if (!extension.StartsWith(".", StringComparison.Ordinal))
        {
            extension = "." + extension;
        }

        string dir = !string.IsNullOrWhiteSpace(defaultDirectory) && Directory.Exists(defaultDirectory)
            ? defaultDirectory
            : Path.GetTempPath();

        var avoidList = avoidPaths?
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Path.GetFullPath(p))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool ShouldAvoid(string path)
        {
            string fullPath = Path.GetFullPath(path);
            if (avoidList != null && avoidList.Contains(fullPath)) return true;
            if (File.Exists(path) && IsFileLocked(path)) return true;
            return false;
        }

        string candidate = Path.Combine(dir, $"{baseFileName}{extension}");
        if (!ShouldAvoid(candidate))
        {
            return candidate;
        }

        for (int i = 1; i <= 1000; i++)
        {
            candidate = Path.Combine(dir, $"{baseFileName}_{i}{extension}");
            if (!ShouldAvoid(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(dir, $"{baseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
    }
}
