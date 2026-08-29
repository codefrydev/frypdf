using System.Collections.Generic;
using PdfEditorApp.Models;

namespace PdfEditorApp.Templates;

public interface ITemplateDefinition
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Category { get; }
    string IconKind { get; }
    string AccentColorHex { get; }

    PdfDocumentModel Create();
}
