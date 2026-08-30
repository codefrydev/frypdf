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
    Info,
    Search
}

public class PdfViewerPageItem : ObservableObject
{
    private bool _isSelected;
    private int _rotationAngle;
    private Bitmap? _bitmap;

    public int PageNumber { get; set; }
    public double WidthPoints { get; set; }
    public double HeightPoints { get; set; }
    public string DimensionsText => $"{Math.Round(WidthPoints):F0} × {Math.Round(HeightPoints):F0} pt";
    public string PageLabel => $"Page {PageNumber}";
    public string PageSummary { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;

    public Bitmap? Bitmap
    {
        get => _bitmap;
        set => SetProperty(ref _bitmap, value);
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
    public string Author { get; set; } = "User";
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
/// Modern, high-fidelity PDF Viewer ViewModel.
/// Provides continuous and single-page viewing, sharp rendering via PdfPig Skia,
/// real page thumbnails, table of contents/bookmarks, live text search, annotations, and 1-click studio bridge.
/// </summary>
public partial class PdfViewerViewModel : ViewModelBase
{
    private CancellationTokenSource? _renderCts;
    private byte[]? _currentPdfBytes;

    public IStorageProvider? StorageProvider { get; set; }

    // --- Events ---
    public event Action<string>? EditInStudioRequested;
    public event Action? BackToHomeRequested;
    public event Action<PdfToolId, string>? RunToolRequested;
    public event Action<string>? ShowToastRequested;

    // --- Observable Properties ---

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string _documentTitle = "Document.pdf";

    [ObservableProperty]
    private int _currentPageNumber = 1;

    [ObservableProperty]
    private int _totalPagesCount = 0;

    [ObservableProperty]
    private double _zoomLevel = 1.0; // 100%

    [ObservableProperty]
    private bool _isContinuousScroll = true;

    [ObservableProperty]
    private bool _isSinglePageMode = false;

    [ObservableProperty]
    private PdfViewerSidebarTab _selectedSidebarTab = PdfViewerSidebarTab.Thumbnails;

    [ObservableProperty]
    private bool _isSidebarOpen = true;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _hasDocument = false;

    [ObservableProperty]
    private PdfViewerPageItem? _selectedPage;

    // Search in PDF
    [ObservableProperty]
    private bool _isSearchBarVisible = false;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _searchMatchCase = false;

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

    // Presentation / Fullscreen
    [ObservableProperty]
    private bool _isFullscreen = false;

    // Collections
    public ObservableCollection<PdfViewerPageItem> Pages { get; } = new();
    public ObservableCollection<PdfViewerBookmarkItem> Bookmarks { get; } = new();
    public ObservableCollection<PdfViewerAnnotationItem> Annotations { get; } = new();
    public ObservableCollection<PdfViewerMetadataItem> MetadataItems { get; } = new();
    public ObservableCollection<PdfViewerSearchMatch> SearchResults { get; } = new();

    public bool HasBookmarks => Bookmarks.Count > 0;
    public bool HasAnnotations => Annotations.Count > 0;
    public bool HasSearchResults => SearchResults.Count > 0;
    public int MatchCount => SearchResults.Count;

    public string ZoomPercentageText => $"{(int)(ZoomLevel * 100)}%";
    public string PageNavigationDisplay => TotalPagesCount > 0 ? $"{CurrentPageNumber} / {TotalPagesCount}" : "0 / 0";

    partial void OnZoomLevelChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomPercentageText));
    }

