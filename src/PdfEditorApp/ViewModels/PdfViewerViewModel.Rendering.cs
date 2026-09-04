using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Services;
using SkiaSharp;
using UglyToad.PdfPig.Rendering.Skia;

namespace PdfEditorApp.ViewModels;

public partial class PdfViewerViewModel
{
    /// <summary>Renders a specific page at the specified scale directly from PDF bytes using Skia.</summary>
    /// <summary>Renders a specific page at the specified scale directly to image bytes.</summary>
    public byte[]? RenderPageBytesAtScale(int pageNumber, float scale)
    {
        if (_currentPdfBytes == null) return null;
        lock (_renderLock)
        {
            try
            {
                var doc = OpenOrReuseDocument();
                if (doc == null) return null;

                using var stream = PdfPigExtensions.GetPageAsPng(doc, pageNumber, scale, 100);
                if (stream != null && stream.Length > 0)
                {
                    return stream.ToArray();
                }
            }
            catch { }
        }
        return null;
    }

    /// <summary>Renders a specific page at the specified scale directly from PDF bytes using Skia.</summary>
    public Bitmap? RenderPageAtScale(int pageNumber, float scale, PdfReaderTheme? theme = null)
    {
        var bytes = RenderPageBytesAtScale(pageNumber, scale);
        if (bytes != null && bytes.Length > 0)
        {
            try
            {
                var activeTheme = theme ?? ReadingTheme;
                if (activeTheme == PdfReaderTheme.Default)
                {
                    using var ms = new MemoryStream(bytes);
                    return new Bitmap(ms);
                }

                // Apply theme color filter using SkiaSharp
                using var stream = new MemoryStream(bytes);
                using var skBitmap = SKBitmap.Decode(stream);
                if (skBitmap != null)
                {
                    using var themed = ApplyThemeToSkBitmap(skBitmap, activeTheme);
                    using var img = SKImage.FromBitmap(themed);
                    using var data = img.Encode(SKEncodedImageFormat.Png, 95);
                    using var outStream = data.AsStream();
                    return new Bitmap(outStream);
                }

                using var fallbackMs = new MemoryStream(bytes);
                return new Bitmap(fallbackMs);
            }
            catch { }
        }
        return null;
    }

