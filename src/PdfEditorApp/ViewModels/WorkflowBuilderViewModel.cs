using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Tools;

namespace PdfEditorApp.ViewModels;

public partial class WorkflowBuilderViewModel : ViewModelBase
{
    private readonly IPdfWorkflowEngine _workflowEngine;
    private readonly IPdfToolRegistry _toolRegistry;
    private CancellationTokenSource? _cts;

    public IStorageProvider? StorageProvider { get; set; }

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private WorkflowDefinition _currentWorkflow = new();

    [ObservableProperty]
    private WorkflowStepDefinition? _selectedStep;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private string _resultSummary = string.Empty;

    public ObservableCollection<string> InputFiles { get; } = new();
    public ObservableCollection<WorkflowStepDefinition> Steps { get; } = new();
    public ObservableCollection<PdfToolDefinition> AvailableTools { get; } = new();
    public ObservableCollection<WorkflowDefinition> PresetWorkflows { get; } = new();

    public WorkflowBuilderViewModel(IPdfWorkflowEngine workflowEngine, IPdfToolRegistry toolRegistry)
    {
        _workflowEngine = workflowEngine;
        _toolRegistry = toolRegistry;

        // Populate available tool palette for workflow steps
        foreach (var t in _toolRegistry.GetAllTools().Where(x => !x.IsWorkflowBanner))
        {
            AvailableTools.Add(t);
        }

        // Load Presets
        foreach (var p in _workflowEngine.GetPresetWorkflows())
        {
            PresetWorkflows.Add(p);
        }

        LoadWorkflow(PresetWorkflows.FirstOrDefault() ?? new WorkflowDefinition());
    }

    public void Open()
    {
        IsOpen = true;
        ResetState();
    }

    public void ResetState()
    {
        IsRunning = false;
        IsComplete = false;
        HasError = false;
        ErrorMessage = string.Empty;
        StatusMessage = "Ready";
        ProgressPercentage = 0;
        ResultSummary = string.Empty;
    }

    public void LoadWorkflow(WorkflowDefinition workflow)
    {
        CurrentWorkflow = workflow.Clone();
        Steps.Clear();
        foreach (var s in CurrentWorkflow.Steps)
        {
            Steps.Add(s);
        }
        SelectedStep = Steps.FirstOrDefault();
        ResetState();
    }

    [RelayCommand]
    public void SelectPreset(WorkflowDefinition preset)
    {
        if (preset != null)
        {
            LoadWorkflow(preset);
        }
    }

    [RelayCommand]
    public void AddStep(PdfToolDefinition tool)
    {
        if (tool == null) return;
        var step = new WorkflowStepDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            ToolId = tool.Id,
            StepName = tool.Name,
            StepDescription = tool.Description,
            IsEnabled = true
        };
        Steps.Add(step);
        CurrentWorkflow.Steps.Add(step);
        SelectedStep = step;
    }

    [RelayCommand]
    public void RemoveStep(WorkflowStepDefinition step)
    {
        if (step == null) return;
        Steps.Remove(step);
        CurrentWorkflow.Steps.Remove(step);
        SelectedStep = Steps.FirstOrDefault();
    }

    [RelayCommand]
    public void MoveStepUp(WorkflowStepDefinition step)
    {
        if (step == null) return;
        int idx = Steps.IndexOf(step);
        if (idx > 0)
        {
            Steps.Move(idx, idx - 1);
            CurrentWorkflow.Steps.Clear();
            CurrentWorkflow.Steps.AddRange(Steps);
            SelectedStep = step;
        }
    }

    [RelayCommand]
    public void MoveStepDown(WorkflowStepDefinition step)
    {
        if (step == null) return;
        int idx = Steps.IndexOf(step);
        if (idx >= 0 && idx < Steps.Count - 1)
        {
            Steps.Move(idx, idx + 1);
            CurrentWorkflow.Steps.Clear();
            CurrentWorkflow.Steps.AddRange(Steps);
            SelectedStep = step;
        }
    }

    [RelayCommand]
    public async Task AddInputFilesAsync()
    {
        if (StorageProvider == null) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Input PDF Files for Pipeline",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PDF Documents (*.pdf)")
                {
                    Patterns = new[] { "*.pdf" }
                }
            }
        });

        if (files != null && files.Count > 0)
        {
            foreach (var f in files)
            {
                string path = f.Path.LocalPath;
                if (!InputFiles.Contains(path)) InputFiles.Add(path);
            }
        }
    }

    [RelayCommand]
    public void RemoveInputFile(string filePath)
    {
        InputFiles.Remove(filePath);
    }

    [RelayCommand]
    public void ClearInputFiles()
    {
        InputFiles.Clear();
    }

    [RelayCommand]
    public async Task RunWorkflowAsync()
    {
        if (InputFiles.Count == 0)
        {
            HasError = true;
            ErrorMessage = "Please add at least one input PDF file to process.";
            return;
        }

        if (Steps.Count == 0 || !Steps.Any(s => s.IsEnabled))
        {
            HasError = true;
            ErrorMessage = "Please add at least one enabled step to the pipeline.";
            return;
        }

        IsRunning = true;
        IsComplete = false;
        HasError = false;
        ErrorMessage = string.Empty;
        StatusMessage = "Starting pipeline...";
        ProgressPercentage = 0;

        _cts = new CancellationTokenSource();
        var progress = new Progress<WorkflowProgressInfo>(p =>
        {
            ProgressPercentage = p.OverallProgressPercentage;
            StatusMessage = p.StatusMessage;
        });

        try
        {
            CurrentWorkflow.Steps = Steps.ToList();
            var result = await _workflowEngine.ExecuteWorkflowAsync(CurrentWorkflow, InputFiles, progress, _cts.Token);

            if (result.Success)
            {
                IsComplete = true;
                ResultSummary = result.Message ?? "Workflow pipeline executed successfully.";
                StatusMessage = "Workflow Complete!";
                ProgressPercentage = 100.0;
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Workflow execution failed.";
                StatusMessage = "Failed";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Workflow cancelled.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Execution error: {ex.Message}";
            StatusMessage = "Error";
        }
        finally
        {
            IsRunning = false;
            _cts = null;
        }
    }

    [RelayCommand]
    public void CancelWorkflow()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            StatusMessage = "Cancelling pipeline...";
        }
    }

    [RelayCommand]
    public void Close()
    {
        CancelWorkflow();
        IsOpen = false;
    }
}
