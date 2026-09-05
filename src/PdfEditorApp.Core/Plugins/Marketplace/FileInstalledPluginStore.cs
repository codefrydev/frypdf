using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PdfEditorApp.Core.Plugins.Marketplace;

/// <summary>
/// File-backed persistent store for installed plugin history saving to "installed_plugins.json".
/// </summary>
public class FileInstalledPluginStore : IInstalledPluginStore
{
    private readonly string _filePath;
    private readonly Dictionary<string, InstalledPluginRecord> _records = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public FileInstalledPluginStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "installed_plugins.json");
        LoadFromFile();
    }

    private void LoadFromFile()
    {
        lock (_lock)
        {
            _records.Clear();
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var list = JsonSerializer.Deserialize<List<InstalledPluginRecord>>(json, JsonOptions);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            if (!string.IsNullOrWhiteSpace(item.PluginId))
                            {
                                _records[item.PluginId] = item;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileInstalledPluginStore] Load error: {ex.Message}");
            }
        }
    }

    public IReadOnlyList<InstalledPluginRecord> GetAll()
    {
        lock (_lock)
        {
            return _records.Values.OrderByDescending(r => r.InstalledAt).ToList();
        }
    }

    public InstalledPluginRecord? Get(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return null;

        lock (_lock)
        {
            return _records.TryGetValue(pluginId, out var record) ? record : null;
        }
    }

    public bool IsInstalled(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return false;

        lock (_lock)
        {
            return _records.ContainsKey(pluginId);
        }
    }

    public void AddOrUpdate(InstalledPluginRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (string.IsNullOrWhiteSpace(record.PluginId)) return;

        lock (_lock)
        {
            _records[record.PluginId] = record;
            SaveInternal();
        }
    }

    public bool Remove(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return false;

        lock (_lock)
        {
            if (_records.Remove(pluginId))
            {
                SaveInternal();
                return true;
            }
            return false;
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            SaveInternal();
        }
    }

    private void SaveInternal()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var list = _records.Values.OrderByDescending(r => r.InstalledAt).ToList();
            var json = JsonSerializer.Serialize(list, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FileInstalledPluginStore] Save error: {ex.Message}");
        }
    }
}
