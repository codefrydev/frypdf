using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel for the Google Docs-inspired Home / Start Screen.
/// Communicates back to the shell via the <see cref="OpenEditorRequested"/> event.
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly IRecentDocumentsService _recentService;

    // --- Events to tell the shell what to do ---
    public event Action<string?>? OpenTemplateRequested;   // templateName (null = blank)
    public event Action? OpenFileRequested;
    public event Action<string>? OpenRecentRequested;      // file path

    // --- Observable State ---

    [ObservableProperty]
    private string _templateSearchQuery = "";

    [ObservableProperty]
    private string _selectedTemplateCategory = "All";

    public ObservableCollection<RecentDocumentItem> RecentDocuments { get; } = new();

    // --- Constructor ---

    public HomeViewModel() : this(new RecentDocumentsService()) { }

    public HomeViewModel(IRecentDocumentsService recentService)
    {
        _recentService = recentService;
        RefreshRecent();
    }

    // --- Template Filtering ---

    partial void OnTemplateSearchQueryChanged(string value) => NotifyTemplateVisibilities();
    partial void OnSelectedTemplateCategoryChanged(string value) => NotifyTemplateVisibilities();

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
        OnPropertyChanged(nameof(HasNoMatchingTemplates));
    }

    private bool CheckTemplateMatch(string category, string name, string description)
    {
        if (SelectedTemplateCategory != "All" &&
            !string.Equals(SelectedTemplateCategory, category, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(TemplateSearchQuery)) return true;

        var q = TemplateSearchQuery.Trim();
        return name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || description.Contains(q, StringComparison.OrdinalIgnoreCase)
            || category.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsBlankTemplateVisible               => CheckTemplateMatch("General",     "Blank Canvas",             "Start fresh with a clean customizable canvas");
    public bool IsAnnualReportTemplateVisible        => CheckTemplateMatch("Corporate",   "Annual Corporate Report",  "Executive summary, financial metrics, and charts");
    public bool IsInvoiceTemplateVisible             => CheckTemplateMatch("Finance",     "Modern Service Invoice",   "Itemized billing table and payment terms");
    public bool IsResumeTemplateVisible              => CheckTemplateMatch("Career",      "Executive Resume / CV",    "Complete CV with QR code, competencies, and metrics");
    public bool IsAcademicPaperTemplateVisible       => CheckTemplateMatch("Academic",    "Academic Research Paper",  "2-column formatted research paper layout");
    public bool IsCertificateTemplateVisible         => CheckTemplateMatch("Certificates","Certificate of Achievement","Award of excellence and official recognition credential");
    public bool IsCertificateNavyGoldTemplateVisible => CheckTemplateMatch("Certificates","Executive Certificate of Honor","Prestigious corporate recognition with navy and gold crest");
    public bool IsDiplomaAcademicTemplateVisible     => CheckTemplateMatch("Certificates","Collegiate Academic Diploma","Formal university degree with ornamental borders and seal");
    public bool IsWeddingTraditionalTemplateVisible  => CheckTemplateMatch("Events & Invitations", "Traditional Indian Wedding Invitation", "Festive marigold toran, Ganesha crest, and dual Muhurtham & Reception schedule");
    public bool IsWeddingRoyalFloralTemplateVisible  => CheckTemplateMatch("Events & Invitations", "Royal Botanical Wedding Invitation", "Luxury gold foil botanical laurel wreath and calligraphy");
    public bool IsGalaInvitationTemplateVisible      => CheckTemplateMatch("Events & Invitations", "Charity Gala & Award Night Invitation", "Black-tie executive event with gold Art Deco frame and VIP QR Code");

    public bool HasNoMatchingTemplates =>
        !IsBlankTemplateVisible && !IsAnnualReportTemplateVisible && !IsInvoiceTemplateVisible
        && !IsResumeTemplateVisible && !IsAcademicPaperTemplateVisible && !IsCertificateTemplateVisible
        && !IsCertificateNavyGoldTemplateVisible && !IsDiplomaAcademicTemplateVisible
        && !IsWeddingTraditionalTemplateVisible && !IsWeddingRoyalFloralTemplateVisible && !IsGalaInvitationTemplateVisible;

    public bool HasRecentDocuments => RecentDocuments.Count > 0;

    // --- Commands ---

    [RelayCommand]
    public void SetTemplateCategory(string? category)
    {
        SelectedTemplateCategory = string.IsNullOrWhiteSpace(category) ? "All" : category;
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
        foreach (var item in _recentService.Load())
            RecentDocuments.Add(item);
        OnPropertyChanged(nameof(HasRecentDocuments));
    }
}