    partial void OnCurrentPageNumberChanged(int value)
    {
        OnPropertyChanged(nameof(PageNavigationDisplay));
        if (value >= 1 && value <= Pages.Count)
        {
            SelectedPage = Pages[value - 1];
        }
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

    partial void OnIsContinuousScrollChanged(bool value)
    {
        IsSinglePageMode = !value;
    }

    partial void OnIsSinglePageModeChanged(bool value)
    {
        IsContinuousScroll = !value;
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

        IsLoading = true;
        StatusMessage = "Loading PDF document...";
        _currentPdfBytes = pdfBytes;
        CurrentFilePath = sourceFilePath;
        DocumentTitle = string.IsNullOrWhiteSpace(sourceFilePath) ? "Document.pdf" : Path.GetFileName(sourceFilePath);

        Pages.Clear();
        Bookmarks.Clear();
        Annotations.Clear();
        MetadataItems.Clear();
        SearchResults.Clear();

        try
        {
            byte[] sanitizedBytes = PdfFileHelper.SanitizePdfBytes(pdfBytes);

            var (metaList, bookmarksList, pagesList, total) = await Task.Run(() =>
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

                    // 1. Metadata
                    var info = doc.Information;
                    var metaList = new List<PdfViewerMetadataItem>
                    {
                        new PdfViewerMetadataItem { Label = "File Name", Value = DocumentTitle, IconKind = "FileDocumentOutline" },
                        new PdfViewerMetadataItem { Label = "Total Pages", Value = $"{total} Pages", IconKind = "BookOpenPageVariantOutline" },
                        new PdfViewerMetadataItem { Label = "File Size", Value = PdfFilePreviewItem.FormatBytes(pdfBytes.Length), IconKind = "DatabaseOutline" },
                        new PdfViewerMetadataItem { Label = "Title", Value = string.IsNullOrWhiteSpace(info.Title) ? "Untitled" : info.Title, IconKind = "FormatTitle" },
                        new PdfViewerMetadataItem { Label = "Author", Value = string.IsNullOrWhiteSpace(info.Author) ? "Unknown" : info.Author, IconKind = "AccountOutline" },
                        new PdfViewerMetadataItem { Label = "Subject", Value = string.IsNullOrWhiteSpace(info.Subject) ? "None" : info.Subject, IconKind = "Subject" },
                        new PdfViewerMetadataItem { Label = "Creator", Value = string.IsNullOrWhiteSpace(info.Creator) ? "PDF Engine" : info.Creator, IconKind = "CogOutline" },
                        new PdfViewerMetadataItem { Label = "Producer", Value = string.IsNullOrWhiteSpace(info.Producer) ? "FryPDF Studio" : info.Producer, IconKind = "ApplicationOutline" },
                        new PdfViewerMetadataItem { Label = "PDF Version", Value = doc.Version.ToString(), IconKind = "ShieldCheckOutline" }
                    };

                    // 2. Bookmarks / Outlines
                    var bookmarksList = new List<PdfViewerBookmarkItem>();
                    try
                    {
                        if (doc.TryGetBookmarks(out var bookmarks) && bookmarks != null && bookmarks.Roots != null)
                        {
                            ExtractBookmarksRecursive(bookmarks.Roots, bookmarksList);
                        }
                    }
                    catch { }

                    // 3. Render pages to Bitmaps
                    var pagesList = new List<PdfViewerPageItem>();
                    for (int i = 1; i <= total; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var page = doc.GetPage(i);
                        string pageText = page.Text ?? "";
                        string summary = "";
                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            var firstLine = pageText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                            summary = firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
                        }

                        Bitmap? bmp = null;
                        try
                        {
                            using var pngStream = PdfPigExtensions.GetPageAsPng(doc, i, 1.5f, 90);
                            if (pngStream != null && pngStream.Length > 0)
                            {
                                pngStream.Position = 0;
                                bmp = new Bitmap(pngStream);
                            }
                        }
                        catch { }

                        var pageItem = new PdfViewerPageItem
                        {
                            PageNumber = i,
                            WidthPoints = Math.Max(100, page.Width),
                            HeightPoints = Math.Max(100, page.Height),
                            RotationAngle = (int)page.Rotation.Value,
                            ExtractedText = pageText,
                            PageSummary = summary,
                            Bitmap = bmp,
                            IsSelected = (i == 1)
                        };
                        pagesList.Add(pageItem);
                    }

                    return (metaList, bookmarksList, pagesList, total);
                }
            }, ct);

            foreach (var m in metaList) MetadataItems.Add(m);
            foreach (var b in bookmarksList) Bookmarks.Add(b);
            foreach (var p in pagesList) Pages.Add(p);

            TotalPagesCount = total;
            CurrentPageNumber = 1;
            SelectedPage = Pages.FirstOrDefault();
            HasDocument = true;
            IsLoading = false;
            StatusMessage = $"Loaded {total} pages.";
            OnPropertyChanged(nameof(HasBookmarks));
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Loading cancelled.";
            IsLoading = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsLoading = false;
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
    public void NextPage()
    {
        if (CurrentPageNumber < TotalPagesCount)
        {
            CurrentPageNumber++;
        }
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (CurrentPageNumber > 1)
        {
            CurrentPageNumber--;
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

    // --- Zoom Controls ---

    [RelayCommand]
    public void ZoomIn()
    {
        ZoomLevel = Math.Min(4.0, Math.Round(ZoomLevel + 0.25, 2));
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
        if (double.TryParse(preset, out double val))
        {
            ZoomLevel = Math.Clamp(val, 0.25, 4.0);
        }
    }

    // --- Page Operations & Rotation ---

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
        var comp = SearchMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        int matchIdx = 1;
        foreach (var page in Pages)
        {
            if (string.IsNullOrEmpty(page.ExtractedText)) continue;

            int startIndex = 0;
            while ((startIndex = page.ExtractedText.IndexOf(q, startIndex, comp)) != -1)
            {
                int snippetStart = Math.Max(0, startIndex - 20);
                int snippetLen = Math.Min(page.ExtractedText.Length - snippetStart, q.Length + 40);
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

    // --- Interactive Annotations & Markups ---

    [RelayCommand]
    public void AddHighlightAnnotation()
    {
        if (SelectedPage == null) return;
        var ann = new PdfViewerAnnotationItem
        {
            Type = "Highlight",
            PageNumber = SelectedPage.PageNumber,
            Author = "Viewer User",
            Content = $"Highlighted text region on Page {SelectedPage.PageNumber}",
            ColorHex = "#FEF08A",
            IconKind = "FormatColorHighlight"
        };
        Annotations.Add(ann);
        SelectedPage.PageAnnotations.Add(ann);
        OnPropertyChanged(nameof(HasAnnotations));
        SelectedSidebarTab = PdfViewerSidebarTab.Annotations;
        ShowToastRequested?.Invoke($"Added Highlight annotation on Page {SelectedPage.PageNumber}");
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
            Author = "Reviewer",
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
        var ann = new PdfViewerAnnotationItem
        {
            Type = "Stamp",
            PageNumber = SelectedPage.PageNumber,
            Author = "Auditor",
            Content = $"Stamp: {text}",
            ColorHex = text.Contains("REJECT") ? "#EF4444" : "#10B981",
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

    // --- Save As / Print / Export ---

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
            ShowToastRequested?.Invoke($"Saved PDF copy to: {Path.GetFileName(file.Path.LocalPath)}");
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
            // Save to temp file and open in studio
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
