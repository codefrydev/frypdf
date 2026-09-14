using System;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Platform-agnostic modifier flags for keyboard combinations.
/// </summary>
[Flags]
public enum ShortcutModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Meta = 8
}
