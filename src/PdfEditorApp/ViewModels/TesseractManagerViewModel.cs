using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Services.Ocr;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel representing an individual downloadable Tesseract OCR language model (.traineddata).
/// </summary>
public partial class TesseractLanguageItemViewModel : ObservableObject
{
    private readonly ITesseractModelService _modelService;

    public TesseractLanguagePackageInfo Model { get; }

    public string Code => Model.Code;
    public string DisplayName => Model.DisplayName;
    public string NativeName => Model.NativeName;
    public string FlagEmoji => Model.FlagEmoji;
    public string Category => Model.Category;
    public string Description => Model.Description;
    public string SampleText => Model.SampleText;
    public string FormattedSize => Model.FormattedSize;
    public string FileName => Model.FileName;

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _statusText = "Available";

    [ObservableProperty]
    private string _progressText = "";

    public TesseractLanguageItemViewModel(TesseractLanguagePackageInfo model, ITesseractModelService modelService)
    {
        Model = model;
        _modelService = modelService;
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        IsInstalled = _modelService.IsLanguageInstalled(Model.Code);
        Model.IsInstalled = IsInstalled;
        if (IsInstalled)
        {
            StatusText = "Installed & Active";
            DownloadProgress = 1.0;
        }
        else
        {
            StatusText = $"Available ({FormattedSize})";
            DownloadProgress = 0.0;
        }
    }

    [RelayCommand]
    public async Task DownloadAsync()
    {
        if (IsDownloading || IsInstalled) return;

        IsDownloading = true;
        StatusText = "Connecting...";
        ProgressText = "0%";
        DownloadProgress = 0.0;

        var progress = new Progress<double>(p =>
        {
            DownloadProgress = p;
            ProgressText = $"{p * 100:F0}%";
            StatusText = $"Downloading {ProgressText} ({FormattedSize})";
        });

        bool success = await _modelService.DownloadLanguageAsync(Model.Code, progress, status =>
        {
            StatusText = status;
        });

        IsDownloading = false;
        IsInstalled = success;
        Model.IsInstalled = success;

        if (success)
        {
            StatusText = "Installed & Ready";
            DownloadProgress = 1.0;
        }
        else
        {
            StatusText = "Download Failed (Retry)";
        }
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (IsDownloading) return;

        bool deleted = await _modelService.DeleteLanguageAsync(Model.Code);
        if (deleted)
        {
            IsInstalled = false;
            Model.IsInstalled = false;
            StatusText = $"Available ({FormattedSize})";
            DownloadProgress = 0.0;
        }
    }
}

/// <summary>
/// Main ViewModel for the On-Demand Tesseract OCR Models &amp; Language Studio.
/// Allows users to browse, search, download, and delete trained neural models on demand.
/// </summary>
public partial class TesseractManagerViewModel : ViewModelBase
{
    private readonly ITesseractModelService _modelService;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _totalCacheSizeFormatted = "0 MB";

    [ObservableProperty]
    private int _installedCount;

    [ObservableProperty]
    private int _totalLanguagesCount;

    [ObservableProperty]
    private bool _isDownloadingAll;

    [ObservableProperty]
    private string _globalStatusMessage = "";

    [ObservableProperty]
    private bool _isTesseractCliInstalled;

    [ObservableProperty]
    private string _tesseractCliStatusText = "Checking runtime...";

    public string TessDataDirectory => _modelService.TessDataDirectory;

    public ObservableCollection<TesseractLanguageItemViewModel> AllLanguages { get; } = new();
    public ObservableCollection<TesseractLanguageItemViewModel> FilteredLanguages { get; } = new();

    public TesseractManagerViewModel() : this(new TesseractModelService()) { }

    public TesseractManagerViewModel(ITesseractModelService modelService)
    {
        _modelService = modelService;
        _modelService.LanguageLibraryChanged += OnLanguageLibraryChanged;

        CheckCliRuntime();
        InitializeLanguages();
        _ = RefreshStatsAsync();
    }

    private void CheckCliRuntime()
    {
        string? path = _modelService.GetTesseractCliPath();
        IsTesseractCliInstalled = !string.IsNullOrEmpty(path);
        TesseractCliStatusText = IsTesseractCliInstalled
            ? $"Tesseract Runtime Active ({Path.GetFileName(path)})"
            : "No CLI on PATH (OS Native OCR or .traineddata will be used)";
    }

