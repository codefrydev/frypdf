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

namespace PdfEditorApp.Services.Tools;

public interface IPdfPageService
{
    Task<ToolExecutionResult> MergePdfAsync(MergeToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> SplitPdfAsync(SplitToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> RotatePdfAsync(RotateToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> CropPdfAsync(CropToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> OrganizePdfAsync(OrganizeToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> AddPageNumbersAsync(PageNumberToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> AddWatermarkAsync(WatermarkToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
}

public class PdfPageService : IPdfPageService
{
    public async Task<ToolExecutionResult> MergePdfAsync(MergeToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (options.InputFiles == null || options.InputFiles.Count == 0)
                return new ToolExecutionResult { Success = false, ErrorMessage = "No input PDF files provided for merging." };

            long totalOriginalBytes = 0;
            foreach (var f in options.InputFiles)
            {
                if (File.Exists(f)) totalOriginalBytes += new FileInfo(f).Length;
            }

            using var outputDoc = new PdfDocument();
            int totalFiles = options.InputFiles.Count;

            for (int i = 0; i < totalFiles; i++)
            {
                ct.ThrowIfCancellationRequested();
                string filePath = options.InputFiles[i];
                if (!File.Exists(filePath)) continue;

                using var inputDoc = PdfFileHelper.OpenDocumentSafely(filePath, PdfDocumentOpenMode.Import);
                int pageCount = inputDoc.PageCount;

                for (int p = 0; p < pageCount; p++)
                {
                    ct.ThrowIfCancellationRequested();
                    var page = inputDoc.Pages[p];
                    outputDoc.AddPage(page);
                }

                progress?.Report((i + 1) / (double)totalFiles * 90.0);
            }

            if (outputDoc.PageCount == 0)
                return new ToolExecutionResult { Success = false, ErrorMessage = "No valid pages found in the provided PDF files to merge." };

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFiles[0]) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                outPath = Path.Combine(dir, "Merged_Document.pdf");
            }

            string? targetDir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            PdfFileHelper.SaveDocumentWithFryPdfMetadata(outputDoc, outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = totalOriginalBytes,
                OutputSizeBytes = outBytes,
                Message = $"Successfully merged {totalFiles} files into {Path.GetFileName(outPath)} ({outputDoc.PageCount} pages)."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> SplitPdfAsync(SplitToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            using var inputDoc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Import);
            int totalPages = inputDoc.PageCount;

            if (totalPages == 0)
                return new ToolExecutionResult { Success = false, ErrorMessage = "The selected PDF has no pages to split." };

            string outDir = options.OutputDirectory;
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetDirectoryName(options.InputFilePath) ?? "", Path.GetFileNameWithoutExtension(options.InputFilePath) + "_split");
            }
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            var createdFiles = new List<string>();
            string baseName = Path.GetFileNameWithoutExtension(options.InputFilePath);

            if (options.SplitOddEven)
            {
                // Odd pages document
                using var oddDoc = new PdfDocument();
                for (int i = 0; i < totalPages; i += 2)
                    oddDoc.AddPage(inputDoc.Pages[i]);
                string oddPath = Path.Combine(outDir, $"{baseName}_OddPages.pdf");
                if (oddDoc.PageCount > 0)
                {
                    PdfFileHelper.SaveDocumentWithFryPdfMetadata(oddDoc, oddPath);
                    createdFiles.Add(oddPath);
                }

                // Even pages document
                using var evenDoc = new PdfDocument();
                for (int i = 1; i < totalPages; i += 2)
                    evenDoc.AddPage(inputDoc.Pages[i]);
                string evenPath = Path.Combine(outDir, $"{baseName}_EvenPages.pdf");
                if (evenDoc.PageCount > 0)
                {
                    PdfFileHelper.SaveDocumentWithFryPdfMetadata(evenDoc, evenPath);
                    createdFiles.Add(evenPath);
                }
            }
            else if (options.Mode == SplitExtractMode.SplitByPageRanges)
            {
                var ranges = ParsePageRanges(options.RangeExpression, totalPages);
                int rIdx = 1;
                foreach (var range in ranges)
                {
                    ct.ThrowIfCancellationRequested();
                    if (range.Count == 0) continue;
                    using var rangeDoc = new PdfDocument();
                    foreach (int pIndex in range)
                    {
                        if (pIndex >= 0 && pIndex < totalPages)
                            rangeDoc.AddPage(inputDoc.Pages[pIndex]);
                    }

                    if (rangeDoc.PageCount > 0)
                    {
                        string rangeName = $"{baseName}_Range{rIdx}_{range.First() + 1}-{range.Last() + 1}.pdf";
                        string rangePath = Path.Combine(outDir, rangeName);
                        PdfFileHelper.SaveDocumentWithFryPdfMetadata(rangeDoc, rangePath);
                        createdFiles.Add(rangePath);
                    }
                    rIdx++;
                }
            }
            else
            {
                // Split every N pages
                int n = Math.Max(1, options.PagesPerSplit);
                int part = 1;
                for (int i = 0; i < totalPages; i += n)
                {
                    ct.ThrowIfCancellationRequested();
                    using var partDoc = new PdfDocument();
                    int count = Math.Min(n, totalPages - i);
                    for (int j = 0; j < count; j++)
                    {
                        partDoc.AddPage(inputDoc.Pages[i + j]);
                    }

                    string partPath = Path.Combine(outDir, $"{baseName}_Part{part}.pdf");
                    PdfFileHelper.SaveDocumentWithFryPdfMetadata(partDoc, partPath);
                    createdFiles.Add(partPath);
                    part++;

                    progress?.Report(i / (double)totalPages * 90.0);
                }
            }

            progress?.Report(100.0);
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = createdFiles.FirstOrDefault(),
                OutputFiles = createdFiles,
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = createdFiles.Sum(f => File.Exists(f) ? new FileInfo(f).Length : 0),
                Message = $"Split PDF into {createdFiles.Count} files in {outDir}."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> RotatePdfAsync(RotateToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            using var doc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Modify);
            int totalPages = doc.PageCount;

            var targetIndices = GetFilteredPageIndices(options.TargetFilter, options.CustomRange, totalPages);

            for (int i = 0; i < totalPages; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (targetIndices.Contains(i))
                {
                    var page = doc.Pages[i];
                    int currentRotate = page.Rotate;
                    int newRotate = (currentRotate + options.RotationDegrees) % 360;
                    if (newRotate < 0) newRotate += 360;
                    page.Rotate = newRotate;
                }
                progress?.Report((i + 1) / (double)totalPages * 90.0);
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Rotated.pdf");
            }

            PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Rotated {targetIndices.Count} pages by {options.RotationDegrees}°."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> CropPdfAsync(CropToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            using var doc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Modify);
            int totalPages = doc.PageCount;

            var targetIndices = GetTargetPageIndices(options.TargetPages, options.CustomRange, totalPages);

            for (int i = 0; i < totalPages; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (targetIndices.Contains(i))
                {
                    var page = doc.Pages[i];
                    var mediaBox = page.MediaBox;
                    double x1 = mediaBox.X1 + options.CropLeftPoints;
                    double y1 = mediaBox.Y1 + options.CropBottomPoints;
                    double x2 = mediaBox.X2 - options.CropRightPoints;
                    double y2 = mediaBox.Y2 - options.CropTopPoints;

                    if (x2 > x1 && y2 > y1)
                    {
                        var rect = new PdfRectangle(new PdfSharpCore.Drawing.XPoint(x1, y1), new PdfSharpCore.Drawing.XPoint(x2, y2));
                        page.CropBox = rect;
                        page.MediaBox = rect;
                    }
                }
                progress?.Report((i + 1) / (double)totalPages * 90.0);
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Cropped.pdf");
            }

            PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Applied crop margins to {targetIndices.Count} pages."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> OrganizePdfAsync(OrganizeToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            using var inputDoc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Import);
            using var outputDoc = new PdfDocument();
            int totalPages = inputDoc.PageCount;

            var order = options.PageOrder.Count > 0 ? options.PageOrder : Enumerable.Range(0, totalPages).ToList();

            int step = 0;
            foreach (int pIndex in order)
            {
                ct.ThrowIfCancellationRequested();
                if (pIndex < 0 || pIndex >= totalPages) continue;
                if (options.PagesToDelete.Contains(pIndex)) continue;

                var page = inputDoc.Pages[pIndex];
                var addedPage = outputDoc.AddPage(page);

                if (options.PageRotations.TryGetValue(pIndex, out int rot) && rot != 0)
                {
                    addedPage.Rotate = (addedPage.Rotate + rot) % 360;
                }

                step++;
                progress?.Report(step / (double)order.Count * 90.0);
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Organized.pdf");
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
                Message = $"Organized PDF to {outputDoc.PageCount} pages."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> AddPageNumbersAsync(PageNumberToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            using var doc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Modify);
            int totalPages = doc.PageCount;

            var targetIndices = GetTargetPageIndices(options.TargetPages, options.CustomRange, totalPages);
            var font = new XFont(options.FontFamily, options.FontSize, XFontStyle.Regular);
            var brush = new XSolidBrush(ParseColor(options.ColorHex));

            int count = 0;
            for (int i = 0; i < totalPages; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (targetIndices.Contains(i))
                {
                    var page = doc.Pages[i];
                    using var gfx = XGraphics.FromPdfPage(page);

                    int pageNum = options.StartingNumber + count;
                    string text = FormatTemplate(options.Template, pageNum, totalPages);

                    var size = gfx.MeasureString(text, font);
                    double margin = options.MarginPoints;
                    double x = 0, y = 0;

                    switch (options.Position)
                    {
                        case PageNumberPosition.TopLeft:
                            x = margin;
                            y = margin + size.Height;
                            break;
                        case PageNumberPosition.TopCenter:
                            x = (page.Width.Point - size.Width) / 2.0;
                            y = margin + size.Height;
                            break;
                        case PageNumberPosition.TopRight:
                            x = page.Width.Point - size.Width - margin;
                            y = margin + size.Height;
                            break;
                        case PageNumberPosition.BottomLeft:
                            x = margin;
                            y = page.Height.Point - margin;
                            break;
                        case PageNumberPosition.BottomCenter:
                            x = (page.Width.Point - size.Width) / 2.0;
                            y = page.Height.Point - margin;
                            break;
                        case PageNumberPosition.BottomRight:
                            x = page.Width.Point - size.Width - margin;
                            y = page.Height.Point - margin;
                            break;
                    }

                    gfx.DrawString(text, font, brush, new XPoint(x, y));
                    count++;
                }

                progress?.Report((i + 1) / (double)totalPages * 90.0);
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Numbered.pdf");
            }

            PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Stamped page numbers on {count} pages."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> AddWatermarkAsync(WatermarkToolOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            using var doc = PdfFileHelper.OpenDocumentSafely(options.InputFilePath, PdfDocumentOpenMode.Modify);
            int totalPages = doc.PageCount;

            var targetIndices = GetTargetPageIndices(options.TargetPages, options.CustomRange, totalPages);
            var baseColor = ParseColor(options.ColorHex);
            byte alpha = (byte)(Math.Clamp(options.Opacity, 0.01, 1.0) * 255);
            var watermarkColor = XColor.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
            var font = new XFont(string.IsNullOrWhiteSpace(options.FontFamily) ? "Helvetica" : options.FontFamily, Math.Max(8, options.FontSize), XFontStyle.Bold);
            var brush = new XSolidBrush(watermarkColor);

            int stampedCount = 0;
            for (int i = 0; i < totalPages; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (targetIndices.Contains(i))
                {
                    var page = doc.Pages[i];
                    using var gfx = XGraphics.FromPdfPage(page);

                    if (options.Position == WatermarkPosition.Tiled)
                    {
                        // Draw repeated tiled watermark diagonally across page
                        for (double y = 50; y < page.Height.Point; y += 180)
                        {
                            for (double x = 50; x < page.Width.Point; x += 220)
                            {
                                var state = gfx.Save();
                                gfx.TranslateTransform(x, y);
                                gfx.RotateTransform(options.RotationAngle);
                                gfx.DrawString(options.Text, font, brush, new XPoint(0, 0), XStringFormats.Center);
                                gfx.Restore(state);
                            }
                        }
                    }
                    else
                    {
                        var state = gfx.Save();
                        double cx = page.Width.Point / 2.0;
                        double cy = page.Height.Point / 2.0;

                        switch (options.Position)
                        {
                            case WatermarkPosition.TopLeft:
                                cx = 100; cy = 100; break;
                            case WatermarkPosition.TopCenter:
                                cx = page.Width.Point / 2.0; cy = 100; break;
                            case WatermarkPosition.TopRight:
                                cx = page.Width.Point - 100; cy = 100; break;
                            case WatermarkPosition.BottomLeft:
                                cx = 100; cy = page.Height.Point - 100; break;
                            case WatermarkPosition.BottomCenter:
                                cx = page.Width.Point / 2.0; cy = page.Height.Point - 100; break;
                            case WatermarkPosition.BottomRight:
                                cx = page.Width.Point - 100; cy = page.Height.Point - 100; break;
                            default: // Center
                                cx = page.Width.Point / 2.0; cy = page.Height.Point / 2.0; break;
                        }

                        gfx.TranslateTransform(cx, cy);
                        gfx.RotateTransform(options.RotationAngle);

                        if (options.Type == WatermarkType.Image && !string.IsNullOrWhiteSpace(options.ImagePath) && File.Exists(options.ImagePath))
                        {
                            try
                            {
                                using var img = XImage.FromFile(options.ImagePath);
                                double imgW = Math.Min(300, img.PixelWidth);
                                double imgH = imgW * (img.PixelHeight / (double)img.PixelWidth);
                                gfx.DrawImage(img, -imgW / 2.0, -imgH / 2.0, imgW, imgH);
                            }
                            catch
                            {
                                gfx.DrawString(options.Text, font, brush, new XPoint(0, 0), XStringFormats.Center);
                            }
                        }
                        else
                        {
                            gfx.DrawString(options.Text, font, brush, new XPoint(0, 0), XStringFormats.Center);
                        }

                        gfx.Restore(state);
                    }

                    stampedCount++;
                }

                progress?.Report((i + 1) / (double)totalPages * 90.0);
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}_Watermarked.pdf");
            }

            PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Applied watermark to {stampedCount} pages."
            };
        }, ct);
    }

    private static string FormatTemplate(string template, int n, int total)
    {
        if (string.IsNullOrWhiteSpace(template)) return n.ToString();
        return template
            .Replace("{n}", n.ToString())
            .Replace("{P}", n.ToString())
            .Replace("{total}", total.ToString())
            .Replace("{N}", total.ToString())
            .Replace("{n:D6}", n.ToString("D6"))
            .Replace("{n:D4}", n.ToString("D4"));
    }

    private static HashSet<int> GetTargetPageIndices(PageTargetSelection selection, string customRange, int totalPages)
    {
        var result = new HashSet<int>();
        switch (selection)
        {
            case PageTargetSelection.AllPages:
                for (int i = 0; i < totalPages; i++) result.Add(i);
                break;
            case PageTargetSelection.OddPagesOnly:
                for (int i = 0; i < totalPages; i += 2) result.Add(i);
                break;
            case PageTargetSelection.EvenPagesOnly:
                for (int i = 1; i < totalPages; i += 2) result.Add(i);
                break;
            case PageTargetSelection.CustomRange:
                var parsed = ParseSingleRangeList(customRange, totalPages);
                foreach (int p in parsed) result.Add(p);
                break;
        }
        return result;
    }

    private static HashSet<int> GetFilteredPageIndices(PageFilterTarget filter, string customRange, int totalPages)
    {
        var result = new HashSet<int>();
        switch (filter)
        {
            case PageFilterTarget.All:
                for (int i = 0; i < totalPages; i++) result.Add(i);
                break;
            case PageFilterTarget.OddPages:
                for (int i = 0; i < totalPages; i += 2) result.Add(i);
                break;
            case PageFilterTarget.EvenPages:
                for (int i = 1; i < totalPages; i += 2) result.Add(i);
                break;
            default:
                for (int i = 0; i < totalPages; i++) result.Add(i);
                break;
        }
        return result;
    }

    private static List<List<int>> ParsePageRanges(string expr, int totalPages)
    {
        var result = new List<List<int>>();
        if (string.IsNullOrWhiteSpace(expr)) return result;
        var parts = expr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var single = ParseSingleRange(p.Trim(), totalPages);
            if (single.Count > 0) result.Add(single);
        }
        return result;
    }

