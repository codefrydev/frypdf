using System;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Services.AI;

/// <summary>
/// AI Agent service that takes natural language user prompts and generates native PDF studio canvas elements.
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
}
