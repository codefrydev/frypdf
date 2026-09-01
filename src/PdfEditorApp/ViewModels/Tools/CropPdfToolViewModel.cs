using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class CropPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private double _cropMarginTop = 36;

    [ObservableProperty]
    private double _cropMarginBottom = 36;

    [ObservableProperty]
    private double _cropMarginLeft = 36;

    [ObservableProperty]
    private double _cropMarginRight = 36;

    [ObservableProperty]
    private PageTargetSelection _targetPages = PageTargetSelection.AllPages;

    [ObservableProperty]
    private string _customRange = string.Empty;

    public CropPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new CropToolOptions
        {
            InputFilePath = file,
            CropTopPoints = CropMarginTop,
            CropBottomPoints = CropMarginBottom,
            CropLeftPoints = CropMarginLeft,
            CropRightPoints = CropMarginRight,
            TargetPages = TargetPages,
            CustomRange = CustomRange
        }, progress, ct);
    }
}
