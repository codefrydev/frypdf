using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PdfEditorApp.Core.Plugins.Settings;

/// <summary>
/// Contract for persisting and retrieving user configurations for plugins.
/// </summary>
public interface IPluginSettingsStore
{
    T GetSetting<T>(string pluginId, string key, T defaultValue);
    void SetSetting<T>(string pluginId, string key, T value);
    Dictionary<string, object> GetPluginSettings(string pluginId);
    void Save();
}

/// <summary>
/// File-backed persistent settings store saving to "plugins.settings.json".
/// </summary>
public class FilePluginSettingsStore : IPluginSettingsStore
{
    private readonly string _filePath;
    private readonly Dictionary<string, Dictionary<string, object>> _data;
    private readonly object _lock = new();

    public FilePluginSettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "plugins.settings.json");
        _data = LoadFromFile();
    }

    private Dictionary<string, Dictionary<string, object>> LoadFromFile()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json) ?? new();
            }
        }
        catch
        {
            // Fall back to clean state on corruption
        }
        return new();
    }

    public T GetSetting<T>(string pluginId, string key, T defaultValue)
    {
        lock (_lock)
        {
            if (_data.TryGetValue(pluginId, out var dict) && dict.TryGetValue(key, out var val))
            {
                if (val is JsonElement elem)
                {
                    return JsonSerializer.Deserialize<T>(elem.GetRawText()) ?? defaultValue;
                }
                if (val is T typedVal)
                {
                    return typedVal;
                }
            }
            return defaultValue;
        }
    }

    public void SetSetting<T>(string pluginId, string key, T value)
    {
        lock (_lock)
        {
            if (!_data.TryGetValue(pluginId, out var dict))
            {
                dict = new();
                _data[pluginId] = dict;
            }
            dict[key] = value!;
            Save();
        }
    }

    public Dictionary<string, object> GetPluginSettings(string pluginId)
    {
        lock (_lock)
        {
            if (_data.TryGetValue(pluginId, out var dict))
            {
                return new(dict);
            }
            return new();
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Silently ignore disk write failures
            }
        }
    }
}
