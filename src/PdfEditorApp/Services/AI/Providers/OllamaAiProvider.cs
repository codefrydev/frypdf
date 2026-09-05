using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OllamaSharp;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.AI.Providers;

public class OllamaAiProvider : IAiProvider
{
    public string ProviderId => "ollama";
    public string DisplayName => "Ollama (Local Private AI)";
    public bool RequiresApiKey => false;
    public bool SupportsCustomEndpoint => true;
    public string DefaultEndpoint => "http://localhost:11434";
    public string IconKind => "Laptop";

    public Task<IReadOnlyList<AiModelInfo>> GetModelsAsync(AiSettingsModel settings, CancellationToken ct = default)
    {
        var models = new List<AiModelInfo>
        {
            new() { Id = "llama3.2", DisplayName = "Llama 3.2 (3B)", Provider = AiProviderType.OllamaLocal, Tier = AiModelTier.FreeLocal, ParameterSize = "3B" },
            new() { Id = "mistral", DisplayName = "Mistral (7B)", Provider = AiProviderType.OllamaLocal, Tier = AiModelTier.FreeLocal, ParameterSize = "7B" },
            new() { Id = "deepseek-r1:8b", DisplayName = "DeepSeek R1 (8B)", Provider = AiProviderType.OllamaLocal, Tier = AiModelTier.FreeLocal, ParameterSize = "8B" },
            new() { Id = "qwen2.5:7b", DisplayName = "Qwen 2.5 (7B)", Provider = AiProviderType.OllamaLocal, Tier = AiModelTier.FreeLocal, ParameterSize = "7B" }
        };
        return Task.FromResult<IReadOnlyList<AiModelInfo>>(models);
    }

    public IChatClient CreateChatClient(AiSettingsModel settings)
    {
        string endpoint = string.IsNullOrWhiteSpace(settings.OllamaEndpoint)
            ? DefaultEndpoint
            : settings.OllamaEndpoint.TrimEnd('/');

        string model = string.IsNullOrWhiteSpace(settings.SelectedModelId)
            ? "llama3.2"
            : settings.SelectedModelId;

        if (!string.IsNullOrWhiteSpace(settings.OllamaApiKey))
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(endpoint), Timeout = TimeSpan.FromSeconds(60) };
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.OllamaApiKey.Trim());
            return new OllamaApiClient(httpClient, model);
        }

        return new OllamaApiClient(new Uri(endpoint), model);
    }

    public async Task<(bool Success, string Message, TimeSpan Latency)> TestConnectionAsync(AiSettingsModel settings, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var client = CreateChatClient(settings);
            var response = await client.GetResponseAsync("Ping", new ChatOptions { MaxOutputTokens = 10 }, ct);
            sw.Stop();
            return (true, "Connected to Ollama", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, ex.Message, sw.Elapsed);
        }
    }
}
