using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class PdfFormsToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private bool _flattenFields;

    [ObservableProperty]
    private bool _exportFieldValuesJson;

    public PdfFormsToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new FormToolOptions
        {
            InputFilePath = PrimaryInputFile,
            FlattenFields = FlattenFields,
            ExportFieldValuesJson = ExportFieldValuesJson
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.PdfForms, options, progress, ct);
    }
}
