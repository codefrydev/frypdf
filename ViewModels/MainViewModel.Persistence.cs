using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels;

public partial class MainViewModel
{
    // --- PERSISTENCE & EXPORT STATE ---

    [ObservableProperty]
    private bool _isNewDocumentDialogOpen;

    [ObservableProperty]
    private bool _isExportSuccessDialogOpen;

    [ObservableProperty]
    private string _lastExportedFilePath = "";

    // --- EXPORT & PERSISTENCE COMMANDS ---

    [RelayCommand]
    public async Task ExportPdfAsync()
    {
        try
        {
            UpdateStatus("Generating PDF with QuestPDF engine...");

            string defaultFileName = Path.ChangeExtension(DocumentTitle, ".pdf");
            if (string.IsNullOrEmpty(defaultFileName)) defaultFileName = "Document.pdf";

            string exportPath = "";

            if (StorageProvider != null)
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export PDF Document",
                    DefaultExtension = "pdf",
                    SuggestedFileName = defaultFileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("PDF Documents (*.pdf)")
                        {
                            Patterns = new[] { "*.pdf" }
                        }
                    }
                });

                if (file != null)
                {
                    exportPath = file.Path.LocalPath;
                }
            }

            if (string.IsNullOrEmpty(exportPath))
            {
                // Fallback to Desktop directory
                exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), defaultFileName);
            }

            var docModel = ToDocumentModel();
            await _exportService.ExportToFileAsync(docModel, exportPath);

            LastExportedFilePath = exportPath;
            IsExportSuccessDialogOpen = true;
            ShowToast($"Exported PDF: {Path.GetFileName(exportPath)}", "ExportVariant");
        }
        catch (Exception ex)
        {
            ShowToast($"Export error: {ex.Message}", "AlertCircleOutline");
        }
    }

    [RelayCommand]
    public async Task SaveProjectAsync()
    {
        try
        {
            string savePath = "";
            string defaultFileName = Path.ChangeExtension(DocumentTitle, ".frypdf");

            if (StorageProvider != null)
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save FryPDF Project",
                    DefaultExtension = "frypdf",
                    SuggestedFileName = defaultFileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("FryPDF Project (*.frypdf)")
                        {
                            Patterns = new[] { "*.frypdf", "*.pdfproj", "*.json" }
                        }
                    }
                });

                if (file != null)
                {
                    savePath = file.Path.LocalPath;
                }
            }

            if (string.IsNullOrEmpty(savePath))
            {
                savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), defaultFileName);
            }

            var docModel = ToDocumentModel();
            await _persistenceService.SaveProjectAsync(docModel, savePath);
            ShowToast($"Project saved to {Path.GetFileName(savePath)}", "ContentSaveOutline");
        }
        catch (Exception ex)
        {
            ShowToast($"Save error: {ex.Message}", "AlertCircleOutline");
        }
    }

    [RelayCommand]
    public async Task OpenProjectAsync()
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
                    var path = files[0].Path.LocalPath;
                    var model = await _persistenceService.LoadProjectAsync(path);
                    if (model != null)
                    {
                        LoadFromDocumentModel(model);
                        ShowToast($"Opened Project: {Path.GetFileName(path)}", "FolderOpenOutline");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Open error: {ex.Message}", "AlertCircleOutline");
        }
    }

    // --- TEMPLATE SELECTION & GALLERY DIALOG STATE ---

    [ObservableProperty]
    private string _templateSearchQuery = "";

    [ObservableProperty]
    private string _selectedTemplateCategory = "All";

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

    partial void OnTemplateSearchQueryChanged(string value)
    {
        NotifyTemplateVisibilities();
    }

    partial void OnSelectedTemplateCategoryChanged(string value)
    {
        NotifyTemplateVisibilities();
    }

    private void NotifyTemplateVisibilities()
    {
        OnPropertyChanged(nameof(IsAnnualReportTemplateVisible));
        OnPropertyChanged(nameof(IsInvoiceTemplateVisible));
        OnPropertyChanged(nameof(IsResumeTemplateVisible));
        OnPropertyChanged(nameof(IsAcademicPaperTemplateVisible));
        OnPropertyChanged(nameof(IsCertificateTemplateVisible));
        OnPropertyChanged(nameof(IsBlankTemplateVisible));
        OnPropertyChanged(nameof(HasNoMatchingTemplates));
    }

    private bool CheckTemplateMatch(string templateId, string category, string name, string description)
    {
        if (SelectedTemplateCategory != "All" && !string.Equals(SelectedTemplateCategory, category, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(TemplateSearchQuery))
        {
            return true;
        }

        string q = TemplateSearchQuery.Trim();
        return name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || description.Contains(q, StringComparison.OrdinalIgnoreCase)
            || category.Contains(q, StringComparison.OrdinalIgnoreCase)
            || templateId.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsAnnualReportTemplateVisible => CheckTemplateMatch("annualreport", "Corporate", "Annual Corporate Report", "Executive summary, financial metrics, and chart");
    public bool IsInvoiceTemplateVisible => CheckTemplateMatch("invoice", "Finance", "Modern Service Invoice", "Itemized billing table and payment terms");
    public bool IsResumeTemplateVisible => CheckTemplateMatch("resume", "Career", "Executive Resume / CV", "Complete CV with QR code, competencies, and verified metrics");
    public bool IsAcademicPaperTemplateVisible => CheckTemplateMatch("academic", "Academic", "Academic Research Paper", "2-column formatted research paper layout");
    public bool IsCertificateTemplateVisible => CheckTemplateMatch("certificate", "Certificates", "Certificate of Achievement", "Award of excellence and official recognition credential");
    public bool IsBlankTemplateVisible => CheckTemplateMatch("blank", "General", "Blank Canvas", "Start fresh with a clean customizable canvas");

    public bool HasNoMatchingTemplates => !IsAnnualReportTemplateVisible
                                       && !IsInvoiceTemplateVisible
                                       && !IsResumeTemplateVisible
                                       && !IsAcademicPaperTemplateVisible
                                       && !IsCertificateTemplateVisible
                                       && !IsBlankTemplateVisible;

    // --- TEMPLATE SELECTION DIALOG COMMANDS ---

    [RelayCommand]
    public void OpenNewDocumentDialog()
    {
        TemplateSearchQuery = "";
        SelectedTemplateCategory = "All";
        IsNewDocumentDialogOpen = true;
    }

    [RelayCommand]
    public void CloseNewDocumentDialog()
    {
        IsNewDocumentDialogOpen = false;
    }

    [RelayCommand]
    public void CloseExportSuccessDialog()
    {
        IsExportSuccessDialogOpen = false;
    }

    [RelayCommand]
    public void CloseExportDialog()
    {
        IsExportSuccessDialogOpen = false;
    }

    [RelayCommand]
    public void SelectTemplate(string? templateName)
    {
        CreateNewFromTemplate(templateName);
    }

    [RelayCommand]
    public void CreateNewFromTemplate(string? templateName)
    {
        var model = string.IsNullOrWhiteSpace(templateName)
            ? _templateService.CreateBlankDocument()
            : _templateService.CreateTemplate(templateName);

        LoadFromDocumentModel(model);
        CloseNewDocumentDialog();
        ShowToast($"Created new document from {templateName ?? "Blank"} template", "FilePlusOutline");
    }

    // --- MODEL CONVERSION & SERIALIZATION ---

    public PdfDocumentModel ToDocumentModel()
    {
        var doc = new PdfDocumentModel
        {
            Title = DocumentTitle,
            Author = DocumentAuthor,
            Subject = DocumentSubject,
            SecuritySettings = SecuritySettings.Clone()
        };

        foreach (var pageVm in Pages)
        {
            doc.Pages.Add(pageVm.ToModel());
        }

        return doc;
    }

    public void LoadFromDocumentModel(PdfDocumentModel model)
    {
        DocumentTitle = model.Title;
        DocumentAuthor = model.Author;
        DocumentSubject = model.Subject;
        SecuritySettings = model.SecuritySettings?.Clone() ?? new PdfSecuritySettings();

        Pages.Clear();
        foreach (var pageModel in model.Pages)
        {
            var pageVm = new PageViewModel();
            pageVm.LoadFromModel(pageModel);
            pageVm.SelectionChanged += OnElementSelectionChanged;
            Pages.Add(pageVm);
        }

        if (Pages.Count > 0)
        {
            SelectPage(Pages[0]);
        }

        RefreshOutline();
        RefreshComments();
        UpdateStatus($"Loaded document: {DocumentTitle}");
    }
}
