using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.ViewModels.Shortcuts;

/// <summary>
/// Observable ViewModel representing a single configurable keyboard shortcut in the settings studio.
/// </summary>
public partial class ShortcutBindingItemViewModel : ObservableObject
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Category { get; init; } = "General";
    public string Description { get; init; } = "";
    public string? ContextId { get; init; }
    public ShortcutScope Scope { get; init; } = ShortcutScope.Workspace;
    public string DefaultGesture { get; init; } = "";
    public bool IsCustomizable { get; init; } = true;

    [ObservableProperty]
    private string _effectiveGesture = "";

    [ObservableProperty]
    private bool _isCustomized;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _hasConflict;

    [ObservableProperty]
    private string? _conflictMessage;

    public string ScopeDisplayName => Scope switch
    {
        ShortcutScope.Global => "Global",
        ShortcutScope.Workspace => !string.IsNullOrEmpty(ContextId) ? ContextId : "Workspace",
        ShortcutScope.Context => !string.IsNullOrEmpty(ContextId) ? ContextId : "Context",
        _ => "General"
    };
}
