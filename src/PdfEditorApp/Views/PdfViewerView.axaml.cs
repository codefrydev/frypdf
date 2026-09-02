using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class PdfViewerView : UserControl
{
    // Pan state
    private bool _isSpacePressed;
    private bool _isPanning;
    private Point _panStart;
    private Vector _scrollStart;

    // Pinch Gesture state
    private bool _isPinching;
    private double _pinchStartZoom = 1.0;

    // Scroll synchronization flags to prevent feedback loops
    private bool _isProgrammaticScroll;
    private bool _isUpdatingFromUserScroll;
    private DateTime _lastWheelPageTurnTime = DateTime.MinValue;
    private PdfViewerViewModel? _subscribedVm;
    private IDisposable? _scrollOffsetSubscription;

    public PdfViewerView()
    {
        InitializeComponent();

        GestureRecognizers.Add(new PinchGestureRecognizer());

        AddHandler(PointerWheelChangedEvent, OnViewerPointerWheelChanged, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerTouchPadGestureMagnifyEvent, OnViewerTouchPadGestureMagnify, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PinchEvent, OnViewerPinch, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PinchEndedEvent, OnViewerPinchEnded, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerPressedEvent, OnViewerPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnViewerPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnViewerPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnViewerKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnViewerKeyUp, RoutingStrategies.Tunnel);

        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        this.GetObservable(Visual.IsVisibleProperty).Subscribe(new ActionObserver<bool>(visible =>
        {
            if (visible)
            {
                SetupScrollListeners();
                SubscribeViewModel(ViewModel);
                Dispatcher.UIThread.Post(OnContinuousScrollOffsetChanged);
            }
        }));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static readonly DirectProperty<PdfViewerView, PdfViewerViewModel?> ViewModelProperty =
        AvaloniaProperty.RegisterDirect<PdfViewerView, PdfViewerViewModel?>(
            nameof(ViewModel),
            o => o.ViewModel);

    public PdfViewerViewModel? ViewModel => DataContext as PdfViewerViewModel;

    private ScrollViewer? ActiveScrollViewer
    {
        get
        {
            if (ViewModel == null) return null;
            if (ViewModel.IsContinuousScroll) return ContinuousScrollViewer;
            if (ViewModel.IsSinglePageMode) return SinglePageScrollViewer;
            if (ViewModel.IsTwoPageSpreadMode) return TwoPageSpreadScrollViewer;
            return null;
        }
    }

    public (double ViewportWidth, double ViewportHeight, double HorizontalPadding, double VerticalPadding) GetViewportDimensions()
    {
        var sv = ActiveScrollViewer;
        if (sv != null)
        {
            double vw = sv.Viewport.Width > 0 ? sv.Viewport.Width : sv.Bounds.Width;
            double vh = sv.Viewport.Height > 0 ? sv.Viewport.Height : sv.Bounds.Height;
            if (vw > 0 && vh > 0)
            {
                double hp = sv.Padding.Left + sv.Padding.Right;
                double vp = sv.Padding.Top + sv.Padding.Bottom;
                return (vw, vh, hp, vp);
            }
        }

        // If the newly active scrollviewer hasn't had a measure pass yet, check sibling viewers
        // which share the exact same grid column bounds
        var sibling = ContinuousScrollViewer ?? SinglePageScrollViewer ?? TwoPageSpreadScrollViewer;
        if (sibling != null)
        {
            double svW = sibling.Viewport.Width > 0 ? sibling.Viewport.Width : sibling.Bounds.Width;
            double svH = sibling.Viewport.Height > 0 ? sibling.Viewport.Height : sibling.Bounds.Height;
            if (svW > 0 && svH > 0)
            {
                double hp = sibling.Padding.Left + sibling.Padding.Right;
                double vp = sibling.Padding.Top + sibling.Padding.Bottom;
                return (svW, svH, hp, vp);
            }
        }

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w > 0 && h > 0)
        {
            if (ViewModel?.IsSidebarOpen == true)
            {
                w = Math.Max(100, w - 260);
            }
            return (w, h, 64.0, 64.0);
        }

        return (800, 600, 64.0, 64.0);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (ViewModel != null && ViewModel.HasDocument && (e.WidthChanged || e.HeightChanged))
        {
            if (ViewModel.IsFitToWidthActive)
            {
                ViewModel.FitToWidth();
            }
            else if (ViewModel.IsFitToPageActive)
            {
                ViewModel.FitToPage();
            }
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        SetupScrollListeners();
        SubscribeViewModel(ViewModel);
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _scrollOffsetSubscription?.Dispose();
        _scrollOffsetSubscription = null;
        UnsubscribeViewModel();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        var oldVm = _subscribedVm;
        var newVm = ViewModel;
        RaisePropertyChanged(ViewModelProperty, oldVm, newVm);
        SubscribeViewModel(newVm);
        SetupScrollListeners();
    }

    private void SubscribeViewModel(PdfViewerViewModel? vm)
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.ScrollToPageRequested -= OnScrollToPageRequested;
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm.ViewportSizeProvider = null;
            _subscribedVm = null;
        }

        if (vm != null)
        {
            _subscribedVm = vm;
            _subscribedVm.ScrollToPageRequested += OnScrollToPageRequested;
            _subscribedVm.PropertyChanged += OnViewModelPropertyChanged;
            _subscribedVm.ViewportSizeProvider = GetViewportDimensions;
        }
    }

    private void UnsubscribeViewModel()
    {
        _scrollOffsetSubscription?.Dispose();
        _scrollOffsetSubscription = null;

        if (_subscribedVm != null)
        {
            _subscribedVm.ScrollToPageRequested -= OnScrollToPageRequested;
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm.ViewportSizeProvider = null;
            _subscribedVm = null;
        }
    }

    private void SetupScrollListeners()
    {
        var continuousViewer = ContinuousScrollViewer;
        if (continuousViewer != null)
        {
            continuousViewer.PropertyChanged -= OnContinuousViewerPropertyChanged;
            continuousViewer.PropertyChanged += OnContinuousViewerPropertyChanged;

            continuousViewer.RemoveHandler(ScrollViewer.ScrollChangedEvent, OnContinuousViewerScrollChanged);
            continuousViewer.AddHandler(ScrollViewer.ScrollChangedEvent, OnContinuousViewerScrollChanged);

            _scrollOffsetSubscription?.Dispose();
            _scrollOffsetSubscription = continuousViewer.GetObservable(ScrollViewer.OffsetProperty)
                .Subscribe(new ActionObserver<Vector>(_ => OnContinuousScrollOffsetChanged()));
        }
    }

    public void OnContinuousViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        OnContinuousScrollOffsetChanged();
    }

    private sealed class ActionObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }

    private void OnContinuousViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs ev)
    {
        if (ev.Property == ScrollViewer.OffsetProperty)
        {
            OnContinuousScrollOffsetChanged();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfViewerViewModel.CurrentPageNumber))
        {
            if (!_isUpdatingFromUserScroll && ViewModel != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ScrollToPage(ViewModel.CurrentPageNumber);
                    ScrollThumbnailIntoView(ViewModel.CurrentPageNumber);
                });
            }
        }
        else if (e.PropertyName == nameof(PdfViewerViewModel.SelectedLayoutMode))
        {
            if (ViewModel != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ScrollToPage(ViewModel.CurrentPageNumber);
                    if (ViewModel.IsFitToPageActive)
                    {
                        ViewModel.FitToPage();
                    }
                    else if (ViewModel.IsFitToWidthActive)
                    {
                        ViewModel.FitToWidth();
                    }
                }, DispatcherPriority.Loaded);
            }
        }
        else if (e.PropertyName == nameof(PdfViewerViewModel.IsLoading))
        {
            // A fresh document just finished loading. The ScrollViewer's offset is likely
            // still (0,0) from before, so no Offset-changed event will fire on its own —
            // without this, only page 1 (rendered eagerly by the load itself) would show
            // anything until the user scrolled.
            if (ViewModel != null && !ViewModel.IsLoading && ViewModel.HasDocument)
            {
                Dispatcher.UIThread.Post(OnContinuousScrollOffsetChanged);
            }
        }
        else if (e.PropertyName == nameof(PdfViewerViewModel.ZoomLevel))
        {
            if (ViewModel != null && ViewModel.IsContinuousScroll)
            {
                Dispatcher.UIThread.Post(OnContinuousScrollOffsetChanged, DispatcherPriority.Background);
            }
        }
    }

    private void OnScrollToPageRequested(int pageNumber)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ScrollToPage(pageNumber);
            ScrollThumbnailIntoView(pageNumber);
        });
    }

    public void ScrollToPage(int pageNumber)
    {
        if (ViewModel == null || pageNumber < 1) return;

        if (ViewModel.IsContinuousScroll)
        {
            var viewer = ContinuousScrollViewer;
            if (viewer == null) return;

            _isProgrammaticScroll = true;
            try
            {
                double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
                var itemsCtrl = ContinuousItemsControl;
                double top = 32.0;

                for (int i = 0; i < pageNumber - 1 && i < ViewModel.Pages.Count; i++)
                {
                    var page = ViewModel.Pages[i];
                    double pageH = page.HeightPoints > 0 ? (page.HeightPoints * zoom) : (842.0 * zoom);
                    double itemTotalH = pageH + 26.0;
                    top += itemTotalH + 28.0;
                }

                double margin = pageNumber == 1 ? 0 : 16.0;
                double targetOffsetY = Math.Max(0, top - margin);
                viewer.Offset = new Vector(viewer.Offset.X, targetOffsetY);
            }
            finally
            {
                _isProgrammaticScroll = false;
            }

            // Programmatic scroll updated the offset while _isProgrammaticScroll suppressed
            // OnContinuousScrollOffsetChanged. Now that the offset is settled and the flag is cleared,
            // immediately trigger visible-page resolution and background rendering for the destination page.
            OnContinuousScrollOffsetChanged();
        }
        else if (ViewModel.IsSinglePageMode)
        {
            var singleViewer = SinglePageScrollViewer;
            if (singleViewer != null)
            {
                singleViewer.Offset = new Vector(singleViewer.Offset.X, 0);
            }
        }
        else if (ViewModel.IsTwoPageSpreadMode)
        {
            var spreadViewer = TwoPageSpreadScrollViewer;
            if (spreadViewer != null)
            {
                spreadViewer.Offset = new Vector(spreadViewer.Offset.X, 0);
            }
        }
    }

    public void ScrollThumbnailIntoView(int pageNumber)
    {
        var thumbViewer = ThumbnailsScrollViewer;
        if (thumbViewer == null || ViewModel == null || pageNumber < 1) return;

        try
        {
            double viewportH = thumbViewer.Viewport.Height;
            if (viewportH <= 0) viewportH = 600;

            double thumbHeight = 270.0;
            double targetY = (pageNumber - 1) * thumbHeight;
            double currentY = thumbViewer.Offset.Y;

            if (targetY < currentY || targetY + thumbHeight > currentY + viewportH)
            {
                thumbViewer.Offset = new Vector(0, Math.Max(0, targetY - 20));
            }
        }
        catch { }
    }

    /// <summary>
    /// Resolves which pages are visible in the viewport and detects which page has the maximum
    /// visible vertical overlap on screen. Uses exact, deterministic pixel geometry calculated
    /// from ScrollViewer.Offset.Y, page dimensions, zoom level, and spacing.
    /// </summary>
    private bool ResolveVisiblePages(out int firstVisiblePage, out int lastVisiblePage, out int anchorPage)
    {
        firstVisiblePage = 1;
        lastVisiblePage = 1;
        anchorPage = 1;

        var viewer = ContinuousScrollViewer;
        if (viewer == null || ViewModel == null) return false;

        int count = ViewModel.Pages.Count;
        if (count == 0) return false;

        double viewportHeight = viewer.Viewport.Height;
        if (viewportHeight <= 0) viewportHeight = viewer.Bounds.Height;
        if (viewportHeight <= 0) viewportHeight = 800;

        double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
        double currentScrollY = viewer.Offset.Y;

        double currentY = 32.0;
        int firstIdx = -1;
        int lastIdx = -1;
        int bestActiveIdx = -1;
        double maxVisibleHeight = -1;

        for (int i = 0; i < count; i++)
        {
            var page = ViewModel.Pages[i];
            double pageH = page.HeightPoints > 0 ? (page.HeightPoints * zoom) : (842.0 * zoom);
            double itemTotalH = pageH + 26.0;

            double top = currentY - currentScrollY;
            double bottom = top + itemTotalH;

            if (bottom > 0 && top < viewportHeight)
            {
                if (firstIdx == -1) firstIdx = i;
                lastIdx = i;

                double visibleTop = Math.Max(0, top);
                double visibleBottom = Math.Min(viewportHeight, bottom);
                double visibleH = Math.Max(0, visibleBottom - visibleTop);
                if (visibleH > maxVisibleHeight)
                {
                    maxVisibleHeight = visibleH;
                    bestActiveIdx = i;
                }
            }

            currentY += itemTotalH + 28.0;
        }

        if (firstIdx == -1)
        {
            if (currentScrollY <= 32.0)
            {
                firstIdx = 0;
                lastIdx = Math.Min(count - 1, 1);
                bestActiveIdx = 0;
            }
            else
            {
                bestActiveIdx = count - 1;
                firstIdx = Math.Max(0, count - 2);
                lastIdx = count - 1;
            }
        }

        firstVisiblePage = ViewModel.Pages[firstIdx].PageNumber;
        lastVisiblePage = ViewModel.Pages[lastIdx].PageNumber;
        anchorPage = ViewModel.Pages[bestActiveIdx].PageNumber;
        return true;
    }

    private void OnContinuousScrollOffsetChanged()
    {
        if (_isProgrammaticScroll || _isUpdatingFromUserScroll) return;
        var vm = ViewModel;
        if (vm == null || !vm.IsContinuousScroll || vm.Pages.Count == 0) return;
        var viewer = ContinuousScrollViewer;
        if (viewer == null) return;

        if (!ResolveVisiblePages(out int firstVisiblePage, out int lastVisiblePage, out int detectedPageNum)) return;

        vm.RequestPagesVisible(firstVisiblePage, lastVisiblePage);

        if (detectedPageNum >= 1 && detectedPageNum <= vm.Pages.Count && vm.CurrentPageNumber != detectedPageNum)
        {
            _isUpdatingFromUserScroll = true;
            try
            {
                vm.CurrentPageNumber = detectedPageNum;
                ScrollThumbnailIntoView(detectedPageNum);
            }
            finally
            {
                _isUpdatingFromUserScroll = false;
            }
        }
    }

    private void OnViewerPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Support Cmd (Meta), Ctrl, and Option (Alt) modifier keys
        bool isZoomModifier = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                              e.KeyModifiers.HasFlag(KeyModifiers.Meta) ||
                              e.KeyModifiers.HasFlag(KeyModifiers.Alt);

        if (isZoomModifier && ViewModel != null)
        {
            double oldZoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            double effectiveDelta = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;

            // Proportional smooth zoom supporting discrete mouse wheel ticks and fractional continuous trackpad gestures
            double zoomDeltaFactor;
            if (Math.Abs(effectiveDelta) >= 1.0)
            {
                zoomDeltaFactor = effectiveDelta > 0 ? 1.15 : (1.0 / 1.15);
            }
            else if (Math.Abs(effectiveDelta) > 0.0001)
            {
                zoomDeltaFactor = Math.Pow(1.002, effectiveDelta * 100);
            }
            else
            {
                zoomDeltaFactor = 1.0;
            }

            double newZoom = Math.Clamp(Math.Round(oldZoom * zoomDeltaFactor, 3), 0.25, 5.0);

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                var scrollViewer = ActiveScrollViewer;
                if (scrollViewer != null)
                {
                    var mouseInViewer = e.GetPosition(scrollViewer);
                    double ratio = newZoom / oldZoom;
                    double targetOffsetX = (scrollViewer.Offset.X + mouseInViewer.X) * ratio - mouseInViewer.X;
                    double targetOffsetY = (scrollViewer.Offset.Y + mouseInViewer.Y) * ratio - mouseInViewer.Y;

                    ViewModel.ZoomLevel = newZoom;
                    scrollViewer.Offset = new Vector(Math.Max(0, targetOffsetX), Math.Max(0, targetOffsetY));
                }
                else
                {
                    ViewModel.ZoomLevel = newZoom;
                }
            }

            e.Handled = true;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            var scrollViewer = ActiveScrollViewer;
            if (scrollViewer != null)
            {
                double delta = e.Delta.Y != 0 ? e.Delta.Y : e.Delta.X;
                scrollViewer.Offset = new Vector(
                    Math.Max(0, scrollViewer.Offset.X - (delta * 40)),
                    scrollViewer.Offset.Y);
                e.Handled = true;
            }
        }
        else if (ViewModel != null && (ViewModel.IsSinglePageMode || ViewModel.IsTwoPageSpreadMode))
        {
            var scrollViewer = ActiveScrollViewer;
            if (scrollViewer != null)
            {
                double deltaY = e.Delta.Y;
                if (Math.Abs(deltaY) > 0.15)
                {
                    bool atBottom = scrollViewer.Offset.Y >= (scrollViewer.Extent.Height - scrollViewer.Viewport.Height - 4.0);
                    bool atTop = scrollViewer.Offset.Y <= 4.0;

                    if (deltaY < 0 && atBottom)
                    {
                        if (DateTime.UtcNow - _lastWheelPageTurnTime > TimeSpan.FromMilliseconds(250))
                        {
                            _lastWheelPageTurnTime = DateTime.UtcNow;
                            ViewModel.NextPageCommand.Execute(null);
                            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, 0);
                            e.Handled = true;
                        }
                    }
                    else if (deltaY > 0 && atTop)
                    {
                        if (DateTime.UtcNow - _lastWheelPageTurnTime > TimeSpan.FromMilliseconds(250))
                        {
                            _lastWheelPageTurnTime = DateTime.UtcNow;
                            ViewModel.PreviousPageCommand.Execute(null);
                            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height));
                            e.Handled = true;
                        }
                    }
                }
            }
        }
    }

    private void OnViewerTouchPadGestureMagnify(object? sender, PointerDeltaEventArgs e)
    {
        if (ViewModel != null)
        {
            double oldZoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            double delta = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;
            if (delta == 0 && (e.Delta.X != 0 || e.Delta.Y != 0))
            {
                delta = e.Delta.X != 0 ? e.Delta.X : e.Delta.Y;
            }

            double zoomFactor = 1.0 + delta;
            double newZoom = Math.Clamp(Math.Round(oldZoom * zoomFactor, 3), 0.25, 5.0);

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                var scrollViewer = ActiveScrollViewer;
                if (scrollViewer != null)
                {
                    var mouseInViewer = e.GetPosition(scrollViewer);
                    double ratio = newZoom / oldZoom;
                    double targetOffsetX = (scrollViewer.Offset.X + mouseInViewer.X) * ratio - mouseInViewer.X;
                    double targetOffsetY = (scrollViewer.Offset.Y + mouseInViewer.Y) * ratio - mouseInViewer.Y;

                    ViewModel.ZoomLevel = newZoom;
                    scrollViewer.Offset = new Vector(Math.Max(0, targetOffsetX), Math.Max(0, targetOffsetY));
                }
                else
                {
                    ViewModel.ZoomLevel = newZoom;
                }
            }

            e.Handled = true;
        }
    }

    private void OnViewerPinch(object? sender, PinchEventArgs e)
    {
        if (ViewModel != null)
        {
            if (!_isPinching)
            {
                _isPinching = true;
                _pinchStartZoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            }

            // e.Scale is the total cumulative scale of the pinch gesture since start (starts at 1.0)
            double targetZoom = Math.Clamp(Math.Round(_pinchStartZoom * e.Scale, 3), 0.25, 5.0);
            if (Math.Abs(targetZoom - ViewModel.ZoomLevel) > 0.002)
            {
                double oldZ = ViewModel.ZoomLevel;
                var scrollViewer = ActiveScrollViewer;

                ViewModel.ZoomLevel = targetZoom;

                if (oldZ > 0 && scrollViewer != null)
                {
                    var origin = e.ScaleOrigin;
                    double ratio = targetZoom / oldZ;
                    double newOffsetX = (scrollViewer.Offset.X + origin.X) * ratio - origin.X;
                    double newOffsetY = (scrollViewer.Offset.Y + origin.Y) * ratio - origin.Y;
                    scrollViewer.Offset = new Vector(Math.Max(0, newOffsetX), Math.Max(0, newOffsetY));
                }
            }
            e.Handled = true;
        }
    }

    private void OnViewerPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        _isPinching = false;
        e.Handled = true;
    }

    private void OnViewerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pt = e.GetCurrentPoint(this);
        if (_isSpacePressed || pt.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            var scrollViewer = ActiveScrollViewer;
            if (scrollViewer != null)
            {
                _scrollStart = scrollViewer.Offset;
            }
            e.Pointer.Capture(this);
            Cursor = new Cursor(StandardCursorType.Hand);
            e.Handled = true;
        }
    }

    private void OnViewerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning)
        {
            var scrollViewer = ActiveScrollViewer;
            if (scrollViewer != null)
            {
                var curPos = e.GetPosition(this);
                var delta = curPos - _panStart;
                scrollViewer.Offset = new Vector(
                    Math.Max(0, _scrollStart.X - delta.X),
                    Math.Max(0, _scrollStart.Y - delta.Y));
                e.Handled = true;
            }
        }
    }

    private void OnViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            Cursor = _isSpacePressed ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
            e.Handled = true;
        }
    }

    private void OnViewerKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpacePressed = false;
            if (!_isPanning)
            {
                Cursor = Cursor.Default;
            }
        }
    }

    private void OnViewerKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        // If typing inside a TextBox, handle Enter & Escape specifically
        if (e.Source is TextBox textBox)
        {
            if (e.Key == Key.Enter)
            {
                if (textBox.Name == "JumpPageTextBox" || textBox == this.FindControl<TextBox>("JumpPageTextBox"))
                {
                    ViewModel.CommitJumpPage();
                    e.Handled = true;
                }
                else if (textBox.Name == "SearchQueryTextBox" || textBox == this.FindControl<TextBox>("SearchQueryTextBox") || ViewModel.IsSearchBarVisible)
                {
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        ViewModel.PreviousMatchCommand.Execute(null);
                    }
                    else
                    {
                        ViewModel.NextMatchCommand.Execute(null);
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                if (ViewModel.IsSearchBarVisible)
                {
                    ViewModel.IsSearchBarVisible = false;
                }
                this.Focus();
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.Space && !_isSpacePressed)
        {
            _isSpacePressed = true;
            Cursor = new Cursor(StandardCursorType.Hand);
            return;
        }

        bool isCtrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);

        if (isCtrlOrCmd)
        {
            switch (e.Key)
            {
                case Key.C:
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        ViewModel.CopySelectedCitationCommand.Execute(null);
                    }
                    else if (ViewModel.HasTextSelection)
                    {
                        ViewModel.CopySelectedTextCommand.Execute(null);
                    }
                    else
                    {
                        ViewModel.CopyPageTextCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
                case Key.A:
                    ViewModel.SelectAllPageTextCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D0:
                case Key.NumPad0:
                    ViewModel.ResetZoomCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D1:
                case Key.NumPad1:
                    ViewModel.FitToWidthCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D9:
                case Key.NumPad9:
                    ViewModel.FitToPageCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.OemPlus:
                case Key.Add:
                    ViewModel.ZoomInCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    ViewModel.ZoomOutCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F:
                    if (ViewModel.HasTextSelection)
                    {
                        ViewModel.SearchSelectedTextCommand.Execute(null);
                    }
                    else
                    {
                        ViewModel.ToggleSearchBarCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
                case Key.R:
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        ViewModel.RotateCounterClockwiseCommand.Execute(null);
                    }
                    else
                    {
                        ViewModel.RotateClockwiseCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
                case Key.B:
                    ViewModel.ToggleSidebarCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
        else
        {
            switch (e.Key)
            {
                case Key.PageDown:
                case Key.Right:
                case Key.Down:
                    ViewModel.NextPageCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.PageUp:
                case Key.Left:
                case Key.Up:
                    ViewModel.PreviousPageCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Home:
                    ViewModel.FirstPageCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.End:
                    ViewModel.LastPageCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.F3:
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        ViewModel.PreviousMatchCommand.Execute(null);
                    }
                    else
                    {
                        ViewModel.NextMatchCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
                case Key.F11:
                    ViewModel.ToggleFullscreenCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    if (ViewModel.IsFullscreen)
                    {
                        ViewModel.IsFullscreen = false;
                        e.Handled = true;
                    }
                    else if (ViewModel.HasTextSelection)
                    {
                        ViewModel.ClearSelection();
                        e.Handled = true;
                    }
                    else if (ViewModel.IsSearchBarVisible)
                    {
                        ViewModel.IsSearchBarVisible = false;
                        e.Handled = true;
                    }
                    break;
            }
        }
    }
}
