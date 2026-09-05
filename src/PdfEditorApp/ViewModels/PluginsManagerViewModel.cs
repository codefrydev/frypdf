using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Plugins.Loader;
using PdfEditorApp.Services.Plugins;
using PdfEditorApp.Services.Tools.Core;

namespace PdfEditorApp.ViewModels;

public enum PluginsManagerTab
{
    Installed,
    Marketplace,
    Updates
}

/// <summary>
/// Master ViewModel for the VS Code-inspired Plugins & Extensions Manager Studio Page.
/// Coordinates search, category filtering, installed modules list, marketplace catalog,
/// local package installation (.fryplugin/.dll), profile switching, and detail inspection.
/// </summary>
public partial class PluginsManagerViewModel : ViewModelBase
{
    private readonly PluginHost? _pluginHost;
    private readonly IPluginMarketplaceService _marketplaceService;
    private readonly IPdfToolRegistry? _toolRegistry;
    private readonly List<PluginItemViewModel> _allInstalled = new();
    private readonly List<MarketplacePluginItem> _allMarketplace = new();

    public IStorageProvider? StorageProvider { get; set; }
    public IClipboard? Clipboard { get; set; }
    public Action<string>? ShowToastCallback { get; set; }

    [ObservableProperty]
    private PluginsManagerTab _selectedTab = PluginsManagerTab.Installed;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _activeProfileName = "desktop";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private PluginItemViewModel? _selectedInstalledPlugin;

    [ObservableProperty]
    private MarketplacePluginItem? _selectedMarketplacePlugin;

    [ObservableProperty]
    private PluginsManagerDetailViewModel? _selectedDetail;

    public ObservableCollection<PluginItemViewModel> FilteredInstalledPlugins { get; } = new();
    public ObservableCollection<MarketplacePluginItem> FilteredMarketplacePlugins { get; } = new();
    public ObservableCollection<string> AvailableCategories { get; } = new();

    public int InstalledCount => _allInstalled.Count;
    public int ActiveInstalledCount => _allInstalled.Count(p => p.IsActive);
    public int MarketplaceCount => _allMarketplace.Count;

    public bool HasSelectedDetail => SelectedDetail != null;

    public PluginsManagerViewModel(
        PluginHost? pluginHost = null,
        IPluginMarketplaceService? marketplaceService = null,
        IPdfToolRegistry? toolRegistry = null)
    {
        _pluginHost = pluginHost;
        _marketplaceService = marketplaceService ?? new PluginMarketplaceService(pluginHost);
        _toolRegistry = toolRegistry;

        InitializeCategories();
        _ = LoadAllDataAsync();
    }

