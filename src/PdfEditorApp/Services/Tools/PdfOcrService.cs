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

using System.Text;
using PdfEditorApp.Services.Ocr;

namespace PdfEditorApp.Services.Tools;

public interface IPdfOcrService
{
    Task<ToolExecutionResult> OcrPdfAsync(OcrToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ScanToPdfAsync(ScanToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
}

public class PdfOcrService : IPdfOcrService
{
    private readonly ICompositeOcrProvider _ocrProvider;

    public PdfOcrService(ICompositeOcrProvider? ocrProvider = null)
    {
        _ocrProvider = ocrProvider ?? CompositeOcrProvider.Default;
    }

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
            progress?.Report(10.0);

            using var pdfPigDoc = UglyToad.PdfPig.PdfDocument.Open(options.InputFilePath);
            try { UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.AddSkiaPageFactory(pdfPigDoc); } catch { }

            int totalPages = pdfPigDoc.NumberOfPages;
            int recognizedWords = 0;
            int pagesWithoutText = 0;
            var scannedPageIndices = new List<int>();

            for (int p = 1; p <= totalPages; p++)
            {
                ct.ThrowIfCancellationRequested();
                var pigPage = pdfPigDoc.GetPage(p);
                var words = pigPage.GetWords().ToList();
                recognizedWords += words.Count;
                if (words.Count == 0)
                {
                    pagesWithoutText++;
                    scannedPageIndices.Add(p);
                }

                progress?.Report(10.0 + (p / (double)totalPages * 15.0));
            }

            if (pagesWithoutText == 0)
            {
                // Document is already fully searchable
                if (!string.Equals(Path.GetFullPath(options.InputFilePath), Path.GetFullPath(outPath), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(options.InputFilePath, outPath, overwrite: true);
                }
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

            // Perform true OCR on scanned pages
            string txtOutPath = Path.ChangeExtension(outPath, ".txt");
            var allText = new StringBuilder();
            int ocrWordsAdded = 0;
            string lastEngineUsed = _ocrProvider.EngineName;

            for (int p = 1; p <= totalPages; p++)
            {
                ct.ThrowIfCancellationRequested();
                var pigPage = pdfPigDoc.GetPage(p);
                var words = pigPage.GetWords().ToList();

                if (words.Count > 0)
                {
                    allText.AppendLine($"--- Page {p} ---");
                    allText.AppendLine(pigPage.Text);
                }
                else
                {
                    using var pngStream = UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.GetPageAsPng(pdfPigDoc, p, 1.5f, 100);
                    if (pngStream != null && pngStream.Length > 0)
                    {
                        var ocrRes = _ocrProvider.RecognizeTextAsync(pngStream.ToArray(), options.Language, ct).GetAwaiter().GetResult();
                        if (ocrRes.Success)
                        {
                            allText.AppendLine($"--- Page {p} (OCR) ---");
                            allText.AppendLine(ocrRes.FullText);
                            recognizedWords += ocrRes.Words.Count;
                            ocrWordsAdded += ocrRes.Words.Count;
                            lastEngineUsed = ocrRes.EngineUsed;
                        }
                    }
                }

                progress?.Report(25.0 + (p / (double)totalPages * 35.0));
            }

            if (options.GenerateTextFile || options.ExtractTextOnly)
            {
                File.WriteAllText(txtOutPath, allText.ToString(), Encoding.UTF8);
            }

            if (options.ExtractTextOnly)
            {
                progress?.Report(100.0);
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = txtOutPath,
                    OutputFiles = new List<string> { txtOutPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = new FileInfo(txtOutPath).Length,
                    Message = $"Extracted text from {totalPages} page(s) ({recognizedWords} words) into {Path.GetFileName(txtOutPath)}."
                };
            }

            // Create searchable PDF with invisible text layer
            using var outPdfDoc = PdfSharpCore.Pdf.IO.PdfReader.Open(options.InputFilePath, PdfDocumentOpenMode.Modify);

            for (int i = 0; i < scannedPageIndices.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                int p = scannedPageIndices[i];

                using var pngStream = UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.GetPageAsPng(pdfPigDoc, p, 1.5f, 100);
                if (pngStream == null || pngStream.Length == 0) continue;

                var ocrRes = _ocrProvider.RecognizeTextAsync(pngStream.ToArray(), options.Language, ct).GetAwaiter().GetResult();
                if (!ocrRes.Success || ocrRes.Words.Count == 0) continue;

                var sharpPage = outPdfDoc.Pages[p - 1];
                double pw = sharpPage.Width.Point;
                double ph = sharpPage.Height.Point;

                using var gfx = XGraphics.FromPdfPage(sharpPage, XGraphicsPdfPageOptions.Append);
                var transparentBrush = new XSolidBrush(XColor.FromArgb(0, 0, 0, 0));

                foreach (var word in ocrRes.Words)
                {
                    double wx = word.NormalizedBounds.X * pw;
                    double wy = word.NormalizedBounds.Y * ph;
                    double ww = Math.Max(1, word.NormalizedBounds.Width * pw);
                    double wh = Math.Max(1, word.NormalizedBounds.Height * ph);

                    double fontSize = Math.Max(4, Math.Min(72, wh * 0.85));
                    var font = new XFont("Helvetica", fontSize, XFontStyle.Regular);
                    gfx.DrawString(word.Text, font, transparentBrush, new XPoint(wx, wy + wh * 0.85));
                }

                progress?.Report(60.0 + ((i + 1) / (double)scannedPageIndices.Count * 38.0));
            }

            outPdfDoc.Save(outPath);
            progress?.Report(100.0);

            long finalBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            var outFiles = new List<string> { outPath };
            if (options.GenerateTextFile && File.Exists(txtOutPath))
            {
                outFiles.Add(txtOutPath);
            }

            string resultMsg = options.GenerateTextFile && File.Exists(txtOutPath)
                ? $"OCR completed using {lastEngineUsed}: recognized and indexed {ocrWordsAdded} words across {pagesWithoutText} scanned page(s). Created Searchable PDF & Text File."
                : $"OCR completed successfully using {lastEngineUsed}: recognized and indexed {ocrWordsAdded} words across {pagesWithoutText} scanned page(s).";

            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = outFiles,
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = finalBytes,
                Message = resultMsg
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
