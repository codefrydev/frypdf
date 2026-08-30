using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class WatermarkToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _watermarkText = "CONFIDENTIAL";

    [ObservableProperty]
    private double _opacity = 0.35;

    [ObservableProperty]
    private double _rotationAngle = -45;

    [ObservableProperty]
    private WatermarkPosition _position = WatermarkPosition.Center;

    [ObservableProperty]
    private string _colorHex = "#EF4444";

    [ObservableProperty]
    private double _fontSize = 48;

    [ObservableProperty]
    private PageTargetSelection _targetPages = PageTargetSelection.AllPages;

    [ObservableProperty]
    private string _customRange = string.Empty;

    public WatermarkToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new WatermarkToolOptions
        {
            InputFilePath = PrimaryInputFile,
            Text = WatermarkText,
            Opacity = Opacity,
            RotationAngle = RotationAngle,
            Position = Position,
            ColorHex = ColorHex,
            FontSize = FontSize,
            TargetPages = TargetPages,
            CustomRange = CustomRange
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.Watermark, options, progress, ct);
    }
}
