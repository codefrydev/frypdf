using System;
using System.Text.Json.Serialization;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Models;

/// <summary>
/// Persisted user interface preferences and customization settings.
/// </summary>
public class UiSettingsModel
{
    // --- Toast / Snackbar Preferences ---

    [JsonPropertyName("toastPosition")]
    public ToastPosition ToastPosition { get; set; } = ToastPosition.BottomCenter;

    [JsonPropertyName("toastStyleVariant")]
    public ToastStyleVariant ToastStyleVariant { get; set; } = ToastStyleVariant.Solid;

    [JsonPropertyName("toastDurationMs")]
    public int ToastDurationMs { get; set; } = 3500;

    [JsonPropertyName("toastShowCloseButton")]
    public bool ToastShowCloseButton { get; set; } = true;

    [JsonPropertyName("toastSoundEnabled")]
    public bool ToastSoundEnabled { get; set; } = false;

    // --- Appearance & Studio Theme ---

    [JsonPropertyName("themeMode")]
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Light;

    [JsonPropertyName("readingTheme")]
    public PdfReaderTheme ReadingTheme { get; set; } = PdfReaderTheme.Default;

    [JsonPropertyName("accentColorHex")]
    public string AccentColorHex { get; set; } = "#0F6CBD";

    // --- Canvas & Document Workspace ---

    [JsonPropertyName("showGridByDefault")]
    public bool ShowGridByDefault { get; set; } = false;

    [JsonPropertyName("snapToGridByDefault")]
    public bool SnapToGridByDefault { get; set; } = false;

    [JsonPropertyName("gridSnapSize")]
    public GridSnapSize GridSnapSize { get; set; } = GridSnapSize.Points20;

    [JsonPropertyName("defaultZoomMode")]
    public PdfViewerZoomMode DefaultZoomMode { get; set; } = PdfViewerZoomMode.FitWidth;

    // --- Layout & Panel Behaviors ---

    [JsonPropertyName("compactRibbonByDefault")]
    public bool CompactRibbonByDefault { get; set; } = false;

    [JsonPropertyName("autoExpandInspectorOnSelect")]
    public bool AutoExpandInspectorOnSelect { get; set; } = true;

    [JsonPropertyName("showShortcutHints")]
    public bool ShowShortcutHints { get; set; } = true;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UiSettingsModel Clone()
    {
        return new UiSettingsModel
        {
            ToastPosition = this.ToastPosition,
            ToastStyleVariant = this.ToastStyleVariant,
            ToastDurationMs = this.ToastDurationMs,
            ToastShowCloseButton = this.ToastShowCloseButton,
            ToastSoundEnabled = this.ToastSoundEnabled,
            ThemeMode = this.ThemeMode,
            ReadingTheme = this.ReadingTheme,
            AccentColorHex = this.AccentColorHex,
            ShowGridByDefault = this.ShowGridByDefault,
            SnapToGridByDefault = this.SnapToGridByDefault,
            GridSnapSize = this.GridSnapSize,
            DefaultZoomMode = this.DefaultZoomMode,
            CompactRibbonByDefault = this.CompactRibbonByDefault,
            AutoExpandInspectorOnSelect = this.AutoExpandInspectorOnSelect,
            ShowShortcutHints = this.ShowShortcutHints,
            UpdatedAt = this.UpdatedAt
        };
    }
}
