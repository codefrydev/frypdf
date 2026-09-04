using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels;

public enum PdfViewerSidebarTab
{
    Thumbnails,
    Bookmarks,
    Annotations,
    Search,
    Info
}

public enum PdfViewerSelectionMode
{
    Text,
    Area
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

