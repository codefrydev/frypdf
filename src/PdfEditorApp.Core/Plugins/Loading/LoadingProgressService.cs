using System;
using System.Threading;

namespace PdfEditorApp.Core.Plugins.Loading;

/// <summary>
/// Thread-safe default implementation of <see cref="ILoadingProgressService"/>.
/// </summary>
public sealed class LoadingProgressService : ILoadingProgressService
{
    private readonly object _syncLock = new();
    private ActiveHandle? _activeHandle;

    public bool IsActive
    {
        get
        {
            lock (_syncLock)
            {
                return _activeHandle != null;
            }
        }
    }

    public LoadingProgressOptions? CurrentOptions
    {
        get
        {
            lock (_syncLock)
            {
                return _activeHandle?.CurrentOptions;
            }
        }
    }

    public event Action? StateChanged;

    public ILoadingProgressHandle Show(LoadingProgressOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ActiveHandle handle;
        lock (_syncLock)
        {
            _activeHandle?.Dispose();
            handle = new ActiveHandle(this, options);
            _activeHandle = handle;
        }

        NotifyStateChanged();
        return handle;
    }

    public void Hide()
    {
        lock (_syncLock)
        {
            if (_activeHandle == null) return;
            _activeHandle = null;
        }

        NotifyStateChanged();
    }

    public void Cancel() => CancelCurrent();

    public void CancelCurrent()
    {
        ActiveHandle? handle;
        lock (_syncLock)
        {
            handle = _activeHandle;
            _activeHandle = null;
        }

        if (handle != null)
        {
            handle.TriggerCancel();
            NotifyStateChanged();
        }
    }

    private void OnHandleDisposed(ActiveHandle handle)
    {
        lock (_syncLock)
        {
            if (ReferenceEquals(_activeHandle, handle))
            {
                _activeHandle = null;
            }
            else
            {
                return;
            }
        }

        NotifyStateChanged();
    }

    internal void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    private sealed class ActiveHandle : ILoadingProgressHandle
    {
        private readonly LoadingProgressService _owner;
        private readonly CancellationTokenSource _cts = new();
        private int _isDisposed;

        public LoadingProgressOptions CurrentOptions { get; private set; }

        public CancellationToken CancellationToken => _cts.Token;

        public ActiveHandle(LoadingProgressService owner, LoadingProgressOptions initialOptions)
        {
            _owner = owner;
            CurrentOptions = initialOptions;
        }

        public void UpdateStatus(string message, double? progressPercent = null, int? activePhaseIndex = null)
        {
            if (Volatile.Read(ref _isDisposed) != 0) return;

            CurrentOptions = new LoadingProgressOptions
            {
                Title = CurrentOptions.Title,
                Category = CurrentOptions.Category,
                StatusMessage = message,
                FileSize = CurrentOptions.FileSize,
                ProgressPercent = progressPercent ?? CurrentOptions.ProgressPercent,
                PipelinePhases = CurrentOptions.PipelinePhases,
                ActivePhaseIndex = activePhaseIndex ?? CurrentOptions.ActivePhaseIndex,
                IsCancellable = CurrentOptions.IsCancellable,
                OnCancel = CurrentOptions.OnCancel,
                CancelButtonText = CurrentOptions.CancelButtonText,
                IconKind = CurrentOptions.IconKind
            };

            _owner.NotifyStateChanged();
        }

        public void UpdateTitle(string title, string? fileSize = null)
        {
            if (Volatile.Read(ref _isDisposed) != 0) return;

            CurrentOptions = new LoadingProgressOptions
            {
                Title = title,
                Category = CurrentOptions.Category,
                StatusMessage = CurrentOptions.StatusMessage,
                FileSize = fileSize ?? CurrentOptions.FileSize,
                ProgressPercent = CurrentOptions.ProgressPercent,
                PipelinePhases = CurrentOptions.PipelinePhases,
                ActivePhaseIndex = CurrentOptions.ActivePhaseIndex,
                IsCancellable = CurrentOptions.IsCancellable,
                OnCancel = CurrentOptions.OnCancel,
                CancelButtonText = CurrentOptions.CancelButtonText,
                IconKind = CurrentOptions.IconKind
            };

            _owner.NotifyStateChanged();
        }

        public void TriggerCancel()
        {
            try
            {
                _cts.Cancel();
            }
            catch { }

            try
            {
                CurrentOptions.OnCancel?.Invoke();
            }
            catch { }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            _owner.OnHandleDisposed(this);
            _cts.Dispose();
        }
    }
}
