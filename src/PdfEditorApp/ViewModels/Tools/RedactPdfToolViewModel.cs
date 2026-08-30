using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class RedactPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _searchPattern = "CONFIDENTIAL";

    [ObservableProperty]
    private bool _caseSensitive;

    [ObservableProperty]
    private bool _permanentScrubText = true;

    public RedactPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override bool ValidateInputs(out string errorMessage)
    {
        if (!base.ValidateInputs(out errorMessage)) return false;

        if (string.IsNullOrWhiteSpace(SearchPattern))
        {
            errorMessage = "Please enter a search text or pattern to redact.";
            return false;
        }
        return true;
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new RedactionToolOptions
        {
            InputFilePath = PrimaryInputFile,
            SearchPatternToRedact = SearchPattern,
            CaseSensitive = CaseSensitive,
            PermanentScrubText = PermanentScrubText
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.RedactPdf, options, progress, ct);
    }
}
