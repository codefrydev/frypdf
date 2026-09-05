using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
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
    private ServiceProvider CreateTestServices()
    {
        var services = new ServiceCollection();
        App.ConfigureServices(services);
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

        vm.SearchQuery = "Gemini";
        Assert.Single(vm.FilteredMarketplacePlugins);
        Assert.Equal("gemini.pdf.studio", vm.FilteredMarketplacePlugins[0].Id);

        vm.SearchQuery = "LaTeX";
        Assert.Single(vm.FilteredMarketplacePlugins);
        Assert.Equal("latex.math.renderer", vm.FilteredMarketplacePlugins[0].Id);

        // 4. Filter by Category
        vm.SearchQuery = "";
        vm.SelectedCategory = "Canvas Elements";
        Assert.Contains(vm.FilteredMarketplacePlugins, m => m.Id == "latex.math.renderer");
        Assert.Contains(vm.FilteredMarketplacePlugins, m => m.Id == "barcode.qr.pro");

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
    public async Task PluginsManager_Marketplace_SimulatesInstallAndMountsIntoHost()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();

        var vm = new PluginsManagerViewModel(host, marketplace);
        await vm.LoadAllDataAsync();

        vm.SelectedTab = PluginsManagerTab.Marketplace;
        var geminiItem = vm.FilteredMarketplacePlugins.First(m => m.Id == "gemini.pdf.studio");
        vm.SelectedMarketplacePlugin = geminiItem;

        Assert.NotNull(vm.SelectedDetail);
        Assert.Equal("gemini.pdf.studio", vm.SelectedDetail.Id);
        Assert.Equal("Google DeepMind / FryPDF", vm.SelectedDetail.Publisher);
        Assert.True(vm.SelectedDetail.IsOfficial);
        Assert.True(vm.SelectedDetail.IsVerified);

        // Perform 1-click install
        await vm.InstallMarketplacePluginCommand.ExecuteAsync("gemini.pdf.studio");

        Assert.True(marketplace.IsPluginInstalled("gemini.pdf.studio"));

        // Clean up
        await marketplace.UninstallPluginAsync("gemini.pdf.studio");
        Assert.False(marketplace.IsPluginInstalled("gemini.pdf.studio"));
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
}
