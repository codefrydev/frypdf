using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Intelligence;
using PdfEditorApp.ViewModels.Tools.Core;
using PdfEditorApp.Core.Data;
using PdfEditorApp.ViewModels.DataStudio;
using PdfEditorApp.ViewModels.BatchGeneration;
using PdfEditorApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Messages;
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
    private readonly IPageOrganizerService _pageOrganizerService;
    private readonly IPdfDocumentOperationsService _pdfOperationsService;
    private readonly IPdfToolRegistry _toolRegistry;

    public ISmartPlacementService SmartPlacement => _placementService;
    public IPageOrganizerService PageOrganizer => _pageOrganizerService;
    public IPdfDocumentOperationsService PdfOperations => _pdfOperationsService;

    // PDF Tool Studio Subsystems
    public PdfToolRunnerViewModel ToolRunner { get; }
    public WorkflowBuilderViewModel WorkflowBuilder { get; }
    public PdfViewerViewModel PdfViewer { get; }
    public DataStudioViewModel DataStudio { get; }
    public BatchGenerationViewModel BatchGeneration { get; }

    // --- HOME / EDITOR / VIEWER VIEW-SWITCHING ---

    [ObservableProperty]
    private bool _isHomePageVisible = true;

    [ObservableProperty]
    private bool _isEditorVisible = false;

    [ObservableProperty]
    private bool _isPdfViewerVisible = false;

    /// <summary>
    /// Dynamic window title that accurately reflects the active application context:
    /// Home dashboard ("FryPDF - Privacy-First PDF Studio"), specific tool ("Merge PDF - FryPDF"),
    /// PDF Reader ("Document.pdf - FryPDF"), or Editor Studio ("Document.pdf - FryPDF").
    /// </summary>
    public string WindowTitle
    {
        get
        {
            if (IsHomePageVisible)
            {
                if (Home != null && Home.IsToolPageActive && Home.ActiveToolCard != null)
                {
                    return $"{Home.ActiveToolCard.Name} - FryPDF";
                }
                if (Home != null && Home.IsToolPageActive && Home.ActiveToolViewModel != null)
                {
                    return $"{Home.ActiveToolViewModel.Tool.Name} - FryPDF";
                }
                if (Home != null && Home.SelectedNavSection == HomeNavSection.Licensing)
                {
                    return "Licenses & Third-Party Tools - FryPDF";
                }
                return "FryPDF - Privacy-First PDF Studio";
            }
            else if (IsPdfViewerVisible)
            {
                if (PdfViewer != null && !string.IsNullOrWhiteSpace(PdfViewer.DocumentTitle) && PdfViewer.DocumentTitle != "Document.pdf")
                {
                    return $"{PdfViewer.DocumentTitle} - FryPDF";
                }
                return "PDF Reader - FryPDF";
            }
            else if (IsEditorVisible)
            {
                if (!string.IsNullOrWhiteSpace(DocumentTitle))
                {
                    return $"{DocumentTitle} - FryPDF";
                }
                return "FryPDF Editor";
            }
            return "FryPDF";
        }
    }

    partial void OnIsHomePageVisibleChanged(bool value) => OnPropertyChanged(nameof(WindowTitle));
    partial void OnIsEditorVisibleChanged(bool value) => OnPropertyChanged(nameof(WindowTitle));
    partial void OnIsPdfViewerVisibleChanged(bool value) => OnPropertyChanged(nameof(WindowTitle));
    partial void OnDocumentTitleChanged(string value) => OnPropertyChanged(nameof(WindowTitle));

    /// <summary>ViewModel for the Home / Start Screen.</summary>
    public HomeViewModel Home { get; }

    private List<ElementViewModelBase> _clipboardElements = new();
    private CancellationTokenSource? _toastCts;

    // --- CORE OBSERVABLE PROPERTIES ---

    [ObservableProperty]
    private string _documentTitle = "Document_2026.pdf";

    [ObservableProperty]
    private string _currentFilePath = "";

    [ObservableProperty]
    private string _documentAuthor = "CodeFryDev";

    [ObservableProperty]
    private string _documentSubject = "PDF Document";

    [ObservableProperty]
    private string _documentKeywords = "";

    [ObservableProperty]
    private string _documentCreator = "FryPDF";

    [ObservableProperty]
    private string _documentProducer = "codefrydev.in";

    [ObservableProperty]
    private RibbonTabKind _activeRibbonTab = RibbonTabKind.Edit;

    [ObservableProperty]
    private SidebarTabKind _activeSidebarTab = SidebarTabKind.Thumbnails;

    // Workspace Panel Collapse & Expand States
    [ObservableProperty]
    private bool _isRibbonCollapsed;

    [ObservableProperty]
    private bool _isLeftSidebarCollapsed;

    [ObservableProperty]
    private bool _isInspectorCollapsed;

    [ObservableProperty]
    private ToolMode _activeToolMode = ToolMode.Select;

    [ObservableProperty]
    private double _zoomLevel = 1.0; // 100%

    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// True while a document open/template load/export is in progress. Drives the
    /// status-bar busy indicator so the app never looks idle while it's actually working.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private PageViewModel? _currentPage;

    partial void OnCurrentPageChanged(PageViewModel? oldValue, PageViewModel? newValue)
    {
        if (oldValue != null)
        {
            oldValue.SelectionChanged -= OnElementSelectionChanged;
            oldValue.MultiSelectionChanged -= OnMultiSelectionChanged;
        }

        if (newValue != null)
        {
            newValue.SelectionChanged += OnElementSelectionChanged;
            newValue.MultiSelectionChanged += OnMultiSelectionChanged;
            Inspector.UpdateSelection(newValue.SelectedElement, newValue);
        }
        else
        {
            Inspector.UpdateSelection(null, null);
        }
    }

    private void OnMultiSelectionChanged()
    {
        Inspector.UpdateSelection(CurrentPage?.SelectedElement, CurrentPage);
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

    [ObservableProperty]
    private ToastNotificationType _toastType = ToastNotificationType.Primary;

    [ObservableProperty]
    private ToastPosition _toastPosition = ToastPosition.BottomCenter;

    [ObservableProperty]
    private ToastStyleVariant _toastStyleVariant = ToastStyleVariant.Solid;

    [ObservableProperty]
    private bool _toastShowCloseButton = true;

    partial void OnToastTypeChanged(ToastNotificationType value) => RefreshToastVisuals();
    partial void OnToastPositionChanged(ToastPosition value) => RefreshToastVisuals();
    partial void OnToastStyleVariantChanged(ToastStyleVariant value) => RefreshToastVisuals();
    partial void OnToastShowCloseButtonChanged(bool value) => RefreshToastVisuals();

    public bool ToastIsSolid
    {
        get
        {
            if (ToastStyleVariant == ToastStyleVariant.Auto)
                return IsDarkMode;
            return ToastStyleVariant == ToastStyleVariant.Solid;
        }
    }

    public IBrush ToastBackgroundBrush
    {
        get
        {
            if (ToastIsSolid)
            {
                return ToastType switch
                {
                    ToastNotificationType.Primary => Brush("#0F6CBD"),
                    ToastNotificationType.Success => Brush("#15803D"),
                    ToastNotificationType.Danger => Brush("#DC2626"),
                    ToastNotificationType.Warning => Brush("#D97706"),
                    ToastNotificationType.General => Brush("#1E293B"),
                    _ => Brush("#1E293B")
                };
            }

            if (IsDarkMode)
            {
                return ToastType switch
                {
                    ToastNotificationType.Primary => Brush("#172554"),
                    ToastNotificationType.Success => Brush("#052E16"),
                    ToastNotificationType.Danger => Brush("#450A0A"),
                    ToastNotificationType.Warning => Brush("#451A03"),
                    ToastNotificationType.General => Brush("#18181B"),
                    _ => Brush("#18181B")
                };
            }
            else
            {
                return ToastType switch
                {
                    ToastNotificationType.Primary => Brush("#EFF6FF"),
                    ToastNotificationType.Success => Brush("#F0FDF4"),
                    ToastNotificationType.Danger => Brush("#FEF2F2"),
                    ToastNotificationType.Warning => Brush("#FFFBEB"),
                    ToastNotificationType.General => Brush("#F8FAFC"),
                    _ => Brush("#F8FAFC")
                };
            }
        }
    }

    public IBrush ToastForegroundBrush
    {
        get
        {
            if (ToastIsSolid) return WhiteBrush;

            if (IsDarkMode)
            {
                return ToastType switch
                {
                    ToastNotificationType.Primary => Brush("#93C5FD"),
                    ToastNotificationType.Success => Brush("#86EFAC"),
                    ToastNotificationType.Danger => Brush("#FCA5A5"),
                    ToastNotificationType.Warning => Brush("#FCD34D"),
                    ToastNotificationType.General => Brush("#E2E8F0"),
                    _ => Brush("#E2E8F0")
                };
            }
            else
            {
                return ToastType switch
                {
                    ToastNotificationType.Primary => Brush("#1D4ED8"),
                    ToastNotificationType.Success => Brush("#15803D"),
                    ToastNotificationType.Danger => Brush("#B91C1C"),
                    ToastNotificationType.Warning => Brush("#B45309"),
                    ToastNotificationType.General => Brush("#334155"),
                    _ => Brush("#334155")
                };
            }
        }
    }

    public IBrush ToastBorderBrush
    {
        get
        {
            if (ToastIsSolid)
            {
                return ToastType switch
                {
                    ToastNotificationType.Primary => Brush("#0D5CA0"),
                    ToastNotificationType.Success => Brush("#166534"),
                    ToastNotificationType.Danger => Brush("#B91C1C"),
                    ToastNotificationType.Warning => Brush("#B45309"),
                    ToastNotificationType.General => Brush("#0F172A"),
                    _ => Brush("#0F172A")
                };
            }

            if (IsDarkMode)
            {
                return ToastType switch
                {
                    ToastNotificationType.Primary => Brush("#1E40AF"),
                    ToastNotificationType.Success => Brush("#166534"),
                    ToastNotificationType.Danger => Brush("#991B1B"),
                    ToastNotificationType.Warning => Brush("#92400E"),
                    ToastNotificationType.General => Brush("#3F3F46"),
                    _ => Brush("#3F3F46")
                };
            }
            else
            {
                return ToastType switch
                {
                    ToastNotificationType.Primary => Brush("#BFDBFE"),
                    ToastNotificationType.Success => Brush("#BBF7D0"),
                    ToastNotificationType.Danger => Brush("#FECACA"),
                    ToastNotificationType.Warning => Brush("#FDE68A"),
                    ToastNotificationType.General => Brush("#CBD5E1"),
                    _ => Brush("#CBD5E1")
                };
            }
        }
    }

    public IBrush ToastIconBrush => ToastIsSolid ? WhiteBrush : ToastForegroundBrush;
    public IBrush ToastCloseBrush => ToastIsSolid ? WhiteBrush : ToastForegroundBrush;

    public HorizontalAlignment ToastHorizontalAlignment => ToastPosition switch
    {
        ToastPosition.TopLeft or ToastPosition.BottomLeft => HorizontalAlignment.Left,
        ToastPosition.TopRight or ToastPosition.BottomRight => HorizontalAlignment.Right,
        _ => HorizontalAlignment.Center
    };

    public VerticalAlignment ToastVerticalAlignment => ToastPosition switch
    {
        ToastPosition.TopLeft or ToastPosition.TopCenter or ToastPosition.TopRight => VerticalAlignment.Top,
        _ => VerticalAlignment.Bottom
    };

    public Thickness ToastMargin => ToastPosition switch
    {
        ToastPosition.TopLeft => new Thickness(32, 54, 0, 0),
        ToastPosition.TopCenter => new Thickness(0, 54, 0, 0),
        ToastPosition.TopRight => new Thickness(0, 54, 32, 0),
        ToastPosition.BottomLeft => new Thickness(32, 0, 0, 48),
        ToastPosition.BottomCenter => new Thickness(0, 0, 0, 48),
        ToastPosition.BottomRight => new Thickness(0, 0, 32, 48),
        _ => new Thickness(0, 0, 0, 48)
    };

    private static readonly SolidColorBrush WhiteBrush = new(Color.Parse("#FFFFFF"));
    private static SolidColorBrush Brush(string hex) => new(Color.Parse(hex));

    public void RefreshToastVisuals()
    {
        OnPropertyChanged(nameof(ToastIsSolid));
        OnPropertyChanged(nameof(ToastBackgroundBrush));
        OnPropertyChanged(nameof(ToastForegroundBrush));
        OnPropertyChanged(nameof(ToastBorderBrush));
        OnPropertyChanged(nameof(ToastIconBrush));
        OnPropertyChanged(nameof(ToastCloseBrush));
        OnPropertyChanged(nameof(ToastHorizontalAlignment));
        OnPropertyChanged(nameof(ToastVerticalAlignment));
        OnPropertyChanged(nameof(ToastMargin));
    }

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
    private bool _isAuditRunning;

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

    [ObservableProperty]
    private bool _isApplyingBatesNumbers;

    [ObservableProperty]
    private double _batesProgressPercentage;

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

    [ObservableProperty]
    private bool _isComparing;

    // Math & Scientific Equation Studio
    [ObservableProperty]
    private bool _isMathStudioOpen;

    [ObservableProperty]
    private string _mathStudioFormula = @"\int_{-\infty}^{\infty} e^{-x^2} \, dx = \sqrt{\pi}";

    [ObservableProperty]
    private string _mathStudioPresetName = "Gaussian Integral";

    [ObservableProperty]
    private string _mathStudioEquationNumber = "(1)";

    [ObservableProperty]
    private bool _mathStudioShowNumber = false;

    [ObservableProperty]
    private MathCategory _mathStudioCategory = MathCategory.Calculus;

    [ObservableProperty]
    private string _mathStudioSvgPreview = "";

    [ObservableProperty]
    private MathElementViewModel? _editingMathElement;

    partial void OnMathStudioFormulaChanged(string value) => UpdateMathStudioPreview();
    partial void OnMathStudioShowNumberChanged(bool value) => UpdateMathStudioPreview();
    partial void OnMathStudioEquationNumberChanged(string value) => UpdateMathStudioPreview();

    public void UpdateMathStudioPreview()
    {
        var options = new Services.MathEngine.MathRenderOptions(
            FontSize: 18,
            TextColorHex: "#0F172A",
            ShowEquationNumber: MathStudioShowNumber,
            EquationNumber: MathStudioEquationNumber,
            TargetWidth: 460,
            TargetHeight: 80
        );
        var result = Services.MathEngine.MathLayoutEngine.RenderToSvg(MathStudioFormula, options);
        MathStudioSvgPreview = result.SvgXml;
    }

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
    public InspectorViewModel Inspector { get; }
    public UndoRedoService UndoRedo { get; } = new();
    public static IStorageProvider? StorageProvider { get; set; }

    // Calculated Properties
    public int CurrentPageNumber => CurrentPage != null ? Pages.IndexOf(CurrentPage) + 1 : 1;
    public int TotalPagesCount => Pages.Count;
    public string PageDimensionsDisplay => CurrentPage != null ? $"{CurrentPage.Width:F0} × {CurrentPage.Height:F0} pt" : "800 × 1131 pt";
    public string SecurityStatusDisplay => SecuritySettings.IsPasswordProtected ? "Protected (Password Required)" : "Standard (No Security)";

    // --- CONSTRUCTORS ---

    public MainViewModel() : this(
        new PdfExportService(),
        new TemplateService(),
        new ProjectPersistenceService(),
        new DocumentAuditService(),
        new SignatureService(),
        new SmartPlacementService(),
        new RecentDocumentsService(),
        new PageOrganizerService(),
        null,
        null,
        new ThemeService(),
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        new UiSettingsService())
    {
    }

    private readonly IThemeService? _themeService;
    private readonly IUiSettingsService? _uiSettingsService;

    public MainViewModel(
        IPdfExportService exportService,
        ITemplateService templateService,
        IProjectPersistenceService persistenceService,
        IDocumentAuditService? auditService = null,
        ISignatureService? signatureService = null,
        ISmartPlacementService? placementService = null,
        IRecentDocumentsService? recentService = null,
        IPageOrganizerService? pageOrganizerService = null,
        IPdfDocumentOperationsService? pdfOperationsService = null,
        IPdfToolRegistry? toolRegistry = null,
        IThemeService? themeService = null,
        IPdfToolViewModelFactory? toolViewModelFactory = null,
        IPdfWorkflowEngine? workflowEngine = null,
        IDataSourceService? dataSourceService = null,
        IDataBindingService? dataBindingService = null,
        IDataMergeEngine? dataMergeEngine = null,
        IBatchPdfGenerator? batchPdfGenerator = null,
        PdfToolRunnerViewModel? toolRunner = null,
        WorkflowBuilderViewModel? workflowBuilder = null,
        DataStudioViewModel? dataStudio = null,
        BatchGenerationViewModel? batchGeneration = null,
        HomeViewModel? homeViewModel = null,
        PdfViewerViewModel? pdfViewer = null,
        InspectorViewModel? inspector = null,
        IUiSettingsService? uiSettingsService = null)
    {
        _exportService = exportService;
        _templateService = templateService;
        _persistenceService = persistenceService;
        _auditService = auditService ?? new DocumentAuditService();
        _signatureService = signatureService ?? new SignatureService();
        _placementService = placementService ?? new SmartPlacementService();
        _recentService = recentService ?? new RecentDocumentsService();
        _pageOrganizerService = pageOrganizerService ?? new PageOrganizerService();
        _themeService = themeService;
        _toolRegistry = toolRegistry ?? new PdfToolRegistry();
        _uiSettingsService = uiSettingsService ?? new UiSettingsService();

        if (_uiSettingsService != null)
        {
            var s = _uiSettingsService.Settings;
            ToastPosition = s.ToastPosition;
            ToastStyleVariant = s.ToastStyleVariant;
            ToastShowCloseButton = s.ToastShowCloseButton;
            _uiSettingsService.SettingsChanged += (newSettings) =>
            {
                ToastPosition = newSettings.ToastPosition;
                ToastStyleVariant = newSettings.ToastStyleVariant;
                ToastShowCloseButton = newSettings.ToastShowCloseButton;
                RefreshToastVisuals();
            };
        }

        if (_themeService != null)
        {
            IsDarkMode = _themeService.IsDarkMode;
            _themeService.ThemeChanged += (mode) =>
            {
                IsDarkMode = _themeService.IsDarkMode;
                if (Home != null) Home.IsDarkMode = IsDarkMode;
                RefreshToastVisuals();
            };
        }

        var effectiveWorkflowEngine = workflowEngine ?? (pdfOperationsService?.WorkflowEngine) ?? new PdfWorkflowEngine();
        _pdfOperationsService = pdfOperationsService ?? new PdfDocumentOperationsService(
            _toolRegistry,
            new PdfPageService(),
            new PdfOptimizationService(),
            new PdfSecurityService(),
            new PdfConversionService(),
            new PdfOcrService(),
            new PdfFormService(),
            new AiDocumentService(),
            new DocumentTranslationService(),
            effectiveWorkflowEngine);

        var effectiveToolFactory = toolViewModelFactory ?? new PdfToolViewModelFactory(_pdfOperationsService, _toolRegistry);

        ToolRunner = toolRunner ?? new PdfToolRunnerViewModel(_pdfOperationsService);

        WorkflowBuilder = workflowBuilder ?? new WorkflowBuilderViewModel(effectiveWorkflowEngine, _toolRegistry);

        var effectiveDataSource = dataSourceService ?? new DataSourceService();
        var effectiveDataBinding = dataBindingService ?? new DataBindingService();
        var effectiveMerge = dataMergeEngine ?? new DataMergeEngine();
        var effectiveBatch = batchPdfGenerator ?? new BatchPdfGeneratorService(effectiveMerge, _exportService);

        DataStudio = dataStudio ?? new DataStudioViewModel(effectiveDataSource, effectiveDataBinding);
        DataStudio.UndoRedo = UndoRedo;
        DataStudio.OnElementCreated += (el, desc) => AddElementWithUndo(el, desc);

        BatchGeneration = batchGeneration ?? new BatchGenerationViewModel(effectiveDataSource, effectiveMerge, effectiveBatch, _templateService);
        DataStudio.OpenBatchMergeRequested += (matrix) =>
        {
            BatchGeneration.StorageProvider = StorageProvider;
            var doc = (IsEditorVisible && Pages.Count > 0) ? ToDocumentModel() : _templateService.CreateEmployeePayslipTemplate();
            BatchGeneration.OpenWithDocument(doc, matrix);
        };

        Inspector = inspector ?? new InspectorViewModel();
        Inspector.UndoRedo = UndoRedo;
        Inspector.DataStudio = DataStudio;

        PdfViewer = pdfViewer ?? new PdfViewerViewModel();
        if (_themeService != null)
        {
            PdfViewer.ReadingTheme = _themeService.ReadingTheme;
            _themeService.ReadingThemeChanged += (rt) =>
            {
                if (PdfViewer.ReadingTheme != rt)
                {
                    PdfViewer.ReadingTheme = rt;
                }
            };
        }

        // Set up Home page
        Home = homeViewModel ?? new HomeViewModel(_recentService, _templateService, _persistenceService, _toolRegistry, ToolRunner, effectiveToolFactory, _themeService, _uiSettingsService);

        UndoRedo.StateChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
        };

        RegisterMessengerHandlers();

        RefreshToastVisuals();

        // Synchronize inspector when selection changes
        Inspector.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Inspector.SelectedElement))
            {
                OnPropertyChanged(nameof(CurrentPage));
            }
        };

        // Update window title dynamically when Home state or PDF Viewer title changes
        Home.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(Home.IsToolPageActive) or nameof(Home.ActiveToolViewModel) or nameof(Home.SelectedNavSection))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        };

        PdfViewer.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PdfViewer.DocumentTitle))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        };

        // Initialize quick command palette indexing
        InitCommandPalette();

        // Initialize default document model
        var defaultDoc = _templateService.CreateAnnualReportTemplate();
        LoadFromDocumentModel(defaultDoc);
    }

    private void RegisterMessengerHandlers()
    {
        WeakReferenceMessenger.Default.Register<MainViewModel, ShowToastMessage>(this, (r, m) => r.ShowToast(m.Message, m.Type, m.ActionLabel));
        WeakReferenceMessenger.Default.Register<MainViewModel, NavigateToHomeMessage>(this, (r, m) => r.NavigateToHome());
        WeakReferenceMessenger.Default.Register<MainViewModel, OpenInEditorMessage>(this, (r, m) => r.OpenEditorWithFile(m.FilePath));
        WeakReferenceMessenger.Default.Register<MainViewModel, OpenInViewerMessage>(this, (r, m) => r.OpenInViewer(m.FilePath));
        WeakReferenceMessenger.Default.Register<MainViewModel, RunToolMessage>(this, (r, m) => r.OpenToolWithInitialFile(m.ToolId, m.FilePath));
        WeakReferenceMessenger.Default.Register<MainViewModel, ReadingThemeChangedMessage>(this, (r, m) =>
        {
            if (r._themeService != null && r._themeService.ReadingTheme != m.Theme)
            {
                r._themeService.SetReadingTheme(m.Theme);
            }
        });
        WeakReferenceMessenger.Default.Register<MainViewModel, ProjectRenamedMessage>(this, (r, m) =>
        {
            if (string.Equals(r.CurrentFilePath, m.OldPath, StringComparison.OrdinalIgnoreCase))
            {
                r.CurrentFilePath = m.NewPath;
                r.DocumentTitle = Path.GetFileName(m.NewPath);
            }
            if (string.Equals(r.PdfViewer.CurrentFilePath, m.OldPath, StringComparison.OrdinalIgnoreCase))
            {
                r.PdfViewer.CurrentFilePath = m.NewPath;
                r.PdfViewer.DocumentTitle = Path.GetFileName(m.NewPath);
            }
        });
        WeakReferenceMessenger.Default.Register<MainViewModel, ProjectDeletedMessage>(this, (r, m) =>
        {
            if (string.Equals(r.CurrentFilePath, m.DeletedPath, StringComparison.OrdinalIgnoreCase))
            {
                r.CurrentFilePath = "";
                r.DocumentTitle = "Untitled.pdf";
                r.IsEditorVisible = false;
                r.IsHomePageVisible = true;
            }
            if (string.Equals(r.PdfViewer.CurrentFilePath, m.DeletedPath, StringComparison.OrdinalIgnoreCase))
            {
                r.PdfViewer.CurrentFilePath = "";
                r.IsPdfViewerVisible = false;
                r.IsHomePageVisible = true;
            }
        });
        WeakReferenceMessenger.Default.Register<MainViewModel, PromptRenameMessage>(this, (r, m) => r.Home.PromptRename(m.FilePath));
        WeakReferenceMessenger.Default.Register<MainViewModel, PromptDeleteMessage>(this, (r, m) => r.Home.PromptDelete(m.FilePath));
        WeakReferenceMessenger.Default.Register<MainViewModel, OpenTemplateMessage>(this, (r, m) => r.OpenEditorWithTemplate(m.TemplateName));
        WeakReferenceMessenger.Default.Register<MainViewModel, OpenProjectFileMessage>(this, (r, m) => _ = r.OpenProjectAndEnterEditorAsync());
        WeakReferenceMessenger.Default.Register<MainViewModel, OpenWorkflowStudioMessage>(this, (r, m) => r.OpenWorkflowStudio());
        WeakReferenceMessenger.Default.Register<MainViewModel, OpenBatchGenerationMessage>(this, (r, m) => r.OpenBatchGeneration());
        WeakReferenceMessenger.Default.Register<MainViewModel, OpenPdfPickerMessage>(this, (r, m) => _ = r.OpenPdfToReadWithPickerAsync());
    }

    [RelayCommand]
    public void OpenTool(PdfToolId toolId)
    {
        IsHomePageVisible = true;
        IsEditorVisible = false;
        IsPdfViewerVisible = false;
        Home.OpenToolPage(toolId);
    }

    public void OpenToolWithInitialFile(PdfToolId toolId, string initialFilePath)
    {
        IsHomePageVisible = true;
        IsEditorVisible = false;
        IsPdfViewerVisible = false;
        Home.OpenToolPage(toolId, initialFilePath);
    }

    [RelayCommand]
    public void OpenWorkflowStudio()
    {
        WorkflowBuilder.StorageProvider = StorageProvider;
        WorkflowBuilder.Open();
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        if (_themeService != null)
        {
            _themeService.ToggleTheme();
            IsDarkMode = _themeService.IsDarkMode;
            Home.IsDarkMode = IsDarkMode;
            ShowToast(IsDarkMode ? "Switched to Dark Studio Theme" : "Switched to Light Studio Theme", IsDarkMode ? "WeatherNight" : "WeatherSunny");
        }
        else
        {
            IsDarkMode = !IsDarkMode;
            Home.IsDarkMode = IsDarkMode;
        }
    }

    [RelayCommand]
    public void SetLightTheme()
    {
        _themeService?.SetTheme(AppThemeMode.Light);
        IsDarkMode = false;
        Home.IsDarkMode = false;
        ShowToast("Switched to Light Studio Theme", "WeatherSunny");
    }

    [RelayCommand]
    public void SetDarkTheme()
    {
        _themeService?.SetTheme(AppThemeMode.Dark);
        IsDarkMode = true;
        Home.IsDarkMode = true;
        ShowToast("Switched to Dark Studio Theme", "WeatherNight");
    }

    [RelayCommand]
    public void SetSystemTheme()
    {
        _themeService?.SetTheme(AppThemeMode.System);
        IsDarkMode = _themeService?.IsDarkMode ?? false;
        Home.IsDarkMode = IsDarkMode;
        ShowToast("Using System Theme Preference", "Laptop");
    }

    // --- HOME / EDITOR / VIEWER NAVIGATION ---

    /// <summary>Switches to the editor and loads the requested template.</summary>
    public void OpenEditorWithTemplate(string? templateName)
    {
        IsHomePageVisible = false;
        IsPdfViewerVisible = false;
        IsEditorVisible = true;

        var model = string.IsNullOrWhiteSpace(templateName)
            ? _templateService.CreateBlankDocument()
            : _templateService.CreateTemplate(templateName);

        LoadFromDocumentModel(model);
        ShowToast($"Created new document from {templateName ?? "Blank"} template", "FilePlusOutline");
    }

    /// <summary>Switches to the editor and loads a project from a file path.</summary>
    public void OpenEditorWithFile(string path)
    {
        _ = OpenEditorWithFileAsync(path);
    }

    /// <summary>Asynchronously switches to the editor and loads a project from a file path.</summary>
    public async Task OpenEditorWithFileAsync(string path)
    {
        // Navigate to the (still-empty) editor shell immediately so the user sees the app
        // respond right away, then populate it — mirrors OpenInViewerAsync's approach.
        IsHomePageVisible = false;
        IsPdfViewerVisible = false;
        IsEditorVisible = true;
        IsBusy = true;
        UpdateStatus($"Opening {Path.GetFileName(path)}...");

        try
        {
            var model = await _persistenceService.LoadProjectAsync(path);
            if (model != null)
            {
                await LoadFromDocumentModelAsync(model);
                CurrentFilePath = path;
                _recentService.Add(new RecentDocumentItem
                {
                    FilePath = path,
                    Title = model.Title,
                    LastOpened = DateTime.UtcNow
                });
                Home.RefreshRecent();
                ShowToast($"Opened: {Path.GetFileName(path)}", "FolderOpenOutline");
            }
            else
            {
                IsBusy = false;
                IsEditorVisible = false;
                IsHomePageVisible = true;
                ShowToast("Could not open file: unrecognized or corrupted document.", "AlertCircleOutline");
            }
        }
        catch (Exception ex)
        {
            IsBusy = false;
            IsEditorVisible = false;
            IsHomePageVisible = true;
            ShowToast($"Could not open file: {ex.Message}", "AlertCircleOutline");
        }
    }

    /// <summary>Opens any PDF in the dedicated PDF Viewer subsystem.</summary>
    [RelayCommand]
    public void OpenPdfReader()
    {
        _ = OpenPdfToReadWithPickerAsync();
    }

    /// <summary>Opens any PDF in the dedicated PDF Viewer subsystem.</summary>
    public void OpenInViewer(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            _ = OpenPdfToReadWithPickerAsync();
        }
        else
        {
            _ = OpenInViewerAsync(path);
        }
    }

    /// <summary>Prompts user to select a PDF file and opens it in PDF Reader mode.</summary>
    public async Task OpenPdfToReadWithPickerAsync()
    {
        try
        {
            if (StorageProvider != null)
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open PDF Document to Read (PDF Reader Mode)",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("PDF Documents (*.pdf)")
                        {
                            Patterns = new[] { "*.pdf" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    string chosenPath = files[0].Path.LocalPath;
                    await OpenInViewerAsync(chosenPath);
                }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Could not open PDF: {ex.Message}", "AlertCircleOutline");
        }
    }

    /// <summary>Asynchronously opens a PDF document in the viewer.</summary>
    public async Task OpenInViewerAsync(string path)
    {
        try
        {
            // Immediately transition to PDF Viewer with zero delay
            DocumentTitle = Path.GetFileName(path);
            IsHomePageVisible = false;
            IsEditorVisible = false;
            IsPdfViewerVisible = true;
            ShowToast($"Opening: {Path.GetFileName(path)}", "FilePdfBox");

            PdfViewer.StorageProvider = StorageProvider;
            await PdfViewer.LoadDocumentAsync(path);

            _recentService.Add(new RecentDocumentItem
            {
                FilePath = path,
                Title = Path.GetFileName(path),
                LastOpened = DateTime.UtcNow
            });
            Home.RefreshRecent();
        }
        catch (Exception ex)
        {
            ShowToast($"Could not open in Viewer: {ex.Message}", "AlertCircleOutline");
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
                    Title = "Open PDF Document or FryPDF Project",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("All Supported Documents (*.pdf, *.frypdf, *.pdfproj, *.json)")
                        {
                            Patterns = new[] { "*.pdf", "*.frypdf", "*.pdfproj", "*.json" }
                        },
                        new FilePickerFileType("PDF Documents (*.pdf)")
                        {
                            Patterns = new[] { "*.pdf" }
                        },
                        new FilePickerFileType("FryPDF Projects (*.frypdf, *.pdfproj, *.json)")
                        {
                            Patterns = new[] { "*.frypdf", "*.pdfproj", "*.json" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    string chosenPath = files[0].Path.LocalPath;
                    OpenEditorWithFile(chosenPath);
                }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Open error: {ex.Message}", "AlertCircleOutline");
        }
    }

    /// <summary>Returns to the Home page from the editor or viewer.</summary>
    [RelayCommand]
    public void NavigateToHome()
    {
        Home.RefreshRecent();
        Home.BackToTools();
        Home.SelectedNavSection = HomeNavSection.Home;
        IsEditorVisible = false;
        IsPdfViewerVisible = false;
        IsHomePageVisible = true;
    }

    public void OnElementSelectionChanged(ElementViewModelBase? selectedElement)
    {
        Inspector.UpdateSelection(selectedElement, CurrentPage);
    }

    // --- TOAST HUD NOTIFICATION FEEDBACK ---

    [RelayCommand]
    public void DismissToast()
    {
        _toastCts?.Cancel();
        IsToastVisible = false;
    }

    public void ShowToast(string message, string? iconKind = null)
    {
        var inferred = InferToastType(message, iconKind);
        ShowToast(message, inferred, iconKind);
    }

    public void ShowToast(string message, ToastNotificationType type, string? iconKind = null, int? customDurationMs = null)
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;

        ToastMessage = message;
        ToastType = type;
        ToastIcon = !string.IsNullOrWhiteSpace(iconKind) ? iconKind : GetDefaultIconForType(type);
        RefreshToastVisuals();
        IsToastVisible = true;
        UpdateStatus(message);

        int duration = customDurationMs ?? (_uiSettingsService?.Settings.ToastDurationMs ?? 3500);
        if (duration > 0)
        {
            Task.Delay(duration, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    Dispatcher.UIThread.Post(() => IsToastVisible = false);
                }
            }, TaskScheduler.Default);
        }
    }

    public static ToastNotificationType InferToastType(string message, string? iconKind)
    {
        if (string.IsNullOrWhiteSpace(message))
            return ToastNotificationType.General;

        var lowerMsg = message.ToLowerInvariant();
        var lowerIcon = iconKind?.ToLowerInvariant() ?? "";

        if (lowerMsg.Contains("error") || lowerMsg.Contains("failed") || lowerMsg.Contains("could not") ||
            lowerMsg.Contains("corrupt") || lowerMsg.Contains("unrecognized") || lowerIcon.Contains("alertcircle") || lowerIcon.Contains("alertoctagon"))
        {
            return ToastNotificationType.Danger;
        }

        if (lowerMsg.Contains("warning") || lowerMsg.Contains("caution") || lowerMsg.Contains("nothing to"))
        {
            return ToastNotificationType.Warning;
        }

        if (lowerMsg.Contains("saved") || lowerMsg.Contains("success") || lowerMsg.Contains("copied") ||
            lowerMsg.Contains("pasted") || lowerMsg.Contains("done") || lowerMsg.Contains("applied") ||
            lowerMsg.Contains("created new document") || lowerIcon.Contains("check"))
        {
            return ToastNotificationType.Success;
        }

        if (lowerMsg.Contains("open") || lowerMsg.Contains("zoom") || lowerMsg.Contains("fit to") ||
            lowerMsg.Contains("switched to") || lowerMsg.Contains("active tool") || lowerMsg.Contains("ribbon") ||
            lowerMsg.Contains("sidebar") || lowerMsg.Contains("inspector") || lowerMsg.Contains("undone") || lowerMsg.Contains("redone"))
        {
            return ToastNotificationType.Primary;
        }

        return ToastNotificationType.General;
    }

    public static string GetDefaultIconForType(ToastNotificationType type) => type switch
    {
        ToastNotificationType.Primary => "InformationOutline",
        ToastNotificationType.Success => "CheckCircleOutline",
        ToastNotificationType.Danger => "AlertOctagonOutline",
        ToastNotificationType.Warning => "AlertOutline",
        ToastNotificationType.General => "InformationOutline",
        _ => "InformationOutline"
    };

    public void UpdateStatus(string message)
    {
        StatusMessage = message;
    }

    // --- TAB & TOOL MODE SELECTION ---

    [RelayCommand]
    public void SelectRibbonTab(RibbonTabKind tab)
    {
        ActiveRibbonTab = tab;
        if (IsRibbonCollapsed)
        {
            IsRibbonCollapsed = false;
        }
    }

    [RelayCommand]
    public void ToggleRibbonCollapse()
    {
        IsRibbonCollapsed = !IsRibbonCollapsed;
        ShowToast(IsRibbonCollapsed ? "Ribbon Minimized (⌘F1 to expand)" : "Ribbon Expanded", IsRibbonCollapsed ? "ChevronDown" : "ChevronUp");
    }

    [RelayCommand]
    public void ToggleLeftSidebar()
    {
        IsLeftSidebarCollapsed = !IsLeftSidebarCollapsed;
        ShowToast(IsLeftSidebarCollapsed ? "Pages Sidebar Minimized (⌘B to expand)" : "Pages Sidebar Expanded", IsLeftSidebarCollapsed ? "DockLeft" : "FileDocumentMultipleOutline");
    }

    [RelayCommand]
    public void ToggleInspectorCollapse()
    {
        IsInspectorCollapsed = !IsInspectorCollapsed;
        ShowToast(IsInspectorCollapsed ? "Inspector Minimized (⌘⇧P to expand)" : "Properties Inspector Expanded", IsInspectorCollapsed ? "DockRight" : "TuneVariant");
    }

    [RelayCommand]
    public void ExpandAllPanels()
    {
        IsRibbonCollapsed = false;
        IsLeftSidebarCollapsed = false;
        IsInspectorCollapsed = false;
        ShowToast("All Workspace Panels Expanded", "ViewQuiltOutline");
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

    public bool CanUndo => UndoRedo.CanUndo;
    public bool CanRedo => UndoRedo.CanRedo;

    [RelayCommand(CanExecute = nameof(CanUndo))]
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

    [RelayCommand(CanExecute = nameof(CanRedo))]
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
        if (CurrentPage == null) return;
        if (CurrentPage.SelectedElements.Count > 0)
        {
            _clipboardElements = CurrentPage.SelectedElements.ToList();
            ShowToast(_clipboardElements.Count == 1 ? $"Copied: {_clipboardElements[0].DisplayName}" : $"Copied {_clipboardElements.Count} Elements", "ContentCopy");
        }
    }

    [RelayCommand]
    public void Cut()
    {
        if (CurrentPage == null || CurrentPage.SelectedElements.Count == 0) return;
        _clipboardElements = CurrentPage.SelectedElements.ToList();
        var page = CurrentPage;
        var targets = _clipboardElements.ToList();

        foreach (var el in targets) page.RemoveElement(el);
        Inspector.UpdateSelection(null, page);

        UndoRedo.RecordAction(
            targets.Count == 1 ? $"Cut {targets[0].DisplayName}" : $"Cut {targets.Count} Elements",
            () => {
                foreach (var el in targets) page.AddElement(el);
                page.SelectElements(targets);
                Inspector.UpdateSelection(page.SelectedElement, page);
            },
            () => {
                foreach (var el in targets) page.RemoveElement(el);
                Inspector.UpdateSelection(null, page);
            }
        );

        ShowToast(targets.Count == 1 ? $"Cut: {targets[0].DisplayName}" : $"Cut {targets.Count} Elements", "ContentCut");
    }

    [RelayCommand]
    public void Paste()
    {
        if (_clipboardElements.Count == 0 || CurrentPage == null) return;
        var page = CurrentPage;
        var newVms = new List<ElementViewModelBase>();

        if (_clipboardElements.Count == 1)
        {
            var el = _clipboardElements[0];
            var model = el.ToModel();
            var clone = model.Clone();
            clone.Id = Guid.NewGuid().ToString("N");

            var (posX, posY) = _placementService.GetPlacementPosition(CurrentPage, clone.Width, clone.Height);
            clone.X = posX;
            clone.Y = posY;

            ElementViewModelBase newVm = clone.Kind switch
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
                ElementKind.Svg => new SvgElementViewModel(),
                _ => new TextElementViewModel()
            };

            newVm.LoadFromModel(clone);
            page.AddElement(newVm);
            newVms.Add(newVm);
        }
        else
        {
            double minX = _clipboardElements.Min(e => e.X);
            double minY = _clipboardElements.Min(e => e.Y);
            double maxX = _clipboardElements.Max(e => e.X + e.Width);
            double maxY = _clipboardElements.Max(e => e.Y + e.Height);
            double groupW = maxX - minX;
            double groupH = maxY - minY;

            var (targetX, targetY) = _placementService.GetPlacementPosition(CurrentPage, groupW, groupH);
            double offsetX = targetX - minX;
            double offsetY = targetY - minY;

            foreach (var el in _clipboardElements)
            {
                var model = el.ToModel();
                var clone = model.Clone();
                clone.Id = Guid.NewGuid().ToString("N");
                clone.X += offsetX;
                clone.Y += offsetY;

                ElementViewModelBase newVm = clone.Kind switch
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
                    ElementKind.Svg => new SvgElementViewModel(),
                    _ => new TextElementViewModel()
                };

                newVm.LoadFromModel(clone);
                page.AddElement(newVm);
                newVms.Add(newVm);
            }
        }

        page.SelectElements(newVms);
        Inspector.UpdateSelection(page.SelectedElement, page);

        UndoRedo.RecordAction(
            newVms.Count == 1 ? $"Paste {newVms[0].DisplayName}" : $"Paste {newVms.Count} Elements",
            () => {
                foreach (var el in newVms) page.RemoveElement(el);
                Inspector.UpdateSelection(null, page);
            },
            () => {
                foreach (var el in newVms) page.AddElement(el);
                page.SelectElements(newVms);
                Inspector.UpdateSelection(page.SelectedElement, page);
            }
        );

        ShowToast(newVms.Count == 1 ? $"Pasted: {newVms[0].DisplayName}" : $"Pasted {newVms.Count} Elements", "ContentPaste");
    }

    [RelayCommand]
    public void Duplicate()
    {
        Inspector.DuplicateSelectedElementCommand.Execute(null);
    }

    [RelayCommand]
    public void SelectAll()
    {
        CurrentPage?.SelectAll();
        Inspector.UpdateSelection(CurrentPage?.SelectedElement, CurrentPage);
    }

    // --- ZOOM COMMANDS ---

    [RelayCommand]
    public void ZoomIn()
    {
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.ZoomIn();
            return;
        }
        ZoomLevel = Math.Clamp(Math.Round(ZoomLevel * 1.2, 2), 0.1, 5.0);
        ShowToast($"Zoom: {(int)(ZoomLevel * 100)}%", "MagnifyPlusOutline");
    }

    [RelayCommand]
    public void ZoomOut()
    {
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.ZoomOut();
            return;
        }
        ZoomLevel = Math.Clamp(Math.Round(ZoomLevel / 1.2, 2), 0.1, 5.0);
        ShowToast($"Zoom: {(int)(ZoomLevel * 100)}%", "MagnifyMinusOutline");
    }

    [RelayCommand]
    public void ResetZoom()
    {
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.ResetZoom();
            return;
        }
        ZoomLevel = 1.0;
        ShowToast("Zoom Reset (100%)", "Magnify");
    }

    [RelayCommand]
    public void FitToWidth()
    {
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.FitToWidth();
            return;
        }
        ZoomLevel = 1.15;
        ShowToast("Fit to Width", "ArrowExpandHorizontal");
    }

    public void FitToWidthDynamic(double viewportWidth)
    {
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.FitToWidthDynamic(viewportWidth);
            return;
        }
        if (CurrentPage != null && viewportWidth > 100)
        {
            ZoomLevel = Math.Clamp(Math.Round((viewportWidth - 64.0) / CurrentPage.Width, 2), 0.1, 5.0);
            ShowToast($"Fit to Width ({(int)(ZoomLevel * 100)}%)", "ArrowExpandHorizontal");
        }
        else
        {
            FitToWidth();
        }
    }

    [RelayCommand]
    public void FitToPage()
    {
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.FitToPage();
            return;
        }
        ZoomLevel = 0.85;
        ShowToast("Fit to Page", "FitToPageOutline");
    }

    public void FitToPageDynamic(double viewportWidth, double viewportHeight)
    {
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.FitToPageDynamic(viewportWidth, viewportHeight);
            return;
        }
        if (CurrentPage != null && viewportWidth > 100 && viewportHeight > 100)
        {
            double scaleX = (viewportWidth - 64.0) / CurrentPage.Width;
            double scaleY = (viewportHeight - 64.0) / CurrentPage.Height;
            ZoomLevel = Math.Clamp(Math.Round(Math.Min(scaleX, scaleY), 2), 0.1, 5.0);
            ShowToast($"Fit to Page ({(int)(ZoomLevel * 100)}%)", "FitToPageOutline");
        }
        else
        {
            FitToPage();
        }
    }

    [RelayCommand]
    public void RenameCurrentDocument()
    {
        if (IsPdfViewerVisible && !string.IsNullOrEmpty(PdfViewer.CurrentFilePath))
        {
            Home.PromptRename(PdfViewer.CurrentFilePath);
        }
        else if (IsEditorVisible)
        {
            if (!string.IsNullOrEmpty(CurrentFilePath) && File.Exists(CurrentFilePath))
            {
                Home.PromptRename(CurrentFilePath);
            }
            else
            {
                var fallbackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), DocumentTitle);
                Home.PromptRename(fallbackPath);
            }
        }
    }

    [RelayCommand]
    public void DuplicateCurrentDocument()
    {
        var target = IsPdfViewerVisible ? PdfViewer.CurrentFilePath : CurrentFilePath;
        if (!string.IsNullOrEmpty(target) && File.Exists(target))
        {
            Home.DuplicateDocument(target);
        }
        else
        {
            ShowToast("Please save the document before duplicating", "AlertCircleOutline");
        }
    }

    [RelayCommand]
    public void RevealCurrentDocumentInFileManager()
    {
        var target = IsPdfViewerVisible ? PdfViewer.CurrentFilePath : CurrentFilePath;
        if (!string.IsNullOrEmpty(target) && File.Exists(target))
        {
            Home.RevealInFileManager(target);
        }
        else
        {
            ShowToast("Document has not been saved to disk yet", "InformationOutline");
        }
    }

    [RelayCommand]
    public async Task CopyCurrentDocumentPath()
    {
        var target = IsPdfViewerVisible ? PdfViewer.CurrentFilePath : CurrentFilePath;
        if (!string.IsNullOrEmpty(target))
        {
            await Home.CopyPath(target);
        }
        else
        {
            ShowToast("Document has no saved disk path yet", "InformationOutline");
        }
    }

    [RelayCommand]
    public void DeleteCurrentDocument()
    {
        var target = IsPdfViewerVisible ? PdfViewer.CurrentFilePath : CurrentFilePath;
        if (!string.IsNullOrEmpty(target) && File.Exists(target))
        {
            Home.PromptDelete(target);
        }
    }
}
