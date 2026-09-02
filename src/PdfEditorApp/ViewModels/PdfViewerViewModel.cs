using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services.Tools;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Rendering.Skia;
using SkiaSharp;

namespace PdfEditorApp.ViewModels;

public enum PdfViewerSidebarTab
{
    Thumbnails,
    Bookmarks,
    Annotations,
    Search,
    Info
}

public class PdfViewerGlyphItem
{
    public char Character { get; set; }
    public Rect Bounds { get; set; }
}

public class PdfViewerWordItem
{
    public string Text { get; set; } = string.Empty;
    public Rect Bounds { get; set; }
    public int LineIndex { get; set; }
    public int WordIndex { get; set; }
    public List<PdfViewerGlyphItem> Glyphs { get; } = new();
}

public class PdfViewerTextLineItem
{
    public int LineIndex { get; set; }
    public Rect Bounds { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<PdfViewerWordItem> Words { get; } = new();
}

public class PdfViewerPageItem : ObservableObject, IDisposable
{
    /// <summary>
    /// Rasterization scale for a page shown at 100% zoom. Matches the ~2x device pixel ratio of
    /// a HiDPI display, which is what mainstream PDF viewers target. This was 2.75x, which
    /// oversampled by roughly 1.9x in area for no visible benefit — and since render cost scales
    /// with pixel count, that directly inflated how long every page took to appear.
    /// Every site that renders or records a scale must use this same value: a mismatch makes
    /// <see cref="PdfViewerViewModel.EnsurePageRendered"/> think the page needs re-rendering and
    /// silently doubles the work.
    /// </summary>
    internal const float BasePageRenderScale = 2.0f;

    private bool _isSelected;
    private int _rotationAngle;
    private float _renderedScale = BasePageRenderScale;
    private Bitmap? _thumbnailBitmap;
    private Bitmap? _bitmap;
    private string _selectedText = string.Empty;
    private bool _hasSelection;
    private bool _isDisposed;

    public PdfReaderTheme AppliedReadingTheme { get; set; } = PdfReaderTheme.Default;

    /// <summary>Guards against piling up redundant background geometry-extraction tasks
    /// while pointer-move events keep firing before the first one completes.</summary>
    public bool IsGeometryLoading { get; set; }

    /// <summary>Guards against queueing duplicate bitmap renders for this page while one is
    /// already in flight — scroll events fire far faster than a page can be rasterized.</summary>
    public bool IsRenderLoading { get; set; }

    public int PageNumber { get; set; }
    public double WidthPoints { get; set; }
    public double HeightPoints { get; set; }
    public string DimensionsText => $"{Math.Round(WidthPoints):F0} × {Math.Round(HeightPoints):F0} pt";
    public string PageLabel => $"Page {PageNumber}";
    public string PageSummary { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;

    public List<PdfViewerWordItem> Words { get; set; } = new();
    public List<PdfViewerTextLineItem> TextLines { get; set; } = new();

    public string SelectedText
    {
        get => _selectedText;
        set
        {
            if (SetProperty(ref _selectedText, value))
            {
                HasSelection = !string.IsNullOrEmpty(value);
            }
        }
    }

    public bool HasSelection
    {
        get => _hasSelection;
        private set => SetProperty(ref _hasSelection, value);
    }

    public List<Rect> SelectionRects { get; } = new();

    public event Action? SelectionChanged;

    public void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedText));
        OnPropertyChanged(nameof(HasSelection));
        SelectionChanged?.Invoke();
    }

    public void ClearSelection()
    {
        if (SelectionRects.Count > 0 || !string.IsNullOrEmpty(SelectedText))
        {
            SelectionRects.Clear();
            SelectedText = string.Empty;
            NotifySelectionChanged();
        }
    }

    public void SelectWord(PdfViewerWordItem word)
    {
        SelectionRects.Clear();
        SelectionRects.Add(word.Bounds);
        SelectedText = word.Text;
        NotifySelectionChanged();
    }

    public void SelectLine(PdfViewerTextLineItem line)
    {
        SelectionRects.Clear();
        SelectionRects.Add(line.Bounds);
        SelectedText = line.Text;
        NotifySelectionChanged();
    }

    public void SelectAll()
    {
        SelectionRects.Clear();
        if (Words.Count == 0 && TextLines.Count == 0)
        {
            SelectedText = string.Empty;
            NotifySelectionChanged();
            return;
        }

        if (TextLines.Count > 0)
        {
            foreach (var line in TextLines)
            {
                SelectionRects.Add(line.Bounds);
            }
        }
        else
        {
            foreach (var word in Words)
            {
                SelectionRects.Add(word.Bounds);
            }
        }

        SelectedText = ExtractedText;
        NotifySelectionChanged();
    }

    public void SetSelectionRange(Point start, Point end)
    {
        SelectionRects.Clear();
        if (Words.Count == 0 || TextLines.Count == 0)
        {
            SelectedText = string.Empty;
            NotifySelectionChanged();
            return;
        }

        // Determine natural reading order start & end
        bool isStartFirst = (start.Y < end.Y - 4) || (Math.Abs(start.Y - end.Y) <= 4 && start.X <= end.X);
        Point firstPoint = isStartFirst ? start : end;
        Point secondPoint = isStartFirst ? end : start;

        var startLine = TextLines
            .Where(l => firstPoint.Y >= l.Bounds.Top - 4 && firstPoint.Y <= l.Bounds.Bottom + 4)
            .OrderBy(l => Math.Max(0, Math.Max(l.Bounds.Left - firstPoint.X, firstPoint.X - l.Bounds.Right)))
            .FirstOrDefault()
            ?? TextLines.OrderBy(l => Math.Abs(l.Bounds.Center.Y - firstPoint.Y) * 10 + Math.Abs(l.Bounds.Center.X - firstPoint.X)).FirstOrDefault();

        var endLine = TextLines
            .Where(l => secondPoint.Y >= l.Bounds.Top - 4 && secondPoint.Y <= l.Bounds.Bottom + 4)
            .OrderBy(l => Math.Max(0, Math.Max(l.Bounds.Left - secondPoint.X, secondPoint.X - l.Bounds.Right)))
            .FirstOrDefault()
            ?? TextLines.OrderBy(l => Math.Abs(l.Bounds.Center.Y - secondPoint.Y) * 10 + Math.Abs(l.Bounds.Center.X - secondPoint.X)).FirstOrDefault();

        if (startLine == null || endLine == null)
        {
            SelectedText = string.Empty;
            NotifySelectionChanged();
            return;
        }

        int startLineIdx = Math.Min(startLine.LineIndex, endLine.LineIndex);
        int endLineIdx = Math.Max(startLine.LineIndex, endLine.LineIndex);

        var sb = new StringBuilder();

        for (int lIdx = startLineIdx; lIdx <= endLineIdx; lIdx++)
        {
            var line = TextLines.FirstOrDefault(l => l.LineIndex == lIdx);
            if (line == null || line.Words.Count == 0) continue;

            List<PdfViewerWordItem> lineSelectedWords;

            if (startLineIdx == endLineIdx)
            {
                double minX = Math.Min(start.X, end.X);
                double maxX = Math.Max(start.X, end.X);

                lineSelectedWords = line.Words
                    .Where(w => w.Bounds.Right >= minX && w.Bounds.Left <= maxX)
                    .ToList();

                if (lineSelectedWords.Count == 0 && line.Bounds.Left <= maxX && line.Bounds.Right >= minX)
                {
                    lineSelectedWords = line.Words.ToList();
                }
            }
            else if (lIdx == startLineIdx)
            {
                double fromX = firstPoint.X;
                lineSelectedWords = line.Words
                    .Where(w => w.Bounds.Right >= fromX)
                    .ToList();

                if (lineSelectedWords.Count == 0 && fromX <= line.Bounds.Left)
                {
                    lineSelectedWords = line.Words.ToList();
                }
            }
            else if (lIdx == endLineIdx)
            {
                double toX = secondPoint.X;
                lineSelectedWords = line.Words
                    .Where(w => w.Bounds.Left <= toX)
                    .ToList();

                if (lineSelectedWords.Count == 0 && toX >= line.Bounds.Right)
                {
                    lineSelectedWords = line.Words.ToList();
                }
            }
            else
            {
                lineSelectedWords = line.Words.ToList();
            }

            if (lineSelectedWords.Count > 0)
            {
                double left = lineSelectedWords.Min(w => w.Bounds.Left);
                double right = lineSelectedWords.Max(w => w.Bounds.Right);
                double top = lineSelectedWords.Min(w => w.Bounds.Top);
                double bottom = lineSelectedWords.Max(w => w.Bounds.Bottom);
                SelectionRects.Add(new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top)));

                string lineTxt = string.Join(" ", lineSelectedWords.Select(w => w.Text));
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(lineTxt);
            }
        }

        SelectedText = sb.ToString();
        NotifySelectionChanged();
    }

    public float RenderedScale
    {
        get => _renderedScale;
        set => SetProperty(ref _renderedScale, value);
    }

    public Bitmap? ThumbnailBitmap
    {
        get => _thumbnailBitmap ?? _bitmap;
        set
        {
            var old = _thumbnailBitmap;
            if (SetProperty(ref _thumbnailBitmap, value) && old != null && old != _bitmap)
            {
                old.Dispose();
            }
        }
    }

    public Bitmap? Bitmap
    {
        get => _bitmap;
        set
        {
            var old = _bitmap;
            if (SetProperty(ref _bitmap, value))
            {
                OnPropertyChanged(nameof(ThumbnailBitmap));
                if (old != null && old != _thumbnailBitmap)
                {
                    old.Dispose();
                }
            }
        }
    }

    public int RotationAngle
    {
        get => _rotationAngle;
        set => SetProperty(ref _rotationAngle, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ObservableCollection<PdfViewerAnnotationItem> PageAnnotations { get; } = new();

    /// <summary>Releases the rendered bitmaps. Safe to call more than once.</summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        var bmp = _bitmap;
        var thumb = _thumbnailBitmap;
        _bitmap = null;
        _thumbnailBitmap = null;

        bmp?.Dispose();
        if (thumb != null && thumb != bmp)
        {
            thumb.Dispose();
        }
    }
}

