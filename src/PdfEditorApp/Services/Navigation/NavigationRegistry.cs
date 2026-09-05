using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Navigation;

/// <summary>
/// Thread-safe registry for workspace pages and navigation sections.
/// </summary>
public sealed class NavigationRegistry : INavigationRegistry
{
    private readonly ConcurrentDictionary<string, NavigationItemDescriptor> _items = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public IDisposable RegisterNavigationItem(NavigationItemDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _items[descriptor.Id] = descriptor;
        RegistryChanged?.Invoke();

        return new UnregisterDisposable(() =>
        {
            UnregisterNavigationItem(descriptor.Id);
        });
    }

    public bool UnregisterNavigationItem(string itemId)
    {
        if (_items.TryRemove(itemId, out _))
        {
            RegistryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public IReadOnlyList<NavigationItemDescriptor> GetAllItems()
    {
        return _items.Values.OrderBy(i => i.Order).ThenBy(i => i.Title).ToList();
    }

    public IReadOnlyList<NavigationItemDescriptor> GetItemsByGroup(string group)
    {
        return _items.Values
            .Where(i => string.Equals(i.Group, group, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Order)
            .ThenBy(i => i.Title)
            .ToList();
    }

    public NavigationItemDescriptor? GetItem(string itemId)
    {
        _items.TryGetValue(itemId, out var item);
        return item;
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
