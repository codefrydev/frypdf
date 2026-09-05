using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Presentation model for a status bar widget contributed dynamically by a plugin.
/// </summary>
public partial class StatusBarWidgetViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _widgetId = "";

    [ObservableProperty]
    private string? _label;

    [ObservableProperty]
    private string? _toolTip;

    [ObservableProperty]
    private string? _iconKind;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private object? _content;

    [ObservableProperty]
    private ICommand? _command;

    [ObservableProperty]
    private object? _commandParameter;
}
