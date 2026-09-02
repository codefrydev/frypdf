using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class ComparePdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _documentAPath = string.Empty;

    [ObservableProperty]
    private string _documentBPath = string.Empty;

    [ObservableProperty]
    private bool _detectTextDiff = true;

    [ObservableProperty]
    private bool _detectVisualDiff = true;

    public override bool UsesWorkspaceShell => true;

    public ComparePdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    [RelayCommand]
    public async Task PickDocumentAAsync()
    {
        if (StorageProvider == null) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select First PDF (Original Document A)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("PDF Document") { Patterns = new[] { "*.pdf" } } }
        });

        if (files != null && files.Count > 0)
        {
            DocumentAPath = files[0].Path.LocalPath;
            if (!SelectedFiles.Contains(DocumentAPath))
            {
                if (SelectedFiles.Count == 0) SelectedFiles.Add(DocumentAPath);
                else SelectedFiles[0] = DocumentAPath;
            }
            ResetState();
        }
    }

    [RelayCommand]
    public async Task PickDocumentBAsync()
    {
        if (StorageProvider == null) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Second PDF (Modified Document B)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("PDF Document") { Patterns = new[] { "*.pdf" } } }
        });

        if (files != null && files.Count > 0)
        {
            DocumentBPath = files[0].Path.LocalPath;
            if (SelectedFiles.Count > 1)
            {
                SelectedFiles[1] = DocumentBPath;
            }
            else
            {
                SelectedFiles.Add(DocumentBPath);
            }
            ResetState();
        }
    }

    public override void SetupInitialFiles(System.Collections.Generic.IEnumerable<string>? filePaths)
    {
        base.SetupInitialFiles(filePaths);
        if (SelectedFiles.Count > 0) DocumentAPath = SelectedFiles[0];
        if (SelectedFiles.Count > 1) DocumentBPath = SelectedFiles[1];
    }

    protected override bool ValidateInputs(out string errorMessage)
    {
        string docA = !string.IsNullOrEmpty(DocumentAPath) ? DocumentAPath : PrimaryInputFile;
        string docB = !string.IsNullOrEmpty(DocumentBPath) ? DocumentBPath : (SelectedFiles.Count > 1 ? SelectedFiles[1] : "");

        if (string.IsNullOrEmpty(docA) || string.IsNullOrEmpty(docB))
        {
            errorMessage = "Please select both Document A and Document B to compare.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        string docA = !string.IsNullOrEmpty(DocumentAPath) ? DocumentAPath : PrimaryInputFile;
        string docB = !string.IsNullOrEmpty(DocumentBPath) ? DocumentBPath : (SelectedFiles.Count > 1 ? SelectedFiles[1] : "");

        var options = new CompareToolOptions
        {
            DocumentAPath = docA,
            DocumentBPath = docB,
            DetectTextDiff = DetectTextDiff,
            DetectVisualDiff = DetectVisualDiff
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.ComparePdf, options, progress, ct);
    }
}