    private void InitializeLanguages()
    {
        AllLanguages.Clear();
        foreach (var lang in _modelService.AvailableLanguages)
        {
            AllLanguages.Add(new TesseractLanguageItemViewModel(lang, _modelService));
        }

        TotalLanguagesCount = AllLanguages.Count;
        ApplyFilter();
    }

    private void OnLanguageLibraryChanged()
    {
        foreach (var item in AllLanguages)
        {
            item.RefreshStatus();
        }
        _ = RefreshStatsAsync();
        ApplyFilter();
    }

    public async Task RefreshStatsAsync()
    {
        long bytes = await _modelService.GetTotalCacheSizeBytesAsync();
        TotalCacheSizeFormatted = TesseractLanguagePackageInfo.FormatBytes(bytes);
        InstalledCount = AllLanguages.Count(l => l.IsInstalled);
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();

    [RelayCommand]
    public void SelectCategory(string category)
    {
        SelectedCategory = category;
    }

    private void ApplyFilter()
    {
        FilteredLanguages.Clear();
        string query = SearchQuery.Trim().ToLowerInvariant();

        foreach (var item in AllLanguages)
        {
            // Category filter
            bool matchesCategory = SelectedCategory switch
            {
                "All" => true,
                "Installed" => item.IsInstalled,
                "European" => item.Category.Contains("Latin", StringComparison.OrdinalIgnoreCase) || item.Category.Contains("European", StringComparison.OrdinalIgnoreCase),
                "EastAsia" => item.Category.Contains("East Asia", StringComparison.OrdinalIgnoreCase) && !item.Category.Contains("Southeast", StringComparison.OrdinalIgnoreCase),
                "SouthAsia" => item.Category.Contains("South Asia", StringComparison.OrdinalIgnoreCase) || item.Category.Contains("Indic", StringComparison.OrdinalIgnoreCase),
                "MiddleEast" => item.Category.Contains("Middle East", StringComparison.OrdinalIgnoreCase),
                "SoutheastAsia" => item.Category.Contains("Southeast", StringComparison.OrdinalIgnoreCase),
                "Specialized" => item.Category.Contains("Specialized", StringComparison.OrdinalIgnoreCase) || item.Category.Contains("Math", StringComparison.OrdinalIgnoreCase),
                _ => true
            };

            if (!matchesCategory) continue;

            // Search query filter
            if (!string.IsNullOrEmpty(query))
            {
                bool matchesSearch =
                    item.Code.ToLowerInvariant().Contains(query) ||
                    item.DisplayName.ToLowerInvariant().Contains(query) ||
                    item.NativeName.ToLowerInvariant().Contains(query) ||
                    item.Description.ToLowerInvariant().Contains(query) ||
                    item.Category.ToLowerInvariant().Contains(query) ||
                    item.FileName.ToLowerInvariant().Contains(query);

                if (!matchesSearch) continue;
            }

            FilteredLanguages.Add(item);
        }
    }

    [RelayCommand]
    public async Task DownloadAllAsync()
    {
        if (IsDownloadingAll) return;

        IsDownloadingAll = true;
        GlobalStatusMessage = "Downloading all OCR language models...";

        var uninstalled = AllLanguages.Where(l => !l.IsInstalled).ToList();
        int completed = 0;

        foreach (var lang in uninstalled)
        {
            GlobalStatusMessage = $"Downloading {lang.DisplayName} ({completed + 1}/{uninstalled.Count})...";
            await lang.DownloadAsync();
            completed++;
        }

        IsDownloadingAll = false;
        GlobalStatusMessage = "All OCR models downloaded successfully!";
        await RefreshStatsAsync();
    }

    [RelayCommand]
    public async Task ClearAllCacheAsync()
    {
        await _modelService.ClearAllCacheAsync();
        foreach (var lang in AllLanguages)
        {
            lang.RefreshStatus();
        }
        await RefreshStatsAsync();
        ApplyFilter();
        GlobalStatusMessage = "OCR models cache cleared. Storage reclaimed.";
    }

    [RelayCommand]
    public void OpenTessDataFolder()
    {
        try
        {
            string dir = TessDataDirectory;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", dir);
            }
            else if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", dir);
            }
            else
            {
                Process.Start("xdg-open", dir);
            }
        }
        catch { }
    }
}
