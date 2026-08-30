using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class MergePdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private bool _normalizePageSizes;

    [ObservableProperty]
    private bool _preserveBookmarks = true;

    public MergePdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    [RelayCommand]
    public void MoveFileUp(string filePath)
    {
        int index = SelectedFiles.IndexOf(filePath);
        if (index > 0)
        {
            SelectedFiles.Move(index, index - 1);
        }
    }

    [RelayCommand]
    public void MoveFileDown(string filePath)
    {
        int index = SelectedFiles.IndexOf(filePath);
        if (index >= 0 && index < SelectedFiles.Count - 1)
        {
            SelectedFiles.Move(index, index + 1);
        }
    }

    protected override bool ValidateInputs(out string errorMessage)
    {
        if (SelectedFiles.Count < 2)
        {
            errorMessage = "Please select at least 2 PDF documents to merge.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new MergeToolOptions
        {
            InputFiles = SelectedFiles.ToList(),
            NormalizePageSizes = NormalizePageSizes,
            PreserveBookmarks = PreserveBookmarks
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.MergePdf, options, progress, ct);
    }
}
