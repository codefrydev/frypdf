using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Presentation wrapper for dynamic plugin ribbon tabs.
/// </summary>
public sealed partial class DynamicRibbonTabViewModel : ViewModelBase
{
    public required RibbonTabDescriptor Descriptor { get; init; }
    public string Id => Descriptor.Id;
    public string Title => Descriptor.Title;

    [ObservableProperty]
    private bool _isActive;
}
