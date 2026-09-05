using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Descriptor representing a workspace page or navigation section contributed by a plugin.
/// </summary>
public sealed class NavigationItemDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Group { get; init; } = "General"; // Overview, Categories, Library, Preferences, Extensions
    public string IconKind { get; init; } = "ApplicationOutline";
    public string? BadgeText { get; init; }
    public string? BadgeColorHex { get; init; }
    public int Order { get; init; } = 100;
    public Type? ViewModelType { get; init; }
    public Func<IServiceProvider, object>? ViewFactory { get; init; }
    public Func<IServiceProvider, object>? ViewModelFactory { get; init; }
}

/// <summary>
/// Registry for pluggable navigation sections and full-page workspace views.
/// </summary>
public interface INavigationRegistry
{
    /// <summary>
    /// Registers a new navigation item descriptor.
    /// </summary>
    IDisposable RegisterNavigationItem(NavigationItemDescriptor descriptor);

    /// <summary>
    /// Unregisters a navigation item by its unique ID.
    /// </summary>
    bool UnregisterNavigationItem(string itemId);

    /// <summary>
    /// Gets all registered navigation items in ascending display order.
    /// </summary>
    IReadOnlyList<NavigationItemDescriptor> GetAllItems();

    /// <summary>
    /// Gets registered navigation items belonging to a specific group.
    /// </summary>
    IReadOnlyList<NavigationItemDescriptor> GetItemsByGroup(string group);

    /// <summary>
    /// Gets a single navigation item by its unique ID.
    /// </summary>
    NavigationItemDescriptor? GetItem(string itemId);

    /// <summary>
    /// Fired when navigation items are registered or unregistered.
    /// </summary>
    event Action? RegistryChanged;
}
