using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.ViewModels.Tools.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Messages;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels;

public partial class PdfToolRunnerViewModel : ViewModelBase
{
    private readonly IPdfDocumentOperationsService _operationsService;
    private CancellationTokenSource? _cts;

    public IStorageProvider? StorageProvider { get; set; }

    [ObservableProperty]
    private PdfToolDefinition _tool = new();

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _lastOutputFilePath = string.Empty;

    [ObservableProperty]
    private string _resultSummaryMessage = string.Empty;

    public ObservableCollection<string> SelectedFiles { get; } = new();

    public bool HasSelectedFiles => SelectedFiles.Count > 0;
    public string SelectedFilesCountText => SelectedFiles.Count == 1 ? "1 file selected" : $"{SelectedFiles.Count} files selected";

    // --- TOOL-SPECIFIC OBSERVABLE OPTIONS ---

    // Split
    [ObservableProperty] private SplitExtractMode _splitMode = SplitExtractMode.SplitEveryNPages;
    [ObservableProperty] private int _splitPagesInterval = 1;
    [ObservableProperty] private string _splitRangeExpression = "1-3, 5, 7-10";
    [ObservableProperty] private bool _splitOddEven;

    // Compress
    [ObservableProperty] private PdfCompressionLevel _compressionLevel = PdfCompressionLevel.Balanced;
    [ObservableProperty] private bool _compressRemoveMetadata;

    // Watermark
    [ObservableProperty] private string _watermarkText = "CONFIDENTIAL";
    [ObservableProperty] private double _watermarkOpacity = 0.35;
    [ObservableProperty] private double _watermarkRotation = -45;
    [ObservableProperty] private WatermarkPosition _watermarkPosition = WatermarkPosition.Center;
    [ObservableProperty] private string _watermarkColorHex = "#EF4444";
    [ObservableProperty] private double _watermarkFontSize = 48;

    // Rotate
    [ObservableProperty] private int _rotateDegrees = 90;
    [ObservableProperty] private PageFilterTarget _rotateFilter = PageFilterTarget.All;

    // Security & Unlock
    [ObservableProperty] private string _userPassword = string.Empty;
    [ObservableProperty] private string _ownerPassword = string.Empty;
    [ObservableProperty] private bool _allowPrinting = true;
    [ObservableProperty] private bool _allowCopying = false;
    [ObservableProperty] private bool _allowModifying = false;

    // Page Numbers
    [ObservableProperty] private PageNumberPosition _pageNumberPosition = PageNumberPosition.BottomCenter;
    [ObservableProperty] private string _pageNumberTemplate = "Page {n} of {total}";
    [ObservableProperty] private int _pageNumberStart = 1;
    [ObservableProperty] private string _pageNumberColorHex = "#334155";

    // OCR
    [ObservableProperty] private string _ocrLanguage = "eng";
    [ObservableProperty] private bool _ocrSearchablePdf = true;

    // AI Summary
    [ObservableProperty] private int _aiMaxBullets = 7;
    [ObservableProperty] private bool _aiIncludeExecutive = true;
    [ObservableProperty] private bool _aiIncludeActions = true;

    // Translation
    [ObservableProperty] private string _targetLanguage = "Spanish";

    // HTML / Web
    [ObservableProperty] private string _htmlInputTextOrUrl = "https://";

    // Image to PDF / PDF to Image
    [ObservableProperty] private string _imageExportFormat = "jpg";
    [ObservableProperty] private int _imageDpi = 300;
    [ObservableProperty] private bool _imageFitToPage = true;

    // Cropping
    [ObservableProperty] private double _cropMarginTop = 36;
    [ObservableProperty] private double _cropMarginBottom = 36;
    [ObservableProperty] private double _cropMarginLeft = 36;
    [ObservableProperty] private double _cropMarginRight = 36;

    // Scan
    [ObservableProperty] private bool _scanAutoDeskew = true;
    [ObservableProperty] private bool _scanEnhanceContrast = true;
    [ObservableProperty] private bool _scanWhitenBackground = true;

