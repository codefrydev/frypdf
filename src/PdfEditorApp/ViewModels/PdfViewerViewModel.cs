using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools.Core;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Rendering.Skia;

namespace PdfEditorApp.ViewModels;

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
    public event Action<string>? RenameRequested;
    public event Action<string>? DeleteRequested;

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
        OnPropertyChanged(nameof(IsSelectionTextGarbled));
    }

    public bool IsSelectionTextGarbled => IsGarbledText(ActiveSelectedText);

    public static bool IsGarbledText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string t = text.Trim();
        if (t.Length < 2) return false;

        string[] words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return false;

        int strangeWords = 0;
        foreach (var w in words)
        {
            // Standalone or embedded unmapped/corrupted symbols
            if (w.Any(c => c == '?' || c == '*' || c == '^' || c == '~' || c == '\uFFFD' || c == '|' || c == '`' || c == '§'))
            {
                strangeWords++;
            }
            else if (w.Length >= 4)
            {
                int upperTransitions = 0;
                for (int i = 1; i < w.Length; i++)
                {
                    if (char.IsLower(w[i - 1]) && char.IsUpper(w[i])) upperTransitions++;
                }
                if (upperTransitions >= 2) strangeWords++;
            }
            else if (w.Length == 1 && !char.IsLetterOrDigit(w[0]))
            {
                strangeWords++;
            }
        }

        if (strangeWords >= 2 || (double)strangeWords / words.Length >= 0.25)
        {
            return true;
        }

        int unmappedChars = t.Count(c => c == '?' || c == '*' || c == '^' || c == '~' || c == '\uFFFD' || c == '|');
        return unmappedChars > 0 && (double)unmappedChars / t.Length >= 0.10;
    }

    // Selection Mode (Text vs Area/Marquee)
    [ObservableProperty]
    private PdfViewerSelectionMode _selectionMode = PdfViewerSelectionMode.Text;

    public bool IsTextSelectionMode => SelectionMode == PdfViewerSelectionMode.Text;
    public bool IsAreaSelectionMode => SelectionMode == PdfViewerSelectionMode.Area;

    partial void OnSelectionModeChanged(PdfViewerSelectionMode value)
    {
        OnPropertyChanged(nameof(IsTextSelectionMode));
        OnPropertyChanged(nameof(IsAreaSelectionMode));
    }

    [ObservableProperty]
    private Rect? _lastSelectedAreaRect;

    // Scanned Document Detection & OCR Banner State
    [ObservableProperty]
    private bool _isScannedDocument = false;

    [ObservableProperty]
    private bool _isCurrentPageScanned = false;

    [ObservableProperty]
    private bool _isScannedBannerDismissed = false;

    [ObservableProperty]
    private bool _isOcrRunning = false;

    private CancellationTokenSource? _ocrCts;

    [ObservableProperty]
    private double _ocrProgress = 0.0;

    [ObservableProperty]
    private string _ocrStatusText = string.Empty;

    public bool ShowScannedDocumentBanner =>
        (IsScannedDocument || IsCurrentPageScanned) &&
        !IsScannedBannerDismissed &&
        !IsOcrRunning &&
        HasDocument;

    partial void OnIsScannedDocumentChanged(bool value) => OnPropertyChanged(nameof(ShowScannedDocumentBanner));
    partial void OnIsCurrentPageScannedChanged(bool value) => OnPropertyChanged(nameof(ShowScannedDocumentBanner));
    partial void OnIsScannedBannerDismissedChanged(bool value) => OnPropertyChanged(nameof(ShowScannedDocumentBanner));
    partial void OnIsOcrRunningChanged(bool value) => OnPropertyChanged(nameof(ShowScannedDocumentBanner));
    partial void OnHasDocumentChanged(bool value) => OnPropertyChanged(nameof(ShowScannedDocumentBanner));

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
            if (value.Words.Count == 0 && !value.IsGeometryLoading)
            {
                EnsurePageGeometry(value);
            }
            IsCurrentPageScanned = (value.Words.Count == 0 && string.IsNullOrWhiteSpace(value.ExtractedText));
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

}
