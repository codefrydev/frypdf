using System;
using System.IO;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Models;

/// <summary>
/// Represents a recently opened or saved FryPDF project file entry.
/// </summary>
public partial class RecentDocumentItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _title = "Untitled";

    [ObservableProperty]
    private DateTime _lastOpened = DateTime.UtcNow;

    [ObservableProperty]
    private int _pageCount = 1;

    [ObservableProperty]
    private string _formatDescription = "A4 Document";

    [ObservableProperty]
    [property: JsonIgnore]
    private PageViewModel? _pagePreview;

    [JsonIgnore]
    public bool HasLivePreview => PagePreview != null;

    [JsonIgnore]
    public string FileName => string.IsNullOrEmpty(FilePath) ? Title : Path.GetFileName(FilePath);

    [JsonIgnore]
    public string DirectoryName => string.IsNullOrEmpty(FilePath) ? "" : (Path.GetDirectoryName(FilePath) ?? "");

    [JsonIgnore]
    public bool FileExists => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

    [JsonIgnore]
    public string FormattedFileSize
    {
        get
        {
            try
            {
                if (!FileExists) return "";
                var bytes = new FileInfo(FilePath).Length;
                if (bytes < 1024) return $"{bytes} B";
                if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            }
            catch
            {
                return "";
            }
        }
    }

    /// <summary>Friendly display string for how long ago the file was opened.</summary>
    [JsonIgnore]
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

