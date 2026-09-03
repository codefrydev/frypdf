using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Organize;

public partial class MergePdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private bool _normalizePageSizes;

    [ObservableProperty]
    private bool _preserveBookmarks = true;

    [ObservableProperty]
    private PdfProcessingEngine _processingEngine = PdfProcessingEngine.StandardPdfSharp;

    public bool IsQuestPdfEngine
    {
        get => ProcessingEngine == PdfProcessingEngine.QuestPdfNative;
        set
        {
            if (value) ProcessingEngine = PdfProcessingEngine.QuestPdfNative;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsStandardEngine));
        }
    }

    public bool IsStandardEngine
    {
        get => ProcessingEngine == PdfProcessingEngine.StandardPdfSharp;
        set
        {
            if (value) ProcessingEngine = PdfProcessingEngine.StandardPdfSharp;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsQuestPdfEngine));
        }
    }

    public override bool UsesWorkspaceShell => true;

    public MergePdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override bool ValidateInputs(out string errorMessage)
    {
        if (SelectedFiles.Count < 2)
        {
            errorMessage = "Please select at least 2 PDF documents to merge.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new MergeToolOptions
        {
            InputFiles = SelectedFiles.ToList(),
            NormalizePageSizes = NormalizePageSizes,
            PreserveBookmarks = PreserveBookmarks,
            Engine = ProcessingEngine
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.MergePdf, options, progress, ct);
    }
}