    public static SKBitmap ApplyThemeToSkBitmap(SKBitmap source, PdfReaderTheme theme)
    {
        var dest = new SKBitmap(source.Width, source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(dest);
        using var paint = new SKPaint();

        if (theme == PdfReaderTheme.Sepia)
        {
            // Warm eye-care sepia matrix
            float[] sepiaMatrix = new float[]
            {
                0.393f, 0.769f, 0.189f, 0, 0,
                0.349f, 0.686f, 0.168f, 0, 0,
                0.272f, 0.534f, 0.131f, 0, 0,
                0,      0,      0,      1, 0
            };
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(sepiaMatrix);
        }
        else if (theme == PdfReaderTheme.Dark)
        {
            // Comfortable dark mode: converts pure white (1.0) to dark slate (30,41,59)
            // and pure black (0.0) to soft off-white (241,245,249)
            float scale = -0.827f;
            float[] darkMatrix = new float[]
            {
                scale, 0, 0, 0, 241f / 255f,
                0, scale, 0, 0, 245f / 255f,
                0, 0, scale, 0, 249f / 255f,
                0, 0, 0, 1, 0
            };
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(darkMatrix);
        }
        else if (theme == PdfReaderTheme.HighContrast)
        {
            // High contrast accessibility: inverted black background with sharp yellow text
            float[] hcMatrix = new float[]
            {
                -1.0f, 0, 0, 0, 1.0f,
                -0.1f, -0.9f, 0, 0, 1.0f,
                0, 0, -1.0f, 0, 40f / 255f,
                0, 0, 0, 1, 0
            };
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(hcMatrix);
        }

        canvas.DrawBitmap(source, 0, 0, paint);
        return dest;
    }

    /// <summary>Ensures a page is rendered at the appropriate scale with top priority.</summary>
    public void EnsurePageRendered(int pageNumber, float? scale = null)
    {
        var page = Pages.FirstOrDefault(p => p.PageNumber == pageNumber);
        if (page == null) return;

        float targetScale = scale ?? Math.Clamp((float)(ZoomLevel * 2.25f), PdfViewerPageItem.BasePageRenderScale, 5.0f);
        if (page.Bitmap == null || Math.Abs(page.RenderedScale - targetScale) > 0.5f || page.AppliedReadingTheme != ReadingTheme)
        {
            // Scrolling fires this repeatedly for the same pages, and a page's Bitmap stays
            // null until its render finishes — so without this guard, scrolling back and forth
            // queues up redundant renders of pages already being rendered. Since every render
            // serializes behind the single render lock, that backlog delays the pages the user
            // is actually looking at and makes scrolling feel unresponsive.
            if (page.IsRenderLoading) return;
            page.IsRenderLoading = true;
            Interlocked.Increment(ref _pendingForegroundRenders);

            var theme = ReadingTheme;
            Task.Run(() =>
            {
                Bitmap? bmp = null;
                try
                {
                    bmp = RenderPageAtScale(pageNumber, targetScale, theme);
                }
                finally
                {
                    // Released on the worker thread, not inside the dispatcher callback, so the
                    // background sweep can resume as soon as the render itself is done rather
                    // than waiting on UI-thread scheduling.
                    Interlocked.Decrement(ref _pendingForegroundRenders);

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (bmp != null)
                        {
                            page.Bitmap = bmp;
                            page.RenderedScale = targetScale;
                            page.AppliedReadingTheme = theme;
                        }
                        page.IsRenderLoading = false;
                    });
                }
            });
        }
    }

    private void ReRenderActivePagesForTheme()
    {
        if (Pages == null || Pages.Count == 0) return;

        foreach (var page in Pages)
        {
            if (page.Bitmap != null)
            {
                EnsurePageRendered(page.PageNumber, page.RenderedScale);
            }
        }

        if (IsSinglePageMode && SelectedPage != null)
        {
            EnsurePageRendered(SelectedPage.PageNumber);
        }
        else if (IsTwoPageSpreadMode && SelectedSpread != null)
        {
            if (SelectedSpread.LeftPage != null) EnsurePageRendered(SelectedSpread.LeftPage.PageNumber);
            if (SelectedSpread.RightPage != null) EnsurePageRendered(SelectedSpread.RightPage.PageNumber);
        }
    }

    /// <summary>
    /// Tells the viewer which pages are actually on screen right now (called from the view's
    /// scroll handler). Renders full-resolution bitmaps for the visible range plus a small
    /// lookahead, and releases them for pages that have scrolled well out of view — so memory
    /// stays bounded by what's near the viewport instead of growing with document length.
    /// </summary>
    public void RequestPagesVisible(int firstPageNumber, int lastPageNumber)
    {
        if (Pages.Count == 0) return;

        // Small lookahead by design. Every render serializes behind a single lock, so a wide
        // window just builds a queue that delays the pages actually on screen.
        const int renderLookahead = 2;
        const int keepAliveLookahead = 40;

        int renderFirst = Math.Max(1, firstPageNumber - renderLookahead);
        int renderLast = Math.Min(Pages.Count, lastPageNumber + renderLookahead);

        if (renderFirst == _lastVisibleFirstPage && renderLast == _lastVisibleLastPage) return;
        _lastVisibleFirstPage = renderFirst;
        _lastVisibleLastPage = renderLast;

        // 1. On-screen pages first. Order matters: renders are serialized, so anything queued
        // ahead of the visible pages directly delays what the user is waiting to see. This
        // used to iterate from (firstVisible - lookahead) upward, which meant the pages just
        // ABOVE the viewport were always rasterized before the page being looked at.
        for (int p = firstPageNumber; p <= lastPageNumber; p++)
        {
            EnsurePageRendered(p);
        }

        // 2. Then the lookahead, nearest-first outward.
        for (int d = 1; d <= renderLookahead; d++)
        {
            int before = firstPageNumber - d;
            if (before >= 1) EnsurePageRendered(before);

            int after = lastPageNumber + d;
            if (after <= Pages.Count) EnsurePageRendered(after);
        }

        // Note: text geometry is deliberately NOT warmed here. It's as expensive as a render
        // and shares the same lock, so warming it for every page in the window starved the
        // visible pages' renders. It stays on-demand (first pointer interaction with a page).

        // 3. Release bitmaps for pages far outside the viewport.
        int keepFirst = Math.Max(1, firstPageNumber - keepAliveLookahead);
        int keepLast = Math.Min(Pages.Count, lastPageNumber + keepAliveLookahead);

        for (int i = 0; i < Pages.Count; i++)
        {
            int pageNum = i + 1;
            if (pageNum < keepFirst || pageNum > keepLast)
            {
                // The "Fallback when loading" placeholder shows again if the user scrolls back;
                // ThumbnailBitmap is left alone since the thumbnail rail may show a wider
                // range than the main viewport and thumbnails are cheap to keep resident.
                Pages[i].Bitmap = null;
            }
        }
    }

    private void StartBackgroundWorker(CancellationToken ct)
    {
        Task.Run(async () =>
        {
            if (_currentPdfBytes == null) return;

            // 1. Asynchronously extract Bookmarks/Outlines without blocking initial page display
            try
            {
                List<PdfViewerBookmarkItem>? bookmarksList = null;
                lock (_renderLock)
                {
                    var doc = OpenOrReuseDocument();
                    if (doc != null && doc.TryGetBookmarks(out var bookmarks) && bookmarks != null && bookmarks.Roots != null)
                    {
                        bookmarksList = new List<PdfViewerBookmarkItem>();
                        ExtractBookmarksRecursive(bookmarks.Roots, bookmarksList);
                    }
                }

                if (bookmarksList != null && bookmarksList.Count > 0 && !ct.IsCancellationRequested)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        Bookmarks.Clear();
                        foreach (var b in bookmarksList) Bookmarks.Add(b);
                        OnPropertyChanged(nameof(HasBookmarks));
                    });
                }
            }
            catch { }

            // 2. Progressively build thumbnails and extract accurate text / geometry for every
            // page — full-resolution page bitmaps are NOT rendered here (beyond the first
            // screenful below); those are rendered on demand for the pages actually scrolled
            // into view (see RequestPagesVisible), otherwise a large document would render
            // every page's full-res bitmap up front and hold them all in memory regardless of
            // whether they're ever looked at.
            const int eagerFirstScreenfulPages = 8;
            for (int i = 1; i <= Pages.Count; i++)
            {
                if (ct.IsCancellationRequested) return;

                int pageNum = i;
                var page = Pages.FirstOrDefault(p => p.PageNumber == pageNum);
                if (page != null)
                {
                    // Stand aside whenever the user is waiting on a visible page. Everything in
                    // this sweep competes for the same render lock, and over a long document
                    // it's minutes of work — without yielding, a scrolled-to page's render sits
                    // behind hundreds of thumbnail renders and text extractions and takes so
                    // long to arrive that scrolling looks like it does nothing at all.
                    if (pageNum > eagerFirstScreenfulPages)
                    {
                        await WaitForForegroundIdleAsync(ct);
                        if (ct.IsCancellationRequested) return;
                    }

                    // Render a bounded first screenful eagerly and unconditionally — a safety
                    // net independent of the view's own viewport/scroll wiring, so the pages a
                    // user sees immediately after opening a document are never left waiting on
                    // that wiring alone.
                    if (pageNum <= eagerFirstScreenfulPages && page.Bitmap == null)
                    {
                        var eagerBmp = RenderPageAtScale(pageNum, PdfViewerPageItem.BasePageRenderScale);
                        if (eagerBmp != null && !ct.IsCancellationRequested)
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                page.Bitmap = eagerBmp;
                                page.RenderedScale = PdfViewerPageItem.BasePageRenderScale;
                            });
                        }
                    }

                    // Render lightweight thumbnail (0.4f scale) if needed
                    if (page.ThumbnailBitmap == null)
                    {
                        var thumbBmp = RenderPageAtScale(pageNum, 0.4f);
                        if (thumbBmp != null && !ct.IsCancellationRequested)
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                page.ThumbnailBitmap = thumbBmp;
                            });
                        }
                    }

                    // Extract accurate dimensions and text if page > 1
                    if (pageNum > 1 && (page.Words.Count == 0 || string.IsNullOrEmpty(page.ExtractedText)))
                    {
                        try
                        {
                            string? txt = null;
                            List<PdfViewerWordItem>? words = null;
                            List<PdfViewerTextLineItem>? lines = null;
                            double w = 0, h = 0;
                            int rot = 0;

                            lock (_renderLock)
                            {
                                var doc = OpenOrReuseDocument();
                                if (doc != null && pageNum <= doc.NumberOfPages)
                                {
                                    var p = doc.GetPage(pageNum);
                                    (txt, words, lines) = ExtractPageTextGeometry(p);
                                    w = Math.Max(100, p.Width);
                                    h = Math.Max(100, p.Height);
                                    rot = (int)p.Rotation.Value;
                                }
                            }

                            if ((words == null || words.Count == 0) && w > 0 && h > 0)
                            {
                                var (ocrTxt, ocrWords, ocrLines) = ExtractOcrPageTextGeometry(pageNum, w, h);
                                if (ocrWords.Count > 0)
                                {
                                    txt = ocrTxt;
                                    words = ocrWords;
                                    lines = ocrLines;
                                }
                            }

                            string summary = "";
                            if (!string.IsNullOrWhiteSpace(txt))
                            {
                                var firstLine = txt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                                summary = firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
                            }

                            if (txt != null && !ct.IsCancellationRequested)
                            {
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    page.WidthPoints = w;
                                    page.HeightPoints = h;
                                    page.RotationAngle = rot;
                                    page.ExtractedText = txt;
                                    page.Words = words!;
                                    page.TextLines = lines!;
                                    page.PageSummary = summary;
                                });
                            }
                        }
                        catch { }
                    }
                }

                await Task.Yield();
            }
        }, ct);
    }

    private void RebuildPageSpreads()
    {
        PageSpreads.Clear();
        if (Pages.Count == 0) return;

        // Page 1 is Cover (Alone on right or single spread)
        int spreadIdx = 1;
        var coverSpread = new PdfViewerPageSpreadItem
        {
            SpreadIndex = spreadIdx++,
            LeftPage = null,
            RightPage = Pages[0],
            SpreadLabel = "Page 1 (Cover)",
            IsSelected = true
        };
        PageSpreads.Add(coverSpread);

        // Subsequent pages paired (2-3, 4-5, etc.)
        for (int i = 1; i < Pages.Count; i += 2)
        {
            var left = Pages[i];
            var right = (i + 1 < Pages.Count) ? Pages[i + 1] : null;
            string lbl = right != null ? $"Pages {left.PageNumber} - {right.PageNumber}" : $"Page {left.PageNumber}";

            PageSpreads.Add(new PdfViewerPageSpreadItem
            {
                SpreadIndex = spreadIdx++,
                LeftPage = left,
                RightPage = right,
                SpreadLabel = lbl,
                IsSelected = false
            });
        }

        SelectedSpread = PageSpreads.FirstOrDefault();
    }

    private void UpdateSelectedSpreadForPage(int pageNum)
    {
        if (PageSpreads.Count == 0) return;

        foreach (var s in PageSpreads)
        {
            bool match = (s.LeftPage?.PageNumber == pageNum || s.RightPage?.PageNumber == pageNum);
            s.IsSelected = match;
            if (match)
            {
                SelectedSpread = s;
            }
        }

        if (IsTwoPageSpreadMode && SelectedSpread != null)
        {
            if (SelectedSpread.LeftPage != null) EnsurePageRendered(SelectedSpread.LeftPage.PageNumber);
            if (SelectedSpread.RightPage != null) EnsurePageRendered(SelectedSpread.RightPage.PageNumber);
        }
    }

}
