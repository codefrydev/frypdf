using System;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

/// <summary>
/// Service providing centralized retrieval, mutation, and persistence of UI user preferences.
/// </summary>
public interface IUiSettingsService
{
    /// <summary>
    /// Current snapshot of UI preferences.
    /// </summary>
    UiSettingsModel Settings { get; }

    /// <summary>
    /// Event raised whenever any UI preference is modified and saved.
    /// </summary>
    event Action<UiSettingsModel>? SettingsChanged;

    /// <summary>
    /// Save current settings state to disk.
    /// </summary>
    void Save();

    /// <summary>
    /// Mutate settings atomically and persist changes.
    /// </summary>
    void UpdateSettings(Action<UiSettingsModel> updateAction);

    /// <summary>
    /// Reset all settings to application defaults and persist.
    /// </summary>
    void ResetToDefaults();
}