    private static List<int> ParseSingleRangeList(string expr, int totalPages)
    {
        var result = new List<int>();
        if (string.IsNullOrWhiteSpace(expr)) return result;
        var parts = expr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            result.AddRange(ParseSingleRange(p.Trim(), totalPages));
        }
        return result.Distinct().ToList();
    }

    private static List<int> ParseSingleRange(string token, int totalPages)
    {
        var list = new List<int>();
        if (token.Contains('-'))
        {
            var bounds = token.Split('-');
            if (bounds.Length == 2 && int.TryParse(bounds[0].Trim(), out int start) && int.TryParse(bounds[1].Trim(), out int end))
            {
                start = Math.Max(1, start);
                end = Math.Min(totalPages, end);
                for (int p = start; p <= end; p++) list.Add(p - 1);
            }
        }
        else if (int.TryParse(token, out int single))
        {
            if (single >= 1 && single <= totalPages) list.Add(single - 1);
        }
        return list;
    }

    private static XColor ParseColor(string hex)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return XColors.Black;
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return XColor.FromArgb(r, g, b);
            }
            if (hex.Length == 8)
            {
                byte a = Convert.ToByte(hex.Substring(0, 2), 16);
                byte r = Convert.ToByte(hex.Substring(2, 2), 16);
                byte g = Convert.ToByte(hex.Substring(4, 2), 16);
                byte b = Convert.ToByte(hex.Substring(6, 2), 16);
                return XColor.FromArgb(a, r, g, b);
            }
        }
        catch { }
        return XColors.Black;
    }
}
