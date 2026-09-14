using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Marketplace;

/// <summary>
/// Record representing an installed plugin persisted to disk.
/// Tracks installation timestamp, enabled state, and last known overlay position.
/// </summary>
public class InstalledPluginRecord
{
    public string PluginId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string ActiveVersion { get; set; } = string.Empty;
    public List<string> InstalledVersions { get; set; } = new();
    public string? ActiveDirectoryPath { get; set; }
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    public bool IsEnabled { get; set; } = true;
    public bool WasOverlayOpen { get; set; } = false;
    public double? LastX { get; set; }
    public double? LastY { get; set; }
}
