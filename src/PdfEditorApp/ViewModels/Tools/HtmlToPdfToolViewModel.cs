using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class HtmlToPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _htmlContentOrUrl = "https://";

    [ObservableProperty]
    private PageFormat _format = PageFormat.A4;

    [ObservableProperty]
    private PageOrientation _orientation = PageOrientation.Portrait;

    [ObservableProperty]
    private double _marginPoints = 36;

    [ObservableProperty]
    private bool _includePageNumbers = true;

    public override bool UsesWorkspaceShell => true;

    public HtmlToPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override bool ValidateInputs(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(HtmlContentOrUrl))
        {
            errorMessage = "Please enter a valid URL or HTML content.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        bool isUrl = HtmlContentOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     HtmlContentOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        var options = new HtmlToPdfOptions
        {
            HtmlContentOrUrl = HtmlContentOrUrl,
            IsUrl = isUrl,
            Format = Format,
            Orientation = Orientation,
            MarginPoints = MarginPoints,
            IncludePageNumbers = IncludePageNumbers
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.HtmlToPdf, options, progress, ct);
    }
}
