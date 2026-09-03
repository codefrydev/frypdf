using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Conversion;

public partial class PdfToPdfAToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private PdfAStandard _standard = PdfAStandard.PdfA2b;

    public override bool UsesWorkspaceShell => true;

    public PdfToPdfAToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new PdfAToolOptions
        {
            InputFilePath = file,
            Standard = Standard
        }, progress, ct);
    }
}
