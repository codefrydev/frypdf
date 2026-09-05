using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.AI.Providers;

public class GroqAiProvider : IAiProvider
{
    public string ProviderId => "groq";
    public string DisplayName => "Groq (Ultra-Fast Cloud LPU)";
    public bool RequiresApiKey => true;
    public bool SupportsCustomEndpoint => true;
    public string DefaultEndpoint => "https://api.groq.com/openai/v1";
    public string IconKind => "LightningBoltOutline";

    public Task<IReadOnlyList<AiModelInfo>> GetModelsAsync(AiSettingsModel settings, CancellationToken ct = default)
    {
        var models = new List<AiModelInfo>
        {
            new() { Id = "llama-3.3-70b-versatile", DisplayName = "Llama 3.3 (70B Versatile)", Provider = AiProviderType.CustomOpenAiCompatible, Tier = AiModelTier.PaidCloud, ParameterSize = "70B" },
            new() { Id = "mixtral-8x7b-32768", DisplayName = "Mixtral (8x7B MoE)", Provider = AiProviderType.CustomOpenAiCompatible, Tier = AiModelTier.PaidCloud, ParameterSize = "8x7B" },
            new() { Id = "deepseek-r1-distill-llama-70b", DisplayName = "DeepSeek R1 Distill (70B)", Provider = AiProviderType.CustomOpenAiCompatible, Tier = AiModelTier.PaidCloud, ParameterSize = "70B" }
        };
        return Task.FromResult<IReadOnlyList<AiModelInfo>>(models);
    }

    public IChatClient CreateChatClient(AiSettingsModel settings)
    {
        string apiKey = string.IsNullOrWhiteSpace(settings.OpenAiApiKey)
            ? "dummy-key"
            : settings.OpenAiApiKey.Trim();

        string model = string.IsNullOrWhiteSpace(settings.CustomModelName)
            ? (string.IsNullOrWhiteSpace(settings.SelectedModelId) ? "llama-3.3-70b-versatile" : settings.SelectedModelId)
            : settings.CustomModelName.Trim();

        string baseUrl = string.IsNullOrWhiteSpace(settings.CustomBaseUrl)
            ? DefaultEndpoint
            : settings.CustomBaseUrl.TrimEnd('/');

        var client = new ChatClient(
            model,
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(baseUrl) });

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
            return (true, "Connected to Groq", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, ex.Message, sw.Elapsed);
        }
    }
}
