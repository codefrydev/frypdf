using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Security;

public partial class UnlockPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _password = string.Empty;

    public override bool UsesWorkspaceShell => true;

    public UnlockPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new UnlockToolOptions
        {
            InputFilePath = file,
            Password = Password
        }, progress, ct);
    }
}
