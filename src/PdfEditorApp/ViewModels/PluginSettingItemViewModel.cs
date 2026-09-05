using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Presentation model for an individual configurable setting inside a plugin's declarative schema.
/// </summary>
public sealed partial class PluginSettingItemViewModel : ViewModelBase
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public string Description { get; init; } = "";
    public string Type { get; init; } = "string"; // string, boolean, number, select, secret
    public List<string> Options { get; init; } = new();

    [ObservableProperty]
    private string _stringValue = string.Empty;

    [ObservableProperty]
    private bool _boolValue;

    [ObservableProperty]
    private double _numberValue;
}
