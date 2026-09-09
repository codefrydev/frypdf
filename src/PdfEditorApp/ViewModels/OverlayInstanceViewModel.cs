using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel representing an active, floating, draggable overlay instance in the 'shell.overlay' slot.
/// Supports 60+ FPS dragging, minimizing to a pill, and clean dismissal.
/// </summary>
public partial class OverlayInstanceViewModel : ObservableObject, IDisposable
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
    [NotifyPropertyChangedFor(nameof(EffectiveHeight))]
    private double _height = 420;

    [ObservableProperty]
    private int _zIndex = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanResize))]
    private bool _isResizable = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStandardChrome))]
    [NotifyPropertyChangedFor(nameof(HasCustomChrome))]
    [NotifyPropertyChangedFor(nameof(HasFloatingPill))]
    [NotifyPropertyChangedFor(nameof(CustomChromeContent))]
    [NotifyPropertyChangedFor(nameof(StandardChromeContent))]
    private OverlayChromeMode _chromeMode = OverlayChromeMode.StandardCard;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveHeight))]
    [NotifyPropertyChangedFor(nameof(EffectiveMinHeight))]
    [NotifyPropertyChangedFor(nameof(CanResize))]
    private bool _isMinimized;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomChromeContent))]
    [NotifyPropertyChangedFor(nameof(StandardChromeContent))]
    private object? _content;

    public bool HasStandardChrome => ChromeMode == OverlayChromeMode.StandardCard;
    public bool HasCustomChrome => ChromeMode == OverlayChromeMode.CustomChrome;
    public bool HasFloatingPill => ChromeMode == OverlayChromeMode.FloatingPill;

    public double EffectiveHeight => IsMinimized ? double.NaN : Height;
    public double EffectiveMinHeight => IsMinimized ? 42 : Descriptor.MinHeight;
    public bool CanResize => IsResizable && !IsMinimized;

    public object? CustomChromeContent => HasCustomChrome ? Content : null;
    public object? StandardChromeContent => HasStandardChrome ? Content : null;

    public OverlayDescriptor Descriptor { get; }

    public OverlayInstanceViewModel(OverlayDescriptor descriptor, Action<OverlayInstanceViewModel>? onClose = null)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _id = descriptor.Id;
        _title = descriptor.Title;
        _iconKind = descriptor.IconKind;
        _width = descriptor.DefaultWidth;
        _height = descriptor.DefaultHeight;
        _isResizable = descriptor.IsResizable;
        _chromeMode = descriptor.ChromeMode;
        _onClose = onClose;
    }

    public void Resize(double newWidth, double newHeight, double maxCanvasWidth = double.PositiveInfinity, double maxCanvasHeight = double.PositiveInfinity)
    {
        var minW = Math.Max(120, Descriptor.MinWidth);
        var minH = Math.Max(80, Descriptor.MinHeight);
        var maxW = Math.Min(Descriptor.MaxWidth, maxCanvasWidth);
        var maxH = Math.Min(Descriptor.MaxHeight, maxCanvasHeight);

        Width = Math.Clamp(newWidth, minW, Math.Max(minW, maxW));
        Height = Math.Clamp(newHeight, minH, Math.Max(minH, maxH));
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

    /// <summary>
    /// Tears down the hosted plugin view and its view model.
    /// </summary>
    /// <remarks>
    /// Nothing used to dispose an overlay instance, so a plugin holding unmanaged or
    /// long-lived resources kept them for the life of the process. The music player is the
    /// worst case: each instance owns a native audio engine, an open playback device, and
    /// DispatcherTimers that root the view model, so an undisposed instance leaks an audio
    /// callback thread that keeps competing for CPU and disk.
    ///
    /// The plugin's resources hang off the view's DataContext, not the view, so both are
    /// checked — a plugin may make either one disposable.
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        DisposeContent(Content);
        Content = null;
    }

    private bool _isDisposed;

    /// <summary>
    /// Disposes a hosted content object and, when it is a control, its view model.
    /// </summary>
    internal static void DisposeContent(object? content)
    {
        if (content == null) return;

        // The view model owns the plugin's resources, so tear it down before the view that
        // binds to it.
        if (content is Avalonia.StyledElement element)
        {
            if (element.DataContext is IDisposable disposableVm)
            {
                TryDispose(disposableVm);
            }

            element.DataContext = null;
        }

        if (content is IDisposable disposableContent)
        {
            TryDispose(disposableContent);
        }
    }

    /// <summary>
    /// Disposes third-party plugin code, which must not be able to abort host teardown.
    /// </summary>
    private static void TryDispose(IDisposable target)
    {
        try
        {
            target.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[OverlayInstance] Dispose threw for {target.GetType().FullName}: {ex.Message}");
        }
    }
}
