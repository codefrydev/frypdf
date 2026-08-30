using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Services;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// ViewModel representing an individual downloadable language / font package card.
/// </summary>
public partial class FontPackageItemViewModel : ObservableObject
{
    private readonly IFontPackageService _fontService;

    public FontPackageInfo Model { get; }

    public string Id => Model.Id;
    public string Name => Model.Name;
    public string NativeName => Model.NativeName;
    public string FlagEmoji => Model.FlagEmoji;
    public string Region => Model.Region;
    public FontPackageCategory Category => Model.Category;
    public string Description => Model.Description;
    public string SampleText => Model.SampleText;
    public string FormattedSize => Model.FormattedSize;
    public IReadOnlyList<string> SupportedLanguages => Model.SupportedLanguages;
    public IReadOnlyList<string> IncludedFontFamilies => Model.IncludedFontFamilies;

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

    public FontPackageItemViewModel(FontPackageInfo model, IFontPackageService fontService)
    {
        Model = model;
        _fontService = fontService;
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        IsInstalled = _fontService.IsPackageInstalled(Model);
        if (IsInstalled)
        {
            StatusText = "Installed";
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

        bool success = await _fontService.DownloadPackageAsync(Model, progress, status =>
        {
            StatusText = status;
        });

        IsDownloading = false;
        IsInstalled = success;

        if (success)
        {
            StatusText = "Installed & Active";
            DownloadProgress = 1.0;
        }
        else
        {
            StatusText = "Download Failed (Click to retry)";
        }
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (IsDownloading) return;

        bool deleted = await _fontService.DeletePackageAsync(Model);
        if (deleted)
        {
            IsInstalled = false;
            StatusText = $"Available ({FormattedSize})";
            DownloadProgress = 0.0;
        }
    }
}

/// <summary>
/// Main ViewModel for the Font &amp; Language Pack Management Studio.
/// Empowers users to download, inspect, and delete international font packs to keep the app ultra-lightweight.
/// </summary>
public partial class FontManagerViewModel : ViewModelBase
{
    private readonly IFontPackageService _fontService;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _totalCacheSizeFormatted = "0 MB";

    [ObservableProperty]
    private int _installedCount;

    [ObservableProperty]
    private int _totalPackagesCount;

    [ObservableProperty]
    private bool _isDownloadingAll;

    [ObservableProperty]
    private string _globalStatusMessage = "";

    public string UserFontDirectory => _fontService.GetUserFontDirectory();

    public ObservableCollection<FontPackageItemViewModel> AllPackages { get; } = new();
    public ObservableCollection<FontPackageItemViewModel> FilteredPackages { get; } = new();

    public FontManagerViewModel() : this(new FontPackageService()) { }

    public FontManagerViewModel(IFontPackageService fontService)
    {
        _fontService = fontService;
        _fontService.FontLibraryChanged += OnFontLibraryChanged;

        InitializePackages();
        _ = RefreshStatsAsync();
    }

    private void InitializePackages()
    {
        AllPackages.Clear();
        foreach (var pack in _fontService.GetAllPackages())
        {
            AllPackages.Add(new FontPackageItemViewModel(pack, _fontService));
        }

        TotalPackagesCount = AllPackages.Count;
        ApplyFilter();
    }

    private void OnFontLibraryChanged()
    {
        foreach (var item in AllPackages)
        {
            item.RefreshStatus();
        }
        _ = RefreshStatsAsync();
        ApplyFilter();
    }

    public async Task RefreshStatsAsync()
    {
        long bytes = await _fontService.GetTotalCacheSizeBytesAsync();
        TotalCacheSizeFormatted = FontPackageInfo.FormatBytes(bytes);
        InstalledCount = AllPackages.Count(p => p.IsInstalled);
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
        FilteredPackages.Clear();
        string query = SearchQuery.Trim().ToLowerInvariant();

        foreach (var pack in AllPackages)
        {
            // Category filter
            bool matchesCategory = SelectedCategory switch
            {
                "All" => true,
                "Installed" => pack.IsInstalled,
                "EastAsia" => pack.Category == FontPackageCategory.EastAsia,
                "SouthAsia" => pack.Category == FontPackageCategory.SouthAsia,
                "MiddleEast" => pack.Category == FontPackageCategory.MiddleEast,
                "SoutheastAsia" => pack.Category == FontPackageCategory.SoutheastAsia,
                "EuropeAndEurasia" => pack.Category == FontPackageCategory.EuropeAndEurasia,
                "DesignAndTypography" => pack.Category == FontPackageCategory.DesignAndTypography,
                _ => true
            };

            if (!matchesCategory) continue;

            // Search query
            if (!string.IsNullOrEmpty(query))
            {
                bool matchesSearch =
                    pack.Name.ToLowerInvariant().Contains(query) ||
                    pack.NativeName.ToLowerInvariant().Contains(query) ||
                    pack.Region.ToLowerInvariant().Contains(query) ||
                    pack.Description.ToLowerInvariant().Contains(query) ||
                    pack.SupportedLanguages.Any(l => l.ToLowerInvariant().Contains(query)) ||
                    pack.IncludedFontFamilies.Any(f => f.ToLowerInvariant().Contains(query));

                if (!matchesSearch) continue;
            }

            FilteredPackages.Add(pack);
        }
    }

    [RelayCommand]
    public async Task DownloadAllAsync()
    {
        if (IsDownloadingAll) return;

        IsDownloadingAll = true;
        GlobalStatusMessage = "Downloading all language packs...";

        var uninstalled = AllPackages.Where(p => !p.IsInstalled).ToList();
        int completed = 0;

        foreach (var pack in uninstalled)
        {
            GlobalStatusMessage = $"Downloading {pack.Name} ({completed + 1}/{uninstalled.Count})...";
            await pack.DownloadAsync();
            completed++;
        }

        IsDownloadingAll = false;
        GlobalStatusMessage = "All packs downloaded successfully!";
        await RefreshStatsAsync();
    }

    [RelayCommand]
    public async Task ClearAllCacheAsync()
    {
        await _fontService.ClearAllCacheAsync();
        foreach (var pack in AllPackages)
        {
            pack.RefreshStatus();
        }
        await RefreshStatsAsync();
        ApplyFilter();
        GlobalStatusMessage = "Font cache cleared. All local font downloads removed.";
    }

    [RelayCommand]
    public void OpenFontFolder()
    {
        try
        {
            string dir = UserFontDirectory;
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
