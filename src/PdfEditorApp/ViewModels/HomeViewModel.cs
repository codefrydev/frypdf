using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools;
using PdfEditorApp.Templates;
using PdfEditorApp.Templates.General;
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
    private readonly IThemeService? _themeService;

    // --- Events to tell the shell what to do ---
    public event Action<string?>? OpenTemplateRequested;   // templateName (null = blank)
    public event Action? OpenFileRequested;
    public event Action<string>? OpenRecentRequested;      // file path
    public event Action<string>? OpenInEditorRequested;    // file path
    public event Action<string>? OpenInViewerRequested;    // file path
    public event Action? OpenWorkflowBuilderRequested;
    public event Action? OpenBatchGenerationRequested;

    // --- Observable State ---

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _selectedToolCategory = "All";

    [ObservableProperty]
    private string _selectedTemplateCategory = "All";

    [ObservableProperty]
    private string _selectedLicenseCategory = "All";

    [ObservableProperty]
    private HomeNavSection _selectedNavSection = HomeNavSection.Home;

    [ObservableProperty]
    private bool _isToolPageActive;

    [ObservableProperty]
    private PdfToolCardViewModel? _activeToolCard;

    [ObservableProperty]
    private PdfToolViewModelBase? _activeToolViewModel;

    [ObservableProperty]
    private bool _isTemplateGalleryExpanded;

    public ObservableCollection<PdfToolCardViewModel> AllTools { get; } = new();
    public ObservableCollection<PdfToolCardViewModel> QuickTools { get; } = new();
    public ObservableCollection<PdfToolCardViewModel> FilteredTools { get; } = new();
    public ObservableCollection<PdfToolCardViewModel> StarredTools { get; } = new();
    public ObservableCollection<TemplateCardViewModel> AllTemplates { get; } = new();
    public ObservableCollection<TemplateCardViewModel> FilteredTemplates { get; } = new();
    public ObservableCollection<TemplateCardViewModel> FeaturedTemplates { get; } = new();
    public ObservableCollection<RecentDocumentItem> RecentDocuments { get; } = new();
    public ObservableCollection<ThirdPartyToolLicense> AllLicenses { get; } = new();
    public ObservableCollection<ThirdPartyToolLicense> FilteredLicenses { get; } = new();

    public PdfToolCardViewModel? WorkflowBannerCard { get; private set; }

    public PdfToolRunnerViewModel? ToolRunner { get; }

    public bool IsHomeSection => SelectedNavSection == HomeNavSection.Home;
    public bool IsNewDocumentSection => SelectedNavSection == HomeNavSection.NewDocument;
    public bool IsPdfReaderSection => SelectedNavSection == HomeNavSection.PdfReader;
    public bool IsToolsSection => SelectedNavSection is >= HomeNavSection.AllTools and <= HomeNavSection.AiAndAutomation;
    public bool IsStarredSection => SelectedNavSection == HomeNavSection.Starred;
    public bool IsTrashSection => SelectedNavSection == HomeNavSection.Trash;
    public bool IsLicensingSection => SelectedNavSection == HomeNavSection.Licensing;
    public bool IsFontPackagesSection => SelectedNavSection == HomeNavSection.FontPackages;
    public bool IsTesseractDataSection => SelectedNavSection == HomeNavSection.TesseractData;
    public bool IsHelpSection => SelectedNavSection == HomeNavSection.Help;

    public FontManagerViewModel FontManager { get; } = new();
    public TesseractManagerViewModel TesseractManager { get; } = new();
    public HelpGuideViewModel HelpGuide { get; } = new();

    public int MatchingToolsCount => FilteredTools.Count;
    public bool HasNoMatchingTools => FilteredTools.Count == 0;
    public int MatchingTemplatesCount => FilteredTemplates.Count;
    public bool HasNoMatchingTemplates => FilteredTemplates.Count == 0;
    public int MatchingLicensesCount => FilteredLicenses.Count;
    public bool HasNoMatchingLicenses => FilteredLicenses.Count == 0;
    public bool HasRecentDocuments => RecentDocuments.Count > 0;
    public bool HasStarredTools => StarredTools.Count > 0;

    // --- Constructor ---

    public HomeViewModel() : this(new RecentDocumentsService(), new TemplateService(), new ProjectPersistenceService(), new PdfToolRegistry(), null, null, new ThemeService()) { }

    public HomeViewModel(
        IRecentDocumentsService recentService,
        ITemplateService templateService,
        IProjectPersistenceService persistenceService,
        IPdfToolRegistry toolRegistry,
        PdfToolRunnerViewModel? toolRunner = null,
        IPdfToolViewModelFactory? toolViewModelFactory = null,
        IThemeService? themeService = null)
    {
        _recentService = recentService;
        _templateService = templateService;
        _persistenceService = persistenceService;
        _toolRegistry = toolRegistry;
        _toolViewModelFactory = toolViewModelFactory;
        _themeService = themeService;
        ToolRunner = toolRunner;

        if (_themeService != null)
        {
            IsDarkMode = _themeService.IsDarkMode;
            _themeService.ThemeChanged += (mode) =>
            {
                IsDarkMode = _themeService.IsDarkMode;
            };
        }

        if (ToolRunner != null)
        {
            ToolRunner.BackRequested += BackToTools;
        }

        HelpGuide.ToolLaunchRequested += (id) => OpenToolPage(id);

        InitializeTools();
        InitializeTemplates();
        InitializeLicenses();
        RefreshRecent();
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        if (_themeService != null)
        {
            _themeService.ToggleTheme();
            IsDarkMode = _themeService.IsDarkMode;
        }
        else
        {
            IsDarkMode = !IsDarkMode;
        }
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
        UpdateFilteredLicenses();
    }

    partial void OnSelectedToolCategoryChanged(string value) => UpdateFilteredTools();
    partial void OnSelectedTemplateCategoryChanged(string value) => UpdateFilteredTemplates();
    partial void OnSelectedLicenseCategoryChanged(string value) => UpdateFilteredLicenses();

    partial void OnSelectedNavSectionChanged(HomeNavSection value)
    {
        OnPropertyChanged(nameof(IsHomeSection));
        OnPropertyChanged(nameof(IsNewDocumentSection));
        OnPropertyChanged(nameof(IsPdfReaderSection));
        OnPropertyChanged(nameof(IsToolsSection));
        OnPropertyChanged(nameof(IsStarredSection));
        OnPropertyChanged(nameof(IsTrashSection));
        OnPropertyChanged(nameof(IsLicensingSection));
        OnPropertyChanged(nameof(IsFontPackagesSection));
        OnPropertyChanged(nameof(IsTesseractDataSection));
        OnPropertyChanged(nameof(IsHelpSection));

        if (value == HomeNavSection.TesseractData)
        {
            _ = TesseractManager.RefreshStatsAsync();
        }
    }

    [RelayCommand]
    public void OpenHelpGuide(string? topicId = null)
    {
        SelectedNavSection = HomeNavSection.Help;
        IsToolPageActive = false;
        if (!string.IsNullOrWhiteSpace(topicId))
        {
            HelpGuide.OpenTopicById(topicId);
        }
        else
        {
            HelpGuide.BackToGrid();
        }
    }

    [RelayCommand]
    public void OpenHelpForTool(PdfToolId toolId)
    {
        SelectedNavSection = HomeNavSection.Help;
        IsToolPageActive = false;
        HelpGuide.OpenGuideForTool(toolId);
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
        OpenToolPage(toolId, null);
    }

    public void OpenToolPage(PdfToolId toolId, string? initialFilePath = null)
    {
        if (toolId == PdfToolId.BatchMailMerge)
        {
            OpenBatchGenerationRequested?.Invoke();
            return;
        }

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
                    ActiveToolViewModel.NavigateToToolRequested -= OnToolNavigateToToolRequested;
                }

                ActiveToolViewModel = _toolViewModelFactory.Create(toolId);
                ActiveToolViewModel.StorageProvider = MainViewModel.StorageProvider;
                ActiveToolViewModel.IsToolStarred = card.IsStarred;
                if (!string.IsNullOrEmpty(initialFilePath))
                {
                    ActiveToolViewModel.SetupInitialFiles(new[] { initialFilePath });
                }
                ActiveToolViewModel.BackRequested += BackToTools;
                ActiveToolViewModel.OpenInEditorRequested += OnToolOpenInEditorRequested;
                ActiveToolViewModel.OpenInViewerRequested += OnToolOpenInViewerRequested;
                ActiveToolViewModel.NavigateToToolRequested += OnToolNavigateToToolRequested;
                ActiveToolViewModel.HelpGuideRequested += (id) => OpenHelpForTool(id);
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
        }
    }

    private void OnToolNavigateToToolRequested(PdfToolId nextToolId, string targetFile)
    {
        OpenToolPage(nextToolId, targetFile);
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
            ActiveToolViewModel.NavigateToToolRequested -= OnToolNavigateToToolRequested;
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
        OpenInViewerRequested?.Invoke(filePath ?? string.Empty);
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

    // --- Licensing & Third-Party Open Source Initialization ---

    private void InitializeLicenses()
    {
        AllLicenses.Clear();

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "Avalonia UI",
            Version = "12.1.1",
            LicenseType = "MIT License",
            Category = "UI & Graphics Frameworks",
            Purpose = "Core cross-platform modern XAML UI application framework providing high-performance GPU-accelerated graphics, styling, smooth vector transitions, and native window management on macOS, Windows, and Linux.",
            Maintainer = "AvaloniaUI Team & .NET Foundation Community",
            ProjectUrl = "https://avaloniaui.net",
            IconKind = "ApplicationOutline",
            AccentColorHex = "#7029E6",
            LicenseText = """
MIT License

Copyright (c) 2014-2026 AvaloniaUI OÜ

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "QuestPDF",
            Version = "2026.8.0",
            LicenseType = "Community / MIT",
            Category = "PDF & Document Engines",
            Purpose = "High-performance vector PDF rendering and generation engine utilizing fluent layout APIs, smart pagination, table structures, vector shapes, and sub-millimeter typographical accuracy.",
            Maintainer = "Marcin Ziąbek & QuestPDF Contributors",
            ProjectUrl = "https://www.questpdf.com",
            IconKind = "FilePdfBox",
            AccentColorHex = "#FF4500",
            LicenseText = """
QuestPDF Community License / MIT License

Copyright (c) 2020-2026 Marcin Ziąbek

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "PdfPig & Skia Rendering",
            Version = "0.1.16.1",
            LicenseType = "Apache 2.0",
            Category = "PDF & Document Engines",
            Purpose = "Pure C# low-level PDF parsing and extraction engine. Extracts structured text glyphs, font matrices, bounding boxes, vector curves, and rasterizes high-fidelity pages to Skia surfaces for the PDF viewer.",
            Maintainer = "Eli White (UglyToad) & Open Source Contributors",
            ProjectUrl = "https://github.com/UglyToad/PdfPig",
            IconKind = "VectorCircle",
            AccentColorHex = "#0284C7",
            LicenseText = """
Apache License
Version 2.0, January 2004
http://www.apache.org/licenses/

Copyright 2018-2026 UglyToad (Eli White) and contributors

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "PdfSharpCore",
            Version = "1.3.67",
            LicenseType = "MIT License",
            Category = "PDF & Document Engines",
            Purpose = "Low-level PDF manipulation engine handling direct binary page extraction, PDF merging, page splitting, rotation, AES-128/256 document security encryption, metadata injection, and Bates stamping.",
            Maintainer = "Stefan Lange, empira Software & Community",
            ProjectUrl = "https://github.com/stefan-lange/pdfsharpcore",
            IconKind = "ShieldLockOutline",
            AccentColorHex = "#10B981",
            LicenseText = """
MIT License

Copyright (c) 2005-2026 empira Software GmbH, Troisdorf (Germany)
Copyright (c) 2017-2026 Stefan Lange and PdfSharpCore contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "SkiaSharp",
            Version = "3.116.1",
            LicenseType = "MIT License",
            Category = "UI & Graphics Frameworks",
            Purpose = "Cross-platform 2D graphics API based on Google's Skia Graphics Library. Powers fluid continuous canvas zooming, anti-aliased Bézier curve evaluation, sub-pixel text rendering, and image caching.",
            Maintainer = "Mono Project & .NET Foundation",
            ProjectUrl = "https://github.com/mono/SkiaSharp",
            IconKind = "Draw",
            AccentColorHex = "#0EA5E9",
            LicenseText = """
MIT License

Copyright (c) 2015-2026 Xamarin Inc.
Copyright (c) 2017-2026 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "Tabula Table Extractor",
            Version = "1.0.1",
            LicenseType = "MIT License",
            Category = "Office & Data Formats",
            Purpose = "Spatial table extraction engine that detects table boundaries, cell grids, columns, and data coordinates from unstructured PDF documents for CSV, JSON, and Excel tabular conversions.",
            Maintainer = "Manuel Aristarán & Tabula Contributors",
            ProjectUrl = "https://github.com/tabulapdf/tabula-java",
            IconKind = "Table",
            AccentColorHex = "#F59E0B",
            LicenseText = """
MIT License

Copyright (c) 2014-2026 Manuel Aristarán, Mike Tigas, and Jeremy B. Merrill

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "DocumentFormat.OpenXml",
            Version = "3.5.1",
            LicenseType = "MIT License",
            Category = "Office & Data Formats",
            Purpose = "High-speed OpenXML engine for reading, parsing, and exporting structured Microsoft Word (.docx), Excel (.xlsx), and PowerPoint (.pptx) documents from converted PDF files.",
            Maintainer = "Microsoft Corporation & .NET Foundation",
            ProjectUrl = "https://github.com/dotnet/Open-XML-SDK",
            IconKind = "FileWordOutline",
            AccentColorHex = "#2563EB",
            LicenseText = """
MIT License

Copyright (c) Microsoft Corporation. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "Material.Icons.Avalonia",
            Version = "3.0.2",
            LicenseType = "MIT License",
            Category = "UI & Graphics Frameworks",
            Purpose = "Vector iconography library supplying thousands of scalable Material Design icon glyphs across toolbars, ribbon bars, inspectors, tool cards, and dialogs.",
            Maintainer = "SKFox5330 & Pictogrammers Community",
            ProjectUrl = "https://github.com/SKFox5330/Material.Icons.Avalonia",
            IconKind = "StarFourPointsOutline",
            AccentColorHex = "#8B5CF6",
            LicenseText = """
MIT License

Copyright (c) 2020-2026 SKFox5330 & Pictogrammers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "CommunityToolkit.Mvvm",
            Version = "8.4.2",
            LicenseType = "MIT License",
            Category = "Architecture & Runtime",
            Purpose = "Official Microsoft .NET Community Toolkit MVVM framework providing high-performance source-generated ObservableObject, RelayCommand, and reactive property notifications.",
            Maintainer = "Microsoft Corporation & Community Toolkit Team",
            ProjectUrl = "https://github.com/CommunityToolkit/dotnet",
            IconKind = "LightningBoltOutline",
            AccentColorHex = "#6366F1",
            LicenseText = """
MIT License

Copyright (c) .NET Foundation and Contributors. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "QRCoder",
            Version = "1.8.0",
            LicenseType = "MIT License",
            Category = "Office & Data Formats",
            Purpose = "Pure C# QR code generating library powering dynamic vector URL, Wi-Fi, vCard, SMS, Email, Crypto, and plain text QR code elements in FryPDF document authoring.",
            Maintainer = "Raffael Herrmann (codebude) & Contributors",
            ProjectUrl = "https://github.com/codebude/QRCoder",
            IconKind = "Qrcode",
            AccentColorHex = "#EC4899",
            LicenseText = """
MIT License

Copyright (c) 2013-2026 Raffael Herrmann

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "Microsoft.Extensions.DependencyInjection",
            Version = "10.0.11",
            LicenseType = "MIT License",
            Category = "Architecture & Runtime",
            Purpose = "High-performance Inversion of Control (IoC) dependency injection container powering modular service registration, loose coupling, and decoupled testing across all 32 PDF tools.",
            Maintainer = "Microsoft Corporation & .NET Runtime Team",
            ProjectUrl = "https://github.com/dotnet/runtime",
            IconKind = "ToyBrickOutline",
            AccentColorHex = "#0078D4",
            LicenseText = """
MIT License

Copyright (c) .NET Foundation and Contributors. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = ".NET 10 Runtime & Base Libraries",
            Version = "10.0",
            LicenseType = "MIT License",
            Category = "Architecture & Runtime",
            Purpose = "Modern cross-platform .NET 10 runtime engine providing hardware-accelerated SIMD (AVX/NEON) vector mathematics, AES-256 cryptography primitives, Task-based asynchronous I/O, and garbage collection.",
            Maintainer = "Microsoft Corporation & .NET Foundation",
            ProjectUrl = "https://dotnet.microsoft.com",
            IconKind = "Microsoft",
            AccentColorHex = "#512BD4",
            LicenseText = """
MIT License

Copyright (c) .NET Foundation and Contributors. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "LiveChartsCore & LiveChartsCore.SkiaSharpView",
            Version = "2.0.5",
            LicenseType = "MIT License",
            Category = "UI & Graphics Frameworks",
            Purpose = "Modern, fluid, and hardware-accelerated charting library for .NET. Renders high-performance bar charts, line graphs, and pie/doughnut charts with SkiaSharp integration directly into PDF documents and vector canvases.",
            Maintainer = "Alberto Rodriguez (beto-rodriguez) & LiveCharts Contributors",
            ProjectUrl = "https://github.com/beto-rodriguez/LiveCharts2",
            IconKind = "ChartBoxOutline",
            AccentColorHex = "#3B82F6",
            LicenseText = """
MIT License

Copyright (c) 2020-2026 Alberto Rodriguez

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "Microsoft.Extensions.Logging.Abstractions",
            Version = "10.0.1",
            LicenseType = "MIT License",
            Category = "Architecture & Runtime",
            Purpose = "Structured logging abstractions and diagnostic interfaces for .NET. Powers logging pipelines, diagnostic tracing, and pipeline telemetry across core PDF deconstruction, OCR processing, and document layout engines.",
            Maintainer = "Microsoft Corporation & .NET Foundation",
            ProjectUrl = "https://github.com/dotnet/runtime",
            IconKind = "FormatListBulleted",
            AccentColorHex = "#2563EB",
            LicenseText = """
MIT License

Copyright (c) .NET Foundation and Contributors. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "SIL Open Font License (OFL 1.1) Typeface Collection",
            Version = "1.1",
            LicenseType = "SIL OFL 1.1",
            Category = "Typography & Typefaces",
            Purpose = "Comprehensive open-source multilingual font collection powering FryPDF typography, including Google Noto Sans (Devanagari, Tamil, Telugu, Bengali, Gujarati, Kannada, Malayalam, Arabic, Urdu, Hebrew, Thai, Lao, Khmer, Myanmar, Sinhala, CJK), Inter, Poppins, Montserrat, Raleway, Lato, Fira Code, Caveat, Orbitron, and Oswald.",
            Maintainer = "SIL International, Google Fonts & Type Designers",
            ProjectUrl = "https://scripts.sil.org/OFL",
            IconKind = "FormatFont",
            AccentColorHex = "#D946EF",
            LicenseText = """
SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007

PREAMBLE
The goals of the Open Font License (OFL) are to stimulate worldwide development of collaborative font projects, to support the font creation efforts of academic and linguistic communities, and to provide a free and open framework in which fonts may be shared and improved in partnership with others.

The OFL allows the licensed fonts to be used, studied, modified and redistributed freely as long as they are not sold by themselves. The fonts, including any derivative works, can be bundled, embedded, redistributed and/or sold with any software provided that any reserved names are not used by derivative works. The fonts and derivatives, however, cannot be released under any other type of license. The requirement for fonts to remain under this license does not apply to any document created using the fonts or their derivatives.

PERMISSION & CONDITIONS
Permission is hereby granted, free of charge, to any person obtaining a copy of the Font Software, to use, study, copy, merge, embed, modify, redistribute, and sell modified and unmodified copies of the Font Software, subject to the following conditions:

1) Neither the Font Software nor any of its individual components, in Original or Modified Versions, may be sold by itself.
2) Original or Modified Versions of the Font Software may be bundled, redistributed and/or sold with any software, provided that each copy contains the above copyright notice and this license.
3) No Modified Version of the Font Software may use the Reserved Font Name(s) unless explicit written permission is granted by the corresponding Copyright Holder.
4) The name(s) of the Copyright Holder(s) or the Author(s) of the Font Software shall not be used to promote, endorse or advertise any Modified Version, except to acknowledge the contribution(s) of the Copyright Holder(s) and the Author(s) or with their explicit written permission.
5) The Font Software, modified or unmodified, in part or in whole, must be distributed entirely under this license, and must not be distributed under any other license.

TERMINATION
This license becomes null and void if any of the above conditions are not met.

DISCLAIMER
THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM OTHER DEALINGS IN THE FONT SOFTWARE.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "Roboto & Roboto Mono Font Family",
            Version = "2.138",
            LicenseType = "Apache 2.0",
            Category = "Typography & Typefaces",
            Purpose = "Google's flagship sans-serif and monospaced typefaces designed by Christian Robertson. Serves as the primary document reader font, technical tables, code spans, and corporate report templates.",
            Maintainer = "Google LLC & Christian Robertson",
            ProjectUrl = "https://github.com/googlefonts/roboto",
            IconKind = "FormatText",
            AccentColorHex = "#EA4335",
            LicenseText = """
Apache License
Version 2.0, January 2004
http://www.apache.org/licenses/

Copyright 2011 Google Inc. All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
"""
        });

        AllLicenses.Add(new ThirdPartyToolLicense
        {
            Name = "Ubuntu Font Family",
            Version = "0.83",
            LicenseType = "Ubuntu Font Licence 1.0",
            Category = "Typography & Typefaces",
            Purpose = "Distinctive contemporary humanist sans-serif typeface designed by Dalton Maag for Canonical Ltd. Included in the Creative & Design typography package for modern headings, branded certificates, and flyers.",
            Maintainer = "Canonical Ltd & Dalton Maag",
            ProjectUrl = "https://design.ubuntu.com/font/",
            IconKind = "Alphabetical",
            AccentColorHex = "#E95420",
            LicenseText = """
Ubuntu Font Licence - Version 1.0
https://ubuntu.com/legal/font-licence

Copyright 2010 Canonical Ltd.
Licensed under the Ubuntu Font Licence version 1.0.

Permission is hereby granted, free of charge, to any person obtaining a copy of the Font Software, to use, study, copy, merge, embed, modify, redistribute, and sell modified and unmodified copies of the Font Software, subject to the following conditions:

1. Neither the Font Software nor any of its individual components, in Original or Modified Versions, may be sold by itself.
2. Original or Modified Versions of the Font Software may be bundled, redistributed and/or sold with any software, provided that each copy contains the above copyright notice and this license.
3. No Modified Version of the Font Software may use the Reserved Font Name(s) unless explicit written permission is granted by the corresponding Copyright Holder.
4. The name(s) of the Copyright Holder(s) or the Author(s) of the Font Software shall not be used to promote, endorse or advertise any Modified Version.
5. The Font Software, modified or unmodified, in part or in whole, must be distributed entirely under this license.

THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
"""
        });

        UpdateFilteredLicenses();
    }

    private void UpdateFilteredLicenses()
    {
        FilteredLicenses.Clear();
        foreach (var lib in AllLicenses)
        {
            if (MatchesLicenseCategory(lib) && MatchesLicenseSearch(lib))
            {
                FilteredLicenses.Add(lib);
            }
        }

        OnPropertyChanged(nameof(MatchingLicensesCount));
        OnPropertyChanged(nameof(HasNoMatchingLicenses));
    }

    private bool MatchesLicenseCategory(ThirdPartyToolLicense lib)
    {
        if (SelectedLicenseCategory == "All") return true;

        return string.Equals(SelectedLicenseCategory, lib.Category, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesLicenseSearch(ThirdPartyToolLicense lib)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;
        var q = SearchQuery.Trim();
        return lib.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || lib.Category.Contains(q, StringComparison.OrdinalIgnoreCase)
            || lib.LicenseType.Contains(q, StringComparison.OrdinalIgnoreCase)
            || lib.Purpose.Contains(q, StringComparison.OrdinalIgnoreCase)
            || lib.Maintainer.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    public void SetLicenseCategory(string? category)
    {
        SelectedLicenseCategory = string.IsNullOrWhiteSpace(category) ? "All" : category;
    }

    [RelayCommand]
    public void ClearLicenseSearch()
    {
        SearchQuery = "";
        SelectedLicenseCategory = "All";
        UpdateFilteredLicenses();
    }

    [RelayCommand]
    public void ToggleLicenseExpand(ThirdPartyToolLicense? license)
    {
        if (license != null)
        {
            license.IsExpanded = !license.IsExpanded;
        }
    }

    [RelayCommand]
    public async Task CopyLicenseText(ThirdPartyToolLicense? license)
    {
        if (license == null) return;
        var textToCopy = $"--- {license.Name} ({license.Version}) ---\nLicense: {license.LicenseType}\nMaintainer: {license.Maintainer}\nProject URL: {license.ProjectUrl}\n\n{license.LicenseText}";
        await CopyToClipboardAsync(textToCopy);
    }

    [RelayCommand]
    public async Task CopyFullAttributions()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("FryPDF — Open Source & Third-Party Software Attribution Notice");
        sb.AppendLine("Copyright (c) 2026 Code Fry Dev. All rights reserved.");
        sb.AppendLine("================================================================================\n");

        foreach (var lib in AllLicenses)
        {
            sb.AppendLine($"--------------------------------------------------------------------------------");
            sb.AppendLine($"Package: {lib.Name} (v{lib.Version})");
            sb.AppendLine($"Category: {lib.Category}");
            sb.AppendLine($"License: {lib.LicenseType}");
            sb.AppendLine($"Maintainer: {lib.Maintainer}");
            sb.AppendLine($"URL: {lib.ProjectUrl}");
            sb.AppendLine($"Purpose: {lib.Purpose}");
            sb.AppendLine($"--------------------------------------------------------------------------------");
            sb.AppendLine(lib.LicenseText.Trim());
            sb.AppendLine();
        }

        await CopyToClipboardAsync(sb.ToString());
    }

    [RelayCommand]
    public async Task OpenLibraryWebsite(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Launcher != null)
            {
                await desktop.MainWindow.Launcher.LaunchUriAsync(new Uri(url));
            }
            else
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }
        catch
        {
            await CopyToClipboardAsync(url);
        }
    }

    private static async Task CopyToClipboardAsync(string text)
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
            {
                await desktop.MainWindow.Clipboard.SetTextAsync(text);
            }
        }
        catch { }
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

