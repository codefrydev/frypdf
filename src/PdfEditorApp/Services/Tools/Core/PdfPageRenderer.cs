using System.IO;
using Avalonia.Media.Imaging;
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
}
