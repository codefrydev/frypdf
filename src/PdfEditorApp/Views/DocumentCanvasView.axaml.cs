using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.Views;

public partial class DocumentCanvasView : UserControl
{
    private bool _isDraggingElement;
    private bool _isResizingHandle;
    private bool _isMarqueeSelecting;
    private Point _marqueeStartPoint;
    private string? _activeResizeHandle;
    private Point _lastPointerPosition;
    private ElementViewModelBase? _draggedElement;
    private List<ElementViewModelBase> _draggedElements = new();

    // Movement & Resize Undo Tracking
    private double _dragStartElementX;
    private double _dragStartElementY;
    private List<(ElementViewModelBase Element, double X, double Y)> _dragStartPositions = new();
    private double _resizeStartX;
    private double _resizeStartY;
    private double _resizeStartW;
    private double _resizeStartH;
    private string _initialTextEditContent = "";

    // Pan state
    private bool _isSpacePressed;
    private bool _isPanning;
    private Point _panStart;
    private Vector _scrollStart;

    public DocumentCanvasView()
    {
        InitializeComponent();

        GestureRecognizers.Add(new PinchGestureRecognizer());

        AddHandler(PointerMovedEvent, OnGlobalPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnGlobalPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent, OnCanvasPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(PinchEvent, OnCanvasPinch, RoutingStrategies.Tunnel);
        AddHandler(PinchEndedEvent, OnCanvasPinchEnded, RoutingStrategies.Tunnel);
        AddHandler(DoubleTappedEvent, OnCanvasDoubleTapped, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnCanvasKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnCanvasKeyUp, RoutingStrategies.Tunnel);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (CanvasScrollViewer != null)
        {
            CanvasScrollViewer.PropertyChanged += (s, ev) =>
            {
                if (ev.Property == ScrollViewer.OffsetProperty || ev.Property == ScrollViewer.ViewportProperty)
                {
                    UpdateViewportOnPlacementService();
                }
            };
        }
    }

    private void UpdateViewportOnPlacementService()
    {
        if (CanvasScrollViewer == null || PageElementsCanvas == null || ViewModel?.CurrentPage == null) return;

        double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
        var scrollCenter = new Point(CanvasScrollViewer.Viewport.Width / 2.0, CanvasScrollViewer.Viewport.Height / 2.0);
        var canvasCenter = CanvasScrollViewer.TranslatePoint(scrollCenter, PageElementsCanvas);

        if (canvasCenter.HasValue)
        {
            double pageCenterX = canvasCenter.Value.X / zoom;
            double pageCenterY = canvasCenter.Value.Y / zoom;
            double visibleWidth = CanvasScrollViewer.Viewport.Width / zoom;
            double visibleHeight = CanvasScrollViewer.Viewport.Height / zoom;

            ViewModel.SmartPlacement.UpdateViewport(pageCenterX, pageCenterY, visibleWidth, visibleHeight);
        }
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        bool isCtrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (isCtrlOrCmd && ViewModel != null && CanvasScrollViewer != null && PageElementsCanvas != null)
        {
            var pointerPos = e.GetPosition(PageElementsCanvas);
            double oldZoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            double canvasX = pointerPos.X / oldZoom;
            double canvasY = pointerPos.Y / oldZoom;

            double zoomFactor = e.Delta.Y > 0 ? 1.15 : (1.0 / 1.15);
            double newZoom = Math.Clamp(Math.Round(oldZoom * zoomFactor, 2), 0.1, 5.0);

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                ViewModel.ZoomLevel = newZoom;

                // Adjust scroll offset so (canvasX, canvasY) remains anchored under the pointer
                var mouseInViewer = e.GetPosition(CanvasScrollViewer);
                double targetOffsetX = (canvasX * newZoom) - mouseInViewer.X;
                double targetOffsetY = (canvasY * newZoom) - mouseInViewer.Y;
                CanvasScrollViewer.Offset = new Vector(Math.Max(0, targetOffsetX), Math.Max(0, targetOffsetY));
            }

            UpdateViewportOnPlacementService();
            e.Handled = true;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && CanvasScrollViewer != null)
        {
            // Shift + Wheel -> Horizontal scroll
            double delta = e.Delta.Y != 0 ? e.Delta.Y : e.Delta.X;
            CanvasScrollViewer.Offset = new Vector(
                Math.Max(0, CanvasScrollViewer.Offset.X - (delta * 40)),
                CanvasScrollViewer.Offset.Y);
            e.Handled = true;
        }
    }

