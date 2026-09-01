using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private int _jpegQuality = 66;

    [ObservableProperty]
    private bool _convertToGrayscale;

    [ObservableProperty]
    private bool _isCustomMode;

    [ObservableProperty]
    private bool _removeMetadata;

    [ObservableProperty]
    private bool _removeDuplicateObjects = true;

    [ObservableProperty]
    private bool _compressStreams = true;

    [ObservableProperty]
    private string _qualityFeedbackMessage = "✅ Balanced clarity & high compression (~70% reduction). Recommended for general use.";

    [ObservableProperty]
    private string _qualityFeedbackSeverity = "Success"; // "Success", "Warning", "Info"

    public bool IsExtremeCompression => !IsCustomMode && CompressionLevel == PdfCompressionLevel.MaximumCompression;
    public bool IsRecommendedCompression => !IsCustomMode && (CompressionLevel == PdfCompressionLevel.Balanced || CompressionLevel == PdfCompressionLevel.SmallSize);
    public bool IsLessCompression => !IsCustomMode && (CompressionLevel == PdfCompressionLevel.HighQuality || CompressionLevel == PdfCompressionLevel.MaximumQuality);

    public CompressPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
        : base(operationsService, tool)
    {
        UpdateQualityFeedback();
    }

    [RelayCommand]
    public void SelectPreset(string preset)
    {
        IsCustomMode = false;
        switch (preset.ToLowerInvariant())
        {
            case "extreme":
            case "maximum":
                CompressionLevel = PdfCompressionLevel.MaximumCompression;
                ImageQualityDpi = 96;
                JpegQuality = 52;
                break;

            case "recommended":
            case "balanced":
                CompressionLevel = PdfCompressionLevel.Balanced;
                ImageQualityDpi = 125;
                JpegQuality = 66;
                break;

            case "less":
            case "high":
                CompressionLevel = PdfCompressionLevel.HighQuality;
                ImageQualityDpi = 180;
                JpegQuality = 82;
                break;

            case "custom":
                IsCustomMode = true;
                break;
        }

        RefreshPresetStates();
        UpdateQualityFeedback();
    }

    [RelayCommand]
    public void ToggleCustomMode()
    {
        IsCustomMode = !IsCustomMode;
        RefreshPresetStates();
        UpdateQualityFeedback();
    }

    partial void OnImageQualityDpiChanged(int value)
    {
        // Safe input clamp
        int clamped = Math.Clamp(value, 50, 600);
        if (clamped != value)
        {
            ImageQualityDpi = clamped;
            return;
        }
        UpdateQualityFeedback();
    }

    partial void OnJpegQualityChanged(int value)
    {
        // Safe input clamp
        int clamped = Math.Clamp(value, 20, 100);
        if (clamped != value)
        {
            JpegQuality = clamped;
            return;
        }
        UpdateQualityFeedback();
    }

    partial void OnConvertToGrayscaleChanged(bool value)
    {
        UpdateQualityFeedback();
    }

    partial void OnCompressionLevelChanged(PdfCompressionLevel value)
    {
        RefreshPresetStates();
        UpdateQualityFeedback();
    }

    partial void OnIsCustomModeChanged(bool value)
    {
        RefreshPresetStates();
        UpdateQualityFeedback();
    }

    private void RefreshPresetStates()
    {
        OnPropertyChanged(nameof(IsExtremeCompression));
        OnPropertyChanged(nameof(IsRecommendedCompression));
        OnPropertyChanged(nameof(IsLessCompression));
    }

    private void UpdateQualityFeedback()
    {
        if (ImageQualityDpi < 72)
        {
            QualityFeedbackSeverity = "Warning";
            QualityFeedbackMessage = "⚠️ Low DPI (<72 DPI): Scanned text or fine lines may become blurry and difficult to read.";
        }
        else if (JpegQuality < 45)
        {
            QualityFeedbackSeverity = "Warning";
            QualityFeedbackMessage = "⚠️ Low JPEG Quality (<45%): Visible compression artifacts, grain, and blockiness will appear around text.";
        }
        else if (ImageQualityDpi >= 300 && JpegQuality >= 90)
        {
            QualityFeedbackSeverity = "Info";
            QualityFeedbackMessage = "ℹ️ Ultra-high print resolution selected: Original fidelity is pristine, but file size reduction will be minimal (<25%).";
        }
        else if (ConvertToGrayscale)
        {
            QualityFeedbackSeverity = "Success";
            QualityFeedbackMessage = "✨ Grayscale Mode: Color channels removed. Saves an extra ~20-30% on scanned forms, receipts, and invoices.";
        }
        else if (IsExtremeCompression || (ImageQualityDpi <= 100 && JpegQuality <= 55))
        {
            QualityFeedbackSeverity = "Success";
            QualityFeedbackMessage = "⚡ Maximum Compression (~75–85% reduction): Highly compact output, ideal for email and strict upload limits.";
        }
        else if (IsLessCompression || (ImageQualityDpi >= 180 && JpegQuality >= 80))
        {
            QualityFeedbackSeverity = "Info";
            QualityFeedbackMessage = "💎 High Fidelity Compression (~35–50% reduction): High clarity with moderate file size savings.";
        }
        else
        {
            QualityFeedbackSeverity = "Success";
            QualityFeedbackMessage = "✅ Balanced Compression (~65–75% reduction): Great visual quality and substantial file size savings.";
        }
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        return await ExecuteBatchAsync(file => new CompressToolOptions
        {
            InputFilePath = file,
            Level = CompressionLevel,
            ImageQualityDpi = Math.Clamp(ImageQualityDpi, 50, 600),
            JpegQuality = Math.Clamp(JpegQuality, 20, 100),
            ConvertToGrayscale = ConvertToGrayscale,
            RemoveMetadata = RemoveMetadata,
            RemoveDuplicateObjects = RemoveDuplicateObjects,
            CompressStreams = CompressStreams
        }, progress, ct);
    }
}
