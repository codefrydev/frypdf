using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Core.Plugins.Loading;
using PdfEditorApp.Messages;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Display item representing a pipeline phase in the loading visualizer.
/// </summary>
public sealed class PipelinePhaseItem : ObservableObject
{
    public required string Name { get; init; }
    public required int Index { get; init; }
    public bool IsLast { get; init; }

    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}

/// <summary>
/// ViewModel driving the universal full-screen loading progress overlay.
/// Responds both to direct service events and CommunityToolkit.Mvvm pub/sub messages.
/// </summary>
public partial class LoadingProgressViewModel : ObservableObject,
    IRecipient<ShowLoadingProgressMessage>,
    IRecipient<UpdateLoadingProgressMessage>,
    IRecipient<HideLoadingProgressMessage>,
    IRecipient<CancelLoadingProgressMessage>
{
    private readonly ILoadingProgressService _service;
    private Action? _currentCancelCallback;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isActive;

    [ObservableProperty]
    private string _title = "Processing Document...";

    [ObservableProperty]
    private string _category = "DOCUMENT STUDIO";

    [ObservableProperty]
    private string _statusMessage = "Please wait...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFileSize))]
    private string? _fileSize;

    public bool HasFileSize => !string.IsNullOrWhiteSpace(FileSize);

    [ObservableProperty]
    private bool _isDeterminate;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isCancellable = true;

    [ObservableProperty]
    private string _cancelButtonText = "Cancel Operation";

    [ObservableProperty]
    private string _iconKind = "FilePdfBox";

    [ObservableProperty]
    private bool _hasPipelinePhases;

    public ObservableCollection<PipelinePhaseItem> PipelinePhases { get; } = new();

    public LoadingProgressViewModel(ILoadingProgressService? service = null)
    {
        _service = service ?? new LoadingProgressService();
        _service.StateChanged += OnServiceStateChanged;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    private void OnServiceStateChanged()
    {
        void Update()
        {
            var options = _service.CurrentOptions;
            if (_service.IsActive && options != null)
            {
                ApplyOptions(options);
                IsActive = true;
            }
            else
            {
                IsActive = false;
                _currentCancelCallback = null;
            }
        }

        if (Dispatcher.UIThread.CheckAccess() || Avalonia.Application.Current == null)
        {
            Update();
        }
        else
        {
            Dispatcher.UIThread.Post(Update);
        }
    }

    private void ApplyOptions(LoadingProgressOptions options)
    {
        Title = options.Title;
        Category = options.Category;
        StatusMessage = options.StatusMessage;
        FileSize = options.FileSize;
        IconKind = options.IconKind;
        IsCancellable = options.IsCancellable;
        CancelButtonText = options.CancelButtonText;
        _currentCancelCallback = options.OnCancel;

        if (options.ProgressPercent.HasValue)
        {
            IsDeterminate = true;
            ProgressPercent = Math.Clamp(options.ProgressPercent.Value, 0.0, 100.0);
        }
        else
        {
            IsDeterminate = false;
            ProgressPercent = 0.0;
        }

        PipelinePhases.Clear();
        if (options.PipelinePhases != null && options.PipelinePhases.Count > 0)
        {
            for (int i = 0; i < options.PipelinePhases.Count; i++)
            {
                PipelinePhases.Add(new PipelinePhaseItem
                {
                    Name = options.PipelinePhases[i],
                    Index = i,
                    IsLast = (i == options.PipelinePhases.Count - 1),
                    IsActive = (i == options.ActivePhaseIndex),
                    IsCompleted = (i < options.ActivePhaseIndex)
                });
            }
        }
        HasPipelinePhases = PipelinePhases.Count > 0;
    }

    public bool CanCancel => IsActive && IsCancellable;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    public void Cancel()
    {
        if (!CanCancel) return;

        var cancelCallback = _currentCancelCallback;
        _service.Cancel();
        IsActive = false;
        _currentCancelCallback = null;

        try
        {
            cancelCallback?.Invoke();
        }
        catch { }

        WeakReferenceMessenger.Default.Send(new NavigateToHomeMessage());
    }

    public void Receive(ShowLoadingProgressMessage message)
    {
        _service.Show(message.Options);
    }

    public void Receive(UpdateLoadingProgressMessage message)
    {
        var options = _service.CurrentOptions;
        if (options != null)
        {
            _service.Show(new LoadingProgressOptions
            {
                Title = options.Title,
                Category = options.Category,
                StatusMessage = message.StatusMessage,
                FileSize = options.FileSize,
                ProgressPercent = message.ProgressPercent ?? options.ProgressPercent,
                PipelinePhases = options.PipelinePhases,
                ActivePhaseIndex = message.ActivePhaseIndex ?? options.ActivePhaseIndex,
                IsCancellable = options.IsCancellable,
                OnCancel = options.OnCancel,
                CancelButtonText = options.CancelButtonText,
                IconKind = options.IconKind
            });
        }
    }

    public void Receive(HideLoadingProgressMessage message)
    {
        _service.Hide();
    }

    public void Receive(CancelLoadingProgressMessage message)
    {
        Cancel();
    }
}
