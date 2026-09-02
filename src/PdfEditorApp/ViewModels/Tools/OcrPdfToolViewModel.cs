using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Ocr;

namespace PdfEditorApp.ViewModels.Tools;

public partial class OcrPdfToolViewModel : PdfToolViewModelBase
{
    private readonly ICompositeOcrProvider _ocrProvider;
    private readonly Dictionary<int, string> _pageTextCache = new();
    private CancellationTokenSource? _pageExtractCts;

    [ObservableProperty]
    private string _language = "eng";

    [ObservableProperty]
    private bool _generateSearchablePdf = true;

    [ObservableProperty]
    private bool _generateTextFile = true;

    [ObservableProperty]
    private bool _extractTextOnly;

    [ObservableProperty]
    private OcrEngineType _selectedEngine = OcrEngineType.Auto;

    [ObservableProperty]
    private string _activeEngineDescription = string.Empty;

    [ObservableProperty]
    private bool _isDownloadingModel;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _downloadStatusText = string.Empty;

    [ObservableProperty]
    private bool _isSideBySideVisible = true;

    [ObservableProperty]
    private string _currentPageExtractedText = string.Empty;

    [ObservableProperty]
    private string _fullDocumentExtractedText = string.Empty;

    [ObservableProperty]
    private bool _showFullDocumentText;

    [ObservableProperty]
    private bool _isExtractingPageText;

    [ObservableProperty]
    private string _wordCountText = "0 words";

    [ObservableProperty]
    private string _charCountText = "0 chars";

    [ObservableProperty]
    private string _copyStatusMessage = string.Empty;

    [ObservableProperty]
    private string? _lastGeneratedTextFilePath;

    public bool HasGeneratedTextFile => !string.IsNullOrEmpty(LastGeneratedTextFilePath) && File.Exists(LastGeneratedTextFilePath);

    public string ViewModeButtonText => ShowFullDocumentText ? "Doc View" : "Page View";

    public string ActiveDisplayedText
    {
        get => ShowFullDocumentText ? FullDocumentExtractedText : CurrentPageExtractedText;
        set
        {
            if (ShowFullDocumentText)
            {
                FullDocumentExtractedText = value;
            }
            else
            {
                CurrentPageExtractedText = value;
                if (Preview.SelectedPage != null)
                {
                    _pageTextCache[Preview.SelectedPage.PageNumber] = value;
                }
            }
            UpdateStats();
        }
    }

    public ObservableCollection<TesseractLanguagePackageInfo> Languages { get; } = new();

    public override bool UsesWorkspaceShell => true;