    private void InitializeCategories()
    {
        AvailableCategories.Clear();
        var cats = new[]
        {
            "All",
            "Tools & Productivity",
            "Conversion & Office",
            "AI & Intelligence",
            "Security & Signatures",
            "Workspace Pages",
            "Canvas Elements",
            "Document I/O",
            "UI & Extensions"
        };
        foreach (var c in cats) AvailableCategories.Add(c);
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedTabChanged(PluginsManagerTab value)
    {
        ApplyFilters();
        // Update detail selection based on the new tab
        if (value == PluginsManagerTab.Installed && SelectedInstalledPlugin != null)
        {
            UpdateDetailFromInstalled(SelectedInstalledPlugin);
        }
        else if (value == PluginsManagerTab.Marketplace && SelectedMarketplacePlugin != null)
        {
            UpdateDetailFromMarketplace(SelectedMarketplacePlugin);
        }
    }

    partial void OnSelectedInstalledPluginChanged(PluginItemViewModel? value)
    {
        if (value != null && SelectedTab == PluginsManagerTab.Installed)
        {
            UpdateDetailFromInstalled(value);
        }
    }

    partial void OnSelectedMarketplacePluginChanged(MarketplacePluginItem? value)
    {
        if (value != null && SelectedTab == PluginsManagerTab.Marketplace)
        {
            UpdateDetailFromMarketplace(value);
        }
    }

    [RelayCommand]
    public void SelectTab(string tabName)
    {
        if (Enum.TryParse<PluginsManagerTab>(tabName, true, out var tab))
        {
            SelectedTab = tab;
        }
    }

    [RelayCommand]
    public void SelectCategory(string category)
    {
        SelectedCategory = category;
    }

    [RelayCommand]
    public async Task RefreshAllAsync()
    {
        await LoadAllDataAsync();
    }

    public async Task LoadAllDataAsync()
    {
        IsBusy = true;
        StatusMessage = "Refreshing loaded plugins and marketplace catalog...";

        try
        {
            PopulateInstalledPlugins();
            var catalog = await _marketplaceService.GetCatalogAsync();
            _allMarketplace.Clear();
            _allMarketplace.AddRange(catalog);

            ApplyFilters();

            if (SelectedDetail == null && FilteredInstalledPlugins.Count > 0)
            {
                SelectedInstalledPlugin = FilteredInstalledPlugins[0];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading plugins: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void PopulateInstalledPlugins()
    {
        _allInstalled.Clear();

        if (_pluginHost != null)
        {
            foreach (var plugin in _pluginHost.LoadedPlugins)
            {
                var vm = new PluginItemViewModel
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Version = plugin.Version?.ToString() ?? "1.0.0",
                    Category = InferCategory(plugin),
                    Description = InferDescription(plugin),
                    IconKind = InferIcon(plugin),
                    IconColorHex = InferColor(plugin),
                    IsActive = true,
                    IsExternal = plugin.GetType().Assembly != typeof(App).Assembly &&
                                 plugin.GetType().Assembly != typeof(PluginHost).Assembly,
                    SourceAssembly = plugin.GetType().Assembly.GetName().Name ?? "Core",
                    ToggleHandler = async (id, active) =>
                    {
                        if (active)
                        {
                            await _pluginHost.EnablePluginAsync(id);
                        }
                        else
                        {
                            await _pluginHost.DisablePluginAsync(id);
                        }
                        OnPropertyChanged(nameof(ActiveInstalledCount));
                    }
                };
                _allInstalled.Add(vm);
            }
        }

        OnPropertyChanged(nameof(InstalledCount));
        OnPropertyChanged(nameof(ActiveInstalledCount));
    }

    private void ApplyFilters()
    {
        var q = SearchQuery.Trim().ToLowerInvariant();
        var cat = SelectedCategory;

        // Filter Installed
        FilteredInstalledPlugins.Clear();
        foreach (var p in _allInstalled)
        {
            if (cat != "All" && !string.Equals(p.Category, cat, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(q) ||
                p.Name.ToLowerInvariant().Contains(q) ||
                p.Id.ToLowerInvariant().Contains(q) ||
                p.Category.ToLowerInvariant().Contains(q) ||
                p.Description.ToLowerInvariant().Contains(q))
            {
                FilteredInstalledPlugins.Add(p);
            }
        }

        // Filter Marketplace
        FilteredMarketplacePlugins.Clear();
        foreach (var m in _allMarketplace)
        {
            if (cat != "All" && !string.Equals(m.Category, cat, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(q) ||
                m.Name.ToLowerInvariant().Contains(q) ||
                m.Id.ToLowerInvariant().Contains(q) ||
                m.Publisher.ToLowerInvariant().Contains(q) ||
                m.Description.ToLowerInvariant().Contains(q) ||
                m.Tags.Any(t => t.ToLowerInvariant().Contains(q)))
            {
                FilteredMarketplacePlugins.Add(m);
            }
        }

        // Maintain selection stability or select the first item so the detail pane is never left blank unexpectedly
        if (SelectedTab == PluginsManagerTab.Installed)
        {
            if (SelectedInstalledPlugin == null || !FilteredInstalledPlugins.Contains(SelectedInstalledPlugin))
            {
                SelectedInstalledPlugin = FilteredInstalledPlugins.Count > 0 ? FilteredInstalledPlugins[0] : null;
            }
            else
            {
                UpdateDetailFromInstalled(SelectedInstalledPlugin);
            }
        }
        else if (SelectedTab == PluginsManagerTab.Marketplace)
        {
            if (SelectedMarketplacePlugin == null || !FilteredMarketplacePlugins.Contains(SelectedMarketplacePlugin))
            {
                SelectedMarketplacePlugin = FilteredMarketplacePlugins.Count > 0 ? FilteredMarketplacePlugins[0] : null;
            }
            else
            {
                UpdateDetailFromMarketplace(SelectedMarketplacePlugin);
            }
        }
    }

    private void UpdateDetailFromInstalled(PluginItemViewModel plugin)
    {
        var detail = PluginsManagerDetailViewModel.FromInstalledPlugin(plugin);
        WireDetailCallbacks(detail);
        SelectedDetail = detail;
        OnPropertyChanged(nameof(HasSelectedDetail));
    }

    private void UpdateDetailFromMarketplace(MarketplacePluginItem item)
    {
        var detail = PluginsManagerDetailViewModel.FromMarketplaceItem(item);
        WireDetailCallbacks(detail);
        SelectedDetail = detail;
        OnPropertyChanged(nameof(HasSelectedDetail));
    }

    private void WireDetailCallbacks(PluginsManagerDetailViewModel detail)
    {
        detail.ToggleActiveCallback = async (id, active) =>
        {
            if (_pluginHost != null)
            {
                if (active) await _pluginHost.EnablePluginAsync(id);
                else await _pluginHost.DisablePluginAsync(id);
            }
            OnPropertyChanged(nameof(ActiveInstalledCount));
        };

        detail.InstallCallback = async (id) =>
        {
            await InstallMarketplacePluginAsync(id);
        };

        detail.UninstallCallback = async (id) =>
        {
            await UninstallMarketplacePluginAsync(id);
        };

        detail.CopyToClipboardCallback = async (text) =>
        {
            if (Clipboard != null)
            {
                await Clipboard.SetTextAsync(text);
            }
        };

        detail.ShowToastCallback = (msg) =>
        {
            ShowToastCallback?.Invoke(msg);
        };
    }

    [RelayCommand]
    public async Task InstallMarketplacePluginAsync(string pluginId)
    {
        IsBusy = true;
        StatusMessage = $"Installing extension '{pluginId}' from store...";

        try
        {
            var progress = new Progress<double>(p =>
            {
                StatusMessage = $"Installing {pluginId} ({(int)(p * 100)}%)...";
            });

            bool success = await _marketplaceService.InstallPluginAsync(pluginId, progress, s => StatusMessage = s);
            if (success)
            {
                ShowToastCallback?.Invoke($"Installed extension '{pluginId}' successfully!");
                await LoadAllDataAsync();
                SelectedTab = PluginsManagerTab.Installed;
            }
            else
            {
                ShowToastCallback?.Invoke($"Failed to install extension '{pluginId}'");
            }
        }
        catch (Exception ex)
        {
            ShowToastCallback?.Invoke($"Install error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UninstallMarketplacePluginAsync(string pluginId)
    {
        IsBusy = true;
        try
        {
            await _marketplaceService.UninstallPluginAsync(pluginId);
            ShowToastCallback?.Invoke($"Uninstalled extension '{pluginId}'");
            await LoadAllDataAsync();
        }
        catch (Exception ex)
        {
            ShowToastCallback?.Invoke($"Uninstall error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task InstallFromFileAsync()
    {
        if (StorageProvider == null) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select FryPDF Plugin Package (.fryplugin) or Assembly (.dll)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("FryPDF Plugins (*.fryplugin; *.dll)")
                {
                    Patterns = new[] { "*.fryplugin", "*.dll" }
                },
                new FilePickerFileType("FryPDF Plugin Package (*.fryplugin)")
                {
                    Patterns = new[] { "*.fryplugin" }
                },
                new FilePickerFileType(".NET Assembly (*.dll)")
                {
                    Patterns = new[] { "*.dll" }
                }
            }
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            await InstallLocalPackageAsync(path);
        }
    }

    public async Task InstallLocalPackageAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;

        IsBusy = true;
        StatusMessage = $"Mounting '{Path.GetFileName(filePath)}'...";

        try
        {
            if (_pluginHost == null)
            {
                ShowToastCallback?.Invoke("PluginHost is not initialized.");
                return;
            }

            IReadOnlyList<IFryPlugin> plugins;
            string displayName;

            if (string.Equals(Path.GetExtension(filePath), ".fryplugin", StringComparison.OrdinalIgnoreCase))
            {
                var pkgResult = FryPluginPackageLoader.UnpackAndLoad(filePath);
                plugins = pkgResult.AssemblyPackage.Plugins;
                displayName = pkgResult.Manifest.Name;
            }
            else
            {
                var pkg = PluginAssemblyLoader.LoadPluginAssembly(filePath);
                plugins = pkg.Plugins;
                displayName = Path.GetFileName(filePath);
            }

            _pluginHost.RegisterPlugins(plugins);
            foreach (var p in plugins)
            {
                await _pluginHost.EnablePluginAsync(p.Id);
            }

            PopulateInstalledPlugins();
            ApplyFilters();

            ShowToastCallback?.Invoke($"Successfully installed and mounted '{displayName}' ({plugins.Count} plugins)!");
            SelectedTab = PluginsManagerTab.Installed;
        }
        catch (Exception ex)
        {
            ShowToastCallback?.Invoke($"Failed to install plugin: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void SwitchProfile(string profileName)
    {
        try
        {
            if (_pluginHost == null) return;

            string profilePath = Path.Combine(AppContext.BaseDirectory, "profiles", $"{profileName}.profile.json");
            if (!File.Exists(profilePath))
            {
                profilePath = $"profiles/{profileName}.profile.json";
            }

            if (File.Exists(profilePath))
            {
                var profile = ProfileLoader.LoadFromFile(profilePath);
                ActiveProfileName = profile.ProfileName;

                var availableBundles = new IFryPluginBundle[]
                {
                    new ToolsOrganizeBundle(),
                    new ToolsSecurityBundle(),
                    new ToolsConversionBundle(),
                    new ToolsIntelligenceBundle(),
                    new DataStudioBundle(),
                    new CanvasElementsBundle(),
                    new DocumentIoBundle(),
                    new AiProvidersBundle(),
                    new OcrEnginesBundle(),
                    new StandardTemplatesBundle(),
                    new StatusBarBundle(),
                    new InspectorBundle(),
                    new CommandPaletteBundle(),
                    new WorkspacePagesBundle(),
                    new DialogsBundle(),
                    new EditorSidebarsBundle()
                };

                ProfileLoader.ApplyProfile(profile, _pluginHost, availableBundles);
                PopulateInstalledPlugins();
                ApplyFilters();
                ShowToastCallback?.Invoke($"Switched to profile '{profileName}'");
            }
        }
        catch (Exception ex)
        {
            ShowToastCallback?.Invoke($"Failed to switch profile: {ex.Message}");
        }
    }

    private record struct PluginMeta(string Category, string Description, string Icon, string Color);

    private static readonly Dictionary<string, PluginMeta> KnownPluginMetas = new(StringComparer.OrdinalIgnoreCase)
    {
        // Organize & Page Tools
        ["frypdf.tool.merge"] = new("Tools & Productivity", "Combine multiple PDF documents or pages into a single organized file.", "CallMerge", "#3B82F6"),
        ["frypdf.tool.split"] = new("Tools & Productivity", "Extract individual pages or split documents by bookmarks and ranges.", "CallSplit", "#6366F1"),
        ["frypdf.tool.rotate"] = new("Tools & Productivity", "Rotate portrait/landscape pages 90°, 180°, or 270° with live preview.", "RotateRight", "#8B5CF6"),
        ["frypdf.tool.organize"] = new("Tools & Productivity", "Reorder, duplicate, delete, and sort pages visually in grid mode.", "ViewGridOutline", "#EC4899"),
        ["frypdf.tool.crop"] = new("Tools & Productivity", "Trim margins and adjust page bounding boxes interactively.", "Crop", "#F43F5E"),
        ["frypdf.tool.pagenumbers"] = new("Tools & Productivity", "Add customizable headers, footers, bates stamps, and pagination.", "Numeric1BoxOutline", "#10B981"),

        // Optimize & Security Tools
        ["frypdf.tool.compress"] = new("Tools & Productivity", "Optimize raster images and remove redundant streams to reduce file size.", "ZipBoxOutline", "#06B6D4"),
        ["frypdf.tool.repair"] = new("Tools & Productivity", "Recover damaged PDF xref tables and repair broken font dictionaries.", "AutoFix", "#F59E0B"),
        ["frypdf.tool.protect"] = new("Security & Signatures", "Encrypt documents with AES-256 and restrict printing or copying.", "LockOutline", "#DC2626"),
        ["frypdf.tool.unlock"] = new("Security & Signatures", "Remove owner permissions and passwords from authorized files.", "LockOpenVariantOutline", "#EA580C"),
        ["frypdf.tool.sign"] = new("Security & Signatures", "Place cryptographic digital signatures, stamps, and handwritten initials.", "Draw", "#7C3AED"),
        ["frypdf.tool.watermark"] = new("Tools & Productivity", "Apply transparent diagonal text, company logos, and draft stamps.", "Watermark", "#0284C7"),
        ["frypdf.tool.redact"] = new("Security & Signatures", "Permanently sanitize sensitive text, PII, and rectangular regions.", "EyeOffOutline", "#334155"),
        ["frypdf.tool.flatten"] = new("Tools & Productivity", "Burn annotations, form fields, and layers into page background raster.", "LayersTripleOutline", "#64748B"),
        ["frypdf.tool.metadata"] = new("Tools & Productivity", "Inspect and edit XMP metadata: title, author, subject, keywords.", "CardTextOutline", "#6366F1"),

        // Convert To/From PDF
        ["frypdf.tool.excel2pdf"] = new("Conversion & Office", "Convert spreadsheet sheets, tables, and workbooks to PDF.", "FileExcelOutline", "#107C41"),
        ["frypdf.tool.pdf2excel"] = new("Conversion & Office", "Extract structured tabular data from PDF into editable XLSX.", "FileExcel", "#107C41"),
        ["frypdf.tool.word2pdf"] = new("Conversion & Office", "Convert DOCX/DOC documents preserving typography and formatting.", "FileWordOutline", "#2B579A"),
        ["frypdf.tool.pdf2word"] = new("Conversion & Office", "Reconstruct editable Word documents from fixed-layout PDFs.", "FileWord", "#2B579A"),
        ["frypdf.tool.ppt2pdf"] = new("Conversion & Office", "Export presentation slides to high-resolution vector PDF.", "FilePowerpointOutline", "#D24726"),
        ["frypdf.tool.pdf2ppt"] = new("Conversion & Office", "Convert presentation slides into editable PPTX decks.", "FilePowerpoint", "#D24726"),
        ["frypdf.tool.pdf2jpg"] = new("Conversion & Office", "Rasterize PDF pages into high-resolution JPEG images.", "FileImageOutline", "#0284C7"),
        ["frypdf.tool.jpg2pdf"] = new("Conversion & Office", "Assemble photo galleries and image scans into a PDF document.", "ImageMultipleOutline", "#0284C7"),
        ["frypdf.tool.html2pdf"] = new("Conversion & Office", "Render responsive web pages and HTML reports with CSS styling.", "LanguageHtml5", "#E44D26"),
        ["frypdf.tool.pdf2markdown"] = new("Conversion & Office", "Deconstruct PDF text into clean CommonMark/GitHub Markdown.", "LanguageMarkdown", "#0EA5E9"),
        ["frypdf.tool.pdf2pdfa"] = new("Conversion & Office", "Convert documents to ISO 19005 archiving standards (PDF/A).", "ShieldCheckOutline", "#059669"),
        ["frypdf.tool.scan2pdf"] = new("Tools & Productivity", "Acquire documents from hardware scanners and apply OCR cleanup.", "Scanner", "#64748B"),

        // Intelligence & AI Tools
        ["frypdf.tool.aisummarizer"] = new("AI & Intelligence", "Generate executive summaries and key takeaway bullet points using LLMs.", "Creation", "#8B5CF6"),
        ["frypdf.tool.translate"] = new("AI & Intelligence", "Translate document text into 50+ languages while preserving layout.", "Translate", "#3B82F6"),
        ["frypdf.tool.ocr"] = new("Tools & Productivity", "Recognize scanned text using Tesseract 5 with Indic/CJK script support.", "TextRecognition", "#059669"),
        ["frypdf.tool.compare"] = new("Tools & Productivity", "Visual side-by-side diff highlighting text and graphic alterations.", "CompareHorizontal", "#F59E0B"),
        ["frypdf.tool.edit"] = new("Tools & Productivity", "Full-fidelity canvas editing: modify text, vectors, shapes, and images.", "FileDocumentEditOutline", "#4F46E5"),

        // AI Inference Providers
        ["frypdf.ai.groq"] = new("AI & Intelligence", "Ultra-fast cloud inference provider powered by Groq LPUs.", "LightningBolt", "#F97316"),
        ["frypdf.ai.ollama"] = new("AI & Intelligence", "100% private, local on-device LLM inference via Ollama.", "Laptop", "#10B981"),
        ["frypdf.ai.openai"] = new("AI & Intelligence", "Cloud multimodal AI intelligence provider via OpenAI.", "Brain", "#06B6D4"),

        // Workspace Navigation Pages
        ["frypdf.page.home"] = new("Workspace Pages", "Main dashboard landing page with quick launch and recent files.", "HomeOutline", "#3B82F6"),
        ["frypdf.page.reader"] = new("Workspace Pages", "Dedicated full-screen PDF reader with continuous smooth scrolling.", "BookOpenPageVariantOutline", "#0284C7"),
        ["frypdf.page.new"] = new("Workspace Pages", "Template gallery for creating professional documents from scratch.", "FileDocumentPlusOutline", "#10B981"),
        ["frypdf.page.tools"] = new("Workspace Pages", "Centralized studio catalog featuring all 32 PDF tools.", "ViewGridOutline", "#8B5CF6"),
        ["frypdf.page.starred"] = new("Workspace Pages", "Quick-access shelf for your favorite and pinned tools.", "StarOutline", "#F59E0B"),
        ["frypdf.page.trash"] = new("Workspace Pages", "Cache management and disk storage cleaner for temp files.", "TrashCanOutline", "#64748B"),
        ["frypdf.page.licensing"] = new("Workspace Pages", "Open-source attribution, third-party libraries, and license info.", "License", "#64748B"),
        ["frypdf.page.fonts"] = new("Workspace Pages", "On-demand typography downloader and multi-script font packages.", "FormatFont", "#EC4899"),
        ["frypdf.page.tesseract"] = new("Workspace Pages", "OCR language pack installer for offline text recognition.", "Translate", "#059669"),
        ["frypdf.page.help"] = new("Workspace Pages", "Interactive tutorials, user manuals, and keyboard shortcut guides.", "HelpCircleOutline", "#0284C7"),
        ["frypdf.page.settings"] = new("Workspace Pages", "Application preferences, theme switching, and hardware acceleration.", "CogOutline", "#64748B"),
        ["frypdf.page.plugins"] = new("Workspace Pages", "Manage installed extensions, install packages, and browse the store.", "PuzzleOutline", "#7C3AED"),

        // Canvas Elements
        ["frypdf.element.text"] = new("Canvas Elements", "Interactive typography block with Indic/CJK script-aware rendering.", "FormatText", "#3B82F6"),
        ["frypdf.element.image"] = new("Canvas Elements", "Bitmap image container supporting PNG, JPEG, WebP, and SVG vectors.", "ImageOutline", "#10B981"),
        ["frypdf.element.shape"] = new("Canvas Elements", "Bézier vector graphics, rectangles, ellipses, and arrow callouts.", "ShapeOutline", "#F59E0B"),
        ["frypdf.element.table"] = new("Canvas Elements", "Data grid element with auto-pagination and cell styling.", "TableLarge", "#6366F1"),
        ["frypdf.element.chart"] = new("Canvas Elements", "Vector charts: bar, line, pie, donut, and scatter plots.", "ChartBar", "#EC4899"),
        ["frypdf.element.math"] = new("Canvas Elements", "LaTeX mathematical formula typesetting rendered via SkiaSharp.", "Sigma", "#8B5CF6"),
        ["frypdf.element.form"] = new("Canvas Elements", "Fillable PDF form controls: textboxes, checkboxes, and signatures.", "FormSelect", "#06B6D4"),

        // Document I/O Engines
        ["frypdf.io.questpdf"] = new("Document I/O", "High-performance vector PDF rendering and generation engine.", "FilePdfBox", "#DC2626"),
        ["frypdf.io.pdfpig"] = new("Document I/O", "Deep structural PDF deconstruction, text clustering, and font extraction.", "BookSearchOutline", "#0284C7"),
        ["frypdf.io.skia"] = new("Document I/O", "Cross-platform hardware-accelerated 2D raster and graphics pipeline.", "Draw", "#10B981"),

        // UI & Extensions
        ["frypdf.sidebar.thumbnails"] = new("UI & Extensions", "Interactive visual page thumbnail sidebar with drag reordering.", "ViewListOutline", "#64748B"),
        ["frypdf.sidebar.inspector"] = new("UI & Extensions", "Context-sensitive property inspector for selected canvas elements.", "TuneVertical", "#4F46E5"),
        ["frypdf.dialog.plugins"] = new("UI & Extensions", "Fast-access modal dialog for quickly viewing installed extensions.", "WindowMaximize", "#7C3AED"),
        ["frypdf.statusbar.core"] = new("UI & Extensions", "Status bar telemetry showing page count, zoom level, and ALC memory.", "DockBottom", "#64748B")
    };

    private static string InferCategory(IFryPlugin plugin)
    {
        if (KnownPluginMetas.TryGetValue(plugin.Id, out var meta))
            return meta.Category;

        var id = plugin.Id.ToLowerInvariant();
        if (id.Contains("page")) return "Workspace Pages";
        if (id.Contains("tool")) return "Tools & Productivity";
        if (id.Contains("element")) return "Canvas Elements";
        if (id.Contains("io") || id.Contains("documentio")) return "Document I/O";
        if (id.Contains("ai")) return "AI & Intelligence";
        if (id.Contains("security") || id.Contains("sign") || id.Contains("protect")) return "Security & Signatures";
        if (id.Contains("convert") || id.Contains("excel") || id.Contains("word") || id.Contains("ppt")) return "Conversion & Office";
        if (id.Contains("sidebar") || id.Contains("dialog") || id.Contains("status")) return "UI & Extensions";
        return "General";
    }

    private static string InferDescription(IFryPlugin plugin)
    {
        if (KnownPluginMetas.TryGetValue(plugin.Id, out var meta))
            return meta.Description;

        return $"Modular studio component '{plugin.Name}' dynamically mounted into FryPDF microkernel.";
    }

    private static string InferIcon(IFryPlugin plugin)
    {
        if (KnownPluginMetas.TryGetValue(plugin.Id, out var meta))
            return meta.Icon;

        var id = plugin.Id.ToLowerInvariant();
        if (id.Contains("ai")) return "Creation";
        if (id.Contains("ocr")) return "TextRecognition";
        if (id.Contains("tool")) return "WrenchOutline";
        if (id.Contains("element")) return "ShapeOutline";
        if (id.Contains("io")) return "FileDocumentOutline";
        if (id.Contains("page")) return "BookOpenPageVariantOutline";
        if (id.Contains("dialog")) return "WindowMaximize";
        if (id.Contains("sidebar")) return "DockRight";
        return "PuzzleOutline";
    }

    private static string InferColor(IFryPlugin plugin)
    {
        if (KnownPluginMetas.TryGetValue(plugin.Id, out var meta))
            return meta.Color;

        var id = plugin.Id.ToLowerInvariant();
        if (id.Contains("ai")) return "#8B5CF6";
        if (id.Contains("ocr")) return "#10B981";
        if (id.Contains("tool")) return "#3B82F6";
        if (id.Contains("element")) return "#EC4899";
        if (id.Contains("io")) return "#F59E0B";
        if (id.Contains("page")) return "#0284C7";
        return "#7C3AED";
    }
}
