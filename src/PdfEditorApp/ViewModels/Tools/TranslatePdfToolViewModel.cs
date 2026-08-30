using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class TranslatePdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _sourceLanguage = "Auto";

    [ObservableProperty]
    private string _targetLanguage = "Spanish";

    [ObservableProperty]
    private bool _preserveLayout = true;

    public TranslatePdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new TranslationOptions
        {
            InputFilePath = PrimaryInputFile,
            SourceLanguage = SourceLanguage,
            TargetLanguage = TargetLanguage,
            PreserveLayout = PreserveLayout
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.TranslatePdf, options, progress, ct);
    }
}
