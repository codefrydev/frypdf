using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class SplitPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private SplitExtractMode _splitMode = SplitExtractMode.SplitEveryNPages;

    [ObservableProperty]
    private int _splitPagesInterval = 1;

    [ObservableProperty]
    private string _splitRangeExpression = "1-3, 5, 7-10";

    [ObservableProperty]
    private bool _splitOddEven;

    [ObservableProperty]
    private bool _extractOddPages;

    public SplitPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new SplitToolOptions
        {
            InputFilePath = PrimaryInputFile,
            Mode = SplitMode,
            PagesPerSplit = SplitPagesInterval,
            RangeExpression = SplitRangeExpression,
            SplitOddEven = SplitOddEven,
            ExtractOddPages = ExtractOddPages
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.SplitPdf, options, progress, ct);
    }
}
