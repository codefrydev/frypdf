using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel representing a dynamic workspace navigation section contributed by a plugin.
/// </summary>
public partial class NavigationItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _group;

    [ObservableProperty]
    private string _iconKind;

    [ObservableProperty]
    private string? _badgeText;

    [ObservableProperty]
    private string? _badgeColorHex;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private bool _isActive;

    public NavigationItemDescriptor Descriptor { get; }

    public NavigationItemViewModel(NavigationItemDescriptor descriptor)
    {
        Descriptor = descriptor;
        _id = descriptor.Id;
        _title = descriptor.Title;
        _group = descriptor.Group;
        _iconKind = descriptor.IconKind;
        _badgeText = descriptor.BadgeText;
        _badgeColorHex = descriptor.BadgeColorHex;
        _order = descriptor.Order;
    }
}

/// <summary>
/// Groups a set of navigation items under a single section header for plugin-driven sidebar rendering.
/// </summary>
public sealed class NavGroupViewModel
{
    /// <summary>Header label displayed above the group (e.g. "OVERVIEW", "CATEGORIES").</summary>
    public string Header { get; }

    /// <summary>Ordered navigation items belonging to this group.</summary>
    public IReadOnlyList<NavigationItemViewModel> Items { get; }

    public NavGroupViewModel(string header, IReadOnlyList<NavigationItemViewModel> items)
    {
        Header = header.ToUpperInvariant();
        Items = items;
    }
}
