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

public partial class JpgToPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private PageFormat _pageFormat = PageFormat.A4;

    [ObservableProperty]
    private PageOrientation _orientation = PageOrientation.Portrait;

    [ObservableProperty]
    private bool _autoOrientation = true;

    [ObservableProperty]
    private bool _fitToPage = true;

    [ObservableProperty]
    private double _marginPoints = 20;

    [ObservableProperty]
    private int _imagesPerPage = 1;

    public override bool UsesWorkspaceShell => true;

    public JpgToPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new ImagesToPdfOptions
        {
            ImageFiles = SelectedFiles.ToList(),
            PageFormat = PageFormat,
            Orientation = Orientation,
            AutoOrientation = AutoOrientation,
            FitToPage = FitToPage,
            MarginPoints = MarginPoints,
            ImagesPerPage = ImagesPerPage
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.JpgToPdf, options, progress, ct);
    }
}
