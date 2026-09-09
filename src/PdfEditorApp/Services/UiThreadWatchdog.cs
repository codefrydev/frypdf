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

    /// <summary>Delays at or above this are reported as warnings.</summary>
    private const int StallThresholdMs = 300;

    /// <summary>
    /// Delays at or above this, but below <see cref="StallThresholdMs"/>, are reported at Debug.
    /// </summary>
    /// <remarks>
    /// The 300ms warning threshold is about a visible freeze. It is far too coarse for the
    /// other thing a blocked UI thread breaks: this process hosts plugins on real-time threads
    /// — the music player runs SoundFlow's audio callback, which is CLR-attached and therefore
    /// suspended by every GC — and an audio device typically has tens of milliseconds of buffer.
    /// A 40ms hitch is already an audible dropout while being invisible at 300ms. Two tiers
    /// keep the warning signal-to-noise intact while making the audio-relevant range visible.
    /// </remarks>
    private const int HitchThresholdMs = 50;

    private readonly Timer _timer;
    private readonly IAppLogService _log;

    /// <summary>1 while a probe is outstanding, so a stalled UI thread cannot queue a backlog.</summary>
    private int _probeInFlight;

    private bool _isDisposed;

    /// <summary>Longest dispatcher delay observed so far, in milliseconds.</summary>
    public long WorstStallMs { get; private set; }

    // Collection counts at the previous report, so each report carries a delta.
    private int _lastGen0;
    private int _lastGen1;
    private int _lastGen2;

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
                if (delayMs < HitchThresholdMs) return;

                if (delayMs > WorstStallMs) WorstStallMs = delayMs;

                // Collections since the last report distinguish a GC-driven stall (which also
                // suspends plugin real-time threads, so it is the audio-relevant kind) from one
                // caused by long synchronous work or lock contention on the UI thread.
                int gen0 = GC.CollectionCount(0);
                int gen1 = GC.CollectionCount(1);
                int gen2 = GC.CollectionCount(2);

                string gcDelta =
                    $"gc {gen0 - _lastGen0}/{gen1 - _lastGen1}/{gen2 - _lastGen2} (gen0/1/2)";

                _lastGen0 = gen0;
                _lastGen1 = gen1;
                _lastGen2 = gen2;

                var level = delayMs >= StallThresholdMs ? AppLogLevel.Warning : AppLogLevel.Debug;

                _log.Log(level, "UiStall",
                    $"UI thread was blocked for {delayMs}ms (probe queued at " +
                    $"{DispatcherPriority.Background} priority, {gcDelta}, " +
                    $"latency mode {System.Runtime.GCSettings.LatencyMode}).");
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
