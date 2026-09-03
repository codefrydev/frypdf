using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Organize;

public partial class RotatePdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private int _rotationDegrees = 90;

    [ObservableProperty]
    private PageFilterTarget _targetFilter = PageFilterTarget.All;

    [ObservableProperty]
    private string _customRange = string.Empty;

    public override bool UsesWorkspaceShell => true;

    public RotatePdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new RotateToolOptions
        {
            InputFilePath = file,
            RotationDegrees = RotationDegrees,
            TargetFilter = TargetFilter,
            CustomRange = CustomRange
        }, progress, ct);
    }
}
