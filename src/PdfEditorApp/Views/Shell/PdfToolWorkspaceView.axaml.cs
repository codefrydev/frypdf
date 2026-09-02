using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;

namespace PdfEditorApp.Views.Shell;

/// <summary>
/// Reader-style shell (toolbar + tabbed left sidebar + live preview canvas) shared by
/// tool screens. Each tool composes it by supplying its own settings content and CTA
/// button through the content-slot properties below; the toolbar, Files/Pages/Info
/// tabs, and preview rendering are shared, backed by <see cref="PdfToolViewModelBase.Preview"/>.
///
/// Also mirrors the PDF Reader's (<see cref="PdfEditorApp.Views.PdfViewerView"/>) zoom
/// gestures — Ctrl/Cmd/Alt+scroll, trackpad pinch, and space/middle-click pan — over the
/// single preview ScrollViewer here, so every tool gets the same interaction the reader
/// has instead of only the toolbar +/- buttons. Zoom bounds (0.5-3.0) match
/// PdfLivePreviewViewModel's own clamp, since ZoomLevel here is set directly rather than
/// through ZoomInCommand/ZoomOutCommand.
/// </summary>
public partial class PdfToolWorkspaceView : UserControl
{
    private const double MinZoom = 0.5;
    private const double MaxZoom = 3.0;

    public static readonly StyledProperty<object?> OptionsContentProperty =
        AvaloniaProperty.Register<PdfToolWorkspaceView, object?>(nameof(OptionsContent));

    public static readonly StyledProperty<object?> ToolbarActionContentProperty =
        AvaloniaProperty.Register<PdfToolWorkspaceView, object?>(nameof(ToolbarActionContent));

    public static readonly StyledProperty<object?> CanvasOverlayContentProperty =
        AvaloniaProperty.Register<PdfToolWorkspaceView, object?>(nameof(CanvasOverlayContent));

    public static readonly StyledProperty<object?> SideBySideContentProperty =
        AvaloniaProperty.Register<PdfToolWorkspaceView, object?>(nameof(SideBySideContent));

    public static readonly DirectProperty<PdfToolWorkspaceView, GridLength> SideBySideColumnWidthProperty =
        AvaloniaProperty.RegisterDirect<PdfToolWorkspaceView, GridLength>(
            nameof(SideBySideColumnWidth),
            o => o.SideBySideColumnWidth,
            (o, v) => o.SideBySideColumnWidth = v);

    private GridLength _sideBySideColumnWidth = new GridLength(0, GridUnitType.Pixel);

    public GridLength SideBySideColumnWidth
    {
        get => _sideBySideColumnWidth;
        set => SetAndRaise(SideBySideColumnWidthProperty, ref _sideBySideColumnWidth, value);
    }

    public static readonly StyledProperty<string> SelectedTabProperty =
        AvaloniaProperty.Register<PdfToolWorkspaceView, string>(nameof(SelectedTab), "Options");

    private readonly ScrollViewer? _previewScrollViewer;

    // Pan state (space-drag or middle-click drag)
    private bool _isSpacePressed;
    private bool _isPanning;
    private Point _panStart;
    private Vector _scrollStart;

    // Pinch gesture state
    private bool _isPinching;
    private double _pinchStartZoom = 1.0;

    /// <summary>The tool's own settings/options UI, shown in the sidebar's Options tab (selected by default).</summary>
    public object? OptionsContent
    {
        get => GetValue(OptionsContentProperty);
        set => SetValue(OptionsContentProperty, value);
    }

    /// <summary>The tool's primary action button (e.g. "Merge PDFs Now"), shown in the toolbar's right slot.</summary>
    public object? ToolbarActionContent
    {
        get => GetValue(ToolbarActionContentProperty);
        set => SetValue(ToolbarActionContentProperty, value);
    }

