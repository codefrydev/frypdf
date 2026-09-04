using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;

namespace PdfEditorApp.Messages;

/// <summary>
/// Broadcast message to display a toast notification anywhere in the application.
/// </summary>
public record ShowToastMessage(string Message, ToastNotificationType Type = ToastNotificationType.Primary, string? ActionLabel = null);

/// <summary>
/// Request to return to the Home dashboard view.
/// </summary>
public record NavigateToHomeMessage();

/// <summary>
/// Request to open a PDF project or file in the Studio Editor.
/// </summary>
public record OpenInEditorMessage(string FilePath);

/// <summary>
/// Request to open a PDF document in the standalone Reader / Viewer.
/// </summary>
public record OpenInViewerMessage(string FilePath);

/// <summary>
/// Request to launch a specific PDF tool on a given input file.
/// </summary>
public record RunToolMessage(PdfToolId ToolId, string FilePath);

/// <summary>
/// Notification that the user-preferred reading theme has changed.
/// </summary>
public record ReadingThemeChangedMessage(PdfReaderTheme Theme);

/// <summary>
/// Notification that a project file was renamed on disk.
/// </summary>
public record ProjectRenamedMessage(string OldPath, string NewPath);

/// <summary>
/// Notification that a project file was deleted from disk.
/// </summary>
public record ProjectDeletedMessage(string DeletedPath);

/// <summary>
/// Request to prompt the user to rename a document.
/// </summary>
public record PromptRenameMessage(string FilePath);

/// <summary>
/// Request to prompt the user to confirm document deletion.
/// </summary>
public record PromptDeleteMessage(string FilePath);

/// <summary>
/// Request to open a template in the Studio Editor.
/// </summary>
public record OpenTemplateMessage(string? TemplateName);

/// <summary>
/// Request to prompt the user to pick and open a project file in the Studio Editor.
/// </summary>
public record OpenProjectFileMessage();

/// <summary>
/// Request to open the Workflow Studio.
/// </summary>
public record OpenWorkflowStudioMessage();

/// <summary>
/// Request to open the Batch Generation Studio.
/// </summary>
public record OpenBatchGenerationMessage();

/// <summary>
/// Request to prompt the user to pick and open a PDF document in the Viewer.
/// </summary>
public record OpenPdfPickerMessage();
