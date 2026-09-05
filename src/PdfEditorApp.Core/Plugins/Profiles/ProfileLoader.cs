using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PdfEditorApp.Core.Plugins.Profiles;

/// <summary>
/// Deserializes profile configuration files and mounts active bundles into the <see cref="PluginHost"/>.
/// </summary>
public static class ProfileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Parses a <see cref="PluginProfile"/> from raw JSON text.
    /// </summary>
    public static PluginProfile LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<PluginProfile>(json, JsonOptions)
               ?? throw new JsonException("Failed to deserialize profile JSON.");
    }

    /// <summary>
    /// Alias for <see cref="LoadFromJson"/>.
    /// </summary>
    public static PluginProfile LoadProfileFromJson(string json) => LoadFromJson(json);

    /// <summary>
    /// Reads and parses a <see cref="PluginProfile"/> from a file on disk.
    /// </summary>
    public static PluginProfile LoadFromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Profile configuration file '{filePath}' was not found.");
        }

        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    /// <summary>
    /// Alias for <see cref="LoadFromFile"/>.
    /// </summary>
    public static PluginProfile LoadProfileFromFile(string filePath) => LoadFromFile(filePath);

    /// <summary>
    /// Mounts bundles defined in the profile into the target <see cref="PluginHost"/>, excluding any disabled plugins.
    /// </summary>
    public static void ApplyProfile(
        PluginProfile profile,
        PluginHost host,
        IEnumerable<IFryPluginBundle> availableBundles)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(availableBundles);

        var bundleMap = availableBundles.ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);
        var disabledSet = new HashSet<string>(profile.DisabledPlugins, StringComparer.OrdinalIgnoreCase);

        foreach (var bundleId in profile.Bundles)
        {
            if (bundleMap.TryGetValue(bundleId, out var bundle))
            {
                foreach (var plugin in bundle.Plugins)
                {
                    if (!disabledSet.Contains(plugin.Id))
                    {
                        host.RegisterPlugin(plugin);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Filters available plugins according to the profile and registers & starts them on the host.
    /// </summary>
    public static async Task ApplyToHostAsync(
        PluginHost host,
        PluginProfile profile,
        IEnumerable<IFryPlugin> availablePlugins)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(availablePlugins);

        foreach (var plugin in availablePlugins)
        {
            if (profile.IsPluginEnabled(plugin.Id))
            {
                host.RegisterPlugin(plugin);
            }
        }

        await host.StartAsync();
    }
}