    private void OnCanvasPinch(object? sender, PinchEventArgs e)
    {
        if (ViewModel != null && CanvasScrollViewer != null)
        {
            double oldZoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            double newZoom = Math.Clamp(Math.Round(oldZoom * e.Scale, 2), 0.1, 5.0);
            if (Math.Abs(newZoom - oldZoom) > 0.005)
            {
                ViewModel.ZoomLevel = newZoom;
            }
            e.Handled = true;
        }
    }

    private void OnCanvasPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        UpdateViewportOnPlacementService();
        e.Handled = true;
    }

    private void OnCanvasDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel == null || CanvasScrollViewer == null) return;

        if (ViewModel.ActiveToolMode == Models.ToolMode.Pan || _isSpacePressed)
        {
            ViewModel.FitToPageDynamic(CanvasScrollViewer.Viewport.Width, CanvasScrollViewer.Viewport.Height);
            e.Handled = true;
        }
        else if (ViewModel.ActiveToolMode == Models.ToolMode.Zoom)
        {
            ViewModel.ResetZoom();
            e.Handled = true;
        }
    }

    private void OnCanvasBackgroundPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel?.CurrentPage == null || PageElementsCanvas == null) return;

        var pointerPoint = e.GetCurrentPoint(this);

        // Middle button click, Spacebar held, or Pan Tool mode => Pan mode
        if (_isSpacePressed || pointerPoint.Properties.IsMiddleButtonPressed || ViewModel.ActiveToolMode == Models.ToolMode.Pan)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            if (CanvasScrollViewer != null)
            {
                _scrollStart = new Vector(CanvasScrollViewer.Offset.X, CanvasScrollViewer.Offset.Y);
            }
            Cursor = new Cursor(StandardCursorType.Hand);
            e.Handled = true;
            return;
        }

        // Zoom Tool mode => Click to Zoom In / Alt-Click to Zoom Out
        if (ViewModel.ActiveToolMode == Models.ToolMode.Zoom)
        {
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                bool isAlt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
                if (isAlt)
                {
                    ViewModel.ZoomOut();
                }
                else
                {
                    ViewModel.ZoomIn();
                }
                UpdateViewportOnPlacementService();
                e.Handled = true;
                return;
            }
        }

        var pos = e.GetPosition(PageElementsCanvas);
        double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
        double canvasX = pos.X / zoom;
        double canvasY = pos.Y / zoom;

        // Register right-click point for context-menu placement (Adobe Acrobat / Photoshop standard)
        if (pointerPoint.Properties.IsRightButtonPressed)
        {
            ViewModel.SmartPlacement.SetContextMenuPointer(canvasX, canvasY);
        }

        if (ViewModel.ActiveToolMode == Models.ToolMode.Draw || ViewModel.ActiveToolMode == Models.ToolMode.Highlight)
        {
            bool isHighlighter = ViewModel.ActiveToolMode == Models.ToolMode.Highlight;
            var ink = new InkElementViewModel
            {
                X = Math.Max(0, canvasX),
                Y = Math.Max(0, canvasY),
                Width = 20,
                Height = isHighlighter ? 18 : 6,
                StrokeColorHex = isHighlighter ? "#FEF08A" : "#0F6CBD",
                StrokeThickness = isHighlighter ? 14.0 : 3.0,
                Opacity = isHighlighter ? 0.45 : 1.0,
                IsHighlighter = isHighlighter
            };

            ViewModel.AddElementWithUndo(ink, isHighlighter ? "Added Highlighter Stroke" : "Added Ink Stroke");
            _isResizingHandle = true;
            _activeResizeHandle = "bottomright";
            _draggedElement = ink;
            _resizeStartX = ink.X;
            _resizeStartY = ink.Y;
            _resizeStartW = ink.Width;
            _resizeStartH = ink.Height;
            _lastPointerPosition = pos;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        // Marquee Selection Drag on background
        bool isToggle = e.KeyModifiers.HasFlag(KeyModifiers.Shift) || e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (!isToggle)
        {
            ViewModel.CurrentPage.ClearSelection();
        }

        if (pointerPoint.Properties.IsLeftButtonPressed)
        {
            _isMarqueeSelecting = true;
            _marqueeStartPoint = pos;
            if (MarqueeSelectionBox != null)
            {
                Canvas.SetLeft(MarqueeSelectionBox, pos.X);
                Canvas.SetTop(MarqueeSelectionBox, pos.Y);
                MarqueeSelectionBox.Width = 0;
                MarqueeSelectionBox.Height = 0;
                MarqueeSelectionBox.IsVisible = true;
            }
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isSpacePressed || e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            if (CanvasScrollViewer != null)
            {
                _scrollStart = new Vector(CanvasScrollViewer.Offset.X, CanvasScrollViewer.Offset.Y);
            }
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (sender is Control control && control.DataContext is ElementViewModelBase elementVm)
        {
            bool isToggle = e.KeyModifiers.HasFlag(KeyModifiers.Shift) || e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);

            if (isToggle)
            {
                ViewModel?.CurrentPage?.ToggleElementSelection(elementVm);
            }
            else if (!elementVm.IsSelected)
            {
                ViewModel?.CurrentPage?.SelectElement(elementVm);
            }

            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed && PageElementsCanvas != null && ViewModel != null)
            {
                var pos = e.GetPosition(PageElementsCanvas);
                double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
                ViewModel.SmartPlacement.SetContextMenuPointer(pos.X / zoom, pos.Y / zoom);
            }

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && ViewModel?.CurrentPage != null)
            {
                _isDraggingElement = true;
                _draggedElement = elementVm;
                _draggedElements = ViewModel.CurrentPage.SelectedElements.ToList();
                if (!_draggedElements.Contains(elementVm)) _draggedElements.Add(elementVm);
                _dragStartPositions = _draggedElements.Select(el => (Element: el, el.X, el.Y)).ToList();

                _dragStartElementX = elementVm.X;
                _dragStartElementY = elementVm.Y;
                _lastPointerPosition = e.GetPosition(PageElementsCanvas);
                e.Pointer.Capture(this);
                e.Handled = true;
            }
        }
    }

    private void OnHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.Tag is string handleName && control.DataContext is ElementViewModelBase elementVm)
        {
            _isResizingHandle = true;
            _activeResizeHandle = handleName;
            _draggedElement = elementVm;
            _resizeStartX = elementVm.X;
            _resizeStartY = elementVm.Y;
            _resizeStartW = elementVm.Width;
            _resizeStartH = elementVm.Height;
            _lastPointerPosition = e.GetPosition(PageElementsCanvas);
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnTextElementDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is TextElementViewModel textVm)
        {
            _initialTextEditContent = textVm.Text;
            textVm.IsInEditMode = true;
            e.Handled = true;
        }
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.DataContext is TextElementViewModel textVm)
        {
            if (textVm.Text != _initialTextEditContent)
            {
                string oldTxt = _initialTextEditContent;
                string newTxt = textVm.Text;
                ViewModel?.UndoRedo.RecordAction(
                    "Edit Text",
                    () => textVm.Text = oldTxt,
                    () => textVm.Text = newTxt
                );
            }
            textVm.IsInEditMode = false;
        }
    }

    private void OnMathElementDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is MathElementViewModel mathVm)
        {
            _initialTextEditContent = mathVm.Formula;
            ViewModel?.OpenMathStudioCommand.Execute(mathVm);
            e.Handled = true;
        }
    }

    private void OnMathTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.DataContext is MathElementViewModel mathVm)
        {
            if (mathVm.Formula != _initialTextEditContent)
            {
                string oldFormula = _initialTextEditContent;
                string newFormula = mathVm.Formula;
                ViewModel?.UndoRedo.RecordAction(
                    "Edit Formula",
                    () => { mathVm.Formula = oldFormula; mathVm.RenderSvg(); },
                    () => { mathVm.Formula = newFormula; mathVm.RenderSvg(); }
                );
            }
            mathVm.IsInEditMode = false;
            mathVm.RenderSvg();
        }
    }

    private void OnGlobalPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && CanvasScrollViewer != null)
        {
            var curPos = e.GetPosition(this);
            var delta = curPos - _panStart;
            CanvasScrollViewer.Offset = new Vector(
                Math.Max(0, _scrollStart.X - delta.X),
                Math.Max(0, _scrollStart.Y - delta.Y));
            e.Handled = true;
            return;
        }

        if (PageElementsCanvas != null && ViewModel != null)
        {
            var pPos = e.GetPosition(PageElementsCanvas);
            double z = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            ViewModel.CursorCanvasX = Math.Max(0, pPos.X / z);
            ViewModel.CursorCanvasY = Math.Max(0, pPos.Y / z);
        }

        if (_isMarqueeSelecting && PageElementsCanvas != null && ViewModel?.CurrentPage != null)
        {
            var curPos = e.GetPosition(PageElementsCanvas);
            double minX = Math.Min(_marqueeStartPoint.X, curPos.X);
            double minY = Math.Min(_marqueeStartPoint.Y, curPos.Y);
            double w = Math.Abs(curPos.X - _marqueeStartPoint.X);
            double h = Math.Abs(curPos.Y - _marqueeStartPoint.Y);

            if (MarqueeSelectionBox != null)
            {
                Canvas.SetLeft(MarqueeSelectionBox, minX);
                Canvas.SetTop(MarqueeSelectionBox, minY);
                MarqueeSelectionBox.Width = w;
                MarqueeSelectionBox.Height = h;
            }

            double z = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            var marqueeRect = new Rect(minX / z, minY / z, Math.Max(1, w / z), Math.Max(1, h / z));

            var intersecting = ViewModel.CurrentPage.Elements
                .Where(el => marqueeRect.Intersects(new Rect(el.X, el.Y, Math.Max(1, el.Width), Math.Max(1, el.Height))))
                .ToList();

            ViewModel.CurrentPage.SelectElements(intersecting);
            e.Handled = true;
            return;
        }

        if (PageElementsCanvas == null || ViewModel?.CurrentPage == null) return;
        if (!_isResizingHandle && !_isDraggingElement) return;

        var currentPos = e.GetPosition(PageElementsCanvas);
        double deltaX = currentPos.X - _lastPointerPosition.X;
        double deltaY = currentPos.Y - _lastPointerPosition.Y;

        double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
        deltaX /= zoom;
        deltaY /= zoom;

        if (_isResizingHandle && _draggedElement != null && !string.IsNullOrEmpty(_activeResizeHandle))
        {
            _draggedElement.Resize(_activeResizeHandle, deltaX, deltaY);
            _lastPointerPosition = currentPos;
            ViewModel.CurrentPage.UpdateSelectionBoundingBox();
        }
        else if (_isDraggingElement && _draggedElements.Count > 0)
        {
            foreach (var el in _draggedElements)
            {
                el.MoveBy(deltaX, deltaY, ViewModel.CurrentPage.Width, ViewModel.CurrentPage.Height);
            }
            _lastPointerPosition = currentPos;
            ViewModel.CurrentPage.UpdateSelectionBoundingBox();
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        // Handled by tunnel/bubble or direct event
    }

    private void OnGlobalPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isMarqueeSelecting)
        {
            if (MarqueeSelectionBox != null) MarqueeSelectionBox.IsVisible = false;
            _isMarqueeSelecting = false;
        }
        else if (_isResizingHandle && _draggedElement != null)
        {
            var el = _draggedElement;
            if (ViewModel != null && ViewModel.SnapToGrid)
            {
                double snap = (double)(int)ViewModel.GridSnapSize;
                if (snap > 1)
                {
                    el.X = Math.Round(el.X / snap) * snap;
                    el.Y = Math.Round(el.Y / snap) * snap;
                    el.Width = Math.Max(snap, Math.Round(el.Width / snap) * snap);
                    el.Height = Math.Max(snap, Math.Round(el.Height / snap) * snap);
                }
            }

            double fromX = _resizeStartX, fromY = _resizeStartY, fromW = _resizeStartW, fromH = _resizeStartH;
            double toX = el.X, toY = el.Y, toW = el.Width, toH = el.Height;
            if (Math.Abs(toW - fromW) > 0.5 || Math.Abs(toH - fromH) > 0.5 || Math.Abs(toX - fromX) > 0.5 || Math.Abs(toY - fromY) > 0.5)
            {
                ViewModel?.UndoRedo.RecordAction(
                    $"Resize {el.DisplayName}",
                    () => { el.X = fromX; el.Y = fromY; el.Width = fromW; el.Height = fromH; ViewModel?.CurrentPage?.UpdateSelectionBoundingBox(); },
                    () => { el.X = toX; el.Y = toY; el.Width = toW; el.Height = toH; ViewModel?.CurrentPage?.UpdateSelectionBoundingBox(); }
                );
            }
        }
        else if (_isDraggingElement && _draggedElements.Count > 0)
        {
            if (ViewModel != null && ViewModel.SnapToGrid)
            {
                double snap = (double)(int)ViewModel.GridSnapSize;
                if (snap > 1)
                {
                    foreach (var el in _draggedElements)
                    {
                        el.X = Math.Round(el.X / snap) * snap;
                        el.Y = Math.Round(el.Y / snap) * snap;
                    }
                }
            }

            var initialPositions = _dragStartPositions.ToList();
            var finalPositions = _draggedElements.Select(el => (Element: el, el.X, el.Y)).ToList();

            bool anyMoved = false;
            for (int i = 0; i < initialPositions.Count; i++)
            {
                if (Math.Abs(initialPositions[i].X - finalPositions[i].X) > 0.5 || Math.Abs(initialPositions[i].Y - finalPositions[i].Y) > 0.5)
                {
                    anyMoved = true;
                    break;
                }
            }

            if (anyMoved)
            {
                string desc = _draggedElements.Count == 1 ? $"Move {_draggedElements[0].DisplayName}" : $"Move {_draggedElements.Count} Elements";
                ViewModel?.UndoRedo.RecordAction(
                    desc,
                    () => {
                        foreach (var p in initialPositions) { p.Element.X = p.X; p.Element.Y = p.Y; }
                        ViewModel?.CurrentPage?.UpdateSelectionBoundingBox();
                    },
                    () => {
                        foreach (var p in finalPositions) { p.Element.X = p.X; p.Element.Y = p.Y; }
                        ViewModel?.CurrentPage?.UpdateSelectionBoundingBox();
                    }
                );
            }
        }

        e.Pointer.Capture(null);
        _isDraggingElement = false;
        _isResizingHandle = false;
        _activeResizeHandle = null;
        _draggedElement = null;
        _draggedElements.Clear();
        _dragStartPositions.Clear();
        _isPanning = false;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (MarqueeSelectionBox != null) MarqueeSelectionBox.IsVisible = false;
        _isMarqueeSelecting = false;
        _isDraggingElement = false;
        _isResizingHandle = false;
        _activeResizeHandle = null;
        _draggedElement = null;
        _draggedElements.Clear();
        _dragStartPositions.Clear();
        _isPanning = false;
    }

    private void OnCanvasKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpacePressed = false;
            Cursor = Cursor.Default;
        }
    }

    private void OnCanvasKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel?.CurrentPage == null) return;

        var selected = ViewModel.CurrentPage.SelectedElement;

        // If user is actively typing inside a TextBox, don't intercept typing keys
        if (e.Source is TextBox)
        {
            if (e.Key == Key.Escape && selected is TextElementViewModel textVm)
            {
                textVm.IsInEditMode = false;
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
        double step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10.0 : 1.0;

        if (isCtrlOrCmd)
        {
            switch (e.Key)
            {
                case Key.D0:
                case Key.NumPad0:
                    ViewModel.FitToPageDynamic(CanvasScrollViewer?.Viewport.Width ?? 800, CanvasScrollViewer?.Viewport.Height ?? 800);
                    e.Handled = true;
                    break;
                case Key.D1:
                case Key.NumPad1:
                    ViewModel.ResetZoom();
                    e.Handled = true;
                    break;
                case Key.D2:
                case Key.NumPad2:
                    ViewModel.FitToWidthDynamic(CanvasScrollViewer?.Viewport.Width ?? 800);
                    e.Handled = true;
                    break;
                case Key.OemPlus:
                case Key.Add:
                    ViewModel.ZoomIn();
                    e.Handled = true;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    ViewModel.ZoomOut();
                    e.Handled = true;
                    break;
                case Key.A:
                    ViewModel.CurrentPage?.SelectAll();
                    e.Handled = true;
                    break;
                case Key.C:
                    ViewModel.CopyCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.X:
                    ViewModel.CutCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.V:
                    ViewModel.PasteCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D:
                    ViewModel.DuplicateCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Z:
                    ViewModel.UndoCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Y:
                    ViewModel.RedoCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.S:
                    ViewModel.SaveProjectCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.OemCloseBrackets:
                    ViewModel.Inspector.BringToFrontCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.OemOpenBrackets:
                    ViewModel.Inspector.SendToBackCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
        else if (selected != null)
        {
            switch (e.Key)
            {
                case Key.Delete:
                case Key.Back:
                    ViewModel.Inspector.DeleteSelectedElementCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    ViewModel.CurrentPage.ClearSelection();
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                    double oldX = selected.X;
                    double oldY = selected.Y;

                    if (e.Key == Key.Left) selected.X = Math.Max(0, selected.X - step);
                    else if (e.Key == Key.Right) selected.X = Math.Min(ViewModel.CurrentPage.Width - selected.Width, selected.X + step);
                    else if (e.Key == Key.Up) selected.Y = Math.Max(0, selected.Y - step);
                    else if (e.Key == Key.Down) selected.Y = Math.Min(ViewModel.CurrentPage.Height - selected.Height, selected.Y + step);

                    double newX = selected.X;
                    double newY = selected.Y;
                    var el = selected;
                    ViewModel.UndoRedo.RecordAction(
                        $"Nudge {el.DisplayName}",
                        () => { el.X = oldX; el.Y = oldY; },
                        () => { el.X = newX; el.Y = newY; }
                    );
                    e.Handled = true;
                    break;
                case Key.OemCloseBrackets:
                    ViewModel.Inspector.BringToFrontCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.OemOpenBrackets:
                    ViewModel.Inspector.SendToBackCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
        else
        {
            // Global quick single-key tool switches (when no element is selected)
            switch (e.Key)
            {
                case Key.V:
                    ViewModel.SetToolModeCommand.Execute("Select");
                    e.Handled = true;
                    break;
                case Key.H:
                    ViewModel.SetToolModeCommand.Execute("Pan");
                    e.Handled = true;
                    break;
                case Key.Z:
                    ViewModel.SetToolModeCommand.Execute("Zoom");
                    e.Handled = true;
                    break;
                case Key.T:
                    ViewModel.AddTextElementCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.R:
                    ViewModel.AddShapeElementCommand.Execute("Rectangle");
                    e.Handled = true;
                    break;
                case Key.N:
                    ViewModel.AddStickyNoteElementCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D:
                    ViewModel.AddInkElementCommand.Execute(false);
                    e.Handled = true;
                    break;
            }
        }
    }
}
