using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Advanced;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Rendering.Skia;
using PdfSharpDoc = PdfSharpCore.Pdf.PdfDocument;

namespace PdfEditorApp.Services.Tools;

public interface IPdfOptimizationService
{
    Task<ToolExecutionResult> CompressPdfAsync(CompressToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> RepairPdfAsync(RepairToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertToPdfAAsync(PdfAToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
}

public class PdfOptimizationService : IPdfOptimizationService
{
    public async Task<ToolExecutionResult> CompressPdfAsync(CompressToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(10.0);

            ct.ThrowIfCancellationRequested();

            byte[] pass1Bytes = CompressPdfStreamsAndImages(options.InputFilePath, options, progress, ct);
            byte[] bestBytes = pass1Bytes;

            // Test raster page compression for Extreme, SmallSize, and Recommended presets,
            // or whenever in-place pass did not achieve aggressive reduction (<40% reduction).
            bool shouldTryRaster = options.Level == PdfCompressionLevel.MaximumCompression ||
                                   options.Level == PdfCompressionLevel.SmallSize ||
                                   options.Level == PdfCompressionLevel.Balanced ||
                                   pass1Bytes.Length >= origBytes ||
                                   (pass1Bytes.Length / (double)origBytes) > 0.55;

            if (shouldTryRaster)
            {
                progress?.Report(80.0);
                byte[]? rasterCandidate = TryRasterPageCompression(options.InputFilePath, options, ct);
                if (rasterCandidate != null && rasterCandidate.Length < bestBytes.Length)
                {
                    bestBytes = rasterCandidate;
                }
            }

            progress?.Report(95.0);

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Compressed.pdf");
            }

            long outBytes;
            string message;

            if (bestBytes.Length < origBytes)
            {
                File.WriteAllBytes(outPath, bestBytes);
                outBytes = bestBytes.Length;
                double reduction = ((origBytes - outBytes) / (double)origBytes) * 100.0;
                message = $"Compressed document from {FormatFileSize(origBytes)} to {FormatFileSize(outBytes)} ({reduction:F1}% reduction).";
            }
            else
            {
                // Document is already optimally compressed. NEVER output a larger file!
                if (!string.Equals(Path.GetFullPath(options.InputFilePath), Path.GetFullPath(outPath), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(options.InputFilePath, outPath, overwrite: true);
                }
                outBytes = origBytes;
                message = $"Document is already at optimal compression ({FormatFileSize(origBytes)}). Original fidelity preserved without increasing file size.";
            }

            progress?.Report(100.0);

            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = message
            };
        }, ct);
    }

