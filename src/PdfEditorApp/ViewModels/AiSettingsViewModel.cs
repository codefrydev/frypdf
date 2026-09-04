using System;
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

            bool isRemote = !string.IsNullOrWhiteSpace(OllamaEndpoint) &&
                            !OllamaEndpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
                            !OllamaEndpoint.Contains("127.0.0.1");

            return AiModelInfo.CreateForCustomId(SelectedModelId, SelectedProvider, isRemote);
        }
    }

    public AiSettingsViewModel() : this(new UiSettingsService(), new AiService())
    {
    }

    public AiSettingsViewModel(IUiSettingsService uiSettingsService, IAiService aiService)
    {
        _uiSettingsService = uiSettingsService ?? throw new ArgumentNullException(nameof(uiSettingsService));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));

        LoadFromSettings(_uiSettingsService.Settings.AiSettings);
        _uiSettingsService.SettingsChanged += s => LoadFromSettings(s.AiSettings);

        // Auto-discover models from Ollama daemon on startup
        _ = DiscoverModelsAsync();
    }

    private bool _isSuppressingSave;

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

            RefreshModelCatalog(s);
        }
        finally
        {
            _isSuppressingSave = false;
        }
    }

    public void RefreshModelCatalog(AiSettingsModel? currentSettings = null)
    {
        var settings = currentSettings ?? BuildSettingsModel();
        var catalog = _aiService.GetUnifiedModelCatalog(settings);

        AvailableModels.Clear();
        foreach (var m in catalog)
        {
            AvailableModels.Add(m);
        }

        PopularModelSuggestions.Clear();
        foreach (var m in AvailableModels.Take(8))
        {
            PopularModelSuggestions.Add(m.Id);
        }

        // Match selected model
        SelectedModel = AvailableModels.FirstOrDefault(m => m.Id.Equals(SelectedModelId, StringComparison.OrdinalIgnoreCase))
                     ?? AvailableModels.FirstOrDefault();

        if (SelectedModel != null && string.IsNullOrWhiteSpace(SelectedModelId))
        {
            SelectedModelId = SelectedModel.Id;
        }

        OnPropertyChanged(nameof(ActiveModelInfo));
    }

    partial void OnSelectedProviderChanged(AiProviderType value)
    {
        OnPropertyChanged(nameof(IsOllamaProvider));
        OnPropertyChanged(nameof(IsOpenAiProvider));
        OnPropertyChanged(nameof(IsCustomProvider));
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
        if (!_isSuppressingSave) SaveSettings();
    }

    partial void OnCustomModelNameChanged(string value)
    {
        if (!_isSuppressingSave) SaveSettings();
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
            if (match != null && !ReferenceEquals(SelectedModel, match))
            {
                _isSuppressingSave = true;
                SelectedModel = match;
                _isSuppressingSave = false;
            }
        }

        OnPropertyChanged(nameof(ActiveModelInfo));
        if (!_isSuppressingSave) SaveSettings();
    }

    partial void OnSelectedModelChanged(AiModelInfo? value)
    {
        if (value != null)
        {
            if (!string.Equals(SelectedModelId, value.Id, StringComparison.OrdinalIgnoreCase))
            {
                SelectedModelId = value.Id;
            }
            OnPropertyChanged(nameof(ActiveModelInfo));
            if (!_isSuppressingSave) SaveSettings();
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
    public async Task DiscoverModelsAsync()
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
                    if (string.IsNullOrWhiteSpace(s.AiSettings.SelectedModelId) ||
                        !discovered.Any(m => m.Id.Equals(s.AiSettings.SelectedModelId, StringComparison.OrdinalIgnoreCase)))
                    {
                        s.AiSettings.SelectedModelId = discovered[0].Id;
                    }
                });

                RefreshModelCatalog(_uiSettingsService.Settings.AiSettings);
                TriggerToast($"Found {discovered.Count} local Ollama models!", ToastNotificationType.Success, "CheckCircleOutline");
            }
            else
            {
                TriggerToast("No Ollama models detected. Ensure Ollama is running (`ollama serve`).", ToastNotificationType.Warning, "AlertOutline");
            }
        }
        catch (Exception ex)
        {
            TriggerToast($"Ollama discovery error: {ex.Message}", ToastNotificationType.Danger, "AlertOctagonOutline");
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
            CustomBaseUrl = CustomBaseUrl,
            CustomModelName = CustomModelName,
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
            s.AiSettings.CustomBaseUrl = CustomBaseUrl;
            s.AiSettings.CustomModelName = CustomModelName;
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
