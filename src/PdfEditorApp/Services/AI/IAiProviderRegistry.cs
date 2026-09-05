using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.AI;

/// <summary>
/// Contract for an AI provider plugin (e.g., Ollama, Groq, OpenAI, Anthropic, Gemini, Mock).
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Unique provider ID, e.g. "ollama", "groq", "openai", "claude", "gemini".
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// User-facing display name, e.g. "Ollama (Local Private AI)" or "Groq (Ultra-Fast Cloud LPU)".
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Indicates whether this provider requires an API key.
    /// </summary>
    bool RequiresApiKey { get; }

    /// <summary>
    /// Indicates whether this provider allows custom endpoint URL configuration.
    /// </summary>
    bool SupportsCustomEndpoint { get; }

    /// <summary>
    /// Default API endpoint URL if applicable.
    /// </summary>
    string DefaultEndpoint => "";

    /// <summary>
    /// Material icon name representing the provider.
    /// </summary>
    string IconKind => "RobotOutline";

    /// <summary>
    /// Retrieves the list of available models supported by this provider.
    /// </summary>
    Task<IReadOnlyList<AiModelInfo>> GetModelsAsync(AiSettingsModel settings, CancellationToken ct = default);

    /// <summary>
    /// Creates a configured <see cref="IChatClient"/> for this provider.
    /// </summary>
    IChatClient CreateChatClient(AiSettingsModel settings);

    /// <summary>
    /// Tests connectivity and latency to the provider endpoint.
    /// </summary>
    Task<(bool Success, string Message, TimeSpan Latency)> TestConnectionAsync(AiSettingsModel settings, CancellationToken ct = default);
}

/// <summary>
/// Registry for discovering and dispatching pluggable AI backends.
/// </summary>
public interface IAiProviderRegistry
{
    /// <summary>
    /// Registers an AI provider into the system.
    /// </summary>
    IDisposable RegisterProvider(IAiProvider provider);

    /// <summary>
    /// Retrieves an AI provider by its unique identifier.
    /// </summary>
    IAiProvider? GetProvider(string providerId);

    /// <summary>
    /// Gets all currently registered AI providers.
    /// </summary>
    IReadOnlyList<IAiProvider> GetAllProviders();

    /// <summary>
    /// Raised whenever an AI provider is registered or unregistered.
    /// </summary>
    event Action? RegistryChanged;
}
