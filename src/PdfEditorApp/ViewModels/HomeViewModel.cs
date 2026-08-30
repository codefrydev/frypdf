using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools;
using PdfEditorApp.Templates;

using PdfEditorApp.ViewModels.Tools;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel for the Google Docs, Canva, and Adobe Acrobat inspired Home / Tools Dashboard.
/// Provides a comprehensive PDF Tools Studio (all 32 tools), expandable template gallery, and recent document management.
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly IRecentDocumentsService _recentService;
    private readonly ITemplateService _templateService;
    private readonly IProjectPersistenceService _persistenceService;
    private readonly IPdfToolRegistry _toolRegistry;
    private readonly IPdfToolViewModelFactory? _toolViewModelFactory;

    // --- Events to tell the shell what to do ---
    public event Action<string?>? OpenTemplateRequested;   // templateName (null = blank)
    public event Action? OpenFileRequested;
    public event Action<string>? OpenRecentRequested;      // file path
    public event Action<string>? OpenInEditorRequested;    // file path
    public event Action<string>? OpenInViewerRequested;    // file path
    public event Action<PdfToolId>? OpenToolRequested;
    public event Action? OpenWorkflowBuilderRequested;

    // --- Observable State ---

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _selectedToolCategory = "All";

    [ObservableProperty]
    private string _selectedTemplateCategory = "All";

    [ObservableProperty]
    private bool _isTemplateGalleryExpanded;

    [ObservableProperty]
    private bool _isToolsDashboardVisible = true;

    // --- Navigation and Tab State ---

    [ObservableProperty]
    private HomeNavSection _selectedNavSection = HomeNavSection.Home;

    [ObservableProperty]
    private bool _isToolPageActive;

    [ObservableProperty]
    private PdfToolCardViewModel? _activeToolCard;

    [ObservableProperty]
    private PdfToolViewModelBase? _activeToolViewModel;

    [ObservableProperty]
    private PdfToolCardViewModel? _workflowBannerCard;

    public PdfToolRunnerViewModel? ToolRunner { get; }

    public ObservableCollection<PdfToolCardViewModel> AllTools { get; } = new();
    public ObservableCollection<PdfToolCardViewModel> FilteredTools { get; } = new();
    public ObservableCollection<PdfToolCardViewModel> QuickTools { get; } = new();
    public ObservableCollection<PdfToolCardViewModel> StarredTools { get; } = new();
    public ObservableCollection<TemplateCardViewModel> AllTemplates { get; } = new();
    public ObservableCollection<TemplateCardViewModel> FilteredTemplates { get; } = new();
    public ObservableCollection<TemplateCardViewModel> FeaturedTemplates { get; } = new();
    public ObservableCollection<RecentDocumentItem> RecentDocuments { get; } = new();

    public bool IsHomeSection => SelectedNavSection == HomeNavSection.Home;
    public bool IsNewDocumentSection => SelectedNavSection == HomeNavSection.NewDocument;
    public bool IsToolsSection => SelectedNavSection is HomeNavSection.AllTools
        or HomeNavSection.OrganizeAndPage
        or HomeNavSection.OptimizeAndSecurity
        or HomeNavSection.ConvertFromPdf
        or HomeNavSection.ConvertToPdf
        or HomeNavSection.EditAndForms
        or HomeNavSection.AiAndAutomation;
    public bool IsStarredSection => SelectedNavSection == HomeNavSection.Starred;
    public bool IsTrashSection => SelectedNavSection == HomeNavSection.Trash;

    public int MatchingToolsCount => FilteredTools.Count;
    public bool HasNoMatchingTools => FilteredTools.Count == 0;
    public int MatchingTemplatesCount => FilteredTemplates.Count;
    public bool HasNoMatchingTemplates => FilteredTemplates.Count == 0;
    public bool HasRecentDocuments => RecentDocuments.Count > 0;
    public bool HasStarredTools => StarredTools.Count > 0;

    // --- Constructor ---

    public HomeViewModel() : this(new RecentDocumentsService(), new TemplateService(), new ProjectPersistenceService(), new PdfToolRegistry()) { }

    public HomeViewModel(
        IRecentDocumentsService recentService,
        ITemplateService templateService,
        IProjectPersistenceService persistenceService,
        IPdfToolRegistry toolRegistry,
        PdfToolRunnerViewModel? toolRunner = null,
        IPdfToolViewModelFactory? toolViewModelFactory = null)
    {
        _recentService = recentService;
        _templateService = templateService;
        _persistenceService = persistenceService;
        _toolRegistry = toolRegistry;
        _toolViewModelFactory = toolViewModelFactory;
        ToolRunner = toolRunner;

        if (ToolRunner != null)
        {
            ToolRunner.BackRequested += BackToTools;
        }

        InitializeTools();
        InitializeTemplates();
        RefreshRecent();
    }

    // --- Tools Initialization ---

    private void InitializeTools()
    {
        AllTools.Clear();
        var toolDefs = _toolRegistry.GetAllTools();

        foreach (var def in toolDefs)
        {
            var cardVm = new PdfToolCardViewModel(def);
            cardVm.ToolSelected += (id) =>
            {
                if (id == PdfToolId.WorkflowBuilder)
                {
                    OpenWorkflowBuilderRequested?.Invoke();
                }
                else
                {
                    OpenToolPage(id);
                }
            };
            cardVm.StarToggled += (id) =>
            {
                RefreshStarredTools();
            };

            if (def.IsWorkflowBanner)
            {
                WorkflowBannerCard = cardVm;
            }
            else
            {
                AllTools.Add(cardVm);
            }
        }

        UpdateFilteredTools();

        QuickTools.Clear();
        var quickIds = new[] { PdfToolId.MergePdf, PdfToolId.CompressPdf, PdfToolId.PdfToWord, PdfToolId.SignPdf, PdfToolId.OcrPdf, PdfToolId.AiSummarizer };
        foreach (var qid in quickIds)
        {
            var match = AllTools.FirstOrDefault(t => t.Id == qid);
            if (match != null) QuickTools.Add(match);
        }
    }

    // --- Template Initialization ---

    private void InitializeTemplates()
    {
        AllTemplates.Clear();

        // 1. Blank Canvas
        var blankDoc = _templateService.CreateBlankDocument();
        var blankPageVm = new PageViewModel();
        blankPageVm.LoadFromModel(blankDoc.Pages[0]);
        AllTemplates.Add(new TemplateCardViewModel
        {
            Id = "",
            Name = "Blank Canvas",
            Category = "General",
            Subtitle = "Clean Canvas",
            Description = "Start fresh with a clean customizable canvas",
            Badge = "Blank",
            AccentColorHex = "#0F6CBD",
            IconKind = "FilePlusOutline",
            PagePreview = blankPageVm,
            IsBlank = true,
            IsFeatured = true,
            IsLandscape = false
        });

        // 2. Templates from ITemplateService
        var templateDefs = _templateService.GetAllTemplates();
        foreach (var def in templateDefs)
        {
            if (def is BlankDocumentTemplate) continue;

            try
            {
                var doc = def.Create();
                var page = doc.Pages.FirstOrDefault() ?? new PdfPageModel();
                var pageVm = new PageViewModel();
                pageVm.LoadFromModel(page);

                bool isFeatured = def.Id is "annualreport" or "invoice" or "resume" or "resumemodern" or "academic" or "mathresearch" or "physicsresearch" or "certificate" or "typographyshowcase";
                string subtitle = def.Id switch
                {
                    "annualreport" => "Executive Summary & Charts",
                    "invoice" => "Itemized Billing & Terms",
                    "resume" => "Executive CV with Live QR",
                    "resumemodern" => "Two-Column Tech & Product",
                    "resumecreative" => "Creative Director & Portfolio",
                    "resumeacademic" => "Faculty Dossier & Grants",
                    "academic" => "2-Column Systems Research",
                    "mathresearch" => "Discrete Hodge Theory",
                    "physicsresearch" => "Cavity QED & Quantum Spin",
                    "historyresearch" => "Mediterranean Trade Ledgers",
                    "financeresearch" => "Jump-Diffusion Econometrics",
                    "certificate" => "Crimson & Gold Award",
                    "certificatenavygold" => "Navy & Gold Crest",
                    "diploma" => "Collegiate Degree",
                    "weddingtraditional" => "Marigold & Ganesha Crest",
                    "weddingroyalfloral" => "Botanical Laurel Wreath",
                    "galainvitation" => "Black-Tie Art Deco",
                    "typographyshowcase" => "Bézier Waves & Ink Specimen",
                    _ => def.Category
                };

                string badge = def.Id switch
                {
                    "annualreport" => "Popular",
                    "invoice" => "Finance",
                    "resume" => "Executive",
                    "resumemodern" => "Tech",
                    "resumecreative" => "Design",
                    "resumeacademic" => "Academic",
                    "academic" => "Systems",
                    "mathresearch" => "Pure Math",
                    "physicsresearch" => "Quantum",
                    "historyresearch" => "History",
                    "financeresearch" => "Quant",
                    "certificate" => "Award",
                    "certificatenavygold" => "Executive",
                    "diploma" => "Degree",
                    "weddingtraditional" => "Festive",
                    "weddingroyalfloral" => "Luxury",
                    "galainvitation" => "Art Deco",
                    "typographyshowcase" => "Featured",
                    _ => def.Category
                };

                AllTemplates.Add(new TemplateCardViewModel
                {
                    Id = def.Id,
                    Name = def.Name,
                    Category = def.Category,
                    Subtitle = subtitle,
                    Description = def.Description,
                    Badge = badge,
                    AccentColorHex = def.AccentColorHex,
                    IconKind = def.IconKind,
                    PagePreview = pageVm,
                    IsBlank = false,
                    IsFeatured = isFeatured,
                    IsLandscape = page.Orientation == PageOrientation.Landscape || page.Width > page.Height
                });
            }
            catch
            {
                // Fallback for safe startup
            }
        }

        UpdateFilteredTemplates();
    }

    // --- Search & Filter Synchronization ---

    partial void OnSearchQueryChanged(string value)
    {
        UpdateFilteredTools();
        UpdateFilteredTemplates();
    }

    partial void OnSelectedToolCategoryChanged(string value) => UpdateFilteredTools();
    partial void OnSelectedTemplateCategoryChanged(string value) => UpdateFilteredTemplates();

    partial void OnSelectedNavSectionChanged(HomeNavSection value)
    {
        OnPropertyChanged(nameof(IsHomeSection));
        OnPropertyChanged(nameof(IsNewDocumentSection));
        OnPropertyChanged(nameof(IsToolsSection));
        OnPropertyChanged(nameof(IsStarredSection));
        OnPropertyChanged(nameof(IsTrashSection));
    }

    private void UpdateFilteredTools()
    {
        FilteredTools.Clear();
        foreach (var tool in AllTools)
        {
            if (MatchesToolCategory(tool) && MatchesToolSearch(tool))
            {
                FilteredTools.Add(tool);
            }
        }

        OnPropertyChanged(nameof(MatchingToolsCount));
        OnPropertyChanged(nameof(HasNoMatchingTools));
    }

    private bool MatchesToolCategory(PdfToolCardViewModel card)
    {
        if (SelectedToolCategory == "All") return true;

        return SelectedToolCategory switch
        {
            "Organize & Page" => card.Category == PdfToolCategory.OrganizeAndPage,
            "Optimize & Security" => card.Category == PdfToolCategory.OptimizeAndSecurity,
            "Convert from PDF" => card.Category == PdfToolCategory.ConvertFromPdf,
            "Convert to PDF" => card.Category == PdfToolCategory.ConvertToPdf,
            "Edit & Forms" => card.Category == PdfToolCategory.EditAndForms,
            "AI & Automation" => card.Category == PdfToolCategory.AiAndAutomation,
            _ => true
        };
    }

    private bool MatchesToolSearch(PdfToolCardViewModel card)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;
        var q = SearchQuery.Trim();
        return card.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || card.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
            || card.CategoryDisplayName.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateFilteredTemplates()
    {
        FilteredTemplates.Clear();
        FeaturedTemplates.Clear();

        foreach (var t in AllTemplates)
        {
            if (MatchesTemplateFilter(t))
            {
                FilteredTemplates.Add(t);
                if (t.IsFeatured)
                {
                    FeaturedTemplates.Add(t);
                }
            }
        }

        if (FeaturedTemplates.Count == 0 && FilteredTemplates.Count > 0)
        {
            foreach (var t in FilteredTemplates)
            {
                FeaturedTemplates.Add(t);
            }
        }

        OnPropertyChanged(nameof(MatchingTemplatesCount));
        OnPropertyChanged(nameof(HasNoMatchingTemplates));
    }

    private bool MatchesTemplateFilter(TemplateCardViewModel card)
    {
        if (SelectedTemplateCategory != "All" &&
            !string.Equals(SelectedTemplateCategory, card.Category, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;

        var q = SearchQuery.Trim();
        return card.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || card.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
            || card.Category.Contains(q, StringComparison.OrdinalIgnoreCase)
            || card.Subtitle.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    // --- Commands ---

    [RelayCommand]
    public void SetToolCategory(string? category)
    {
        SelectedToolCategory = string.IsNullOrWhiteSpace(category) ? "All" : category;
    }

    [RelayCommand]
    public void SetTemplateCategory(string? category)
    {
        SelectedTemplateCategory = string.IsNullOrWhiteSpace(category) ? "All" : category;
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = "";
    }

    [RelayCommand]
    public void ToggleTemplateGallery()
    {
        IsTemplateGalleryExpanded = !IsTemplateGalleryExpanded;
    }

    [RelayCommand]
    public void CollapseTemplateGallery()
    {
        IsTemplateGalleryExpanded = false;
    }

    [RelayCommand]
    public void ClearTemplateSearch()
    {
        SearchQuery = "";
        SelectedTemplateCategory = "All";
        UpdateFilteredTools();
        UpdateFilteredTemplates();
    }

    [RelayCommand]
    public void SelectNavSection(string sectionName)
    {
        if (Enum.TryParse<HomeNavSection>(sectionName, true, out var section))
        {
            SelectedNavSection = section;
            IsToolPageActive = false;

            if (section == HomeNavSection.NewDocument)
            {
                IsTemplateGalleryExpanded = true;
            }

            // Automatically set tool category filter according to selected section
            switch (section)
            {
                case HomeNavSection.OrganizeAndPage:
                    SelectedToolCategory = "Organize & Page";
                    break;
                case HomeNavSection.OptimizeAndSecurity:
                    SelectedToolCategory = "Optimize & Security";
                    break;
                case HomeNavSection.ConvertFromPdf:
                    SelectedToolCategory = "Convert from PDF";
                    break;
                case HomeNavSection.ConvertToPdf:
                    SelectedToolCategory = "Convert to PDF";
                    break;
                case HomeNavSection.EditAndForms:
                    SelectedToolCategory = "Edit & Forms";
                    break;
                case HomeNavSection.AiAndAutomation:
                    SelectedToolCategory = "AI & Automation";
                    break;
                default:
                    SelectedToolCategory = "All";
                    break;
            }
            UpdateFilteredTools();
        }
    }

    [RelayCommand]
    public void OpenToolPage(PdfToolId toolId)
    {
        var card = AllTools.FirstOrDefault(t => t.Id == toolId);
        if (card != null)
        {
            ActiveToolCard = card;
            IsToolPageActive = true;

            if (_toolViewModelFactory != null)
            {
                if (ActiveToolViewModel != null)
                {
                    ActiveToolViewModel.BackRequested -= BackToTools;
                    ActiveToolViewModel.OpenInEditorRequested -= OnToolOpenInEditorRequested;
                    ActiveToolViewModel.OpenInViewerRequested -= OnToolOpenInViewerRequested;
                }

                ActiveToolViewModel = _toolViewModelFactory.Create(toolId);
                ActiveToolViewModel.StorageProvider = MainViewModel.StorageProvider;
                ActiveToolViewModel.IsToolStarred = card.IsStarred;
                ActiveToolViewModel.BackRequested += BackToTools;
                ActiveToolViewModel.OpenInEditorRequested += OnToolOpenInEditorRequested;
                ActiveToolViewModel.OpenInViewerRequested += OnToolOpenInViewerRequested;
                ActiveToolViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(PdfToolViewModelBase.IsToolStarred) && ActiveToolCard != null && ActiveToolViewModel != null)
                    {
                        if (ActiveToolCard.IsStarred != ActiveToolViewModel.IsToolStarred)
                        {
                            ActiveToolCard.ToggleStar();
                            RefreshStarredTools();
                        }
                    }
                };
            }

            ToolRunner?.SetupForTool(card.Definition);
            if (ToolRunner != null)
            {
                ToolRunner.IsToolStarred = card.IsStarred;
            }
            OpenToolRequested?.Invoke(toolId);
        }
    }

    private void OnToolOpenInEditorRequested(string filePath)
    {
        OpenInEditorRequested?.Invoke(filePath);
    }

    private void OnToolOpenInViewerRequested(string filePath)
    {
        OpenInViewerRequested?.Invoke(filePath);
    }

    [RelayCommand]
    public void BackToTools()
    {
        IsToolPageActive = false;
        ActiveToolCard = null;
        if (ActiveToolViewModel != null)
        {
            ActiveToolViewModel.BackRequested -= BackToTools;
            ActiveToolViewModel.OpenInEditorRequested -= OnToolOpenInEditorRequested;
            ActiveToolViewModel.OpenInViewerRequested -= OnToolOpenInViewerRequested;
            ActiveToolViewModel = null;
        }
    }

    [RelayCommand]
    public void ToggleStarActiveTool()
    {
        if (ActiveToolCard != null)
        {
            ActiveToolCard.ToggleStar();
            if (ActiveToolViewModel != null)
            {
                ActiveToolViewModel.IsToolStarred = ActiveToolCard.IsStarred;
            }
            if (ToolRunner != null)
            {
                ToolRunner.IsToolStarred = ActiveToolCard.IsStarred;
            }
            RefreshStarredTools();
        }
    }

    public void RefreshStarredTools()
    {
        StarredTools.Clear();
        foreach (var tool in AllTools.Where(t => t.IsStarred))
        {
            StarredTools.Add(tool);
        }
        OnPropertyChanged(nameof(HasStarredTools));
    }

    [RelayCommand]
    public void SelectTool(PdfToolId toolId)
    {
        if (toolId == PdfToolId.WorkflowBuilder)
        {
            OpenWorkflowBuilderRequested?.Invoke();
        }
        else
        {
            OpenToolPage(toolId);
        }
    }

    [RelayCommand]
    public void OpenWorkflowBuilder()
    {
        OpenWorkflowBuilderRequested?.Invoke();
    }

    [RelayCommand]
    public void SelectTemplate(string? templateName)
    {
        OpenTemplateRequested?.Invoke(templateName);
    }

    [RelayCommand]
    public void OpenFile()
    {
        OpenFileRequested?.Invoke();
    }

    [RelayCommand]
    public void OpenRecent(string filePath)
    {
        OpenRecentRequested?.Invoke(filePath);
    }

    [RelayCommand]
    public void OpenInViewer(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            OpenInViewerRequested?.Invoke(filePath);
        }
        else
        {
            OpenFileRequested?.Invoke();
        }
    }

    [RelayCommand]
    public void RemoveRecent(string filePath)
    {
        _recentService.Remove(filePath);
        RefreshRecent();
    }

    [RelayCommand]
    public void ClearRecent()
    {
        _recentService.Clear();
        RefreshRecent();
    }

    // --- Helpers ---

    public void RefreshRecent()
    {
        RecentDocuments.Clear();
        var items = _recentService.Load();
        foreach (var item in items)
        {
            RecentDocuments.Add(item);
        }
        OnPropertyChanged(nameof(HasRecentDocuments));

        _ = LoadRecentPreviewsAsync(items);
    }

    private async Task LoadRecentPreviewsAsync(List<RecentDocumentItem> items)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.FilePath) || !File.Exists(item.FilePath))
                continue;

            try
            {
                if (item.FilePath.EndsWith(".frypdf", StringComparison.OrdinalIgnoreCase) ||
                    item.FilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var doc = await _persistenceService.LoadProjectAsync(item.FilePath);
                    if (doc != null && doc.Pages.Count > 0)
                    {
                        var pageVm = new PageViewModel();
                        pageVm.LoadFromModel(doc.Pages[0]);
                        item.PagePreview = pageVm;
                        item.PageCount = doc.Pages.Count;
                        item.FormatDescription = $"{doc.Pages[0].Format} {doc.Pages[0].Orientation}";
                    }
                }
            }
            catch { }
        }
    }
}
