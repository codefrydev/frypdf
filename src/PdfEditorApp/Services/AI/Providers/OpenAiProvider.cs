using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.AI.Providers;

public class OpenAiProvider : IAiProvider
{
    public string ProviderId => "openai";
    public string DisplayName => "OpenAI Official Cloud API";
    public bool RequiresApiKey => true;
    public bool SupportsCustomEndpoint => false;
    public string DefaultEndpoint => "https://api.openai.com/v1";
    public string IconKind => "Brain";

    public Task<IReadOnlyList<AiModelInfo>> GetModelsAsync(AiSettingsModel settings, CancellationToken ct = default)
    {
        var models = new List<AiModelInfo>
        {
            new() { Id = "gpt-4o", DisplayName = "GPT-4o (Omni)", Provider = AiProviderType.OpenAiCloud, Tier = AiModelTier.PaidCloud, ParameterSize = "Flagship" },
            new() { Id = "gpt-4o-mini", DisplayName = "GPT-4o Mini", Provider = AiProviderType.OpenAiCloud, Tier = AiModelTier.PaidCloud, ParameterSize = "Fast & Cheap" },
            new() { Id = "o1-mini", DisplayName = "o1-mini (Reasoning)", Provider = AiProviderType.OpenAiCloud, Tier = AiModelTier.PaidCloud, ParameterSize = "Reasoning" }
        };
        return Task.FromResult<IReadOnlyList<AiModelInfo>>(models);
    }

    public IChatClient CreateChatClient(AiSettingsModel settings)
    {
        string apiKey = string.IsNullOrWhiteSpace(settings.OpenAiApiKey)
            ? "dummy-key"
            : settings.OpenAiApiKey.Trim();

        string model = string.IsNullOrWhiteSpace(settings.SelectedModelId)
            ? "gpt-4o-mini"
            : settings.SelectedModelId;

        var client = new ChatClient(model, apiKey);
        return client.AsIChatClient();
    }

    public async Task<(bool Success, string Message, TimeSpan Latency)> TestConnectionAsync(AiSettingsModel settings, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var client = CreateChatClient(settings);
            var response = await client.GetResponseAsync("Ping", new ChatOptions { MaxOutputTokens = 10 }, ct);
            sw.Stop();
            return (true, "Connected to OpenAI", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, ex.Message, sw.Elapsed);
        }
    }
}
