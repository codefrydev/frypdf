using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public interface IThemeService
{
    AppThemeMode CurrentTheme { get; }
    bool IsDarkMode { get; }
    event Action<AppThemeMode>? ThemeChanged;

    void SetTheme(AppThemeMode mode);
    void ToggleTheme();
    void Initialize();
}

public class ThemeService : IThemeService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FryPDF");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "theme_preference.json");

    private AppThemeMode _currentTheme = AppThemeMode.Light;

    public AppThemeMode CurrentTheme => _currentTheme;

    public bool IsDarkMode => _currentTheme == AppThemeMode.Dark ||
        (_currentTheme == AppThemeMode.System && Application.Current?.ActualThemeVariant == ThemeVariant.Dark);

    public event Action<AppThemeMode>? ThemeChanged;

    public ThemeService()
    {
    }

    public void Initialize()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Theme", out var themeProp) &&
                    Enum.TryParse<AppThemeMode>(themeProp.GetString(), true, out var savedTheme))
                {
                    _currentTheme = savedTheme;
                }
            }
        }
        catch
        {
            _currentTheme = AppThemeMode.Light;
        }

        ApplyThemeToApplication(_currentTheme);
    }

    public void SetTheme(AppThemeMode mode)
    {
        if (_currentTheme == mode && Application.Current != null)
        {
            ApplyThemeToApplication(mode);
            return;
        }

        _currentTheme = mode;
        ApplyThemeToApplication(mode);
        SavePreference(mode);
        ThemeChanged?.Invoke(mode);
    }

    public void ToggleTheme()
    {
        var newTheme = IsDarkMode ? AppThemeMode.Light : AppThemeMode.Dark;
        SetTheme(newTheme);
    }

    private void ApplyThemeToApplication(AppThemeMode mode)
    {
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

        if (Dispatcher.UIThread.CheckAccess())
        {
            Apply();
        }
        else
        {
            Dispatcher.UIThread.Post(Apply);
        }
    }

    private void SavePreference(AppThemeMode mode)
    {
        try
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var payload = new { Theme = mode.ToString(), UpdatedAt = DateTime.UtcNow };
            string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Ignore storage permission failures gracefully
        }
    }
}
