using System.Collections.ObjectModel;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Presentation model for a dynamic ribbon group containing contributed actions.
/// </summary>
public sealed class DynamicRibbonGroupViewModel
{
    public required string Id { get; init; }
    public required string TabId { get; init; }
    public required string Title { get; init; }
    public ObservableCollection<RibbonActionDescriptor> Actions { get; } = new();
}