    // Redaction
    [ObservableProperty] private string _redactSearchPattern = "CONFIDENTIAL";
    [ObservableProperty] private bool _redactCaseSensitive = false;
    [ObservableProperty] private bool _redactPermanentScrub = true;

    // Forms
    [ObservableProperty] private bool _formFlattenFields = false;
    [ObservableProperty] private bool _formExportJson = false;

    // Markdown
    [ObservableProperty] private bool _markdownIncludeTables = true;
    [ObservableProperty] private bool _markdownExtractImages = false;

    // Compare
    [ObservableProperty] private string _compareDocBPath = string.Empty;

    // Starred state for active tool
    [ObservableProperty] private bool _isToolStarred;

    // Navigation events
    public event Action? BackRequested;

    public PdfToolRunnerViewModel(IPdfDocumentOperationsService operationsService)
    {
        _operationsService = operationsService;
    }

    [RelayCommand]
    public void GoBack()
    {
        BackRequested?.Invoke();
    }

    [RelayCommand]
    public void OpenInEditor()
    {
        if (!string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath))
        {
            WeakReferenceMessenger.Default.Send(new OpenInEditorMessage(LastOutputFilePath));
        }
    }

    [RelayCommand]
    public void OpenInViewer()
    {
        string targetPath = !string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath)
            ? LastOutputFilePath
            : (SelectedFiles.Count > 0 && File.Exists(SelectedFiles[0]) ? SelectedFiles[0] : string.Empty);

        if (!string.IsNullOrEmpty(targetPath))
        {
            WeakReferenceMessenger.Default.Send(new OpenInViewerMessage(targetPath));
        }
    }

    public void SetupForTool(PdfToolDefinition tool, string? initialFilePath = null)
    {
        Tool = tool;
        SelectedFiles.Clear();
        if (!string.IsNullOrEmpty(initialFilePath) && File.Exists(initialFilePath))
        {
            SelectedFiles.Add(initialFilePath);
        }

        ResetState();
        IsOpen = true;
    }

    public void ResetState()
    {
        IsRunning = false;
        IsComplete = false;
        HasError = false;
        ErrorMessage = string.Empty;
        StatusMessage = "Ready";
        ProgressPercentage = 0;
        LastOutputFilePath = string.Empty;
        ResultSummaryMessage = string.Empty;
        OnPropertyChanged(nameof(HasSelectedFiles));
        OnPropertyChanged(nameof(SelectedFilesCountText));
    }

    [RelayCommand]
    public async Task AddFilesAsync()
    {
        if (StorageProvider == null) return;

        var patterns = Tool.AcceptedFileExtensions.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(e => e.Trim().StartsWith("*") ? e.Trim() : "*" + e.Trim())
                                                  .ToArray();

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"Select Files for {Tool.Name}",
            AllowMultiple = Tool.SupportsMultiFile,
            FileTypeFilter = new[]
            {
                new FilePickerFileType($"{Tool.Name} Inputs")
                {
                    Patterns = patterns.Length > 0 ? patterns : new[] { "*.pdf" }
                }
            }
        });

        if (files != null && files.Count > 0)
        {
            if (!Tool.SupportsMultiFile) SelectedFiles.Clear();
            foreach (var f in files)
            {
                string path = f.Path.LocalPath;
                if (!SelectedFiles.Contains(path)) SelectedFiles.Add(path);
            }
            ResetState();
        }
    }

    [RelayCommand]
    public void RemoveFile(string filePath)
    {
        SelectedFiles.Remove(filePath);
        ResetState();
    }

    [RelayCommand]
    public void ClearFiles()
    {
        SelectedFiles.Clear();
        ResetState();
    }

    [RelayCommand]
    public void Close()
    {
        CancelExecution();
        IsOpen = false;
    }

    [RelayCommand]
    public void CancelExecution()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            StatusMessage = "Cancelling...";
        }
    }

    [RelayCommand]
    public async Task ExecuteToolAsync()
    {
        if (SelectedFiles.Count == 0 && Tool.Id != PdfToolId.HtmlToPdf)
        {
            HasError = true;
            ErrorMessage = "Please select at least one file to process.";
            return;
        }

        IsRunning = true;
        IsComplete = false;
        HasError = false;
        ErrorMessage = string.Empty;
        ProgressPercentage = 5.0;
        StatusMessage = $"Processing {Tool.Name}...";

        _cts = new CancellationTokenSource();
        var progress = new Progress<double>(p =>
        {
            ProgressPercentage = p;
            StatusMessage = $"Processing ({p:F0}%)...";
        });

        try
        {
            object options = BuildOptionsObject();
            var result = await _operationsService.ExecuteToolAsync(Tool.Id, options, progress, _cts.Token);

            if (result.Success)
            {
                IsComplete = true;
                LastOutputFilePath = result.OutputFilePath ?? "";
                ResultSummaryMessage = result.Message ?? "Operation completed successfully.";
                StatusMessage = "Completed successfully!";
                ProgressPercentage = 100.0;
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Operation failed.";
                StatusMessage = "Failed";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unexpected error: {ex.Message}";
            StatusMessage = "Error";
        }
        finally
        {
            IsRunning = false;
            _cts = null;
        }
    }

    [RelayCommand]
    public void OpenOutputFile()
    {
        if (!string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = LastOutputFilePath,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    [RelayCommand]
    public void OpenOutputFolder()
    {
        if (!string.IsNullOrEmpty(LastOutputFilePath))
        {
            string? dir = Directory.Exists(LastOutputFilePath) ? LastOutputFilePath : Path.GetDirectoryName(LastOutputFilePath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = dir,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }
    }

    private object BuildOptionsObject()
    {
        string firstFile = SelectedFiles.FirstOrDefault() ?? "";

        switch (Tool.Id)
        {
            case PdfToolId.MergePdf:
                return new MergeToolOptions { InputFiles = SelectedFiles.ToList() };

            case PdfToolId.SplitPdf:
                return new SplitToolOptions
                {
                    InputFilePath = firstFile,
                    Mode = SplitMode,
                    PagesPerSplit = SplitPagesInterval,
                    RangeExpression = SplitRangeExpression,
                    SplitOddEven = SplitOddEven
                };

            case PdfToolId.CompressPdf:
                return new CompressToolOptions
                {
                    InputFilePath = firstFile,
                    Level = CompressionLevel,
                    RemoveMetadata = CompressRemoveMetadata
                };

            case PdfToolId.Watermark:
                return new WatermarkToolOptions
                {
                    InputFilePath = firstFile,
                    Text = WatermarkText,
                    Opacity = WatermarkOpacity,
                    RotationAngle = WatermarkRotation,
                    Position = WatermarkPosition,
                    ColorHex = WatermarkColorHex,
                    FontSize = WatermarkFontSize
                };

            case PdfToolId.RotatePdf:
                return new RotateToolOptions
                {
                    InputFilePath = firstFile,
                    RotationDegrees = RotateDegrees,
                    TargetFilter = RotateFilter
                };

            case PdfToolId.ProtectPdf:
                return new SecurityToolOptions
                {
                    InputFilePath = firstFile,
                    UserPassword = UserPassword,
                    OwnerPassword = OwnerPassword,
                    AllowPrinting = AllowPrinting,
                    AllowCopying = AllowCopying,
                    AllowModifying = AllowModifying
                };

            case PdfToolId.UnlockPdf:
                return new UnlockToolOptions
                {
                    InputFilePath = firstFile,
                    Password = UserPassword
                };

            case PdfToolId.PageNumbers:
                return new PageNumberToolOptions
                {
                    InputFilePath = firstFile,
                    Template = PageNumberTemplate,
                    Position = PageNumberPosition,
                    StartingNumber = PageNumberStart,
                    ColorHex = PageNumberColorHex
                };

            case PdfToolId.PdfToWord:
                return new WordConversionOptions { InputFilePath = firstFile };

            case PdfToolId.PdfToExcel:
                return new ExcelConversionOptions { InputFilePath = firstFile };

            case PdfToolId.PdfToPowerPoint:
                return new PptxConversionOptions { InputFilePath = firstFile };

            case PdfToolId.WordToPdf:
            case PdfToolId.ExcelToPdf:
            case PdfToolId.PowerPointToPdf:
                return new OfficeToPdfOptions { InputFilePath = firstFile };

            case PdfToolId.PdfToJpg:
                return new ImageConversionOptions
                {
                    InputFilePath = firstFile,
                    OutputFormat = ImageExportFormat,
                    Dpi = ImageDpi
                };

            case PdfToolId.JpgToPdf:
                return new ImagesToPdfOptions
                {
                    ImageFiles = SelectedFiles.ToList(),
                    FitToPage = ImageFitToPage
                };

            case PdfToolId.HtmlToPdf:
                return new HtmlToPdfOptions
                {
                    HtmlContentOrUrl = HtmlInputTextOrUrl,
                    IsUrl = HtmlInputTextOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                };

            case PdfToolId.PdfToPdfA:
                return new PdfAToolOptions { InputFilePath = firstFile };

            case PdfToolId.RepairPdf:
                return new RepairToolOptions { InputFilePath = firstFile };

            case PdfToolId.OcrPdf:
                return new OcrToolOptions { InputFilePath = firstFile, Language = OcrLanguage };

            case PdfToolId.ScanToPdf:
                return new ScanToolOptions
                {
                    InputImageFiles = SelectedFiles.ToList(),
                    AutoDeskew = ScanAutoDeskew,
                    EnhanceContrast = ScanEnhanceContrast,
                    WhitenBackground = ScanWhitenBackground
                };

            case PdfToolId.AiSummarizer:
                return new AiSummaryOptions
                {
                    InputFilePath = firstFile,
                    MaxBulletPoints = AiMaxBullets,
                    IncludeExecutiveSummary = AiIncludeExecutive,
                    IncludeActionItems = AiIncludeActions
                };

            case PdfToolId.TranslatePdf:
                return new TranslationOptions
                {
                    InputFilePath = firstFile,
                    TargetLanguage = TargetLanguage
                };

            case PdfToolId.PdfToMarkdown:
                return new MarkdownConversionOptions
                {
                    InputFilePath = firstFile,
                    IncludeTables = MarkdownIncludeTables,
                    IncludeImages = MarkdownExtractImages
                };

            case PdfToolId.SignPdf:
                return new SignToolOptions
                {
                    InputFilePath = firstFile,
                    SignerName = "Authorized Signatory",
                    Style = SignatureStyle.CursiveElegance
                };

            case PdfToolId.RedactPdf:
                return new RedactionToolOptions
                {
                    InputFilePath = firstFile,
                    SearchPatternToRedact = RedactSearchPattern,
                    CaseSensitive = RedactCaseSensitive,
                    PermanentScrubText = RedactPermanentScrub
                };

            case PdfToolId.CropPdf:
                return new CropToolOptions
                {
                    InputFilePath = firstFile,
                    CropTopPoints = CropMarginTop,
                    CropBottomPoints = CropMarginBottom,
                    CropLeftPoints = CropMarginLeft,
                    CropRightPoints = CropMarginRight
                };

            case PdfToolId.PdfForms:
                return new FormToolOptions
                {
                    InputFilePath = firstFile,
                    FlattenFields = FormFlattenFields,
                    ExportFieldValuesJson = FormExportJson
                };

            case PdfToolId.ComparePdf:
                return new CompareToolOptions
                {
                    DocumentAPath = firstFile,
                    DocumentBPath = !string.IsNullOrEmpty(CompareDocBPath) ? CompareDocBPath : (SelectedFiles.Count > 1 ? SelectedFiles[1] : firstFile)
                };

            default:
                return new { InputFilePath = firstFile };
        }
    }
}
