using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Descriptor defining a canvas element type contributed by a plugin or built-in system.
/// </summary>
public record CanvasElementDescriptor
{
    /// <summary>Unique identifier for this element type, e.g. "frypdf.element.text" or "vendor.element.barcode".</summary>
    public required string ElementTypeId { get; init; }

    /// <summary>Human-readable display name, e.g. "Barcode / QR Code".</summary>
    public required string DisplayName { get; init; }

    /// <summary>The domain model type (must inherit from <see cref="PdfEditorApp.Core.Models.Elements.PdfElementBase"/>).</summary>
    public required Type ModelType { get; init; }

    /// <summary>The presentation ViewModel type for the canvas element.</summary>
    public required Type ViewModelType { get; init; }

    /// <summary>Icon representing this element type in the inserter menu or toolbar.</summary>
    public string IconKind { get; init; } = "ShapeOutline";

    /// <summary>Default initial width in points when placed on canvas.</summary>
    public double DefaultWidth { get; init; } = 150;

    /// <summary>Default initial height in points when placed on canvas.</summary>
    public double DefaultHeight { get; init; } = 100;

    /// <summary>Search tags for element discovery.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>Optional factory delegate to instantiate the ViewModel from an optional model instance.</summary>
    public Func<IServiceProvider, object?, object>? Factory { get; init; }

    /// <summary>Whether this element can be inserted directly from the Ribbon Insert tab / toolbar.</summary>
    public bool CanInsertFromToolbar { get; init; } = true;

    /// <summary>Category grouping in the inserter menu (e.g. "Basic", "Media", "Data", "Forms", "Math").</summary>
    public string InsertionCategory { get; init; } = "Basic";

    /// <summary>Optional single-key shortcut for rapid insertion (e.g. "T", "R", "I").</summary>
    public string? ShortcutKey { get; init; }

    /// <summary>Ordering weight in the inserter menu (lower numbers appear first).</summary>
    public int SortOrder { get; init; } = 100;
}
