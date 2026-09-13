using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Services.Plugins;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PluginsManagerPageTests
{
    private ServiceProvider CreateTestServices(string? testDir = null)
    {
        var services = new ServiceCollection();
        App.ConfigureServices(services);
        if (testDir != null)
        {
            var storePath = Path.Combine(testDir, "installed_plugins.json");
            services.AddSingleton<IInstalledPluginStore>(new FileInstalledPluginStore(storePath));
            services.AddSingleton<IPluginMarketplaceService>(sp =>
            {
                var host = sp.GetRequiredService<PluginHost>();
                var overlay = sp.GetService<IOverlayRegistry>();
                var store = sp.GetRequiredService<IInstalledPluginStore>();
                var pluginsDir = Path.Combine(testDir, "plugins");
                return new PluginMarketplaceService(host, overlay, store, null, null, pluginsDir);
            });
        }
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task PluginsManager_LoadsAllInstalledPluginsAndCategorizesCorrectly()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();

        // Mount domain bundles
        var bundles = new IFryPluginBundle[]
        {
            new ToolsOrganizeBundle(),
            new ToolsSecurityBundle(),
            new ToolsConversionBundle(),
            new ToolsIntelligenceBundle(),
            new DataStudioBundle()
        };

        var profile = new PluginProfile
        {
            ProfileName = "desktop",
            Bundles = bundles.Select(b => b.Id).ToList()
        };
        ProfileLoader.ApplyProfile(profile, host, bundles);
        await host.StartAsync();

        var toolRegistry = sp.GetRequiredService<IPdfToolRegistry>();
        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();

        var vm = new PluginsManagerViewModel(host, marketplace, toolRegistry);
        await vm.LoadAllDataAsync();

        Assert.Equal(33, vm.InstalledCount);
        Assert.Equal(33, vm.ActiveInstalledCount);
        Assert.NotEmpty(vm.FilteredInstalledPlugins);

        // Verify Categories
        var categories = vm.FilteredInstalledPlugins.Select(p => p.Category).Distinct().ToList();
        Assert.Contains("Tools & Productivity", categories);

        // Verify specific plugins are present
        Assert.Contains(vm.FilteredInstalledPlugins, p => p.Id == "frypdf.tool.merge");
        Assert.Contains(vm.FilteredInstalledPlugins, p => p.Id == "frypdf.tool.compress");

        await host.StopAsync();
    }

    [Fact]
    public async Task PluginsManager_SearchAndFilter_WorksAcrossBothInstalledAndMarketplace()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var bundles = new IFryPluginBundle[] { new ToolsOrganizeBundle(), new ToolsSecurityBundle() };
        var profile = new PluginProfile { ProfileName = "desktop", Bundles = bundles.Select(b => b.Id).ToList() };
        ProfileLoader.ApplyProfile(profile, host, bundles);
        await host.StartAsync();

        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();
        var vm = new PluginsManagerViewModel(host, marketplace);
        await vm.LoadAllDataAsync();

        // 1. Search Installed
        vm.SearchQuery = "merge";
        Assert.Single(vm.FilteredInstalledPlugins);
        Assert.Equal("frypdf.tool.merge", vm.FilteredInstalledPlugins[0].Id);

        // 2. Clear Search
        vm.SearchQuery = "";
        Assert.Equal(13, vm.FilteredInstalledPlugins.Count);

        // 3. Switch to Marketplace Tab & Search
        vm.SelectedTab = PluginsManagerTab.Marketplace;
        Assert.NotEmpty(vm.FilteredMarketplacePlugins);

        vm.SearchQuery = "Snake";
        Assert.Single(vm.FilteredMarketplacePlugins);
        Assert.Equal("frypdf.overlay.snake", vm.FilteredMarketplacePlugins[0].Id);

        vm.SearchQuery = "Arcade";
        Assert.True(vm.FilteredMarketplacePlugins.Count >= 2);
        Assert.Contains(vm.FilteredMarketplacePlugins, m => m.Id == "frypdf.overlay.snake");
        Assert.Contains(vm.FilteredMarketplacePlugins, m => m.Id == "com.frypdf.plugin.tictactoe");

        // 4. Filter by Category
        vm.SearchQuery = "";
        vm.SelectedCategory = "UI & Extensions";
        Assert.Contains(vm.FilteredMarketplacePlugins, m => m.Id == "frypdf.overlay.snake");

        await host.StopAsync();
    }

    [Fact]
    public async Task PluginsManager_Selection_PopulatesDetailViewModelAndContributions()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var bundles = new IFryPluginBundle[] { new ToolsConversionBundle() };
        var profile = new PluginProfile { ProfileName = "desktop", Bundles = bundles.Select(b => b.Id).ToList() };
        ProfileLoader.ApplyProfile(profile, host, bundles);
        await host.StartAsync();

        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();
        var vm = new PluginsManagerViewModel(host, marketplace);
        await vm.LoadAllDataAsync();

        Assert.NotEmpty(vm.FilteredInstalledPlugins);
        var target = vm.FilteredInstalledPlugins.First(p => p.Id == "frypdf.tool.pdftoword");

        // Switch to Installed tab to select an installed plugin
        vm.SelectedTab = PluginsManagerTab.Installed;
        vm.SelectedInstalledPlugin = target;
        Assert.NotNull(vm.SelectedDetail);
        Assert.Equal("frypdf.tool.pdftoword", vm.SelectedDetail.Id);
        Assert.Equal(target.Name, vm.SelectedDetail.Name);
        Assert.True(vm.SelectedDetail.IsInstalled);
        Assert.True(vm.SelectedDetail.IsActive);
        Assert.NotEmpty(vm.SelectedDetail.ContributedFeatures);
        Assert.NotEmpty(vm.SelectedDetail.Highlights);
        Assert.NotEmpty(vm.SelectedDetail.Dependencies);

        // Test Sub-Tab navigation
        vm.SelectedDetail.SelectTab("Contributions");
        Assert.Equal(ExtensionDetailTab.Contributions, vm.SelectedDetail.SelectedTab);

        vm.SelectedDetail.SelectTab("Runtime");
        Assert.Equal(ExtensionDetailTab.Runtime, vm.SelectedDetail.SelectedTab);
        Assert.Contains("Active", vm.SelectedDetail.RuntimeStatus);

        await host.StopAsync();
    }

    [Fact]
    public async Task PluginsManager_DefaultTabIsStore_AndButtonsReflectStateAccurately()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var bundles = new IFryPluginBundle[] { new ToolsOrganizeBundle() };
        var profile = new PluginProfile { ProfileName = "desktop", Bundles = bundles.Select(b => b.Id).ToList() };
        ProfileLoader.ApplyProfile(profile, host, bundles);
        await host.StartAsync();

        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();
        var vm = new PluginsManagerViewModel(host, marketplace);
        await vm.LoadAllDataAsync();

        // 1. First tab is Store (Marketplace)
        Assert.Equal(PluginsManagerTab.Marketplace, vm.SelectedTab);
        Assert.NotNull(vm.SelectedDetail);

        // 2. Uninstalled Store Item button state verification
        var uninstalledItem = vm.FilteredMarketplacePlugins.FirstOrDefault(m => !m.IsInstalled);
        Assert.NotNull(uninstalledItem);
        vm.SelectedMarketplacePlugin = uninstalledItem;

        var detail = vm.SelectedDetail;
        Assert.NotNull(detail);
        Assert.False(detail.IsInstalled);
        Assert.True(detail.CanInstall);
        Assert.False(detail.CanUninstall); // Critical fix: NO delete button for uninstalled store items!
        Assert.False(detail.CanToggleActive);
        Assert.Equal("Available in Store", detail.RuntimeStatus);

        // 3. Installed System Built-in plugin button state verification
        vm.SelectedTab = PluginsManagerTab.Installed;
        var mergePlugin = vm.FilteredInstalledPlugins.First(p => p.Id == "frypdf.tool.merge");
        vm.SelectedInstalledPlugin = mergePlugin;

        detail = vm.SelectedDetail;
        Assert.NotNull(detail);
        Assert.True(detail.IsInstalled);
        Assert.True(detail.IsSystemBuiltIn);
        Assert.False(detail.CanUninstall); // System built-in cannot be uninstalled
        Assert.True(detail.CanToggleActive); // Can be enabled/disabled
        Assert.True(detail.IsActiveAndInstalled);

        await host.StopAsync();
    }

    [Fact]
    public async Task PluginsManager_InstallProgressAndStatus_UpdatesDuringInstallation()
    {
        var tempDir = Path.Combine(AppContext.BaseDirectory, $"frypdf_progress_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var sp = CreateTestServices(tempDir);
            var host = sp.GetRequiredService<PluginHost>();
            var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();

            var vm = new PluginsManagerViewModel(host, marketplace);
            await vm.LoadAllDataAsync();

            var snake = vm.FilteredMarketplacePlugins.ToList().First(m => m.Id == "frypdf.overlay.snake");
            vm.SelectedMarketplacePlugin = snake;

            // Before install
            Assert.False(vm.IsInstalling);
            Assert.Equal(0, vm.InstallProgress);
            Assert.False(vm.SelectedDetail!.IsInstalling);

            // Trigger install command
            var installTask = vm.InstallMarketplacePluginCommand.ExecuteAsync("frypdf.overlay.snake");
            await installTask;

            // After install finishes
            Assert.False(vm.IsInstalling);
            Assert.True(marketplace.IsPluginInstalled("frypdf.overlay.snake"));

            // Re-select installed Snake in marketplace
            vm.SelectedMarketplacePlugin = vm.FilteredMarketplacePlugins.ToList().First(m => m.Id == "frypdf.overlay.snake");
            var detail = vm.SelectedDetail!;
            Assert.True(detail.IsInstalled);
            Assert.True(detail.IsExternal);
            Assert.True(detail.CanUninstall); // External installed items CAN be uninstalled

            await marketplace.UninstallPluginAsync("frypdf.overlay.snake");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task PluginsManager_Marketplace_InstallsAndMountsRealPluginIntoHost()
    {
        var tempDir = Path.Combine(AppContext.BaseDirectory, $"frypdf_pm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var sp = CreateTestServices(tempDir);
            var host = sp.GetRequiredService<PluginHost>();
            var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();

            var vm = new PluginsManagerViewModel(host, marketplace);
            await vm.LoadAllDataAsync();

            vm.SelectedTab = PluginsManagerTab.Marketplace;
            var snakeItem = vm.FilteredMarketplacePlugins.First(m => m.Id == "frypdf.overlay.snake");
            vm.SelectedMarketplacePlugin = snakeItem;

            Assert.NotNull(vm.SelectedDetail);
            Assert.Equal("frypdf.overlay.snake", vm.SelectedDetail.Id);
            Assert.Equal("FryPDF Team", vm.SelectedDetail.Publisher);
            Assert.True(vm.SelectedDetail.IsOfficial);
            Assert.True(vm.SelectedDetail.IsVerified);

            // Verify initially not active in host
            Assert.False(host.IsPluginActive("frypdf.overlay.snake"));

            // Perform 1-click install: mounts real SnakeGamePlugin into PluginHost and activates it!
            await vm.InstallMarketplacePluginCommand.ExecuteAsync("frypdf.overlay.snake");

            Assert.True(marketplace.IsPluginInstalled("frypdf.overlay.snake"));
            Assert.True(host.IsPluginActive("frypdf.overlay.snake"));

            // Clean up: uninstalls and disables from host
            await marketplace.UninstallPluginAsync("frypdf.overlay.snake");
            Assert.False(marketplace.IsPluginInstalled("frypdf.overlay.snake"));
            Assert.False(host.IsPluginActive("frypdf.overlay.snake"));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task WorkspacePagesBundle_RegistersPluginsPageDescriptorCorrectly()
    {
        var sp = CreateTestServices();
        var navRegistry = sp.GetRequiredService<PdfEditorApp.Core.Plugins.Descriptors.INavigationRegistry>();

        var bundle = new WorkspacePagesBundle();
        Assert.Contains(bundle.Plugins, p => p.Id == "frypdf.page.plugins");

        var ctx = sp.GetRequiredService<IFryPluginContext>();
        var plugin = bundle.Plugins.First(p => p.Id == "frypdf.page.plugins");
        await plugin.ApplyAsync(ctx);

        var item = navRegistry.GetItem("Plugins");
        Assert.NotNull(item);
        Assert.Equal("Plugins & Extensions", item.Title);
        Assert.Equal("Preferences", item.Group);
        Assert.Equal("PuzzleOutline", item.IconKind);
        Assert.NotNull(item.ViewFactory);
    }

    [Fact]
    public void HomeViewModel_NavigatesToPluginsSectionCorrectly()
    {
        var sp = CreateTestServices();
        var home = sp.GetRequiredService<HomeViewModel>();

        Assert.NotNull(home.PluginsManager);

        home.SelectNavSectionCommand.Execute("Plugins");

        Assert.True(home.IsPluginsSection);
        Assert.Equal(PdfEditorApp.Core.Models.HomeNavSection.Plugins, home.SelectedNavSection);
    }

    [Fact]
    public void PluginsManagerDetail_OverlayTypeDetection_And_LaunchCommand_WorkAsExpected()
    {
        // 1. Overlay detection by ID, tags, and known plugins
        Assert.True(PluginsManagerDetailViewModel.DetermineIsOverlayType("com.frypdf.plugin.chess"));
        Assert.True(PluginsManagerDetailViewModel.DetermineIsOverlayType("frypdf.overlay.snake"));
        Assert.True(PluginsManagerDetailViewModel.DetermineIsOverlayType("frypdf.overlay.scratchpad"));
        Assert.True(PluginsManagerDetailViewModel.DetermineIsOverlayType("com.frypdf.plugin.tictactoe"));
        Assert.True(PluginsManagerDetailViewModel.DetermineIsOverlayType("custom.plugin", tags: new[] { "overlay", "utility" }));
        Assert.True(PluginsManagerDetailViewModel.DetermineIsOverlayType("custom.plugin", features: new[] { "Shell Overlay: Floating Card" }));

        // Non-overlay modules should return false
        Assert.False(PluginsManagerDetailViewModel.DetermineIsOverlayType("frypdf.tool.merge", tags: new[] { "pdf", "tool" }));
        Assert.False(PluginsManagerDetailViewModel.DetermineIsOverlayType("frypdf.element.text"));

        // 2. CanLaunch flag logic
        var installedOverlay = new MarketplacePluginItem
        {
            Id = "com.frypdf.plugin.chess",
            Name = "Chess",
            Publisher = "Code Fry Dev",
            Version = "1.0.0",
            Description = "Interactive Chess shell overlay",
            Category = "UI & Extensions",
            Tags = new[] { "game", "chess", "overlay" },
            Status = MarketplacePluginStatus.Installed
        };

        var detailVm = PluginsManagerDetailViewModel.FromMarketplaceItem(installedOverlay);
        Assert.True(detailVm.IsOverlayType);
        Assert.True(detailVm.IsInstalled);
        Assert.True(detailVm.CanLaunch);

        // Non-installed overlay cannot launch yet
        installedOverlay.Status = MarketplacePluginStatus.Available;
        var uninstalledDetail = PluginsManagerDetailViewModel.FromMarketplaceItem(installedOverlay);
        Assert.True(uninstalledDetail.IsOverlayType);
        Assert.False(uninstalledDetail.IsInstalled);
        Assert.False(uninstalledDetail.CanLaunch);

        // 3. LaunchCommand invokes callback
        string launchedId = string.Empty;
        detailVm.LaunchCallback = id =>
        {
            launchedId = id;
            return Task.CompletedTask;
        };

        detailVm.LaunchCommand.Execute(null);
        Assert.Equal("com.frypdf.plugin.chess", launchedId);
    }
}
