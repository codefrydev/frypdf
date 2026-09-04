using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel for the interactive Help &amp; Guides Page in FryPDF.
/// Provides live multi-term search, category filtering, detailed step-by-step walkthroughs, pro tips, and direct tool launching.
/// </summary>
public partial class HelpGuideViewModel : ViewModelBase
{
    private readonly IHelpGuideService _helpService;

    // --- Events ---
    public event Action<PdfToolId>? ToolLaunchRequested;

    // --- Observable State ---

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _selectedCategory = "All Guides";

    [ObservableProperty]
    private HelpGuideItem? _selectedGuide;

    [ObservableProperty]
    private bool _isDetailViewActive;

    public ObservableCollection<HelpGuideItem> AllGuides { get; } = new();
    public ObservableCollection<HelpGuideItem> FilteredGuides { get; } = new();
    public ObservableCollection<HelpGuideItem> FeaturedGuides { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    public int MatchingGuidesCount => FilteredGuides.Count;
    public bool HasNoMatchingGuides => FilteredGuides.Count == 0;
    public bool HasActiveSearch => !string.IsNullOrWhiteSpace(SearchQuery);

    public HelpGuideViewModel() : this(new HelpGuideService()) { }

    public HelpGuideViewModel(IHelpGuideService helpService)
    {
        _helpService = helpService ?? new HelpGuideService();
        InitializeGuides();
    }

    private void InitializeGuides()
    {
        AllGuides.Clear();
        FeaturedGuides.Clear();
        Categories.Clear();

        var categories = _helpService.GetAllCategories();
        foreach (var cat in categories)
        {
            Categories.Add(cat);
        }

        var all = _helpService.GetAllGuides();
        foreach (var item in all)
        {
            AllGuides.Add(item);
            if (item.IsFeatured)
            {
                FeaturedGuides.Add(item);
            }
        }

        UpdateFilteredGuides();
    }

    // --- Search & Filter Synchronization ---

    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(HasActiveSearch));
        UpdateFilteredGuides();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        UpdateFilteredGuides();
    }

    partial void OnSelectedGuideChanged(HelpGuideItem? value)
    {
        IsDetailViewActive = value != null;
    }

    private void UpdateFilteredGuides()
    {
        FilteredGuides.Clear();

        string query = SearchQuery.Trim();
        bool hasQuery = !string.IsNullOrWhiteSpace(query);
        string category = SelectedCategory;
        bool isAllCategory = string.IsNullOrWhiteSpace(category) ||
                             category.Equals("All Guides", StringComparison.OrdinalIgnoreCase) ||
                             category.Equals("All", StringComparison.OrdinalIgnoreCase);

        foreach (var guide in AllGuides)
        {
            bool matchesCategory = isAllCategory || guide.Category.Equals(category, StringComparison.OrdinalIgnoreCase);
            bool matchesQuery = !hasQuery || MatchesSearch(guide, query);

            if (matchesCategory && matchesQuery)
            {
                FilteredGuides.Add(guide);
            }
        }

        OnPropertyChanged(nameof(MatchingGuidesCount));
        OnPropertyChanged(nameof(HasNoMatchingGuides));
    }

    private bool MatchesSearch(HelpGuideItem guide, string query)
    {
        var terms = query.Split(new[] { ' ', ',', '+', '&' }, StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length == 0) return true;

        string aggregate = $"{guide.Title} {guide.Summary} {guide.Description} {guide.Category} {guide.Keywords} {guide.Badge} {guide.SupportedFormats} {string.Join(" ", guide.Steps)} {string.Join(" ", guide.KeyFeatures)} {string.Join(" ", guide.ProTips)}";

        return terms.All(term => aggregate.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    // --- Commands ---

    [RelayCommand]
    public void SetCategory(string category)
    {
        SelectedCategory = category;
        IsDetailViewActive = false;
        SelectedGuide = null;
    }

    [RelayCommand]
    public void SelectGuide(HelpGuideItem? guide)
    {
        if (guide == null) return;
        SelectedGuide = guide;
        IsDetailViewActive = true;
    }

    [RelayCommand]
    public void BackToGrid()
    {
        SelectedGuide = null;
        IsDetailViewActive = false;
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = "";
    }

    [RelayCommand]
    public void LaunchTool(HelpGuideItem? guide)
    {
        var target = guide ?? SelectedGuide;
        if (target?.RelatedToolId.HasValue == true)
        {
            ToolLaunchRequested?.Invoke(target.RelatedToolId.Value);
        }
    }

    [RelayCommand]
    public void OpenTopicById(string topicId)
    {
        var match = _helpService.GetGuideById(topicId);
        if (match != null)
        {
            SelectedGuide = match;
            IsDetailViewActive = true;
        }
    }

    [RelayCommand]
    public void OpenGuideForTool(PdfToolId toolId)
    {
        var match = _helpService.GetGuideByToolId(toolId);
        if (match != null)
        {
            SelectedGuide = match;
            IsDetailViewActive = true;
        }
    }

    [RelayCommand]
    public void OpenExternalLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Launcher != null)
            {
                _ = desktop.MainWindow.Launcher.LaunchUriAsync(new Uri(url));
            }
        }
        catch { }
    }
}
