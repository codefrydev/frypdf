using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PdfEditorApp.Models;

/// <summary>
/// Represents a third-party library, package, tool, or engine utilized by FryPDF,
/// along with its license type, repository/website link, maintainer information, and full license text.
/// </summary>
public partial class ThirdPartyToolLicense : ObservableObject
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string LicenseType { get; init; }
    public required string Category { get; init; }
    public required string Purpose { get; init; }
    public required string Maintainer { get; init; }
    public required string ProjectUrl { get; init; }
    public required string LicenseText { get; init; }
    public required string AccentColorHex { get; init; }
    public required string IconKind { get; init; }

    [ObservableProperty]
    private bool _isExpanded;

    public string BadgeColorHex => LicenseType.ToUpperInvariant() switch
    {
        var l when l.Contains("MIT") => "#10B981",       // Green
        var l when l.Contains("APACHE") => "#0284C7",   // Blue
        var l when l.Contains("COMMUNITY") => "#8B5CF6", // Purple
        var l when l.Contains("MICROSOFT") => "#0078D4", // Microsoft Blue
        var l when l.Contains("OFL") || l.Contains("SIL") => "#D946EF", // Fuchsia / Pink
        var l when l.Contains("UBUNTU") => "#EA580C",   // Ubuntu Orange
        _ => "#6366F1"
    };

    public string BadgeBackgroundHex => LicenseType.ToUpperInvariant() switch
    {
        var l when l.Contains("MIT") => "#ECFDF5",
        var l when l.Contains("APACHE") => "#F0F9FF",
        var l when l.Contains("COMMUNITY") => "#F5F3FF",
        var l when l.Contains("MICROSOFT") => "#EFF6FF",
        var l when l.Contains("OFL") || l.Contains("SIL") => "#FDF4FF",
        var l when l.Contains("UBUNTU") => "#FFF7ED",
        _ => "#EEF2FF"
    };
}
