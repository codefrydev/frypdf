using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace PdfEditorApp.Services;

/// <summary>
/// Coalesces a burst of requests into a single deferred callback on the UI thread.
/// </summary>
/// <remarks>
/// Several view models grew their own copy of this: a <see cref="CancellationTokenSource"/>
/// field, cancel-and-dispose-the-previous-one, <c>Task.Run</c> + <c>Task.Delay</c>, then
/// <c>Dispatcher.UIThread.Post</c> — see the zoom debounce in
/// <c>ViewModels/Shell/PdfLivePreviewViewModel</c> and the chart resize debounce in
/// <c>ViewModels/ElementViewModels/ChartElementViewModel</c>. Each copy had to remember to
/// dispose the outgoing token source, and the ones that forgot leaked a timer handle per tick.
///
/// This only coalesces. It deliberately does not move work off the UI thread, because what
/// needs to move differs per caller: the callback runs on the UI thread so it can read view
/// model state safely, and expensive rendering inside it is the caller's job to offload.
///
/// Required by section 4 of .agents/rules/performance_and_zero_lag_mandate.md, which asks for
/// 150-250ms debouncing on text filters and throttling of continuous input.
/// </remarks>
internal sealed class UiDebouncer : IDisposable
{
    private readonly int _delayMs;
    private readonly Action _onElapsed;
    private readonly DispatcherPriority _priority;

    private CancellationTokenSource? _cts;
    private bool _isDisposed;

    /// <param name="delayMs">Quiet period before <paramref name="onElapsed"/> runs.</param>
    /// <param name="onElapsed">Invoked on the UI thread once requests stop for the delay.</param>
    /// <param name="priority">
    /// Priority of the posted callback. Defaults to <see cref="DispatcherPriority.Background"/>
    /// so a deferred re-render yields to the user's next interaction.
    /// </param>
    public UiDebouncer(int delayMs, Action onElapsed, DispatcherPriority? priority = null)
    {
        if (delayMs < 0) throw new ArgumentOutOfRangeException(nameof(delayMs));

        _delayMs = delayMs;
        _onElapsed = onElapsed ?? throw new ArgumentNullException(nameof(onElapsed));
        _priority = priority ?? DispatcherPriority.Background;
    }

    /// <summary>
    /// Restarts the quiet period, discarding any pending callback.
    /// </summary>
    public void Request()
    {
        if (_isDisposed) return;

        // Cancel *and* dispose the outgoing source: replacing it without disposing leaks its
        // registrations and internal timer handle on every request.
        _cts?.Cancel();
        _cts?.Dispose();

        var cts = new CancellationTokenSource();
        _cts = cts;
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delayMs, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested) return;

            Dispatcher.UIThread.Post(() =>
            {
                if (!token.IsCancellationRequested && !_isDisposed) _onElapsed();
            }, _priority);
        }, token);
    }

    /// <summary>
    /// Runs the callback immediately, cancelling anything pending.
    /// </summary>
    /// <remarks>Must be called on the UI thread, like the callback itself.</remarks>
    public void Flush()
    {
        if (_isDisposed) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _onElapsed();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
