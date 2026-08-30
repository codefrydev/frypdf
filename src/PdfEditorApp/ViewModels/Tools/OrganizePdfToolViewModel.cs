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

    public OrganizePdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new OrganizeToolOptions
        {
            InputFilePath = PrimaryInputFile,
            PageOrder = PageOrder,
            PagesToDelete = PagesToDelete
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.OrganizePdf, options, progress, ct);
    }
}
