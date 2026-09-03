using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public interface IThemeService
{
    AppThemeMode CurrentTheme { get; }
    PdfReaderTheme ReadingTheme { get; }
    bool IsDarkMode { get; }
    event Action<AppThemeMode>? ThemeChanged;
    event Action<PdfReaderTheme>? ReadingThemeChanged;

    void SetTheme(AppThemeMode mode);
    void ToggleTheme();
    void SetReadingTheme(PdfReaderTheme theme);
    void Initialize();
}

public class ThemePreferenceData
{
    [JsonPropertyName("Theme")]
    public string? Theme { get; set; }

    [JsonPropertyName("ReadingTheme")]
    public string? ReadingTheme { get; set; }

    [JsonPropertyName("IsDarkMode")]
    public bool? IsDarkMode { get; set; }

    [JsonPropertyName("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class ThemeService : IThemeService
{
    private AppThemeMode _currentTheme = AppThemeMode.Light;
    private PdfReaderTheme _readingTheme = PdfReaderTheme.Default;
    private bool _isHookedToActualThemeVariant = false;

    public AppThemeMode CurrentTheme => _currentTheme;
    public PdfReaderTheme ReadingTheme => _readingTheme;

    public bool IsDarkMode => _currentTheme == AppThemeMode.Dark ||
        (_currentTheme == AppThemeMode.System && Application.Current?.ActualThemeVariant == ThemeVariant.Dark);

    public event Action<AppThemeMode>? ThemeChanged;
    public event Action<PdfReaderTheme>? ReadingThemeChanged;

    public ThemeService()
    {
        Initialize();
    }

    public static string GetSettingsDirectory()
    {
        string? appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Path.GetTempPath();
        }

        var dir = Path.Combine(appData, "FryPDF");
        try
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch
        {
            dir = Path.Combine(Path.GetTempPath(), "FryPDF");
            try { Directory.CreateDirectory(dir); } catch { /* Ignore */ }
        }

        return dir;
    }

    public static string GetSettingsFilePath()
    {
        return Path.Combine(GetSettingsDirectory(), "theme_preference.json");
    }

    public void Initialize()
    {
        try
        {
            var filePath = GetSettingsFilePath();

            // Check primary path, then fallback to LocalApplicationData if different
            if (!File.Exists(filePath))
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrWhiteSpace(localAppData))
                {
                    var alternatePath = Path.Combine(localAppData, "FryPDF", "theme_preference.json");
                    if (File.Exists(alternatePath))
                    {
                        filePath = alternatePath;
                    }
                }
            }

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                bool loaded = false;
                try
                {
                    var data = JsonSerializer.Deserialize<ThemePreferenceData>(json, options);
                    if (data != null)
                    {
                        if (!string.IsNullOrWhiteSpace(data.Theme) &&
                            Enum.TryParse<AppThemeMode>(data.Theme, true, out var parsedTheme))
                        {
                            _currentTheme = parsedTheme;
                            loaded = true;
                        }
                        else if (data.IsDarkMode.HasValue)
                        {
                            _currentTheme = data.IsDarkMode.Value ? AppThemeMode.Dark : AppThemeMode.Light;
                            loaded = true;
                        }

                        if (!string.IsNullOrWhiteSpace(data.ReadingTheme) &&
                            Enum.TryParse<PdfReaderTheme>(data.ReadingTheme, true, out var parsedReadingTheme))
                        {
                            _readingTheme = parsedReadingTheme;
                        }
                    }
                }
                catch
                {
                    // Fallback to manual parsing below
                }

                if (!loaded)
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.NameEquals("Theme") || prop.NameEquals("theme") || 
                            prop.NameEquals("ThemeMode") || prop.NameEquals("themeMode"))
                        {
                            if (prop.Value.ValueKind == JsonValueKind.String &&
                                Enum.TryParse<AppThemeMode>(prop.Value.GetString(), true, out var t))
                            {
                                _currentTheme = t;
                            }
                            else if (prop.Value.ValueKind == JsonValueKind.Number &&
                                     prop.Value.TryGetInt32(out var intVal) &&
                                     Enum.IsDefined(typeof(AppThemeMode), intVal))
                            {
                                _currentTheme = (AppThemeMode)intVal;
                            }
                        }
                        else if (prop.NameEquals("ReadingTheme") || prop.NameEquals("readingTheme") ||
                                 prop.NameEquals("ReaderTheme") || prop.NameEquals("readerTheme"))
                        {
                            if (prop.Value.ValueKind == JsonValueKind.String &&
                                Enum.TryParse<PdfReaderTheme>(prop.Value.GetString(), true, out var rt))
                            {
                                _readingTheme = rt;
                            }
                            else if (prop.Value.ValueKind == JsonValueKind.Number &&
                                     prop.Value.TryGetInt32(out var intVal) &&
                                     Enum.IsDefined(typeof(PdfReaderTheme), intVal))
                            {
                                _readingTheme = (PdfReaderTheme)intVal;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            _currentTheme = AppThemeMode.Light;
            _readingTheme = PdfReaderTheme.Default;
        }

        ApplyThemeToApplication(_currentTheme);
        HookActualThemeVariant();
        ThemeChanged?.Invoke(_currentTheme);
        ReadingThemeChanged?.Invoke(_readingTheme);
    }

    public void SetTheme(AppThemeMode mode)
    {
        _currentTheme = mode;
        ApplyThemeToApplication(mode);
        SavePreference();
        ThemeChanged?.Invoke(mode);
    }

    public void SetReadingTheme(PdfReaderTheme theme)
    {
        _readingTheme = theme;
        SavePreference();
        ReadingThemeChanged?.Invoke(theme);
    }

    public void ToggleTheme()
    {
        var newTheme = IsDarkMode ? AppThemeMode.Light : AppThemeMode.Dark;
        SetTheme(newTheme);
    }

    private void ApplyThemeToApplication(AppThemeMode mode)
    {
        if (Application.Current == null) return;

        void Apply()
        {
            if (Application.Current == null) return;

            Application.Current.RequestedThemeVariant = mode switch
            {
                AppThemeMode.Dark => ThemeVariant.Dark,
                AppThemeMode.Light => ThemeVariant.Light,
                AppThemeMode.System => ThemeVariant.Default,
                _ => ThemeVariant.Light
            };
        }

        try
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Apply();
            }
            else
            {
                Dispatcher.UIThread.Post(Apply);
            }
        }
        catch
        {
            // Headless unit test environment without active dispatcher loop
        }
    }

    private void HookActualThemeVariant()
    {
        if (_isHookedToActualThemeVariant || Application.Current == null) return;

        try
        {
            Application.Current.ActualThemeVariantChanged += (sender, args) =>
            {
                if (_currentTheme == AppThemeMode.System)
                {
                    ThemeChanged?.Invoke(_currentTheme);
                }
            };
            _isHookedToActualThemeVariant = true;
        }
        catch
        {
            // Ignore if platform doesn't support event
        }
    }

    private void SavePreference()
    {
        try
        {
            var dir = GetSettingsDirectory();
            var filePath = Path.Combine(dir, "theme_preference.json");
            var payload = new ThemePreferenceData
            {
                Theme = _currentTheme.ToString(),
                ReadingTheme = _readingTheme.ToString(),
                IsDarkMode = IsDarkMode,
                UpdatedAt = DateTime.UtcNow
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(payload, options);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Ignore storage permission failures gracefully
        }
    }
}
