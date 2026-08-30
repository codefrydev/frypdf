using System.Collections.Generic;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public interface IRecentDocumentsService
{
    /// <summary>Returns the list of recent documents, newest first.</summary>
    List<RecentDocumentItem> Load();

    /// <summary>Adds or bumps an item to the top of the recent list and persists it.</summary>
    void Add(RecentDocumentItem item);

    /// <summary>Removes a specific file path from the recent list.</summary>
    void Remove(string filePath);

    /// <summary>Clears all recent documents.</summary>
    void Clear();
}
