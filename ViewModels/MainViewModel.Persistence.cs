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
            string defaultFileName = Path.ChangeExtension(DocumentTitle, ".pdfproj");

            if (StorageProvider != null)
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save PDF Creator Project",
                    DefaultExtension = "pdfproj",
                    SuggestedFileName = defaultFileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("PDF Creator Project (*.pdfproj)")
                        {
                            Patterns = new[] { "*.pdfproj", "*.json" }
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
                    Title = "Open PDF Creator Project",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("PDF Creator Project (*.pdfproj, *.json)")
                        {
                            Patterns = new[] { "*.pdfproj", "*.json" }
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

    // --- TEMPLATE SELECTION DIALOG COMMANDS ---

    [RelayCommand]
    public void OpenNewDocumentDialog()
    {
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
        var model = templateName?.ToLower() switch
        {
            "invoice" => _templateService.CreateInvoiceTemplate(),
            "resume" => _templateService.CreateResumeTemplate(),
            "academic" => _templateService.CreateAcademicPaperTemplate(),
            "certificate" => _templateService.CreateCertificateTemplate(),
            "annualreport" => _templateService.CreateAnnualReportTemplate(),
            _ => _templateService.CreateBlankDocument()
        };

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
            Subject = DocumentSubject
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
