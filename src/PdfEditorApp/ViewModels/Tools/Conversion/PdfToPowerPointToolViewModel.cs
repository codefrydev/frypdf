using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Conversion;

public partial class PdfToPowerPointToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private bool _editableText = true;

    public override bool UsesWorkspaceShell => true;

    public PdfToPowerPointToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new PptxConversionOptions
        {
            InputFilePath = file,
            EditableText = EditableText
        }, progress, ct);
    }
}
