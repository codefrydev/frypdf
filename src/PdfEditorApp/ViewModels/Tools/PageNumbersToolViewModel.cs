using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class PageNumbersToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private PageNumberPosition _position = PageNumberPosition.BottomCenter;

    [ObservableProperty]
    private string _template = "Page {n} of {total}";

    [ObservableProperty]
    private int _startingNumber = 1;

    [ObservableProperty]
    private string _fontFamily = "Helvetica";

    [ObservableProperty]
    private double _fontSize = 10;

    [ObservableProperty]
    private string _colorHex = "#334155";

    [ObservableProperty]
    private PageTargetSelection _targetPages = PageTargetSelection.AllPages;

    [ObservableProperty]
    private string _customRange = string.Empty;

    [ObservableProperty]
    private double _marginPoints = 28;

    public override bool UsesWorkspaceShell => true;

    public PageNumbersToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new PageNumberToolOptions
        {
            InputFilePath = file,
            Position = Position,
            Template = Template,
            StartingNumber = StartingNumber,
            FontFamily = FontFamily,
            FontSize = FontSize,
            ColorHex = ColorHex,
            TargetPages = TargetPages,
            CustomRange = CustomRange,
            MarginPoints = MarginPoints
        }, progress, ct);
    }
}
