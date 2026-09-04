using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material.Icons;
using Material.Icons.Avalonia;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views.Controls;

/// <summary>
/// High-performance, pixel-perfect text selection and highlight markup overlay for PDF Viewer pages.
/// Provides continuous mouse drag selection, multi-click word/line selection, I-beam hover cursor,
/// Acrobat-style vibrant visual selection highlights, permanent vector highlight annotations,
/// and rich right-click context menu actions (Copy, Citation, Highlight, Note, Search).
/// </summary>
public class PdfTextOverlayControl : Control
{
    public static readonly StyledProperty<PdfViewerPageItem?> PageProperty =
        AvaloniaProperty.Register<PdfTextOverlayControl, PdfViewerPageItem?>(nameof(Page));

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<PdfTextOverlayControl, double>(nameof(ZoomLevel), defaultValue: 1.0);

    public static readonly StyledProperty<PdfViewerViewModel?> ViewerViewModelProperty =
        AvaloniaProperty.Register<PdfTextOverlayControl, PdfViewerViewModel?>(nameof(ViewerViewModel));

    public PdfViewerPageItem? Page
    {
        get => GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public PdfViewerViewModel? ViewerViewModel
    {
        get => GetValue(ViewerViewModelProperty);
        set => SetValue(ViewerViewModelProperty, value);
    }

    // Interaction state
    private bool _isPointerDown;
    private Point _dragStartPoint;
    private bool _hasMovedSinceDown;
    private PdfViewerPageItem? _subscribedPage;
    private bool _isAreaSelecting;
    private Rect _currentMarqueeRect;
    private Point? _lastAnchorPoint;
    private bool _isTargetedOcrRunning;

    // Visual brushes (cached for maximum performance)
    private static readonly IBrush SelectionFillBrush = new SolidColorBrush(Color.FromArgb(95, 59, 130, 246));
    private static readonly IPen SelectionBorderPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 37, 99, 235)), 1.0);
    private static readonly IBrush MarqueeFillBrush = new SolidColorBrush(Color.FromArgb(45, 15, 108, 189));
    private static readonly IPen MarqueeBorderPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 15, 108, 189)), 1.5, DashStyle.Dash);
    private static readonly IBrush HandleFillBrush = new SolidColorBrush(Colors.White);
    private static readonly IPen HandleBorderPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 15, 108, 189)), 1.0);

    static PdfTextOverlayControl()
    {
        AffectsRender<PdfTextOverlayControl>(PageProperty, ZoomLevelProperty);
    }

    public PdfTextOverlayControl()
    {
        ClipToBounds = true;
        IsHitTestVisible = true;
        ContextMenu = CreateSelectionContextMenu();
    }

    private PdfViewerViewModel? ResolveViewModel()
    {
        if (ViewerViewModel != null) return ViewerViewModel;
        if (DataContext is PdfViewerViewModel vm) return vm;
        var parent = this.FindAncestorOfType<PdfViewerView>();
        return parent?.DataContext as PdfViewerViewModel;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PageProperty)
        {
            if (_subscribedPage != null)
            {
                _subscribedPage.SelectionChanged -= OnPageSelectionChanged;
            }

            _subscribedPage = change.GetNewValue<PdfViewerPageItem?>();

            if (_subscribedPage != null)
            {
                _subscribedPage.SelectionChanged += OnPageSelectionChanged;
            }

            InvalidateVisual();
        }
        else if (change.Property == ZoomLevelProperty)
        {
            InvalidateVisual();
        }
    }

    private void OnPageSelectionChanged()
    {
        InvalidateVisual();
    }

    private (double scaleX, double scaleY) GetEffectiveScales(PdfViewerPageItem page)
    {
        double fallbackZoom = Math.Max(0.1, ZoomLevel > 0 ? ZoomLevel : 1.0);
        double sx = (page.WidthPoints > 0 && Bounds.Width > 0) ? Bounds.Width / page.WidthPoints : fallbackZoom;
        double sy = (page.HeightPoints > 0 && Bounds.Height > 0) ? Bounds.Height / page.HeightPoints : fallbackZoom;
        return (sx, sy);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var page = Page;
        if (page == null) return;

        var vm = ResolveViewModel();
        if (page.Words.Count == 0 && vm != null)
        {
            vm.EnsurePageGeometry(page);
        }

        var (scaleX, scaleY) = GetEffectiveScales(page);
        var pos = e.GetPosition(this);
        var unscaledPos = new Point(pos.X / scaleX, pos.Y / scaleY);

        if (_isPointerDown)
        {
            _hasMovedSinceDown = true;

            if (_isAreaSelecting)
            {
                double minX = Math.Min(_dragStartPoint.X, unscaledPos.X);
                double minY = Math.Min(_dragStartPoint.Y, unscaledPos.Y);
                double maxX = Math.Max(_dragStartPoint.X, unscaledPos.X);
                double maxY = Math.Max(_dragStartPoint.Y, unscaledPos.Y);
                _currentMarqueeRect = new Rect(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));

                page.SelectionRects.Clear();
                page.SelectionRects.Add(_currentMarqueeRect);

                if (vm != null)
                {
                    vm.LastSelectedAreaRect = _currentMarqueeRect;
                }

                InvalidateVisual();
                e.Handled = true;
            }
            else
            {
                page.SetSelectionRange(_dragStartPoint, unscaledPos);

                if (vm != null)
                {
                    vm.ActiveSelectedText = page.SelectedText;
                    vm.ActiveSelectedPageNumber = page.PageNumber;
                    vm.HasTextSelection = !string.IsNullOrEmpty(page.SelectedText);
                }

                InvalidateVisual();
                e.Handled = true;
            }
        }
        else
        {
            // Dynamic Cursor
            bool isAreaMode = (vm != null && vm.SelectionMode == PdfViewerSelectionMode.Area) || e.KeyModifiers.HasFlag(KeyModifiers.Alt);
            if (isAreaMode)
            {
                Cursor = new Cursor(StandardCursorType.Cross);
            }
            else
            {
                bool isOverText = page.Words != null && page.Words.Any(w => w.Bounds.Contains(unscaledPos));
                if (isOverText)
                {
                    Cursor = new Cursor(StandardCursorType.Ibeam);
                }
                else if (page.Words == null || page.Words.Count == 0)
                {
                    Cursor = new Cursor(StandardCursorType.Cross);
                }
                else
                {
                    Cursor = Cursor.Default;
                }
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var page = Page;
        if (page == null) return;

        var vm = ResolveViewModel();
        if (page.Words.Count == 0 && vm != null)
        {
            vm.EnsurePageGeometry(page);
        }

        var (scaleX, scaleY) = GetEffectiveScales(page);
        var currentPoint = e.GetCurrentPoint(this);
        var pos = e.GetPosition(this);
        var unscaledPos = new Point(pos.X / scaleX, pos.Y / scaleY);

        if (currentPoint.Properties.IsLeftButtonPressed)
        {
            int clickCount = e.ClickCount;
            bool isShift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            bool isAlt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
            bool isAreaMode = (vm != null && vm.SelectionMode == PdfViewerSelectionMode.Area) || isAlt || (page.Words.Count == 0);

            if (clickCount == 1)
            {
                // Synchronize active page and thumbnail selection on click
                if (vm != null)
                {
                    vm.SelectPage(page);
                    if (vm.ActiveSelectedPageNumber != page.PageNumber)
                    {
                        vm.ClearSelection();
                    }
                }

                if (isShift && _lastAnchorPoint.HasValue && !isAreaMode && page.Words.Count > 0)
                {
                    // Shift + Click: Extend selection from previous anchor to clicked point
                    page.SetSelectionRange(_lastAnchorPoint.Value, unscaledPos);
                    SyncSelectionToViewModel(page, vm);
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }

                _isPointerDown = true;
                _dragStartPoint = unscaledPos;
                _lastAnchorPoint = unscaledPos;
                _hasMovedSinceDown = false;
                _isAreaSelecting = isAreaMode;
                _currentMarqueeRect = default;

                e.Pointer.Capture(this);
                e.Handled = true;
            }
            else if (clickCount == 2 && !isAreaMode)
            {
                // Double-click: Select word under cursor
                var word = page.Words?.FirstOrDefault(w => w.Bounds.Contains(unscaledPos))
                           ?? page.Words?.OrderBy(w => Math.Abs(w.Bounds.Center.X - unscaledPos.X) + Math.Abs(w.Bounds.Center.Y - unscaledPos.Y) * 2).FirstOrDefault();
                if (word != null)
                {
                    page.SelectWord(word);
                    _lastAnchorPoint = word.Bounds.TopLeft;
                    SyncSelectionToViewModel(page, vm);
                    InvalidateVisual();
                    e.Handled = true;
                }
            }
            else if (clickCount >= 3 && !isAreaMode)
            {
                // Triple-click: Select entire line under cursor
                var line = page.TextLines?.FirstOrDefault(l => l.Bounds.Contains(unscaledPos))
                           ?? page.TextLines?.OrderBy(l => Math.Abs(l.Bounds.Center.Y - unscaledPos.Y)).FirstOrDefault();
                if (line != null)
                {
                    page.SelectLine(line);
                    _lastAnchorPoint = line.Bounds.TopLeft;
                    SyncSelectionToViewModel(page, vm);
                    InvalidateVisual();
                    e.Handled = true;
                }
            }
        }
        else if (currentPoint.Properties.IsRightButtonPressed)
        {
            // Right click: If clicking inside existing selection, keep it; otherwise select word under cursor
            bool isInsideSelection = page.SelectionRects != null && page.SelectionRects.Any(r => r.Contains(unscaledPos));
            if (!isInsideSelection)
            {
                var word = page.Words?.FirstOrDefault(w => w.Bounds.Contains(unscaledPos));
                if (word != null)
                {
                    page.SelectWord(word);
                    SyncSelectionToViewModel(page, vm);
                    InvalidateVisual();
                }
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isPointerDown)
        {
            _isPointerDown = false;
            e.Pointer.Capture(null);

            var page = Page;
            var vm = ResolveViewModel();
            if (page != null)
            {
                if (!_hasMovedSinceDown && e.InitialPressMouseButton == MouseButton.Left)
                {
                    // Single click with no drag on blank space: clear selection
                    var (scaleX, scaleY) = GetEffectiveScales(page);
                    var pos = e.GetPosition(this);
                    var unscaledPos = new Point(pos.X / scaleX, pos.Y / scaleY);
                    bool hitWord = page.Words != null && page.Words.Any(w => w.Bounds.Contains(unscaledPos));

                    if (!hitWord)
                    {
                        page.ClearSelection();
                        vm?.ClearSelection();
                        _currentMarqueeRect = default;
                    }
                    else
                    {
                        var word = page.Words!.First(w => w.Bounds.Contains(unscaledPos));
                        page.SelectWord(word);
                        _lastAnchorPoint = word.Bounds.TopLeft;
                        SyncSelectionToViewModel(page, vm);
                    }
                }
                else if (_isAreaSelecting && _hasMovedSinceDown)
                {
                    if (vm != null)
                    {
                        vm.LastSelectedAreaRect = _currentMarqueeRect;
                    }

                    List<PdfViewerWordItem> selectedWords = new();
                    if (page.Words != null && page.Words.Count > 0)
                    {
                        selectedWords = page.Words
                            .Where(w => w.Bounds.Intersects(_currentMarqueeRect))
                            .OrderBy(w => w.Bounds.Top)
                            .ThenBy(w => w.Bounds.Left)
                            .ToList();
                    }

                    bool wordsAreGarbled = selectedWords.Count > 0 && selectedWords.All(w => PdfViewerViewModel.IsGarbledText(w.Text));

                    if (selectedWords.Count > 0 && !wordsAreGarbled)
                    {
                        page.SelectedText = string.Join(" ", selectedWords.Select(w => w.Text));
                        SyncSelectionToViewModel(page, vm);
                    }
                    else if (vm != null && _currentMarqueeRect.Width > 5 && _currentMarqueeRect.Height > 5)
                    {
                        // Scanned page (zero words) or garbled words in area: perform targeted OCR on cropped rect
                        _isTargetedOcrRunning = true;
                        InvalidateVisual();
                        var rectToOcr = _currentMarqueeRect;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                string text = await vm.ExtractOcrTextFromRegionAsync(page, rectToOcr, forceOcr: true);
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    _isTargetedOcrRunning = false;
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        page.SelectedText = text;
                                        SyncSelectionToViewModel(page, vm);
                                    }
                                    InvalidateVisual();
                                });
                            }
                            catch
                            {
                                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                {
                                    _isTargetedOcrRunning = false;
                                    InvalidateVisual();
                                });
                            }
                        });
                    }
                }
                else
                {
                    SyncSelectionToViewModel(page, vm);
                }
            }

            InvalidateVisual();
            e.Handled = true;
        }
    }

    private void SyncSelectionToViewModel(PdfViewerPageItem page, PdfViewerViewModel? vm)
    {
        if (vm != null)
        {
            vm.ActiveSelectedText = page.SelectedText;
            vm.ActiveSelectedPageNumber = page.PageNumber;
            vm.HasTextSelection = !string.IsNullOrEmpty(page.SelectedText);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // 1. Fill transparent bounds to guarantee 100% reliable mouse hit-testing across entire page card
        context.FillRectangle(Brushes.Transparent, new Rect(0, 0, Math.Max(1, Bounds.Width), Math.Max(1, Bounds.Height)));

        var page = Page;
        if (page == null) return;

        var (scaleX, scaleY) = GetEffectiveScales(page);

        // 2. Draw Permanent Highlight Annotations on this page
        if (page.PageAnnotations != null && page.PageAnnotations.Count > 0)
        {
            foreach (var ann in page.PageAnnotations)
            {
                if (ann.Type == "Highlight" && ann.HighlightRects != null && ann.HighlightRects.Count > 0)
                {
                    IBrush highlightBrush = CreateHighlightBrush(ann.ColorHex);
                    foreach (var rect in ann.HighlightRects)
                    {
                        var scaledRect = new Rect(
                            rect.X * scaleX,
                            rect.Y * scaleY,
                            Math.Max(2, rect.Width * scaleX),
                            Math.Max(2, rect.Height * scaleY));

                        context.FillRectangle(highlightBrush, scaledRect, 2.0f);
                    }
                }
            }
        }

        // 3. Draw Active User Text Selection
        if (page.SelectionRects != null && page.SelectionRects.Count > 0)
        {
            var vm = ResolveViewModel();
            bool isMarquee = _isAreaSelecting || (vm != null && vm.SelectionMode == PdfViewerSelectionMode.Area) || (page.Words.Count == 0);

            foreach (var rect in page.SelectionRects)
            {
                var scaledRect = new Rect(
                    rect.X * scaleX,
                    rect.Y * scaleY,
                    Math.Max(2, rect.Width * scaleX),
                    Math.Max(2, rect.Height * scaleY));

                if (isMarquee)
                {
                    // Draw Area / Marquee Box
                    context.FillRectangle(MarqueeFillBrush, scaledRect, 2.0f);
                    context.DrawRectangle(MarqueeBorderPen, scaledRect, 2.0f);

                    // Draw 4 corner handles
                    const double handleSize = 6.0;
                    const double halfHandle = handleSize / 2.0;
                    var corners = new[]
                    {
                        new Point(scaledRect.Left, scaledRect.Top),
                        new Point(scaledRect.Right, scaledRect.Top),
                        new Point(scaledRect.Left, scaledRect.Bottom),
                        new Point(scaledRect.Right, scaledRect.Bottom)
                    };

                    foreach (var corner in corners)
                    {
                        var handleRect = new Rect(corner.X - halfHandle, corner.Y - halfHandle, handleSize, handleSize);
                        context.FillRectangle(HandleFillBrush, handleRect);
                        context.DrawRectangle(HandleBorderPen, handleRect);
                    }

                    if (_isTargetedOcrRunning)
                    {
                        // Draw "⚡ OCR..." badge
                        var badgeRect = new Rect(scaledRect.Left + 4, Math.Max(0, scaledRect.Top - 22), 65, 18);
                        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 15, 108, 189)), badgeRect, 4.0f);
                        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
                        var formattedText = new FormattedText("⚡ OCR...", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10.5, Brushes.White);
                        context.DrawText(formattedText, new Point(badgeRect.Left + 6, badgeRect.Top + 2));
                    }
                }
                else
                {
                    context.FillRectangle(SelectionFillBrush, scaledRect, 2.0f);
                    context.DrawRectangle(SelectionBorderPen, scaledRect, 2.0f);
                }
            }
        }
    }

    private static IBrush CreateHighlightBrush(string colorHex)
    {
        try
        {
            if (Color.TryParse(colorHex, out var parsed))
            {
                return new SolidColorBrush(Color.FromArgb(98, parsed.R, parsed.G, parsed.B));
            }
        }
        catch { }

        return new SolidColorBrush(Color.FromArgb(98, 254, 240, 138)); // Default yellow
    }

    private ContextMenu CreateSelectionContextMenu()
    {
        var menu = new ContextMenu();

        // 1. Copy
        var copyItem = new MenuItem
        {
            Header = "Copy Text",
            InputGesture = new KeyGesture(Key.C, KeyModifiers.Control | KeyModifiers.Meta)
        };
        copyItem.Icon = new MaterialIcon { Kind = MaterialIconKind.ContentCopy, Width = 15, Height = 15 };
        copyItem.Click += (s, e) => ResolveViewModel()?.CopySelectedTextCommand.Execute(null);
        menu.Items.Add(copyItem);

        // 2. Copy with Citation
        var citationItem = new MenuItem
        {
            Header = "Copy with Page Reference",
            InputGesture = new KeyGesture(Key.C, KeyModifiers.Control | KeyModifiers.Shift)
        };
        citationItem.Icon = new MaterialIcon { Kind = MaterialIconKind.FormatQuoteClose, Width = 15, Height = 15 };
        citationItem.Click += (s, e) => ResolveViewModel()?.CopySelectedCitationCommand.Execute(null);
        menu.Items.Add(citationItem);

        // 3. Copy Area Snapshot Image
        var copySnapshotItem = new MenuItem
        {
            Header = "Copy Area Snapshot",
            InputGesture = new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift)
        };
        copySnapshotItem.Icon = new MaterialIcon { Kind = MaterialIconKind.CameraOutline, Width = 15, Height = 15, Foreground = new SolidColorBrush(Color.Parse("#0284C7")) };
        copySnapshotItem.Click += (s, e) => ResolveViewModel()?.CopySelectedAreaImageCommand.Execute(null);
        menu.Items.Add(copySnapshotItem);

        // 4. Recognize Selection with OCR
        var ocrSelectionItem = new MenuItem
        {
            Header = "Recognize Selection with OCR",
            InputGesture = new KeyGesture(Key.R, KeyModifiers.Control | KeyModifiers.Shift)
        };
        ocrSelectionItem.Icon = new MaterialIcon { Kind = MaterialIconKind.ScanHelper, Width = 15, Height = 15, Foreground = new SolidColorBrush(Color.Parse("#0F6CBD")) };
        ocrSelectionItem.Click += (s, e) => ResolveViewModel()?.RecognizeSelectedTextOcrCommand.Execute(null);
        menu.Items.Add(ocrSelectionItem);

        // 5. Recognize Entire Page (OCR)
        var ocrItem = new MenuItem
        {
            Header = "Recognize Page (OCR)"
        };
        ocrItem.Icon = new MaterialIcon { Kind = MaterialIconKind.TextRecognition, Width = 15, Height = 15, Foreground = new SolidColorBrush(Color.Parse("#0F6CBD")) };
        ocrItem.Click += (s, e) => ResolveViewModel()?.RecognizeActivePageOcrCommand.Execute(null);
        menu.Items.Add(ocrItem);

        menu.Items.Add(new Separator());

        // 3. Highlight submenu
        var highlightMenu = new MenuItem
        {
            Header = "Highlight Selection"
        };
        highlightMenu.Icon = new MaterialIcon { Kind = MaterialIconKind.FormatColorHighlight, Width = 15, Height = 15, Foreground = new SolidColorBrush(Color.Parse("#CA8A04")) };

        var yellowHl = new MenuItem { Header = "Yellow Highlight" };
        yellowHl.Icon = new Border { Width = 12, Height = 12, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.Parse("#FEF08A")), BorderBrush = new SolidColorBrush(Color.Parse("#CA8A04")), BorderThickness = new Thickness(1) };
        yellowHl.Click += (s, e) => ResolveViewModel()?.HighlightSelectedTextCommand.Execute("#FEF08A");
        highlightMenu.Items.Add(yellowHl);

        var greenHl = new MenuItem { Header = "Green Highlight" };
        greenHl.Icon = new Border { Width = 12, Height = 12, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.Parse("#A7F3D0")), BorderBrush = new SolidColorBrush(Color.Parse("#059669")), BorderThickness = new Thickness(1) };
        greenHl.Click += (s, e) => ResolveViewModel()?.HighlightSelectedTextCommand.Execute("#A7F3D0");
        highlightMenu.Items.Add(greenHl);

        var blueHl = new MenuItem { Header = "Blue Highlight" };
        blueHl.Icon = new Border { Width = 12, Height = 12, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.Parse("#BAE6FD")), BorderBrush = new SolidColorBrush(Color.Parse("#0284C7")), BorderThickness = new Thickness(1) };
        blueHl.Click += (s, e) => ResolveViewModel()?.HighlightSelectedTextCommand.Execute("#BAE6FD");
        highlightMenu.Items.Add(blueHl);

        var pinkHl = new MenuItem { Header = "Pink Highlight" };
        pinkHl.Icon = new Border { Width = 12, Height = 12, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.Parse("#FBCFE8")), BorderBrush = new SolidColorBrush(Color.Parse("#DB2777")), BorderThickness = new Thickness(1) };
        pinkHl.Click += (s, e) => ResolveViewModel()?.HighlightSelectedTextCommand.Execute("#FBCFE8");
        highlightMenu.Items.Add(pinkHl);

        var orangeHl = new MenuItem { Header = "Orange Highlight" };
        orangeHl.Icon = new Border { Width = 12, Height = 12, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.Parse("#FED7AA")), BorderBrush = new SolidColorBrush(Color.Parse("#EA580C")), BorderThickness = new Thickness(1) };
        orangeHl.Click += (s, e) => ResolveViewModel()?.HighlightSelectedTextCommand.Execute("#FED7AA");
        highlightMenu.Items.Add(orangeHl);

        menu.Items.Add(highlightMenu);

        // 4. Add Sticky Note from selection
        var noteItem = new MenuItem
        {
            Header = "Add Note on Selection...",
            InputGesture = new KeyGesture(Key.N, KeyModifiers.Control | KeyModifiers.Meta)
        };
        noteItem.Icon = new MaterialIcon { Kind = MaterialIconKind.CommentTextOutline, Width = 15, Height = 15, Foreground = new SolidColorBrush(Color.Parse("#0284C7")) };
        noteItem.Click += (s, e) => ResolveViewModel()?.AddNoteFromSelectionCommand.Execute(null);
        menu.Items.Add(noteItem);

        menu.Items.Add(new Separator());

        // 5. Search in document
        var searchItem = new MenuItem
        {
            Header = "Find in PDF",
            InputGesture = new KeyGesture(Key.F, KeyModifiers.Control | KeyModifiers.Meta)
        };
        searchItem.Icon = new MaterialIcon { Kind = MaterialIconKind.Magnify, Width = 15, Height = 15 };
        searchItem.Click += (s, e) => ResolveViewModel()?.SearchSelectedTextCommand.Execute(null);
        menu.Items.Add(searchItem);

        // 6. Search on web
        var webSearchItem = new MenuItem
        {
            Header = "Search with Google"
        };
        webSearchItem.Icon = new MaterialIcon { Kind = MaterialIconKind.Web, Width = 15, Height = 15, Foreground = new SolidColorBrush(Color.Parse("#2563EB")) };
        webSearchItem.Click += (s, e) => ResolveViewModel()?.SearchWebSelectedTextCommand.Execute(null);
        menu.Items.Add(webSearchItem);

        menu.Items.Add(new Separator());

        // 7. Select All
        var selectAllItem = new MenuItem
        {
            Header = "Select All on Page",
            InputGesture = new KeyGesture(Key.A, KeyModifiers.Control | KeyModifiers.Meta)
        };
        selectAllItem.Icon = new MaterialIcon { Kind = MaterialIconKind.SelectAll, Width = 15, Height = 15 };
        selectAllItem.Click += (s, e) => ResolveViewModel()?.SelectAllPageTextCommand.Execute(null);
        menu.Items.Add(selectAllItem);

        return menu;
    }
}
