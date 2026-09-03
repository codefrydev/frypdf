using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.Tools.Security;

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

    [ObservableProperty]
    private bool _allowAssembly = true;

    [ObservableProperty]
    private bool _encryptMetadata = true;

    [ObservableProperty]
    private PdfProcessingEngine _processingEngine = PdfProcessingEngine.StandardPdfSharp;

    [ObservableProperty]
    private PdfEncryptionLevel _encryptionLevel = PdfEncryptionLevel.Aes256Bit;

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

    public bool IsAes256
    {
        get => EncryptionLevel == PdfEncryptionLevel.Aes256Bit;
        set
        {
            if (value) EncryptionLevel = PdfEncryptionLevel.Aes256Bit;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAes128));
        }
    }

    public bool IsAes128
    {
        get => EncryptionLevel == PdfEncryptionLevel.Aes128Bit;
        set
        {
            if (value) EncryptionLevel = PdfEncryptionLevel.Aes128Bit;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAes256));
        }
    }

    public override bool UsesWorkspaceShell => true;

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
        return await ExecuteBatchAsync(file => new SecurityToolOptions
        {
            InputFilePath = file,
            UserPassword = UserPassword,
            OwnerPassword = OwnerPassword,
            AllowPrinting = AllowPrinting,
            AllowModifying = AllowModifying,
            AllowCopying = AllowCopying,
            AllowAnnotating = AllowAnnotating,
            AllowFormFilling = AllowFormFilling,
            AllowAssembly = AllowAssembly,
            EncryptMetadata = EncryptMetadata,
            Engine = ProcessingEngine,
            EncryptionLevel = EncryptionLevel
        }, progress, ct);
    }
}
