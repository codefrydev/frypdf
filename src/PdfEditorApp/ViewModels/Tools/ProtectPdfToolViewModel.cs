using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools;

public partial class ProtectPdfToolViewModel : PdfToolViewModelBase
{
    [ObservableProperty]
    private string _userPassword = string.Empty;

    [ObservableProperty]
    private string _ownerPassword = string.Empty;

    [ObservableProperty]
    private bool _allowPrinting = true;

    [ObservableProperty]
    private bool _allowModifying;

    [ObservableProperty]
    private bool _allowCopying;

    [ObservableProperty]
    private bool _allowAnnotating = true;

    [ObservableProperty]
    private bool _allowFormFilling = true;

    public ProtectPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
    }

    protected override bool ValidateInputs(out string errorMessage)
    {
        if (!base.ValidateInputs(out errorMessage)) return false;

        if (string.IsNullOrEmpty(UserPassword) && string.IsNullOrEmpty(OwnerPassword))
        {
            errorMessage = "Please enter a user or owner password to encrypt the document.";
            return false;
        }
        return true;
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        var options = new SecurityToolOptions
        {
            InputFilePath = PrimaryInputFile,
            UserPassword = UserPassword,
            OwnerPassword = OwnerPassword,
            AllowPrinting = AllowPrinting,
            AllowModifying = AllowModifying,
            AllowCopying = AllowCopying,
            AllowAnnotating = AllowAnnotating,
            AllowFormFilling = AllowFormFilling
        };

        return await OperationsService.ExecuteToolAsync(PdfToolId.ProtectPdf, options, progress, ct);
    }
}
