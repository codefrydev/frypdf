using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

/// <summary>
/// Default implementation of <see cref="IUiSettingsService"/> with JSON persistence.
/// </summary>
public class UiSettingsService : IUiSettingsService
{
    private readonly string _settingsFilePath;
    private UiSettingsModel _settings;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters = { new JsonStringEnumConverter() }
    };

    public UiSettingsModel Settings
    {
        get
        {
            lock (_lock)
            {
                return _settings;
            }
        }
    }

    public event Action<UiSettingsModel>? SettingsChanged;

    public UiSettingsService(string? customSettingsFilePath = null)
    {
        _settingsFilePath = customSettingsFilePath ?? GetDefaultSettingsFilePath();
        _settings = LoadSettings();
    }

    public static string GetDefaultSettingsFilePath()
    {
        return Path.Combine(ThemeService.GetSettingsDirectory(), "ui_settings.json");
    }

    private UiSettingsModel LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<UiSettingsModel>(json, JsonOptions);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Gracefully fall back to defaults if file is corrupt or unreadable
        }

        return new UiSettingsModel();
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                _settings.UpdatedAt = DateTime.UtcNow;
                var dir = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(_settings, JsonOptions);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Never crash the UI on disk write failure
            }
        }

        SettingsChanged?.Invoke(Settings);
    }

    public void UpdateSettings(Action<UiSettingsModel> updateAction)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        lock (_lock)
        {
            updateAction(_settings);
        }

        Save();
    }

    public void ResetToDefaults()
    {
        lock (_lock)
        {
            _settings = new UiSettingsModel();
        }

        Save();
    }
}
