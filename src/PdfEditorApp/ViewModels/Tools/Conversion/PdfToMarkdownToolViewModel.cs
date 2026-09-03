using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Conversion;

public partial class PdfToMarkdownToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private bool _includeTables = true;

    [ObservableProperty]
    private bool _includeImages;

    [ObservableProperty]
    private bool _includeMetadataHeader = true;

    public override bool UsesWorkspaceShell => true;

    public PdfToMarkdownToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new MarkdownConversionOptions
        {
            InputFilePath = file,
            IncludeTables = IncludeTables,
            IncludeImages = IncludeImages,
            IncludeMetadataHeader = IncludeMetadataHeader
        }, progress, ct);
    }
}
