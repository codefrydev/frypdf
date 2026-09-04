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
using PdfEditorApp.Messages;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.ViewModels.Shell;
using UglyToad.PdfPig;

namespace PdfEditorApp.ViewModels.Tools.Core;

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
    [NotifyPropertyChangedFor(nameof(CanExecuteTool))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteToolCommand))]
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

    [ObservableProperty]
    private long _originalSizeBytes;

    [ObservableProperty]
    private long _outputSizeBytes;

    [ObservableProperty]
    private double _sizeReductionPercentage;

    public bool HasSizeReduction => OriginalSizeBytes > 0 && OutputSizeBytes > 0 && OutputSizeBytes < OriginalSizeBytes;
    public string SizeComparisonText => OriginalSizeBytes > 0 && OutputSizeBytes > 0 
        ? $"{PdfFilePreviewItem.FormatBytes(OriginalSizeBytes)} → {PdfFilePreviewItem.FormatBytes(OutputSizeBytes)}"
        : "";
    public string SizeReductionBadgeText => $"{SizeReductionPercentage:F0}% SAVED";

    public ObservableCollection<string> SelectedFiles { get; } = new();

    public ObservableCollection<PdfFilePreviewItem> SelectedFilePreviewItems { get; } = new();

    /// <summary>
    /// Reader-style live preview (page render, zoom, page nav) of the currently
    /// selected input file, shared by the reader-matching tool shell
    /// (<see cref="PdfEditorApp.Views.Shell.PdfToolWorkspaceView"/>). Reloads
    /// automatically whenever <see cref="SelectedFiles"/> changes.
    /// </summary>
    public PdfLivePreviewViewModel Preview { get; } = new();

    /// <summary>
    /// True for tools migrated to the shared reader-style workspace shell
    /// (<see cref="PdfEditorApp.Views.Shell.PdfToolWorkspaceView"/>), which has its own
    /// back button, tool identity, and toolbar. <see cref="PdfEditorApp.Views.PdfToolPageView"/>
    /// hides its separate header banner for these so there's exactly one toolbar row,
    /// matching the PDF Reader, instead of two stacked bars.
    /// </summary>
    public virtual bool UsesWorkspaceShell => false;

    public ObservableCollection<PdfPagePreviewThumbnail> OutputPageThumbnails { get; } = new();

    public bool HasSelectedFiles => SelectedFiles.Count > 0;
    public virtual bool CanExecuteTool => !IsRunning;
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
    public event Action<PdfToolId, string>? NavigateToToolRequested;
    public event Action<PdfToolId>? HelpGuideRequested;

    [RelayCommand]
    public void OpenToolHelpGuide()
    {
        HelpGuideRequested?.Invoke(Tool.Id);
    }

    [RelayCommand]
    public void NavigateToTool(string toolName)
    {
        if (Enum.TryParse<PdfToolId>(toolName, true, out var toolId))
        {
            string targetFile = !string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath)
                ? LastOutputFilePath
                : PrimaryInputFile;
            NavigateToToolRequested?.Invoke(toolId, targetFile);
        }
    }

    protected PdfToolViewModelBase(IPdfDocumentOperationsService operationsService, PdfToolDefinition tool)
    {
        OperationsService = operationsService;
        Tool = tool;
        SelectedFiles.CollectionChanged += (_, _) => { _ = Preview.LoadDocumentAsync(PrimaryInputFile); };
    }

    [RelayCommand]
    public void GoBack()
    {
        BackRequested?.Invoke();
    }

    [RelayCommand]
    public void OpenInEditor()
    {
        string? targetPath = (!string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath))
            ? LastOutputFilePath
            : ((HasSelectedFiles && File.Exists(PrimaryInputFile)) ? PrimaryInputFile : null);

        if (!string.IsNullOrEmpty(targetPath))
        {
            WeakReferenceMessenger.Default.Send(new OpenInEditorMessage(targetPath));
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
            WeakReferenceMessenger.Default.Send(new OpenInViewerMessage(targetPath));
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

    [RelayCommand(CanExecute = nameof(CanExecuteTool))]
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

                OriginalSizeBytes = result.OriginalSizeBytes;
                OutputSizeBytes = result.OutputSizeBytes;
                if (result.OriginalSizeBytes > 0 && result.OutputSizeBytes > 0 && result.OutputSizeBytes < result.OriginalSizeBytes)
                {
                    SizeReductionPercentage = ((result.OriginalSizeBytes - result.OutputSizeBytes) / (double)result.OriginalSizeBytes) * 100.0;
                }
                else
                {
                    SizeReductionPercentage = 0.0;
                }
                OnPropertyChanged(nameof(HasSizeReduction));
                OnPropertyChanged(nameof(SizeComparisonText));
                OnPropertyChanged(nameof(SizeReductionBadgeText));

                // Load rich in-app preview for the output document
                if (!string.IsNullOrEmpty(LastOutputFilePath) && File.Exists(LastOutputFilePath))
                {
                    int pages = PdfFileHelper.InspectPageCountSafely(LastOutputFilePath);
                    OutputFilePreview = PdfFilePreviewItem.CreateFromFile(LastOutputFilePath, 1, pages);
                    LoadOutputPreviewThumbnails(LastOutputFilePath);
                    _ = Preview.LoadDocumentAsync(LastOutputFilePath);
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
            SaveOutputFileToPath(file.Path.LocalPath);
        }
    }

    /// <summary>
    /// Saves the current output file to <paramref name="targetPath"/>.
    /// Gracefully prevents self-copy errors and handles locked file IO exceptions.
    /// </summary>
    public void SaveOutputFileToPath(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath) || string.IsNullOrWhiteSpace(LastOutputFilePath) || !File.Exists(LastOutputFilePath)) return;

        string fullSource = Path.GetFullPath(LastOutputFilePath);
        string fullTarget = Path.GetFullPath(targetPath);

        // If the user picked the exact same path where the output is already stored,
        // copying the file onto itself will throw an IOException ("used by another process").
        // Since it's already there, simply update notification and return successfully.
        if (string.Equals(fullSource, fullTarget, StringComparison.OrdinalIgnoreCase))
        {
            RememberedSaveDirectory = Path.GetDirectoryName(targetPath);
            SavedNotificationMessage = $"Saved successfully to: {targetPath}";
            HasSavedNotification = true;
            HasError = false;
            return;
        }

        try
        {
            File.Copy(LastOutputFilePath, targetPath, overwrite: true);
            RememberedSaveDirectory = Path.GetDirectoryName(targetPath);
            SavedNotificationMessage = $"Saved successfully to: {targetPath}";
            HasSavedNotification = true;
            HasError = false;
        }
        catch (IOException)
        {
            HasSavedNotification = false;
            HasError = true;
            ErrorMessage = $"Cannot save to '{Path.GetFileName(targetPath)}': The file is currently open or in use by another application. Please close it and try again.";
        }
        catch (UnauthorizedAccessException)
        {
            HasSavedNotification = false;
            HasError = true;
            ErrorMessage = $"Cannot save to '{Path.GetFileName(targetPath)}': Access denied. Please choose a different folder.";
        }
        catch (Exception ex)
        {
            HasSavedNotification = false;
            HasError = true;
            ErrorMessage = $"Failed to save file: {ex.Message}";
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

    /// <summary>
    /// Runs the tool once per file in <see cref="SelectedFiles"/> via the standard
    /// PdfToolId dispatch, instead of silently processing only <see cref="PrimaryInputFile"/>.
    /// One failing file does not abort the rest; the aggregated result reports exactly
    /// how many files succeeded/failed instead of a blanket success.
    /// </summary>
    protected async Task<ToolExecutionResult> ExecuteBatchAsync(
        Func<string, object> buildOptions,
        IProgress<double> progress,
        CancellationToken ct)
    {
        var files = SelectedFiles.ToList();
        var results = new List<(string File, ToolExecutionResult Result)>();

        for (int i = 0; i < files.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            string file = files[i];
            int index = i;
            int total = files.Count;
            var fileProgress = new Progress<double>(p =>
                progress.Report((index + Math.Clamp(p, 0, 100) / 100.0) / total * 100.0));

            ToolExecutionResult result;
            try
            {
                result = await OperationsService.ExecuteToolAsync(Tool.Id, buildOptions(file), fileProgress, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result = new ToolExecutionResult { Success = false, ErrorMessage = ex.Message };
            }

            results.Add((file, result));
        }

        progress.Report(100);

        // Single file: return the underlying result untouched so existing single-file
        // behavior (messages, ExtraData, etc.) is unaffected.
        if (results.Count == 1)
        {
            return results[0].Result;
        }

        var succeeded = results.Where(r => r.Result.Success).ToList();
        var failed = results.Where(r => !r.Result.Success).ToList();
        string DescribeFailure((string File, ToolExecutionResult Result) f) =>
            $"{Path.GetFileName(f.File)} ({f.Result.ErrorMessage ?? "failed"})";

        if (succeeded.Count == 0)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"All {results.Count} files failed: {string.Join("; ", failed.Select(DescribeFailure))}"
            };
        }

        string message = failed.Count == 0
            ? $"Successfully processed {succeeded.Count} of {results.Count} files."
            : $"Processed {succeeded.Count} of {results.Count} files. Failed: {string.Join("; ", failed.Select(DescribeFailure))}";

        return new ToolExecutionResult
        {
            Success = true,
            OutputFilePath = succeeded[^1].Result.OutputFilePath,
            OutputFiles = succeeded
                .Select(s => s.Result.OutputFilePath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p!)
                .ToList(),
            Message = message,
            OriginalSizeBytes = succeeded.Sum(s => s.Result.OriginalSizeBytes),
            OutputSizeBytes = succeeded.Sum(s => s.Result.OutputSizeBytes)
        };
    }

    protected abstract Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct);
}
