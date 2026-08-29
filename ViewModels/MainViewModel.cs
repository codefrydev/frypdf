using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IPdfExportService _exportService;
    private readonly ITemplateService _templateService;
    private readonly IProjectPersistenceService _persistenceService;

    private ElementViewModelBase? _clipboardElement;
    private CancellationTokenSource? _toastCts;

    // --- CORE OBSERVABLE PROPERTIES ---

    [ObservableProperty]
    private string _documentTitle = "Annual_Report_2026.pdf";

    [ObservableProperty]
    private string _documentAuthor = "ACME CORP.";

    [ObservableProperty]
    private string _documentSubject = "Fiscal Year 2026 Annual Report";

    [ObservableProperty]
    private RibbonTabKind _activeRibbonTab = RibbonTabKind.Edit;

    [ObservableProperty]
    private SidebarTabKind _activeSidebarTab = SidebarTabKind.Thumbnails;

    [ObservableProperty]
    private ToolMode _activeToolMode = ToolMode.Select;

    [ObservableProperty]
    private double _zoomLevel = 1.0; // 100%

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private PageViewModel? _currentPage;

    // Toast HUD State
    [ObservableProperty]
    private string _toastMessage = "";

    [ObservableProperty]
    private string _toastIcon = "CheckCircleOutline";

    [ObservableProperty]
    private bool _isToastVisible;

    // Collections
    public ObservableCollection<PageViewModel> Pages { get; } = new();
    public ObservableCollection<OutlineItem> OutlineItems { get; } = new();
    public ObservableCollection<CommentItem> CommentItems { get; } = new();

    // Child Subsystems
    public InspectorViewModel Inspector { get; } = new();
    public UndoRedoService UndoRedo { get; } = new();
    public static IStorageProvider? StorageProvider { get; set; }

    // Calculated Properties
    public int CurrentPageNumber => CurrentPage != null ? Pages.IndexOf(CurrentPage) + 1 : 1;
    public int TotalPagesCount => Pages.Count;
    public string PageDimensionsDisplay => CurrentPage != null ? $"{CurrentPage.Width:F0} × {CurrentPage.Height:F0} pt" : "800 × 1131 pt";

    // --- CONSTRUCTORS ---

    public MainViewModel() : this(new PdfExportService(), new TemplateService(), new ProjectPersistenceService())
    {
    }

    public MainViewModel(
        IPdfExportService exportService,
        ITemplateService templateService,
        IProjectPersistenceService persistenceService)
    {
        _exportService = exportService;
        _templateService = templateService;
        _persistenceService = persistenceService;

        // Connect undo/redo service to inspector
        Inspector.UndoRedo = UndoRedo;

        // Initialize document with default Annual Report template
        var defaultDoc = _templateService.CreateAnnualReportTemplate();
        LoadFromDocumentModel(defaultDoc);

        // Synchronize inspector when selection changes
        Inspector.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Inspector.SelectedElement))
            {
                OnPropertyChanged(nameof(CurrentPage));
            }
        };

        // Initialize quick command palette indexing
        InitCommandPalette();
    }

    public void OnElementSelectionChanged(ElementViewModelBase? selectedElement)
    {
        Inspector.UpdateSelection(selectedElement, CurrentPage);
    }

    // --- TOAST HUD NOTIFICATION FEEDBACK ---

    public void ShowToast(string message, string iconKind = "CheckCircleOutline")
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;

        ToastMessage = message;
        ToastIcon = iconKind;
        IsToastVisible = true;
        UpdateStatus(message);

        Task.Delay(2200, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                Dispatcher.UIThread.Post(() => IsToastVisible = false);
            }
        }, TaskScheduler.Default);
    }

    public void UpdateStatus(string message)
    {
        StatusMessage = message;
    }

    // --- TAB & TOOL MODE SELECTION ---

    [RelayCommand]
    public void SelectRibbonTab(RibbonTabKind tab)
    {
        ActiveRibbonTab = tab;
    }

    [RelayCommand]
    public void SetToolMode(string modeStr)
    {
        if (Enum.TryParse<ToolMode>(modeStr, true, out var parsed))
        {
            ActiveToolMode = parsed;
            ShowToast($"Active Tool: {parsed}", "CursorDefaultOutline");
        }
    }

    // --- UNDO / REDO & CLIPBOARD COMMANDS ---

    [RelayCommand]
    public void Undo()
    {
        if (UndoRedo.CanUndo)
        {
            var desc = UndoRedo.Undo();
            ShowToast($"Undone: {desc ?? "Action"}", "Undo");
        }
        else
        {
            ShowToast("Nothing to undo", "InformationOutline");
        }
    }

    [RelayCommand]
    public void Redo()
    {
        if (UndoRedo.CanRedo)
        {
            var desc = UndoRedo.Redo();
            ShowToast($"Redone: {desc ?? "Action"}", "Redo");
        }
        else
        {
            ShowToast("Nothing to redo", "InformationOutline");
        }
    }

    [RelayCommand]
    public void Copy()
    {
        if (CurrentPage?.SelectedElement != null)
        {
            _clipboardElement = CurrentPage.SelectedElement;
            ShowToast($"Copied: {_clipboardElement.DisplayName}", "ContentCopy");
        }
    }

    [RelayCommand]
    public void Cut()
    {
        if (CurrentPage?.SelectedElement != null)
        {
            _clipboardElement = CurrentPage.SelectedElement;
            var elToRemove = CurrentPage.SelectedElement;
            var page = CurrentPage;
            page.RemoveElement(elToRemove);

            UndoRedo.RecordAction(
                $"Cut {elToRemove.DisplayName}",
                () => page.AddElement(elToRemove),
                () => page.RemoveElement(elToRemove)
            );

            ShowToast($"Cut: {_clipboardElement.DisplayName}", "ContentCut");
        }
    }

    [RelayCommand]
    public void Paste()
    {
        if (_clipboardElement != null && CurrentPage != null)
        {
            var model = _clipboardElement.ToModel();
            var clone = model.Clone();
            clone.Id = Guid.NewGuid().ToString();
            clone.X += 20;
            clone.Y += 20;

            ElementViewModelBase? newVm = clone.Kind switch
            {
                ElementKind.Text => new TextElementViewModel(),
                ElementKind.Heading => new TextElementViewModel(),
                ElementKind.Shape => new ShapeElementViewModel(),
                ElementKind.Image => new ImageElementViewModel(),
                ElementKind.Divider => new DividerElementViewModel(),
                ElementKind.Table => new TableElementViewModel(),
                ElementKind.Chart => new ChartElementViewModel(),
                ElementKind.Watermark => new WatermarkElementViewModel(),
                ElementKind.FormField => new FormFieldElementViewModel(),
                ElementKind.QrCode => new QrCodeElementViewModel(),
                ElementKind.Barcode => new BarcodeElementViewModel(),
                ElementKind.Redaction => new RedactionElementViewModel(),
                ElementKind.Ink => new InkElementViewModel(),
                ElementKind.StickyNote => new StickyNoteElementViewModel(),
                _ => new TextElementViewModel()
            };

            newVm.LoadFromModel(clone);
            AddElementWithUndo(newVm, $"Paste {newVm.DisplayName}");
        }
    }

    [RelayCommand]
    public void Duplicate()
    {
        if (CurrentPage?.SelectedElement != null)
        {
            var model = CurrentPage.SelectedElement.ToModel();
            var clone = model.Clone();
            clone.Id = Guid.NewGuid().ToString();
            clone.X += 20;
            clone.Y += 20;

            ElementViewModelBase? newVm = clone.Kind switch
            {
                ElementKind.Text => new TextElementViewModel(),
                ElementKind.Heading => new TextElementViewModel(),
                ElementKind.Shape => new ShapeElementViewModel(),
                ElementKind.Image => new ImageElementViewModel(),
                ElementKind.Divider => new DividerElementViewModel(),
                ElementKind.Table => new TableElementViewModel(),
                ElementKind.Chart => new ChartElementViewModel(),
                ElementKind.Watermark => new WatermarkElementViewModel(),
                ElementKind.FormField => new FormFieldElementViewModel(),
                ElementKind.QrCode => new QrCodeElementViewModel(),
                ElementKind.Barcode => new BarcodeElementViewModel(),
                ElementKind.Redaction => new RedactionElementViewModel(),
                ElementKind.Ink => new InkElementViewModel(),
                ElementKind.StickyNote => new StickyNoteElementViewModel(),
                _ => new TextElementViewModel()
            };

            newVm.LoadFromModel(clone);
            AddElementWithUndo(newVm, $"Duplicate {newVm.DisplayName}");
        }
    }

    // --- ZOOM COMMANDS ---

    [RelayCommand]
    public void ZoomIn()
    {
        ZoomLevel = Math.Min(2.5, Math.Round(ZoomLevel + 0.1, 2));
        ShowToast($"Zoom: {(int)(ZoomLevel * 100)}%", "MagnifyPlusOutline");
    }

    [RelayCommand]
    public void ZoomOut()
    {
        ZoomLevel = Math.Max(0.4, Math.Round(ZoomLevel - 0.1, 2));
        ShowToast($"Zoom: {(int)(ZoomLevel * 100)}%", "MagnifyMinusOutline");
    }

    [RelayCommand]
    public void ResetZoom()
    {
        ZoomLevel = 1.0;
        ShowToast("Zoom Reset (100%)", "Magnify");
    }

    [RelayCommand]
    public void FitToWidth()
    {
        ZoomLevel = 1.15;
        ShowToast("Fit to Width", "ArrowExpandHorizontal");
    }

    [RelayCommand]
    public void FitToPage()
    {
        ZoomLevel = 0.85;
        ShowToast("Fit to Page", "FitToPageOutline");
    }
}
