using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class PowerPointToPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private PageOrientation _orientation = PageOrientation.Landscape;

    public PowerPointToPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new OfficeToPdfOptions
        {
            InputFilePath = PrimaryInputFile,
            Orientation = Orientation
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.PowerPointToPdf, options, progress, ct);
    }
}
