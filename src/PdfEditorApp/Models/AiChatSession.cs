using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfEditorApp.Models;

/// <summary>
/// Represents a conversation session containing a sequence of AI chat messages.
/// </summary>
public partial class AiChatSession : ObservableObject
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string _title = "New Chat";

    public DateTime CreatedAt { get; init; } = DateTime.Now;

    [ObservableProperty]
    private DateTime _lastModified = DateTime.Now;

    public ObservableCollection<AiChatMessage> Messages { get; } = new();

    public int MessageCount => Messages.Count;

    public string PreviewSnippet
    {
        get
        {
            var last = Messages.LastOrDefault();
            if (last == null) return "Empty conversation";
            string text = last.Content?.Trim() ?? "";
            return text.Length <= 40 ? text : text[..40] + "...";
        }
    }

    public string FormattedLastModified => LastModified.ToString("MMM d, HH:mm");

    public AiChatSession()
    {
        Messages.CollectionChanged += OnMessagesChanged;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MessageCount));
        OnPropertyChanged(nameof(PreviewSnippet));
    }
}
