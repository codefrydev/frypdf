using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Messages;
using PdfEditorApp.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Categories for grouping preferences in the Settings & UI Studio.
/// </summary>
public enum SettingsCategory
{
    All,
    Appearance,
    Canvas,
    Notifications,
    Ai,
    Privacy
}

/// <summary>
/// ViewModel managing user UI customization preferences, notification placements, and workspace behaviors.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IUiSettingsService _uiSettingsService;
    private readonly IThemeService? _themeService;
    private bool _isUpdatingFromService;

    public void TriggerToast(string message, ToastNotificationType type = ToastNotificationType.Primary, string? icon = null)
    {
        WeakReferenceMessenger.Default.Send(new ShowToastMessage(message, type, icon));
    }

    [ObservableProperty]
    private SettingsCategory _selectedCategory = SettingsCategory.All;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public bool HasSearchQuery => !string.IsNullOrWhiteSpace(SearchQuery);

    public bool IsAppearanceVisible =>
        (SelectedCategory is SettingsCategory.All or SettingsCategory.Appearance) &&
        MatchesSearch("appearance theme color dark light reading reader sepia night contrast ribbon shortcut hint density");

    public bool IsCanvasVisible =>
        (SelectedCategory is SettingsCategory.All or SettingsCategory.Canvas) &&
        MatchesSearch("canvas grid snap zoom alignment inspector document guidelines points");

    public bool IsNotificationsVisible =>
        (SelectedCategory is SettingsCategory.All or SettingsCategory.Notifications) &&
        MatchesSearch("notification toast snackbar sound duration alert badge position placement audio dismiss playground preview");

    public bool IsAiVisible =>
        (SelectedCategory is SettingsCategory.All or SettingsCategory.Ai) &&
        MatchesSearch("ai local llm ollama openai groq model endpoint token cloud intelligence assistant provider");

    public bool IsPrivacyVisible =>
        (SelectedCategory is SettingsCategory.All or SettingsCategory.Privacy) &&
        MatchesSearch("privacy storage reset default offline telemetry data json profile factory");

    public bool HasSearchResults =>
        IsAppearanceVisible || IsCanvasVisible || IsNotificationsVisible || IsAiVisible || IsPrivacyVisible;

    private bool MatchesSearch(string categoryKeywords)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;
        var q = SearchQuery.Trim().ToLowerInvariant();
        return categoryKeywords.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(HasSearchQuery));
        RefreshCategoryVisibilities();
    }

    partial void OnSelectedCategoryChanged(SettingsCategory value)
    {
        RefreshCategoryVisibilities();
    }

    private void RefreshCategoryVisibilities()
    {
        OnPropertyChanged(nameof(IsAppearanceVisible));
        OnPropertyChanged(nameof(IsCanvasVisible));
        OnPropertyChanged(nameof(IsNotificationsVisible));
        OnPropertyChanged(nameof(IsAiVisible));
        OnPropertyChanged(nameof(IsPrivacyVisible));
        OnPropertyChanged(nameof(HasSearchResults));
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    public void SelectCategory(string categoryName)
    {
        if (Enum.TryParse<SettingsCategory>(categoryName, true, out var cat))
        {
            SelectedCategory = cat;
        }
    }

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

    public AiSettingsViewModel AiSettings { get; }

    public SettingsViewModel() : this(new UiSettingsService(), new ThemeService())
    {
    }

    public SettingsViewModel(IUiSettingsService uiSettingsService, IThemeService? themeService = null)
    {
        _uiSettingsService = uiSettingsService;
        _themeService = themeService;
        AiSettings = new AiSettingsViewModel(_uiSettingsService, new Services.AI.AiService());

        LoadFromSettings(_uiSettingsService.Settings);
        _uiSettingsService.SettingsChanged += LoadFromSettings;

        if (_themeService != null)
        {
            _themeService.ThemeChanged += (_) => RefreshPreview();
        }
    }

    private void LoadFromSettings(UiSettingsModel s)
    {
        _isUpdatingFromService = true;
        try
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
        finally
        {
            _isUpdatingFromService = false;
        }
    }

    partial void OnShowGridByDefaultChanged(bool value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.ShowGridByDefault = value);
    }

    partial void OnSnapToGridByDefaultChanged(bool value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.SnapToGridByDefault = value);
    }

    partial void OnGridSnapSizeChanged(GridSnapSize value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.GridSnapSize = value);
    }

    partial void OnDefaultZoomModeChanged(PdfViewerZoomMode value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.DefaultZoomMode = value);
    }

    partial void OnCompactRibbonByDefaultChanged(bool value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.CompactRibbonByDefault = value);
    }

    partial void OnAutoExpandInspectorOnSelectChanged(bool value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.AutoExpandInspectorOnSelect = value);
    }

    partial void OnShowShortcutHintsChanged(bool value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.ShowShortcutHints = value);
    }

    partial void OnThemeModeChanged(AppThemeMode value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.ThemeMode = value);
        _themeService?.SetTheme(value);
        RefreshPreview();
    }

    partial void OnReadingThemeChanged(PdfReaderTheme value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.ReadingTheme = value);
        _themeService?.SetReadingTheme(value);
    }

    partial void OnToastPositionChanged(ToastPosition value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.ToastPosition = value);
        RefreshPreview();
    }

    partial void OnToastStyleVariantChanged(ToastStyleVariant value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.ToastStyleVariant = value);
        RefreshPreview();
    }

    partial void OnToastDurationMsChanged(int value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.ToastDurationMs = value);
    }

    partial void OnToastShowCloseButtonChanged(bool value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.ToastShowCloseButton = value);
        RefreshPreview();
    }

    partial void OnToastSoundEnabledChanged(bool value)
    {
        if (_isUpdatingFromService) return;
        _uiSettingsService.UpdateSettings(s => s.ToastSoundEnabled = value);
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
        TriggerToast($"Notification placement set to {GetPositionName(pos)}", ToastNotificationType.Primary, "DockBottom");
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
        TriggerToast($"Snackbar visual style changed to {variant}", ToastNotificationType.Primary, "PaletteOutline");
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
        TriggerToast($"Notification timeout set to {durLabel}", ToastNotificationType.General, "ClockOutline");
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
        TriggerToast("All UI preferences reset to factory defaults", ToastNotificationType.Success, "Restore");
    }

    // --- Interactive Live Test Playground Commands ---

    [RelayCommand]
    public void TestPrimaryNotification()
    {
        PreviewToastType = ToastNotificationType.Primary;
        PreviewToastMessage = "Primary message comes here";
        PreviewToastIcon = "InformationOutline";
        RefreshPreview();
        TriggerToast("Primary message comes here", ToastNotificationType.Primary, "InformationOutline");
    }

    [RelayCommand]
    public void TestSuccessNotification()
    {
        PreviewToastType = ToastNotificationType.Success;
        PreviewToastMessage = "Success message comes here";
        PreviewToastIcon = "CheckCircleOutline";
        RefreshPreview();
        TriggerToast("Success message comes here", ToastNotificationType.Success, "CheckCircleOutline");
    }

    [RelayCommand]
    public void TestDangerNotification()
    {
        PreviewToastType = ToastNotificationType.Danger;
        PreviewToastMessage = "Danger message comes here";
        PreviewToastIcon = "AlertOctagonOutline";
        RefreshPreview();
        TriggerToast("Danger message comes here", ToastNotificationType.Danger, "AlertOctagonOutline");
    }

    [RelayCommand]
    public void TestWarningNotification()
    {
        PreviewToastType = ToastNotificationType.Warning;
        PreviewToastMessage = "Warning message comes here";
        PreviewToastIcon = "AlertOutline";
        RefreshPreview();
        TriggerToast("Warning message comes here", ToastNotificationType.Warning, "AlertOutline");
    }

    [RelayCommand]
    public void TestGeneralNotification()
    {
        PreviewToastType = ToastNotificationType.General;
        PreviewToastMessage = "General message comes here";
        PreviewToastIcon = "InformationOutline";
        RefreshPreview();
        TriggerToast("General message comes here", ToastNotificationType.General, "InformationOutline");
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
