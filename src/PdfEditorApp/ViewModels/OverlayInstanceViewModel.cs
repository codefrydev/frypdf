using System;
using Avalonia.Threading;
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

    private IDisposable? _trackedDisposableViewModel;

    partial void OnContentChanged(object? oldValue, object? newValue)
    {
        if (oldValue is Avalonia.StyledElement oldElement)
        {
            oldElement.DataContextChanged -= OnElementDataContextChanged;
        }

        _trackedDisposableViewModel = null;

        if (newValue is Avalonia.StyledElement newElement)
        {
            newElement.DataContextChanged += OnElementDataContextChanged;
            try
            {
                _trackedDisposableViewModel = newElement.DataContext as IDisposable;
            }
            catch (InvalidOperationException)
            {
                // Called from a thread that does not own newElement
            }
        }
    }

    private void OnElementDataContextChanged(object? sender, EventArgs e)
    {
        if (sender is Avalonia.StyledElement el)
        {
            try
            {
                _trackedDisposableViewModel = el.DataContext as IDisposable;
            }
            catch (InvalidOperationException)
            {
            }
        }
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

        var content = Content;

        // If we haven't captured the VM yet, try to read it now if this thread owns the element
        if (_trackedDisposableViewModel == null && content is Avalonia.StyledElement element)
        {
            try
            {
                _trackedDisposableViewModel = element.DataContext as IDisposable;
            }
            catch (InvalidOperationException)
            {
                // Different thread owns element
            }
        }

        IDisposable? vmToDispose = _trackedDisposableViewModel;
        _trackedDisposableViewModel = null;

        if (vmToDispose != null)
        {
            TryDispose(vmToDispose);
        }

        if (content is Avalonia.StyledElement styledElement)
        {
            styledElement.DataContextChanged -= OnElementDataContextChanged;

            bool cleanedUpInline = false;
            try
            {
                var liveVm = styledElement.DataContext as IDisposable;
                styledElement.DataContext = null;
                if (liveVm != null && !ReferenceEquals(liveVm, vmToDispose))
                {
                    TryDispose(liveVm);
                }

                if (styledElement is IDisposable dispElem)
                {
                    TryDispose(dispElem);
                }

                Content = null;
                cleanedUpInline = true;
            }
            catch (InvalidOperationException)
            {
                // Different thread owns styledElement
            }

            if (!cleanedUpInline)
            {
                try
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            var liveVm = styledElement.DataContext as IDisposable;
                            styledElement.DataContext = null;
                            if (liveVm != null && !ReferenceEquals(liveVm, vmToDispose))
                            {
                                TryDispose(liveVm);
                            }

                            if (styledElement is IDisposable dispElem)
                            {
                                TryDispose(dispElem);
                            }

                            Content = null;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[OverlayInstance] Off-thread Dispose UI cleanup error: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OverlayInstance] Could not post UI cleanup: {ex.Message}");
                }
            }
        }
        else if (content is IDisposable disposableContent)
        {
            TryDispose(disposableContent);
            Content = null;
        }
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
            if (Dispatcher.UIThread.CheckAccess())
            {
                var disposableVm = element.DataContext as IDisposable;
                element.DataContext = null;

                if (disposableVm != null)
                {
                    TryDispose(disposableVm);
                }

                if (element is IDisposable disposableElement)
                {
                    TryDispose(disposableElement);
                }
            }
            else
            {
                // Off-UI thread. We cannot access element.DataContext directly without throwing
                // an InvalidOperationException from AvaloniaObject.VerifyAccess().
                try
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            var disposableVm = element.DataContext as IDisposable;
                            element.DataContext = null;

                            if (disposableVm != null)
                            {
                                TryDispose(disposableVm);
                            }

                            if (element is IDisposable disposableElement)
                            {
                                TryDispose(disposableElement);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[OverlayInstance] Off-thread DisposeContent post error: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OverlayInstance] Could not post DisposeContent to Dispatcher: {ex.Message}");
                }
            }
        }
        else if (content is IDisposable disposableContent)
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
