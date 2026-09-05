using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Metadata descriptor defining a PDF tool contributed by a plugin.
/// </summary>
public record PdfToolDescriptor
{
    /// <summary>Unique string identifier, e.g. "frypdf.tool.merge" or "vendor.tool.stamp".</summary>
    public required string Id { get; init; }

    /// <summary>Optional integer or legacy enum tag for backwards compatibility with legacy UI.</summary>
    public int? LegacyId { get; init; }

    /// <summary>Human-readable display title, e.g. "Merge PDF".</summary>
    public required string Name { get; init; }

    /// <summary>Descriptive summary explaining the tool's capabilities.</summary>
    public required string Description { get; init; }

    /// <summary>Display category grouping, e.g. "Organize & Page", "Security", "Conversion", "Intelligence".</summary>
    public required string Category { get; init; }

    /// <summary>Material Design / vector icon identifier.</summary>
    public required string IconKind { get; init; }

    /// <summary>Brand or accent hex color for the icon, e.g. "#EA580C".</summary>
    public string IconColorHex { get; init; } = "#2563EB";

    /// <summary>Subtle tonal background container hex color, e.g. "#FFF7ED".</summary>
    public string BackgroundAccentHex { get; init; } = "#EFF6FF";

    /// <summary>Whether this tool supports bulk multi-file processing.</summary>
    public bool SupportsMultiFile { get; init; }

    /// <summary>Comma-separated file extensions accepted by this tool, e.g. ".pdf".</summary>
    public string AcceptedFileExtensions { get; init; } = ".pdf";

    /// <summary>Search tags and keywords for quick discovery.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Factory delegate to instantiate the tool's execution ViewModel given a service provider.
    /// </summary>
    public Func<IServiceProvider, object>? CreateViewModel { get; init; }

    /// <summary>Arbitrary metadata properties.</summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
