using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfEditorApp.Core.Plugins.Marketplace;

/// <summary>
/// Model representing a specific release version of a plugin published in the catalog.
/// Enables multi-version history, targeted rollbacks, and host compatibility validation.
/// </summary>
public sealed partial class MarketplacePluginVersion : ObservableObject
{
    public required string Version { get; init; }
    public string ReleaseDate { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
    public string FormattedSize { get; init; } = "1.0 MB";
    public string MinHostVersion { get; init; } = "1.0.0";
    public string MaxHostVersion { get; init; } = string.Empty;
    public string TargetFramework { get; init; } = "net10.0";
    public string ReleaseNotes { get; init; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstalledAndNotActive))]
    private bool _isInstalled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstalledAndNotActive))]
    private bool _isActive;

    public bool IsInstalledAndNotActive => IsInstalled && !IsActive;

    [ObservableProperty]
    private bool _isCompatible = true;

    [ObservableProperty]
    private string _compatibilityNote = string.Empty;

    public override string ToString() => $"v{Version}";
}
