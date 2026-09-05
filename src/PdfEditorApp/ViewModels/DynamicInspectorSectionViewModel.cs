using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Presentation model for a dynamic inspector property panel section contributed by a plugin.
/// </summary>
public partial class DynamicInspectorSectionViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _sectionId = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _iconKind = "Tune";

    [ObservableProperty]
    private int _order = 100;

    [ObservableProperty]
    private object? _content;

    [ObservableProperty]
    private bool _isExpanded = true;
}
