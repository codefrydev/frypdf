using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using UglyToad.PdfPig.Rendering.Skia;

namespace PdfEditorApp.ViewModels.Tools;

public partial class RedactPdfToolViewModel : PdfToolViewModelBase
{
    private const double BaseDisplayWidthPx = 640.0;
    private const double MinZoom = 0.5;
    private const double MaxZoom = 3.0;
    private const double ZoomStep = 0.25;

    private List<PdfViewerWordItem> _currentPageWords = new();
    private CancellationTokenSource? _zoomRenderCts;

    [ObservableProperty]
    private string _searchPattern = "CONFIDENTIAL";

    [ObservableProperty]
    private bool _caseSensitive;

    [ObservableProperty]
    private bool _permanentScrubText = true;

    [ObservableProperty]
    private int _currentPageNumber = 1;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private Bitmap? _pageBitmap;

    [ObservableProperty]
    private double _pageWidthPoints;

    [ObservableProperty]
    private double _pageHeightPoints;

    [ObservableProperty]
    private double _displayPageHeight = BaseDisplayWidthPx;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _isLoadingPreview;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _searchStatusMessage = string.Empty;

    public double DisplayPageWidth => BaseDisplayWidthPx * ZoomLevel;
    public string ZoomPercentText => $"{ZoomLevel * 100:F0}%";
    public bool CanZoomIn => ZoomLevel < MaxZoom - 0.001;
    public bool CanZoomOut => ZoomLevel > MinZoom + 0.001;

    public ObservableCollection<RedactionMarkItem> Marks { get; } = new();

    public ObservableCollection<RedactionMarkItem> CurrentPageMarks { get; } = new();

    public bool HasMarks => Marks.Count > 0;

    public string PageIndicatorText => TotalPages > 0 ? $"Page {CurrentPageNumber} of {TotalPages}" : "";
    public bool CanGoToPreviousPage => CurrentPageNumber > 1;
    public bool CanGoToNextPage => CurrentPageNumber < TotalPages;
    public bool HasSearchStatusMessage => !string.IsNullOrEmpty(SearchStatusMessage);

