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
using PdfEditorApp.Templates;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel for the Google Docs & Canva inspired Home / Start Screen.
/// Provides expandable template gallery with live, realistic previews and recent document management.
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly IRecentDocumentsService _recentService;
    private readonly ITemplateService _templateService;
    private readonly IProjectPersistenceService _persistenceService;

    // --- Events to tell the shell what to do ---
    public event Action<string?>? OpenTemplateRequested;   // templateName (null = blank)
    public event Action? OpenFileRequested;
    public event Action<string>? OpenRecentRequested;      // file path

    // --- Observable State ---

    [ObservableProperty]
    private string _templateSearchQuery = "";

    [ObservableProperty]
    private string _selectedTemplateCategory = "All";

    [ObservableProperty]
    private bool _isTemplateGalleryExpanded;

    public ObservableCollection<TemplateCardViewModel> AllTemplates { get; } = new();
    public ObservableCollection<TemplateCardViewModel> FilteredTemplates { get; } = new();
    public ObservableCollection<TemplateCardViewModel> FeaturedTemplates { get; } = new();
    public ObservableCollection<RecentDocumentItem> RecentDocuments { get; } = new();

    public int MatchingTemplatesCount => FilteredTemplates.Count;
    public bool HasNoMatchingTemplates => FilteredTemplates.Count == 0;
    public bool HasRecentDocuments => RecentDocuments.Count > 0;

    // --- Constructor ---

    public HomeViewModel() : this(new RecentDocumentsService(), new TemplateService(), new ProjectPersistenceService()) { }

    public HomeViewModel(IRecentDocumentsService recentService) : this(recentService, new TemplateService(), new ProjectPersistenceService()) { }

    public HomeViewModel(IRecentDocumentsService recentService, ITemplateService templateService, IProjectPersistenceService persistenceService)
    {
        _recentService = recentService;
        _templateService = templateService;
        _persistenceService = persistenceService;

        InitializeTemplates();
        RefreshRecent();
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

                bool isFeatured = def.Id is "annualreport" or "invoice" or "resume" or "academic" or "certificate";
                string subtitle = def.Id switch
                {
                    "annualreport" => "Executive Summary & Charts",
                    "invoice" => "Itemized Billing & Terms",
                    "resume" => "Executive CV with Live QR",
                    "academic" => "2-Column Research Layout",
                    "certificate" => "Crimson & Gold Award",
                    "certificatenavygold" => "Navy & Gold Crest",
                    "diploma" => "Collegiate Degree",
                    "weddingtraditional" => "Marigold & Ganesha Crest",
                    "weddingroyalfloral" => "Botanical Laurel Wreath",
                    "galainvitation" => "Black-Tie Art Deco",
                    _ => def.Category
                };

                string badge = def.Id switch
                {
                    "annualreport" => "Popular",
                    "invoice" => "Finance",
                    "resume" => "Career",
                    "academic" => "Academic",
                    "certificate" => "Award",
                    "certificatenavygold" => "Executive",
                    "diploma" => "Degree",
                    "weddingtraditional" => "Festive",
                    "weddingroyalfloral" => "Luxury",
                    "galainvitation" => "Art Deco",
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

    // --- Template Filtering ---

    partial void OnTemplateSearchQueryChanged(string value) => UpdateFilteredTemplates();
    partial void OnSelectedTemplateCategoryChanged(string value) => UpdateFilteredTemplates();

    private void UpdateFilteredTemplates()
    {
        FilteredTemplates.Clear();
        FeaturedTemplates.Clear();

        foreach (var t in AllTemplates)
        {
            if (MatchesFilter(t))
            {
                FilteredTemplates.Add(t);
                if (t.IsFeatured)
                {
                    FeaturedTemplates.Add(t);
                }
            }
        }

        // If no featured matches in category, show all filtered in featured slot as fallback
        if (FeaturedTemplates.Count == 0 && FilteredTemplates.Count > 0)
        {
            foreach (var t in FilteredTemplates)
            {
                FeaturedTemplates.Add(t);
            }
        }

        OnPropertyChanged(nameof(MatchingTemplatesCount));
        OnPropertyChanged(nameof(HasNoMatchingTemplates));
        NotifyTemplateVisibilities();
    }

    private bool MatchesFilter(TemplateCardViewModel card)
    {
        if (SelectedTemplateCategory != "All" &&
            !string.Equals(SelectedTemplateCategory, card.Category, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(TemplateSearchQuery)) return true;

        var q = TemplateSearchQuery.Trim();
        return card.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || card.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
            || card.Category.Contains(q, StringComparison.OrdinalIgnoreCase)
            || card.Subtitle.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private void NotifyTemplateVisibilities()
    {
        OnPropertyChanged(nameof(IsBlankTemplateVisible));
        OnPropertyChanged(nameof(IsAnnualReportTemplateVisible));
        OnPropertyChanged(nameof(IsInvoiceTemplateVisible));
        OnPropertyChanged(nameof(IsResumeTemplateVisible));
        OnPropertyChanged(nameof(IsAcademicPaperTemplateVisible));
        OnPropertyChanged(nameof(IsCertificateTemplateVisible));
        OnPropertyChanged(nameof(IsCertificateNavyGoldTemplateVisible));
        OnPropertyChanged(nameof(IsDiplomaAcademicTemplateVisible));
        OnPropertyChanged(nameof(IsWeddingTraditionalTemplateVisible));
        OnPropertyChanged(nameof(IsWeddingRoyalFloralTemplateVisible));
        OnPropertyChanged(nameof(IsGalaInvitationTemplateVisible));
    }

    public bool IsBlankTemplateVisible               => FilteredTemplates.Any(t => t.Id == "");
    public bool IsAnnualReportTemplateVisible        => FilteredTemplates.Any(t => t.Id == "annualreport");
    public bool IsInvoiceTemplateVisible             => FilteredTemplates.Any(t => t.Id == "invoice");
    public bool IsResumeTemplateVisible              => FilteredTemplates.Any(t => t.Id == "resume");
    public bool IsAcademicPaperTemplateVisible       => FilteredTemplates.Any(t => t.Id == "academic");
    public bool IsCertificateTemplateVisible         => FilteredTemplates.Any(t => t.Id == "certificate");
    public bool IsCertificateNavyGoldTemplateVisible => FilteredTemplates.Any(t => t.Id == "certificatenavygold");
    public bool IsDiplomaAcademicTemplateVisible     => FilteredTemplates.Any(t => t.Id == "diploma");
    public bool IsWeddingTraditionalTemplateVisible  => FilteredTemplates.Any(t => t.Id == "weddingtraditional");
    public bool IsWeddingRoyalFloralTemplateVisible  => FilteredTemplates.Any(t => t.Id == "weddingroyalfloral");
    public bool IsGalaInvitationTemplateVisible      => FilteredTemplates.Any(t => t.Id == "galainvitation");

    // --- Commands ---

    [RelayCommand]
    public void ToggleTemplateGallery()
    {
        IsTemplateGalleryExpanded = !IsTemplateGalleryExpanded;
    }

    [RelayCommand]
    public void ExpandTemplateGallery()
    {
        IsTemplateGalleryExpanded = true;
    }

    [RelayCommand]
    public void CollapseTemplateGallery()
    {
        IsTemplateGalleryExpanded = false;
    }

    [RelayCommand]
    public void SetTemplateCategory(string? category)
    {
        SelectedTemplateCategory = string.IsNullOrWhiteSpace(category) ? "All" : category;
    }

    [RelayCommand]
    public void ClearTemplateSearch()
    {
        TemplateSearchQuery = "";
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

        // Asynchronously load previews for recent project files
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
            catch
            {
                // Silently keep default document representation
            }
        }
    }
}