    /// <summary>Optional interactive layer drawn over the current page (e.g. Redact's mark-drawing canvas).</summary>
    public object? CanvasOverlayContent
    {
        get => GetValue(CanvasOverlayContentProperty);
        set => SetValue(CanvasOverlayContentProperty, value);
    }

    /// <summary>Optional side-by-side comparison panel shown next to the preview canvas (e.g. OCR extracted text or side-by-side diff).</summary>
    public object? SideBySideContent
    {
        get => GetValue(SideBySideContentProperty);
        set => SetValue(SideBySideContentProperty, value);
    }

    public string SelectedTab
    {
        get => GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    /// <summary>Typed DataContext accessor so page-card templates (whose own DataContext is the page item) can still reach the tool ViewModel by element name.</summary>
    public PdfToolViewModelBase? ViewModel => DataContext as PdfToolViewModelBase;

    public PdfToolWorkspaceView()
    {
        InitializeComponent();
        _previewScrollViewer = this.FindControl<ScrollViewer>("PreviewScrollViewer");

        GestureRecognizers.Add(new PinchGestureRecognizer());

        AddHandler(PointerWheelChangedEvent, OnWorkspacePointerWheelChanged, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerTouchPadGestureMagnifyEvent, OnWorkspaceTouchPadGestureMagnify, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PinchEvent, OnWorkspacePinch, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PinchEndedEvent, OnWorkspacePinchEnded, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerPressedEvent, OnWorkspacePointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnWorkspacePointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnWorkspacePointerReleased, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnWorkspaceKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnWorkspaceKeyUp, RoutingStrategies.Tunnel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SideBySideContentProperty)
        {
            SideBySideColumnWidth = change.NewValue != null
                ? new GridLength(1, GridUnitType.Star)
                : new GridLength(0, GridUnitType.Pixel);
        }
    }

    private void OnTabButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string tabName })
        {
            SelectedTab = tabName;
        }
    }

    private void OnThumbnailPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: PdfToolPreviewPage page })
        {
            ViewModel?.Preview.SelectPageCommand.Execute(page);
        }
    }

    private void SetZoomAnchoredAtPointer(double newZoom, double oldZoom, Point pointerInViewer)
    {
        var preview = ViewModel?.Preview;
        if (preview == null) return;

        if (_previewScrollViewer != null && oldZoom > 0)
        {
            double ratio = newZoom / oldZoom;
            double targetOffsetX = (_previewScrollViewer.Offset.X + pointerInViewer.X) * ratio - pointerInViewer.X;
            double targetOffsetY = (_previewScrollViewer.Offset.Y + pointerInViewer.Y) * ratio - pointerInViewer.Y;

            preview.ZoomLevel = newZoom;
            _previewScrollViewer.Offset = new Vector(Math.Max(0, targetOffsetX), Math.Max(0, targetOffsetY));
        }
        else
        {
            preview.ZoomLevel = newZoom;
        }
    }

    private void OnWorkspacePointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var preview = ViewModel?.Preview;
        if (preview == null || !preview.HasDocument) return;

        bool isZoomModifier = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                              e.KeyModifiers.HasFlag(KeyModifiers.Meta) ||
                              e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (!isZoomModifier) return;

        double oldZoom = preview.ZoomLevel > 0 ? preview.ZoomLevel : 1.0;
        double effectiveDelta = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;

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

        double newZoom = Math.Clamp(Math.Round(oldZoom * zoomDeltaFactor, 3), MinZoom, MaxZoom);
        if (Math.Abs(newZoom - oldZoom) > 0.001 && _previewScrollViewer != null)
        {
            SetZoomAnchoredAtPointer(newZoom, oldZoom, e.GetPosition(_previewScrollViewer));
        }

        e.Handled = true;
    }

    private void OnWorkspaceTouchPadGestureMagnify(object? sender, PointerDeltaEventArgs e)
    {
        var preview = ViewModel?.Preview;
        if (preview == null || !preview.HasDocument || _previewScrollViewer == null) return;

        double oldZoom = preview.ZoomLevel > 0 ? preview.ZoomLevel : 1.0;
        double delta = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;
        if (delta == 0 && (e.Delta.X != 0 || e.Delta.Y != 0))
        {
            delta = e.Delta.X != 0 ? e.Delta.X : e.Delta.Y;
        }

        double newZoom = Math.Clamp(Math.Round(oldZoom * (1.0 + delta), 3), MinZoom, MaxZoom);
        if (Math.Abs(newZoom - oldZoom) > 0.001)
        {
            SetZoomAnchoredAtPointer(newZoom, oldZoom, e.GetPosition(_previewScrollViewer));
        }

        e.Handled = true;
    }

    private void OnWorkspacePinch(object? sender, PinchEventArgs e)
    {
        var preview = ViewModel?.Preview;
        if (preview == null || !preview.HasDocument) return;

        if (!_isPinching)
        {
            _isPinching = true;
            _pinchStartZoom = preview.ZoomLevel > 0 ? preview.ZoomLevel : 1.0;
        }

        double targetZoom = Math.Clamp(Math.Round(_pinchStartZoom * e.Scale, 3), MinZoom, MaxZoom);
        if (Math.Abs(targetZoom - preview.ZoomLevel) > 0.002)
        {
            double oldZoom = preview.ZoomLevel;
            if (_previewScrollViewer != null && oldZoom > 0)
            {
                double ratio = targetZoom / oldZoom;
                var origin = e.ScaleOrigin;
                double newOffsetX = (_previewScrollViewer.Offset.X + origin.X) * ratio - origin.X;
                double newOffsetY = (_previewScrollViewer.Offset.Y + origin.Y) * ratio - origin.Y;

                preview.ZoomLevel = targetZoom;
                _previewScrollViewer.Offset = new Vector(Math.Max(0, newOffsetX), Math.Max(0, newOffsetY));
            }
            else
            {
                preview.ZoomLevel = targetZoom;
            }
        }

        e.Handled = true;
    }

    private void OnWorkspacePinchEnded(object? sender, PinchEndedEventArgs e)
    {
        _isPinching = false;
        e.Handled = true;
    }

    private void OnWorkspacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pt = e.GetCurrentPoint(this);
        if (_isSpacePressed || pt.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            if (_previewScrollViewer != null)
            {
                _scrollStart = _previewScrollViewer.Offset;
            }
            e.Pointer.Capture(this);
            Cursor = new Cursor(StandardCursorType.Hand);
            e.Handled = true;
        }
    }

    private void OnWorkspacePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && _previewScrollViewer != null)
        {
            var curPos = e.GetPosition(this);
            var delta = curPos - _panStart;
            _previewScrollViewer.Offset = new Vector(
                Math.Max(0, _scrollStart.X - delta.X),
                Math.Max(0, _scrollStart.Y - delta.Y));
            e.Handled = true;
        }
    }

    private void OnWorkspacePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            Cursor = _isSpacePressed ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
            e.Handled = true;
        }
    }

    private void OnWorkspaceKeyDown(object? sender, KeyEventArgs e)
    {
        var preview = ViewModel?.Preview;
        if (preview == null) return;

        if (e.Source is TextBox)
        {
            // Don't hijack typing in the Options tab's own text fields.
            return;
        }

        if (e.Key == Key.Space && !_isSpacePressed)
        {
            _isSpacePressed = true;
            Cursor = new Cursor(StandardCursorType.Hand);
            return;
        }

        bool isCtrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (!isCtrlOrCmd) return;

        switch (e.Key)
        {
            case Key.OemPlus:
            case Key.Add:
                preview.ZoomInCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.OemMinus:
            case Key.Subtract:
                preview.ZoomOutCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.D0:
            case Key.NumPad0:
                preview.ResetZoomCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void OnWorkspaceKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpacePressed = false;
            Cursor = Cursor.Default;
        }
    }
}
