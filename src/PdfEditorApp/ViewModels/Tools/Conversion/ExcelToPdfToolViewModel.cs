using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Conversion;

public partial class ExcelToPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private PageOrientation _orientation = PageOrientation.Landscape;

    public override bool UsesWorkspaceShell => true;

    public ExcelToPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
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
