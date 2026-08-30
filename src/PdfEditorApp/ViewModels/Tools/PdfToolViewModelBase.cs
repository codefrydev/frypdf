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

namespace PdfEditorApp.ViewModels.Tools;

/// <summary>
/// Abstract base class for all individual PDF tool ViewModels.
/// Provides unified lifecycle, file management, asynchronous execution, cancellation, and navigation.
/// </summary>
public abstract partial class PdfToolViewModelBase : ViewModelBase
{
    protected readonly IPdfDocumentOperationsService OperationsService;
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

    [ObservableProperty]
    private bool _isToolStarred;

    public ObservableCollection<string> SelectedFiles { get; } = new();

    public bool HasSelectedFiles => SelectedFiles.Count > 0;
    public string SelectedFilesCountText => SelectedFiles.Count == 1 ? "1 file selected" : $"{SelectedFiles.Count} files selected";
    public string PrimaryInputFile => SelectedFiles.FirstOrDefault() ?? string.Empty;

    // Events
    public event Action? BackRequested;
    public event Action<string>? OpenInEditorRequested;

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
        OnPropertyChanged(nameof(HasSelectedFiles));
        OnPropertyChanged(nameof(SelectedFilesCountText));
        OnPropertyChanged(nameof(PrimaryInputFile));
    }

    [RelayCommand]
    public async Task AddFilesAsync()
    {
        if (StorageProvider == null) return;

        var patterns = Tool.AcceptedFileExtensions
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
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
                if (!SelectedFiles.Contains(path))
                {
                    SelectedFiles.Add(path);
                }
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
    public void ToggleStar()
    {
        IsToolStarred = !IsToolStarred;
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

    /// <summary>
    /// Validates whether the tool is ready for execution.
    /// </summary>
    protected virtual bool ValidateInputs(out string errorMessage)
    {
        if (SelectedFiles.Count == 0 && Tool.Id != PdfToolId.HtmlToPdf)
        {
            errorMessage = "Please select at least one document to process.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Executes the core logic specific to this tool.
    /// </summary>
    protected abstract Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct);
}
