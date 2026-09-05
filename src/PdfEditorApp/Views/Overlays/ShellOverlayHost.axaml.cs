using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
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
