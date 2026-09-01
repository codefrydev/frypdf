using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using UglyToad.PdfPig;

namespace PdfEditorApp.Services.Tools;

public interface IPdfOcrService
{
    Task<ToolExecutionResult> OcrPdfAsync(OcrToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ScanToPdfAsync(ScanToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
}

public class PdfOcrService : IPdfOcrService
{
    public async Task<ToolExecutionResult> OcrPdfAsync(OcrToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Searchable.pdf");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(15.0);

            // Read pages and analyze existing text glyphs. Note: this indexes text PdfPig
            // already finds embedded in the page — it does not perform true image-to-text
            // OCR recognition on scanned/image-only pages. A page that already has embedded
            // text is already selectable, so no duplicate overlay is drawn for it either way.
            using var pdfPigDoc = UglyToad.PdfPig.PdfDocument.Open(options.InputFilePath);

            int totalPages = pdfPigDoc.NumberOfPages;
            int recognizedWords = 0;
            int pagesWithoutText = 0;

            for (int p = 1; p <= totalPages; p++)
            {
                ct.ThrowIfCancellationRequested();
                var pigPage = pdfPigDoc.GetPage(p);
                var words = pigPage.GetWords().ToList();
                recognizedWords += words.Count;
                if (words.Count == 0) pagesWithoutText++;

                progress?.Report(15.0 + (p / (double)totalPages * 75.0));
            }

            progress?.Report(100.0);

            if (pagesWithoutText == 0)
            {
                // No new text layer is needed — the document is already fully searchable.
                // Still produce the expected output file (a straight copy) so Save/Open/
                // preview flows downstream have something to point at.
                File.Copy(options.InputFilePath, outPath, overwrite: true);
                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = $"All {totalPages} page(s) already contain selectable text ({recognizedWords} words) — this document was already searchable."
                };
            }

            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = pagesWithoutText == totalPages
                    ? $"No extractable text was found on any of the {totalPages} page(s) — this looks like a scanned/image-only document. This tool indexes text that already exists in a PDF; it does not yet perform true image-to-text OCR recognition."
                    : $"{pagesWithoutText} of {totalPages} page(s) have no extractable text (likely scanned images) and could not be made searchable. This tool indexes text that already exists in a PDF; it does not yet perform true image-to-text OCR recognition for those pages."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ScanToPdfAsync(ScanToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (options.InputImageFiles == null || options.InputImageFiles.Count == 0)
                return new ToolExecutionResult { Success = false, ErrorMessage = "No scanned images provided." };

            long totalOriginalBytes = options.InputImageFiles.Where(File.Exists).Sum(f => new FileInfo(f).Length);
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputImageFiles[0]) ?? Path.GetTempPath();
                outPath = Path.Combine(dir, "Scanned_Document.pdf");
            }

            ct.ThrowIfCancellationRequested();
            using var pdfDoc = new PdfSharpCore.Pdf.PdfDocument();
            int total = options.InputImageFiles.Count;

            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                string imgFile = options.InputImageFiles[i];
                if (!File.Exists(imgFile)) continue;

                // Load and enhance scanned image with SkiaSharp
                using var skBitmap = SkiaSharp.SKBitmap.Decode(imgFile);
                if (skBitmap != null)
                {
                    // If enhancement requested (contrast / whitening)
                    using var enhancedSurface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(skBitmap.Width, skBitmap.Height));
                    var canvas = enhancedSurface.Canvas;

                    // Compose the requested enhancement filters. Note: AutoDeskew is not
                    // applied here — real skew correction needs angle detection (e.g. a
                    // Hough transform / projection-profile analysis), which is a separate,
                    // larger piece of work rather than a color filter.
                    SkiaSharp.SKColorFilter? colorFilter = null;
                    void Compose(SkiaSharp.SKColorFilter next)
                    {
                        colorFilter = colorFilter == null ? next : SkiaSharp.SKColorFilter.CreateCompose(next, colorFilter);
                    }

                    if (options.ConvertToGrayscale)
                    {
                        Compose(SkiaSharp.SKColorFilter.CreateColorMatrix(new float[]
                        {
                            0.21f, 0.72f, 0.07f, 0, 0,
                            0.21f, 0.72f, 0.07f, 0, 0,
                            0.21f, 0.72f, 0.07f, 0, 0,
                            0,     0,     0,     1, 0
                        }));
                    }

                    if (options.EnhanceContrast)
                    {
                        const float contrast = 1.25f;
                        float t = 0.5f * (1f - contrast);
                        Compose(SkiaSharp.SKColorFilter.CreateColorMatrix(new float[]
                        {
                            contrast, 0, 0, 0, t,
                            0, contrast, 0, 0, t,
                            0, 0, contrast, 0, t,
                            0, 0, 0, 1, 0
                        }));
                    }

                    if (options.WhitenBackground)
                    {
                        const float brighten = 0.10f;
                        Compose(SkiaSharp.SKColorFilter.CreateColorMatrix(new float[]
                        {
                            1, 0, 0, 0, brighten,
                            0, 1, 0, 0, brighten,
                            0, 0, 1, 0, brighten,
                            0, 0, 0, 1, 0
                        }));
                    }

                    using var paint = new SkiaSharp.SKPaint { ColorFilter = colorFilter };

                    canvas.DrawBitmap(skBitmap, 0, 0, paint);

                    using var enhancedImage = enhancedSurface.Snapshot();
                    using var data = enhancedImage.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 92);

                    using var ms = new MemoryStream();
                    data.SaveTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    var page = pdfDoc.AddPage();
                    var xImage = XImage.FromStream(() => new MemoryStream(ms.ToArray()));

                    using var gfx = XGraphics.FromPdfPage(page);
                    double maxW = page.Width.Point - 40;
                    double maxH = page.Height.Point - 40;
                    double scale = Math.Min(maxW / xImage.PixelWidth, maxH / xImage.PixelHeight);
                    double drawW = xImage.PixelWidth * scale;
                    double drawH = xImage.PixelHeight * scale;
                    double posX = 20 + ((maxW - drawW) / 2.0);
                    double posY = 20 + ((maxH - drawH) / 2.0);

                    gfx.DrawImage(xImage, posX, posY, drawW, drawH);
                }

                progress?.Report((i + 1) / (double)total * 90.0);
            }

            pdfDoc.Save(outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = totalOriginalBytes,
                OutputSizeBytes = outBytes,
                Message = $"Enhanced {total} scanned pages and compiled into PDF: {Path.GetFileName(outPath)}"
            };
        }, ct);
    }
}
