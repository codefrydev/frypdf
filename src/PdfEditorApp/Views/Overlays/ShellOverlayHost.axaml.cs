using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views.Overlays;

public partial class ShellOverlayHost : UserControl
{
    private MainViewModel? _observedVm;

    public ShellOverlayHost()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_observedVm != null)
        {
            _observedVm.ActiveOverlays.CollectionChanged -= OnActiveOverlaysChanged;
        }

        _observedVm = DataContext as MainViewModel;

        if (_observedVm != null)
        {
            _observedVm.ActiveOverlays.CollectionChanged += OnActiveOverlaysChanged;
            ClampAndPositionOverlays();
        }
    }

    private void OnActiveOverlaysChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ClampAndPositionOverlays();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ClampAndPositionOverlays();
    }

    private Point _dragStartPoint;
    private bool _isDraggingStandardHeader;

    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is OverlayInstanceViewModel vm && _observedVm != null)
        {
            vm.BringToFront(_observedVm.ActiveOverlays);
        }
    }

    private void OnStandardHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDraggingStandardHeader = true;
            var root = this.VisualRoot as Visual ?? this;
            _dragStartPoint = e.GetPosition(root);
            e.Pointer.Capture(sender as Control);

            if (sender is Control ctrl && ctrl.DataContext is OverlayInstanceViewModel vm && _observedVm != null)
            {
                vm.BringToFront(_observedVm.ActiveOverlays);
            }
            e.Handled = true;
        }
    }

    private void OnStandardHeaderPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDraggingStandardHeader && sender is Control ctrl && ctrl.DataContext is OverlayInstanceViewModel vm)
        {
            var root = this.VisualRoot as Visual ?? this;
            var currentPoint = e.GetPosition(root);
            var deltaX = currentPoint.X - _dragStartPoint.X;
            var deltaY = currentPoint.Y - _dragStartPoint.Y;
            _dragStartPoint = currentPoint;

            vm.X = Math.Max(0, vm.X + deltaX);
            vm.Y = Math.Max(0, vm.Y + deltaY);
            e.Handled = true;
        }
    }

    private void OnStandardHeaderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDraggingStandardHeader)
        {
            _isDraggingStandardHeader = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void ClampAndPositionOverlays()
    {
        if (_observedVm?.ActiveOverlays == null) return;
        if (Bounds.Width < 200 || Bounds.Height < 200) return;

        foreach (var overlay in _observedVm.ActiveOverlays)
        {
            // If overlay is unpositioned or placed off-screen, dock it near the top-right / middle-right
            if (overlay.X <= 0 || overlay.X + overlay.Width > Bounds.Width)
            {
                overlay.X = Math.Max(20, Bounds.Width - overlay.Width - 36);
            }

            if (overlay.Y <= 0 || overlay.Y + overlay.Height > Bounds.Height)
            {
                overlay.Y = Math.Max(60, Math.Min(100, Bounds.Height - overlay.Height - 60));
            }
        }
    }
}
