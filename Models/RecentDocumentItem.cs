using System;

namespace PdfEditorApp.Models;

/// <summary>
/// Represents a recently opened or saved FryPDF project file entry.
/// </summary>
public class RecentDocumentItem
{
    public string FilePath { get; set; } = "";
    public string Title { get; set; } = "Untitled";
    public DateTime LastOpened { get; set; } = DateTime.UtcNow;

    /// <summary>Friendly display string for how long ago the file was opened.</summary>
    public string RelativeTime
    {
        get
        {
            var delta = DateTime.UtcNow - LastOpened;
            if (delta.TotalMinutes < 1) return "Just now";
            if (delta.TotalHours < 1) return $"{(int)delta.TotalMinutes}m ago";
            if (delta.TotalDays < 1) return $"{(int)delta.TotalHours}h ago";
            if (delta.TotalDays < 7) return $"{(int)delta.TotalDays}d ago";
            return LastOpened.ToString("MMM d, yyyy");
        }
    }
}
