using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel representing an active, floating, draggable overlay instance in the 'shell.overlay' slot.
/// Supports 60+ FPS dragging, minimizing to a pill, and clean dismissal.
/// </summary>
public partial class OverlayInstanceViewModel : ObservableObject
{
    private readonly Action<OverlayInstanceViewModel>? _onClose;

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _iconKind = "WindowRestore";

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _width = 340;

    [ObservableProperty]
    private double _height = 420;

    [ObservableProperty]
    private int _zIndex = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStandardChrome))]
    [NotifyPropertyChangedFor(nameof(HasCustomChrome))]
    [NotifyPropertyChangedFor(nameof(HasFloatingPill))]
    private OverlayChromeMode _chromeMode = OverlayChromeMode.StandardCard;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private bool _isMinimized;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private object? _content;

    public bool HasStandardChrome => ChromeMode == OverlayChromeMode.StandardCard;
    public bool HasCustomChrome => ChromeMode == OverlayChromeMode.CustomChrome;
    public bool HasFloatingPill => ChromeMode == OverlayChromeMode.FloatingPill;

    public OverlayDescriptor Descriptor { get; }

    public OverlayInstanceViewModel(OverlayDescriptor descriptor, Action<OverlayInstanceViewModel>? onClose = null)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _id = descriptor.Id;
        _title = descriptor.Title;
        _iconKind = descriptor.IconKind;
        _width = descriptor.DefaultWidth;
        _height = descriptor.DefaultHeight;
        _chromeMode = descriptor.ChromeMode;
        _onClose = onClose;
    }

    [RelayCommand]
    public void ToggleMinimize()
    {
        IsMinimized = !IsMinimized;
    }

    [RelayCommand]
    public void TogglePin()
    {
        IsPinned = !IsPinned;
    }

    [RelayCommand]
    public void Close()
    {
        IsVisible = false;
        _onClose?.Invoke(this);
    }

    public void BringToFront(System.Collections.Generic.IEnumerable<OverlayInstanceViewModel> allOverlays)
    {
        int max = 0;
        foreach (var o in allOverlays)
        {
            if (o.ZIndex > max) max = o.ZIndex;
        }
        ZIndex = max + 1;
    }
}
