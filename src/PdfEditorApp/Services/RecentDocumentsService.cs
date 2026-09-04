using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

/// <summary>
/// Persists up to 20 recent documents to a JSON file in the user's application data folder.
/// </summary>
public class RecentDocumentsService : IRecentDocumentsService
{
    private const int MaxRecent = 20;

    private static string StoragePath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "FryPDF");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "recent.json");
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public List<RecentDocumentItem> Load()
    {
        try
        {
            if (!File.Exists(StoragePath)) return new List<RecentDocumentItem>();
            var json = File.ReadAllText(StoragePath);
            var list = JsonSerializer.Deserialize<List<RecentDocumentItem>>(json, _jsonOptions)
                   ?? new List<RecentDocumentItem>();
            return list.Where(x => !string.IsNullOrWhiteSpace(x.FilePath) && File.Exists(x.FilePath)).ToList();
        }
        catch
        {
            return new List<RecentDocumentItem>();
        }
    }

    public void Add(RecentDocumentItem item)
    {
        if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
            return;

        var list = Load();
        // Remove existing entry for the same path (bump to top)
        list.RemoveAll(x => string.Equals(x.FilePath, item.FilePath, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, item);
        // Trim to max
        if (list.Count > MaxRecent) list = list.Take(MaxRecent).ToList();
        Save(list);
    }

    public void Remove(string filePath)
    {
        var list = Load();
        list.RemoveAll(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        Save(list);
    }

    public void Rename(string oldFilePath, string newFilePath, string newTitle)
    {
        if (string.IsNullOrWhiteSpace(oldFilePath) || string.IsNullOrWhiteSpace(newFilePath))
            return;

        var list = Load();
        var match = list.FirstOrDefault(x => string.Equals(x.FilePath, oldFilePath, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            match.FilePath = newFilePath;
            match.Title = newTitle;
            match.LastOpened = DateTime.UtcNow;
            Save(list);
        }
    }

    public void Clear()
    {
        Save(new List<RecentDocumentItem>());
    }

    private static void Save(List<RecentDocumentItem> list)
    {
        try
        {
            var json = JsonSerializer.Serialize(list, _jsonOptions);
            File.WriteAllText(StoragePath, json);
        }
        catch
        {
            // Best-effort: silently swallow disk errors
        }
    }
}