public class PdfViewerPageSpreadItem : ObservableObject
{
    private bool _isSelected;

    public int SpreadIndex { get; set; }
    public PdfViewerPageItem? LeftPage { get; set; }
    public PdfViewerPageItem? RightPage { get; set; }
    public string SpreadLabel { get; set; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class PdfViewerBookmarkItem
{
    public string Title { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public ObservableCollection<PdfViewerBookmarkItem> Children { get; } = new();
    public bool HasChildren => Children.Count > 0;
}

public class PdfViewerAnnotationItem : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "Highlight"; // Highlight, StickyNote, Stamp, Ink, Signature
    public int PageNumber { get; set; } = 1;
    public string Author { get; set; } = "Reader";
    public string Content { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#FEF08A";
    public string IconKind { get; set; } = "FormatColorHighlight";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string TimeFormatted => CreatedAt.ToString("HH:mm · MMM d");
    public List<Rect> HighlightRects { get; set; } = new();
}

public class PdfViewerMetadataItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string IconKind { get; set; } = "InformationOutline";
}

public class PdfViewerSearchMatch
{
    public int PageNumber { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public int MatchIndex { get; set; }
}

/// <summary>
/// Professional, Adobe Acrobat-style standalone PDF Reader & Viewer ViewModel.
/// Features continuous scroll, single-page, and two-page spread viewing, sharp Skia rendering,
/// eye-comfort reading themes (Sepia, Night/Dark, High Contrast), real page thumbnails,
/// document outline / bookmarks, in-document search, annotations, and seamless bridge to FryPDF Editor.
/// </summary>
public partial class PdfViewerViewModel : ViewModelBase
{
    private CancellationTokenSource? _renderCts;
    private CancellationTokenSource? _zoomDebounceCts;
    private CancellationTokenSource? _backgroundRenderCts;
    private readonly object _renderLock = new();
    private byte[]? _currentPdfBytes;
    private string? _currentPassword;
    private int _lastVisibleFirstPage = -1;
    private int _lastVisibleLastPage = -1;

    /// <summary>
    /// Count of in-flight renders for pages the user is actually looking at. Every rasterization
    /// and text extraction serializes behind <see cref="_renderLock"/> (the shared PdfPig document
    /// is not thread-safe), so the whole-document background sweep must stand aside while this is
    /// non-zero — otherwise a visible page's render queues behind hundreds of background jobs and
    /// effectively never arrives.
    /// </summary>
    private int _pendingForegroundRenders;

    /// <summary>Parks background sweep work while user-visible renders are outstanding.</summary>
    private async Task WaitForForegroundIdleAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && Volatile.Read(ref _pendingForegroundRenders) > 0)
        {
            try
            {
                await Task.Delay(40, ct);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// The currently-open document, kept alive and reused for every on-demand page render and
    /// geometry extraction — opening/parsing a large PDF from bytes is expensive, and doing it
    /// fresh on every single page request (as this used to) meant every page you touched paid
    /// a whole-document re-parse instead of just the cost of that one page. Always access under
    /// <see cref="_renderLock"/>; PdfPig documents aren't safe for concurrent multi-thread use.
    /// </summary>
    private PdfDocument? _openDocument;

    /// <summary>Returns the shared open document for <see cref="_currentPdfBytes"/>, opening it
    /// (with the same repair fallback used elsewhere) if this is the first access since the
    /// document loaded. Caller must hold <see cref="_renderLock"/>.</summary>
    private PdfDocument? OpenOrReuseDocument()
    {
        if (_openDocument != null) return _openDocument;
        if (_currentPdfBytes == null) return null;

        var parsingOptions = new ParsingOptions();
        if (!string.IsNullOrEmpty(_currentPassword))
        {
            parsingOptions.Password = _currentPassword;
        }

        PdfDocument? doc = null;
        try
        {
            doc = PdfDocument.Open(_currentPdfBytes, parsingOptions);
        }
        catch
        {
            try
            {
                byte[] repaired = PdfFileHelper.SalvageAndRepairPdfBytes(_currentPdfBytes);
                doc = PdfDocument.Open(repaired, parsingOptions);
            }
            catch
            {
                return null;
            }
        }

        try { PdfPigExtensions.AddSkiaPageFactory(doc); } catch { }
        _openDocument = doc;
        return doc;
    }

    public IStorageProvider? StorageProvider { get; set; }

    // --- Events ---
    public event Action<string>? EditInStudioRequested;
    public event Action? BackToHomeRequested;
    public event Action<PdfToolId, string>? RunToolRequested;
    public event Action<string>? ShowToastRequested;
    public event Action? OpenFileRequested;
    public event Action<PdfReaderTheme>? ReadingThemeChanged;
    public event Action<int>? ScrollToPageRequested;

    // --- Observable Properties ---

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string _documentTitle = "Document.pdf";

    [ObservableProperty]
    private int _currentPageNumber = 1;

    [ObservableProperty]
    private string _jumpPageText = "1";

    [ObservableProperty]
    private int _totalPagesCount = 0;

    [ObservableProperty]
    private double _zoomLevel = 1.0; // 100%

    [ObservableProperty]
    private PdfViewerZoomMode _zoomMode = PdfViewerZoomMode.Custom;

    [ObservableProperty]
    private bool _isFitToWidthActive = false;

    [ObservableProperty]
    private bool _isFitToPageActive = false;

    private bool _isApplyingFitZoom = false;

    /// <summary>
    /// Optional provider supplied by the view (e.g. PdfViewerView) to return the current
    /// viewport dimensions (ViewportWidth, ViewportHeight, HorizontalPadding, VerticalPadding).
    /// </summary>
    public Func<(double ViewportWidth, double ViewportHeight, double HorizontalPadding, double VerticalPadding)>? ViewportSizeProvider { get; set; }

    // Layout Modes: Continuous Scroll, Single Page, Two-Page Spread
    [ObservableProperty]
    private PdfViewLayoutMode _selectedLayoutMode = PdfViewLayoutMode.ContinuousScroll;

    [ObservableProperty]
    private bool _isContinuousScroll = true;

    [ObservableProperty]
    private bool _isSinglePageMode = false;

    [ObservableProperty]
    private bool _isTwoPageSpreadMode = false;

    // Reading Themes: Default (White), Sepia (Book comfort), Dark (Night mode), High Contrast
    [ObservableProperty]
    private PdfReaderTheme _readingTheme = PdfReaderTheme.Default;

    [ObservableProperty]
    private string _themeBackgroundHex = "#F1F5F9";

    [ObservableProperty]
    private string _themePaperBackgroundHex = "#FFFFFF";

    [ObservableProperty]
    private string _themeTextColorHex = "#0F172A";

    [ObservableProperty]
    private string _themeBorderColorHex = "#E2E8F0";

    [ObservableProperty]
    private PdfViewerSidebarTab _selectedSidebarTab = PdfViewerSidebarTab.Thumbnails;

    [ObservableProperty]
    private bool _isSidebarOpen = true;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isOpeningDocument = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _hasDocument = false;

    [ObservableProperty]
    private PdfViewerPageItem? _selectedPage;

    [ObservableProperty]
    private PdfViewerPageSpreadItem? _selectedSpread;

    // In-Document Search
    [ObservableProperty]
    private bool _isSearchBarVisible = false;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _searchMatchCase = false;

    [ObservableProperty]
    private bool _searchWholeWord = false;

    [ObservableProperty]
    private int _currentMatchIndex = 0;

    [ObservableProperty]
    private int _totalMatchesCount = 0;

    [ObservableProperty]
    private string _searchStatusText = string.Empty;

    // Sticky Note quick add popup
    [ObservableProperty]
    private bool _isAddNoteOpen = false;

    [ObservableProperty]
    private string _newNoteText = string.Empty;

    // Stamp quick add popup
    [ObservableProperty]
    private bool _isAddStampOpen = false;

    // Highlight color selection
    [ObservableProperty]
    private string _selectedHighlightColorHex = "#FEF08A"; // Yellow default

    // Presentation / Fullscreen Mode
    [ObservableProperty]
    private bool _isFullscreen = false;

    // Interactive Text Selection State
    [ObservableProperty]
    private string _activeSelectedText = string.Empty;

    [ObservableProperty]
    private bool _hasTextSelection = false;

    [ObservableProperty]
    private int _activeSelectedPageNumber = 1;

    public string SelectedTextSnippet
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ActiveSelectedText)) return string.Empty;
            string clean = ActiveSelectedText.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return clean.Length > 35 ? clean.Substring(0, 35) + "…" : clean;
        }
    }

