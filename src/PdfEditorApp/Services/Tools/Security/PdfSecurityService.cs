using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.Security;
using QuestPDF.Fluent;

namespace PdfEditorApp.Services.Tools.Security;

public interface IPdfSecurityService
{
    Task<ToolExecutionResult> ProtectPdfAsync(SecurityToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> UnlockPdfAsync(UnlockToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> SignPdfAsync(SignToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> RedactPdfAsync(RedactionToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> AddWatermarkAsync(WatermarkToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Read-only: finds regions matching <paramref name="pattern"/> without writing anything.
    /// Lets a preview UI show matches for review before committing to RedactPdfAsync.
    /// </summary>
    Task<List<RedactionRegion>> FindRedactionMatchesAsync(string filePath, string pattern, bool caseSensitive, CancellationToken ct = default);
}

public class PdfSecurityService : IPdfSecurityService
{
    private readonly IPdfPageService _pageService;
    private readonly IQuestPdfOperationsEngine _questEngine;

    public PdfSecurityService(IQuestPdfOperationsEngine? questEngine = null, IPdfPageService? pageService = null)
    {
        _questEngine = questEngine ?? new QuestPdfOperationsEngine();
        _pageService = pageService ?? new PdfPageService(_questEngine);
    }

    public Task<ToolExecutionResult> AddWatermarkAsync(WatermarkToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return _pageService.AddWatermarkAsync(options, progress, ct);
    }

    public async Task<ToolExecutionResult> ProtectPdfAsync(SecurityToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (options.Engine == PdfProcessingEngine.QuestPdfNative)
        {
            var questResult = await _questEngine.EncryptAsync(options, progress, ct);
            if (questResult.Success)
                return questResult;

            System.Diagnostics.Debug.WriteLine($"QuestPDF encryption fallback: {questResult.ErrorMessage}");
        }

        return await ProtectWithPdfSharpAsync(options, progress, ct);
    }

    private async Task<ToolExecutionResult> ProtectWithPdfSharpAsync(SecurityToolOptions options, IProgress<double>? progress, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);
            ct.ThrowIfCancellationRequested();

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Protected.pdf");
            }

            string? targetDir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            using var doc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Modify);
            var security = doc.SecuritySettings;

            string userPwd = options.UserPassword ?? string.Empty;
            string ownerPwd = options.OwnerPassword ?? string.Empty;
            string? generatedOwnerPassword = null;

            if (!string.IsNullOrEmpty(userPwd))
                security.UserPassword = userPwd;

            if (!string.IsNullOrEmpty(ownerPwd))
            {
                security.OwnerPassword = ownerPwd;
            }
            else if (!string.IsNullOrEmpty(userPwd))
            {
                generatedOwnerPassword = GenerateRandomPassword();
                security.OwnerPassword = generatedOwnerPassword;
            }

            security.PermitPrint = options.AllowPrinting;
            security.PermitModifyDocument = options.AllowModifying;
            security.PermitExtractContent = options.AllowCopying;
            security.PermitAnnotations = options.AllowAnnotating;
            security.PermitFormsFill = options.AllowFormFilling;

            PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, outPath);
            progress?.Report(100.0);

            long stdOutBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = stdOutBytes,
                Message = generatedOwnerPassword != null
                    ? $"Document successfully encrypted and secured with requested permission constraints. No owner password was provided, so a random one was generated (you'll need it later to change permissions): {generatedOwnerPassword}"
                    : "Document successfully encrypted and secured with requested permission constraints."
            };
        }, ct);
    }

    private static string GenerateRandomPassword(int length = 20)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);
        foreach (var b in bytes)
        {
            sb.Append(chars[b % chars.Length]);
        }
        return sb.ToString();
    }

    public async Task<ToolExecutionResult> UnlockPdfAsync(UnlockToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (options.Engine == PdfProcessingEngine.QuestPdfNative)
        {
            var questResult = await _questEngine.DecryptAsync(options, progress, ct);
            if (questResult.Success)
                return questResult;

            System.Diagnostics.Debug.WriteLine($"QuestPDF unlock fallback: {questResult.ErrorMessage}");
        }

        return await UnlockWithPdfSharpAsync(options, progress, ct);
    }

    private async Task<ToolExecutionResult> UnlockWithPdfSharpAsync(UnlockToolOptions options, IProgress<double>? progress, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);
            ct.ThrowIfCancellationRequested();

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Unlocked.pdf");
            }

            string? targetDir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            try
            {
                using var inputDoc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Import, options.Password ?? "");
                using var outputDoc = new PdfDocument();

                PdfFileHelper.SetFryPdfMetadata(outputDoc, inputDoc.Info.Title, inputDoc.Info.Author);

                int pageCount = inputDoc.PageCount;
                for (int i = 0; i < pageCount; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    outputDoc.AddPage(inputDoc.Pages[i]);
                    progress?.Report(20.0 + (i / (double)pageCount * 70.0));
                }

                PdfFileHelper.SaveDocumentWithFryPdfMetadata(outputDoc, outPath);
                progress?.Report(100.0);

                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = "Security restrictions and passwords successfully removed. Output PDF is fully unlocked."
                };
            }
            catch (Exception ex)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to unlock PDF with provided credentials: {ex.Message}"
                };
            }
        }, ct);
    }

    public async Task<ToolExecutionResult> SignPdfAsync(SignToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);

            ct.ThrowIfCancellationRequested();
            using var doc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Modify);

            int targetPageIndex = Math.Max(0, Math.Min(doc.PageCount - 1, options.TargetPageNumber - 1));
            var page = doc.Pages[targetPageIndex];

            using var gfx = XGraphics.FromPdfPage(page);

            // Draw visual signature badge
            double x = options.X;
            double y = options.Y;
            double w = options.Width;
            double h = options.Height;

            // Background badge border
            var rectBrush = new XSolidBrush(XColor.FromArgb(245, 247, 250));
            var borderPen = new XPen(XColor.FromArgb(15, 108, 189), 1.5);
            gfx.DrawRoundedRectangle(borderPen, rectBrush, x, y, w, h, 6, 6);

            // Draw cursive signature text or imported signature image
            if (!string.IsNullOrWhiteSpace(options.SignatureImageDataUri) && options.SignatureImageDataUri.StartsWith("data:image"))
            {
                try
                {
                    int commaIdx = options.SignatureImageDataUri.IndexOf(',');
                    string b64 = commaIdx >= 0 ? options.SignatureImageDataUri.Substring(commaIdx + 1) : options.SignatureImageDataUri;
                    byte[] imgBytes = Convert.FromBase64String(b64);
                    using var ms = new MemoryStream(imgBytes);
                    var xImg = XImage.FromStream(() => new MemoryStream(imgBytes));
                    gfx.DrawImage(xImg, x + 8, y + 6, w - 16, h - 26);
                }
                catch { }
            }
            else
            {
                string sigFont = options.Style switch
                {
                    SignatureStyle.CursiveElegance => "Times New Roman",
                    SignatureStyle.ClassicScript => "Georgia",
                    SignatureStyle.ModernHandwriting => "Courier New",
                    _ => "Helvetica"
                };

                var cursiveFont = new XFont(sigFont, 18, XFontStyle.Italic | XFontStyle.Bold);
                var cursiveBrush = new XSolidBrush(XColor.FromArgb(15, 108, 189));
                gfx.DrawString(options.SignerName, cursiveFont, cursiveBrush, new XPoint(x + 10, y + 24));
            }

            // Draw cryptographic / verification badge metadata
            var metaFont = new XFont("Helvetica", 8, XFontStyle.Regular);
            var metaBrush = new XSolidBrush(XColor.FromArgb(100, 116, 139));
            string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            string reasonStr = !string.IsNullOrWhiteSpace(options.Reason) ? $" • {options.Reason}" : "";
            gfx.DrawString($"Digitally Verified: {options.SignerName}{reasonStr}", metaFont, metaBrush, new XPoint(x + 10, y + h - 14));
            gfx.DrawString($"Timestamp: {dateStr}", metaFont, metaBrush, new XPoint(x + 10, y + h - 4));

            // If a certificate is provided, read it to label the visual badge with the
            // signer identity it names. Note: this does NOT apply a cryptographic PDF
            // signature (no /ByteRange or signature dictionary is embedded) — it's a
            // visual badge only, so the document is not tamper-evident. Say so honestly
            // rather than claiming "Digitally Signed", which would be a false integrity
            // guarantee for anyone relying on it.
            if (!string.IsNullOrEmpty(options.CertificatePath) && File.Exists(options.CertificatePath))
            {
                try
                {
                    var cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(options.CertificatePath, options.CertificatePassword);
                    doc.Info.Subject = $"Visual signature badge referencing certificate '{cert.SubjectName.Name}' at {dateStr} — not a cryptographic PDF signature";
                }
                catch { }
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Signed.pdf");
            }

            PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new System.Collections.Generic.List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Placed electronic signature for '{options.SignerName}' on page {options.TargetPageNumber}."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> RedactPdfAsync(RedactionToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(async () =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(10.0);

            ct.ThrowIfCancellationRequested();

            // Start from any regions supplied directly (e.g. a UI that already resolved
            // matches or manual marks for the user to review), then add matches for
            // SearchPatternToRedact if one was also given.
            var regions = new List<RedactionRegion>(options.Regions);
            string? pattern = options.SearchPatternToRedact?.Trim();
            if (!string.IsNullOrEmpty(pattern))
            {
                var matched = await FindRedactionMatchesAsync(options.InputFilePath, pattern, options.CaseSensitive, ct);
                regions.AddRange(matched);
            }

            progress?.Report(30.0);

            if (regions.Count == 0)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = string.IsNullOrEmpty(pattern)
                        ? "No redaction regions were specified."
                        : $"No matches for \"{pattern}\" were found in this document — nothing was redacted."
                };
            }

            var regionsByPage = regions
                .Where(r => r.PageIndex >= 0)
                .GroupBy(r => r.PageIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            using var inputDoc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Import);
            using var outputDoc = new PdfDocument();

            // For genuine (not just visual) sanitization, redacted pages are rendered to a
            // raster image with the black boxes baked in, then that image becomes the
            // page's ENTIRE content — the original text/vector data for that page is never
            // copied into the output at all, so it can't be recovered by anyone who removes
            // the black box or runs text extraction. Pages with no matches are left as
            // normal, fully-selectable vector pages.
            UglyToad.PdfPig.PdfDocument? pigForRender = null;
            if (options.PermanentScrubText)
            {
                pigForRender = UglyToad.PdfPig.PdfDocument.Open(options.InputFilePath);
                try { UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.AddSkiaPageFactory(pigForRender); } catch { }
            }

            try
            {
                int totalRedactions = 0;
                int totalPages = inputDoc.PageCount;
                for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
                {
                    ct.ThrowIfCancellationRequested();
                    bool hasRegions = regionsByPage.TryGetValue(pageIndex, out var pageRegions) && pageRegions!.Count > 0;
                    bool flattened = false;

                    if (hasRegions && options.PermanentScrubText && pigForRender != null)
                    {
                        const float scale = 2.0f; // ~144 DPI: legible while keeping file size reasonable
                        using var pngStream = UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.GetPageAsPng(pigForRender, pageIndex + 1, scale, 100);
                        if (pngStream != null && pngStream.Length > 0)
                        {
                            pngStream.Position = 0;
                            using var bitmap = SkiaSharp.SKBitmap.Decode(pngStream);
                            if (bitmap != null)
                            {
                                using (var canvas = new SkiaSharp.SKCanvas(bitmap))
                                using (var blackPaint = new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black, Style = SkiaSharp.SKPaintStyle.Fill })
                                {
                                    foreach (var r in pageRegions!)
                                    {
                                        canvas.DrawRect((float)(r.X * scale), (float)(r.Y * scale), (float)(r.Width * scale), (float)(r.Height * scale), blackPaint);
                                        totalRedactions++;
                                    }
                                }

                                using var flattenedImage = SkiaSharp.SKImage.FromBitmap(bitmap);
                                using var flattenedData = flattenedImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                                var flattenedBytes = flattenedData.ToArray();

                                var srcPage = inputDoc.Pages[pageIndex];
                                var newPage = outputDoc.AddPage();
                                newPage.Width = srcPage.Width;
                                newPage.Height = srcPage.Height;
                                using var gfx = XGraphics.FromPdfPage(newPage);
                                using var xImage = XImage.FromStream(() => new MemoryStream(flattenedBytes));
                                gfx.DrawImage(xImage, 0, 0, newPage.Width.Point, newPage.Height.Point);
                                flattened = true;
                            }
                        }
                    }

                    if (!flattened)
                    {
                        var importedPage = outputDoc.AddPage(inputDoc.Pages[pageIndex]);
                        if (hasRegions)
                        {
                            using var gfx = XGraphics.FromPdfPage(importedPage);
                            var fillBrush = new XSolidBrush(XColors.Black);
                            foreach (var r in pageRegions!)
                            {
                                gfx.DrawRectangle(fillBrush, r.X, r.Y, r.Width, r.Height);
                                if (!string.IsNullOrWhiteSpace(r.Reason) && r.Width > 40 && r.Height > 12)
                                {
                                    var reasonFont = new XFont("Helvetica", Math.Min(9, r.Height * 0.5), XFontStyle.Bold);
                                    var reasonBrush = new XSolidBrush(XColors.White);
                                    gfx.DrawString($"[{r.Reason}]", reasonFont, reasonBrush, new XPoint(r.X + 4, r.Y + (r.Height * 0.7)));
                                }
                                totalRedactions++;
                            }
                        }
                    }

                    progress?.Report(30.0 + (pageIndex / (double)totalPages * 60.0));
                }

                string outPath = options.OutputFilePath;
                if (string.IsNullOrWhiteSpace(outPath))
                {
                    string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                    string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                    outPath = Path.Combine(dir, $"{name}_Redacted.pdf");
                }

                PdfFileHelper.SaveDocumentWithFryPdfMetadata(outputDoc, outPath);
                progress?.Report(100.0);

                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = options.PermanentScrubText
                        ? $"Permanently redacted {totalRedactions} region(s) — affected pages were flattened to images so the underlying text cannot be recovered."
                        : $"Redacted {totalRedactions} region(s) with a visual overlay only — underlying text is still present in the PDF and can be recovered by anyone who removes the overlay. Enable \"Permanent Deep Content Sanitization\" for irrecoverable redaction."
                };
            }
            finally
            {
                pigForRender?.Dispose();
            }
        }, ct);
    }

    public async Task<List<RedactionRegion>> FindRedactionMatchesAsync(string filePath, string pattern, bool caseSensitive, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var regions = new List<RedactionRegion>();
            string? trimmed = pattern?.Trim();
            if (string.IsNullOrEmpty(trimmed) || !File.Exists(filePath)) return regions;

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            string[] patternWords = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            void AddMatchRegions(int pageIndex, double pageHeight, IEnumerable<UglyToad.PdfPig.Content.Word> matchedWords)
            {
                var lineGroups = matchedWords
                    .GroupBy(w => Math.Round(w.BoundingBox.Bottom / 4.0) * 4.0)
                    .OrderByDescending(g => g.Key);

                foreach (var line in lineGroups)
                {
                    double left = line.Min(w => w.BoundingBox.Left);
                    double right = line.Max(w => w.BoundingBox.Right);
                    double top = line.Max(w => w.BoundingBox.Top);
                    double bottom = line.Min(w => w.BoundingBox.Bottom);

                    regions.Add(new RedactionRegion
                    {
                        PageIndex = pageIndex,
                        X = left,
                        Y = Math.Max(0, pageHeight - top),
                        Width = Math.Max(1, right - left),
                        Height = Math.Max(1, top - bottom),
                        Reason = "Pattern match"
                    });
                }
            }

            using var pigDoc = UglyToad.PdfPig.PdfDocument.Open(filePath);
            for (int p = 1; p <= pigDoc.NumberOfPages; p++)
            {
                ct.ThrowIfCancellationRequested();
                var pigPage = pigDoc.GetPage(p);
                double pageHeight = pigPage.Height;
                var words = pigPage.GetWords().ToList();

                if (patternWords.Length > 1)
                {
                    for (int i = 0; i + patternWords.Length <= words.Count; i++)
                    {
                        bool match = true;
                        for (int k = 0; k < patternWords.Length; k++)
                        {
                            if (!string.Equals(words[i + k].Text, patternWords[k], comparison)) { match = false; break; }
                        }
                        if (match)
                        {
                            var matchedSubList = words.GetRange(i, patternWords.Length);
                            AddMatchRegions(p - 1, pageHeight, matchedSubList);
                        }
                    }
                }
                else if (patternWords.Length == 1)
                {
                    foreach (var w in words)
                    {
                        if (w.Text.IndexOf(patternWords[0], comparison) >= 0)
                        {
                            AddMatchRegions(p - 1, pageHeight, new[] { w });
                        }
                    }
                }
            }

            return regions;
        }, ct);
    }
}
