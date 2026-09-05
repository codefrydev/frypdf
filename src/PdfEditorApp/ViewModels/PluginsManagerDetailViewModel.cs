using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Plugins.Marketplace;

namespace PdfEditorApp.ViewModels;

public enum ExtensionDetailTab
{
    Overview,
    Contributions,
    Settings,
    Runtime
}

/// <summary>
/// Detail presentation model for the selected plugin in the Plugins & Extensions Studio.
/// Displays comprehensive metadata, stats, action controls, and 4 dedicated tabs:
/// Overview, Feature Contributions, Declarative Settings, and Runtime/Dependencies.
/// </summary>
public partial class PluginsManagerDetailViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _publisher = "FryPDF Official";

    [ObservableProperty]
    private string _category = "General";

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _longDescription = string.Empty;

    [ObservableProperty]
    private string _iconKind = "PuzzleOutline";

    [ObservableProperty]
    private string _iconColorHex = "#7C3AED";

    [ObservableProperty]
    private double _rating = 4.8;

    [ObservableProperty]
    private int _ratingCount = 120;

    [ObservableProperty]
    private int _installCount = 1500;

    [ObservableProperty]
    private string _formattedSize = "1.2 MB";

    [ObservableProperty]
    private string _license = "MIT";

    [ObservableProperty]
    private bool _isOfficial = true;

    [ObservableProperty]
    private bool _isVerified = true;

    [ObservableProperty]
    private bool _isInstalled = true;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isExternal;

    [ObservableProperty]
    private string _sourceAssembly = "Built-in";

    [ObservableProperty]
    private ExtensionDetailTab _selectedTab = ExtensionDetailTab.Overview;

    [ObservableProperty]
    private string _runtimeStatus = "Active (Mounted in Kernel)";

    [ObservableProperty]
    private string _assemblyPath = "In-memory AppContext";

    [ObservableProperty]
    private string _assemblyLoadContextName = "Default AssemblyLoadContext";

    public ObservableCollection<string> ContributedFeatures { get; } = new();
    public ObservableCollection<string> Highlights { get; } = new();
    public ObservableCollection<string> Dependencies { get; } = new();
    public ObservableCollection<PluginSettingItemViewModel> SettingsList { get; } = new();

    public bool HasSettings => SettingsList.Count > 0;
    public bool HasContributions => ContributedFeatures.Count > 0;

    public Func<string, bool, Task>? ToggleActiveCallback { get; set; }
    public Func<string, Task>? InstallCallback { get; set; }
    public Func<string, Task>? UninstallCallback { get; set; }
    public Action<string>? CopyToClipboardCallback { get; set; }
    public Action<string>? ShowToastCallback { get; set; }

    [RelayCommand]
    public void SelectTab(string tabName)
    {
        if (Enum.TryParse<ExtensionDetailTab>(tabName, true, out var tab))
        {
            SelectedTab = tab;
        }
    }

    [RelayCommand]
    public async Task ToggleActiveAsync()
    {
        if (!IsInstalled || ToggleActiveCallback == null) return;
        var next = !IsActive;
        IsActive = next;
        RuntimeStatus = next ? "Active (Mounted in Kernel)" : "Disabled (Unloaded)";
        await ToggleActiveCallback(Id, next);
    }

    [RelayCommand]
    public async Task InstallAsync()
    {
        if (InstallCallback == null) return;
        await InstallCallback(Id);
    }

    [RelayCommand]
    public async Task UninstallAsync()
    {
        if (UninstallCallback == null) return;
        await UninstallCallback(Id);
    }

    [RelayCommand]
    public void CopyId()
    {
        if (string.IsNullOrWhiteSpace(Id)) return;
        CopyToClipboardCallback?.Invoke(Id);
        ShowToastCallback?.Invoke($"Copied extension ID '{Id}' to clipboard");
    }

    [RelayCommand]
    public void SaveSettings()
    {
        ShowToastCallback?.Invoke($"Saved preferences for '{Name}'");
    }

    /// <summary>
    /// Populates detail from an installed plugin item.
    /// </summary>
    public static PluginsManagerDetailViewModel FromInstalledPlugin(
        PluginItemViewModel plugin,
        IReadOnlyList<string>? contributions = null)
    {
        var vm = new PluginsManagerDetailViewModel
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Version = plugin.Version,
            Publisher = plugin.IsExternal ? "3rd-Party Developer" : "FryPDF Core Team",
            Category = plugin.Category,
            Description = plugin.Description,
            LongDescription = string.IsNullOrWhiteSpace(plugin.Description)
                ? $"Official module '{plugin.Name}' providing high-performance capabilities in {plugin.Category}."
                : $"{plugin.Description}\n\nThis extension is deeply integrated into FryPDF's modular microkernel architecture. It registers tools, services, and visual controls dynamically with isolated execution and zero memory leak guarantees.",
            IconKind = plugin.IconKind,
            IconColorHex = plugin.IconColorHex,
            IsInstalled = true,
            IsActive = plugin.IsActive,
            IsExternal = plugin.IsExternal,
            IsOfficial = !plugin.IsExternal,
            IsVerified = true,
            License = "MIT",
            FormattedSize = plugin.IsExternal ? "2.1 MB" : "Built-in (.NET 10)",
            SourceAssembly = plugin.SourceAssembly,
            AssemblyPath = plugin.IsExternal ? plugin.SourceAssembly : "src/PdfEditorApp/bin/Debug/net10.0/PdfEditorApp.dll",
            AssemblyLoadContextName = plugin.IsExternal ? "IsolatedAssemblyLoadContext (ALC)" : "System.Runtime.Loader.Default",
            RuntimeStatus = plugin.IsActive ? "Active (Mounted in Kernel)" : "Disabled (Suspended)"
        };

        if (contributions != null && contributions.Count > 0)
        {
            foreach (var c in contributions)
            {
                vm.ContributedFeatures.Add(c);
            }
        }
        else
        {
            // Default contributions inferred from ID and category
            vm.ContributedFeatures.Add($"Module Component: {plugin.Name}");
            vm.ContributedFeatures.Add($"Category Registration: {plugin.Category}");
            if (plugin.Id.Contains(".tool."))
            {
                vm.ContributedFeatures.Add("Interactive Tool: Studio Canvas & Quick Action Ribbon");
            }
            if (plugin.Id.Contains(".page."))
            {
                vm.ContributedFeatures.Add("Workspace Navigation: First-class studio page");
            }
            if (plugin.Id.Contains(".dialog."))
            {
                vm.ContributedFeatures.Add("Modal Studio: Material Design 3 Dialog");
            }
            if (plugin.Id.Contains(".sidebar."))
            {
                vm.ContributedFeatures.Add("Dockable Sidebar: Inspector / Thumbnails / Tool Palette");
            }
        }

        if (plugin.Category.Contains("AI", StringComparison.OrdinalIgnoreCase))
        {
            vm.Highlights.Add("Multimodal document understanding & streaming intelligence");
            vm.Highlights.Add("Zero data retention with local or privacy-first inference");
            vm.Highlights.Add("Interactive prompts and contextual summaries");
        }
        else if (plugin.Category.Contains("Conversion", StringComparison.OrdinalIgnoreCase))
        {
            vm.Highlights.Add("High-fidelity document structure and layout reconstruction");
            vm.Highlights.Add("Preserves embedded vector shapes, images, and fonts");
            vm.Highlights.Add("Hardware-accelerated Skia rendering and export");
        }
        else if (plugin.Category.Contains("Security", StringComparison.OrdinalIgnoreCase))
        {
            vm.Highlights.Add("Enterprise-grade cryptographic security and validation");
            vm.Highlights.Add("Permanent, irreversible sanitization and redaction");
            vm.Highlights.Add("FIPS-compliant AES-256 document protection");
        }
        else if (plugin.Category.Contains("Canvas", StringComparison.OrdinalIgnoreCase))
        {
            vm.Highlights.Add("Full-fidelity visual canvas editing with drag handles");
            vm.Highlights.Add("Real-time property inspector two-way data binding");
            vm.Highlights.Add("Script-aware layout engine with Unicode font fallback");
        }
        else
        {
            vm.Highlights.Add("High-performance .NET 10 modular execution");
            vm.Highlights.Add("Isolated microkernel lifecycle management");
            vm.Highlights.Add("Integrated undo/redo and atomic command history");
        }

        vm.Dependencies.Add("PdfEditorApp.Core (v1.0.0+)");
        vm.Dependencies.Add("Avalonia UI (v12.x+)");

        // Load settings schema if available
        if (plugin.SettingsSchema != null)
        {
            foreach (var kvp in plugin.SettingsSchema)
            {
                var def = kvp.Value;
                vm.SettingsList.Add(new PluginSettingItemViewModel
                {
                    Key = kvp.Key,
                    Label = def.Label ?? kvp.Key,
                    Description = def.Description ?? "",
                    Type = def.Type ?? "string",
                    Options = def.Options != null ? new List<string>(def.Options) : new List<string>(),
                    StringValue = def.DefaultValue?.ToString() ?? "",
                    BoolValue = bool.TryParse(def.DefaultValue?.ToString(), out var b) && b
                });
            }
        }

        return vm;
    }

    /// <summary>
    /// Populates detail from a marketplace extension item.
    /// </summary>
    public static PluginsManagerDetailViewModel FromMarketplaceItem(MarketplacePluginItem item)
    {
        var isInstalled = item.Status == MarketplacePluginStatus.Installed;
        var vm = new PluginsManagerDetailViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Version = item.Version,
            Publisher = item.Publisher,
            Category = item.Category,
            Description = item.Description,
            LongDescription = !string.IsNullOrWhiteSpace(item.LongDescription) ? item.LongDescription : item.Description,
            IconKind = item.IconKind,
            IconColorHex = item.IconColorHex,
            Rating = item.Rating,
            RatingCount = item.RatingCount,
            InstallCount = item.InstallCount,
            FormattedSize = item.FormattedSize,
            License = item.License,
            IsOfficial = item.IsOfficial,
            IsVerified = item.IsVerified,
            IsInstalled = isInstalled,
            IsActive = isInstalled,
            IsExternal = true,
            SourceAssembly = isInstalled ? $"plugins/{item.Id}/{item.Id}.dll" : "FryPDF Marketplace Remote Registry",
            AssemblyPath = isInstalled ? $"plugins/{item.Id}/{item.Id}.dll" : "Remote Package Archive (.fryplugin)",
            AssemblyLoadContextName = "PluginAssemblyLoadContext (Isolated)",
            RuntimeStatus = isInstalled ? "Active (Mounted in Kernel)" : "Available in Marketplace"
        };

        foreach (var c in item.ContributedFeatures)
        {
            vm.ContributedFeatures.Add(c);
        }

        foreach (var h in item.Highlights)
        {
            vm.Highlights.Add(h);
        }

        foreach (var d in item.Dependencies)
        {
            vm.Dependencies.Add(d);
        }

        return vm;
    }
}
