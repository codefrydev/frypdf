using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class PdfToWordToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private bool _extractTables = true;

    [ObservableProperty]
    private bool _extractImages = true;

    [ObservableProperty]
    private bool _ocrFallback;

    public PdfToWordToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new WordConversionOptions
        {
            InputFilePath = PrimaryInputFile,
            ExtractTables = ExtractTables,
            ExtractImages = ExtractImages,
            OcrFallback = OcrFallback
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.PdfToWord, options, progress, ct);
    }
}
