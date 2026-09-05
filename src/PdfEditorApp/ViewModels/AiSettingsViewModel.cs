using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Messages;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.AI;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel managing AI provider configuration, Ollama local model discovery, and model selection.
/// </summary>
public partial class AiSettingsViewModel : ViewModelBase
{
    private readonly IUiSettingsService _uiSettingsService;
    private readonly IAiService _aiService;

    [ObservableProperty]
    private AiProviderType _selectedProvider = AiProviderType.OllamaLocal;

    [ObservableProperty]
    private string _ollamaEndpoint = "http://localhost:11434";

    [ObservableProperty]
    private string _ollamaApiKey = string.Empty;

    [ObservableProperty]
    private string _selectedModelId = "llama3.2";

    [ObservableProperty]
    private string _openAiApiKey = string.Empty;

    [ObservableProperty]
    private string _customBaseUrl = string.Empty;

    [ObservableProperty]
    private string _customModelName = string.Empty;

    [ObservableProperty]
    private float _temperature = 0.7f;

    [ObservableProperty]
    private string _systemInstructions = string.Empty;

    [ObservableProperty]
    private bool _isDetectingModels;

    [ObservableProperty]
    private bool _isFetchingLibrary;

    [ObservableProperty]
    private string _ollamaLibrarySearchQuery = string.Empty;

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private string _testConnectionStatus = string.Empty;

    [ObservableProperty]
    private bool _testConnectionSuccess;

    [ObservableProperty]
    private bool _hasTestedConnection;

    [ObservableProperty]
    private AiModelInfo? _selectedModel;

    public ObservableCollection<AiModelInfo> AvailableModels { get; } = new();

    public ObservableCollection<string> PopularModelSuggestions { get; } = new();

    public ObservableCollection<string> CustomModelHistory { get; } = new();

    public bool IsOllamaProvider => SelectedProvider == AiProviderType.OllamaLocal;
    public bool IsOpenAiProvider => SelectedProvider == AiProviderType.OpenAiCloud;
    public bool IsCustomProvider => SelectedProvider == AiProviderType.CustomOpenAiCompatible;

    /// <summary>
    /// Guaranteed non-null metadata for the currently active model (whether picked from library or custom typed).
    /// </summary>
    public AiModelInfo ActiveModelInfo
    {
        get
        {
            if (SelectedModel != null && string.Equals(SelectedModel.Id, SelectedModelId, StringComparison.OrdinalIgnoreCase))
            {
                return SelectedModel;
            }

            var match = AvailableModels.FirstOrDefault(m => m.Id.Equals(SelectedModelId, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }

            bool isRemote = SelectedProvider switch
            {
                AiProviderType.OllamaLocal => !string.IsNullOrWhiteSpace(OllamaEndpoint) &&
                                              !OllamaEndpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
                                              !OllamaEndpoint.Contains("127.0.0.1"),
                AiProviderType.CustomOpenAiCompatible => !string.IsNullOrWhiteSpace(CustomBaseUrl) &&
                                                         !CustomBaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
                                                         !CustomBaseUrl.Contains("127.0.0.1"),
                _ => true
            };

            string? endpoint = SelectedProvider == AiProviderType.CustomOpenAiCompatible ? CustomBaseUrl : OllamaEndpoint;
            return AiModelInfo.CreateForCustomId(SelectedModelId, SelectedProvider, isRemote, endpoint);
        }
    }

    /// <summary>
    /// Contextual placeholder text for the active model input field.
    /// </summary>
    public string ModelInputPlaceholder => SelectedProvider switch
    {
        AiProviderType.OllamaLocal => "Type model name (e.g. llama3.2, mistral, deepseek-r1, qwen2.5)...",
        AiProviderType.OpenAiCloud => "Type model name (e.g. gpt-4o, gpt-4o-mini, o3-mini)...",
        _ => "Type ANY model name (e.g. openai/gpt-oss-120b, llama-3.3-70b-versatile, qwen/qwen3.6-27b)..."
    };

    /// <summary>
    /// Subtitle description for Section C tailored to the active AI provider.
    /// </summary>
    public string ModelSectionSubtitle => SelectedProvider switch
    {
        AiProviderType.OllamaLocal => "Pick from installed models or enter any model tag from the Ollama library.",
        AiProviderType.OpenAiCloud => "Select an official OpenAI model or enter an enterprise/fine-tuned model ID.",
        _ => "Enter any custom model identifier supported by your endpoint. Future models are saved dynamically."
    };

    public AiSettingsViewModel() : this(new UiSettingsService(), new AiService())
    {
    }

    public AiSettingsViewModel(IUiSettingsService uiSettingsService, IAiService aiService)
    {
        _uiSettingsService = uiSettingsService ?? throw new ArgumentNullException(nameof(uiSettingsService));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));

