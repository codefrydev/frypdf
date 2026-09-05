using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Services.Overlays;

/// <summary>
/// Thread-safe registry and lifecycle manager for floating plugins targeting the 'shell.overlay' slot.
/// </summary>
public sealed class OverlayRegistry : IOverlayRegistry
{
    private readonly ConcurrentDictionary<string, OverlayDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, OverlayInstanceViewModel> _activeInstances = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceProvider _serviceProvider;

    public event Action? RegistryChanged;
    public event Action? ActiveOverlaysChanged;

    public ObservableCollection<OverlayInstanceViewModel> ActiveOverlays { get; } = new();

    public OverlayRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IDisposable RegisterOverlay(OverlayDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _descriptors[descriptor.Id] = descriptor;

        RegistryChanged?.Invoke();

        return new UnregisterDisposable(() =>
        {
            UnregisterOverlay(descriptor.Id);
        });
    }

    public bool UnregisterOverlay(string overlayId)
    {
        if (_descriptors.TryRemove(overlayId, out _))
        {
            HideOverlay(overlayId);
            RegistryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public OverlayDescriptor? GetOverlay(string overlayId)
    {
        if (_descriptors.TryGetValue(overlayId, out var descriptor))
        {
            return descriptor;
        }

        if (!overlayId.StartsWith("frypdf.overlay.", StringComparison.OrdinalIgnoreCase))
        {
            if (_descriptors.TryGetValue($"frypdf.overlay.{overlayId}", out descriptor))
            {
                return descriptor;
            }
        }

        return null;
    }

    public IReadOnlyList<OverlayDescriptor> GetAllOverlays()
    {
        return _descriptors.Values.OrderBy(d => d.Title).ToList();
    }

    public void ShowOverlay(string overlayId)
    {
        var desc = GetOverlay(overlayId);
        if (desc == null) return;

        if (_activeInstances.TryGetValue(desc.Id, out var existingInstance))
        {
            RunOnUIThread(() =>
            {
                existingInstance.IsVisible = true;
                existingInstance.IsMinimized = false;
                if (!ActiveOverlays.Contains(existingInstance))
                {
                    ActiveOverlays.Add(existingInstance);
                }
                ActiveOverlaysChanged?.Invoke();
            });
            return;
        }

        object? content = null;
        if (desc.ViewFactory != null)
        {
            try
            {
                content = desc.ViewFactory(_serviceProvider);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OverlayRegistry] Error creating view for {desc.Id}: {ex.Message}");
            }
        }
        else if (desc.ViewType != null)
        {
            try
            {
                content = Activator.CreateInstance(desc.ViewType);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OverlayRegistry] Error instantiating ViewType {desc.ViewType}: {ex.Message}");
            }
        }

        var instance = new OverlayInstanceViewModel(desc, inst =>
        {
            HideOverlay(inst.Id);
        })
        {
            Content = content,
            X = desc.InitialX ?? 0,
            Y = desc.InitialY ?? 0,
            IsVisible = true,
            IsMinimized = false
        };

        if (_activeInstances.TryAdd(desc.Id, instance))
        {
            RunOnUIThread(() =>
            {
                if (!ActiveOverlays.Contains(instance))
                {
                    ActiveOverlays.Add(instance);
                }
                ActiveOverlaysChanged?.Invoke();
            });
        }
    }

    public void HideOverlay(string overlayId)
    {
        var desc = GetOverlay(overlayId);
        var targetId = desc?.Id ?? overlayId;

        if (_activeInstances.TryRemove(targetId, out var instance))
        {
            RunOnUIThread(() =>
            {
                instance.IsVisible = false;
                ActiveOverlays.Remove(instance);
                ActiveOverlaysChanged?.Invoke();
            });
        }
    }

    private static void RunOnUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
        {
            Dispatcher.UIThread.Post(action);
        }
        else
        {
            action();
        }
    }

    public void ToggleOverlay(string overlayId)
    {
        if (IsOverlayVisible(overlayId))
        {
            HideOverlay(overlayId);
        }
        else
        {
            ShowOverlay(overlayId);
        }
    }

    public bool IsOverlayVisible(string overlayId)
    {
        var desc = GetOverlay(overlayId);
        var targetId = desc?.Id ?? overlayId;
        return _activeInstances.TryGetValue(targetId, out var inst) && inst.IsVisible;
    }

    private sealed class UnregisterDisposable : IDisposable
    {
        private Action? _action;

        public UnregisterDisposable(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            var action = System.Threading.Interlocked.Exchange(ref _action, null);
            action?.Invoke();
        }
    }
}
