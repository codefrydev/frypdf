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

using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel controlling the interactive AI Studio Assistant dialog/panel.
/// Powers natural language prompting, in-place element modifications, live tool invocation tracking, and atomic canvas undo.
/// </summary>
public partial class AiAssistantViewModel : ViewModelBase
{
    private readonly IPdfStudioAgentService _agentService;
    private readonly IUiSettingsService _uiSettingsService;
    private readonly IAiService _aiService;
    private CancellationTokenSource? _generationCts;

    public Func<PageViewModel?>? GetCurrentPage { get; set; }
    public Func<ElementViewModelBase?>? GetSelectedElement { get; set; }
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
    private ElementViewModelBase? _targetElement;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTargetElement))]
    private bool _isModifyMode;

    [ObservableProperty]
    private string _targetElementTitle = string.Empty;

    [ObservableProperty]
    private string _targetElementKindBadge = string.Empty;

    [ObservableProperty]
    private string _targetElementKindIcon = "AutoFixHigh";

    public bool HasTargetElement => TargetElement != null;

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
        var selected = GetSelectedElement?.Invoke();
        if (selected != null)
        {
            OpenForElement(selected);
            return;
        }

        TargetElement = null;
        IsModifyMode = false;
        PopulateDefaultSuggestions();
        IsOpen = true;
    }

    public void OpenForElement(ElementViewModelBase element)
    {
        UpdateActiveModelDisplay();
        TargetElement = element;
        IsModifyMode = true;
        TargetElementTitle = GetElementTitle(element);
        TargetElementKindBadge = element.Kind.ToString();
        TargetElementKindIcon = GetElementIcon(element);
        PopulateElementSuggestions(element);
        IsOpen = true;
    }

    [RelayCommand]
    public void SwitchToModifyMode()
    {
        if (TargetElement != null)
        {
            IsModifyMode = true;
            PopulateElementSuggestions(TargetElement);
        }
    }

    [RelayCommand]
    public void SwitchToCreateMode()
    {
        IsModifyMode = false;
        PopulateDefaultSuggestions();
    }

    private static string GetElementTitle(ElementViewModelBase element)
    {
        if (element is ChartElementViewModel chart)
        {
            return string.IsNullOrWhiteSpace(chart.Title) ? "Untitled Chart" : chart.Title;
        }
        if (element is TextElementViewModel text)
        {
            string t = text.Text?.Replace("\r", " ").Replace("\n", " ").Trim() ?? "";
            return string.IsNullOrWhiteSpace(t) ? "Text Element" : (t.Length <= 35 ? t : t[..35] + "...");
        }
        if (element is TableElementViewModel table)
        {
            return $"Table ({table.Rows.Count} rows, {table.Headers.Count} cols)";
        }
        if (element is ShapeElementViewModel shape)
        {
            return $"{shape.ShapeType} Shape";
        }
        if (element is ImageElementViewModel img)
        {
            return string.IsNullOrWhiteSpace(img.AltText) ? "Image Asset" : img.AltText;
        }
        if (element is MathElementViewModel math)
        {
            if (!string.IsNullOrWhiteSpace(math.PresetName))
            {
                return math.PresetName;
            }
            string f = math.Formula?.Trim() ?? "";
            return string.IsNullOrWhiteSpace(f) ? "Math Formula" : (f.Length <= 35 ? f : f[..35] + "...");
        }
        if (element is SvgElementViewModel)
        {
            return "Vector SVG Ornament";
        }
        if (element is DividerElementViewModel)
        {
            return "Divider Line";
        }
        if (element is FormFieldElementViewModel form)
        {
            return string.IsNullOrWhiteSpace(form.Label) ? "Form Field" : $"Field: {form.Label}";
        }
        if (element is QrCodeElementViewModel qr)
        {
            return string.IsNullOrWhiteSpace(qr.Content) ? "QR Code" : $"QR: {qr.Content}";
        }
        if (element is BarcodeElementViewModel barcode)
        {
            return string.IsNullOrWhiteSpace(barcode.CodeValue) ? "Barcode" : $"Barcode: {barcode.CodeValue}";
        }
        if (element is StickyNoteElementViewModel note)
        {
            string nt = note.NoteText?.Trim() ?? "";
            return string.IsNullOrWhiteSpace(nt) ? "Sticky Note" : (nt.Length <= 30 ? nt : nt[..30] + "...");
        }
        return $"{element.Kind} Element";
    }

    private static string GetElementIcon(ElementViewModelBase element)
    {
        return element switch
        {
            ChartElementViewModel => "ChartBoxOutline",
            TextElementViewModel => "FormatText",
            TableElementViewModel => "Table",
            ShapeElementViewModel => "ShapeOutline",
            ImageElementViewModel => "ImageOutline",
            MathElementViewModel => "Sigma",
            SvgElementViewModel => "VectorSquare",
            DividerElementViewModel => "Minus",
            FormFieldElementViewModel => "TextBoxOutline",
            QrCodeElementViewModel => "Qrcode",
            BarcodeElementViewModel => "Barcode",
            StickyNoteElementViewModel => "NoteTextOutline",
            _ => "AutoFixHigh"
        };
    }

    private void PopulateElementSuggestions(ElementViewModelBase element)
    {
        SuggestedPrompts.Clear();

        if (element is ChartElementViewModel)
        {
            SuggestedPrompts.Add("Switch chart type to a smooth Line chart");
            SuggestedPrompts.Add("Use Emerald Green / Mint color palette");
            SuggestedPrompts.Add("Add Q1 2027 Projections with +15% revenue growth");
            SuggestedPrompts.Add("Change to Donut Pie chart showing category share");
            SuggestedPrompts.Add("Sort categories in ascending order of values");
            SuggestedPrompts.Add("Convert to Horizontal Bar chart");
        }
        else if (element is TextElementViewModel)
        {
            SuggestedPrompts.Add("Polish tone to be executive, authoritative, and concise");
            SuggestedPrompts.Add("Format with modern styled checkmarks (✔)");
            SuggestedPrompts.Add("Convert into a clean numbered list (1., 2., 3.)");
            SuggestedPrompts.Add("Summarize into 3 key bullet points");
            SuggestedPrompts.Add("Make title bold, 20pt, with #0F6CBD accent color");
            SuggestedPrompts.Add("Fix grammar, punctuation, and improve phrasing");
            SuggestedPrompts.Add("Translate content into Spanish");
        }
        else if (element is TableElementViewModel)
        {
            SuggestedPrompts.Add("Add a Total summary row with computed column sums");
            SuggestedPrompts.Add("Format monetary columns with currency ($) and commas");
            SuggestedPrompts.Add("Change header theme to Emerald Green (#047857)");
            SuggestedPrompts.Add("Apply alternating row zebra shading");
        }
        else if (element is ShapeElementViewModel)
        {
            SuggestedPrompts.Add("Change to soft rounded card with 12pt corner radius and subtle shadow");
            SuggestedPrompts.Add("Set fill to light blue tint (#EFF6FF) with blue stroke (#2563EB)");
            SuggestedPrompts.Add("Make shape a circle with emerald green theme");
        }
        else if (element is MathElementViewModel)
        {
            SuggestedPrompts.Add("Add equation number (1.1) and right align");
            SuggestedPrompts.Add("Enlarge formula font to 18pt");
            SuggestedPrompts.Add("Set formula color to modern blue (#0F6CBD)");
            SuggestedPrompts.Add("Change to Pythagorean theorem: a^2 + b^2 = c^2");
            SuggestedPrompts.Add("Change to Quadratic formula: x = \\frac{-b \\pm \\sqrt{b^2 - 4ac}}{2a}");
        }
        else if (element is ImageElementViewModel)
        {
            SuggestedPrompts.Add("Set opacity to 80% for clean watermark presentation");
            SuggestedPrompts.Add("Make image semi-transparent (50% opacity)");
            SuggestedPrompts.Add("Add rounded border (8pt) with blue accent");
        }
        else if (element is SvgElementViewModel)
        {
            SuggestedPrompts.Add("Set tint color to Luxury Gold (#D97706)");
            SuggestedPrompts.Add("Set tint color to Modern Blue (#0F6CBD)");
            SuggestedPrompts.Add("Set tint color to Emerald Forest (#047857)");
        }
        else if (element is DividerElementViewModel)
        {
            SuggestedPrompts.Add("Make divider line thicker (2.5pt)");
            SuggestedPrompts.Add("Set divider color to Modern Blue (#0F6CBD)");
            SuggestedPrompts.Add("Set divider color to Soft Slate (#CBD5E1)");
        }
        else if (element is QrCodeElementViewModel)
        {
            SuggestedPrompts.Add("Update encoded URL to https://example.com");
            SuggestedPrompts.Add("Set dark color to Corporate Navy (#1E293B)");
        }
        else if (element is BarcodeElementViewModel)
        {
            SuggestedPrompts.Add("Update barcode code value");
            SuggestedPrompts.Add("Set bar color to Corporate Navy (#1E293B)");
        }
        else if (element is FormFieldElementViewModel)
        {
            SuggestedPrompts.Add("Make this a required field");
            SuggestedPrompts.Add("Set border color to Modern Blue (#0F6CBD)");
        }
        else if (element is StickyNoteElementViewModel)
        {
            SuggestedPrompts.Add("Update status to Approved");
            SuggestedPrompts.Add("Change note color to Soft Amber (#FEF3C7)");
            SuggestedPrompts.Add("Change note color to Mint Green (#DCFCE7)");
        }
        else
        {
            SuggestedPrompts.Add($"Enhance styling and appearance of this {element.Kind}");
            SuggestedPrompts.Add($"Resize and adjust alignment of this {element.Kind}");
        }
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

        IsGenerating = true;
        CanUndoLastGeneration = false;
        ActivityLogs.Clear();
        ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] Prompt: \"{PromptText}\"");

        _generationCts = new CancellationTokenSource();

        try
        {
            var settings = _uiSettingsService.Settings.AiSettings;

            if (IsModifyMode && TargetElement != null)
            {
                StatusMessage = $"Modifying {TargetElementKindBadge}...";
                var result = await _agentService.ModifyElementAsync(
                    TargetElement,
                    PromptText,
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
                    StatusMessage = $"Completed! {result.Message}";
                    CanUndoLastGeneration = true;
                    ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] Finished successfully in {result.Duration.TotalSeconds:0.1}s");
                    TriggerToast($"✨ AI updated {TargetElementKindBadge}!", ToastNotificationType.Success, "AutoFixHigh");
                    TargetElementTitle = GetElementTitle(TargetElement);
                }
                else
                {
                    LastGenerationSummary = result.Message;
                    StatusMessage = "Modification finished with notice.";
                    ActivityLogs.Add($"[{DateTime.Now:HH:mm:ss}] {result.Message}");
                    TriggerToast(result.Message, ToastNotificationType.Warning, "InformationOutline");
                }
            }
            else
            {
                var targetPage = GetCurrentPage?.Invoke();
                if (targetPage == null)
                {
                    TriggerToast("No active document page found. Please open or create a page.", ToastNotificationType.Warning, "AlertOutline");
                    return;
                }

                StatusMessage = "Initializing AI agent...";
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
