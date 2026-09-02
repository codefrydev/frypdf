using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class OrganizePdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private List<int> _pageOrder = new();

    [ObservableProperty]
    private List<int> _pagesToDelete = new();

    public override bool UsesWorkspaceShell => true;

    public OrganizePdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        // This standalone view has no reorder/delete/rotate controls yet, so PageOrder and
        // PagesToDelete can never be populated here — calling through would silently save an
        // unchanged copy while claiming success. Say so honestly instead of faking a result.
        if (PageOrder.Count == 0 && PagesToDelete.Count == 0)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = "This standalone tool doesn't have page reorder/delete/rotate controls yet, so there's nothing to apply. Use the FryPDF Pages Sidebar in the Editor for visual drag-and-drop reordering, rotation, and deletion."
            };
        }

        var options = new OrganizeToolOptions
        {
            InputFilePath = PrimaryInputFile,
            PageOrder = PageOrder,
            PagesToDelete = PagesToDelete
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.OrganizePdf, options, progress, ct);
    }
}
