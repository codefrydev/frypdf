using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Defines how a navigation section or workspace view is hosted within the application window.
/// </summary>
public enum NavigationDisplayMode
{
    /// <summary>
    /// Hosted inside standard scrollable container beneath the global search bar.
    /// Ideal for dashboards, lists, settings, and documentation pages.
    /// </summary>
    ScrollableDocument = 0,

    /// <summary>
    /// Fills the content viewport completely without an outer ScrollViewer.
    /// Ideal for interactive canvases, graphic editors, split views, and tools that manage their own scrolling and layout.
    /// </summary>
    FullViewport = 1,

    /// <summary>
    /// Immersive studio mode: fills the entire viewport, hides the top global search bar,
    /// and collapses the navigation sidebar to an icon rail to maximize workspace canvas space.
    /// </summary>
    ImmersiveStudio = 2
}

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

    /// <summary>
    /// Controls how the host shell renders this navigation item (e.g. standard scrollable vs full-bleed viewport).
    /// </summary>
    public NavigationDisplayMode DisplayMode { get; init; } = NavigationDisplayMode.ScrollableDocument;

    /// <summary>
    /// When true, hides the top global search bar so the workspace page can occupy the full vertical height
    /// or supply its own contextual command/header bar.
    /// </summary>
    public bool HideTopSearchBar { get; init; } = false;

    /// <summary>
    /// Optional factory to create a contextual header bar to display in the top slot.
    /// </summary>
    public Func<IServiceProvider, object>? HeaderFactory { get; init; }
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
