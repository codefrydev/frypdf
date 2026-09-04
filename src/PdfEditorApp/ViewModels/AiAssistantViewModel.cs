using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
/// ViewModel controlling the interactive AI Studio Assistant dialog/panel.
/// Powers natural language prompting, live tool invocation tracking, and atomic canvas undo.
/// </summary>
public partial class AiAssistantViewModel : ViewModelBase
{
    private readonly IPdfStudioAgentService _agentService;
    private readonly IUiSettingsService _uiSettingsService;
    private readonly IAiService _aiService;
    private CancellationTokenSource? _generationCts;

    public Func<PageViewModel?>? GetCurrentPage { get; set; }
    public IUndoRedoService? UndoRedo { get; set; }
    public Action? RequestOpenSettings { get; set; }

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _promptText = string.Empty;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string _statusMessage = "Ready to generate document elements";

    [ObservableProperty]
    private string _lastGenerationSummary = string.Empty;

    [ObservableProperty]
    private bool _canUndoLastGeneration;

    [ObservableProperty]
    private string _activeModelDisplayName = "Llama 3.2";

    [ObservableProperty]
    private string _activeModelTierBadge = "Free / Local";

    [ObservableProperty]
    private AiModelTier _activeModelTier = AiModelTier.FreeLocal;

    [ObservableProperty]
    private bool _isActiveModelFree = true;

    public bool IsActiveModelFreeLocal => ActiveModelTier == AiModelTier.FreeLocal;
    public bool IsActiveModelFreeCloud => ActiveModelTier == AiModelTier.FreeCloud;
    public bool IsActiveModelPaidCloud => ActiveModelTier == AiModelTier.PaidCloud;

    public ObservableCollection<string> ActivityLogs { get; } = new();
    public ObservableCollection<string> SuggestedPrompts { get; } = new();

