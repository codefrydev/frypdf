using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Descriptor representing an editor sidebar panel contributed by a plugin.
/// </summary>
public sealed class SidebarTabDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string IconKind { get; init; } = "DockLeft";
    public string Tooltip { get; init; } = "";
    public int Order { get; init; } = 100;
    public Type? ViewModelType { get; init; }
    public Func<IServiceProvider, object>? ViewFactory { get; init; }
    public Func<IServiceProvider, object>? ViewModelFactory { get; init; }
}

/// <summary>
/// Registry for pluggable document editor left/right sidebar tabs.
/// </summary>
public interface ISidebarRegistry
{
    /// <summary>
    /// Registers a sidebar tab descriptor.
    /// </summary>
    IDisposable RegisterTab(SidebarTabDescriptor descriptor);

    /// <summary>
    /// Unregisters a sidebar tab by its ID.
    /// </summary>
    bool UnregisterTab(string tabId);

    /// <summary>
    /// Gets all registered sidebar tabs ordered by priority.
    /// </summary>
    IReadOnlyList<SidebarTabDescriptor> GetAllTabs();

    /// <summary>
    /// Gets a single tab descriptor by its ID.
    /// </summary>
    SidebarTabDescriptor? GetTab(string tabId);

    /// <summary>
    /// Fired when sidebar tabs are registered or unregistered.
    /// </summary>
    event Action? RegistryChanged;
}
