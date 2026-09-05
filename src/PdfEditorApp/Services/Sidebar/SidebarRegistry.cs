using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Sidebar;

/// <summary>
/// Thread-safe registry for document editor left/right sidebar tabs.
/// </summary>
public sealed class SidebarRegistry : ISidebarRegistry
{
    private readonly ConcurrentDictionary<string, SidebarTabDescriptor> _tabs = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public IDisposable RegisterTab(SidebarTabDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _tabs[descriptor.Id] = descriptor;
        RegistryChanged?.Invoke();

        return new UnregisterDisposable(() =>
        {
            UnregisterTab(descriptor.Id);
        });
    }

    public bool UnregisterTab(string tabId)
    {
        if (_tabs.TryRemove(tabId, out _))
        {
            RegistryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public SidebarTabDescriptor? GetTab(string tabId)
    {
        _tabs.TryGetValue(tabId, out var tab);
        return tab;
    }

    public IReadOnlyList<SidebarTabDescriptor> GetAllTabs()
    {
        return _tabs.Values.OrderBy(t => t.Order).ThenBy(t => t.Title).ToList();
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
            var act = System.Threading.Interlocked.Exchange(ref _action, null);
            act?.Invoke();
        }
    }
}
