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

    public DocumentCanvasView()
    {
        InitializeComponent();

        AddHandler(PointerMovedEvent, OnGlobalPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnGlobalPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnCanvasKeyDown, RoutingStrategies.Tunnel);
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnCanvasBackgroundPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // If clicking directly on the canvas background, deselect all elements
        if (e.Source is ScrollViewer || (e.Source is Border b && b.Name == null && b.Child is Grid))
        {
            ViewModel?.CurrentPage?.ClearSelection();
        }
    }

    private void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is ElementViewModelBase elementVm)
        {
            ViewModel?.CurrentPage?.SelectElement(elementVm);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDraggingElement = true;
                _draggedElement = elementVm;
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
            _lastPointerPosition = e.GetPosition(PageElementsCanvas);
            e.Handled = true;
        }
    }

    private void OnTextElementDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control control && control.DataContext is TextElementViewModel textVm)
        {
            textVm.IsInEditMode = true;
            e.Handled = true;
        }
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.DataContext is TextElementViewModel textVm)
        {
            textVm.IsInEditMode = false;
        }
    }

    private void OnGlobalPointerMoved(object? sender, PointerEventArgs e)
    {
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
        _isDraggingElement = false;
        _isResizingHandle = false;
        _activeResizeHandle = null;
        _draggedElement = null;
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
                    selected.X = Math.Max(0, selected.X - step);
                    e.Handled = true;
                    break;
                case Key.Right:
                    selected.X = Math.Min(ViewModel.CurrentPage.Width - selected.Width, selected.X + step);
                    e.Handled = true;
                    break;
                case Key.Up:
                    selected.Y = Math.Max(0, selected.Y - step);
                    e.Handled = true;
                    break;
                case Key.Down:
                    selected.Y = Math.Min(ViewModel.CurrentPage.Height - selected.Height, selected.Y + step);
                    e.Handled = true;
                    break;
            }
        }
    }
}
