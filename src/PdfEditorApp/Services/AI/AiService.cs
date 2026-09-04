using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.AI;

/// <summary>
/// Core implementation of IAiService leveraging Microsoft.Extensions.AI, OllamaSharp, and OpenAI SDK.
/// </summary>
public class AiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly List<AiModelInfo> _ollamaLibraryCache = new();
    private readonly object _libraryLock = new();

    public AiService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiModelInfo>> DiscoverOllamaModelsAsync(string endpoint, CancellationToken ct = default)
    {
        var results = new List<AiModelInfo>();

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = "http://localhost:11434";
        }

        bool isRemote = !string.IsNullOrWhiteSpace(endpoint) &&
                        !endpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
                        !endpoint.Contains("127.0.0.1");

        try
        {
            string url = $"{endpoint.TrimEnd('/')}/api/tags";
            var response = await _httpClient.GetFromJsonAsync<OllamaTagsResponse>(url, ct);

            if (response?.Models != null && response.Models.Count > 0)
            {
                foreach (var m in response.Models)
                {
                    string paramSize = m.Details?.ParameterSize ?? ExtractParamSizeFromName(m.Name);
                    bool hasRemoteHost = !string.IsNullOrWhiteSpace(m.RemoteHost);
                    bool isCloudModel = isRemote ||
                                        hasRemoteHost ||
                                        m.Name.Contains("cloud", StringComparison.OrdinalIgnoreCase) ||
                                        (m.Size > 0 && m.Size < 1024 * 1024 && !string.IsNullOrEmpty(paramSize));

                    var modelTier = isCloudModel ? AiModelTier.FreeCloud : AiModelTier.FreeLocal;
                    string formattedSize = (isCloudModel && m.Size < 1024 * 1024)
                        ? "Ollama Cloud"
                        : AiModelInfo.FormatBytes(m.Size);

                    var capabilitiesList = new List<string>();
                    if (m.Capabilities != null && m.Capabilities.Count > 0)
                    {
                        foreach (var cap in m.Capabilities)
                        {
                            capabilitiesList.Add(Capitalize(cap));
                        }
                    }
                    else
                    {
                        if (isCloudModel) capabilitiesList.Add("Cloud");
                        capabilitiesList.Add("Completion");
                    }
                    string capStr = string.Join(" • ", capabilitiesList);

                    string hostNote = hasRemoteHost ? $" [via {m.RemoteHost}]" : string.Empty;
                    string desc = !string.IsNullOrEmpty(m.Details?.Family)
                        ? $"{Capitalize(m.Details.Family)} architecture ({paramSize}) [{(isCloudModel ? "Ollama Free Cloud" : "Local Free")}]{hostNote}"
                        : (isCloudModel ? $"Ollama Free Cloud model{hostNote}" : "Local Ollama model");

                    string category = isCloudModel ? "Ollama Cloud (Installed)" : "Installed on Ollama";

                    results.Add(new AiModelInfo
                    {
                        Id = m.Name,
                        DisplayName = FormatModelDisplayName(m.Name),
                        Provider = AiProviderType.OllamaLocal,
                        Tier = modelTier,
                        Category = category,
                        Capabilities = capStr,
                        SizeBytes = m.Size,
                        FormattedSize = formattedSize,
                        ParameterSize = paramSize,
                        Description = desc,
                        IsInstalledLocally = !isRemote && !hasRemoteHost
                    });
                }
            }
        }
        catch
        {
            // If Ollama is not currently running or unreachable, return empty list gracefully
        }

        return results;
    }

    /// <inheritdoc />
    public IReadOnlyList<AiModelInfo> GetCloudCatalogModels()
    {
        return new List<AiModelInfo>
        {
            new()
            {
                Id = "llama-3.1-8b-instant",
                DisplayName = "Llama 3.1 8B (Groq Free Cloud)",
                Provider = AiProviderType.CustomOpenAiCompatible,
                Tier = AiModelTier.FreeCloud,
                Description = "Ultra-fast free cloud tier inference powered by Groq LPU",
                ParameterSize = "8B Cloud",
                IsInstalledLocally = false
            },
            new()
            {
                Id = "meta-llama/llama-3.2-3b-instruct:free",
                DisplayName = "Llama 3.2 3B (OpenRouter Free Cloud)",
                Provider = AiProviderType.CustomOpenAiCompatible,
                Tier = AiModelTier.FreeCloud,
                Description = "Official free cloud tier model via OpenRouter API",
                ParameterSize = "3B Cloud",
                IsInstalledLocally = false
            },
            new()
            {
                Id = "gpt-4o-mini",
                DisplayName = "GPT-4o Mini (OpenAI)",
                Provider = AiProviderType.OpenAiCloud,
                Tier = AiModelTier.PaidCloud,
                Description = "Fast, lightweight flagship-grade intelligence (recommended for high accuracy)",
                ParameterSize = "Cloud API",
                IsInstalledLocally = false
            },
            new()
            {
                Id = "gpt-4o",
                DisplayName = "GPT-4o (OpenAI)",
                Provider = AiProviderType.OpenAiCloud,
                Tier = AiModelTier.PaidCloud,
                Description = "Omni flagship model with complex layout reasoning and multimodal capabilities",
                ParameterSize = "Cloud API",
                IsInstalledLocally = false
            },
            new()
            {
                Id = "o1-mini",
                DisplayName = "o1-mini Reasoning (OpenAI)",
                Provider = AiProviderType.OpenAiCloud,
                Tier = AiModelTier.PaidCloud,
                Description = "Deep reasoning model for math, complex data tables, and dense structure",
                ParameterSize = "Reasoning Cloud",
                IsInstalledLocally = false
            },
            new()
            {
                Id = "llama-3.3-70b-versatile",
                DisplayName = "Llama 3.3 70B (Groq / OpenRouter)",
                Provider = AiProviderType.CustomOpenAiCompatible,
                Tier = AiModelTier.PaidCloud,
                Description = "High-speed LPU inference via Groq or compatible OpenAI provider",
                ParameterSize = "70B Cloud",
                IsInstalledLocally = false
            },
            new()
            {
                Id = "mixtral-8x7b-32768",
                DisplayName = "Mixtral 8x7B (Custom / Groq)",
                Provider = AiProviderType.CustomOpenAiCompatible,
                Tier = AiModelTier.PaidCloud,
                Description = "High-throughput mixture of experts via custom API endpoint",
                ParameterSize = "8x7B Cloud",
                IsInstalledLocally = false
            }
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<AiModelInfo> GetOllamaLibraryModels()
    {
        lock (_libraryLock)
        {
            return _ollamaLibraryCache.ToList();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiModelInfo>> FetchOllamaOnlineLibraryAsync(string? query = null, CancellationToken ct = default)
    {
        var fetched = new List<AiModelInfo>();
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // Fetch both newest and popular trending models from ollama.com
                var taskNewest = _httpClient.GetStringAsync("https://ollama.com/search?o=newest", ct);
                var taskPopular = _httpClient.GetStringAsync("https://ollama.com/search", ct);
                await Task.WhenAll(taskNewest, taskPopular);

                var newestList = ParseOllamaLibraryHtml(taskNewest.Result);
                var popularList = ParseOllamaLibraryHtml(taskPopular.Result);

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in newestList.Concat(popularList))
                {
                    if (seen.Add(m.Id))
                    {
                        fetched.Add(m);
                    }
                }
            }
            else
            {
                string searchUrl = $"https://ollama.com/search?q={Uri.EscapeDataString(query.Trim())}";
                string html = await _httpClient.GetStringAsync(searchUrl, ct);
                fetched.AddRange(ParseOllamaLibraryHtml(html));
            }

            if (fetched.Count > 0)
            {
                lock (_libraryLock)
                {
                    _ollamaLibraryCache.Clear();
                    _ollamaLibraryCache.AddRange(fetched);
                }
            }
        }
        catch
        {
            // Network error
        }

        lock (_libraryLock)
        {
            return _ollamaLibraryCache.ToList();
        }
    }

    /// <summary>
    /// Parses public HTML from ollama.com/search to extract live model cards, capabilities, tags, and sizes.
    /// </summary>
    public static List<AiModelInfo> ParseOllamaLibraryHtml(string html)
    {
        var list = new List<AiModelInfo>();
        if (string.IsNullOrWhiteSpace(html)) return list;

        var itemMatches = Regex.Matches(
            html,
            @"<a\s+href=""/library/([^""/?#]+)""(?:\s+class=""[^""]*"")?>(.*?)(?=</li>|<a\s+href=""/library/|$)",
            RegexOptions.Singleline);

        foreach (Match m in itemMatches)
        {
            string id = m.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(id) || list.Any(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                continue;

            string block = m.Groups[2].Value;

            // Extract Description
            string desc = string.Empty;
            var descMatch = Regex.Match(block, @"<p\s+class=""max-w-lg[^""]*""[^>]*>(.*?)</p>", RegexOptions.Singleline);
            if (descMatch.Success)
            {
                desc = System.Net.WebUtility.HtmlDecode(descMatch.Groups[1].Value.Trim());
            }

            // Extract Capability Badges (vision, tools, thinking, cloud, etc.)
            var badgeMatches = Regex.Matches(block, @"<span[^>]*class=""[^""]*rounded-md[^""]*""[^>]*>\s*([a-zA-Z0-9_\-\.]+)\s*</span>", RegexOptions.Singleline);
            var capabilities = new List<string>();
            bool isCloud = false;

            foreach (Match bm in badgeMatches)
            {
                string tag = bm.Groups[1].Value.Trim();
                if (string.Equals(tag, "cloud", StringComparison.OrdinalIgnoreCase))
                {
                    isCloud = true;
                }
                else if (!string.IsNullOrEmpty(tag))
                {
                    capabilities.Add(Capitalize(tag));
                }
            }

            if (id.Contains("cloud", StringComparison.OrdinalIgnoreCase))
            {
                isCloud = true;
            }

            // Extract Pulls (e.g. 69.7K Pulls)
            string pulls = string.Empty;
            var pullsMatch = Regex.Match(block, @">\s*([0-9\.]+[KMB]?)\s*</span>\s*<span[^>]*>&nbsp;Pulls</span>", RegexOptions.Singleline);
            if (pullsMatch.Success)
            {
                pulls = pullsMatch.Groups[1].Value + " pulls";
            }

            string paramSize = ExtractParamSizeFromName(id);
            if (string.IsNullOrEmpty(paramSize))
            {
                var pMatch = Regex.Match(desc, @"\b([0-9]+(?:\.[0-9]+)?B)\b", RegexOptions.IgnoreCase);
                if (pMatch.Success)
                {
                    paramSize = pMatch.Groups[1].Value.ToUpperInvariant();
                }
            }

            string capStr = string.Join(" • ", capabilities);
            if (string.IsNullOrEmpty(capStr))
            {
                capStr = isCloud ? "Cloud • Fast Inference" : "Tools • Text";
            }

            var tier = isCloud ? AiModelTier.FreeCloud : AiModelTier.FreeLocal;
            string formattedSize = isCloud ? "Ollama Cloud" : (!string.IsNullOrEmpty(pulls) ? pulls : "Ollama Library");

            list.Add(new AiModelInfo
            {
                Id = id,
                DisplayName = FormatModelDisplayName(id),
                Provider = AiProviderType.OllamaLocal,
                Tier = tier,
                Category = isCloud ? "Ollama Cloud (Online)" : "Ollama Library (Online)",
                Capabilities = capStr,
                ParameterSize = paramSize,
                FormattedSize = formattedSize,
                Description = string.IsNullOrEmpty(desc) ? $"Ollama {(isCloud ? "Cloud" : "Library")} model {id}" : desc,
                IsInstalledLocally = false
            });
        }

        return list;
    }

    /// <inheritdoc />
    public IReadOnlyList<AiModelInfo> GetUnifiedModelCatalog(AiSettingsModel settings)
    {
        var catalog = new List<AiModelInfo>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Add Discovered Ollama Models (Installed on user's machine/endpoint)
        if (settings.DiscoveredOllamaModels.Count > 0)
        {
            foreach (var m in settings.DiscoveredOllamaModels)
            {
                if (seenIds.Add(m.Id))
                {
                    catalog.Add(m);
                }
            }
        }

        // 2. Add Cached Online Library Models (from previous fetch)
        if (settings.OllamaLibraryCache != null && settings.OllamaLibraryCache.Count > 0)
        {
            foreach (var cached in settings.OllamaLibraryCache)
            {
                if (seenIds.Add(cached.Id))
                {
                    catalog.Add(cached);
                }
            }
        }

        // 3. Add Ollama Library Models (Official online or fallback seed models)
        foreach (var libModel in GetOllamaLibraryModels())
        {
            if (seenIds.Add(libModel.Id))
            {
                catalog.Add(libModel);
            }
        }

        // 4. Add Cloud Models (Free Cloud & Paid Cloud)
        foreach (var cloudModel in GetCloudCatalogModels())
        {
            if (seenIds.Add(cloudModel.Id))
            {
                catalog.Add(cloudModel);
            }
        }

        return catalog;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message, TimeSpan Latency)> TestConnectionAsync(AiSettingsModel settings, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var chatClient = CreateChatClient(settings);
            var response = await chatClient.GetResponseAsync(
                "Respond in 3 words confirming you are connected to FryPDF Studio.",
                new ChatOptions
                {
                    MaxOutputTokens = 25,
                    Temperature = 0.2f
                },
                ct);

            sw.Stop();
            string reply = response?.Text?.Trim() ?? "OK";
            return (true, $"Success! Model responded: \"{reply}\"", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, $"Connection failed: {ex.Message}", sw.Elapsed);
        }
    }

    /// <inheritdoc />
    public IChatClient CreateChatClient(AiSettingsModel settings)
    {
        switch (settings.SelectedProvider)
        {
            case AiProviderType.OllamaLocal:
            {
                string endpoint = string.IsNullOrWhiteSpace(settings.OllamaEndpoint)
                    ? "http://localhost:11434"
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

                // OllamaApiClient implements IChatClient directly from Microsoft.Extensions.AI
                return new OllamaApiClient(new Uri(endpoint), model);
            }

            case AiProviderType.OpenAiCloud:
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

            case AiProviderType.CustomOpenAiCompatible:
            {
                string apiKey = string.IsNullOrWhiteSpace(settings.OpenAiApiKey)
                    ? "dummy-key"
                    : settings.OpenAiApiKey.Trim();

                string model = string.IsNullOrWhiteSpace(settings.CustomModelName)
                    ? (string.IsNullOrWhiteSpace(settings.SelectedModelId) ? "llama-3.3-70b-versatile" : settings.SelectedModelId)
                    : settings.CustomModelName.Trim();

                string baseUrl = string.IsNullOrWhiteSpace(settings.CustomBaseUrl)
                    ? "https://api.openai.com/v1"
                    : settings.CustomBaseUrl.Trim();

                var client = new ChatClient(
                    model,
                    new ApiKeyCredential(apiKey),
                    new OpenAIClientOptions { Endpoint = new Uri(baseUrl) });

                return client.AsIChatClient();
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(settings.SelectedProvider));
        }
    }

    private static string FormatModelDisplayName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "Unknown";
        string clean = rawName.Replace(":latest", "");
        return clean switch
        {
            "llama3.2" => "Llama 3.2 (Meta)",
            "llama3.1" => "Llama 3.1 (Meta)",
            "mistral" => "Mistral 7B",
            "phi3" => "Phi-3 Mini (Microsoft)",
            "phi4" => "Phi-4 (Microsoft)",
            "gemma2" => "Gemma 2 (Google)",
            "deepseek-r1" => "DeepSeek-R1 (Reasoning)",
            "qwen2.5" => "Qwen 2.5",
            _ => Capitalize(clean)
        };
    }

    private static string ExtractParamSizeFromName(string name)
    {
        if (name.Contains("3.2", StringComparison.OrdinalIgnoreCase)) return "3B";
        if (name.Contains("70b", StringComparison.OrdinalIgnoreCase)) return "70B";
        if (name.Contains("8b", StringComparison.OrdinalIgnoreCase)) return "8B";
        if (name.Contains("7b", StringComparison.OrdinalIgnoreCase)) return "7B";
        if (name.Contains("14b", StringComparison.OrdinalIgnoreCase)) return "14B";
        if (name.Contains("3b", StringComparison.OrdinalIgnoreCase)) return "3B";
        if (name.Contains("1b", StringComparison.OrdinalIgnoreCase)) return "1B";
        return string.Empty;
    }

    private static string Capitalize(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpperInvariant(str[0]) + str[1..];
    }

    // JSON DTO for Ollama /api/tags
    private sealed class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelDto>? Models { get; set; }
    }

    private sealed class OllamaModelDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("remote_model")]
        public string? RemoteModel { get; set; }

        [JsonPropertyName("remote_host")]
        public string? RemoteHost { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("details")]
        public OllamaDetailsDto? Details { get; set; }

        [JsonPropertyName("capabilities")]
        public List<string>? Capabilities { get; set; }
    }

    private sealed class OllamaDetailsDto
    {
        [JsonPropertyName("family")]
        public string? Family { get; set; }

        [JsonPropertyName("families")]
        public List<string>? Families { get; set; }

        [JsonPropertyName("parameter_size")]
        public string? ParameterSize { get; set; }

        [JsonPropertyName("quantization_level")]
        public string? QuantizationLevel { get; set; }

        [JsonPropertyName("context_length")]
        public long? ContextLength { get; set; }
    }
}
