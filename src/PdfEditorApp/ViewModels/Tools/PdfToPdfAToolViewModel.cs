using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class PdfToPdfAToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private PdfAStandard _standard = PdfAStandard.PdfA2b;

    public PdfToPdfAToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new PdfAToolOptions
        {
            InputFilePath = PrimaryInputFile,
            Standard = Standard
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.PdfToPdfA, options, progress, ct);
    }
}