    partial void OnActiveSelectedTextChanged(string value)
    {
        HasTextSelection = !string.IsNullOrWhiteSpace(value);
        OnPropertyChanged(nameof(SelectedTextSnippet));
    }

    // Collections
    public ObservableCollection<PdfViewerPageItem> Pages { get; } = new();
    public ObservableCollection<PdfViewerPageSpreadItem> PageSpreads { get; } = new();
    public ObservableCollection<PdfViewerBookmarkItem> Bookmarks { get; } = new();
    public ObservableCollection<PdfViewerAnnotationItem> Annotations { get; } = new();
    public ObservableCollection<PdfViewerMetadataItem> MetadataItems { get; } = new();
    public ObservableCollection<PdfViewerSearchMatch> SearchResults { get; } = new();

    public static readonly string[] AvailableZoomPresets = new[]
    {
        "50%", "75%", "100%", "125%", "150%", "200%", "300%", "400%"
    };

    public bool HasBookmarks => Bookmarks.Count > 0;
    public bool HasAnnotations => Annotations.Count > 0;
    public bool HasSearchResults => SearchResults.Count > 0;
    public int MatchCount => SearchResults.Count;

    public string ZoomPercentageText => $"{(int)Math.Round(ZoomLevel * 100)}%";
    public string PageNavigationDisplay => TotalPagesCount > 0 ? $"{CurrentPageNumber} / {TotalPagesCount}" : "0 / 0";

    public bool IsThemeDefault => ReadingTheme == PdfReaderTheme.Default;
    public bool IsThemeSepia => ReadingTheme == PdfReaderTheme.Sepia;
    public bool IsThemeDark => ReadingTheme == PdfReaderTheme.Dark;
    public bool IsThemeHighContrast => ReadingTheme == PdfReaderTheme.HighContrast;

    partial void OnZoomModeChanged(PdfViewerZoomMode value)
    {
        IsFitToWidthActive = (value == PdfViewerZoomMode.FitWidth);
        IsFitToPageActive = (value == PdfViewerZoomMode.FitPage);
    }

    partial void OnZoomLevelChanged(double value)
    {
        if (!_isApplyingFitZoom)
        {
            ZoomMode = PdfViewerZoomMode.Custom;
        }

        OnPropertyChanged(nameof(ZoomPercentageText));
        InvalidateVisiblePageCache();

        // Dynamic High-DPI Vector Re-render on Zoom Change (Debounced)
        _zoomDebounceCts?.Cancel();
        _zoomDebounceCts = new CancellationTokenSource();
        var token = _zoomDebounceCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(140, token);
                if (token.IsCancellationRequested) return;

                float dynamicScale = Math.Clamp((float)(value * 2.25f), PdfViewerPageItem.BasePageRenderScale, 5.0f);
                if (SelectedPage != null && Math.Abs(SelectedPage.RenderedScale - dynamicScale) > 0.4f)
                {
                    var highResBmp = RenderPageAtScale(SelectedPage.PageNumber, dynamicScale);
                    if (highResBmp != null && !token.IsCancellationRequested)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            SelectedPage.Bitmap = highResBmp;
                            SelectedPage.RenderedScale = dynamicScale;
                        });
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    public void InvalidateVisiblePageCache()
    {
        _lastVisibleFirstPage = -1;
        _lastVisibleLastPage = -1;
    }

    partial void OnCurrentPageNumberChanged(int value)
    {
        OnPropertyChanged(nameof(PageNavigationDisplay));
        JumpPageText = value.ToString();
        if (value >= 1 && value <= Pages.Count)
        {
            var targetPage = Pages[value - 1];
            if (SelectedPage != targetPage)
            {
                SelectedPage = targetPage;
            }
            foreach (var p in Pages)
            {
                p.IsSelected = (p.PageNumber == value);
            }
            EnsurePageRendered(value);
            if (value > 1) EnsurePageRendered(value - 1);
            if (value < Pages.Count) EnsurePageRendered(value + 1);
        }
        UpdateSelectedSpreadForPage(value);
    }

    partial void OnTotalPagesCountChanged(int value)
    {
        OnPropertyChanged(nameof(PageNavigationDisplay));
    }

    partial void OnSelectedPageChanged(PdfViewerPageItem? value)
    {
        if (value != null)
        {
            foreach (var p in Pages)
            {
                p.IsSelected = (p.PageNumber == value.PageNumber);
            }
            if (CurrentPageNumber != value.PageNumber)
            {
                CurrentPageNumber = value.PageNumber;
            }
        }
    }

    partial void OnSelectedLayoutModeChanged(PdfViewLayoutMode value)
    {
        IsContinuousScroll = (value == PdfViewLayoutMode.ContinuousScroll);
        IsSinglePageMode = (value == PdfViewLayoutMode.SinglePage);
        IsTwoPageSpreadMode = (value == PdfViewLayoutMode.TwoPageSpread);

        if (IsTwoPageSpreadMode)
        {
            if (PageSpreads.Count == 0 && Pages.Count > 0)
            {
                RebuildPageSpreads();
            }
            else
            {
                UpdateSelectedSpreadForPage(CurrentPageNumber);
            }

            if (SelectedSpread?.LeftPage != null) EnsurePageRendered(SelectedSpread.LeftPage.PageNumber);
            if (SelectedSpread?.RightPage != null) EnsurePageRendered(SelectedSpread.RightPage.PageNumber);

            FitToPage();
        }
        else if (IsSinglePageMode)
        {
            if (CurrentPageNumber >= 1 && CurrentPageNumber <= Pages.Count)
            {
                SelectedPage = Pages[CurrentPageNumber - 1];
            }
            EnsurePageRendered(CurrentPageNumber);

            FitToPage();
        }
        else if (IsContinuousScroll)
        {
            if (IsFitToWidthActive)
            {
                FitToWidth();
            }
            else if (IsFitToPageActive)
            {
                FitToPage();
            }
        }
    }

