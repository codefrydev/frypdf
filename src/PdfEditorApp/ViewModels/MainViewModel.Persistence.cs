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
                else
                {
                    UpdateStatus("Export cancelled.");
                    return;
                }
            }
            else
            {
                exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), defaultFileName);
            }

            var docModel = ToDocumentModel();
            IsBusy = true;
            try
            {
                var progress = new Progress<double>(p => UpdateStatus($"Generating PDF... {p:F0}%"));
                await _exportService.ExportToFileAsync(docModel, exportPath, progress);
            }
            finally
            {
                IsBusy = false;
            }

            LastExportedFilePath = exportPath;
            IsExportSuccessDialogOpen = true;
            ShowToast($"Exported PDF: {Path.GetFileName(exportPath)}", "ExportVariant");
        }
        catch (Exception ex)
        {
            IsBusy = false;
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
                else
                {
                    UpdateStatus("Save cancelled.");
                    return;
                }
            }
            else
            {
                savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), defaultFileName);
            }

            var docModel = ToDocumentModel();
            await _persistenceService.SaveProjectAsync(docModel, savePath);
            // Record in recent documents
            _recentService.Add(new RecentDocumentItem
            {
                FilePath = savePath,
                Title = DocumentTitle,
                LastOpened = DateTime.UtcNow
            });
            Home.RefreshRecent();
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
        // When in editor, use the file picker directly
        await OpenProjectAndEnterEditorAsync();
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
        OpenEditorWithTemplate(templateName);
    }

    [RelayCommand]
    public void CreateNewFromTemplate(string? templateName)
    {
        OpenEditorWithTemplate(templateName);
    }

    // --- MODEL CONVERSION & SERIALIZATION ---

    public PdfDocumentModel ToDocumentModel()
    {
        var doc = new PdfDocumentModel
        {
            Title = DocumentTitle,
            Author = DocumentAuthor,
            Subject = DocumentSubject,
            Keywords = DocumentKeywords,
            Creator = string.IsNullOrWhiteSpace(DocumentCreator) ? "FryPDF" : DocumentCreator,
            Producer = string.IsNullOrWhiteSpace(DocumentProducer) ? "codefrydev.in" : DocumentProducer,
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
        ApplyDocumentMetadata(model);

        // The previous document's undo/redo history is meaningless once its pages are gone —
        // undoing after switching documents must never resurrect a replaced document's state.
        // This also releases any deleted chart/image bitmaps still pinned by that history.
        UndoRedo.Clear();

        Pages.Clear();
        foreach (var pageModel in model.Pages)
        {
            var pageVm = new PageViewModel();
            pageVm.LoadFromModel(pageModel);
            pageVm.SelectionChanged += OnElementSelectionChanged;
            Pages.Add(pageVm);
        }

        FinishLoadingDocument();
    }

    /// <summary>
    /// Same as <see cref="LoadFromDocumentModel"/> but yields back to the dispatcher every
    /// few pages, so opening a large document doesn't freeze the UI thread for the whole
    /// build — used by every user-facing "open a document" path.
    /// </summary>
    public async Task LoadFromDocumentModelAsync(PdfDocumentModel model)
    {
        ApplyDocumentMetadata(model);

        // See LoadFromDocumentModel — the outgoing document's undo/redo history must not
        // survive the switch.
        UndoRedo.Clear();

        Pages.Clear();
        int i = 0;
        foreach (var pageModel in model.Pages)
        {
            var pageVm = new PageViewModel();
            pageVm.LoadFromModel(pageModel);
            pageVm.SelectionChanged += OnElementSelectionChanged;
            Pages.Add(pageVm);

            if (++i % 8 == 0)
            {
                await Task.Yield();
            }
        }

        FinishLoadingDocument();
    }

    private void ApplyDocumentMetadata(PdfDocumentModel model)
    {
        DocumentTitle = model.Title;
        DocumentAuthor = model.Author;
        DocumentSubject = model.Subject;
        DocumentKeywords = model.Keywords ?? "";
        DocumentCreator = string.IsNullOrWhiteSpace(model.Creator) ? "FryPDF" : model.Creator;
        DocumentProducer = string.IsNullOrWhiteSpace(model.Producer) ? "codefrydev.in" : model.Producer;
        SecuritySettings = model.SecuritySettings?.Clone() ?? new PdfSecuritySettings();
    }

    private void FinishLoadingDocument()
    {
        if (Pages.Count > 0)
        {
            SelectPage(Pages[0]);
        }

        RefreshOutline();
        RefreshComments();
        UpdateStatus($"Loaded document: {DocumentTitle}");
        IsBusy = false;
    }
}
