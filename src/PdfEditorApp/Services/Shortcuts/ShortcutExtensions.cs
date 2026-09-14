using System;
using Avalonia.Input;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Shortcuts;

/// <summary>
/// Avalonia-specific extension methods for <see cref="IShortcutRegistry"/>.
/// </summary>
public static class ShortcutExtensions
{
    /// <summary>
    /// Attempts to dispatch an Avalonia <see cref="KeyEventArgs"/> through the shortcut registry.
    /// </summary>
    public static bool TryDispatch(this IShortcutRegistry registry, KeyEventArgs e, string? activeContextId = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(e);

        if (registry is ShortcutRegistry concreteRegistry)
        {
            return concreteRegistry.TryDispatch(e, activeContextId);
        }

        var modifiers = ParseKeyModifiers(e.KeyModifiers);

        var keyName = e.Key.ToString();
        var handled = registry.TryDispatch(keyName, modifiers, activeContextId);
        if (handled)
        {
            e.Handled = true;
        }
        return handled;
    }

    /// <summary>
    /// Translates Avalonia KeyModifiers into platform-neutral ShortcutModifiers.
    /// </summary>
    public static ShortcutModifiers ParseKeyModifiers(KeyModifiers keyModifiers)
    {
        var modifiers = ShortcutModifiers.None;
        if (keyModifiers.HasFlag(KeyModifiers.Control)) modifiers |= ShortcutModifiers.Control;
        if (keyModifiers.HasFlag(KeyModifiers.Shift)) modifiers |= ShortcutModifiers.Shift;
        if (keyModifiers.HasFlag(KeyModifiers.Alt)) modifiers |= ShortcutModifiers.Alt;
        if (keyModifiers.HasFlag(KeyModifiers.Meta)) modifiers |= ShortcutModifiers.Meta;
        return modifiers;
    }
}
