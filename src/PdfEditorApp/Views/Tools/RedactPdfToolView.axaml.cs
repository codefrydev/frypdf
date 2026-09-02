using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PdfEditorApp.ViewModels.Tools;

namespace PdfEditorApp.Views.Tools;

public partial class RedactPdfToolView : UserControl
{
    private readonly Border? _liveSelectionBox;
    private bool _isDragging;
    private bool _dragForceDrawBox;
    private Point _dragStart;

    public RedactPdfToolView()
    {
        InitializeComponent();
        _liveSelectionBox = this.FindControl<Border>("LiveSelectionBox");
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not InputElement element) return;
        if (!e.GetCurrentPoint(element).Properties.IsLeftButtonPressed) return;

        _isDragging = true;
        // Captured once at drag start: holding Alt (Option on macOS) switches this one
        // drag to raw-rectangle "draw a box" mode instead of the default text-selection
        // snapping — matching how modifier-driven mode switches work in the PDF Reader.
        _dragForceDrawBox = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        _dragStart = e.GetPosition(element);
        e.Pointer.Capture(element);

        if (_liveSelectionBox != null)
        {
            Canvas.SetLeft(_liveSelectionBox, _dragStart.X);
            Canvas.SetTop(_liveSelectionBox, _dragStart.Y);
            _liveSelectionBox.Width = 0;
            _liveSelectionBox.Height = 0;
            _liveSelectionBox.IsVisible = true;
        }
    }

    private void OnPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || sender is not InputElement element || _liveSelectionBox == null) return;

        var pos = e.GetPosition(element);
        double x = System.Math.Min(pos.X, _dragStart.X);
        double y = System.Math.Min(pos.Y, _dragStart.Y);
        double w = System.Math.Abs(pos.X - _dragStart.X);
        double h = System.Math.Abs(pos.Y - _dragStart.Y);

        Canvas.SetLeft(_liveSelectionBox, x);
        Canvas.SetTop(_liveSelectionBox, y);
        _liveSelectionBox.Width = w;
        _liveSelectionBox.Height = h;
    }

    private void OnPreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        e.Pointer.Capture(null);

        if (_liveSelectionBox == null) return;
        _liveSelectionBox.IsVisible = false;

        if (DataContext is RedactPdfToolViewModel vm && _liveSelectionBox.Width >= 2 && _liveSelectionBox.Height >= 2)
        {
            var rect = new Rect(
                Canvas.GetLeft(_liveSelectionBox),
                Canvas.GetTop(_liveSelectionBox),
                _liveSelectionBox.Width,
                _liveSelectionBox.Height);
            vm.AddManualMark(rect, _dragForceDrawBox);
        }
    }

    private void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        bool zoomModifier = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (!zoomModifier || DataContext is not RedactPdfToolViewModel vm) return;

        if (e.Delta.Y > 0) vm.Preview.ZoomInCommand.Execute(null);
        else if (e.Delta.Y < 0) vm.Preview.ZoomOutCommand.Execute(null);
        e.Handled = true;
    }
}
