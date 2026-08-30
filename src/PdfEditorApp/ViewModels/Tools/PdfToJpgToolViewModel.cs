using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class PdfToJpgToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _outputFormat = "jpg";

    [ObservableProperty]
    private int _dpi = 300;

    [ObservableProperty]
    private int _jpgQuality = 90;

    [ObservableProperty]
    private bool _grayscale;

    [ObservableProperty]
    private PageTargetSelection _targetPages = PageTargetSelection.AllPages;

    [ObservableProperty]
    private string _pageRange = "1";

    public PdfToJpgToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new ImageConversionOptions
        {
            InputFilePath = PrimaryInputFile,
            OutputFormat = OutputFormat,
            Dpi = Dpi,
            JpgQuality = JpgQuality,
            Grayscale = Grayscale,
            TargetPages = TargetPages,
            PageRange = PageRange
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.PdfToJpg, options, progress, ct);
    }
}
