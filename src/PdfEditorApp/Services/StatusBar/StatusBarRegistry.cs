using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.StatusBar;

public class StatusBarRegistry : IStatusBarRegistry
{
    private readonly ConcurrentDictionary<string, StatusBarWidgetDescriptor> _widgets = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public StatusBarRegistry(bool seedDefaults = true)
    {
        if (seedDefaults)
        {
            RegisterBuiltInWidgets();
        }
    }

    private void RegisterBuiltInWidgets()
    {
        RegisterWidget(new StatusBarWidgetDescriptor
        {
            WidgetId = "frypdf.status.active_indicator",
            Alignment = StatusBarAlignment.Left,
            Order = 10,
            ToolTip = "System Operational & Ready",
            Factory = sp => new ViewModels.StatusBarWidgetViewModel
            {
                WidgetId = "frypdf.status.active_indicator",
                Label = "Ready",
                IconKind = "CheckCircleOutline",
                ToolTip = "System Operational & Ready",
                IsActive = true
            }
        });

        RegisterWidget(new StatusBarWidgetDescriptor
        {
            WidgetId = "frypdf.status.memory_monitor",
            Alignment = StatusBarAlignment.Right,
            Order = 90,
            ToolTip = "SkiaSharp High-Performance Graphics Acceleration",
            Factory = sp => new ViewModels.StatusBarWidgetViewModel
            {
                WidgetId = "frypdf.status.memory_monitor",
                Label = "Skia 64-bit",
                IconKind = "Memory",
                ToolTip = "SkiaSharp High-Performance Graphics Acceleration",
                IsActive = true
            }
        });
    }

    public IDisposable RegisterWidget(StatusBarWidgetDescriptor widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        _widgets[widget.WidgetId] = widget;
        RegistryChanged?.Invoke();

        return new DisposableAction(() =>
        {
            _widgets.TryRemove(widget.WidgetId, out _);
            RegistryChanged?.Invoke();
        });
    }

    public IReadOnlyList<StatusBarWidgetDescriptor> GetWidgets(StatusBarAlignment alignment)
    {
        return _widgets.Values
            .Where(w => w.Alignment == alignment)
            .OrderBy(w => w.Order)
            .ToList();
    }

    public IReadOnlyList<StatusBarWidgetDescriptor> GetAllWidgets()
    {
        return _widgets.Values.OrderBy(w => w.Order).ToList();
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        private Action? _action = action;
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }
}
