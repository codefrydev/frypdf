using System;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.Services.AI;

/// <summary>
/// AI Agent service that takes natural language user prompts and generates or modifies native PDF studio canvas elements.
/// </summary>
public interface IPdfStudioAgentService
{
    /// <summary>
    /// Executes an AI generation prompt on the specified page using Microsoft.Extensions.AI.
    /// </summary>
    Task<AiAgentResult> ExecutePromptAsync(
        string userPrompt,
        PageViewModel targetPage,
        AiSettingsModel settings,
        Action<string>? progressCallback = null,
        CancellationToken ct = default);

    /// <summary>
    /// Modifies an existing canvas element in-place using natural language instructions via Microsoft.Extensions.AI.
    /// </summary>
    Task<AiAgentResult> ModifyElementAsync(
        ElementViewModelBase targetElement,
        string modificationPrompt,
        AiSettingsModel settings,
        Action<string>? progressCallback = null,
        CancellationToken ct = default);
}
