using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PdfEditorApp.Core.Plugins.Manifests;

/// <summary>
/// Declarative manifest representation parsed from a plugin package's "plugin.json".
/// </summary>
public class PluginManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("entryPoint")]
    public string EntryPoint { get; set; } = "";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "PuzzleOutline";

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();

    [JsonPropertyName("settingsSchema")]
    public Dictionary<string, PluginSettingDefinition> SettingsSchema { get; set; } = new();
}

public class PluginSettingDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string"; // string, boolean, number, select

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("default")]
    public object? DefaultValue { get; set; }

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }
}