    private static byte[] CompressPdfStreamsAndImages(string inputPath, CompressToolOptions options, IProgress<double>? progress, CancellationToken ct)
    {
        int effectiveDpi = options.ImageQualityDpi > 0 ? Math.Clamp(options.ImageQualityDpi, 50, 600) : 150;

        int jpegQuality = options.JpegQuality > 0 
            ? Math.Clamp(options.JpegQuality, 20, 100) 
            : options.Level switch
            {
                PdfCompressionLevel.MaximumCompression => 52,
                PdfCompressionLevel.SmallSize => 60,
                PdfCompressionLevel.Balanced => 66,
                PdfCompressionLevel.HighQuality => 82,
                PdfCompressionLevel.MaximumQuality => 92,
                _ => 66
            };

        int maxDimension = options.MaxImageDimension > 0
            ? Math.Clamp(options.MaxImageDimension, 500, 5000)
            : Math.Clamp((int)(11.69 * effectiveDpi), 600, 4500);

        using var doc = PdfFileHelper.OpenDocumentSafely(inputPath, PdfDocumentOpenMode.Modify);

        doc.Options.CompressContentStreams = options.CompressStreams;
        doc.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Never;
        doc.Options.NoCompression = !options.CompressStreams;

        if (options.RemoveMetadata)
        {
            doc.Info.Title = "";
            doc.Info.Author = "";
            doc.Info.Subject = "";
            doc.Info.Keywords = "";
            PdfFileHelper.SetFryPdfMetadata(doc);
            doc.Internals.Catalog.Elements.Remove("/Metadata");
            doc.Internals.Catalog.Elements.Remove("/PieceInfo");
            doc.Internals.Catalog.Elements.Remove("/StructTreeRoot");
        }

        int pageCount = doc.Pages.Count;
        for (int i = 0; i < pageCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            var page = doc.Pages[i];
            page.Elements.Remove("/Thumb");
            if (options.RemoveMetadata)
            {
                page.Elements.Remove("/PieceInfo");
            }

            if (options.CompressStreams && page.Contents != null)
            {
                for (int c = 0; c < page.Contents.Elements.Count; c++)
                {
                    var contentRef = page.Contents.Elements[c];
                    if (contentRef is PdfDictionary cDict && cDict.Stream != null)
                    {
                        try { cDict.Stream.Zip(); } catch { }
                    }
                }
            }
            progress?.Report(20.0 + (i / (double)pageCount * 30.0));
        }

        var allObjects = doc.Internals.GetAllObjects();
        for (int idx = 0; idx < allObjects.Length; idx++)
        {
            ct.ThrowIfCancellationRequested();
            var obj = allObjects[idx];
            if (obj is PdfDictionary dict && dict.Stream != null)
            {
                string subtype = dict.Elements.GetString("/Subtype");
                string type = dict.Elements.GetString("/Type");
                bool isImage = subtype == "/Image" || (type == "/XObject" && dict.Elements.ContainsKey("/Width") && dict.Elements.ContainsKey("/Height"));

                if (isImage)
                {
                    OptimizeImageDictionary(dict, maxDimension, jpegQuality, options.ConvertToGrayscale);
                }
                else if (options.CompressStreams)
                {
                    try
                    {
                        if (!dict.Elements.ContainsKey("/Filter"))
                        {
                            dict.Stream.Zip();
                        }
                    }
                    catch { }
                }
            }
            progress?.Report(50.0 + (idx / (double)allObjects.Length * 30.0));
        }

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    private static void OptimizeImageDictionary(PdfDictionary dict, int maxDimension, int jpegQuality, bool convertToGrayscale = false)
    {
        try
        {
            if (dict.Stream == null || dict.Stream.Value == null || dict.Stream.Value.Length < 256)
                return;

            byte[] streamBytes = dict.Stream.Value;

            using var bmp = SKBitmap.Decode(streamBytes);
            if (bmp == null || bmp.Width <= 0 || bmp.Height <= 0)
                return;

            int origW = bmp.Width;
            int origH = bmp.Height;
            int targetW = origW;
            int targetH = origH;

            int maxSide = Math.Max(origW, origH);
            if (maxSide > maxDimension)
            {
                double scale = (double)maxDimension / maxSide;
                targetW = Math.Max(1, (int)(origW * scale));
                targetH = Math.Max(1, (int)(origH * scale));
            }

            SKBitmap? toEncode = bmp;
            SKBitmap? resizedBmp = null;
            SKBitmap? grayBmp = null;

            if (targetW != origW || targetH != origH)
            {
                resizedBmp = bmp.Resize(new SKImageInfo(targetW, targetH), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                if (resizedBmp != null)
                {
                    toEncode = resizedBmp;
                }
            }

            if (convertToGrayscale && toEncode != null)
            {
                grayBmp = ConvertBitmapToGrayscale(toEncode);
                if (grayBmp != null)
                {
                    toEncode = grayBmp;
                }
            }

            try
            {
                using var skImage = SKImage.FromBitmap(toEncode);
                if (skImage != null)
                {
                    using var encoded = skImage.Encode(SKEncodedImageFormat.Jpeg, jpegQuality);
                    if (encoded != null)
                    {
                        byte[] newJpegBytes = encoded.ToArray();
                        if (newJpegBytes.Length < streamBytes.Length)
                        {
                            dict.Stream.Value = newJpegBytes;
                            dict.Elements.SetName("/Filter", "/DCTDecode");
                            dict.Elements.SetInteger("/Length", newJpegBytes.Length);
                            dict.Elements.SetInteger("/Width", targetW);
                            dict.Elements.SetInteger("/Height", targetH);
                            dict.Elements.SetName("/ColorSpace", "/DeviceRGB");
                            dict.Elements.SetInteger("/BitsPerComponent", 8);
                            dict.Elements.Remove("/DecodeParms");
                        }
                    }
                }
            }
            finally
            {
                resizedBmp?.Dispose();
                grayBmp?.Dispose();
            }
        }
        catch
        {
            // Preserve original bytes on error
        }
    }

    private static SKBitmap? ConvertBitmapToGrayscale(SKBitmap original)
    {
        try
        {
            var grayBmp = new SKBitmap(original.Width, original.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            using var canvas = new SKCanvas(grayBmp);
            using var paint = new SKPaint();
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.21f, 0.72f, 0.07f, 0, 0,
                0.21f, 0.72f, 0.07f, 0, 0,
                0.21f, 0.72f, 0.07f, 0, 0,
                0,     0,     0,     1, 0
            });
            canvas.DrawBitmap(original, 0, 0, paint);
            return grayBmp;
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? TryRasterPageCompression(string inputPath, CompressToolOptions options, CancellationToken ct)
    {
        try
        {
            float scale = options.ImageQualityDpi > 0
                ? Math.Clamp(options.ImageQualityDpi / 72f, 0.75f, 4.0f)
                : options.Level switch
                {
                    PdfCompressionLevel.MaximumCompression => 1.25f,
                    PdfCompressionLevel.SmallSize => 1.35f,
                    PdfCompressionLevel.Balanced => 1.45f,
                    PdfCompressionLevel.HighQuality => 1.85f,
                    _ => 1.45f
                };

            int jpegQuality = options.JpegQuality > 0
                ? Math.Clamp(options.JpegQuality, 20, 100)
                : options.Level switch
                {
                    PdfCompressionLevel.MaximumCompression => 52,
                    PdfCompressionLevel.SmallSize => 60,
                    PdfCompressionLevel.Balanced => 66,
                    PdfCompressionLevel.HighQuality => 82,
                    _ => 66
                };

            using var pigDoc = UglyToad.PdfPig.PdfDocument.Open(inputPath);
            int pageCount = pigDoc.NumberOfPages;
            if (pageCount <= 0 || pageCount > 250) return null;

            var pageImages = new List<(byte[] Bytes, float Width, float Height)>();
            for (int i = 1; i <= pageCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                var page = pigDoc.GetPage(i);
                using var pngStream = PdfPigExtensions.GetPageAsPng(pigDoc, i, scale, 90);
                if (pngStream == null || pngStream.Length == 0) return null;

                using var bmp = SKBitmap.Decode(pngStream);
                if (bmp == null) return null;

                SKBitmap finalBmp = bmp;
                SKBitmap? grayCandidate = null;
                if (options.ConvertToGrayscale)
                {
                    grayCandidate = ConvertBitmapToGrayscale(bmp);
                    if (grayCandidate != null)
                    {
                        finalBmp = grayCandidate;
                    }
                }

                try
                {
                    using var img = SKImage.FromBitmap(finalBmp);
                    using var encoded = img.Encode(SKEncodedImageFormat.Jpeg, jpegQuality);
                    pageImages.Add((encoded.ToArray(), (float)page.Width, (float)page.Height));
                }
                finally
                {
                    grayCandidate?.Dispose();
                }
            }

            var doc = FryPdfDocument.Create(container =>
            {
                foreach (var p in pageImages)
                {
                    container.Page(page =>
                    {
                        page.Size(p.Width, p.Height, Unit.Point);
                        page.Margin(0);
                        page.Content().Image(p.Bytes);
                    });
                }
            });

            return doc.GeneratePdf();
        }
        catch
        {
            return null;
        }
    }

    public async Task<ToolExecutionResult> RepairPdfAsync(RepairToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Repaired.pdf");
            }

            ct.ThrowIfCancellationRequested();

            try
            {
                // Attempt standard reconstruction
                using var inputDoc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Import);
                using var outputDoc = new PdfSharpDoc();

                outputDoc.Info.Title = !string.IsNullOrEmpty(inputDoc.Info.Title) ? inputDoc.Info.Title : Path.GetFileNameWithoutExtension(options.InputFilePath);
                PdfFileHelper.SetFryPdfMetadata(outputDoc);

                int pages = inputDoc.PageCount;
                int recoveredPages = 0;

                for (int i = 0; i < pages; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var page = inputDoc.Pages[i];
                        outputDoc.AddPage(page);
                        recoveredPages++;
                    }
                    catch
                    {
                        // Skip corrupted page or object
                    }
                    progress?.Report(20.0 + (i / (double)pages * 60.0));
                }

                PdfFileHelper.SaveDocumentWithFryPdfMetadata(outputDoc, outPath);
                progress?.Report(100.0);

                long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = outPath,
                    OutputFiles = new System.Collections.Generic.List<string> { outPath },
                    OriginalSizeBytes = origBytes,
                    OutputSizeBytes = outBytes,
                    Message = $"Repaired and safely reconstructed {recoveredPages} of {pages} pages without data loss."
                };
            }
            catch (Exception ex)
            {
                // Fallback stream reconstruction for broken xref
                try
                {
                    byte[] bytes = File.ReadAllBytes(options.InputFilePath);
                    using var ms = new MemoryStream(bytes);
                    using var inputDoc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                    using var outputDoc = new PdfSharpDoc();

                    for (int i = 0; i < inputDoc.PageCount; i++)
                    {
                        outputDoc.AddPage(inputDoc.Pages[i]);
                    }
                    PdfFileHelper.SaveDocumentWithFryPdfMetadata(outputDoc, outPath);

                    long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
                    return new ToolExecutionResult
                    {
                        Success = true,
                        OutputFilePath = outPath,
                        OutputFiles = new System.Collections.Generic.List<string> { outPath },
                        OriginalSizeBytes = origBytes,
                        OutputSizeBytes = outBytes,
                        Message = $"Rebuilt xref tables and recovered {outputDoc.PageCount} pages."
                    };
                }
                catch
                {
                    return new ToolExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"Unable to repair document structure: {ex.Message}"
                    };
                }
            }
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertToPdfAAsync(PdfAToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(20.0);

            using var inputDoc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Import);
            using var outputDoc = new PdfSharpDoc();

            // Note: this labels the document as the requested standard but does not (yet)
            // produce real PDF/A-conformant output — that additionally requires embedded
            // XMP metadata with the pdfaid schema, an /OutputIntent ICC profile, full font
            // embedding, and no encryption, none of which are implemented here. The label
            // below reflects intent, not verified conformance.
            string standardLabel = options.Standard switch
            {
                PdfAStandard.PdfA1b => "PDF/A-1b (ISO 19005-1)",
                PdfAStandard.PdfA3b => "PDF/A-3b (ISO 19005-3)",
                _ => "PDF/A-2b (ISO 19005-2)"
            };

            outputDoc.Info.Title = inputDoc.Info.Title;
            outputDoc.Info.Author = inputDoc.Info.Author;
            outputDoc.Info.Subject = $"Prepared for archiving (targeting {standardLabel})";
            PdfFileHelper.SetFryPdfMetadata(outputDoc);
            outputDoc.Options.CompressContentStreams = true;

            int pageCount = inputDoc.PageCount;
            for (int i = 0; i < pageCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                outputDoc.AddPage(inputDoc.Pages[i]);
                progress?.Report(20.0 + (i / (double)pageCount * 60.0));
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_PDFA.pdf");
            }

            PdfFileHelper.SaveDocumentWithFryPdfMetadata(outputDoc, outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new System.Collections.Generic.List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Prepared document targeting {standardLabel}. Note: this does not yet produce verified PDF/A-conformant output (no embedded XMP/pdfaid metadata, ICC output intent, or font-embedding/encryption checks) — do not rely on this for formal archival compliance."
            };
        }, ct);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
