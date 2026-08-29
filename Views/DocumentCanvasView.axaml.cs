using System;
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
    private string? _activeResizeHandle;
    private Point _lastPointerPosition;
    private ElementViewModelBase? _draggedElement;

    // Movement & Resize Undo Tracking
    private double _dragStartElementX;
    private double _dragStartElementY;
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

        AddHandler(PointerMovedEvent, OnGlobalPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnGlobalPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent, OnCanvasPointerWheelChanged, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnCanvasKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnCanvasKeyUp, RoutingStrategies.Tunnel);
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        bool isCtrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (isCtrlOrCmd && ViewModel != null)
        {
            if (e.Delta.Y > 0)
            {
                ViewModel.ZoomInCommand.Execute(null);
            }
            else if (e.Delta.Y < 0)
            {
                ViewModel.ZoomOutCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    private void OnCanvasBackgroundPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel?.CurrentPage == null || PageElementsCanvas == null) return;

        var pointerPoint = e.GetCurrentPoint(this);

        // Middle button click or Spacebar held => Pan mode
        if (_isSpacePressed || pointerPoint.Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            if (CanvasScrollViewer != null)
            {
                _scrollStart = new Vector(CanvasScrollViewer.Offset.X, CanvasScrollViewer.Offset.Y);
            }
            e.Handled = true;
            return;
        }

        var pos = e.GetPosition(PageElementsCanvas);
        double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
        double canvasX = pos.X / zoom;
        double canvasY = pos.Y / zoom;

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
            e.Handled = true;
            return;
        }

        // If clicking directly on the canvas background in select mode, deselect all elements
        if (e.Source is ScrollViewer || (e.Source is Border b && b.Name == null && b.Child is Grid))
        {
            ViewModel?.CurrentPage?.ClearSelection();
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
            e.Handled = true;
            return;
        }

        if (sender is Control control && control.DataContext is ElementViewModelBase elementVm)
        {
            ViewModel?.CurrentPage?.SelectElement(elementVm);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDraggingElement = true;
                _draggedElement = elementVm;
                _dragStartElementX = elementVm.X;
                _dragStartElementY = elementVm.Y;
                _lastPointerPosition = e.GetPosition(PageElementsCanvas);
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

        if (PageElementsCanvas == null || _draggedElement == null || ViewModel?.CurrentPage == null) return;

        var currentPos = e.GetPosition(PageElementsCanvas);
        double deltaX = currentPos.X - _lastPointerPosition.X;
        double deltaY = currentPos.Y - _lastPointerPosition.Y;

        double zoom = ViewModel.ZoomLevel > 0 ? ViewModel.ZoomLevel : 1.0;
        deltaX /= zoom;
        deltaY /= zoom;

        if (_isResizingHandle && !string.IsNullOrEmpty(_activeResizeHandle))
        {
            _draggedElement.Resize(_activeResizeHandle, deltaX, deltaY);
            _lastPointerPosition = currentPos;
        }
        else if (_isDraggingElement)
        {
            _draggedElement.MoveBy(deltaX, deltaY, ViewModel.CurrentPage.Width, ViewModel.CurrentPage.Height);
            _lastPointerPosition = currentPos;
        }
    }

    private void OnGlobalPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizingHandle && _draggedElement != null)
        {
            var el = _draggedElement;
            double fromX = _resizeStartX, fromY = _resizeStartY, fromW = _resizeStartW, fromH = _resizeStartH;
            double toX = el.X, toY = el.Y, toW = el.Width, toH = el.Height;
            if (Math.Abs(toW - fromW) > 0.5 || Math.Abs(toH - fromH) > 0.5 || Math.Abs(toX - fromX) > 0.5 || Math.Abs(toY - fromY) > 0.5)
            {
                ViewModel?.UndoRedo.RecordAction(
                    $"Resize {el.DisplayName}",
                    () => { el.X = fromX; el.Y = fromY; el.Width = fromW; el.Height = fromH; },
                    () => { el.X = toX; el.Y = toY; el.Width = toW; el.Height = toH; }
                );
            }
        }
        else if (_isDraggingElement && _draggedElement != null)
        {
            var el = _draggedElement;
            double fromX = _dragStartElementX, fromY = _dragStartElementY;
            double toX = el.X, toY = el.Y;
            if (Math.Abs(toX - fromX) > 0.5 || Math.Abs(toY - fromY) > 0.5)
            {
                ViewModel?.UndoRedo.RecordAction(
                    $"Move {el.DisplayName}",
                    () => { el.X = fromX; el.Y = fromY; },
                    () => { el.X = toX; el.Y = toY; }
                );
            }
        }

        _isDraggingElement = false;
        _isResizingHandle = false;
        _activeResizeHandle = null;
        _draggedElement = null;
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
                case Key.T:
                    ViewModel.AddTextElementCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.R:
                    ViewModel.AddShapeElementCommand.Execute("Rectangle");
                    e.Handled = true;
                    break;
                case Key.H:
                    ViewModel.AddInkElementCommand.Execute(true);
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
