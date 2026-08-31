using System.Collections.Generic;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

/// <summary>
/// Service interface for accessing and searching the built-in FryPDF help guides, tool tutorials, and documentation knowledge base.
/// </summary>
public interface IHelpGuideService
{
    /// <summary>
    /// Returns all available help guide topics.
    /// </summary>
    IReadOnlyList<HelpGuideItem> GetAllGuides();

    /// <summary>
    /// Returns help guides matching a specific category.
    /// </summary>
    IReadOnlyList<HelpGuideItem> GetGuidesByCategory(string category);

    /// <summary>
    /// Looks up a guide item by unique ID.
    /// </summary>
    HelpGuideItem? GetGuideById(string id);

    /// <summary>
    /// Looks up a guide item corresponding to a specific PDF tool.
    /// </summary>
    HelpGuideItem? GetGuideByToolId(PdfToolId toolId);

    /// <summary>
    /// Returns all distinct categories in the knowledge base.
    /// </summary>
    IReadOnlyList<string> GetAllCategories();
}
