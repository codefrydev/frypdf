using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Ribbon;

/// <summary>
/// Thread-safe in-memory registry for dynamic Ribbon tabs, groups, and action descriptors contributed by plugins.
/// </summary>
public class RibbonRegistry : IRibbonRegistry
{
    private readonly ConcurrentDictionary<string, RibbonTabDescriptor> _tabs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RibbonGroupDescriptor> _groups = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RibbonActionDescriptor> _actions = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public IDisposable RegisterTab(RibbonTabDescriptor tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        ArgumentException.ThrowIfNullOrWhiteSpace(tab.Id);

        _tabs[tab.Id] = tab;
        RegistryChanged?.Invoke();

        return new UnregisterDisposable(() =>
        {
            if (_tabs.TryRemove(tab.Id, out _))
            {
                RegistryChanged?.Invoke();
            }
        });
    }

    public IDisposable RegisterGroup(RibbonGroupDescriptor group)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentException.ThrowIfNullOrWhiteSpace(group.Id);

        _groups[group.Id] = group;
        RegistryChanged?.Invoke();

        return new UnregisterDisposable(() =>
        {
            if (_groups.TryRemove(group.Id, out _))
            {
                RegistryChanged?.Invoke();
            }
        });
    }

    public void RegisterAction(RibbonActionDescriptor action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(action.Id);

        _actions[action.Id] = action;
        RegistryChanged?.Invoke();
    }

    public bool UnregisterAction(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId)) return false;

        if (_actions.TryRemove(actionId, out _))
        {
            RegistryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public IReadOnlyList<RibbonTabDescriptor> GetAllTabs()
    {
        return _tabs.Values.OrderBy(t => t.Order).ThenBy(t => t.Title).ToList();
    }

    public IReadOnlyList<RibbonGroupDescriptor> GetGroupsForTab(string tabId)
    {
        if (string.IsNullOrWhiteSpace(tabId)) return Array.Empty<RibbonGroupDescriptor>();

        return _groups.Values
            .Where(g => string.Equals(g.TabId, tabId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(g => g.Order)
            .ThenBy(g => g.Title)
            .ToList();
    }

    public IReadOnlyList<RibbonActionDescriptor> GetActionsForTab(string tabId)
    {
        if (string.IsNullOrWhiteSpace(tabId)) return Array.Empty<RibbonActionDescriptor>();

        return _actions.Values
            .Where(a => string.Equals(a.TabId, tabId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Order)
            .ToList();
    }

    public IReadOnlyList<RibbonActionDescriptor> GetActionsForGroup(string tabId, string groupId)
    {
        if (string.IsNullOrWhiteSpace(tabId) || string.IsNullOrWhiteSpace(groupId))
            return Array.Empty<RibbonActionDescriptor>();

        return _actions.Values
            .Where(a => string.Equals(a.TabId, tabId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(a.GroupId, groupId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Order)
            .ToList();
    }

    public IReadOnlyList<RibbonActionDescriptor> GetAllActions()
    {
        return _actions.Values
            .OrderBy(a => a.TabId)
            .ThenBy(a => a.Order)
            .ToList();
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
