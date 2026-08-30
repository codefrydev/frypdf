using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class UnlockPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _password = string.Empty;

    public UnlockPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new UnlockToolOptions
        {
            InputFilePath = PrimaryInputFile,
            Password = Password
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.UnlockPdf, options, progress, ct);
    }
}
