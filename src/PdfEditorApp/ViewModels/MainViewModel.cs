using System;
using System.Collections.ObjectModel;
using System.IO;
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
    private readonly IDocumentAuditService _auditService;
    private readonly ISignatureService _signatureService;
    private readonly ISmartPlacementService _placementService;
    private readonly IRecentDocumentsService _recentService;

    public ISmartPlacementService SmartPlacement => _placementService;

    // --- HOME / EDITOR VIEW-SWITCHING ---

    [ObservableProperty]
    private bool _isHomePageVisible = true;

    [ObservableProperty]
    private bool _isEditorVisible = false;

    /// <summary>ViewModel for the Home / Start Screen.</summary>
    public HomeViewModel Home { get; }

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

    partial void OnCurrentPageChanged(PageViewModel? oldValue, PageViewModel? newValue)
    {
        if (oldValue != null)
        {
            oldValue.SelectionChanged -= OnElementSelectionChanged;
        }

        if (newValue != null)
        {
            newValue.SelectionChanged += OnElementSelectionChanged;
            Inspector.UpdateSelection(newValue.SelectedElement, newValue);
        }
        else
        {
            Inspector.UpdateSelection(null, null);
        }
    }

    // Canvas Grid & Snap-to-Grid
    [ObservableProperty]
    private bool _showGrid;

    [ObservableProperty]
    private bool _snapToGrid;

    [ObservableProperty]
    private GridSnapSize _gridSnapSize = GridSnapSize.Points20;

    // Toast HUD State
    [ObservableProperty]
    private string _toastMessage = "";

    [ObservableProperty]
    private string _toastIcon = "CheckCircleOutline";

    [ObservableProperty]
    private bool _isToastVisible;

    // Acrobat Suite Dialogs State
    [ObservableProperty]
    private bool _isSignatureStudioOpen;

    [ObservableProperty]
    private string _signatureSignerName = "Jane Doe";

    [ObservableProperty]
    private SignatureStyle _selectedSignatureStyle = SignatureStyle.CursiveElegance;

    [ObservableProperty]
    private bool _isSecurityDialogOpen;

    [ObservableProperty]
    private PdfSecuritySettings _securitySettings = new();

    [ObservableProperty]
    private bool _isPreflightDialogOpen;

    [ObservableProperty]
    private DocumentAuditReport? _activeAuditReport;

    [ObservableProperty]
    private bool _isHeaderFooterDialogOpen;

    [ObservableProperty]
    private string _headerLeftText = "";

    [ObservableProperty]
    private string _headerCenterText = "";

    [ObservableProperty]
    private string _headerRightText = "";

    [ObservableProperty]
    private string _footerLeftText = "CONFIDENTIAL & PROPRIETARY";

    [ObservableProperty]
    private string _footerCenterText = "";

    [ObservableProperty]
    private string _footerRightText = "Page {P} of {N}";

    [ObservableProperty]
    private bool _isWatermarkManagerOpen;

    [ObservableProperty]
    private string _watermarkPresetText = "CONFIDENTIAL";

    [ObservableProperty]
    private string _watermarkColorHex = "#DC2626";

    [ObservableProperty]
    private double _watermarkOpacity = 0.15;

    [ObservableProperty]
    private double _watermarkAngle = -35;

    [ObservableProperty]
    private bool _isSearchRedactDialogOpen;

    [ObservableProperty]
    private string _searchRedactQuery = "";

    [ObservableProperty]
    private string _selectedExemptionCode = "[REDACTED - (b)(4) PRIVILEGED]";

    [ObservableProperty]
    private bool _isCustomStampDialogOpen;

    [ObservableProperty]
    private string _customStampText = "CERTIFIED TRUE COPY";

    [ObservableProperty]
    private string _customStampColorHex = "#0F6CBD";

    // Bates Numbering Studio
    [ObservableProperty]
    private bool _isBatesNumberingDialogOpen;

    [ObservableProperty]
    private string _batesPrefix = "CONF-BATES-";

    [ObservableProperty]
    private string _batesSuffix = "";

    [ObservableProperty]
    private int _batesStartingNumber = 1;

    [ObservableProperty]
    private int _batesNumberOfDigits = 6;

    [ObservableProperty]
    private BatesPosition _batesPosition = BatesPosition.BottomLeft;

    [ObservableProperty]
    private string _batesFontColorHex = "#0F172A";

    [ObservableProperty]
    private double _batesFontSize = 9.0;

    // Organize Pages: Split & Extract Studio
    [ObservableProperty]
    private bool _isSplitExtractDialogOpen;

    [ObservableProperty]
    private SplitExtractMode _splitExtractMode = SplitExtractMode.SplitEveryNPages;

    [ObservableProperty]
    private int _splitPageInterval = 1;

    [ObservableProperty]
    private string _splitPageRanges = "1-2";

    // Document Comparison Diff Studio
    [ObservableProperty]
    private bool _isCompareDialogOpen;

    [ObservableProperty]
    private DocumentComparisonReport? _activeComparisonReport;

    // In-Canvas Interactive Find & Replace
    [ObservableProperty]
    private bool _isFindReplaceOpen;

    [ObservableProperty]
    private string _findQuery = "";

    [ObservableProperty]
    private string _replaceQuery = "";

    [ObservableProperty]
    private bool _findMatchCase;

    [ObservableProperty]
    private int _findMatchesCount;

    // Canvas Rulers & Measure Tool
    [ObservableProperty]
    private bool _showRulers = true;

    [ObservableProperty]
    private RulerUnit _rulerUnit = RulerUnit.Points;

    [ObservableProperty]
    private double _cursorCanvasX;

    [ObservableProperty]
    private double _cursorCanvasY;

    // Presentation / Read Mode & Theme
    [ObservableProperty]
    private bool _isPresentationMode;

    [ObservableProperty]
    private bool _isDarkMode;

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
    public string SecurityStatusDisplay => SecuritySettings.IsPasswordProtected ? "Protected (Password Required)" : "Standard (No Security)";

    // --- CONSTRUCTORS ---

    public MainViewModel() : this(new PdfExportService(), new TemplateService(), new ProjectPersistenceService(), new DocumentAuditService(), new SignatureService(), new SmartPlacementService(), new RecentDocumentsService())
    {
    }

    public MainViewModel(
        IPdfExportService exportService,
        ITemplateService templateService,
        IProjectPersistenceService persistenceService,
        IDocumentAuditService? auditService = null,
        ISignatureService? signatureService = null,
        ISmartPlacementService? placementService = null,
        IRecentDocumentsService? recentService = null)
    {
        _exportService = exportService;
        _templateService = templateService;
        _persistenceService = persistenceService;
        _auditService = auditService ?? new DocumentAuditService();
        _signatureService = signatureService ?? new SignatureService();
        _placementService = placementService ?? new SmartPlacementService();
        _recentService = recentService ?? new RecentDocumentsService();

        // Connect undo/redo service to inspector
        Inspector.UndoRedo = UndoRedo;

        // Set up Home page and wire its navigation events
        Home = new HomeViewModel(_recentService);
        Home.OpenTemplateRequested += OpenEditorWithTemplate;
        Home.OpenFileRequested += () => _ = OpenProjectAndEnterEditorAsync();
        Home.OpenRecentRequested += OpenEditorWithFile;

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

        // Initialize default document model
        var defaultDoc = _templateService.CreateAnnualReportTemplate();
        LoadFromDocumentModel(defaultDoc);
    }

    // --- HOME / EDITOR NAVIGATION ---

    /// <summary>Switches to the editor and loads the requested template.</summary>
    public void OpenEditorWithTemplate(string? templateName)
    {
        var model = string.IsNullOrWhiteSpace(templateName)
            ? _templateService.CreateBlankDocument()
            : _templateService.CreateTemplate(templateName);

        LoadFromDocumentModel(model);
        IsHomePageVisible = false;
        IsEditorVisible = true;
        ShowToast($"Created new document from {templateName ?? "Blank"} template", "FilePlusOutline");
    }

    /// <summary>Switches to the editor and loads a project from a file path.</summary>
    public void OpenEditorWithFile(string path)
    {
        try
        {
            var model = _persistenceService.LoadProjectAsync(path).GetAwaiter().GetResult();
            if (model != null)
            {
                LoadFromDocumentModel(model);
                _recentService.Add(new RecentDocumentItem
                {
                    FilePath = path,
                    Title = model.Title,
                    LastOpened = DateTime.UtcNow
                });
                Home.RefreshRecent();
                IsHomePageVisible = false;
                IsEditorVisible = true;
                ShowToast($"Opened: {Path.GetFileName(path)}", "FolderOpenOutline");
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Could not open file: {ex.Message}", "AlertCircleOutline");
        }
    }

    private async Task OpenProjectAndEnterEditorAsync()
    {
        try
        {
            if (StorageProvider != null)
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open FryPDF Project",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("FryPDF Project (*.frypdf, *.pdfproj, *.json)")
                        {
                            Patterns = new[] { "*.frypdf", "*.pdfproj", "*.json" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    OpenEditorWithFile(files[0].Path.LocalPath);
                }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Open error: {ex.Message}", "AlertCircleOutline");
        }
    }

    /// <summary>Returns to the Home page from the editor.</summary>
    [RelayCommand]
    public void NavigateToHome()
    {
        Home.RefreshRecent();
        IsEditorVisible = false;
        IsHomePageVisible = true;
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

            var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, clone.Width, clone.Height);
            clone.X = posX;
            clone.Y = posY;

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
                ElementKind.Measurement => new MeasurementElementViewModel(),
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

            var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, clone.Width, clone.Height);
            clone.X = posX;
            clone.Y = posY;

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
                ElementKind.Measurement => new MeasurementElementViewModel(),
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
