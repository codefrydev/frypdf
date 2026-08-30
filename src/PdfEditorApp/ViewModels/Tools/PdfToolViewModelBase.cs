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
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools;
using UglyToad.PdfPig;

namespace PdfEditorApp.ViewModels.Tools;

/// <summary>
/// Abstract base class for all individual PDF tool ViewModels.
/// Provides unified lifecycle, file management, visual previews, asynchronous execution, cancellation, and navigation.
/// </summary>
public abstract partial class PdfToolViewModelBase : ViewModelBase
{
    protected readonly IPdfDocumentOperationsService OperationsService;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Remembers the user's last chosen save/export directory across tool executions.
    /// </summary>
    public static string? RememberedSaveDirectory { get; set; }

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

    [ObservableProperty]
    private bool _isToolStarred;

    [ObservableProperty]
    private PdfFilePreviewItem? _outputFilePreview;

    [ObservableProperty]
    private PdfPagePreviewThumbnail? _selectedOutputPage;

    [ObservableProperty]
    private string _savedNotificationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasSavedNotification;

    public ObservableCollection<string> SelectedFiles { get; } = new();

    public ObservableCollection<PdfFilePreviewItem> SelectedFilePreviewItems { get; } = new();

    public ObservableCollection<PdfPagePreviewThumbnail> OutputPageThumbnails { get; } = new();

    public bool HasSelectedFiles => SelectedFiles.Count > 0;
    public string SelectedFilesCountText => SelectedFiles.Count == 1 ? "1 file selected" : $"{SelectedFiles.Count} files selected";
    public string PrimaryInputFile => SelectedFiles.FirstOrDefault() ?? string.Empty;

    public int TotalSelectedPages => SelectedFilePreviewItems.Sum(p => p.PageCount);

    public string TotalSummaryText
    {
        get
        {
            if (SelectedFilePreviewItems.Count == 0) return "No files selected";
            long totalBytes = SelectedFilePreviewItems.Sum(p => p.FileSizeBytes);
            int pages = TotalSelectedPages;
            if (SelectedFilePreviewItems.Count == 1)
                return $"1 Document · {pages} Page{(pages != 1 ? "s" : "")} · {PdfFilePreviewItem.FormatBytes(totalBytes)}";
            return $"{SelectedFilePreviewItems.Count} Documents · {pages} Total Pages · {PdfFilePreviewItem.FormatBytes(totalBytes)}";
        }
    }

    // Events
    public event Action? BackRequested;
    public event Action<string>? OpenInEditorRequested;
    public event Action<string>? OpenInViewerRequested;

    protected PdfToolViewModelBase(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
    {
        OperationsService = operationsService;
        Tool = tool;
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
            OpenInEditorRequested?.Invoke(LastOutputFilePath);
        }
        else if (HasSelectedFiles && File.Exists(PrimaryInputFile))
        {
            OpenInEditorRequested?.Invoke(PrimaryInputFile);
        }
    }

    [RelayCommand]
    public void OpenInViewer()
    {
        string targetPath = !string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath)
            ? LastOutputFilePath
            : (HasSelectedFiles && File.Exists(PrimaryInputFile) ? PrimaryInputFile : string.Empty);

