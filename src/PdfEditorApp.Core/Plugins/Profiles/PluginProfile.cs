using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfEditorApp.Core.Plugins.Profiles;

/// <summary>
/// Defines an application runtime profile configuration.
/// Mirrors DeepSeek Harness profiles (e.g. 'desktop', 'headless', 'sdk').
/// </summary>
public class PluginProfile
{
    /// <summary>Name of the profile, e.g. "desktop" or "headless".</summary>
    public string ProfileName { get; set; } = "desktop";

    /// <summary>Alias for ProfileName to support standard metadata naming.</summary>
    public string Name
    {
        get => ProfileName;
        set => ProfileName = value;
    }

    /// <summary>Profile version description.</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>Profile summary description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>List of bundle IDs to mount in this profile.</summary>
    public List<string> Bundles { get; set; } = new();

    /// <summary>Optional list of individually enabled plugin IDs.</summary>
    public List<string> EnabledPlugins { get; set; } = new();

    /// <summary>Optional list of individual plugin IDs to disable even if their bundle is included.</summary>
    public List<string> DisabledPlugins { get; set; } = new();

    /// <summary>Profile-specific configuration key-value pairs.</summary>
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Checks whether a plugin is enabled under this profile.</summary>
    public bool IsPluginEnabled(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        if (DisabledPlugins.Any(d => string.Equals(d, pluginId, StringComparison.OrdinalIgnoreCase)))
            return false;
        if (EnabledPlugins.Count > 0)
            return EnabledPlugins.Any(e => string.Equals(e, pluginId, StringComparison.OrdinalIgnoreCase));
        return true;
    }

    /// <summary>Checks whether a bundle is configured in this profile.</summary>
    public bool IsBundleEnabled(string bundleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleId);
        return Bundles.Any(b => string.Equals(b, bundleId, StringComparison.OrdinalIgnoreCase));
    }
}
