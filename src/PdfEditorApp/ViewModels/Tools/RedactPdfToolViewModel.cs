using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.ViewModels.Tools;

public partial class RedactPdfToolViewModel : PdfToolViewModelBase
{
    private List<PdfViewerWordItem> _currentPageWords = new();

    [ObservableProperty]
    private string _searchPattern = "CONFIDENTIAL";

    [ObservableProperty]
    private bool _caseSensitive;

    [ObservableProperty]
    private bool _permanentScrubText = true;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _searchStatusMessage = string.Empty;

    public ObservableCollection<RedactionMarkItem> Marks { get; } = new();

    public ObservableCollection<RedactionMarkItem> CurrentPageMarks { get; } = new();

    public override bool UsesWorkspaceShell => true;

    public bool HasMarks => Marks.Count > 0;
    public bool HasSearchStatusMessage => !string.IsNullOrEmpty(SearchStatusMessage);

    /// <summary>
    /// Increments each time word geometry finishes refreshing for the current page.
    /// Word extraction runs independently of Preview's own page rendering (both react
    /// to the same file/page-change signals but complete on their own schedule), so
    /// this is the only reliable way to know marking is ready to snap to text — there's
    /// no user-visible "loading words" state to show instead.
    /// </summary>
    public int WordsRefreshedCount { get; private set; }

    partial void OnSearchStatusMessageChanged(string value) => OnPropertyChanged(nameof(HasSearchStatusMessage));

    public RedactPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
        Marks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasMarks));

        // Rendering/zoom/page-nav now lives entirely in the shared Preview (from
        // PdfToolViewModelBase), which reloads itself on SelectedFiles changes. This
        // handler only clears the redaction-specific state that Preview doesn't know
        // about.
        SelectedFiles.CollectionChanged += (_, _) =>
        {
            Marks.Clear();
            CurrentPageMarks.Clear();
            SearchStatusMessage = string.Empty;
        };

        // Marks are positioned/scaled relative to the current page and zoom, and
        // word-snap marking needs the current page's word geometry — both need to be
        // recomputed whenever Preview's document, page, or zoom changes.
        Preview.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Preview.IsLoading) && !Preview.IsLoading && Preview.HasDocument)
            {
                _ = RefreshCurrentPageWordsAsync();
            }
            else if (e.PropertyName == nameof(Preview.CurrentPageNumber))
            {
                _ = RefreshCurrentPageWordsAsync();
            }
            else if (e.PropertyName == nameof(Preview.ZoomLevel))
            {
                RecomputeCurrentPageMarks();
            }
        };
    }

    /// <summary>
    /// Re-extracts word geometry for whatever page Preview is currently showing, reusing
    /// the PDF Reader's own extraction (<see cref="PdfViewerViewModel.ExtractPageTextGeometry"/>)
    /// rather than duplicating it. Independent of Preview's own bitmap rendering — this
    /// only needs the page's text layer, not a rasterized image.
    /// </summary>
    private async Task RefreshCurrentPageWordsAsync()
    {
        string path = PrimaryInputFile;
        int pageNumber = Preview.CurrentPageNumber;

        if (string.IsNullOrEmpty(path) || !File.Exists(path) || pageNumber < 1)
        {
            _currentPageWords = new List<PdfViewerWordItem>();
            RecomputeCurrentPageMarks();
            return;
        }

        var words = await Task.Run(() =>
        {
            try
            {
                using var doc = UglyToad.PdfPig.PdfDocument.Open(path);
                if (pageNumber > doc.NumberOfPages) return new List<PdfViewerWordItem>();

                var page = doc.GetPage(pageNumber);
                var (_, extractedWords, _) = PdfViewerViewModel.ExtractPageTextGeometry(page);
                return extractedWords;
            }
            catch
            {
                return new List<PdfViewerWordItem>();
            }
        });

        if (path != PrimaryInputFile || pageNumber != Preview.CurrentPageNumber) return;

        _currentPageWords = words;
        WordsRefreshedCount++;
        RecomputeCurrentPageMarks();
    }

    private void RecomputeCurrentPageMarks()
    {
        CurrentPageMarks.Clear();

        double pageWidthPoints = Preview.SelectedPage?.WidthPoints ?? 0;
        if (pageWidthPoints <= 0) return;

        // Preview renders each page at WidthPoints * ZoomLevel pixels (same convention as
        // the PDF Reader), so ZoomLevel alone is the points-to-pixels scale factor.
        double scale = Preview.ZoomLevel;
        foreach (var mark in Marks.Where(m => m.Region.PageIndex == Preview.CurrentPageNumber - 1))
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
        double pageWidthPoints = Preview.SelectedPage?.WidthPoints ?? 0;
        if (pageWidthPoints <= 0 || displayRect.Width < 2 || displayRect.Height < 2) return;

        double scale = Preview.ZoomLevel;
        double pdfX = displayRect.X / scale;
        double pdfY = displayRect.Y / scale;
        double pdfWidth = Math.Max(1, displayRect.Width / scale);
        double pdfHeight = Math.Max(1, displayRect.Height / scale);
        int pageIndex = Preview.CurrentPageNumber - 1;

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
                PageIndex = pageIndex,
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
                PageIndex = pageIndex,
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

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, System.Threading.CancellationToken ct)
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
