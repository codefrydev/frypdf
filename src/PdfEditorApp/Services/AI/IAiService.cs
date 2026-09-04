using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.AI;

/// <summary>
/// Service responsible for managing AI providers, model catalogs, connection verification, and IChatClient creation.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Auto-discovers all locally installed models on the specified Ollama endpoint.
    /// </summary>
    Task<IReadOnlyList<AiModelInfo>> DiscoverOllamaModelsAsync(string endpoint, CancellationToken ct = default);

    /// <summary>
    /// Retrieves standard cloud and OpenAI-compatible models labeled with Paid/Cloud tier badges.
    /// </summary>
    IReadOnlyList<AiModelInfo> GetCloudCatalogModels();

    /// <summary>
    /// Retrieves official featured models provided in the Ollama library.
    /// </summary>
    IReadOnlyList<AiModelInfo> GetOllamaLibraryModels();

    /// <summary>
    /// Fetches live models directly from Ollama's online model catalog/search (ollama.com).
    /// </summary>
    Task<IReadOnlyList<AiModelInfo>> FetchOllamaOnlineLibraryAsync(string? query = null, CancellationToken ct = default);

    /// <summary>
    /// Combines discovered local models and cloud models into a unified catalog.
    /// </summary>
    IReadOnlyList<AiModelInfo> GetUnifiedModelCatalog(AiSettingsModel settings);

    /// <summary>
    /// Tests connectivity to the currently configured AI provider.
    /// </summary>
    Task<(bool Success, string Message, TimeSpan Latency)> TestConnectionAsync(AiSettingsModel settings, CancellationToken ct = default);

    /// <summary>
    /// Creates a configured Microsoft.Extensions.AI IChatClient based on active settings.
    /// </summary>
    IChatClient CreateChatClient(AiSettingsModel settings);
}
