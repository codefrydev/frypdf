using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PdfEditorApp.Models;

public enum AiChatRole
{
    User,
    Assistant,
    System
}

/// <summary>
/// Represents a single message within an AI Studio Assistant conversation thread.
/// </summary>
public partial class AiChatMessage : ObservableObject
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public AiChatRole Role { get; init; } = AiChatRole.User;

    public bool IsUser => Role == AiChatRole.User;
    public bool IsAssistant => Role == AiChatRole.Assistant;
    public bool IsSystem => Role == AiChatRole.System;

    [ObservableProperty]
    private string _content = string.Empty;

    public DateTime Timestamp { get; init; } = DateTime.Now;

    public string FormattedTime => Timestamp.ToString("HH:mm");

    [ObservableProperty]
    private string? _targetElementTitle;

    [ObservableProperty]
    private string? _targetElementKind;

    public bool HasTargetElement => !string.IsNullOrEmpty(TargetElementTitle);

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _isSuccess = true;

    [ObservableProperty]
    private TimeSpan _duration = TimeSpan.Zero;

    public string FormattedDuration => Duration.TotalSeconds > 0 ? $"{Duration.TotalSeconds:0.1}s" : "";

    [ObservableProperty]
    private bool _canUndo;

    public Action? UndoAction { get; set; }

    public ObservableCollection<string> ToolCalls { get; } = new();

    public bool HasToolCalls => ToolCalls.Count > 0;

    [RelayCommand]
    public void Undo()
    {
        if (CanUndo)
        {
            UndoAction?.Invoke();
            CanUndo = false;
        }
    }
}
