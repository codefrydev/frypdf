using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Threading;

namespace PdfEditorApp.Services;

/// <summary>
/// Detects stalls on the Avalonia UI thread and reports them to <see cref="AppLogService"/>.
/// </summary>
/// <remarks>
/// Nothing in the app previously measured UI-thread responsiveness, so a multi-second freeze
/// left no trace: the per-operation stopwatches all stop before Avalonia's layout and render
/// pass, which is where the time actually goes. This probes from the outside instead — it posts
/// a trivial callback to the dispatcher on a fixed cadence and measures how long the dispatcher
/// takes to run it. Any block, whatever its cause, shows up.
/// </remarks>
public sealed class UiThreadWatchdog : IDisposable
{
    private const int ProbeIntervalMs = 500;

    /// <summary>Delays at or above this are reported. Below ~2 frames is normal scheduling jitter.</summary>
    private const int StallThresholdMs = 300;

    private readonly Timer _timer;
    private readonly IAppLogService _log;

    /// <summary>1 while a probe is outstanding, so a stalled UI thread cannot queue a backlog.</summary>
    private int _probeInFlight;

    private bool _isDisposed;

    /// <summary>Longest dispatcher delay observed so far, in milliseconds.</summary>
    public long WorstStallMs { get; private set; }

    public UiThreadWatchdog(IAppLogService? log = null)
    {
        _log = log ?? AppLogService.Instance;
        _timer = new Timer(_ => Probe(), null, ProbeIntervalMs, ProbeIntervalMs);
    }

    private void Probe()
    {
        if (_isDisposed) return;

        // A stalled UI thread must not accumulate probes; one outstanding at a time is enough
        // to measure the stall, and the delay it reports covers the whole blocked period.
        if (Interlocked.CompareExchange(ref _probeInFlight, 1, 0) != 0) return;

        if (Application.Current == null)
        {
            Interlocked.Exchange(ref _probeInFlight, 0);
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            Dispatcher.UIThread.Post(() =>
            {
                sw.Stop();
                Interlocked.Exchange(ref _probeInFlight, 0);

                long delayMs = sw.ElapsedMilliseconds;
                if (delayMs < StallThresholdMs) return;

                if (delayMs > WorstStallMs) WorstStallMs = delayMs;

                _log.Log(AppLogLevel.Warning, "UiStall",
                    $"UI thread was blocked for {delayMs}ms (probe queued at " +
                    $"{DispatcherPriority.Background} priority).");
            }, DispatcherPriority.Background);
        }
        catch
        {
            // No dispatcher loop available — nothing to measure.
            Interlocked.Exchange(ref _probeInFlight, 0);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _timer.Dispose();
    }
}