    partial void OnSearchStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasSearchStatusMessage));

    public RedactPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
        Marks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasMarks));
        SelectedFiles.CollectionChanged += (_, _) => { _ = LoadDocumentAsync(); };
    }

    partial void OnCurrentPageNumberChanged(int value)
    {
        OnPropertyChanged(nameof(PageIndicatorText));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(PageIndicatorText));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }

    partial void OnZoomLevelChanged(double value)
    {
        OnPropertyChanged(nameof(DisplayPageWidth));
        OnPropertyChanged(nameof(ZoomPercentText));
        OnPropertyChanged(nameof(CanZoomIn));
        OnPropertyChanged(nameof(CanZoomOut));

        RecomputeDisplayHeight();
        RecomputeCurrentPageMarks();

        // Re-render at a zoom-appropriate resolution so zoomed-in text stays sharp
        // (matching the PDF Reader's own zoom behavior), debounced so rapid zooming
        // doesn't trigger a render per tick.
        _zoomRenderCts?.Cancel();
        var cts = new CancellationTokenSource();
        _zoomRenderCts = cts;
        _ = RerenderAfterZoomDebounceAsync(cts.Token);
    }

    private async Task RerenderAfterZoomDebounceAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(150, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (ct.IsCancellationRequested) return;
        await RenderCurrentPageAsync();
    }

    private void RecomputeDisplayHeight()
    {
        DisplayPageHeight = PageWidthPoints > 0
            ? DisplayPageWidth * (PageHeightPoints / PageWidthPoints)
            : DisplayPageWidth;
    }

    [RelayCommand]
    private void ZoomIn() => ZoomLevel = Math.Min(MaxZoom, Math.Round(ZoomLevel + ZoomStep, 2));

    [RelayCommand]
    private void ZoomOut() => ZoomLevel = Math.Max(MinZoom, Math.Round(ZoomLevel - ZoomStep, 2));

    private async Task LoadDocumentAsync()
    {
        Marks.Clear();
        CurrentPageMarks.Clear();
        PageBitmap = null;
        CurrentPageNumber = 1;
        TotalPages = 0;
        ZoomLevel = 1.0;
        SearchStatusMessage = string.Empty;

        string path = PrimaryInputFile;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        IsLoadingPreview = true;
        try
        {
            TotalPages = await Task.Run(() =>
            {
                try
                {
                    using var doc = UglyToad.PdfPig.PdfDocument.Open(path);
                    return doc.NumberOfPages;
                }
                catch
                {
                    return 0;
                }
            });

            if (TotalPages > 0)
            {
                await RenderCurrentPageAsync();
            }
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }

    private async Task RenderCurrentPageAsync()
    {
        string path = PrimaryInputFile;
        if (string.IsNullOrEmpty(path) || !File.Exists(path) || CurrentPageNumber < 1) return;

        IsLoadingPreview = true;
        try
        {
            int pageNumber = CurrentPageNumber;
            // Render resolution scales with zoom (like the PDF Reader) so zoomed-in text
            // stays sharp instead of just stretching a fixed-resolution bitmap.
            float renderScale = (float)Math.Clamp(2.0 * ZoomLevel, 1.5, 6.0);

            var (bitmap, widthPoints, heightPoints, words) = await Task.Run(() =>
            {
                double w = 0, h = 0;
                Bitmap? bmp = null;
                var wordList = new List<PdfViewerWordItem>();
                try
                {
                    using var doc = UglyToad.PdfPig.PdfDocument.Open(path);
                    if (pageNumber > doc.NumberOfPages) return (bmp, w, h, wordList);

                    // Dimensions come from the page itself, independent of whether the
                    // bitmap can actually be decoded below — keeps highlight-position math
                    // correct even if rendering fails for some reason.
                    var page = doc.GetPage(pageNumber);
                    w = page.Width;
                    h = page.Height;

                    // Word geometry (already in top-down, page-point space, same as
                    // RedactionRegion) powers text-selection drag-to-mark below — reuses
                    // the PDF Reader's own extraction rather than duplicating it.
                    try
                    {
                        var (_, extractedWords, _) = PdfViewerViewModel.ExtractPageTextGeometry(page);
                        wordList = extractedWords;
                    }
                    catch { }

                    try { PdfPigExtensions.AddSkiaPageFactory(doc); } catch { }

                    using var stream = PdfPigExtensions.GetPageAsPng(doc, pageNumber, renderScale, 100);
                    if (stream != null && stream.Length > 0)
                    {
                        stream.Position = 0;
                        try { bmp = new Bitmap(stream); } catch { bmp = null; }
                    }
                }
                catch { }
                return (bmp, w, h, wordList);
            });

            PageBitmap = bitmap;
            PageWidthPoints = widthPoints;
            PageHeightPoints = heightPoints;
            RecomputeDisplayHeight();
            _currentPageWords = words;

            RecomputeCurrentPageMarks();
        }
        finally
        {
            IsLoadingPreview = false;
        }
    }

    private void RecomputeCurrentPageMarks()
    {
        CurrentPageMarks.Clear();
        if (PageWidthPoints <= 0) return;

        double scale = DisplayPageWidth / PageWidthPoints;
        foreach (var mark in Marks.Where(m => m.Region.PageIndex == CurrentPageNumber - 1))
        {
            mark.DisplayX = mark.Region.X * scale;
            mark.DisplayY = mark.Region.Y * scale;
            mark.DisplayWidth = Math.Max(2, mark.Region.Width * scale);
            mark.DisplayHeight = Math.Max(2, mark.Region.Height * scale);
            CurrentPageMarks.Add(mark);
        }
    }

    [RelayCommand]
    private async Task FindAndMarkAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchPattern) || string.IsNullOrEmpty(PrimaryInputFile)) return;

        IsSearching = true;
        try
        {
            var matches = await OperationsService.SecurityService.FindRedactionMatchesAsync(PrimaryInputFile, SearchPattern, CaseSensitive);

            if (matches.Count == 0)
            {
                SearchStatusMessage = $"No matches found for \"{SearchPattern}\".";
                return;
            }

            int added = 0;
            foreach (var region in matches)
            {
                bool alreadyMarked = Marks.Any(m =>
                    m.Region.PageIndex == region.PageIndex &&
                    Math.Abs(m.Region.X - region.X) < 0.5 &&
                    Math.Abs(m.Region.Y - region.Y) < 0.5);
                if (alreadyMarked) continue;

                Marks.Add(new RedactionMarkItem { Region = region, Label = SearchPattern });
                added++;
            }

            SearchStatusMessage = added > 0
                ? $"Marked {added} new match(es) for \"{SearchPattern}\" ({matches.Count} found total)."
                : $"All {matches.Count} match(es) for \"{SearchPattern}\" were already marked.";

            RecomputeCurrentPageMarks();
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void RemoveMark(RedactionMarkItem? item)
    {
        if (item == null) return;
        Marks.Remove(item);
        CurrentPageMarks.Remove(item);
    }

    [RelayCommand]
    private void ClearMarks()
    {
        Marks.Clear();
        CurrentPageMarks.Clear();
    }

    /// <summary>
    /// Converts a manual drag rectangle (in on-screen display-pixel space, from the
    /// interactive preview's code-behind pointer handling) into a mark. By default snaps
    /// to whichever words the drag rectangle touches, matching the PDF Reader's text
    /// selection; pass <paramref name="forceDrawBox"/> true (held Alt while dragging) to
    /// use the raw rectangle instead, for images, signatures, or anything text search and
    /// selection can't target.
    /// </summary>
    public void AddManualMark(Rect displayRect, bool forceDrawBox = false)
    {
        if (PageWidthPoints <= 0 || displayRect.Width < 2 || displayRect.Height < 2) return;

        double scale = DisplayPageWidth / PageWidthPoints;
        double pdfX = displayRect.X / scale;
        double pdfY = displayRect.Y / scale;
        double pdfWidth = Math.Max(1, displayRect.Width / scale);
        double pdfHeight = Math.Max(1, displayRect.Height / scale);

        RedactionRegion region;
        string label;

        if (!forceDrawBox)
        {
            var dragRect = new Rect(pdfX, pdfY, pdfWidth, pdfHeight);
            var touched = _currentPageWords.Where(w => w.Bounds.Intersects(dragRect)).ToList();
            if (touched.Count == 0) return;

            double left = touched.Min(w => w.Bounds.Left);
            double top = touched.Min(w => w.Bounds.Top);
            double right = touched.Max(w => w.Bounds.Right);
            double bottom = touched.Max(w => w.Bounds.Bottom);

            region = new RedactionRegion
            {
                PageIndex = CurrentPageNumber - 1,
                X = left,
                Y = top,
                Width = right - left,
                Height = bottom - top,
                Reason = "Manual selection"
            };

            label = string.Join(" ", touched.OrderBy(w => w.Bounds.Left).Select(w => w.Text));
            if (label.Length > 60) label = label.Substring(0, 60) + "…";
        }
        else
        {
            region = new RedactionRegion
            {
                PageIndex = CurrentPageNumber - 1,
                X = pdfX,
                Y = pdfY,
                Width = pdfWidth,
                Height = pdfHeight,
                Reason = "Manual selection"
            };
            label = "Manual selection";
        }

        Marks.Add(new RedactionMarkItem { Region = region, Label = label });
        RecomputeCurrentPageMarks();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPageNumber >= TotalPages) return;
        CurrentPageNumber++;
        await RenderCurrentPageAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPageNumber <= 1) return;
        CurrentPageNumber--;
        await RenderCurrentPageAsync();
    }

    protected override bool ValidateInputs(out string errorMessage)
    {
        if (!base.ValidateInputs(out errorMessage)) return false;

        if (Marks.Count == 0)
        {
            errorMessage = "Search for text to redact and review the matches below before running Redact.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new RedactionToolOptions
        {
            InputFilePath = PrimaryInputFile,
            Regions = Marks.Select(m => m.Region).ToList(),
            PermanentScrubText = PermanentScrubText
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.RedactPdf, options, progress, ct);
    }
}
