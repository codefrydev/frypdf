using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

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
public class FilePluginSettingsStore : IPluginSettingsStore, IDisposable
{
    /// <summary>
    /// Quiet period before a <see cref="SetSetting{T}"/> is flushed to disk.
    /// </summary>
    /// <remarks>
    /// Long enough to collapse a continuous gesture into one write, short enough that a crash
    /// loses at most this much. A flush is scheduled once and not restarted by later writes,
    /// so staleness is bounded by this value no matter how fast settings change.
    /// </remarks>
    private const int FlushDelayMs = 500;

    private readonly string _filePath;
    private readonly Dictionary<string, Dictionary<string, object>> _data;
    private readonly object _lock = new();

    /// <summary>Coalesces implicit writes; see <see cref="FlushDelayMs"/>.</summary>
    private readonly Timer _flushTimer;

    /// <summary>1 while a flush is scheduled, so writes coalesce instead of queueing.</summary>
    private int _flushScheduled;

    private bool _isDisposed;

    public FilePluginSettingsStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "plugins.settings.json");
        _data = LoadFromFile();
        _flushTimer = new Timer(_ => FlushPending(), null, Timeout.Infinite, Timeout.Infinite);
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

    /// <summary>
    /// Stores a value, flushing to disk on a short coalescing delay.
    /// </summary>
    /// <remarks>
    /// This used to call <see cref="Save()"/> inline, so every set re-serialized the whole
    /// settings dictionary and did a blocking <c>File.WriteAllText</c> while holding the lock.
    /// Callers that write several keys in one handler multiplied that: the music player's
    /// volume handler sets four keys per change, so dragging the volume slider issued hundreds
    /// of synchronous whole-file writes per second on the UI thread — competing for the same
    /// disk that a plugin's audio thread reads from.
    ///
    /// <see cref="Save()"/> still flushes immediately and synchronously for callers that need
    /// a durability point.
    /// </remarks>
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
        }

        ScheduleFlush();
    }

    /// <summary>
    /// Arms the coalescing flush timer if it is not already armed.
    /// </summary>
    /// <remarks>
    /// Deliberately does not restart an armed timer: a continuous gesture would otherwise keep
    /// pushing the write out and never persist until it stopped.
    /// </remarks>
    private void ScheduleFlush()
    {
        if (_isDisposed) return;
        if (Interlocked.CompareExchange(ref _flushScheduled, 1, 0) != 0) return;

        try
        {
            _flushTimer.Change(FlushDelayMs, Timeout.Infinite);
        }
        catch (ObjectDisposedException)
        {
            Interlocked.Exchange(ref _flushScheduled, 0);
        }
    }

    /// <summary>Timer callback — runs on the thread pool, never the UI thread.</summary>
    private void FlushPending()
    {
        Interlocked.Exchange(ref _flushScheduled, 0);
        Save();
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

    /// <summary>
    /// Flushes pending changes to disk immediately and synchronously.
    /// </summary>
    /// <remarks>
    /// Serialization happens under the lock, but the disk write does not — holding the lock
    /// across I/O made every concurrent reader wait on the filesystem.
    /// </remarks>
    public void Save()
    {
        string json;
        lock (_lock)
        {
            json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
        }

        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Write-then-replace, so an interrupted flush cannot leave a truncated settings
            // file that fails to deserialize on next launch.
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch
        {
            // Silently ignore disk write failures
        }
    }

    /// <summary>
    /// Flushes any pending changes and stops the coalescing timer.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _flushTimer.Dispose();

        // A write may have been armed but not yet fired; losing it on shutdown would silently
        // discard the user's last change.
        if (Interlocked.Exchange(ref _flushScheduled, 0) == 1)
        {
            Save();
        }
    }
}