        if (!string.IsNullOrEmpty(targetPath))
        {
            if (OpenInViewerRequested != null)
            {
                OpenInViewerRequested.Invoke(targetPath);
            }
            else
            {
                OpenOutputFile();
            }
        }
    }

    public virtual void SetupInitialFiles(IEnumerable<string>? filePaths)
    {
        SelectedFiles.Clear();
        if (filePaths != null)
        {
            foreach (var file in filePaths)
            {
                if (!string.IsNullOrEmpty(file) && File.Exists(file))
                {
                    SelectedFiles.Add(file);
                }
            }
        }
        SyncPreviewItems();
        ResetState();
        IsOpen = true;
    }

    public virtual void ResetState()
    {
        IsRunning = false;
        IsComplete = false;
        HasError = false;
        ErrorMessage = string.Empty;
        StatusMessage = "Ready";
        ProgressPercentage = 0;
        LastOutputFilePath = string.Empty;
        ResultSummaryMessage = string.Empty;
        OutputFilePreview = null;
        OutputPageThumbnails.Clear();
        SelectedOutputPage = null;
        SavedNotificationMessage = string.Empty;
        HasSavedNotification = false;
        OnPropertyChanged(nameof(HasSelectedFiles));
        OnPropertyChanged(nameof(SelectedFilesCountText));
        OnPropertyChanged(nameof(PrimaryInputFile));
        OnPropertyChanged(nameof(TotalSelectedPages));
        OnPropertyChanged(nameof(TotalSummaryText));
    }

    public void SyncPreviewItems()
    {
        SelectedFilePreviewItems.Clear();
        for (int i = 0; i < SelectedFiles.Count; i++)
        {
            string path = SelectedFiles[i];
            int pageCount = 1;
            if (File.Exists(path))
            {
                pageCount = PdfFileHelper.InspectPageCountSafely(path);
            }
            var item = PdfFilePreviewItem.CreateFromFile(path, i + 1, pageCount);
            SelectedFilePreviewItems.Add(item);
        }

        OnPropertyChanged(nameof(HasSelectedFiles));
        OnPropertyChanged(nameof(SelectedFilesCountText));
        OnPropertyChanged(nameof(PrimaryInputFile));
        OnPropertyChanged(nameof(TotalSelectedPages));
        OnPropertyChanged(nameof(TotalSummaryText));
    }

    [RelayCommand]
    public async Task AddFilesAsync()
    {
        if (StorageProvider == null) return;

        var patterns = Tool.AcceptedFileExtensions
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim().StartsWith("*") ? e.Trim() : "*" + e.Trim())
            .ToArray();

        IStorageFolder? startFolder = null;
        if (!string.IsNullOrEmpty(RememberedSaveDirectory) && Directory.Exists(RememberedSaveDirectory))
        {
            try
            {
                startFolder = await StorageProvider.TryGetFolderFromPathAsync(RememberedSaveDirectory);
            }
            catch { }
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"Select Files for {Tool.Name}",
            AllowMultiple = Tool.SupportsMultiFile,
            SuggestedStartLocation = startFolder,
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
                if (!SelectedFiles.Contains(path))
                {
                    SelectedFiles.Add(path);
                }
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) RememberedSaveDirectory = dir;
            }
            SyncPreviewItems();
            ResetState();
        }
    }

    [RelayCommand]
    public void MoveFileUp(object? parameter)
    {
        string? filePath = parameter switch
        {
            string s => s,
            PdfFilePreviewItem item => item.FilePath,
            _ => null
        };

        if (string.IsNullOrEmpty(filePath)) return;

        int index = SelectedFiles.IndexOf(filePath);
        if (index > 0)
        {
            SelectedFiles.Move(index, index - 1);
            SyncPreviewItems();
            ResetState();
        }
    }

    [RelayCommand]
    public void MoveFileDown(object? parameter)
    {
        string? filePath = parameter switch
        {
            string s => s,
            PdfFilePreviewItem item => item.FilePath,
            _ => null
        };

        if (string.IsNullOrEmpty(filePath)) return;

        int index = SelectedFiles.IndexOf(filePath);
        if (index >= 0 && index < SelectedFiles.Count - 1)
        {
            SelectedFiles.Move(index, index + 1);
            SyncPreviewItems();
            ResetState();
        }
    }

    [RelayCommand]
    public void RemoveFile(object? parameter)
    {
        string? filePath = parameter switch
        {
            string s => s,
            PdfFilePreviewItem item => item.FilePath,
            _ => null
        };

        if (string.IsNullOrEmpty(filePath)) return;

        SelectedFiles.Remove(filePath);
        SyncPreviewItems();
        ResetState();
    }

    [RelayCommand]
    public void ClearFiles()
    {
        SelectedFiles.Clear();
        SelectedFilePreviewItems.Clear();
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
        if (!ValidateInputs(out var validationError))
        {
            HasError = true;
            ErrorMessage = validationError;
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
            var result = await ExecuteCoreAsync(progress, _cts.Token);

            if (result.Success)
            {
                IsComplete = true;
                LastOutputFilePath = result.OutputFilePath ?? "";
                ResultSummaryMessage = result.Message ?? "Operation completed successfully.";
                StatusMessage = "Completed successfully!";
                ProgressPercentage = 100.0;

                // Load rich in-app preview for the output document
                if (!string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath))
                {
                    int pages = PdfFileHelper.InspectPageCountSafely(LastOutputFilePath);
                    OutputFilePreview = PdfFilePreviewItem.CreateFromFile(LastOutputFilePath, 1, pages);
                    LoadOutputPreviewThumbnails(LastOutputFilePath);
                }
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
        }
    }

    private void LoadOutputPreviewThumbnails(string filePath)
    {
        OutputPageThumbnails.Clear();
        if (!File.Exists(filePath)) return;

        try
        {
            using var pig = UglyToad.PdfPig.PdfDocument.Open(filePath);
            int total = pig.NumberOfPages;
            for (int i = 1; i <= total; i++)
            {
                var page = pig.GetPage(i);
                bool isLandscape = page.Width > page.Height;
                string summary = string.Empty;
                if (!string.IsNullOrWhiteSpace(page.Text))
                {
                    string firstLine = page.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    if (firstLine.Length > 40) firstLine = firstLine.Substring(0, 40) + "...";
                    summary = firstLine;
                }

                var thumb = new PdfPagePreviewThumbnail
                {
                    PageNumber = i,
                    PageLabel = $"Page {i} of {total}",
                    WidthPoints = Math.Round(page.Width, 1),
                    HeightPoints = Math.Round(page.Height, 1),
                    IsLandscape = isLandscape,
                    DimensionsText = $"{Math.Round(page.Width):F0} × {Math.Round(page.Height):F0} pt",
                    PageSummary = summary,
                    IsSelected = (i == 1)
                };
                OutputPageThumbnails.Add(thumb);
            }
            SelectedOutputPage = OutputPageThumbnails.FirstOrDefault();
        }
        catch
        {
            int pageCount = PdfFileHelper.InspectPageCountSafely(filePath);
            for (int i = 1; i <= Math.Max(1, pageCount); i++)
            {
                OutputPageThumbnails.Add(new PdfPagePreviewThumbnail
                {
                    PageNumber = i,
                    PageLabel = $"Page {i} of {pageCount}",
                    WidthPoints = 595,
                    HeightPoints = 842,
                    IsLandscape = false,
                    DimensionsText = "595 × 842 pt",
                    IsSelected = (i == 1)
                });
            }
            SelectedOutputPage = OutputPageThumbnails.FirstOrDefault();
        }
    }

    [RelayCommand]
    public void SelectPageThumbnail(PdfPagePreviewThumbnail? page)
    {
        if (page == null) return;
        foreach (var p in OutputPageThumbnails)
        {
            p.IsSelected = (p.PageNumber == page.PageNumber);
        }
        SelectedOutputPage = page;
    }

    [RelayCommand]
    public async Task SaveOutputFileAsAsync()
    {
        if (StorageProvider == null || string.IsNullOrWhiteSpace(LastOutputFilePath) || !File.Exists(LastOutputFilePath)) return;

        IStorageFolder? startFolder = null;
        if (!string.IsNullOrEmpty(RememberedSaveDirectory) && Directory.Exists(RememberedSaveDirectory))
        {
            try
            {
                startFolder = await StorageProvider.TryGetFolderFromPathAsync(RememberedSaveDirectory);
            }
            catch { }
        }

        string suggestedName = Path.GetFileName(LastOutputFilePath);
        string ext = Path.GetExtension(LastOutputFilePath).TrimStart('.');
        if (string.IsNullOrEmpty(ext)) ext = "pdf";

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Save {Tool.Name} Output File",
            SuggestedFileName = suggestedName,
            DefaultExtension = ext,
            SuggestedStartLocation = startFolder,
            FileTypeChoices = new[]
            {
                new FilePickerFileType($"{ext.ToUpperInvariant()} File")
                {
                    Patterns = new[] { $"*.{ext}" }
                }
            }
        });

        if (file != null)
        {
            string targetPath = file.Path.LocalPath;
            File.Copy(LastOutputFilePath, targetPath, overwrite: true);
            RememberedSaveDirectory = Path.GetDirectoryName(targetPath);
            SavedNotificationMessage = $"Saved successfully to: {targetPath}";
            HasSavedNotification = true;
        }
    }

    [RelayCommand]
    public void OpenOutputFile()
    {
        if (string.IsNullOrWhiteSpace(LastOutputFilePath) || !File.Exists(LastOutputFilePath)) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = LastOutputFilePath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch { }
    }

    [RelayCommand]
    public void OpenOutputFolder()
    {
        if (string.IsNullOrWhiteSpace(LastOutputFilePath)) return;

        try
        {
            string? dir = File.Exists(LastOutputFilePath) ? Path.GetDirectoryName(LastOutputFilePath) : LastOutputFilePath;
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
        }
        catch { }
    }

    [RelayCommand]
    public void StartOver()
    {
        ClearFiles();
        ResetState();
    }

    [RelayCommand]
    public void ToggleStar()
    {
        IsToolStarred = !IsToolStarred;
    }

    protected virtual bool ValidateInputs(out string errorMessage)
    {
        if (!HasSelectedFiles)
        {
            errorMessage = "Please select at least one document to proceed.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    protected abstract Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct);
}
