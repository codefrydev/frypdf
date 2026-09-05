using System;
using System.Collections.Generic;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Contract for a document template contributed by a plugin.
/// </summary>
public interface ITemplateDescriptor
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Category { get; }
    string IconKind { get; }
    string AccentColorHex { get; }

    PdfDocumentModel Create();
}

/// <summary>
/// Registry for discovering and dispatching document templates.
/// </summary>
public interface ITemplateRegistry
{
    IDisposable RegisterTemplate(ITemplateDescriptor template);
    ITemplateDescriptor? GetTemplate(string id);
    IReadOnlyList<ITemplateDescriptor> GetAllTemplates();
    IReadOnlyList<string> GetCategories();
    event Action? RegistryChanged;
}