    partial void OnReadingThemeChanged(PdfReaderTheme value)
    {
        OnPropertyChanged(nameof(IsThemeDefault));
        OnPropertyChanged(nameof(IsThemeSepia));
        OnPropertyChanged(nameof(IsThemeDark));
        OnPropertyChanged(nameof(IsThemeHighContrast));

        switch (value)
        {
            case PdfReaderTheme.Sepia:
                ThemeBackgroundHex = "#EDE3C9";
                ThemePaperBackgroundHex = "#FBF0D9";
                ThemeTextColorHex = "#433422";
                ThemeBorderColorHex = "#E6D5B8";
                break;
            case PdfReaderTheme.Dark:
                ThemeBackgroundHex = "#0F172A";
                ThemePaperBackgroundHex = "#1E293B";
                ThemeTextColorHex = "#F1F5F9";
                ThemeBorderColorHex = "#334155";
                break;
            case PdfReaderTheme.HighContrast:
                ThemeBackgroundHex = "#000000";
                ThemePaperBackgroundHex = "#000000";
                ThemeTextColorHex = "#FFFF00";
                ThemeBorderColorHex = "#FFFF00";
                break;
            case PdfReaderTheme.Default:
            default:
                ThemeBackgroundHex = "#F1F5F9";
                ThemePaperBackgroundHex = "#FFFFFF";
                ThemeTextColorHex = "#0F172A";
                ThemeBorderColorHex = "#E2E8F0";
                break;
        }

        ReRenderActivePagesForTheme();
        ReadingThemeChanged?.Invoke(value);
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ClearSearch();
        }
        else
        {
            PerformSearch();
        }
    }

    partial void OnSearchMatchCaseChanged(bool value)
    {
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            PerformSearch();
        }
    }

    partial void OnSearchWholeWordChanged(bool value)
    {
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            PerformSearch();
        }
    }

    // --- Core Document Loading & Text Geometry Extraction ---

    public static (string text, List<PdfViewerWordItem> words, List<PdfViewerTextLineItem> lines) ExtractPageTextGeometry(Page page)
    {
        double pageHeight = Math.Max(100, page.Height);
        var rawWords = page.GetWords().Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList();
        if (rawWords.Count == 0)
        {
            return (page.Text ?? string.Empty, new List<PdfViewerWordItem>(), new List<PdfViewerTextLineItem>());
        }

        var wordItems = new List<PdfViewerWordItem>();
        int wordIdx = 0;
        foreach (var w in rawWords)
        {
            double x = Math.Max(0, w.BoundingBox.Left);
            double y = Math.Max(0, pageHeight - w.BoundingBox.Top);
            double width = Math.Max(1, w.BoundingBox.Width);
            double height = Math.Max(1, w.BoundingBox.Height);

            var wordItem = new PdfViewerWordItem
            {
                Text = w.Text,
                Bounds = new Rect(x, y, width, height),
                WordIndex = wordIdx++
            };

            if (w.Letters != null && w.Letters.Count > 0)
            {
                foreach (var letter in w.Letters)
                {
                    if (string.IsNullOrEmpty(letter.Value)) continue;
                    double lx = Math.Max(0, letter.BoundingBox.Left);
                    double ly = Math.Max(0, pageHeight - letter.BoundingBox.Top);
                    double lw = Math.Max(0.5, letter.BoundingBox.Width);
                    double lh = Math.Max(1, letter.BoundingBox.Height);

                    wordItem.Glyphs.Add(new PdfViewerGlyphItem
                    {
                        Character = letter.Value[0],
                        Bounds = new Rect(lx, ly, lw, lh)
                    });
                }
            }

            wordItems.Add(wordItem);
        }

        // Group words into lines based on vertical overlap
        var sortedWords = wordItems.OrderBy(w => w.Bounds.Top).ThenBy(w => w.Bounds.Left).ToList();
        var lineList = new List<PdfViewerTextLineItem>();

        foreach (var word in sortedWords)
        {
            var line = lineList.FirstOrDefault(l =>
            {
                double overlapTop = Math.Max(l.Bounds.Top, word.Bounds.Top);
                double overlapBottom = Math.Min(l.Bounds.Bottom, word.Bounds.Bottom);
                double overlap = overlapBottom - overlapTop;
                return overlap > Math.Min(l.Bounds.Height, word.Bounds.Height) * 0.45;
            });

            if (line == null)
            {
                line = new PdfViewerTextLineItem
                {
                    LineIndex = lineList.Count,
                    Bounds = word.Bounds
                };
                line.Words.Add(word);
                lineList.Add(line);
            }
            else
            {
                line.Words.Add(word);
                line.Bounds = line.Bounds.Union(word.Bounds);
            }
        }

        // Sort words in each line horizontally and build line text
        var sb = new StringBuilder();
        int lineIdx = 0;
        foreach (var line in lineList.OrderBy(l => l.Bounds.Top))
        {
            line.LineIndex = lineIdx++;
            var sortedLineWords = line.Words.OrderBy(w => w.Bounds.Left).ToList();
            line.Words.Clear();
            line.Words.AddRange(sortedLineWords);

            foreach (var w in line.Words)
            {
                w.LineIndex = line.LineIndex;
            }

            line.Text = string.Join(" ", line.Words.Select(w => w.Text));
            sb.AppendLine(line.Text);
        }

        string fullText = sb.ToString().TrimEnd();
        if (string.IsNullOrWhiteSpace(fullText))
        {
            fullText = page.Text ?? string.Empty;
        }

        return (fullText, wordItems, lineList.OrderBy(l => l.Bounds.Top).ToList());
    }

    public byte[]? CurrentPdfBytes => _currentPdfBytes;

    /// <summary>
    /// Populates a page's word/line geometry for hit-testing (hover cursor, click-to-select-word).
    /// Runs off the UI thread — the first hover/click on a page the background sweep hasn't
    /// reached yet used to open and fully parse the PDF synchronously on the dispatcher thread.
    /// </summary>
    public void EnsurePageGeometry(PdfViewerPageItem page)
    {
        if (page.Words.Count > 0 || page.IsGeometryLoading || _currentPdfBytes == null || _currentPdfBytes.Length == 0) return;

        page.IsGeometryLoading = true;
        int pageNumber = page.PageNumber;

        Task.Run(() =>
        {
            string? text = null;
            List<PdfViewerWordItem>? words = null;
            List<PdfViewerTextLineItem>? lines = null;
            double width = 0, height = 0;

            try
            {
                lock (_renderLock)
                {
                    var doc = OpenOrReuseDocument();
                    if (doc != null && pageNumber >= 1 && pageNumber <= doc.NumberOfPages)
                    {
                        var p = doc.GetPage(pageNumber);
                        (text, words, lines) = ExtractPageTextGeometry(p);
                        if (p.Width > 0 && p.Height > 0)
                        {
                            width = p.Width;
                            height = p.Height;
                        }
                    }
                }
            }
            catch { }

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (text != null)
                {
                    page.ExtractedText = text;
                    page.Words = words!;
                    page.TextLines = lines!;
                    if (width > 0 && height > 0)
                    {
                        page.WidthPoints = width;
                        page.HeightPoints = height;
                    }
                }
                page.IsGeometryLoading = false;
            });
        });
    }

    public async Task LoadDocumentAsync(string filePath, string? password = null)
    {
        if (!File.Exists(filePath))
        {
            StatusMessage = $"File not found: {filePath}";
            return;
        }

        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(filePath);
            await LoadDocumentFromBytesAsync(bytes, filePath, password);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading document: {ex.Message}";
        }
    }

    public Task LoadDocumentBytesAsync(byte[] pdfBytes, string? documentTitle = null, string? password = null)
        => LoadDocumentFromBytesAsync(pdfBytes, documentTitle ?? "Document.pdf", password);

    public async Task LoadDocumentFromBytesAsync(byte[] pdfBytes, string sourceFilePath = "", string? password = null)
    {
        _renderCts?.Cancel();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        _backgroundRenderCts?.Cancel();
        _backgroundRenderCts = new CancellationTokenSource();

        IsLoading = true;
        IsOpeningDocument = true;
        HasDocument = true;
        StatusMessage = "Opening PDF document...";
        _currentPdfBytes = pdfBytes;
        _currentPassword = password;
        CurrentFilePath = sourceFilePath;
        DocumentTitle = string.IsNullOrWhiteSpace(sourceFilePath) ? "Document.pdf" : Path.GetFileName(sourceFilePath);

        // Release the previous document's rendered bitmaps before replacing it — this
        // ViewModel is a long-lived singleton reused across every document open, so
        // nothing else ever frees this memory otherwise.
        foreach (var oldPage in Pages)
        {
            oldPage.Dispose();
        }
        _lastVisibleFirstPage = -1;
        _lastVisibleLastPage = -1;

        lock (_renderLock)
        {
            _openDocument?.Dispose();
            _openDocument = null;
        }

        Pages.Clear();
        PageSpreads.Clear();
        Bookmarks.Clear();
        Annotations.Clear();
        MetadataItems.Clear();
        SearchResults.Clear();

        try
        {
            byte[] sanitizedBytes = PdfFileHelper.SanitizePdfBytes(pdfBytes);
            _currentPdfBytes = sanitizedBytes;

            var (metaList, pagesList, total) = await Task.Run(() =>
            {
                var parsingOptions = new ParsingOptions();
                if (!string.IsNullOrEmpty(password))
                {
                    parsingOptions.Password = password;
                }

                PdfDocument? doc = null;
                try
                {
                    doc = PdfDocument.Open(sanitizedBytes, parsingOptions);
                }
                catch
                {
                    try
                    {
                        byte[] repaired = PdfFileHelper.SalvageAndRepairPdfBytes(sanitizedBytes);
                        doc = PdfDocument.Open(repaired, parsingOptions);
                    }
                    catch
                    {
                        doc = PdfDocument.Open(pdfBytes, parsingOptions);
                    }
                }

                try
                {
                    try
                    {
                        PdfPigExtensions.AddSkiaPageFactory(doc);
                    }
                    catch { }

                    int total = doc.NumberOfPages;
                    if (total == 0)
                    {
                        doc.Dispose();
                        return (new List<PdfViewerMetadataItem>(), new List<PdfViewerPageItem>(), 0);
                    }

                    // 1. Fast Page 1 extraction & immediate render
                    var firstPage = doc.GetPage(1);
                    double defaultWidth = Math.Max(100, firstPage.Width);
                    double defaultHeight = Math.Max(100, firstPage.Height);
                    int defaultRot = (int)firstPage.Rotation.Value;
                    var (firstPageText, firstWords, firstLines) = ExtractPageTextGeometry(firstPage);
                    string firstPageSummary = "";
                    if (!string.IsNullOrWhiteSpace(firstPageText))
                    {
                        var firstLine = firstPageText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                        firstPageSummary = firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
                    }

                    Bitmap? bmp1 = null;
                    try
                    {
                        using var pngStream = PdfPigExtensions.GetPageAsPng(doc, 1, PdfViewerPageItem.BasePageRenderScale, 100);
                        if (pngStream != null && pngStream.Length > 0)
                        {
                            pngStream.Position = 0;
                            bmp1 = new Bitmap(pngStream);
                        }
                    }
                    catch { }

                    // 2. Instant Skeleton Generation, with REAL per-page dimensions. Reading a
                    // page's declared size is cheap (the page dictionary's MediaBox) — nothing
                    // like the cost of full word/text extraction — so it's worth doing for every
                    // page right now rather than defaulting every page to page 1's size until
                    // the much slower progressive background sweep happens to reach it. Every
                    // scroll-position and click-to-navigate calculation sums page heights across
                    // however many preceding pages there are, so a wrong default anywhere in
                    // that chain drifts every page after it out of sync until the real value
                    // loads — for a large document, that's most of the book for a long time.
                    var pagesList = new List<PdfViewerPageItem>(total);
                    for (int i = 1; i <= total; i++)
                    {
                        double pageWidth = defaultWidth;
                        double pageHeight = defaultHeight;
                        int pageRot = (i == 1) ? defaultRot : 0;

                        if (i > 1)
                        {
                            try
                            {
                                var pg = doc.GetPage(i);
                                if (pg.Width > 0) pageWidth = pg.Width;
                                if (pg.Height > 0) pageHeight = pg.Height;
                                pageRot = (int)pg.Rotation.Value;
                            }
                            catch { }
                        }

                        pagesList.Add(new PdfViewerPageItem
                        {
                            PageNumber = i,
                            WidthPoints = pageWidth,
                            HeightPoints = pageHeight,
                            RotationAngle = pageRot,
                            ExtractedText = (i == 1) ? firstPageText : "",
                            Words = (i == 1) ? firstWords : new List<PdfViewerWordItem>(),
                            TextLines = (i == 1) ? firstLines : new List<PdfViewerTextLineItem>(),
                            PageSummary = (i == 1) ? firstPageSummary : "",
                            Bitmap = (i == 1) ? bmp1 : null,
                            RenderedScale = PdfViewerPageItem.BasePageRenderScale,
                            IsSelected = (i == 1)
                        });
                    }

                    // 3. Metadata
                    var info = doc.Information;
                    string dimsInches = $"{defaultWidth / 72.0:F1}\" × {defaultHeight / 72.0:F1}\"";
                    string dimsMm = $"{defaultWidth * 25.4 / 72.0:F0} × {defaultHeight * 25.4 / 72.0:F0} mm";

                    var metaList = new List<PdfViewerMetadataItem>
                    {
                        new PdfViewerMetadataItem { Label = "File Name", Value = DocumentTitle, IconKind = "FileDocumentOutline" },
                        new PdfViewerMetadataItem { Label = "File Size", Value = PdfFilePreviewItem.FormatBytes(pdfBytes.Length), IconKind = "DatabaseOutline" },
                        new PdfViewerMetadataItem { Label = "Total Pages", Value = $"{total} Pages", IconKind = "BookOpenPageVariantOutline" },
                        new PdfViewerMetadataItem { Label = "Page Dimensions", Value = $"{dimsInches} ({dimsMm})", IconKind = "AspectRatio" },
                        new PdfViewerMetadataItem { Label = "Title", Value = string.IsNullOrWhiteSpace(info.Title) ? "Untitled Document" : info.Title, IconKind = "FormatTitle" },
                        new PdfViewerMetadataItem { Label = "Author", Value = string.IsNullOrWhiteSpace(info.Author) ? "Unknown Author" : info.Author, IconKind = "AccountOutline" },
                        new PdfViewerMetadataItem { Label = "Subject", Value = string.IsNullOrWhiteSpace(info.Subject) ? "None specified" : info.Subject, IconKind = "Subject" },
                        new PdfViewerMetadataItem { Label = "Keywords", Value = string.IsNullOrWhiteSpace(info.Keywords) ? "None" : info.Keywords, IconKind = "TagOutline" },
                        new PdfViewerMetadataItem { Label = "Creator Application", Value = string.IsNullOrWhiteSpace(info.Creator) ? "FryPDF" : info.Creator, IconKind = "CogOutline" },
                        new PdfViewerMetadataItem { Label = "PDF Producer", Value = string.IsNullOrWhiteSpace(info.Producer) ? "codefrydev.in" : info.Producer, IconKind = "ApplicationOutline" },
                        new PdfViewerMetadataItem { Label = "PDF Version", Value = $"PDF {doc.Version}", IconKind = "ShieldCheckOutline" },
                        new PdfViewerMetadataItem { Label = "Security Status", Value = doc.IsEncrypted ? "Password Protected (Encrypted)" : "Standard (No Security)", IconKind = doc.IsEncrypted ? "LockOutline" : "LockOpenOutline" }
                    };

                    // Keep this document open for reuse by on-demand rendering/geometry
                    // extraction instead of parsing the whole PDF from scratch again on the
                    // very first page request right after this.
                    lock (_renderLock)
                    {
                        _openDocument?.Dispose();
                        _openDocument = doc;
                    }

                    return (metaList, pagesList, total);
                }
                catch
                {
                    doc.Dispose();
                    throw;
                }
            }, ct);

            foreach (var m in metaList) MetadataItems.Add(m);
            foreach (var p in pagesList) Pages.Add(p);

            TotalPagesCount = total;
            CurrentPageNumber = 1;
            JumpPageText = "1";
            SelectedPage = Pages.FirstOrDefault();
            HasDocument = true;
            IsLoading = false;
            IsOpeningDocument = false;
            StatusMessage = $"Ready • {total} pages";

            RebuildPageSpreads();

            // Start progressive background worker to render remaining pages, thumbnails, and bookmarks
            StartBackgroundWorker(_backgroundRenderCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Loading cancelled.";
            IsLoading = false;
            IsOpeningDocument = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsLoading = false;
            IsOpeningDocument = false;
        }
    }

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

    private static void ExtractBookmarksRecursive(IEnumerable<BookmarkNode> nodes, IList<PdfViewerBookmarkItem> targetList)
    {
        if (nodes == null) return;
        foreach (var node in nodes)
        {
            if (node is DocumentBookmarkNode docNode)
            {
                var bm = new PdfViewerBookmarkItem
                {
                    Title = docNode.Title ?? "Section",
                    PageNumber = Math.Max(1, docNode.PageNumber)
                };
                if (docNode.Children != null && docNode.Children.Count > 0)
                {
                    ExtractBookmarksRecursive(docNode.Children, bm.Children);
                }
                targetList.Add(bm);
            }
        }
    }

    // --- Navigation & Page Selection ---

    public void RequestScrollToPage(int pageNumber)
    {
        if (pageNumber >= 1 && pageNumber <= TotalPagesCount)
        {
            ScrollToPageRequested?.Invoke(pageNumber);
        }
    }

    [RelayCommand]
    public void SelectPage(PdfViewerPageItem? page)
    {
        if (page == null) return;
        SelectedPage = page;
        CurrentPageNumber = page.PageNumber;
        RequestScrollToPage(page.PageNumber);
    }

    [RelayCommand]
    public void SelectSpread(PdfViewerPageSpreadItem? spread)
    {
        if (spread == null) return;
        SelectedSpread = spread;
        if (spread.LeftPage != null)
        {
            CurrentPageNumber = spread.LeftPage.PageNumber;
        }
        else if (spread.RightPage != null)
        {
            CurrentPageNumber = spread.RightPage.PageNumber;
        }
        RequestScrollToPage(CurrentPageNumber);
    }

    [RelayCommand]
    public void JumpToBookmark(PdfViewerBookmarkItem? bookmark)
    {
        if (bookmark == null) return;
        int pageIndex = bookmark.PageNumber - 1;
        if (pageIndex >= 0 && pageIndex < Pages.Count)
        {
            SelectedPage = Pages[pageIndex];
            CurrentPageNumber = bookmark.PageNumber;
            RequestScrollToPage(bookmark.PageNumber);
        }
    }

    [RelayCommand]
    public void JumpToAnnotation(PdfViewerAnnotationItem? ann)
    {
        if (ann == null) return;
        int pageIndex = ann.PageNumber - 1;
        if (pageIndex >= 0 && pageIndex < Pages.Count)
        {
            SelectedPage = Pages[pageIndex];
            CurrentPageNumber = ann.PageNumber;
            RequestScrollToPage(ann.PageNumber);
        }
    }

    [RelayCommand]
    public void CommitJumpPage()
    {
        if (int.TryParse(JumpPageText, out int target) && target >= 1 && target <= TotalPagesCount)
        {
            CurrentPageNumber = target;
            RequestScrollToPage(target);
        }
        else
        {
            JumpPageText = CurrentPageNumber.ToString();
        }
    }

    [RelayCommand]
    public void NextPage()
    {
        if (IsTwoPageSpreadMode)
        {
            if (CurrentPageNumber + 2 <= TotalPagesCount)
            {
                CurrentPageNumber += 2;
            }
            else if (CurrentPageNumber < TotalPagesCount)
            {
                CurrentPageNumber = TotalPagesCount;
            }
        }
        else
        {
            if (CurrentPageNumber < TotalPagesCount)
            {
                CurrentPageNumber++;
            }
        }
        RequestScrollToPage(CurrentPageNumber);
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (IsTwoPageSpreadMode)
        {
            if (CurrentPageNumber - 2 >= 1)
            {
                CurrentPageNumber -= 2;
            }
            else
            {
                CurrentPageNumber = 1;
            }
        }
        else
        {
            if (CurrentPageNumber > 1)
            {
                CurrentPageNumber--;
            }
        }
        RequestScrollToPage(CurrentPageNumber);
    }

    [RelayCommand]
    public void FirstPage()
    {
        if (TotalPagesCount > 0)
        {
            CurrentPageNumber = 1;
            RequestScrollToPage(1);
        }
    }

    [RelayCommand]
    public void LastPage()
    {
        if (TotalPagesCount > 0)
        {
            CurrentPageNumber = TotalPagesCount;
            RequestScrollToPage(TotalPagesCount);
        }
    }

    // --- Layout & View Modes ---

    [RelayCommand]
    public void SetContinuousScrollLayout()
    {
        SetLayoutMode("ContinuousScroll");
    }

    [RelayCommand]
    public void SetSinglePageLayout()
    {
        SetLayoutMode("SinglePage");
    }

    [RelayCommand]
    public void SetTwoPageSpreadLayout()
    {
        SetLayoutMode("TwoPageSpread");
    }

    [RelayCommand]
    public void SetLayoutMode(string? modeStr)
    {
        if (Enum.TryParse<PdfViewLayoutMode>(modeStr, true, out var mode))
        {
            SelectedLayoutMode = mode;
            ShowToastRequested?.Invoke(mode switch
            {
                PdfViewLayoutMode.ContinuousScroll => "Continuous Scroll Layout",
                PdfViewLayoutMode.SinglePage => "Single Page Fit Layout",
                PdfViewLayoutMode.TwoPageSpread => "Two-Page Book Spread Layout",
                _ => "Layout Changed"
            });
        }
    }

    // --- Reading Themes ---

    [RelayCommand]
    public void SetDefaultReadingTheme()
    {
        SetReadingTheme("Default");
    }

    [RelayCommand]
    public void SetSepiaReadingTheme()
    {
        SetReadingTheme("Sepia");
    }

    [RelayCommand]
    public void SetDarkReadingTheme()
    {
        SetReadingTheme("Dark");
    }

    [RelayCommand]
    public void SetHighContrastReadingTheme()
    {
        SetReadingTheme("HighContrast");
    }

    [RelayCommand]
    public void SetReadingTheme(string? themeStr)
    {
        if (Enum.TryParse<PdfReaderTheme>(themeStr, true, out var theme))
        {
            ReadingTheme = theme;
            ShowToastRequested?.Invoke(theme switch
            {
                PdfReaderTheme.Sepia => "Warm Sepia Book Reading Mode",
                PdfReaderTheme.Dark => "Dark Night Reading Mode",
                PdfReaderTheme.HighContrast => "High Contrast Reading Mode",
                _ => "Standard Daylight Reading Mode"
            });
        }
    }

    // --- Zoom Controls ---

    [RelayCommand]
    public void ZoomIn()
    {
        ZoomMode = PdfViewerZoomMode.Custom;
        ZoomLevel = Math.Min(5.0, Math.Round(ZoomLevel + 0.25, 2));
    }

    [RelayCommand]
    public void ZoomOut()
    {
        ZoomMode = PdfViewerZoomMode.Custom;
        ZoomLevel = Math.Max(0.25, Math.Round(ZoomLevel - 0.25, 2));
    }

    [RelayCommand]
    public void ResetZoom()
    {
        ZoomMode = PdfViewerZoomMode.Custom;
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    public void FitToWidth()
    {
        if (ViewportSizeProvider != null)
        {
            var dims = ViewportSizeProvider();
            if (dims.ViewportWidth > 100)
            {
                FitToWidthDynamic(dims.ViewportWidth, dims.HorizontalPadding);
                return;
            }
        }

        _isApplyingFitZoom = true;
        try
        {
            ZoomMode = PdfViewerZoomMode.FitWidth;
            ZoomLevel = 1.35;
        }
        finally
        {
            _isApplyingFitZoom = false;
        }
    }

    public void FitToWidthDynamic(double viewportWidth, double horizontalPadding = 64.0)
    {
        if (viewportWidth <= 100) return;

        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber) ?? Pages.FirstOrDefault();
        if (page == null)
        {
            _isApplyingFitZoom = true;
            try
            {
                ZoomMode = PdfViewerZoomMode.FitWidth;
                ZoomLevel = 1.35;
            }
            finally
            {
                _isApplyingFitZoom = false;
            }
            return;
        }

        double availableWidth = Math.Max(100.0, viewportWidth - horizontalPadding - 8.0);
        double targetWidth = (page.RotationAngle % 180 == 0) ? page.WidthPoints : page.HeightPoints;

        if (IsTwoPageSpreadMode && SelectedSpread != null)
        {
            if (SelectedSpread.LeftPage != null && SelectedSpread.RightPage != null)
            {
                double leftW = (SelectedSpread.LeftPage.RotationAngle % 180 == 0) ? SelectedSpread.LeftPage.WidthPoints : SelectedSpread.LeftPage.HeightPoints;
                double rightW = (SelectedSpread.RightPage.RotationAngle % 180 == 0) ? SelectedSpread.RightPage.WidthPoints : SelectedSpread.RightPage.HeightPoints;
                targetWidth = leftW + rightW;
                availableWidth = Math.Max(100.0, availableWidth - 16.0); // 16px gap between pages
            }
            else
            {
                var single = SelectedSpread.LeftPage ?? SelectedSpread.RightPage ?? page;
                targetWidth = (single.RotationAngle % 180 == 0) ? single.WidthPoints : single.HeightPoints;
            }
        }

        if (targetWidth > 10.0)
        {
            double calculatedZoom = Math.Clamp(Math.Round(availableWidth / targetWidth, 2), 0.25, 5.0);
            if (calculatedZoom * targetWidth > availableWidth && calculatedZoom > 0.25)
            {
                calculatedZoom = Math.Max(0.25, Math.Round(calculatedZoom - 0.01, 2));
            }

            _isApplyingFitZoom = true;
            try
            {
                ZoomMode = PdfViewerZoomMode.FitWidth;
                ZoomLevel = calculatedZoom;
            }
            finally
            {
                _isApplyingFitZoom = false;
            }

            StatusMessage = $"Fit to Width ({(int)Math.Round(ZoomLevel * 100)}%)";
            ShowToastRequested?.Invoke($"Fit to Width ({(int)Math.Round(ZoomLevel * 100)}%)");
        }
    }

    [RelayCommand]
    public void FitToPage()
    {
        if (ViewportSizeProvider != null)
        {
            var dims = ViewportSizeProvider();
            if (dims.ViewportWidth > 100 && dims.ViewportHeight > 100)
            {
                FitToPageDynamic(dims.ViewportWidth, dims.ViewportHeight, dims.HorizontalPadding, dims.VerticalPadding);
                return;
            }
        }

        _isApplyingFitZoom = true;
        try
        {
            ZoomMode = PdfViewerZoomMode.FitPage;
            ZoomLevel = 0.95;
        }
        finally
        {
            _isApplyingFitZoom = false;
        }
    }

    public void FitToPageDynamic(double viewportWidth, double viewportHeight, double horizontalPadding = 64.0, double verticalPadding = 64.0)
    {
        if (viewportWidth <= 100 || viewportHeight <= 100) return;

        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber) ?? Pages.FirstOrDefault();
        if (page == null)
        {
            _isApplyingFitZoom = true;
            try
            {
                ZoomMode = PdfViewerZoomMode.FitPage;
                ZoomLevel = 0.95;
            }
            finally
            {
                _isApplyingFitZoom = false;
            }
            return;
        }

        double availableWidth = Math.Max(100.0, viewportWidth - horizontalPadding - 8.0);
        // Reserve 36px for page footnote indicator and card vertical spacing
        double availableHeight = Math.Max(100.0, viewportHeight - verticalPadding - 36.0);

        double targetWidth = (page.RotationAngle % 180 == 0) ? page.WidthPoints : page.HeightPoints;
        double targetHeight = (page.RotationAngle % 180 == 0) ? page.HeightPoints : page.WidthPoints;

        if (IsTwoPageSpreadMode && SelectedSpread != null)
        {
            if (SelectedSpread.LeftPage != null && SelectedSpread.RightPage != null)
            {
                double leftW = (SelectedSpread.LeftPage.RotationAngle % 180 == 0) ? SelectedSpread.LeftPage.WidthPoints : SelectedSpread.LeftPage.HeightPoints;
                double leftH = (SelectedSpread.LeftPage.RotationAngle % 180 == 0) ? SelectedSpread.LeftPage.HeightPoints : SelectedSpread.LeftPage.WidthPoints;
                double rightW = (SelectedSpread.RightPage.RotationAngle % 180 == 0) ? SelectedSpread.RightPage.WidthPoints : SelectedSpread.RightPage.HeightPoints;
                double rightH = (SelectedSpread.RightPage.RotationAngle % 180 == 0) ? SelectedSpread.RightPage.HeightPoints : SelectedSpread.RightPage.WidthPoints;
                targetWidth = leftW + rightW;
                targetHeight = Math.Max(leftH, rightH);
                availableWidth = Math.Max(100.0, availableWidth - 16.0);
            }
            else
            {
                var single = SelectedSpread.LeftPage ?? SelectedSpread.RightPage ?? page;
                targetWidth = (single.RotationAngle % 180 == 0) ? single.WidthPoints : single.HeightPoints;
                targetHeight = (single.RotationAngle % 180 == 0) ? single.HeightPoints : single.WidthPoints;
            }
        }

        if (targetWidth > 10.0 && targetHeight > 10.0)
        {
            double scaleX = availableWidth / targetWidth;
            double scaleY = availableHeight / targetHeight;
            double calculatedZoom = Math.Clamp(Math.Round(Math.Min(scaleX, scaleY), 2), 0.25, 5.0);
            if ((calculatedZoom * targetWidth > availableWidth || calculatedZoom * targetHeight > availableHeight) && calculatedZoom > 0.25)
            {
                calculatedZoom = Math.Max(0.25, Math.Round(calculatedZoom - 0.01, 2));
            }

            _isApplyingFitZoom = true;
            try
            {
                ZoomMode = PdfViewerZoomMode.FitPage;
                ZoomLevel = calculatedZoom;
            }
            finally
            {
                _isApplyingFitZoom = false;
            }

            StatusMessage = $"Fit to Page ({(int)Math.Round(ZoomLevel * 100)}%)";
            ShowToastRequested?.Invoke($"Fit to Page ({(int)Math.Round(ZoomLevel * 100)}%)");
        }
    }

    [RelayCommand]
    public void SetZoomPreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset)) return;
        string clean = preset.Replace("%", "").Trim();
        if (double.TryParse(clean, out double val))
        {
            ZoomMode = PdfViewerZoomMode.Custom;
            ZoomLevel = Math.Clamp(val / 100.0, 0.25, 5.0);
        }
    }

    // --- Page Operations & Visual Rotation ---

    [RelayCommand]
    public void RotateClockwise()
    {
        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber) ?? Pages.FirstOrDefault();
        if (page != null)
        {
            SelectedPage = page;
            page.RotationAngle = (page.RotationAngle + 90) % 360;
            StatusMessage = $"Page {page.PageNumber} rotated 90° CW ({page.RotationAngle}°).";
            ShowToastRequested?.Invoke($"Page {page.PageNumber} rotated 90° CW ({page.RotationAngle}°)");

            if (IsFitToPageActive)
            {
                FitToPage();
            }
            else if (IsFitToWidthActive)
            {
                FitToWidth();
            }
        }
    }

    [RelayCommand]
    public void RotateCounterClockwise()
    {
        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber) ?? Pages.FirstOrDefault();
        if (page != null)
        {
            SelectedPage = page;
            page.RotationAngle = (page.RotationAngle + 270) % 360;
            StatusMessage = $"Page {page.PageNumber} rotated 90° CCW ({page.RotationAngle}°).";
            ShowToastRequested?.Invoke($"Page {page.PageNumber} rotated 90° CCW ({page.RotationAngle}°)");

            if (IsFitToPageActive)
            {
                FitToPage();
            }
            else if (IsFitToWidthActive)
            {
                FitToWidth();
            }
        }
    }

    [RelayCommand]
    public void RotateAllPagesClockwise()
    {
        foreach (var p in Pages)
        {
            p.RotationAngle = (p.RotationAngle + 90) % 360;
        }
        StatusMessage = "Rotated all pages 90° clockwise.";
        ShowToastRequested?.Invoke("Rotated all pages 90° CW");

        if (IsFitToPageActive)
        {
            FitToPage();
        }
        else if (IsFitToWidthActive)
        {
            FitToWidth();
        }
    }

    // --- Text Search & Find in Document ---

    [RelayCommand]
    public void ToggleSearchBar()
    {
        IsSearchBarVisible = !IsSearchBarVisible;
        if (IsSearchBarVisible)
        {
            SelectedSidebarTab = PdfViewerSidebarTab.Search;
            IsSidebarOpen = true;
        }
    }

    [RelayCommand]
    public void PerformSearch()
    {
        SearchResults.Clear();
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            TotalMatchesCount = 0;
            CurrentMatchIndex = 0;
            SearchStatusText = string.Empty;
            OnPropertyChanged(nameof(HasSearchResults));
            return;
        }

        string q = SearchQuery.Trim();
        int matchIdx = 1;

        if (SearchWholeWord)
        {
            var regexOptions = SearchMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
            string pattern = $@"\b{Regex.Escape(q)}\b";

            foreach (var page in Pages)
            {
                if (string.IsNullOrEmpty(page.ExtractedText)) continue;

                var matches = Regex.Matches(page.ExtractedText, pattern, regexOptions);
                foreach (Match m in matches)
                {
                    int snippetStart = Math.Max(0, m.Index - 25);
                    int snippetLen = Math.Min(page.ExtractedText.Length - snippetStart, m.Length + 50);
                    string snippet = "..." + page.ExtractedText.Substring(snippetStart, snippetLen).Replace('\r', ' ').Replace('\n', ' ') + "...";

                    SearchResults.Add(new PdfViewerSearchMatch
                    {
                        PageNumber = page.PageNumber,
                        Snippet = snippet,
                        MatchIndex = matchIdx++
                    });
                }
            }
        }
        else
        {
            var comp = SearchMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            foreach (var page in Pages)
            {
                if (string.IsNullOrEmpty(page.ExtractedText)) continue;

                int startIndex = 0;
                while ((startIndex = page.ExtractedText.IndexOf(q, startIndex, comp)) != -1)
                {
                    int snippetStart = Math.Max(0, startIndex - 25);
                    int snippetLen = Math.Min(page.ExtractedText.Length - snippetStart, q.Length + 50);
                    string snippet = "..." + page.ExtractedText.Substring(snippetStart, snippetLen).Replace('\r', ' ').Replace('\n', ' ') + "...";

                    SearchResults.Add(new PdfViewerSearchMatch
                    {
                        PageNumber = page.PageNumber,
                        Snippet = snippet,
                        MatchIndex = matchIdx++
                    });

                    startIndex += q.Length;
                }
            }
        }

        TotalMatchesCount = SearchResults.Count;
        CurrentMatchIndex = TotalMatchesCount > 0 ? 1 : 0;
        SearchStatusText = TotalMatchesCount > 0 ? $"{CurrentMatchIndex} of {TotalMatchesCount} matches" : "No matches found";
        OnPropertyChanged(nameof(HasSearchResults));

        if (TotalMatchesCount > 0)
        {
            JumpToMatch(SearchResults[0]);
        }
    }

    [RelayCommand]
    public void NextMatch()
    {
        if (TotalMatchesCount == 0) return;
        CurrentMatchIndex = (CurrentMatchIndex % TotalMatchesCount) + 1;
        SearchStatusText = $"{CurrentMatchIndex} of {TotalMatchesCount} matches";
        JumpToMatch(SearchResults[CurrentMatchIndex - 1]);
    }

    [RelayCommand]
    public void PreviousMatch()
    {
        if (TotalMatchesCount == 0) return;
        CurrentMatchIndex = (CurrentMatchIndex - 2 + TotalMatchesCount) % TotalMatchesCount + 1;
        SearchStatusText = $"{CurrentMatchIndex} of {TotalMatchesCount} matches";
        JumpToMatch(SearchResults[CurrentMatchIndex - 1]);
    }

    [RelayCommand]
    public void JumpToMatch(PdfViewerSearchMatch? match)
    {
        if (match == null) return;
        int pageIdx = match.PageNumber - 1;
        if (pageIdx >= 0 && pageIdx < Pages.Count)
        {
            SelectedPage = Pages[pageIdx];
            CurrentPageNumber = match.PageNumber;
            RequestScrollToPage(match.PageNumber);
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        TotalMatchesCount = 0;
        CurrentMatchIndex = 0;
        SearchStatusText = string.Empty;
        OnPropertyChanged(nameof(HasSearchResults));
    }

    // --- Interactive Annotations & Review Markups ---

    [RelayCommand]
    public void SetHighlightColor(string? colorHex)
    {
        if (!string.IsNullOrWhiteSpace(colorHex))
        {
            SelectedHighlightColorHex = colorHex;
        }
    }

    [RelayCommand]
    public void AddHighlightAnnotation(string? customColorHex = null)
    {
        var page = Pages.FirstOrDefault(p => p.PageNumber == ActiveSelectedPageNumber) ?? SelectedPage;
        if (page == null) return;
        string color = string.IsNullOrWhiteSpace(customColorHex) ? SelectedHighlightColorHex : customColorHex;
        string textToHighlight = !string.IsNullOrWhiteSpace(ActiveSelectedText) ? ActiveSelectedText : page.SelectedText;

        var highlightRects = new List<Rect>(page.SelectionRects);

        var ann = new PdfViewerAnnotationItem
        {
            Type = "Highlight",
            PageNumber = page.PageNumber,
            Author = "Reader Reviewer",
            Content = !string.IsNullOrWhiteSpace(textToHighlight) ? textToHighlight : $"Highlighted text passage on Page {page.PageNumber}",
            ColorHex = color,
            IconKind = "FormatColorHighlight",
            HighlightRects = highlightRects
        };
        Annotations.Add(ann);
        page.PageAnnotations.Add(ann);
        OnPropertyChanged(nameof(HasAnnotations));
        page.ClearSelection();
        ClearSelection();
        SelectedSidebarTab = PdfViewerSidebarTab.Annotations;
        ShowToastRequested?.Invoke($"Added Highlight on Page {page.PageNumber}");
    }

    [RelayCommand]
    public void HighlightSelectedText(string? customColorHex = null)
    {
        AddHighlightAnnotation(customColorHex);
    }

    [RelayCommand]
    public void OpenAddNoteDialog()
    {
        NewNoteText = string.Empty;
        IsAddNoteOpen = true;
    }

    [RelayCommand]
    public void AddNoteFromSelection()
    {
        if (!string.IsNullOrWhiteSpace(ActiveSelectedText))
        {
            NewNoteText = $"Re: \"{ActiveSelectedText}\"\n\n";
        }
        else
        {
            NewNoteText = string.Empty;
        }
        IsAddNoteOpen = true;
    }

    [RelayCommand]
    public void ConfirmAddNote()
    {
        if (SelectedPage == null || string.IsNullOrWhiteSpace(NewNoteText))
        {
            IsAddNoteOpen = false;
            return;
        }

        var ann = new PdfViewerAnnotationItem
        {
            Type = "StickyNote",
            PageNumber = SelectedPage.PageNumber,
            Author = "Reader Note",
            Content = NewNoteText.Trim(),
            ColorHex = "#38BDF8",
            IconKind = "NoteTextOutline"
        };
        Annotations.Add(ann);
        SelectedPage.PageAnnotations.Add(ann);
        OnPropertyChanged(nameof(HasAnnotations));
        IsAddNoteOpen = false;
        SelectedSidebarTab = PdfViewerSidebarTab.Annotations;
        ShowToastRequested?.Invoke($"Added Sticky Note on Page {SelectedPage.PageNumber}");
    }

    [RelayCommand]
    public void AddStamp(string? stampText)
    {
        if (SelectedPage == null) return;
        string text = string.IsNullOrWhiteSpace(stampText) ? "APPROVED" : stampText;
        string color = text switch
        {
            "REJECTED" => "#EF4444",
            "CONFIDENTIAL" => "#DC2626",
            "DRAFT" => "#F59E0B",
            "FINAL" => "#8B5CF6",
            "REVIEWED" => "#3B82F6",
            _ => "#10B981" // APPROVED
        };

        var ann = new PdfViewerAnnotationItem
        {
            Type = "Stamp",
            PageNumber = SelectedPage.PageNumber,
            Author = "Auditor",
            Content = $"Stamp: {text}",
            ColorHex = color,
            IconKind = "Stamp"
        };
        Annotations.Add(ann);
        SelectedPage.PageAnnotations.Add(ann);
        OnPropertyChanged(nameof(HasAnnotations));
        IsAddStampOpen = false;
        SelectedSidebarTab = PdfViewerSidebarTab.Annotations;
        ShowToastRequested?.Invoke($"Applied '{text}' stamp on Page {SelectedPage.PageNumber}");
    }

    [RelayCommand]
    public void DeleteAnnotation(PdfViewerAnnotationItem? ann)
    {
        if (ann == null) return;
        Annotations.Remove(ann);
        foreach (var p in Pages)
        {
            p.PageAnnotations.Remove(ann);
            p.NotifySelectionChanged();
        }
        OnPropertyChanged(nameof(HasAnnotations));
    }

    // --- Interactive Selection & Clipboard Operations ---

    [RelayCommand]
    public async Task CopySelectedTextAsync()
    {
        if (string.IsNullOrWhiteSpace(ActiveSelectedText))
        {
            if (SelectedPage != null && !string.IsNullOrWhiteSpace(SelectedPage.ExtractedText))
            {
                await CopyPageTextAsync();
            }
            return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(ActiveSelectedText);
                string snippet = ActiveSelectedText.Length > 40 ? ActiveSelectedText.Substring(0, 40) + "..." : ActiveSelectedText;
                ShowToastRequested?.Invoke($"Copied: \"{snippet}\"");
            }
        }
    }

    [RelayCommand]
    public async Task CopySelectedCitationAsync()
    {
        if (string.IsNullOrWhiteSpace(ActiveSelectedText)) return;

        string citation = $"\"{ActiveSelectedText}\"\n— Page {ActiveSelectedPageNumber}, {DocumentTitle}";
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(citation);
                ShowToastRequested?.Invoke("Copied citation with page reference");
            }
        }
    }

    [RelayCommand]
    public void SearchSelectedText()
    {
        if (string.IsNullOrWhiteSpace(ActiveSelectedText)) return;
        SearchQuery = ActiveSelectedText.Trim();
        IsSearchBarVisible = true;
        SelectedSidebarTab = PdfViewerSidebarTab.Search;
        IsSidebarOpen = true;
        PerformSearch();
    }

    [RelayCommand]
    public void SearchWebSelectedText()
    {
        if (string.IsNullOrWhiteSpace(ActiveSelectedText)) return;
        try
        {
            string query = Uri.EscapeDataString(ActiveSelectedText.Trim());
            string url = $"https://www.google.com/search?q={query}";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch { }
    }

    [RelayCommand]
    public void SelectAllPageText()
    {
        var page = SelectedPage ?? Pages.FirstOrDefault(p => p.PageNumber == CurrentPageNumber);
        if (page == null) return;
        page.SelectAll();
        ActiveSelectedText = page.SelectedText;
        ActiveSelectedPageNumber = page.PageNumber;
        HasTextSelection = !string.IsNullOrEmpty(ActiveSelectedText);
        ShowToastRequested?.Invoke($"Selected all text on Page {page.PageNumber}");
    }

    [RelayCommand]
    public void ClearSelection()
    {
        ActiveSelectedText = string.Empty;
        HasTextSelection = false;
        foreach (var p in Pages)
        {
            p.ClearSelection();
        }
    }

    [RelayCommand]
    public async Task CopyPageTextAsync()
    {
        if (SelectedPage == null || string.IsNullOrWhiteSpace(SelectedPage.ExtractedText))
        {
            ShowToastRequested?.Invoke("No text on current page to copy.");
            return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(SelectedPage.ExtractedText);
                ShowToastRequested?.Invoke($"Copied all text from Page {SelectedPage.PageNumber} to Clipboard.");
            }
        }
    }

    [RelayCommand]
    public async Task CopyAllDocumentTextAsync()
    {
        var sb = new StringBuilder();
        foreach (var page in Pages)
        {
            if (!string.IsNullOrWhiteSpace(page.ExtractedText))
            {
                sb.AppendLine($"--- Page {page.PageNumber} ---");
                sb.AppendLine(page.ExtractedText);
                sb.AppendLine();
            }
        }

        if (sb.Length == 0)
        {
            ShowToastRequested?.Invoke("No text found in document.");
            return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(sb.ToString());
                ShowToastRequested?.Invoke($"Copied text from all {Pages.Count} pages to Clipboard.");
            }
        }
    }

    // --- File Operations: Open Another PDF, Save As ---

    [RelayCommand]
    public async Task OpenAnotherPdfAsync()
    {
        if (StorageProvider == null)
        {
            OpenFileRequested?.Invoke();
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open PDF Document to Read",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PDF Documents (*.pdf)")
                {
                    Patterns = new[] { "*.pdf" }
                }
            }
        });

        if (files.Count > 0)
        {
            string chosenPath = files[0].Path.LocalPath;
            await LoadDocumentAsync(chosenPath);
            ShowToastRequested?.Invoke($"Reading: {Path.GetFileName(chosenPath)}");
        }
    }

    [RelayCommand]
    public async Task SaveAsAsync()
    {
        if (_currentPdfBytes == null || _currentPdfBytes.Length == 0 || StorageProvider == null) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PDF As",
            DefaultExtension = "pdf",
            SuggestedFileName = DocumentTitle,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF Documents (*.pdf)")
                {
                    Patterns = new[] { "*.pdf" }
                }
            }
        });

        if (file != null)
        {
            await File.WriteAllBytesAsync(file.Path.LocalPath, _currentPdfBytes);
            ShowToastRequested?.Invoke($"Saved copy: {Path.GetFileName(file.Path.LocalPath)}");
        }
    }

    // --- Sidebar & Layout Commands ---

    [RelayCommand]
    public void SelectSidebarTab(string tabName)
    {
        if (Enum.TryParse<PdfViewerSidebarTab>(tabName, true, out var tab))
        {
            SelectedSidebarTab = tab;
            IsSidebarOpen = true;
        }
    }

    [RelayCommand]
    public void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    [RelayCommand]
    public void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        ShowToastRequested?.Invoke(IsFullscreen ? "Entered Presentation Reading Mode (Esc to exit)" : "Exited Fullscreen Mode");
    }

    // --- Bridge to Studio & Tools ---

    [RelayCommand]
    public void EditInStudio()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath) && File.Exists(CurrentFilePath))
        {
            EditInStudioRequested?.Invoke(CurrentFilePath);
        }
        else if (_currentPdfBytes != null)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), DocumentTitle);
            File.WriteAllBytes(tempPath, _currentPdfBytes);
            EditInStudioRequested?.Invoke(tempPath);
        }
    }

    [RelayCommand]
    public void RunToolOnDocument(string? toolIdStr)
    {
        if (!string.IsNullOrEmpty(CurrentFilePath) && File.Exists(CurrentFilePath) && Enum.TryParse<PdfToolId>(toolIdStr, true, out var toolId))
        {
            RunToolRequested?.Invoke(toolId, CurrentFilePath);
        }
    }

    [RelayCommand]
    public void BackToHome()
    {
        BackToHomeRequested?.Invoke();
    }
}
