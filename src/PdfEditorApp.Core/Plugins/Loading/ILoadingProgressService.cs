using System;
using System.Threading;

namespace PdfEditorApp.Core.Plugins.Loading;

/// <summary>
/// Active handle representing an in-flight loading progress display.
/// Disposing this handle dismisses the loading overlay.
/// </summary>
public interface ILoadingProgressHandle : IDisposable
{
    /// <summary>Updates the live status message and optionally updates progress percent and active phase.</summary>
    void UpdateStatus(string message, double? progressPercent = null, int? activePhaseIndex = null);

    /// <summary>Updates the primary title and optional file size label.</summary>
    void UpdateTitle(string title, string? fileSize = null);

    /// <summary>Cancellation token triggered if the user clicks Cancel or presses Esc.</summary>
    CancellationToken CancellationToken { get; }
}

/// <summary>
/// Universal microkernel service for managing application-wide, full-screen loading experiences.
/// Contributed through plugins and accessible across all tools, engines, and viewmodels.
/// </summary>
public interface ILoadingProgressService
{
    /// <summary>
    /// Displays the full-screen loading progress overlay with the specified options.
    /// Returns a disposable handle to update and cleanly dismiss the loading display.
    /// </summary>
    ILoadingProgressHandle Show(LoadingProgressOptions options);

    /// <summary>Directly hides the loading progress overlay if active.</summary>
    void Hide();

    /// <summary>Triggers cancellation on the active loading operation if cancellable.</summary>
    void Cancel();

    /// <summary>Gets whether the loading progress overlay is currently active.</summary>
    bool IsActive { get; }

    /// <summary>Gets the current loading configuration options, if active.</summary>
    LoadingProgressOptions? CurrentOptions { get; }

    /// <summary>Fired whenever the loading overlay state, status, or progress updates.</summary>
    event Action? StateChanged;
}
