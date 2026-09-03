using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Conversion;

public partial class WordToPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private PageOrientation _orientation = PageOrientation.Portrait;

    public override bool UsesWorkspaceShell => true;

    public WordToPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new OfficeToPdfOptions
        {
            InputFilePath = file,
            Orientation = Orientation
        }, progress, ct);
    }
}
