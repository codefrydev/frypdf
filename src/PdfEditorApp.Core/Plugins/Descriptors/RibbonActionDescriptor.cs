using System;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Descriptor defining a Ribbon action button or menu item contributed dynamically by a plugin.
/// </summary>
public record RibbonActionDescriptor
{
    /// <summary>Unique identifier, e.g. "ribbon.action.ocr" or "ribbon.action.stamp".</summary>
    public required string Id { get; init; }

    /// <summary>Target Ribbon Tab, e.g. "Home", "Tools", "Insert", "Security", "View".</summary>
    public required string TabId { get; init; }

    /// <summary>Target Group inside the tab, e.g. "Organize", "AI Studio", "Annotations".</summary>
    public required string GroupId { get; init; }

    /// <summary>Display label on the button or menu item.</summary>
    public required string Label { get; init; }

    /// <summary>Optional descriptive tooltip.</summary>
    public string? Tooltip { get; init; }

    /// <summary>Material Design / vector icon identifier.</summary>
    public required string IconKind { get; init; }

    /// <summary>Display sort order within the group.</summary>
    public int Order { get; init; } = 100;

    /// <summary>Optional action delegate to execute when clicked.</summary>
    public Action<IServiceProvider>? Action { get; init; }
}
