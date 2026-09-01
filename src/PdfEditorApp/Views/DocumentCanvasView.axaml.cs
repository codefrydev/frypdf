using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
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

    // Potential Drag Tracking (for reliable click vs drag detection)
    private bool _isPotentialDrag;
    private ElementViewModelBase? _potentialDragElement;
    private Point _potentialDragStartPos;
    private bool _wasAlreadySelectedOnPress;

    // In-place text editing baseline tracking
    private readonly Dictionary<string, string> _initialEditContents = new();

    // Movement & Resize Undo Tracking
    private double _dragStartElementX;
    private double _dragStartElementY;
    private List<(ElementViewModelBase Element, double X, double Y)> _dragStartPositions = new();
    private double _resizeStartX;
    private double _resizeStartY;
    private double _resizeStartW;
    private double _resizeStartH;

    // Pan state
    private bool _isSpacePressed;
    private bool _isPanning;
    private Point _panStart;
    private Vector _scrollStart;

    // Pinch Gesture state
    private bool _isPinching;
    private double _pinchStartZoom = 1.0;

    public DocumentCanvasView()
    {
        InitializeComponent();

        GestureRecognizers.Add(new PinchGestureRecognizer());

        AddHandler(PointerMovedEvent, OnGlobalPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnGlobalPointerReleased, RoutingStrategies.Bubble);
        AddHandler(PointerWheelChangedEvent, OnCanvasPointerWheelChanged, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerTouchPadGestureMagnifyEvent, OnCanvasTouchPadGestureMagnify, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PinchEvent, OnCanvasPinch, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PinchEndedEvent, OnCanvasPinchEnded, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(DoubleTappedEvent, OnCanvasDoubleTapped, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnCanvasKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnCanvasKeyUp, RoutingStrategies.Tunnel);

        InspectorViewModel.OnActiveTextFormattingApplied = textVm =>
        {
            if (ActiveInPlaceTextBox != null && ActiveInPlaceTextBox.DataContext == textVm)
            {
                ActiveInPlaceTextBox.Text = textVm.Text;
                ActiveInPlaceTextBox.SelectionStart = textVm.ActiveSelectionStart;
                ActiveInPlaceTextBox.SelectionEnd = textVm.ActiveSelectionStart + textVm.ActiveSelectionLength;
                ActiveInPlaceTextBox.Focus();
            }
        };
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
        // Support Cmd (Meta), Ctrl, and Option (Alt) modifier keys as well as Zoom tool mode
        bool isZoomModifier = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                              e.KeyModifiers.HasFlag(KeyModifiers.Meta) ||
                              e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        bool isZoomTool = ViewModel?.ActiveToolMode == Models.ToolMode.Zoom;

        if ((isZoomModifier || isZoomTool) && ViewModel != null && CanvasScrollViewer != null)
        {
            double oldZoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            double effectiveDelta = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;

            // Proportional smooth zoom supporting both discrete mouse wheel ticks and continuous trackpad pinch gestures
            double zoomDeltaFactor;
            if (Math.Abs(effectiveDelta) >= 1.0)
            {
                // Discrete mouse wheel tick (e.g. standard external wheel)
                zoomDeltaFactor = effectiveDelta > 0 ? 1.15 : (1.0 / 1.15);
            }
            else if (Math.Abs(effectiveDelta) > 0.0001)
            {
                // High-precision smooth trackpad pinch / scroll gesture
                zoomDeltaFactor = Math.Pow(1.002, effectiveDelta * 100);
            }
            else
            {
                zoomDeltaFactor = 1.0;
            }

            double newZoom = Math.Clamp(Math.Round(oldZoom * zoomDeltaFactor, 3), 0.1, 5.0);

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                var mouseInViewer = e.GetPosition(CanvasScrollViewer);
                double ratio = newZoom / oldZoom;
                double targetOffsetX = (CanvasScrollViewer.Offset.X + mouseInViewer.X) * ratio - mouseInViewer.X;
                double targetOffsetY = (CanvasScrollViewer.Offset.Y + mouseInViewer.Y) * ratio - mouseInViewer.Y;

                ViewModel.ZoomLevel = newZoom;
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

    private void OnCanvasTouchPadGestureMagnify(object? sender, PointerDeltaEventArgs e)
    {
        if (ViewModel != null && CanvasScrollViewer != null)
        {
            double oldZoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            double delta = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;
            if (delta == 0 && (e.Delta.X != 0 || e.Delta.Y != 0))
            {
                delta = e.Delta.X != 0 ? e.Delta.X : e.Delta.Y;
            }

            double zoomFactor = 1.0 + delta;
            double newZoom = Math.Clamp(Math.Round(oldZoom * zoomFactor, 3), 0.1, 5.0);

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                var mouseInViewer = e.GetPosition(CanvasScrollViewer);
                double ratio = newZoom / oldZoom;
                double targetOffsetX = (CanvasScrollViewer.Offset.X + mouseInViewer.X) * ratio - mouseInViewer.X;
                double targetOffsetY = (CanvasScrollViewer.Offset.Y + mouseInViewer.Y) * ratio - mouseInViewer.Y;

                ViewModel.ZoomLevel = newZoom;
                CanvasScrollViewer.Offset = new Vector(Math.Max(0, targetOffsetX), Math.Max(0, targetOffsetY));
            }

            UpdateViewportOnPlacementService();
            e.Handled = true;
        }
    }

    private void OnCanvasPinch(object? sender, PinchEventArgs e)
    {
        if (ViewModel != null && CanvasScrollViewer != null)
        {
            if (!_isPinching)
            {
                _isPinching = true;
                _pinchStartZoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            }

            // e.Scale is the total cumulative scale of the pinch gesture since start (starts at 1.0)
            double targetZoom = Math.Clamp(Math.Round(_pinchStartZoom * e.Scale, 3), 0.1, 5.0);
            if (Math.Abs(targetZoom - ViewModel.ZoomLevel) > 0.002)
            {
                double oldZ = ViewModel.ZoomLevel;
                ViewModel.ZoomLevel = targetZoom;

                if (oldZ > 0)
                {
                    var origin = e.ScaleOrigin;
                    double ratio = targetZoom / oldZ;
                    double newOffsetX = (CanvasScrollViewer.Offset.X + origin.X) * ratio - origin.X;
                    double newOffsetY = (CanvasScrollViewer.Offset.Y + origin.Y) * ratio - origin.Y;
                    CanvasScrollViewer.Offset = new Vector(Math.Max(0, newOffsetX), Math.Max(0, newOffsetY));
                }
            }
            e.Handled = true;
        }
    }

    private void OnCanvasPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        _isPinching = false;
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
            return;
        }
        else if (ViewModel.ActiveToolMode == Models.ToolMode.Zoom)
        {
            ViewModel.ResetZoom();
            e.Handled = true;
            return;
        }

        // Ignore double-taps originating inside overlays (CanvasTextHudView, FindReplaceBarView)
        if (e.Source is Visual sourceVisual && (sourceVisual.FindAncestorOfType<CanvasTextHudView>() != null || sourceVisual.FindAncestorOfType<FindReplaceBarView>() != null))
        {
            return;
        }

        // Direct in-place editing trigger on double-click
        if (ViewModel.CurrentPage == null || PageElementsCanvas == null) return;

        // 1. Check if the double-tap happened directly on an element visual or its ancestor
        ElementViewModelBase? targetElement = null;
        if (e.Source is Visual visual)
        {
            targetElement = (visual as Control)?.DataContext as ElementViewModelBase ??
                            visual.FindAncestorOfType<Control>()?.DataContext as ElementViewModelBase;
        }

        // 2. If not found from visual source, check geometric coordinates on the canvas
        if (targetElement == null)
        {
            var canvasPos = e.GetPosition(PageElementsCanvas);
            double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
            double docX = canvasPos.X / zoom;
            double docY = canvasPos.Y / zoom;

            targetElement = ViewModel.CurrentPage.Elements
                .Where(el => !el.IsLocked && docX >= el.X && docX <= el.X + el.Width && docY >= el.Y && docY <= el.Y + el.Height)
                .OrderByDescending(el => el.ZIndex)
                .FirstOrDefault();
        }

        if (targetElement == null)
        {
            targetElement = ViewModel.CurrentPage.SelectedElement;
        }

        if (targetElement != null && !targetElement.IsLocked)
        {
            ViewModel.CurrentPage.SelectElement(targetElement);

            if (targetElement is TextElementViewModel textVm)
            {
                textVm.IsInEditMode = true;
                e.Handled = true;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    FocusInPlaceTextBox(textVm);
                }, Avalonia.Threading.DispatcherPriority.Input);
            }
            else if (targetElement is ShapeElementViewModel shapeVm)
            {
                shapeVm.IsInEditMode = true;
                e.Handled = true;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    FocusInPlaceTextBox(shapeVm);
                }, Avalonia.Threading.DispatcherPriority.Input);
            }
            else if (targetElement is StickyNoteElementViewModel stickyVm)
            {
                stickyVm.IsInEditMode = true;
                e.Handled = true;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    FocusInPlaceTextBox(stickyVm);
                }, Avalonia.Threading.DispatcherPriority.Loaded);
            }
            else if (targetElement is MathElementViewModel mathVm)
            {
                mathVm.IsInEditMode = true;
                e.Handled = true;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    FocusInPlaceTextBox(mathVm);
                }, Avalonia.Threading.DispatcherPriority.Loaded);
            }
        }
    }

    private void OnCanvasBackgroundPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel?.CurrentPage == null || PageElementsCanvas == null) return;
        Focus();

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

        if (pointerPoint.Properties.IsLeftButtonPressed && e.Pointer.Type != PointerType.Touch)
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
            // If the clicked element is locked (e.g. background watermark or full-page frame),
            // check if there is an unlocked element under the cursor to select and edit instead.
            if (elementVm.IsLocked && PageElementsCanvas != null && ViewModel?.CurrentPage != null)
            {
                var canvasPos = e.GetPosition(PageElementsCanvas);
                double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
                double docX = canvasPos.X / zoom;
                double docY = canvasPos.Y / zoom;

                var topUnlocked = ViewModel.CurrentPage.Elements
                    .Where(el => !el.IsLocked && docX >= el.X && docX <= el.X + el.Width && docY >= el.Y && docY <= el.Y + el.Height)
                    .OrderByDescending(el => el.ZIndex)
                    .FirstOrDefault();

                if (topUnlocked != null)
                {
                    elementVm = topUnlocked;
                }
            }

            bool wasAlreadySelected = elementVm.IsSelected;
            bool isToggle = e.KeyModifiers.HasFlag(KeyModifiers.Shift) || e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);

            if (isToggle)
            {
                ViewModel?.CurrentPage?.ToggleElementSelection(elementVm);
            }
            else if (!elementVm.IsSelected)
            {
                ViewModel?.CurrentPage?.SelectElement(elementVm);
            }

            if (!elementVm.IsInEditMode)
            {
                Focus();
            }

            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed && PageElementsCanvas != null && ViewModel != null)
            {
                var pos = e.GetPosition(PageElementsCanvas);
                double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
                ViewModel.SmartPlacement.SetContextMenuPointer(pos.X / zoom, pos.Y / zoom);
            }

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && ViewModel?.CurrentPage != null && !elementVm.IsLocked)
            {
                if (elementVm.IsInEditMode)
                {
                    return;
                }

                _isPotentialDrag = true;
                _potentialDragElement = elementVm;
                _potentialDragStartPos = e.GetPosition(PageElementsCanvas);
                _wasAlreadySelectedOnPress = wasAlreadySelected;
                _lastPointerPosition = _potentialDragStartPos;
                e.Handled = true;
            }
        }
    }

    private void OnHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.Tag is string handleName && control.DataContext is ElementViewModelBase elementVm)
        {
            Focus();
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

    private void FocusInPlaceTextBox(ElementViewModelBase elementVm)
    {
        if (PageElementsCanvas == null) return;

        var textBox = PageElementsCanvas.GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(tb => tb.DataContext == elementVm && tb.IsVisible);

        if (textBox != null)
        {
            textBox.Focus();
            textBox.CaretIndex = textBox.Text?.Length ?? 0;
            textBox.SelectAll();
        }
    }

    public static TextBox? ActiveInPlaceTextBox { get; private set; }

    private static void UpdateSelectionFromTextBox(TextBox textBox)
    {
        if (textBox.DataContext is TextElementViewModel textVm)
        {
            int start = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
            int end = Math.Max(textBox.SelectionStart, textBox.SelectionEnd);
            int len = end - start;

            // If TextBox temporarily lost focus and reports 0 length while ViewModel already has a selection,
            // preserve the selection so toolbar/HUD commands can format it!
            if (!textBox.IsFocused && len == 0 && textVm.HasTextSelection)
            {
                return;
            }

            string text = textBox.Text ?? "";
            string sel = len > 0 && start + len <= text.Length
                ? text.Substring(start, len)
                : string.Empty;

            textVm.UpdateTextSelection(start, len, sel);
        }
    }

    private void OnInPlaceTextBoxAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.GotFocus += (s, args) =>
            {
                ActiveInPlaceTextBox = textBox;
                UpdateSelectionFromTextBox(textBox);
                if (textBox.DataContext is ElementViewModelBase el)
                {
                    _initialEditContents[el.Id] = textBox.Text ?? "";
                }
            };

            textBox.LostFocus += (s, args) =>
            {
                if (textBox.DataContext is TextElementViewModel textVm && textVm.IsInEditMode)
                {
                    // User is still in edit mode (e.g. interacting with HUD, Ribbon, or Sidebar).
                    // Keep ActiveInPlaceTextBox assigned so inline formatting commands continue working!
                    return;
                }

                if (ActiveInPlaceTextBox == textBox)
                {
                    ActiveInPlaceTextBox = null;
                }
            };

            textBox.PropertyChanged += (s, args) =>
            {
                if (args.Property == Visual.IsVisibleProperty && textBox.IsVisible)
                {
                    ActiveInPlaceTextBox = textBox;
                    if (textBox.DataContext is ElementViewModelBase el)
                    {
                        _initialEditContents[el.Id] = textBox.Text ?? "";
                    }

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        textBox.Focus();
                        textBox.CaretIndex = textBox.Text?.Length ?? 0;
                        textBox.SelectAll();
                        UpdateSelectionFromTextBox(textBox);
                    }, Avalonia.Threading.DispatcherPriority.Input);
                }
                else if (args.Property == TextBox.SelectionStartProperty || args.Property == TextBox.SelectionEndProperty || args.Property == TextBox.TextProperty)
                {
                    UpdateSelectionFromTextBox(textBox);
                }
            };

            textBox.PointerReleased += (s, args) =>
            {
                UpdateSelectionFromTextBox(textBox);
            };

            textBox.KeyUp += (s, args) =>
            {
                UpdateSelectionFromTextBox(textBox);
            };

            if (textBox.IsVisible)
            {
                ActiveInPlaceTextBox = textBox;
                if (textBox.DataContext is ElementViewModelBase el)
                {
                    _initialEditContents[el.Id] = textBox.Text ?? "";
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    textBox.Focus();
                    textBox.CaretIndex = textBox.Text?.Length ?? 0;
                    textBox.SelectAll();
                    UpdateSelectionFromTextBox(textBox);
                }, Avalonia.Threading.DispatcherPriority.Input);
            }
        }
    }

    private void OnTextElementDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is TextElementViewModel textVm)
        {
            if (textVm.IsLocked) return;

            if (textVm.Spans != null && textVm.Spans.Count > 0)
            {
                textVm.Text = textVm.GetMarkdownText();
            }

            textVm.IsInEditMode = true;
            e.Handled = true;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                FocusInPlaceTextBox(textVm);
            }, Avalonia.Threading.DispatcherPriority.Input);
        }
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.DataContext is TextElementViewModel textVm)
        {
            // If the element is still in edit mode (e.g. interacting with HUD, Ribbon, or Sidebar),
            // DO NOT exit edit mode or wipe spans!
            if (textVm.IsInEditMode)
            {
                return;
            }

            _initialEditContents.TryGetValue(textVm.Id, out var oldTxt);
            string finalPlain = textVm.Text ?? "";
            var finalSpans = textVm.Spans?.Select(s => s.Clone()).ToList();

            if (finalPlain != oldTxt)
            {
                _initialEditContents[textVm.Id] = finalPlain;
                var savedSpans = finalSpans;
                ViewModel?.UndoRedo.RecordAction(
                    "Edit Text",
                    () => {
                        textVm.Text = oldTxt ?? "";
                        textVm.Spans = null;
                    },
                    () => {
                        textVm.Text = finalPlain;
                        textVm.Spans = savedSpans != null ? savedSpans.Select(s => s.Clone()).ToList() : null;
                    }
                );
            }
        }
    }

    private void OnShapeElementDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is ShapeElementViewModel shapeVm)
        {
            shapeVm.IsInEditMode = true;
            e.Handled = true;

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                FocusInPlaceTextBox(shapeVm);
            }, Avalonia.Threading.DispatcherPriority.Input);
        }
    }

    private void OnShapeTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.DataContext is ShapeElementViewModel shapeVm)
        {
            string currentLabel = shapeVm.Label ?? "";
            if (_initialEditContents.TryGetValue(shapeVm.Id, out var oldTxt) && currentLabel != oldTxt)
            {
                _initialEditContents[shapeVm.Id] = currentLabel;
                ViewModel?.UndoRedo.RecordAction(
                    "Edit Shape Label",
                    () => shapeVm.Label = oldTxt,
                    () => shapeVm.Label = currentLabel
                );
            }
        }
    }

    private void OnTableCellLostFocus(object? sender, RoutedEventArgs e)
    {
        // Table cells have two-way binding directly to TableHeaderItem/TableCellItem
    }

    private void OnStickyNoteTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        // Sticky note text has two-way binding directly to NoteText
    }

    private void OnMathElementDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is MathElementViewModel mathVm)
        {
            ViewModel?.OpenMathStudioCommand.Execute(mathVm);
            e.Handled = true;
        }
    }

    private void OnMathTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.DataContext is MathElementViewModel mathVm)
        {
            if (_initialEditContents.TryGetValue(mathVm.Id, out var oldFormula) && mathVm.Formula != oldFormula)
            {
                string newFormula = mathVm.Formula;
                _initialEditContents[mathVm.Id] = newFormula;
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

        if (_isPotentialDrag && !_isDraggingElement && PageElementsCanvas != null && ViewModel?.CurrentPage != null)
        {
            var curPos = e.GetPosition(PageElementsCanvas);
            double distSq = Math.Pow(curPos.X - _potentialDragStartPos.X, 2) + Math.Pow(curPos.Y - _potentialDragStartPos.Y, 2);
            if (distSq > 9.0) // 3px drag threshold
            {
                _isDraggingElement = true;
                _draggedElement = _potentialDragElement;
                _draggedElements = ViewModel.CurrentPage.SelectedElements.Where(el => !el.IsLocked).ToList();
                if (_draggedElement != null && !_draggedElements.Contains(_draggedElement)) _draggedElements.Add(_draggedElement);

                if (_draggedElement != null && !string.IsNullOrEmpty(_draggedElement.GroupId))
                {
                    var groupMembers = ViewModel.CurrentPage.Elements.Where(el => el.GroupId == _draggedElement.GroupId && !el.IsLocked);
                    foreach (var gm in groupMembers)
                    {
                        if (!_draggedElements.Contains(gm)) _draggedElements.Add(gm);
                    }
                }

                _dragStartPositions = _draggedElements.Select(el => (Element: el, el.X, el.Y)).ToList();
                if (_draggedElement != null)
                {
                    _dragStartElementX = _draggedElement.X;
                    _dragStartElementY = _draggedElement.Y;
                }
                _lastPointerPosition = curPos;
                e.Pointer.Capture(this);
            }
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
        if (_isPotentialDrag && !_isDraggingElement && !_isResizingHandle && !_isMarqueeSelecting)
        {
            // Click without drag occurred!
            if (_wasAlreadySelectedOnPress && _potentialDragElement != null && !_potentialDragElement.IsInEditMode)
            {
                // Second click on already selected element enters in-place edit mode!
                if (_potentialDragElement is TextElementViewModel textVm)
                {
                    textVm.IsInEditMode = true;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => FocusInPlaceTextBox(textVm), Avalonia.Threading.DispatcherPriority.Input);
                }
                else if (_potentialDragElement is ShapeElementViewModel shapeVm)
                {
                    shapeVm.IsInEditMode = true;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => FocusInPlaceTextBox(shapeVm), Avalonia.Threading.DispatcherPriority.Input);
                }
            }
        }
        _isPotentialDrag = false;
        _potentialDragElement = null;

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

        if (e.Pointer.Captured == this)
        {
            e.Pointer.Capture(null);
        }
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

        // Check if user is actively typing inside any TextBox / TextPresenter
        var topLevel = TopLevel.GetTopLevel(this);
        var focused = topLevel?.FocusManager?.GetFocusedElement();
        bool isFocusedInTextBox = focused is TextBox ||
                                  focused is Avalonia.Controls.Presenters.TextPresenter ||
                                  (focused is Control ctrl && ctrl.FindAncestorOfType<TextBox>() != null);

        bool isSourceTextBox = e.Source is TextBox ||
                               e.Source is Avalonia.Controls.Presenters.TextPresenter ||
                               (e.Source is Control sCtrl && sCtrl.FindAncestorOfType<TextBox>() != null);

        bool isEditingText = isFocusedInTextBox || isSourceTextBox || (selected != null && selected.IsInEditMode);

        if (isEditingText)
        {
            if (e.Key == Key.Escape)
            {
                if (selected != null)
                {
                    selected.IsInEditMode = false;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta)))
            {
                if (selected != null)
                {
                    selected.IsInEditMode = false;
                }
                e.Handled = true;
            }
            // Allow all typing, backspace, delete, arrows, and shortcuts to flow to the TextBox
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
                    ViewModel.ResetZoom();
                    e.Handled = true;
                    break;
                case Key.D1:
                case Key.NumPad1:
                    ViewModel.FitToWidthDynamic(CanvasScrollViewer?.Viewport.Width ?? 800);
                    e.Handled = true;
                    break;
                case Key.D9:
                case Key.NumPad9:
                    ViewModel.FitToPageDynamic(CanvasScrollViewer?.Viewport.Width ?? 800, CanvasScrollViewer?.Viewport.Height ?? 800);
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
        else if (selected != null || (ViewModel.CurrentPage?.SelectedElements.Count ?? 0) > 0)
        {
            // If user starts typing any alphanumeric or punctuation character while a text element is selected,
            // immediately enter in-place edit mode and route focus to the TextBox!
            if (selected is TextElementViewModel textEl && !textEl.IsInEditMode && !isCtrlOrCmd && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                bool isPrintable = (e.Key >= Key.A && e.Key <= Key.Z) ||
                                   (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                                   (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                                   e.Key == Key.Space || e.Key == Key.Enter || e.Key == Key.F2 ||
                                   e.Key == Key.OemPeriod || e.Key == Key.OemComma || e.Key == Key.OemMinus || e.Key == Key.OemPlus;

                if (isPrintable)
                {
                    textEl.IsInEditMode = true;

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        FocusInPlaceTextBox(textEl);
                    }, Avalonia.Threading.DispatcherPriority.Input);

                    if (e.Key == Key.Enter || e.Key == Key.F2)
                    {
                        e.Handled = true;
                        return;
                    }
                    return;
                }
            }
            else if (selected is ShapeElementViewModel shapeEl && !shapeEl.IsInEditMode && !isCtrlOrCmd && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                if (e.Key == Key.Enter || e.Key == Key.F2)
                {
                    shapeEl.IsInEditMode = true;
                    e.Handled = true;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => FocusInPlaceTextBox(shapeEl), Avalonia.Threading.DispatcherPriority.Input);
                    return;
                }
            }

            switch (e.Key)
            {
                case Key.Enter:
                case Key.F2:
                    break;
                case Key.Delete:
                case Key.Back:
                    ViewModel.Inspector.DeleteSelectedElementCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    ViewModel.CurrentPage?.ClearSelection();
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down:
                    if (selected != null && ViewModel?.CurrentPage is { } page)
                    {
                        double oldX = selected.X;
                        double oldY = selected.Y;

                        if (e.Key == Key.Left) selected.X = Math.Max(0, selected.X - step);
                        else if (e.Key == Key.Right) selected.X = Math.Min(page.Width - selected.Width, selected.X + step);
                        else if (e.Key == Key.Up) selected.Y = Math.Max(0, selected.Y - step);
                        else if (e.Key == Key.Down) selected.Y = Math.Min(page.Height - selected.Height, selected.Y + step);

                        double newX = selected.X;
                        double newY = selected.Y;
                        var el = selected;
                        ViewModel.UndoRedo.RecordAction(
                            $"Nudge {el.DisplayName}",
                            () => { el.X = oldX; el.Y = oldY; },
                            () => { el.X = newX; el.Y = newY; }
                        );
                        e.Handled = true;
                    }
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
