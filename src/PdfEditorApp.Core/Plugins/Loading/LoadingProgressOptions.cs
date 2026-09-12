using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Loading;

/// <summary>
/// Configuration options for displaying a full-screen loading progress overlay.
/// </summary>
public sealed class LoadingProgressOptions
{
    /// <summary>Primary title of the task or document being processed.</summary>
    public required string Title { get; init; }

    /// <summary>Category badge text (e.g. "DOCUMENT VIEWER", "OCR ENGINE", "BATCH PROCESSING", "PDF EXPORT").</summary>
    public string Category { get; init; } = "PROCESSING";

    /// <summary>Initial live status message.</summary>
    public string StatusMessage { get; init; } = "Please wait...";

    /// <summary>Optional human-readable file size or data metric (e.g. "14.8 MB", "12 items").</summary>
    public string? FileSize { get; init; }

    /// <summary>Optional progress percent (0.0 to 100.0). If null, progress is indeterminate.</summary>
    public double? ProgressPercent { get; init; }

    /// <summary>Optional sequence of processing phase names displayed as step chips.</summary>
    public IReadOnlyList<string>? PipelinePhases { get; init; }

    /// <summary>Zero-based index of the currently active pipeline phase.</summary>
    public int ActivePhaseIndex { get; init; } = 0;

    /// <summary>Whether the operation exposes a cancellation button and accepts Esc / Cmd+W gestures.</summary>
    public bool IsCancellable { get; init; } = true;

    /// <summary>Optional cancellation callback invoked when the user cancels.</summary>
    public Action? OnCancel { get; init; }

    /// <summary>Text for the cancellation button.</summary>
    public string CancelButtonText { get; init; } = "Cancel Operation";

    /// <summary>Optional icon kind override for the badge.</summary>
    public string IconKind { get; init; } = "FilePdfBox";
}
