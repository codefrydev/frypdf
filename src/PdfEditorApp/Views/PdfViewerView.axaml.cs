using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private PdfViewerViewModel? ViewModel => DataContext as PdfViewerViewModel;

    private ScrollViewer? ActiveScrollViewer
    {
        get
        {
            if (ViewModel == null) return null;
            if (ViewModel.IsContinuousScroll) return this.FindControl<ScrollViewer>("ContinuousScrollViewer");
            if (ViewModel.IsSinglePageMode) return this.FindControl<ScrollViewer>("SinglePageScrollViewer");
            if (ViewModel.IsTwoPageSpreadMode) return this.FindControl<ScrollViewer>("TwoPageSpreadScrollViewer");
            return null;
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
                case Key.Escape:
                    if (ViewModel.HasTextSelection)
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
