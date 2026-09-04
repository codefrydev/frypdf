using System.IO;
using Avalonia.Media.Imaging;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Rendering.Skia;

namespace PdfEditorApp.Services.Tools.Core;

/// <summary>
/// Single shared PdfPig+Skia page rasterization path, used by both the PDF reader
/// (<see cref="PdfEditorApp.ViewModels.PdfViewerViewModel"/>) and tool-screen live
/// previews (<see cref="PdfEditorApp.ViewModels.Shell.PdfLivePreviewViewModel"/>),
/// so the render pipeline exists in exactly one place instead of being reimplemented
/// per screen.
/// </summary>
public static class PdfPageRenderer
{
    /// <summary>
    /// Renders one page of a PDF file to a bitmap at the given scale. Falls back to
    /// PdfFileHelper's salvage/repair path if the file can't be opened directly.
    /// Page dimensions are still returned even if bitmap decoding fails, since callers
    /// (page-card sizing, mark-position math) depend on them independent of render success.
    /// </summary>
    public static (Bitmap? Bitmap, double WidthPoints, double HeightPoints) RenderPageAtScale(
        string filePath, int pageNumber, float scale)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || pageNumber < 1)
        {
            return (null, 0, 0);
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            PdfDocument? doc = null;
            try
            {
                doc = PdfDocument.Open(bytes);
            }
            catch
            {
                try
                {
                    byte[] repaired = PdfFileHelper.SalvageAndRepairPdfBytes(bytes);
                    doc = PdfDocument.Open(repaired);
                }
                catch
                {
                    return (null, 0, 0);
                }
            }

            using (doc)
            {
                if (pageNumber > doc.NumberOfPages)
                {
                    return (null, 0, 0);
                }

                var page = doc.GetPage(pageNumber);
                double widthPoints = page.Width;
                double heightPoints = page.Height;

                try { PdfPigExtensions.AddSkiaPageFactory(doc); } catch { }

                using var stream = PdfPigExtensions.GetPageAsPng(doc, pageNumber, scale, 100);
                if (stream != null && stream.Length > 0)
                {
                    stream.Position = 0;
                    Bitmap? bitmap = null;
                    try { bitmap = new Bitmap(stream); } catch { }
                    return (bitmap, widthPoints, heightPoints);
                }

                return (null, widthPoints, heightPoints);
            }
        }
        catch
        {
            return (null, 0, 0);
        }
    }

    /// <summary>
    /// Renders a raw SVG XML string to PNG bytes using QuestPDF's native vector SVG engine
    /// and PdfPig/Skia. Pure headless, high-DPI rasterization without UI dependencies.
    /// </summary>
    public static byte[]? RenderSvgToPngBytes(string svgMarkup, double targetWidth, double targetHeight)
    {
        if (string.IsNullOrWhiteSpace(svgMarkup)) return null;

        try
        {
            float w = (float)System.Math.Max(30, targetWidth);
            float h = (float)System.Math.Max(30, targetHeight);

            // High-DPI scale (clamped to prevent memory bloat, capped at max 2048px)
            float maxDim = System.Math.Max(w, h);
            float scale = maxDim > 0 ? (float)System.Math.Clamp(2048.0 / maxDim, 1.0, 2.5) : 2.0f;

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(w, h, QuestPDF.Infrastructure.Unit.Point);
                    page.Margin(0);
                    page.PageColor(QuestPDF.Helpers.Colors.Transparent);
                    page.Content().Svg(svgMarkup).FitArea();
                });
            });

            byte[] pdfBytes = doc.GeneratePdf();
            using var pdfPigDoc = PdfDocument.Open(pdfBytes);
            try { PdfPigExtensions.AddSkiaPageFactory(pdfPigDoc); } catch { }

            using var stream = PdfPigExtensions.GetPageAsPng(pdfPigDoc, 1, scale, 100);
            if (stream != null && stream.Length > 0)
            {
                return stream.ToArray();
            }
        }
        catch
        {
            // Graceful fallback
        }

        return null;
    }

    /// <summary>
    /// Renders a raw SVG XML string to a high-DPI Avalonia Bitmap using QuestPDF's native vector SVG engine
    /// and PdfPig/Skia. Ensures complete visual fidelity for complex multi-element diagrams (paths, circles,
    /// rects, polygons, texts, gradients, markers) on the live Avalonia canvas.
    /// </summary>
    public static Bitmap? RenderSvgToBitmap(string svgMarkup, double targetWidth, double targetHeight)
    {
        byte[]? pngBytes = RenderSvgToPngBytes(svgMarkup, targetWidth, targetHeight);
        if (pngBytes != null && pngBytes.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(pngBytes);
                return new Bitmap(ms);
            }
            catch
            {
                // In headless unit test runners without Avalonia platform rendering context
                return null;
            }
        }

        return null;
    }
}
