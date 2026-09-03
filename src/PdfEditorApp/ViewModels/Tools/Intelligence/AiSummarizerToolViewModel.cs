using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Intelligence;

public partial class AiSummarizerToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private int _maxBulletPoints = 7;

    [ObservableProperty]
    private bool _includeExecutiveSummary = true;

    [ObservableProperty]
    private bool _includeActionItems = true;

    [ObservableProperty]
    private string _targetLanguage = "English";

    [ObservableProperty]
    private string _customPrompt = string.Empty;

    public override bool UsesWorkspaceShell => true;

    public AiSummarizerToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new AiSummaryOptions
        {
            InputFilePath = PrimaryInputFile,
            MaxBulletPoints = MaxBulletPoints,
            IncludeExecutiveSummary = IncludeExecutiveSummary,
            IncludeActionItems = IncludeActionItems,
            TargetLanguage = TargetLanguage,
            CustomPrompt = string.IsNullOrWhiteSpace(CustomPrompt) ? null : CustomPrompt
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.AiSummarizer, options, progress, ct);
    }
}
