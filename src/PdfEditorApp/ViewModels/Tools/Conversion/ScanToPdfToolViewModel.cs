using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Conversion;

public partial class ScanToPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private bool _autoDeskew = true;

    [ObservableProperty]
    private bool _enhanceContrast = true;

    [ObservableProperty]
    private bool _whitenBackground = true;

    [ObservableProperty]
    private bool _convertToGrayscale;

    [ObservableProperty]
    private PageFormat _format = PageFormat.A4;

    public override bool UsesWorkspaceShell => true;

    public ScanToPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new ScanToolOptions
        {
            InputImageFiles = SelectedFiles.ToList(),
            AutoDeskew = AutoDeskew,
            EnhanceContrast = EnhanceContrast,
            WhitenBackground = WhitenBackground,
            ConvertToGrayscale = ConvertToGrayscale,
            Format = Format
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.ScanToPdf, options, progress, ct);
    }
}
