using System;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class RepairPdfToolViewModel : PdfToolViewModelBase
{
    public RepairPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new RepairToolOptions
        {
            InputFilePath = PrimaryInputFile
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.RepairPdf, options, progress, ct);
    }
}
