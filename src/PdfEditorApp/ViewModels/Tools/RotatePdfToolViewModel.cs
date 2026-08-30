using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class RotatePdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private int _rotationDegrees = 90;

    [ObservableProperty]
    private PageFilterTarget _targetFilter = PageFilterTarget.All;

    [ObservableProperty]
    private string _customRange = string.Empty;

    public RotatePdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new RotateToolOptions
        {
            InputFilePath = PrimaryInputFile,
            RotationDegrees = RotationDegrees,
            TargetFilter = TargetFilter,
            CustomRange = CustomRange
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.RotatePdf, options, progress, ct);
    }
}
