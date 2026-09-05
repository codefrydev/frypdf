using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Presentation wrapper for dynamic plugin sidebar tabs.
/// </summary>
public sealed partial class DynamicSidebarTabViewModel : ViewModelBase
{
    public required SidebarTabDescriptor Descriptor { get; init; }
    public string Id => Descriptor.Id;
    public string Title => Descriptor.Title;
    public string IconKind => Descriptor.IconKind;
    public string Tooltip => Descriptor.Tooltip;

    [ObservableProperty]
    private bool _isActive;
}
