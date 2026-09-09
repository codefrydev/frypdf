using System;
using System.Runtime;
using System.Threading;
using PdfEditorApp.Core.Plugins.Realtime;

namespace PdfEditorApp.Services;

/// <summary>
/// Reference-counted implementation of <see cref="IRealtimeWorkCoordinator"/> that switches the
/// GC to <see cref="GCLatencyMode.SustainedLowLatency"/> while real-time work is declared.
/// </summary>
/// <remarks>
/// Only the latency mode is touched. Workstation GC with background collection — the .NET
/// default, and what this app runs — is already the right choice for a desktop app with a
/// deadline thread; switching to Server GC would raise throughput and memory at the cost of the
/// pause behaviour that actually matters here. What the default does not do is avoid *blocking*
/// generation-2 collections, and those are long enough to empty an audio device's buffer.
///
/// The honest limit: this does nothing about generation 0 and 1 pauses, which still suspend
/// every managed thread including a plugin's audio callback. Reducing allocation on the editor's
/// hot paths and giving the plugin real buffer headroom are the fixes; this only widens the
/// margin.
/// </remarks>
public sealed class RealtimeWorkCoordinator : IRealtimeWorkCoordinator
{
    private readonly IAppLogService _log;
    private readonly object _gate = new();

    private int _activeCount;
    private GCLatencyMode _previousMode;

    public RealtimeWorkCoordinator(IAppLogService? log = null)
    {
        _log = log ?? AppLogService.Instance;
    }

    public bool IsRealtimeWorkActive => Volatile.Read(ref _activeCount) > 0;

    public IDisposable BeginRealtimeWork(string reason)
    {
        lock (_gate)
        {
            if (_activeCount == 0)
            {
                _previousMode = GCSettings.LatencyMode;

                // NoGCRegion cannot be left via LatencyMode, and Batch/Interactive are both
                // safe to raise from. Guard anyway: assigning over an active NoGCRegion throws.
                if (_previousMode != GCLatencyMode.NoGCRegion)
                {
                    try
                    {
                        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                        _log.Log(AppLogLevel.Info, "Realtime",
                            $"Real-time work started ('{reason}'); GC latency mode " +
                            $"{_previousMode} -> {GCSettings.LatencyMode}.");
                    }
                    catch (Exception ex)
                    {
                        // Non-fatal: the caller's work still runs, just without the tuning.
                        _log.LogWarning("Realtime",
                            $"Could not raise GC latency mode for '{reason}'", ex);
                    }
                }
            }

            _activeCount++;
        }

        return new RealtimeScope(this, reason);
    }

    private void End(string reason)
    {
        lock (_gate)
        {
            if (_activeCount == 0) return;

            _activeCount--;
            if (_activeCount > 0) return;

            try
            {
                if (GCSettings.LatencyMode != GCLatencyMode.NoGCRegion)
                {
                    GCSettings.LatencyMode = _previousMode;
                    _log.Log(AppLogLevel.Info, "Realtime",
                        $"Real-time work ended ('{reason}'); GC latency mode restored to " +
                        $"{GCSettings.LatencyMode}.");
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning("Realtime", $"Could not restore GC latency mode after '{reason}'", ex);
            }
        }
    }

    /// <summary>Handle returned to the caller; idempotent so a double dispose cannot underflow.</summary>
    private sealed class RealtimeScope : IDisposable
    {
        private RealtimeWorkCoordinator? _owner;
        private readonly string _reason;

        public RealtimeScope(RealtimeWorkCoordinator owner, string reason)
        {
            _owner = owner;
            _reason = reason;
        }

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.End(_reason);
        }
    }
}
