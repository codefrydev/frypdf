using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class CompressPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private PdfCompressionLevel _compressionLevel = PdfCompressionLevel.Balanced;

    [ObservableProperty]
    private int _imageQualityDpi = 150;

    [ObservableProperty]
    private bool _removeMetadata;

    [ObservableProperty]
    private bool _removeDuplicateObjects = true;

    [ObservableProperty]
    private bool _compressStreams = true;

    public CompressPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new CompressToolOptions
        {
            InputFilePath = PrimaryInputFile,
            Level = CompressionLevel,
            ImageQualityDpi = ImageQualityDpi,
            RemoveMetadata = RemoveMetadata,
            RemoveDuplicateObjects = RemoveDuplicateObjects,
            CompressStreams = CompressStreams
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.CompressPdf, options, progress, ct);
    }
}
