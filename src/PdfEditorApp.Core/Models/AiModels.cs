using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PdfEditorApp.Core.Models;

/// <summary>
/// Supported AI provider categories in FryPDF.
/// </summary>
public enum AiProviderType
{
    /// <summary>
    /// Local self-hosted Ollama runtime (100% private, free, offline).
    /// </summary>
    OllamaLocal = 0,

    /// <summary>
    /// Cloud OpenAI API (requires user API key, paid usage).
    /// </summary>
    OpenAiCloud = 1,

    /// <summary>
    /// Custom OpenAI-compatible endpoint (Groq, OpenRouter, LM Studio, LocalAI, vLLM).
    /// </summary>
    CustomOpenAiCompatible = 2
}

/// <summary>
/// Economic and locality classification tier of an AI model.
/// </summary>
public enum AiModelTier
{
    /// <summary>
    /// Free, zero cloud data transfer, run locally on device.
    /// </summary>
    FreeLocal = 0,

    /// <summary>
    /// Free cloud tier (e.g. Ollama Cloud free tier, OpenRouter free models, Groq free tier).
    /// </summary>
    FreeCloud = 1,

    /// <summary>
    /// Cloud-hosted API model requiring tokens / paid subscription.
    /// </summary>
    PaidCloud = 2
}

/// <summary>
/// Metadata representing an available AI language model in FryPDF.
/// </summary>
public class AiModelInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public AiProviderType Provider { get; set; } = AiProviderType.OllamaLocal;

    [JsonPropertyName("tier")]
    public AiModelTier Tier { get; set; } = AiModelTier.FreeLocal;

    [JsonPropertyName("sizeBytes")]
    public long? SizeBytes { get; set; }

    [JsonPropertyName("formattedSize")]
    public string FormattedSize { get; set; } = string.Empty;

    [JsonPropertyName("parameterSize")]
    public string ParameterSize { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("isInstalledLocally")]
    public bool IsInstalledLocally { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Ollama Library";

    [JsonPropertyName("capabilities")]
    public string Capabilities { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsFree => Tier == AiModelTier.FreeLocal || Tier == AiModelTier.FreeCloud;

    [JsonIgnore]
    public bool IsCloud => Tier == AiModelTier.FreeCloud || Tier == AiModelTier.PaidCloud;

    [JsonIgnore]
    public string TierBadgeText => Tier switch
    {
        AiModelTier.FreeLocal => "Free / Local",
        AiModelTier.FreeCloud => "Free / Cloud",
        AiModelTier.PaidCloud => "Paid / Cloud",
        _ => "Free"
    };

    [JsonIgnore]
    public string BadgeBackgroundColor => Tier switch
    {
        AiModelTier.FreeLocal => "#DCFCE7",
        AiModelTier.FreeCloud => "#E0F2FE",
        AiModelTier.PaidCloud => "#EDE9FE",
        _ => "#F3F4F6"
    };

    [JsonIgnore]
    public string BadgeBorderColor => Tier switch
    {
        AiModelTier.FreeLocal => "#86EFAC",
        AiModelTier.FreeCloud => "#7DD3FC",
        AiModelTier.PaidCloud => "#C4B5FD",
        _ => "#D1D5DB"
    };

    [JsonIgnore]
    public string BadgeForegroundColor => Tier switch
    {
        AiModelTier.FreeLocal => "#15803D",
        AiModelTier.FreeCloud => "#0369A1",
        AiModelTier.PaidCloud => "#6D28D9",
        _ => "#374151"
    };

    [JsonIgnore]
    public string FullTitleWithBadge => $"[{TierBadgeText}] {DisplayName}";

    public override bool Equals(object? obj) =>
        obj is AiModelInfo other && string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        string.IsNullOrEmpty(Id) ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Id);

    public override string ToString() => string.IsNullOrWhiteSpace(DisplayName) ? Id : FullTitleWithBadge;

    public static AiModelInfo CreateForCustomId(string id, AiProviderType provider, bool isEndpointRemote = false, string? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = provider == AiProviderType.CustomOpenAiCompatible ? "openai/gpt-oss-120b" : "llama3.2";
        }

        id = id.Trim();
        bool isCloud = id.Contains("cloud", StringComparison.OrdinalIgnoreCase) ||
                       id.Contains(":free", StringComparison.OrdinalIgnoreCase) ||
                       isEndpointRemote;

        AiModelTier tier;
        if (provider == AiProviderType.OllamaLocal)
        {
            tier = isCloud ? AiModelTier.FreeCloud : AiModelTier.FreeLocal;
        }
        else if (provider == AiProviderType.CustomOpenAiCompatible)
        {
            bool isGroq = (endpoint?.Contains("groq", StringComparison.OrdinalIgnoreCase) == true) || id.Contains("groq", StringComparison.OrdinalIgnoreCase);
            bool isFree = id.Contains("free", StringComparison.OrdinalIgnoreCase) || isGroq;
            if (!isEndpointRemote)
            {
                tier = AiModelTier.FreeLocal;
            }
            else
            {
                tier = isFree ? AiModelTier.FreeCloud : AiModelTier.PaidCloud;
            }
        }
        else
        {
            tier = AiModelTier.PaidCloud;
        }

        string formattedSize = isCloud
            ? (provider == AiProviderType.CustomOpenAiCompatible && (endpoint?.Contains("groq", StringComparison.OrdinalIgnoreCase) == true) ? "Groq LPU Cloud" : "Cloud API")
            : "Local / Custom";

        return new AiModelInfo
        {
            Id = id,
            DisplayName = id,
            Provider = provider,
            Tier = tier,
            Category = provider == AiProviderType.OllamaLocal ? "Ollama Custom" : (provider == AiProviderType.CustomOpenAiCompatible ? "Custom Compatible" : "OpenAI Cloud"),
            Description = $"Model identifier: {id}",
            FormattedSize = formattedSize,
            IsInstalledLocally = !isCloud && provider == AiProviderType.OllamaLocal
        };
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return string.Empty;
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):0.0} GB";
        if (bytes >= 1024L * 1024)
            return $"{bytes / (1024.0 * 1024):0.0} MB";
        return $"{bytes / 1024.0:0.0} KB";
    }
}

/// <summary>
/// Result of an AI Studio Agent prompt execution.
/// </summary>
public class AiAgentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ElementsCreatedCount { get; set; }
    public List<string> ActionDescriptions { get; set; } = new();
    public string? RawOutput { get; set; }
    public TimeSpan Duration { get; set; }
}
