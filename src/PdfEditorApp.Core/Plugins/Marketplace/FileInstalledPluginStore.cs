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

    public void UpdateOverlayState(string pluginId, bool wasOverlayOpen, double? lastX = null, double? lastY = null)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return;

        lock (_lock)
        {
            if (_records.TryGetValue(pluginId, out var existing))
            {
                existing.WasOverlayOpen = wasOverlayOpen;
                if (lastX.HasValue) existing.LastX = lastX.Value;
                if (lastY.HasValue) existing.LastY = lastY.Value;
                SaveInternal();
            }
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

    /// <summary>
    /// Serializes the registry through a temporary file and swaps it into place.
    /// </summary>
    /// <remarks>
    /// This used to write straight over installed_plugins.json. A failure partway through
    /// left a truncated registry — losing the record of every installed plugin — while
    /// AddOrUpdate/Remove still reported success. The write is now atomic, and a failure
    /// leaves the previous registry intact.
    /// </remarks>
    private void SaveInternal()
    {
        string? tempPath = null;
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var list = _records.Values.OrderByDescending(r => r.InstalledAt).ToList();
            var json = JsonSerializer.Serialize(list, JsonOptions);

            tempPath = _filePath + $".{Guid.NewGuid():N}.tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(_filePath))
            {
                File.Replace(tempPath, _filePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, _filePath);
            }

            tempPath = null;
        }
        catch (Exception ex)
        {
            // Debug.WriteLine is captured process-wide by AppLogService's trace listener, which
            // this Core project cannot reference directly.
            System.Diagnostics.Debug.WriteLine(
                $"[FileInstalledPluginStore] Failed to persist '{_filePath}'; the previous registry is unchanged: {ex}");
        }
        finally
        {
            if (tempPath != null)
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* best effort */ }
            }
        }
    }
}
