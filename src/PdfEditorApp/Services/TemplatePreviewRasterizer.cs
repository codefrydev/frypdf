using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PdfEditorApp.ViewModels;
using PdfEditorApp.Views.Controls;

namespace PdfEditorApp.Services;

/// <summary>
/// Renders a template's page preview to a bitmap once, so the gallery can show an
/// <c>&lt;Image&gt;</c> instead of a live visual tree per card.
/// </summary>
/// <remarks>
/// The template gallery used to build one control per page element for every card — roughly
/// 5-6k visuals and ~60k characters of text shaping — in a single synchronous layout pass when
/// the page became visible. A <c>Viewbox</c> scales *after* layout, so the miniature size saved
/// none of that work. Rasterising once collapses each card to a single image draw.
///
/// Avalonia rendering is UI-thread-only, so this cannot move to a background thread. Callers
/// should instead spread the work across dispatcher frames (see
/// <c>HomeViewModel.QueueTemplatePreviewRasterization</c>) so the UI stays responsive.
/// </remarks>
public static class TemplatePreviewRasterizer
{
    /// <summary>Rendered width in pixels. 2x the ~176pt card so the thumbnail stays crisp.</summary>
    public const int PreviewPixelWidth = 352;

    /// <summary>Why the most recent <see cref="Render"/> returned null, for diagnostics.</summary>
    public static string? LastFailureReason { get; private set; }

    /// <summary>
    /// True when every sampled pixel is identical, i.e. nothing but the page background was drawn.
    /// </summary>
    /// <remarks>
    /// Samples a coarse grid rather than the whole surface — enough to tell "flat colour" from
    /// "has content" without making the check itself expensive.
    /// </remarks>
    private static bool IsUniform(Bitmap bitmap)
    {
        const int samplesPerAxis = 16;

        int width = bitmap.PixelSize.Width;
        int height = bitmap.PixelSize.Height;
        if (width < 2 || height < 2) return true;

        // One row at a time from a pooled buffer, rather than the whole surface.
        //
        // This used to allocate `new byte[width * 4 * height]` and pin it — about 700 KB for a
        // 352x498 preview — to read 256 pixels, once per template. Anything from 85 KB up lands
        // on the Large Object Heap, and pinning it there blocks compaction, so rasterizing a
        // gallery of templates fragmented the LOH and drove exactly the long blocking
        // collections that section 5 of .agents/rules/performance_and_zero_lag_mandate.md
        // exists to prevent. A single row is ~1.4 KB and comes from ArrayPool.
        int rowBytes = width * 4;
        var row = ArrayPool<byte>.Shared.Rent(rowBytes);

        // Pinned rather than `fixed`: this project does not enable unsafe blocks.
        var handle = GCHandle.Alloc(row, GCHandleType.Pinned);
        try
        {
            uint first = 0;
            bool haveFirst = false;

            for (int sy = 0; sy < samplesPerAxis; sy++)
            {
                int y = (int)((sy + 0.5) / samplesPerAxis * height);
                if (y >= height) y = height - 1;

                bitmap.CopyPixels(new PixelRect(0, y, width, 1), handle.AddrOfPinnedObject(), rowBytes, rowBytes);

                for (int sx = 0; sx < samplesPerAxis; sx++)
                {
                    int x = (int)((sx + 0.5) / samplesPerAxis * width);
                    if (x >= width) x = width - 1;

                    uint pixel = BitConverter.ToUInt32(row, x * 4);

                    if (!haveFirst)
                    {
                        first = pixel;
                        haveFirst = true;
                    }
                    else if (pixel != first)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        finally
        {
            handle.Free();
            ArrayPool<byte>.Shared.Return(row);
        }
    }

    /// <summary>
    /// Renders <paramref name="page"/> to a bitmap, or returns null when there is nothing to
    /// draw or no render surface is available (headless/unit-test hosts).
    /// </summary>
    /// <param name="surface">
    /// The thumbnail-sized container that <paramref name="host"/> sits in; this is what is
    /// actually rendered, so the host's scale transform is applied.
    /// </param>
    /// <param name="host">
    /// A <see cref="TemplatePagePreview"/> that is attached to the visual tree. Rendering a
    /// detached control yields a flat, empty bitmap because its ItemsControl containers are
    /// never realized, so the caller must supply a mounted host.
    /// </param>
    public static Bitmap? Render(PageViewModel? page, TemplatePagePreview host, Avalonia.Controls.Canvas surface)
    {
        // Each guard names itself: a silent null here is exactly the kind of failure that
        // looks like a performance win while quietly producing nothing.
        if (page == null) { LastFailureReason = "page was null"; return null; }
        if (page.Width <= 0 || page.Height <= 0) { LastFailureReason = $"page has no size ({page.Width}x{page.Height})"; return null; }
        if (Application.Current == null) { LastFailureReason = "no Application"; return null; }
        if (!Dispatcher.UIThread.CheckAccess()) { LastFailureReason = "not on the UI thread"; return null; }
        if (TopLevel.GetTopLevel(host) == null) { LastFailureReason = "render host is not attached to a TopLevel"; return null; }

        LastFailureReason = null;

        try
        {
            var preview = host;
            preview.DataContext = page;

            // Lay the page out at its true geometry — element positions are in page units.
            var pageSize = new Size(page.Width, page.Height);
            preview.Measure(pageSize);
            preview.Arrange(new Rect(pageSize));
            preview.UpdateLayout();

            double scale = PreviewPixelWidth / page.Width;
            var thumbSize = new PixelSize(
                Math.Max(1, (int)Math.Round(page.Width * scale)),
                Math.Max(1, (int)Math.Round(page.Height * scale)));

            // Scale during the render: the page lays out at true geometry inside the surface,
            // and the surface is sized to the thumbnail.
            preview.RenderTransformOrigin = RelativePoint.TopLeft;
            preview.RenderTransform = new Avalonia.Media.ScaleTransform(scale, scale);

            surface.Width = thumbSize.Width;
            surface.Height = thumbSize.Height;
            surface.Measure(new Size(thumbSize.Width, thumbSize.Height));
            surface.Arrange(new Rect(0, 0, thumbSize.Width, thumbSize.Height));
            surface.UpdateLayout();

            var target = new RenderTargetBitmap(thumbSize, new Vector(96, 96));
            target.Render(surface);

            // A control that was never attached to a visual tree can fail to realize its
            // ItemsControl containers, which yields a fast but entirely blank bitmap. That
            // would be a silent regression — the gallery would look empty while every timing
            // number improved — so check for it and say so.
            if (IsUniform(target))
            {
                AppLogService.Instance.Log(AppLogLevel.Warning, "Templates",
                    "A template preview rendered as a single flat colour — the page content was " +
                    "not realized. The card will fall back to its placeholder.");
                target.Dispose();
                LastFailureReason = "rendered as a flat colour";
                return null;
            }

            return target;
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogWarning("Templates",
                "Could not rasterize a template preview; the card will show its placeholder", ex);
            LastFailureReason = ex.GetType().Name + ": " + ex.Message;
            return null;
        }
    }
}
