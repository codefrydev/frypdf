using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Conversion;

public partial class PdfToWordToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private bool _extractTables = true;

    [ObservableProperty]
    private bool _extractImages = true;

    [ObservableProperty]
    private bool _ocrFallback;

    public override bool UsesWorkspaceShell => true;

    public PdfToWordToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new WordConversionOptions
        {
            InputFilePath = file,
            ExtractTables = ExtractTables,
            ExtractImages = ExtractImages,
            OcrFallback = OcrFallback
        }, progress, ct);
    }
}