    public OcrPdfToolViewModel(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool, ICompositeOcrProvider? ocrProvider = null)
        : base(operationsService, tool)
    {
        _ocrProvider = ocrProvider ?? CompositeOcrProvider.Default;
        UpdateActiveEngineDescription();
        LoadLanguages();

        Preview.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Preview.SelectedPage) && Preview.SelectedPage != null)
            {
                _ = ExtractTextForPageAsync(Preview.SelectedPage.PageNumber);
            }
        };
    }

    partial void OnSelectedEngineChanged(OcrEngineType value)
    {
        _ocrProvider.PreferredEngine = value;
        UpdateActiveEngineDescription();
    }

    partial void OnCurrentPageExtractedTextChanged(string value)
    {
        OnPropertyChanged(nameof(ActiveDisplayedText));
        UpdateStats();
    }

    partial void OnFullDocumentExtractedTextChanged(string value)
    {
        OnPropertyChanged(nameof(ActiveDisplayedText));
        UpdateStats();
    }

    partial void OnShowFullDocumentTextChanged(bool value)
    {
        OnPropertyChanged(nameof(ActiveDisplayedText));
        OnPropertyChanged(nameof(ViewModeButtonText));
        UpdateStats();
    }

    partial void OnLastGeneratedTextFilePathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasGeneratedTextFile));
    }

    private void UpdateActiveEngineDescription()
    {
        ActiveEngineDescription = _ocrProvider.ActiveEngine.EngineName;
    }

    private void LoadLanguages()
    {
        Languages.Clear();
        foreach (var lang in _ocrProvider.ModelService.AvailableLanguages)
        {
            Languages.Add(lang);
        }
    }

    [RelayCommand]
    public void ToggleViewMode()
    {
        ShowFullDocumentText = !ShowFullDocumentText;
    }

    [RelayCommand]
    public void ToggleSideBySide()
    {
        IsSideBySideVisible = !IsSideBySideVisible;
    }

    [RelayCommand]
    public async Task ExtractCurrentPageTextAsync()
    {
        if (Preview.SelectedPage != null)
        {
            _pageTextCache.Remove(Preview.SelectedPage.PageNumber);
            await ExtractTextForPageAsync(Preview.SelectedPage.PageNumber);
        }
    }

    private int _currentlyExtractingPage = -1;

    public async Task ExtractTextForPageAsync(int pageNumber)
    {
        if (SelectedFiles.Count == 0 || !File.Exists(SelectedFiles[0])) return;

        if (_pageTextCache.TryGetValue(pageNumber, out var cachedText))
        {
            CurrentPageExtractedText = cachedText;
            return;
        }

        if (_currentlyExtractingPage == pageNumber && IsExtractingPageText)
        {
            return;
        }

        _pageExtractCts?.Cancel();
        _pageExtractCts = new CancellationTokenSource();
        var ct = _pageExtractCts.Token;
        _currentlyExtractingPage = pageNumber;

        IsExtractingPageText = true;
        try
        {
            string filePath = SelectedFiles[0];
            string recognizedText = await Task.Run(() =>
            {
                using var pdfPigDoc = UglyToad.PdfPig.PdfDocument.Open(filePath);
                if (pageNumber < 1 || pageNumber > pdfPigDoc.NumberOfPages) return string.Empty;

                var page = pdfPigDoc.GetPage(pageNumber);
                var vectorWords = page.GetWords().ToList();
                if (vectorWords.Count > 0)
                {
                    return page.Text;
                }

                // Scanned page: render image and run OCR
                try { UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.AddSkiaPageFactory(pdfPigDoc); } catch { }
                using var pngStream = UglyToad.PdfPig.Rendering.Skia.PdfPigExtensions.GetPageAsPng(pdfPigDoc, pageNumber, 1.5f, 100);
                if (pngStream == null || pngStream.Length == 0) return string.Empty;

                var ocrRes = _ocrProvider.RecognizeTextAsync(pngStream.ToArray(), Language, ct).GetAwaiter().GetResult();
                return ocrRes.Success ? ocrRes.FullText : string.Empty;
            }, ct);

            if (!ct.IsCancellationRequested)
            {
                _pageTextCache[pageNumber] = recognizedText;
                CurrentPageExtractedText = recognizedText;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            CurrentPageExtractedText = $"[Error extracting page {pageNumber}: {ex.Message}]";
        }
        finally
        {
            if (_currentlyExtractingPage == pageNumber)
            {
                _currentlyExtractingPage = -1;
                IsExtractingPageText = false;
            }
        }
    }

    private void UpdateStats()
    {
        string text = ActiveDisplayedText;
        if (string.IsNullOrWhiteSpace(text))
        {
            WordCountText = "0 words";
            CharCountText = "0 chars";
            return;
        }

        int words = text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        WordCountText = $"{words:N0} words";
        CharCountText = $"{text.Length:N0} chars";
    }

    [RelayCommand]
    public async Task CopyCurrentTextAsync()
    {
        string text = ActiveDisplayedText;
        if (string.IsNullOrEmpty(text)) return;

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(text);
        }

        CopyStatusMessage = "Copied to clipboard!";
        await Task.Delay(2000);
        CopyStatusMessage = string.Empty;
    }

    [RelayCommand]
    public async Task ExportTextFileAsync()
    {
        string text = ActiveDisplayedText;
        if (string.IsNullOrWhiteSpace(text) && Preview.SelectedPage != null)
        {
            await ExtractTextForPageAsync(Preview.SelectedPage.PageNumber);
            text = ActiveDisplayedText;
        }

        if (string.IsNullOrWhiteSpace(text)) return;

        if (StorageProvider != null)
        {
            string defaultName = SelectedFiles.Count > 0
                ? $"{Path.GetFileNameWithoutExtension(SelectedFiles[0])}_extracted.txt"
                : "Extracted_Text.txt";

            var file = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Extracted Text File",
                SuggestedFileName = defaultName,
                DefaultExtension = "txt",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Text Document (*.txt)")
                    {
                        Patterns = new[] { "*.txt" }
                    }
                }
            });

            if (file != null)
            {
                await File.WriteAllTextAsync(file.Path.LocalPath, text, Encoding.UTF8);
                LastGeneratedTextFilePath = file.Path.LocalPath;
                CopyStatusMessage = $"Saved to {Path.GetFileName(file.Path.LocalPath)}!";
                await Task.Delay(2500);
                CopyStatusMessage = string.Empty;
            }
        }
    }

    [RelayCommand]
    public void OpenGeneratedTextFile()
    {
        if (!string.IsNullOrEmpty(LastGeneratedTextFilePath) && File.Exists(LastGeneratedTextFilePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = LastGeneratedTextFilePath,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    [RelayCommand]
    public async Task DownloadSelectedLanguageAsync()
    {
        if (IsDownloadingModel) return;

        IsDownloadingModel = true;
        DownloadProgress = 0.0;
        DownloadStatusText = $"Starting download for {Language}...";

        var progress = new Progress<double>(p => DownloadProgress = p * 100);

        try
        {
            bool ok = await _ocrProvider.ModelService.DownloadLanguageAsync(
                Language,
                progress,
                status => DownloadStatusText = status,
                CancellationToken.None);

            LoadLanguages();
            DownloadStatusText = ok ? "Language model ready!" : "Failed to download model.";
        }
        catch (Exception ex)
        {
            DownloadStatusText = $"Download error: {ex.Message}";
        }
        finally
        {
            IsDownloadingModel = false;
        }
    }

    protected override async Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
    {
        _ocrProvider.PreferredEngine = SelectedEngine;
        var result = await ExecuteBatchAsync(file => new OcrToolOptions
        {
            InputFilePath = file,
            Language = Language,
            GenerateSearchablePdf = GenerateSearchablePdf,
            GenerateTextFile = GenerateTextFile,
            ExtractTextOnly = ExtractTextOnly
        }, progress, ct);

        // Find the generated text file in result.OutputFiles if any
        if (result.OutputFiles != null)
        {
            var txt = result.OutputFiles.FirstOrDefault(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
            if (txt != null && File.Exists(txt))
            {
                LastGeneratedTextFilePath = txt;
                FullDocumentExtractedText = await File.ReadAllTextAsync(txt, ct);
                ShowFullDocumentText = true;
            }
        }

        OnPropertyChanged(nameof(HasSearchablePdfOutput));
        return result;
    }

    public bool HasSearchablePdfOutput => !string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath) && LastOutputFilePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    public async Task PreviewTextFileAsync()
    {
        if (!string.IsNullOrEmpty(LastGeneratedTextFilePath) && File.Exists(LastGeneratedTextFilePath))
        {
            await Preview.LoadDocumentAsync(LastGeneratedTextFilePath);
        }
        else if (!string.IsNullOrEmpty(FullDocumentExtractedText))
        {
            Preview.LoadTextContent(FullDocumentExtractedText, "Extracted_Text.txt");
        }
    }

    [RelayCommand]
    public async Task PreviewSearchablePdfAsync()
    {
        if (!string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath) && LastOutputFilePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            await Preview.LoadDocumentAsync(LastOutputFilePath);
        }
    }
}
