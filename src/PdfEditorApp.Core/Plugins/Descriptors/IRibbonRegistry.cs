using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Descriptor representing a Ribbon Tab contributed dynamically by a plugin.
/// </summary>
public sealed class RibbonTabDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public int Order { get; init; } = 100;
    public bool IsDynamic { get; init; } = true;
}

/// <summary>
/// Descriptor representing a logical group of tools within a Ribbon Tab.
/// </summary>
public sealed class RibbonGroupDescriptor
{
    public required string Id { get; init; }
    public required string TabId { get; init; }
    public required string Title { get; init; }
    public int Order { get; init; } = 100;
}

/// <summary>
/// Registry for dynamic Ribbon tabs, groups, and actions contributed by plugins.
/// </summary>
public interface IRibbonRegistry
{
    /// <summary>
    /// Registers a dynamic ribbon tab.
    /// </summary>
    IDisposable RegisterTab(RibbonTabDescriptor tab);

    /// <summary>
    /// Registers a ribbon tool group inside a tab.
    /// </summary>
    IDisposable RegisterGroup(RibbonGroupDescriptor group);

    /// <summary>
    /// Registers a ribbon action descriptor.
    /// </summary>
    void RegisterAction(RibbonActionDescriptor action);

    /// <summary>
    /// Unregisters a ribbon action by its unique ID.
    /// </summary>
    bool UnregisterAction(string actionId);

    /// <summary>
    /// Gets all registered ribbon tabs.
    /// </summary>
    IReadOnlyList<RibbonTabDescriptor> GetAllTabs();

    /// <summary>
    /// Gets all groups registered under a specific ribbon tab.
    /// </summary>
    IReadOnlyList<RibbonGroupDescriptor> GetGroupsForTab(string tabId);

    /// <summary>
    /// Gets all actions registered for a specific Ribbon tab (e.g. "Home", "Tools", "Plugins").
    /// </summary>
    IReadOnlyList<RibbonActionDescriptor> GetActionsForTab(string tabId);

    /// <summary>
    /// Gets all actions registered for a specific group within a tab.
    /// </summary>
    IReadOnlyList<RibbonActionDescriptor> GetActionsForGroup(string tabId, string groupId);

    /// <summary>
    /// Gets all registered ribbon actions across all tabs.
    /// </summary>
    IReadOnlyList<RibbonActionDescriptor> GetAllActions();

    /// <summary>
    /// Triggered when tabs, groups, or actions are added or removed.
    /// </summary>
    event Action? RegistryChanged;
}
