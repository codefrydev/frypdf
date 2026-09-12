using PdfEditorApp.Core.Plugins.Loading;

namespace PdfEditorApp.Messages;

/// <summary>Message requesting the display of the full-screen loading progress overlay.</summary>
public record ShowLoadingProgressMessage(LoadingProgressOptions Options);

/// <summary>Message requesting a live status/progress update on the active loading overlay.</summary>
public record UpdateLoadingProgressMessage(string StatusMessage, double? ProgressPercent = null, int? ActivePhaseIndex = null);

/// <summary>Message requesting that the loading overlay be dismissed.</summary>
public record HideLoadingProgressMessage();

/// <summary>Message requesting cancellation of the currently running operation.</summary>
public record CancelLoadingProgressMessage();
