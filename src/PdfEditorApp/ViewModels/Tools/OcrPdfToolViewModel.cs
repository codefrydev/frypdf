using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class OcrPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _language = "eng";

    [ObservableProperty]
    private bool _generateSearchablePdf = true;

    [ObservableProperty]
    private bool _extractTextOnly;

    public OcrPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new OcrToolOptions
        {
            InputFilePath = file,
            Language = Language,
            GenerateSearchablePdf = GenerateSearchablePdf,
            ExtractTextOnly = ExtractTextOnly
        }, progress, ct);
    }
}
