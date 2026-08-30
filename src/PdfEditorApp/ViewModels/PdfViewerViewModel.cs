using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

namespace PdfEditorApp.ViewModels;

public enum PdfViewerSidebarTab
{
    Thumbnails,
    Bookmarks,
    Annotations,
    Search,
    Info
}

public class PdfViewerPageItem : ObservableObject
{
    private bool _isSelected;
    private int _rotationAngle;
    private float _renderedScale = 2.75f;
    private Bitmap? _thumbnailBitmap;
    private Bitmap? _bitmap;

    public int PageNumber { get; set; }
    public double WidthPoints { get; set; }
    public double HeightPoints { get; set; }
    public string DimensionsText => $"{Math.Round(WidthPoints):F0} × {Math.Round(HeightPoints):F0} pt";
    public string PageLabel => $"Page {PageNumber}";
    public string PageSummary { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;

    public float RenderedScale
    {
        get => _renderedScale;
        set => SetProperty(ref _renderedScale, value);
    }

    public Bitmap? ThumbnailBitmap
    {
        get => _thumbnailBitmap ?? _bitmap;
        set => SetProperty(ref _thumbnailBitmap, value);
    }

    public Bitmap? Bitmap
    {
        get => _bitmap;
        set
        {
            if (SetProperty(ref _bitmap, value))
            {
                OnPropertyChanged(nameof(ThumbnailBitmap));
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
/// document outline / bookmarks, in-document search, annotations, and seamless bridge to FryPDF Studio Editor.
/// </summary>
public partial class PdfViewerViewModel : ViewModelBase
{
    private CancellationTokenSource? _renderCts;
    private CancellationTokenSource? _zoomDebounceCts;
    private CancellationTokenSource? _backgroundRenderCts;
    private readonly object _renderLock = new();
    private byte[]? _currentPdfBytes;
    private string? _currentPassword;

    public IStorageProvider? StorageProvider { get; set; }

    // --- Events ---
    public event Action<string>? EditInStudioRequested;
    public event Action? BackToHomeRequested;
    public event Action<PdfToolId, string>? RunToolRequested;
    public event Action<string>? ShowToastRequested;
    public event Action? OpenFileRequested;

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
    private string _themeBackgroundHex = "#0F172A";

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

    partial void OnZoomLevelChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomPercentageText));

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

                float dynamicScale = Math.Clamp((float)(value * 2.25f), 2.5f, 5.0f);
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

    partial void OnCurrentPageNumberChanged(int value)
    {
        OnPropertyChanged(nameof(PageNavigationDisplay));
        JumpPageText = value.ToString();
        if (value >= 1 && value <= Pages.Count)
        {
            SelectedPage = Pages[value - 1];
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
            CurrentPageNumber = value.PageNumber;
        }
    }

    partial void OnSelectedLayoutModeChanged(PdfViewLayoutMode value)
    {
        IsContinuousScroll = (value == PdfViewLayoutMode.ContinuousScroll);
        IsSinglePageMode = (value == PdfViewLayoutMode.SinglePage);
        IsTwoPageSpreadMode = (value == PdfViewLayoutMode.TwoPageSpread);

        if (IsTwoPageSpreadMode && PageSpreads.Count == 0 && Pages.Count > 0)
        {
            RebuildPageSpreads();
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
                ThemePaperBackgroundHex = "#FBF0D9";
                ThemeTextColorHex = "#433422";
                ThemeBorderColorHex = "#E6D5B8";
                break;
            case PdfReaderTheme.Dark:
                ThemePaperBackgroundHex = "#1E293B";
                ThemeTextColorHex = "#F1F5F9";
                ThemeBorderColorHex = "#334155";
                break;
            case PdfReaderTheme.HighContrast:
                ThemePaperBackgroundHex = "#000000";
                ThemeTextColorHex = "#FFFF00";
                ThemeBorderColorHex = "#FFFF00";
                break;
            case PdfReaderTheme.Default:
            default:
                ThemePaperBackgroundHex = "#FFFFFF";
                ThemeTextColorHex = "#0F172A";
                ThemeBorderColorHex = "#E2E8F0";
                break;
        }
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

    // --- Core Document Loading ---

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

                using (doc)
                {
                    try
                    {
                        PdfPigExtensions.AddSkiaPageFactory(doc);
                    }
                    catch { }

                    int total = doc.NumberOfPages;
                    if (total == 0) return (new List<PdfViewerMetadataItem>(), new List<PdfViewerPageItem>(), 0);

                    // 1. Fast Page 1 extraction & immediate render
                    var firstPage = doc.GetPage(1);
                    double defaultWidth = Math.Max(100, firstPage.Width);
                    double defaultHeight = Math.Max(100, firstPage.Height);
                    int defaultRot = (int)firstPage.Rotation.Value;
                    string firstPageText = firstPage.Text ?? "";
                    string firstPageSummary = "";
                    if (!string.IsNullOrWhiteSpace(firstPageText))
                    {
                        var firstLine = firstPageText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                        firstPageSummary = firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
                    }

                    Bitmap? bmp1 = null;
                    try
                    {
                        using var pngStream = PdfPigExtensions.GetPageAsPng(doc, 1, 2.75f, 100);
                        if (pngStream != null && pngStream.Length > 0)
                        {
                            pngStream.Position = 0;
                            bmp1 = new Bitmap(pngStream);
                        }
                    }
                    catch { }

                    // 2. Instant Skeleton Generation (< 0.5ms for all 500+ pages)
                    var pagesList = new List<PdfViewerPageItem>(total);
                    for (int i = 1; i <= total; i++)
                    {
                        pagesList.Add(new PdfViewerPageItem
                        {
                            PageNumber = i,
                            WidthPoints = defaultWidth,
                            HeightPoints = defaultHeight,
                            RotationAngle = (i == 1) ? defaultRot : 0,
                            ExtractedText = (i == 1) ? firstPageText : "",
                            PageSummary = (i == 1) ? firstPageSummary : "",
                            Bitmap = (i == 1) ? bmp1 : null,
                            RenderedScale = 2.75f,
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
                        new PdfViewerMetadataItem { Label = "Creator Application", Value = string.IsNullOrWhiteSpace(info.Creator) ? "PDF Engine" : info.Creator, IconKind = "CogOutline" },
                        new PdfViewerMetadataItem { Label = "PDF Producer", Value = string.IsNullOrWhiteSpace(info.Producer) ? "FryPDF Studio" : info.Producer, IconKind = "ApplicationOutline" },
                        new PdfViewerMetadataItem { Label = "PDF Version", Value = $"PDF {doc.Version}", IconKind = "ShieldCheckOutline" },
                        new PdfViewerMetadataItem { Label = "Security Status", Value = doc.IsEncrypted ? "Password Protected (Encrypted)" : "Standard (No Security)", IconKind = doc.IsEncrypted ? "LockOutline" : "LockOpenOutline" }
                    };

                    return (metaList, pagesList, total);
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

                using (doc)
                {
                    try
                    {
                        PdfPigExtensions.AddSkiaPageFactory(doc);
                    }
                    catch { }

                    using var stream = PdfPigExtensions.GetPageAsPng(doc, pageNumber, scale, 100);
                    if (stream != null && stream.Length > 0)
                    {
                        return stream.ToArray();
                    }
                }
            }
            catch { }
        }
        return null;
    }

    /// <summary>Renders a specific page at the specified scale directly from PDF bytes using Skia.</summary>
    public Bitmap? RenderPageAtScale(int pageNumber, float scale)
    {
        var bytes = RenderPageBytesAtScale(pageNumber, scale);
        if (bytes != null && bytes.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                return new Bitmap(ms);
            }
            catch { }
        }
        return null;
    }

    /// <summary>Ensures a page is rendered at the appropriate scale with top priority.</summary>
    public void EnsurePageRendered(int pageNumber, float? scale = null)
    {
        var page = Pages.FirstOrDefault(p => p.PageNumber == pageNumber);
        if (page == null) return;

        float targetScale = scale ?? Math.Clamp((float)(ZoomLevel * 2.25f), 2.75f, 5.0f);
        if (page.Bitmap == null || Math.Abs(page.RenderedScale - targetScale) > 0.5f)
        {
            Task.Run(() =>
            {
                var bmp = RenderPageAtScale(pageNumber, targetScale);
                if (bmp != null)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        page.Bitmap = bmp;
                        page.RenderedScale = targetScale;
                    });
                }
            });
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
                var parsingOptions = new ParsingOptions();
                if (!string.IsNullOrEmpty(_currentPassword))
                {
                    parsingOptions.Password = _currentPassword;
                }

                using var doc = PdfDocument.Open(_currentPdfBytes, parsingOptions);
                if (doc.TryGetBookmarks(out var bookmarks) && bookmarks != null && bookmarks.Roots != null)
                {
                    var bookmarksList = new List<PdfViewerBookmarkItem>();
                    ExtractBookmarksRecursive(bookmarks.Roots, bookmarksList);
                    if (bookmarksList.Count > 0 && !ct.IsCancellationRequested)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            Bookmarks.Clear();
                            foreach (var b in bookmarksList) Bookmarks.Add(b);
                            OnPropertyChanged(nameof(HasBookmarks));
                        });
                    }
                }
            }
            catch { }

            // 2. Progressively render remaining pages and extract accurate text / geometry
            for (int i = 1; i <= Pages.Count; i++)
            {
                if (ct.IsCancellationRequested) return;

                int pageNum = i;
                var page = Pages.FirstOrDefault(p => p.PageNumber == pageNum);
                if (page != null)
                {
                    // Render page bitmap if not yet rendered
                    if (page.Bitmap == null)
                    {
                        var bmp = RenderPageAtScale(pageNum, 2.75f);
                        if (bmp != null && !ct.IsCancellationRequested)
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                page.Bitmap = bmp;
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
                    if (pageNum > 1 && string.IsNullOrEmpty(page.ExtractedText))
                    {
                        try
                        {
                            using var doc = PdfDocument.Open(_currentPdfBytes);
                            var p = doc.GetPage(pageNum);
                            string txt = p.Text ?? "";
                            double w = Math.Max(100, p.Width);
                            double h = Math.Max(100, p.Height);
                            int rot = (int)p.Rotation.Value;

                            string summary = "";
                            if (!string.IsNullOrWhiteSpace(txt))
                            {
                                var firstLine = txt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                                summary = firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
                            }

                            if (!ct.IsCancellationRequested)
                            {
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    page.WidthPoints = w;
                                    page.HeightPoints = h;
                                    page.RotationAngle = rot;
                                    page.ExtractedText = txt;
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

    [RelayCommand]
    public void SelectPage(PdfViewerPageItem? page)
    {
        if (page == null) return;
        SelectedPage = page;
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
    }

    [RelayCommand]
    public void JumpToBookmark(PdfViewerBookmarkItem? bookmark)
    {
        if (bookmark == null) return;
        int pageIndex = bookmark.PageNumber - 1;
        if (pageIndex >= 0 && pageIndex < Pages.Count)
        {
            SelectedPage = Pages[pageIndex];
        }
    }

    [RelayCommand]
    public void CommitJumpPage()
    {
        if (int.TryParse(JumpPageText, out int target) && target >= 1 && target <= TotalPagesCount)
        {
            CurrentPageNumber = target;
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
    }

    [RelayCommand]
    public void FirstPage()
    {
        if (TotalPagesCount > 0)
        {
            CurrentPageNumber = 1;
        }
    }

    [RelayCommand]
    public void LastPage()
    {
        if (TotalPagesCount > 0)
        {
            CurrentPageNumber = TotalPagesCount;
        }
    }

    // --- Layout & View Modes ---

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
        ZoomLevel = Math.Min(5.0, Math.Round(ZoomLevel + 0.25, 2));
    }

    [RelayCommand]
    public void ZoomOut()
    {
        ZoomLevel = Math.Max(0.25, Math.Round(ZoomLevel - 0.25, 2));
    }

    [RelayCommand]
    public void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    public void FitToWidth()
    {
        ZoomLevel = 1.35;
    }

    [RelayCommand]
    public void FitToPage()
    {
        ZoomLevel = 0.95;
    }

    [RelayCommand]
    public void SetZoomPreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset)) return;
        string clean = preset.Replace("%", "").Trim();
        if (double.TryParse(clean, out double val))
        {
            ZoomLevel = Math.Clamp(val / 100.0, 0.25, 5.0);
        }
    }

    // --- Page Operations & Visual Rotation ---

    [RelayCommand]
    public void RotateClockwise()
    {
        if (SelectedPage != null)
        {
            SelectedPage.RotationAngle = (SelectedPage.RotationAngle + 90) % 360;
            StatusMessage = $"Page {SelectedPage.PageNumber} rotated clockwise.";
        }
    }

    [RelayCommand]
    public void RotateCounterClockwise()
    {
        if (SelectedPage != null)
        {
            SelectedPage.RotationAngle = (SelectedPage.RotationAngle + 270) % 360;
            StatusMessage = $"Page {SelectedPage.PageNumber} rotated counter-clockwise.";
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
        if (SelectedPage == null) return;
        string color = string.IsNullOrWhiteSpace(customColorHex) ? SelectedHighlightColorHex : customColorHex;
        var ann = new PdfViewerAnnotationItem
        {
            Type = "Highlight",
            PageNumber = SelectedPage.PageNumber,
            Author = "Reader Reviewer",
            Content = $"Highlighted text passage on Page {SelectedPage.PageNumber}",
            ColorHex = color,
            IconKind = "FormatColorHighlight"
        };
        Annotations.Add(ann);
        SelectedPage.PageAnnotations.Add(ann);
        OnPropertyChanged(nameof(HasAnnotations));
        SelectedSidebarTab = PdfViewerSidebarTab.Annotations;
        ShowToastRequested?.Invoke($"Added Highlight on Page {SelectedPage.PageNumber}");
    }

    [RelayCommand]
    public void OpenAddNoteDialog()
    {
        NewNoteText = string.Empty;
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
        }
        OnPropertyChanged(nameof(HasAnnotations));
    }

    // --- Clipboard & Text Copy ---

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
