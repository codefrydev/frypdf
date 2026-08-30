using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class SignPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _signerName = "Jane Doe";

    [ObservableProperty]
    private SignatureStyle _style = SignatureStyle.CursiveElegance;

    [ObservableProperty]
    private string _reason = "Document Approved and Verified";

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private int _targetPageNumber = 1;

    [ObservableProperty]
    private double _x = 100;

    [ObservableProperty]
    private double _y = 600;

    [ObservableProperty]
    private double _width = 200;

    [ObservableProperty]
    private double _height = 70;

    public SignPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new SignToolOptions
        {
            InputFilePath = PrimaryInputFile,
            SignerName = SignerName,
            Style = Style,
            Reason = Reason,
            Location = string.IsNullOrWhiteSpace(Location) ? null : Location,
            TargetPageNumber = TargetPageNumber,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.SignPdf, options, progress, ct);
    }
}
