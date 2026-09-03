using PdfEditorApp.ViewModels.Tools.Core;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;

namespace PdfEditorApp.Services.Tools.Core;

/// <summary>
/// Factory interface for creating dedicated PDF Tool ViewModels for any registered tool ID.
/// </summary>
public interface IPdfToolViewModelFactory
{
    PdfToolViewModelBase Create(PdfToolId toolId);
    PdfToolViewModelBase CreateToolViewModel(PdfToolId toolId);
    PdfToolViewModelBase CreateToolViewModel(PdfToolDefinition toolDefinition);
}
