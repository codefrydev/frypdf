using System;

namespace PdfEditorApp.Core.Plugins.Realtime;

/// <summary>
/// Lets a plugin tell the host it is running work with a hard deadline, so the host can tune
/// the runtime for latency while that work is live.
/// </summary>
/// <remarks>
/// FryPDF loads plugins into its own process, so a plugin can own a thread the host knows
/// nothing about. The music player is the motivating case: SoundFlow's playback callback is a
/// reverse P/Invoke from miniaudio's real-time thread that runs managed code, which makes it a
/// CLR-attached thread and therefore suspended by every garbage collection. Editing a document
/// is the most allocation-heavy thing this app does, so editor activity set the collection rate
/// that decided whether the user's music stuttered.
///
/// Scope this around the period real-time work is actually active, not the plugin's lifetime:
/// <code>
/// _realtimeScope = coordinator?.BeginRealtimeWork("music playback");
/// // ... later, when playback stops ...
/// _realtimeScope?.Dispose();
/// </code>
///
/// This is a mitigation, not a fix. <c>SustainedLowLatency</c> suppresses blocking generation-2
/// collections but not generation 0 and 1, so a plugin still must not depend on it: buffer
/// generously, keep disk I/O and decoding off the deadline thread, and do not allocate per
/// callback. See the real-time section of
/// <c>.agents/rules/performance_and_zero_lag_mandate.md</c>.
/// </remarks>
public interface IRealtimeWorkCoordinator
{
    /// <summary>
    /// Declares real-time work active until the returned handle is disposed.
    /// </summary>
    /// <param name="reason">Short description, for diagnostics. E.g. "music playback".</param>
    /// <returns>
    /// A handle that ends the declaration. Reference-counted, so overlapping declarations from
    /// several plugins are safe and the last one out restores the previous setting. Disposing
    /// twice is a no-op.
    /// </returns>
    IDisposable BeginRealtimeWork(string reason);

    /// <summary>True while at least one real-time declaration is outstanding.</summary>
    bool IsRealtimeWorkActive { get; }
}
