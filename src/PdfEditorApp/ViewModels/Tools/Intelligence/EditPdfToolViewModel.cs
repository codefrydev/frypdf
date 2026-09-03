using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Intelligence;

public partial class EditPdfToolViewModel : PdfToolViewModelBase
{
    public EditPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    [RelayCommand]
    public void LaunchEditor()
    {
        OpenInEditor();
    }

    protected override Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        progress.Report(50);
        OpenInEditor();
        progress.Report(100);

        return Task.FromResult(new ToolExecutionResult
        {
            Success = true,
            OutputFilePath = PrimaryInputFile,
            Message = "Document opened in FryPDF Editor."
        });
    }
}