    public AiAssistantViewModel(
        IPdfStudioAgentService agentService,
        IUiSettingsService uiSettingsService,
        IAiService aiService)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
        _uiSettingsService = uiSettingsService ?? throw new ArgumentNullException(nameof(uiSettingsService));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));

        PopulateDefaultSuggestions();
        UpdateActiveModelDisplay();

        _uiSettingsService.SettingsChanged += _ => UpdateActiveModelDisplay();
    }

    private void PopulateDefaultSuggestions()
    {
        SuggestedPrompts.Add("Add a modern corporate invoice header with company name Acme Corp, invoice #1001, date today, and a blue accent bar");
        SuggestedPrompts.Add("Create a 4-column financial summary table with columns: Quarter, Revenue, Expenses, Net Profit, and 4 sample rows");
        SuggestedPrompts.Add("Add an amber warning callout card with title 'Important Notice' and a 2-line explanation");
        SuggestedPrompts.Add("Add a green pill badge with text 'PAID & VERIFIED' and an adjacent QR code");
        SuggestedPrompts.Add("Design a modern certificate header with a classic Georgia title, gold accent divider, and recipient subtitle");
        SuggestedPrompts.Add("Add an elegant floral divider ornament with a centered title paragraph");
    }

    public void UpdateActiveModelDisplay()
    {
        var settings = _uiSettingsService.Settings.AiSettings;
        var catalog = _aiService.GetUnifiedModelCatalog(settings);
        var match = catalog.FirstOrDefault(m => m.Id.Equals(settings.SelectedModelId, StringComparison.OrdinalIgnoreCase))
                 ?? catalog.FirstOrDefault();

        if (match != null)
        {
            ActiveModelDisplayName = match.DisplayName;
            ActiveModelTier = match.Tier;
            ActiveModelTierBadge = match.TierBadgeText;
            IsActiveModelFree = match.IsFree;
        }
        else
        {
            ActiveModelDisplayName = settings.SelectedModelId;
            bool isOllama = settings.SelectedProvider == AiProviderType.OllamaLocal;
            bool isRemote = isOllama && settings.IsOllamaRemote;
            ActiveModelTier = isOllama ? (isRemote ? AiModelTier.FreeCloud : AiModelTier.FreeLocal) : AiModelTier.PaidCloud;
            ActiveModelTierBadge = ActiveModelTier switch
            {
                AiModelTier.FreeLocal => "Free / Local",
                AiModelTier.FreeCloud => "Free / Cloud",
                _ => "Paid / Cloud"
            };
            IsActiveModelFree = isOllama;
        }

        OnPropertyChanged(nameof(IsActiveModelFreeLocal));
        OnPropertyChanged(nameof(IsActiveModelFreeCloud));
        OnPropertyChanged(nameof(IsActiveModelPaidCloud));
    }

    [RelayCommand]
    public void Open()
    {
        UpdateActiveModelDisplay();
        IsOpen = true;
    }

    [RelayCommand]
    public void Close()
    {
        if (IsGenerating)
        {
            CancelGeneration();
        }
        IsOpen = false;
    }

    [RelayCommand]
    public void UsePrompt(string? prompt)
    {
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            PromptText = prompt;
        }
    }

    [RelayCommand]
    public async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(PromptText) || IsGenerating) return;

        var targetPage = GetCurrentPage?.Invoke();
        if (targetPage == null)
        {
            TriggerToast("No active document page found. Please open or create a page.", ToastNotificationType.Warning, "AlertOutline");
            return;
        }

        IsGenerating = true;
        CanUndoLastGeneration = false;
        StatusMessage = "Initializing AI agent...";
        ActivityLogs.Clear();
        ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] Prompt: \"{PromptText}\"");

        _generationCts = new CancellationTokenSource();

        try
        {
            var settings = _uiSettingsService.Settings.AiSettings;
            var result = await _agentService.ExecutePromptAsync(
                PromptText,
                targetPage,
                settings,
                msg =>
                {
                    StatusMessage = msg;
                    ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
                },
                _generationCts.Token);

            if (result.Success)
            {
                LastGenerationSummary = result.Message;
                StatusMessage = $"Completed! {result.ElementsCreatedCount} elements added.";
                CanUndoLastGeneration = true;
                ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] Finished successfully in {result.Duration.TotalSeconds:0.1}s");
                TriggerToast($"AI Studio generated {result.ElementsCreatedCount} elements!", ToastNotificationType.Success, "AutoFixHigh");
            }
            else
            {
                LastGenerationSummary = result.Message;
                StatusMessage = "Generation finished with notice.";
                ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] {result.Message}");
                TriggerToast(result.Message, ToastNotificationType.Warning, "InformationOutline");
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Generation cancelled.";
            ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] Generation was cancelled by user.");
            TriggerToast("AI Generation cancelled", ToastNotificationType.General, "CloseCircleOutline");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}");
            TriggerToast($"AI Error: {ex.Message}", ToastNotificationType.Danger, "AlertOctagonOutline");
        }
        finally
        {
            IsGenerating = false;
            _generationCts?.Dispose();
            _generationCts = null;
        }
    }

    [RelayCommand]
    public void CancelGeneration()
    {
        _generationCts?.Cancel();
    }

    [RelayCommand]
    public void UndoGeneration()
    {
        if (UndoRedo != null && UndoRedo.CanUndo)
        {
            string? undone = UndoRedo.Undo();
            CanUndoLastGeneration = false;
            StatusMessage = "AI elements reverted.";
            ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] Reverted action: {undone}");
            TriggerToast("Reverted AI-generated elements", ToastNotificationType.Primary, "Undo");
        }
    }

    [RelayCommand]
    public void OpenSettings()
    {
        IsOpen = false;
        RequestOpenSettings?.Invoke();
    }

    private void TriggerToast(string message, ToastNotificationType type, string icon)
    {
        WeakReferenceMessenger.Default.Send(new ShowToastMessage(message, type, icon));
    }
}
