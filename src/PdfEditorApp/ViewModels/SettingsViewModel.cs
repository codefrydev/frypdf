using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel managing user UI customization preferences, notification placements, and workspace behaviors.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IUiSettingsService _uiSettingsService;
    private readonly IThemeService? _themeService;

    public event Action<string, ToastNotificationType, string?>? TriggerToastRequested;

    [ObservableProperty]
    private ToastPosition _toastPosition;

    [ObservableProperty]
    private ToastStyleVariant _toastStyleVariant;

    [ObservableProperty]
    private int _toastDurationMs;

    [ObservableProperty]
    private bool _toastShowCloseButton;

    [ObservableProperty]
    private bool _toastSoundEnabled;

    [ObservableProperty]
    private AppThemeMode _themeMode;

    [ObservableProperty]
    private PdfReaderTheme _readingTheme;

    [ObservableProperty]
    private bool _showGridByDefault;

    [ObservableProperty]
    private bool _snapToGridByDefault;

    [ObservableProperty]
    private GridSnapSize _gridSnapSize;

    [ObservableProperty]
    private PdfViewerZoomMode _defaultZoomMode;

    [ObservableProperty]
    private bool _compactRibbonByDefault;

    [ObservableProperty]
    private bool _autoExpandInspectorOnSelect;

    [ObservableProperty]
    private bool _showShortcutHints;

    // --- Interactive Live Preview State ---

    [ObservableProperty]
    private ToastNotificationType _previewToastType = ToastNotificationType.Primary;

    [ObservableProperty]
    private string _previewToastMessage = "Primary message comes here";

    [ObservableProperty]
    private string _previewToastIcon = "InformationOutline";

    public bool PreviewIsSolid => ToastStyleVariant switch
    {
        ToastStyleVariant.Subtle => false,
        ToastStyleVariant.Solid => true,
        ToastStyleVariant.Auto => _themeService?.IsDarkMode ?? false,
        _ => true
    };

    public IBrush PreviewBackgroundBrush
    {
        get
        {
            if (PreviewIsSolid)
            {
                return PreviewToastType switch
                {
                    ToastNotificationType.Primary => Brush("#0F6CBD"),
                    ToastNotificationType.Success => Brush("#15803D"),
                    ToastNotificationType.Danger => Brush("#DC2626"),
                    ToastNotificationType.Warning => Brush("#D97706"),
                    ToastNotificationType.General => Brush("#1E293B"),
                    _ => Brush("#1E293B")
                };
            }

            return (_themeService?.IsDarkMode == true) ? PreviewToastType switch
            {
                ToastNotificationType.Primary => Brush("#172554"),
                ToastNotificationType.Success => Brush("#052E16"),
                ToastNotificationType.Danger => Brush("#450A0A"),
                ToastNotificationType.Warning => Brush("#451A03"),
                ToastNotificationType.General => Brush("#18181B"),
                _ => Brush("#18181B")
            } : PreviewToastType switch
            {
                ToastNotificationType.Primary => Brush("#EFF6FF"),
                ToastNotificationType.Success => Brush("#F0FDF4"),
                ToastNotificationType.Danger => Brush("#FEF2F2"),
                ToastNotificationType.Warning => Brush("#FFFBEB"),
                ToastNotificationType.General => Brush("#F8FAFC"),
                _ => Brush("#F8FAFC")
            };
        }
    }

    public IBrush PreviewForegroundBrush
    {
        get
        {
            if (PreviewIsSolid) return WhiteBrush;

            return (_themeService?.IsDarkMode == true) ? PreviewToastType switch
            {
                ToastNotificationType.Primary => Brush("#93C5FD"),
                ToastNotificationType.Success => Brush("#86EFAC"),
                ToastNotificationType.Danger => Brush("#FCA5A5"),
                ToastNotificationType.Warning => Brush("#FCD34D"),
                ToastNotificationType.General => Brush("#E2E8F0"),
                _ => Brush("#E2E8F0")
            } : PreviewToastType switch
            {
                ToastNotificationType.Primary => Brush("#1D4ED8"),
                ToastNotificationType.Success => Brush("#15803D"),
                ToastNotificationType.Danger => Brush("#B91C1C"),
                ToastNotificationType.Warning => Brush("#B45309"),
                ToastNotificationType.General => Brush("#334155"),
                _ => Brush("#334155")
            };
        }
    }

    public IBrush PreviewBorderBrush
    {
        get
        {
            if (PreviewIsSolid)
            {
                return PreviewToastType switch
                {
                    ToastNotificationType.Primary => Brush("#0D5CA0"),
                    ToastNotificationType.Success => Brush("#166534"),
                    ToastNotificationType.Danger => Brush("#B91C1C"),
                    ToastNotificationType.Warning => Brush("#B45309"),
                    ToastNotificationType.General => Brush("#0F172A"),
                    _ => Brush("#0F172A")
                };
            }

            return (_themeService?.IsDarkMode == true) ? PreviewToastType switch
            {
                ToastNotificationType.Primary => Brush("#1E40AF"),
                ToastNotificationType.Success => Brush("#166534"),
                ToastNotificationType.Danger => Brush("#991B1B"),
                ToastNotificationType.Warning => Brush("#92400E"),
                ToastNotificationType.General => Brush("#3F3F46"),
                _ => Brush("#3F3F46")
            } : PreviewToastType switch
            {
                ToastNotificationType.Primary => Brush("#BFDBFE"),
                ToastNotificationType.Success => Brush("#BBF7D0"),
                ToastNotificationType.Danger => Brush("#FECACA"),
                ToastNotificationType.Warning => Brush("#FDE68A"),
                ToastNotificationType.General => Brush("#CBD5E1"),
                _ => Brush("#CBD5E1")
            };
        }
    }

    public IBrush PreviewIconBrush => PreviewForegroundBrush;
    public IBrush PreviewCloseBrush => PreviewIsSolid ? Brush("#CCFFFFFF") : PreviewForegroundBrush;

    private static readonly SolidColorBrush WhiteBrush = new(Color.Parse("#FFFFFF"));
    private static SolidColorBrush Brush(string hex) => new(Color.Parse(hex));

    public void RefreshPreview()
    {
        OnPropertyChanged(nameof(PreviewIsSolid));
        OnPropertyChanged(nameof(PreviewBackgroundBrush));
        OnPropertyChanged(nameof(PreviewForegroundBrush));
        OnPropertyChanged(nameof(PreviewBorderBrush));
        OnPropertyChanged(nameof(PreviewIconBrush));
        OnPropertyChanged(nameof(PreviewCloseBrush));
    }

    public SettingsViewModel() : this(new UiSettingsService(), new ThemeService())
    {
    }

    public SettingsViewModel(IUiSettingsService uiSettingsService, IThemeService? themeService = null)
    {
        _uiSettingsService = uiSettingsService;
        _themeService = themeService;

        LoadFromSettings(_uiSettingsService.Settings);
        _uiSettingsService.SettingsChanged += LoadFromSettings;

        if (_themeService != null)
        {
            _themeService.ThemeChanged += (_) => RefreshPreview();
        }
    }

    private void LoadFromSettings(UiSettingsModel s)
    {
        ToastPosition = s.ToastPosition;
        ToastStyleVariant = s.ToastStyleVariant;
        ToastDurationMs = s.ToastDurationMs;
        ToastShowCloseButton = s.ToastShowCloseButton;
        ToastSoundEnabled = s.ToastSoundEnabled;
        ThemeMode = s.ThemeMode;
        ReadingTheme = s.ReadingTheme;
        ShowGridByDefault = s.ShowGridByDefault;
        SnapToGridByDefault = s.SnapToGridByDefault;
        GridSnapSize = s.GridSnapSize;
        DefaultZoomMode = s.DefaultZoomMode;
        CompactRibbonByDefault = s.CompactRibbonByDefault;
        AutoExpandInspectorOnSelect = s.AutoExpandInspectorOnSelect;
        ShowShortcutHints = s.ShowShortcutHints;
        RefreshPreview();
    }

    // --- Position Selection Commands ---

    [RelayCommand]
    public void SetToastPosition(object? param)
    {
        ToastPosition pos;
        if (param is ToastPosition p) pos = p;
        else if (param is string s && Enum.TryParse<ToastPosition>(s, true, out var parsed)) pos = parsed;
        else return;

        ToastPosition = pos;
        _uiSettingsService.UpdateSettings(s => s.ToastPosition = pos);
        TriggerToastRequested?.Invoke($"Notification placement set to {GetPositionName(pos)}", ToastNotificationType.Primary, "DockBottom");
        RefreshPreview();
    }

    [RelayCommand]
    public void SetToastStyleVariant(object? param)
    {
        ToastStyleVariant variant;
        if (param is ToastStyleVariant v) variant = v;
        else if (param is string s && Enum.TryParse<ToastStyleVariant>(s, true, out var parsed)) variant = parsed;
        else return;

        ToastStyleVariant = variant;
        _uiSettingsService.UpdateSettings(s => s.ToastStyleVariant = variant);
        TriggerToastRequested?.Invoke($"Snackbar visual style changed to {variant}", ToastNotificationType.Primary, "PaletteOutline");
        RefreshPreview();
    }

    [RelayCommand]
    public void SetToastDuration(object? param)
    {
        int durationMs;
        if (param is int d) durationMs = d;
        else if (param is string s && int.TryParse(s, out var parsed)) durationMs = parsed;
        else return;

        ToastDurationMs = durationMs;
        _uiSettingsService.UpdateSettings(s => s.ToastDurationMs = durationMs);
        string durLabel = durationMs > 0 ? $"{durationMs / 1000.0:0.#} seconds" : "Manual Close Only";
        TriggerToastRequested?.Invoke($"Notification timeout set to {durLabel}", ToastNotificationType.General, "ClockOutline");
    }

    [RelayCommand]
    public void ToggleToastCloseButton()
    {
        ToastShowCloseButton = !ToastShowCloseButton;
        _uiSettingsService.UpdateSettings(s => s.ToastShowCloseButton = ToastShowCloseButton);
        RefreshPreview();
    }

    [RelayCommand]
    public void ToggleToastSound()
    {
        ToastSoundEnabled = !ToastSoundEnabled;
        _uiSettingsService.UpdateSettings(s => s.ToastSoundEnabled = ToastSoundEnabled);
    }

    [RelayCommand]
    public void SetThemeMode(object? param)
    {
        AppThemeMode mode;
        if (param is AppThemeMode m) mode = m;
        else if (param is string s && Enum.TryParse<AppThemeMode>(s, true, out var parsed)) mode = parsed;
        else return;

        ThemeMode = mode;
        _uiSettingsService.UpdateSettings(s => s.ThemeMode = mode);
        _themeService?.SetTheme(mode);
        RefreshPreview();
    }

    [RelayCommand]
    public void SetReadingTheme(object? param)
    {
        PdfReaderTheme theme;
        if (param is PdfReaderTheme t) theme = t;
        else if (param is string s && Enum.TryParse<PdfReaderTheme>(s, true, out var parsed)) theme = parsed;
        else return;

        ReadingTheme = theme;
        _uiSettingsService.UpdateSettings(s => s.ReadingTheme = theme);
        _themeService?.SetReadingTheme(theme);
    }

    [RelayCommand]
    public void ToggleGridByDefault()
    {
        ShowGridByDefault = !ShowGridByDefault;
        _uiSettingsService.UpdateSettings(s => s.ShowGridByDefault = ShowGridByDefault);
    }

    [RelayCommand]
    public void ToggleSnapByDefault()
    {
        SnapToGridByDefault = !SnapToGridByDefault;
        _uiSettingsService.UpdateSettings(s => s.SnapToGridByDefault = SnapToGridByDefault);
    }

    [RelayCommand]
    public void SetGridSnapSize(object? param)
    {
        GridSnapSize size;
        if (param is GridSnapSize g) size = g;
        else if (param is string s && Enum.TryParse<GridSnapSize>(s, true, out var parsed)) size = parsed;
        else return;

        GridSnapSize = size;
        _uiSettingsService.UpdateSettings(s => s.GridSnapSize = size);
    }

    [RelayCommand]
    public void SetDefaultZoomMode(object? param)
    {
        PdfViewerZoomMode mode;
        if (param is PdfViewerZoomMode m) mode = m;
        else if (param is string s && Enum.TryParse<PdfViewerZoomMode>(s, true, out var parsed)) mode = parsed;
        else return;

        DefaultZoomMode = mode;
        _uiSettingsService.UpdateSettings(s => s.DefaultZoomMode = mode);
    }

    [RelayCommand]
    public void ToggleCompactRibbon()
    {
        CompactRibbonByDefault = !CompactRibbonByDefault;
        _uiSettingsService.UpdateSettings(s => s.CompactRibbonByDefault = CompactRibbonByDefault);
    }

    [RelayCommand]
    public void ToggleAutoExpandInspector()
    {
        AutoExpandInspectorOnSelect = !AutoExpandInspectorOnSelect;
        _uiSettingsService.UpdateSettings(s => s.AutoExpandInspectorOnSelect = AutoExpandInspectorOnSelect);
    }

    [RelayCommand]
    public void ToggleShortcutHints()
    {
        ShowShortcutHints = !ShowShortcutHints;
        _uiSettingsService.UpdateSettings(s => s.ShowShortcutHints = ShowShortcutHints);
    }

    [RelayCommand]
    public void ResetAllDefaults()
    {
        _uiSettingsService.ResetToDefaults();
        LoadFromSettings(_uiSettingsService.Settings);
        RefreshPreview();
        TriggerToastRequested?.Invoke("All UI preferences reset to factory defaults", ToastNotificationType.Success, "Restore");
    }

    // --- Interactive Live Test Playground Commands ---

    [RelayCommand]
    public void TestPrimaryNotification()
    {
        PreviewToastType = ToastNotificationType.Primary;
        PreviewToastMessage = "Primary message comes here";
        PreviewToastIcon = "InformationOutline";
        RefreshPreview();
        TriggerToastRequested?.Invoke("Primary message comes here", ToastNotificationType.Primary, "InformationOutline");
    }

    [RelayCommand]
    public void TestSuccessNotification()
    {
        PreviewToastType = ToastNotificationType.Success;
        PreviewToastMessage = "Success message comes here";
        PreviewToastIcon = "CheckCircleOutline";
        RefreshPreview();
        TriggerToastRequested?.Invoke("Success message comes here", ToastNotificationType.Success, "CheckCircleOutline");
    }

    [RelayCommand]
    public void TestDangerNotification()
    {
        PreviewToastType = ToastNotificationType.Danger;
        PreviewToastMessage = "Danger message comes here";
        PreviewToastIcon = "AlertOctagonOutline";
        RefreshPreview();
        TriggerToastRequested?.Invoke("Danger message comes here", ToastNotificationType.Danger, "AlertOctagonOutline");
    }

    [RelayCommand]
    public void TestWarningNotification()
    {
        PreviewToastType = ToastNotificationType.Warning;
        PreviewToastMessage = "Warning message comes here";
        PreviewToastIcon = "AlertOutline";
        RefreshPreview();
        TriggerToastRequested?.Invoke("Warning message comes here", ToastNotificationType.Warning, "AlertOutline");
    }

    [RelayCommand]
    public void TestGeneralNotification()
    {
        PreviewToastType = ToastNotificationType.General;
        PreviewToastMessage = "General message comes here";
        PreviewToastIcon = "InformationOutline";
        RefreshPreview();
        TriggerToastRequested?.Invoke("General message comes here", ToastNotificationType.General, "InformationOutline");
    }

    private static string GetPositionName(ToastPosition pos) => pos switch
    {
        ToastPosition.TopLeft => "Top-Left",
        ToastPosition.TopCenter => "Top-Center",
        ToastPosition.TopRight => "Top-Right",
        ToastPosition.BottomLeft => "Bottom-Left",
        ToastPosition.BottomCenter => "Bottom-Center",
        ToastPosition.BottomRight => "Bottom-Right",
        _ => "Bottom-Center"
    };
}
