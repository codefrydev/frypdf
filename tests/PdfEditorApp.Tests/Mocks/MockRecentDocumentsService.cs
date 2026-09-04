using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.Tests.Mocks;

/// <summary>
/// In-memory mock implementation of <see cref="IRecentDocumentsService"/> for isolated unit tests.
/// Prevents unit tests from reading or writing to the user's real application data folder.
/// </summary>
public class MockRecentDocumentsService : IRecentDocumentsService
{
    public List<RecentDocumentItem> Items { get; } = new();

    public List<RecentDocumentItem> Load() => new(Items);

    public void Add(RecentDocumentItem item)
    {
        Items.RemoveAll(x => string.Equals(x.FilePath, item.FilePath, System.StringComparison.OrdinalIgnoreCase));
        Items.Insert(0, item);
    }

    public void Remove(string filePath) =>
        Items.RemoveAll(x => string.Equals(x.FilePath, filePath, System.StringComparison.OrdinalIgnoreCase));

    public void Rename(string oldFilePath, string newFilePath, string newTitle)
    {
        var match = Items.Find(x => string.Equals(x.FilePath, oldFilePath, System.StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            match.FilePath = newFilePath;
            match.Title = newTitle;
            match.LastOpened = System.DateTime.UtcNow;
        }
    }

    public void Clear() => Items.Clear();
}