        LoadFromSettings(_uiSettingsService.Settings.AiSettings);
        _uiSettingsService.SettingsChanged += s => LoadFromSettings(s.AiSettings);

        // Auto-discover models from Ollama daemon on startup (silent)
        _ = DiscoverModelsCoreAsync(showToast: false);
    }

    private bool _isSuppressingSave;
    private bool _isSyncingModelProperties;

    private void LoadFromSettings(AiSettingsModel s)
    {
        _isSuppressingSave = true;
        try
        {
            SelectedProvider = s.SelectedProvider;
            OllamaEndpoint = s.OllamaEndpoint;
            OllamaApiKey = s.OllamaApiKey;
            SelectedModelId = s.SelectedModelId;
            OpenAiApiKey = s.OpenAiApiKey;
            CustomBaseUrl = s.CustomBaseUrl;
            CustomModelName = s.CustomModelName;
            Temperature = s.Temperature;
            SystemInstructions = s.SystemInstructions;

            CustomModelHistory.Clear();
            if (s.CustomModelHistory != null && s.CustomModelHistory.Count > 0)
            {
                foreach (var m in s.CustomModelHistory)
                {
                    if (!string.IsNullOrWhiteSpace(m) && !CustomModelHistory.Contains(m.Trim(), StringComparer.OrdinalIgnoreCase))
                    {
                        CustomModelHistory.Add(m.Trim());
                    }
                }
            }

            RefreshModelCatalog(s);
        }
        finally
        {
            _isSuppressingSave = false;
        }
    }

    public void RememberCustomModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return;
        string clean = modelId.Trim();
        if (!CustomModelHistory.Contains(clean, StringComparer.OrdinalIgnoreCase))
        {
            CustomModelHistory.Insert(0, clean);
            if (!_isSuppressingSave)
            {
                SaveSettings();
            }
        }
    }

    [RelayCommand]
    public void ApplyPreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset)) return;

        switch (preset.ToLowerInvariant())
        {
            case "groq":
                CustomBaseUrl = "https://api.groq.com/openai/v1";
                SelectedModelId = "openai/gpt-oss-120b";
                CustomModelName = "openai/gpt-oss-120b";
                RememberCustomModel("openai/gpt-oss-120b");
                TriggerToast("Preset applied: Groq (https://api.groq.com/openai/v1)", ToastNotificationType.Primary, "Flash");
                break;

            case "openrouter":
                CustomBaseUrl = "https://openrouter.ai/api/v1";
                SelectedModelId = "meta-llama/llama-3.2-3b-instruct:free";
                CustomModelName = "meta-llama/llama-3.2-3b-instruct:free";
                RememberCustomModel("meta-llama/llama-3.2-3b-instruct:free");
                TriggerToast("Preset applied: OpenRouter (https://openrouter.ai/api/v1)", ToastNotificationType.Primary, "CloudOutline");
                break;

            case "together":
                CustomBaseUrl = "https://api.together.xyz/v1";
                SelectedModelId = "meta-llama/Llama-3.3-70B-Instruct-Turbo";
                CustomModelName = "meta-llama/Llama-3.3-70B-Instruct-Turbo";
                RememberCustomModel("meta-llama/Llama-3.3-70B-Instruct-Turbo");
                TriggerToast("Preset applied: Together AI", ToastNotificationType.Primary, "Api");
                break;

            case "lmstudio":
                CustomBaseUrl = "http://localhost:1234/v1";
                SelectedModelId = "local-model";
                CustomModelName = "local-model";
                TriggerToast("Preset applied: LM Studio (http://localhost:1234/v1)", ToastNotificationType.Primary, "Laptop");
                break;
        }

        RefreshModelCatalog();
    }

    public void RefreshModelCatalog(AiSettingsModel? currentSettings = null)
    {
        var settings = currentSettings ?? BuildSettingsModel();
        var catalog = _aiService.GetUnifiedModelCatalog(settings);

        AvailableModels.Clear();
        PopularModelSuggestions.Clear();

        if (IsOllamaProvider)
        {
            // Only Ollama local/cloud models
            foreach (var m in catalog.Where(x => x.Provider == AiProviderType.OllamaLocal))
            {
                AvailableModels.Add(m);
            }

            foreach (var m in AvailableModels.Take(8))
            {
                if (!string.IsNullOrEmpty(m?.Id))
                {
                    PopularModelSuggestions.Add(m.Id);
                }
            }
        }
        else if (IsOpenAiProvider)
        {
            // OpenAI models + cloud models
            foreach (var m in catalog.Where(x => x.Provider != AiProviderType.OllamaLocal))
            {
                AvailableModels.Add(m);
            }

            PopularModelSuggestions.Add("gpt-4o-mini");
            PopularModelSuggestions.Add("gpt-4o");
            PopularModelSuggestions.Add("o1-mini");
            PopularModelSuggestions.Add("o3-mini");
        }
        else // IsCustomProvider
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. User's saved custom models from history (dynamic - never need to touch codebase)
            foreach (var customM in CustomModelHistory.ToList())
            {
                if (seenIds.Add(customM))
                {
                    AvailableModels.Add(AiModelInfo.CreateForCustomId(customM, AiProviderType.CustomOpenAiCompatible, isEndpointRemote: true));
                }
            }

            // 2. Cloud-compatible models from catalog (Groq, OpenRouter) - NO Ollama models
            foreach (var m in catalog.Where(x => x.Provider != AiProviderType.OllamaLocal))
            {
                if (seenIds.Add(m.Id))
                {
                    AvailableModels.Add(m);
                }
            }

            // 3. Contextual Quick Picks based on entered endpoint
            string baseUrl = (CustomBaseUrl ?? string.Empty).ToLowerInvariant();
            if (baseUrl.Contains("openrouter"))
            {
                PopularModelSuggestions.Add("meta-llama/llama-3.2-3b-instruct:free");
                PopularModelSuggestions.Add("deepseek/deepseek-r1");
                PopularModelSuggestions.Add("google/gemini-2.0-flash-exp:free");
                PopularModelSuggestions.Add("mistralai/mistral-7b-instruct:free");
            }
            else // Default to Groq / high-speed LPU suggestions
            {
                PopularModelSuggestions.Add("openai/gpt-oss-120b");
                PopularModelSuggestions.Add("llama-3.3-70b-versatile");
                PopularModelSuggestions.Add("qwen/qwen-2.5-coder-32b");
                PopularModelSuggestions.Add("deepseek-r1-distill-llama-70b");
                PopularModelSuggestions.Add("llama-3.1-8b-instant");
            }

            // Append any user custom models to quick picks
            var existingSuggestions = new HashSet<string>(PopularModelSuggestions, StringComparer.OrdinalIgnoreCase);
            foreach (var customM in CustomModelHistory.ToList().Take(4))
            {
                if (existingSuggestions.Add(customM))
                {
                    PopularModelSuggestions.Add(customM);
                }
            }
        }

        // Match selected model from catalog if present
        var match = AvailableModels.FirstOrDefault(m => m != null && string.Equals(m.Id, SelectedModelId, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            SelectedModel = match;
        }
        else if (SelectedModel == null && !IsCustomProvider)
        {
            SelectedModel = AvailableModels.FirstOrDefault();
            if (SelectedModel != null && (string.IsNullOrWhiteSpace(SelectedModelId) || SelectedModelId == "llama3.2"))
            {
                SelectedModelId = SelectedModel.Id;
            }
        }
        else if (match == null)
        {
            SelectedModel = null;
        }

        OnPropertyChanged(nameof(ActiveModelInfo));
    }

    partial void OnSelectedProviderChanged(AiProviderType value)
    {
        OnPropertyChanged(nameof(IsOllamaProvider));
        OnPropertyChanged(nameof(IsOpenAiProvider));
        OnPropertyChanged(nameof(IsCustomProvider));
        OnPropertyChanged(nameof(ModelInputPlaceholder));
        OnPropertyChanged(nameof(ModelSectionSubtitle));
        OnPropertyChanged(nameof(ActiveModelInfo));
        if (!_isSuppressingSave)
        {
            SaveSettings();
            RefreshModelCatalog();
        }
    }

    partial void OnOllamaEndpointChanged(string value)
    {
        if (!_isSuppressingSave) SaveSettings();
    }

    partial void OnOllamaApiKeyChanged(string value)
    {
        if (!_isSuppressingSave) SaveSettings();
    }

    partial void OnOpenAiApiKeyChanged(string value)
    {
        if (!_isSuppressingSave) SaveSettings();
    }

    partial void OnCustomBaseUrlChanged(string value)
    {
        OnPropertyChanged(nameof(ActiveModelInfo));
        if (!_isSuppressingSave)
        {
            SaveSettings();
            RefreshModelCatalog();
        }
    }

    partial void OnCustomModelNameChanged(string value)
    {
        if (_isSuppressingSave || _isSyncingModelProperties) return;

        try
        {
            _isSyncingModelProperties = true;
            if (IsCustomProvider && !string.IsNullOrWhiteSpace(value) && !string.Equals(SelectedModelId, value, StringComparison.OrdinalIgnoreCase))
            {
                SelectedModelId = value.Trim();
                SelectedModel = AvailableModels.FirstOrDefault(m => m.Id.Equals(SelectedModelId, StringComparison.OrdinalIgnoreCase));
                OnPropertyChanged(nameof(ActiveModelInfo));
            }
        }
        finally
        {
            _isSyncingModelProperties = false;
        }

        SaveSettings();
    }

    partial void OnTemperatureChanged(float value)
    {
        if (!_isSuppressingSave) SaveSettings();
    }

    partial void OnSystemInstructionsChanged(string value)
    {
        if (!_isSuppressingSave) SaveSettings();
    }

    partial void OnSelectedModelIdChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var match = AvailableModels.FirstOrDefault(m => m.Id.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
            if (!ReferenceEquals(SelectedModel, match))
            {
                _isSuppressingSave = true;
                SelectedModel = match;
                _isSuppressingSave = false;
            }

            if (!_isSyncingModelProperties && IsCustomProvider && !string.Equals(CustomModelName, value, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _isSyncingModelProperties = true;
                    CustomModelName = value.Trim();
                }
                finally
                {
                    _isSyncingModelProperties = false;
                }
            }
        }

        OnPropertyChanged(nameof(ActiveModelInfo));
        if (!_isSuppressingSave) SaveSettings();
    }

    partial void OnSelectedModelChanged(AiModelInfo? value)
    {
        if (value != null && !_isSuppressingSave)
        {
            if (!string.Equals(SelectedModelId, value.Id, StringComparison.OrdinalIgnoreCase))
            {
                SelectedModelId = value.Id;
            }
            OnPropertyChanged(nameof(ActiveModelInfo));
            SaveSettings();
        }
    }

    [RelayCommand]
    public void SelectModel(object? param)
    {
        if (param is AiModelInfo model)
        {
            SelectedModel = model;
            SelectedModelId = model.Id;
        }
        else if (param is string modelId && !string.IsNullOrWhiteSpace(modelId))
        {
            SelectedModelId = modelId.Trim();
            var match = AvailableModels.FirstOrDefault(m => m.Id.Equals(SelectedModelId, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                SelectedModel = match;
            }
        }

        if (IsCustomProvider && !string.IsNullOrWhiteSpace(SelectedModelId))
        {
            RememberCustomModel(SelectedModelId);
        }
    }

    [RelayCommand]
    public void SaveCustomModel(string? modelId)
    {
        var target = string.IsNullOrWhiteSpace(modelId) ? SelectedModelId : modelId;
        if (!string.IsNullOrWhiteSpace(target))
        {
            RememberCustomModel(target);
            RefreshModelCatalog();
            TriggerToast($"Model '{target.Trim()}' saved to history!", ToastNotificationType.Success, "BookmarkCheckOutline");
        }
    }

    [RelayCommand]
    public void SetProvider(object? param)
    {
        AiProviderType provider;
        if (param is AiProviderType p) provider = p;
        else if (param is string s && Enum.TryParse<AiProviderType>(s, true, out var parsed)) provider = parsed;
        else return;

        SelectedProvider = provider;
        TriggerToast($"AI provider switched to {GetProviderTitle(provider)}", ToastNotificationType.Primary, "RobotOutline");
    }

    [RelayCommand]
    public Task DiscoverModelsAsync() => DiscoverModelsCoreAsync(showToast: true);

    private async Task DiscoverModelsCoreAsync(bool showToast)
    {
        IsDetectingModels = true;
        try
        {
            var discovered = await _aiService.DiscoverOllamaModelsAsync(OllamaEndpoint);

            if (discovered.Count > 0)
            {
                _uiSettingsService.UpdateSettings(s =>
                {
                    s.AiSettings.DiscoveredOllamaModels.Clear();
                    s.AiSettings.DiscoveredOllamaModels.AddRange(discovered);
                    if (s.AiSettings.SelectedProvider == AiProviderType.OllamaLocal &&
                        (string.IsNullOrWhiteSpace(s.AiSettings.SelectedModelId) || s.AiSettings.SelectedModelId == "llama3.2"))
                    {
                        s.AiSettings.SelectedModelId = discovered[0].Id;
                    }
                });

                RefreshModelCatalog(_uiSettingsService.Settings.AiSettings);
                if (showToast)
                {
                    TriggerToast($"Found {discovered.Count} local Ollama models!", ToastNotificationType.Success, "CheckCircleOutline");
                }
            }
            else if (showToast)
            {
                TriggerToast("No Ollama models detected. Ensure Ollama is running (`ollama serve`).", ToastNotificationType.Warning, "AlertOutline");
            }
        }
        catch (Exception ex)
        {
            if (showToast)
            {
                TriggerToast($"Ollama discovery error: {ex.Message}", ToastNotificationType.Danger, "AlertOctagonOutline");
            }
        }
        finally
        {
            IsDetectingModels = false;
        }
    }

    [RelayCommand]
    public async Task FetchOnlineLibraryAsync()
    {
        IsFetchingLibrary = true;
        try
        {
            var onlineModels = await _aiService.FetchOllamaOnlineLibraryAsync(OllamaLibrarySearchQuery);
            if (onlineModels.Count > 0)
            {
                _uiSettingsService.UpdateSettings(s =>
                {
                    s.AiSettings.OllamaLibraryCache.Clear();
                    s.AiSettings.OllamaLibraryCache.AddRange(onlineModels);
                });

                RefreshModelCatalog(_uiSettingsService.Settings.AiSettings);
                TriggerToast($"Discovered {onlineModels.Count} models directly from Ollama library!", ToastNotificationType.Success, "CloudDownloadOutline");
            }
            else
            {
                TriggerToast("No online models retrieved. Check internet connection.", ToastNotificationType.Warning, "AlertOutline");
            }
        }
        catch (Exception ex)
        {
            TriggerToast($"Error fetching Ollama library: {ex.Message}", ToastNotificationType.Danger, "AlertOctagonOutline");
        }
        finally
        {
            IsFetchingLibrary = false;
        }
    }

    [RelayCommand]
    public async Task TestConnectionAsync()
    {
        IsTestingConnection = true;
        HasTestedConnection = false;
        TestConnectionStatus = "Connecting...";

        try
        {
            var settings = BuildSettingsModel();
            var (success, msg, latency) = await _aiService.TestConnectionAsync(settings);

            HasTestedConnection = true;
            TestConnectionSuccess = success;
            TestConnectionStatus = success
                ? $"Connected ({latency.TotalMilliseconds:0}ms): {msg}"
                : $"Failed ({latency.TotalMilliseconds:0}ms): {msg}";

            if (success && IsCustomProvider && !string.IsNullOrWhiteSpace(SelectedModelId))
            {
                RememberCustomModel(SelectedModelId);
            }

            TriggerToast(
                success ? "AI model connection verified!" : "Connection test failed.",
                success ? ToastNotificationType.Success : ToastNotificationType.Danger,
                success ? "CheckCircleOutline" : "AlertOctagonOutline");
        }
        catch (Exception ex)
        {
            HasTestedConnection = true;
            TestConnectionSuccess = false;
            TestConnectionStatus = $"Error: {ex.Message}";
            TriggerToast($"Test error: {ex.Message}", ToastNotificationType.Danger, "AlertOctagonOutline");
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    private AiSettingsModel BuildSettingsModel()
    {
        return new AiSettingsModel
        {
            SelectedProvider = SelectedProvider,
            OllamaEndpoint = OllamaEndpoint,
            OllamaApiKey = OllamaApiKey,
            SelectedModelId = SelectedModelId,
            OpenAiApiKey = OpenAiApiKey,
            CustomBaseUrl = AiService.NormalizeCustomOpenAiBaseUrl(CustomBaseUrl),
            CustomModelName = CustomModelName,
            CustomModelHistory = CustomModelHistory.ToList(),
            Temperature = Temperature,
            SystemInstructions = SystemInstructions,
            DiscoveredOllamaModels = _uiSettingsService.Settings.AiSettings.DiscoveredOllamaModels
        };
    }

    private void SaveSettings()
    {
        _uiSettingsService.UpdateSettings(s =>
        {
            s.AiSettings.SelectedProvider = SelectedProvider;
            s.AiSettings.OllamaEndpoint = OllamaEndpoint;
            s.AiSettings.OllamaApiKey = OllamaApiKey;
            s.AiSettings.SelectedModelId = SelectedModelId;
            s.AiSettings.OpenAiApiKey = OpenAiApiKey;
            s.AiSettings.CustomBaseUrl = AiService.NormalizeCustomOpenAiBaseUrl(CustomBaseUrl);
            s.AiSettings.CustomModelName = CustomModelName;
            s.AiSettings.CustomModelHistory = CustomModelHistory.ToList();
            s.AiSettings.Temperature = Temperature;
            s.AiSettings.SystemInstructions = SystemInstructions;
        });
    }

    private void TriggerToast(string message, ToastNotificationType type, string icon)
    {
        WeakReferenceMessenger.Default.Send(new ShowToastMessage(message, type, icon));
    }

    private static string GetProviderTitle(AiProviderType provider) => provider switch
    {
        AiProviderType.OllamaLocal => "Local Ollama",
        AiProviderType.OpenAiCloud => "OpenAI Cloud",
        AiProviderType.CustomOpenAiCompatible => "Custom OpenAI-Compatible",
        _ => "AI Provider"
    };
}
