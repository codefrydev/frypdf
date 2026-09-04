using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Models;

/// <summary>
/// Persisted configuration settings for AI providers, models, and local LLM runtime.
/// </summary>
public class AiSettingsModel
{
    [JsonPropertyName("selectedProvider")]
    public AiProviderType SelectedProvider { get; set; } = AiProviderType.OllamaLocal;

    [JsonPropertyName("ollamaEndpoint")]
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    [JsonPropertyName("ollamaApiKey")]
    public string OllamaApiKey { get; set; } = string.Empty;

    [JsonPropertyName("selectedModelId")]
    public string SelectedModelId { get; set; } = "llama3.2";

    [JsonPropertyName("openAiApiKey")]
    public string OpenAiApiKey { get; set; } = string.Empty;

    [JsonPropertyName("customBaseUrl")]
    public string CustomBaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("customModelName")]
    public string CustomModelName { get; set; } = string.Empty;

    [JsonPropertyName("customModelHistory")]
    public List<string> CustomModelHistory { get; set; } = new()
    {
        "openai/gpt-oss-120b",
        "llama-3.3-70b-versatile",
        "qwen/qwen-2.5-coder-32b",
        "deepseek-r1-distill-llama-70b",
        "llama-3.1-8b-instant"
    };

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.7f;

    [JsonPropertyName("systemInstructions")]
    public string SystemInstructions { get; set; } = string.Empty;

    [JsonPropertyName("discoveredOllamaModels")]
    public List<AiModelInfo> DiscoveredOllamaModels { get; set; } = new();

    [JsonPropertyName("ollamaLibraryCache")]
    public List<AiModelInfo> OllamaLibraryCache { get; set; } = new();

    [JsonPropertyName("lastConnectedAt")]
    public DateTime? LastConnectedAt { get; set; }

    [JsonIgnore]
    public bool IsOllamaRemote => !string.IsNullOrWhiteSpace(OllamaEndpoint) &&
                                  !OllamaEndpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
                                  !OllamaEndpoint.Contains("127.0.0.1");

    public AiSettingsModel Clone()
    {
        var copy = new AiSettingsModel
        {
            SelectedProvider = this.SelectedProvider,
            OllamaEndpoint = this.OllamaEndpoint,
            OllamaApiKey = this.OllamaApiKey,
            SelectedModelId = this.SelectedModelId,
            OpenAiApiKey = this.OpenAiApiKey,
            CustomBaseUrl = this.CustomBaseUrl,
            CustomModelName = this.CustomModelName,
            Temperature = this.Temperature,
            SystemInstructions = this.SystemInstructions,
            LastConnectedAt = this.LastConnectedAt
        };

        foreach (var m in DiscoveredOllamaModels)
        {
            copy.DiscoveredOllamaModels.Add(new AiModelInfo
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                Provider = m.Provider,
                Tier = m.Tier,
                Category = m.Category,
                Capabilities = m.Capabilities,
                SizeBytes = m.SizeBytes,
                FormattedSize = m.FormattedSize,
                ParameterSize = m.ParameterSize,
                Description = m.Description,
                IsInstalledLocally = m.IsInstalledLocally
            });
        }

        foreach (var m in OllamaLibraryCache)
        {
            copy.OllamaLibraryCache.Add(new AiModelInfo
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                Provider = m.Provider,
                Tier = m.Tier,
                Category = m.Category,
                Capabilities = m.Capabilities,
                SizeBytes = m.SizeBytes,
                FormattedSize = m.FormattedSize,
                ParameterSize = m.ParameterSize,
                Description = m.Description,
                IsInstalledLocally = m.IsInstalledLocally
            });
        }

        copy.CustomModelHistory.Clear();
        copy.CustomModelHistory.AddRange(this.CustomModelHistory);

        return copy;
    }
}
