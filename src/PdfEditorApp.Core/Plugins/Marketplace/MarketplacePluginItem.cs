using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Marketplace;

/// <summary>
/// Status of a plugin in the FryPDF Plugin Store / Marketplace.
/// </summary>
public enum MarketplacePluginStatus
{
    Available,
    Installing,
    Installed,
    UpdateAvailable
}

/// <summary>
/// Domain model for an extension or plugin available in the FryPDF Marketplace / Store.
/// Inspired by the VS Code Extensions Gallery item schema.
/// </summary>
public sealed class MarketplacePluginItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Publisher { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }
    public string LongDescription { get; init; } = string.Empty;
    public string Category { get; init; } = "General";
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public double Rating { get; init; } = 4.8;
    public int RatingCount { get; init; } = 120;
    public int InstallCount { get; init; } = 1500;
    public string FormattedSize { get; init; } = "1.2 MB";
    public string IconKind { get; init; } = "PuzzleOutline";
    public string IconColorHex { get; init; } = "#7C3AED";
    public string License { get; init; } = "MIT";
    public bool IsVerified { get; init; } = true;
    public bool IsOfficial { get; init; } = false;
    public string DownloadUrl { get; init; } = string.Empty;
    public IReadOnlyList<string> Highlights { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ContributedFeatures { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();
    public MarketplacePluginStatus Status { get; set; } = MarketplacePluginStatus.Available;
}
