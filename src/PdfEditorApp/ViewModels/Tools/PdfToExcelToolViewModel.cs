using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class PdfToExcelToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private bool _detectAllTables = true;

    [ObservableProperty]
    private bool _separateSheetsPerPage;

    public PdfToExcelToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new ExcelConversionOptions
        {
            InputFilePath = file,
            DetectAllTables = DetectAllTables,
            SeparateSheetsPerPage = SeparateSheetsPerPage
        }, progress, ct);
    }
}
