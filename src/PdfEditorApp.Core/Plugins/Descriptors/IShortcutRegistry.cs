using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Central registry for managing, customizing, and dispatching keyboard shortcuts across FryPDF and its plugins.
/// </summary>
public interface IShortcutRegistry
{
    /// <summary>
    /// Registers a shortcut descriptor into the registry.
    /// </summary>
    IDisposable RegisterShortcut(ShortcutDescriptor descriptor);

    /// <summary>
    /// Unregisters a shortcut descriptor by its unique ID.
    /// </summary>
    bool UnregisterShortcut(string shortcutId);

    /// <summary>
    /// Gets all registered shortcuts.
    /// </summary>
    IReadOnlyList<ShortcutDescriptor> GetAllShortcuts();

    /// <summary>
    /// Gets a single registered shortcut by its unique ID.
    /// </summary>
    ShortcutDescriptor? GetShortcut(string shortcutId);

    /// <summary>
    /// Gets all registered shortcuts matching a given category.
    /// </summary>
    IReadOnlyList<ShortcutDescriptor> GetShortcutsByCategory(string category);

    /// <summary>
    /// Gets the current effective gesture for a shortcut ID (user custom override if set, else platform-appropriate default).
    /// </summary>
    string GetEffectiveGesture(string shortcutId);

    /// <summary>
    /// Assigns a custom key gesture to a shortcut, or passes null to restore its default.
    /// </summary>
    void SetCustomGesture(string shortcutId, string? newGesture);

    /// <summary>
    /// Resets a specific shortcut to its developer-defined default gesture.
    /// </summary>
    void ResetToDefault(string shortcutId);

    /// <summary>
    /// Resets all shortcuts to their developer-defined defaults.
    /// </summary>
    void ResetAllToDefaults();

    /// <summary>
    /// Attempts to dispatch a keystroke with platform-agnostic arguments.
    /// </summary>
    bool TryDispatch(string key, ShortcutModifiers modifiers, string? activeContextId = null);

    /// <summary>
    /// Triggered whenever shortcuts are registered, unregistered, or customized.
    /// </summary>
    event Action? ShortcutsChanged;
}
