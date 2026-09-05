using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Plugins.Snake;
using PdfEditorApp.Services.Overlays;
using PdfEditorApp.Services.Palette;
using PdfEditorApp.Services.Plugins;
using PdfEditorApp.Services.Ribbon;
using PdfEditorApp.Services.StatusBar;
using Xunit;

namespace PdfEditorApp.Tests;

public class SnakeMarketplaceIntegrationTests
{
    private ServiceProvider CreateTestServices()
    {
        var services = new ServiceCollection();
        App.ConfigureServices(services);
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test_installed_{Guid.NewGuid():N}.json");
        services.AddSingleton<IInstalledPluginStore>(new FileInstalledPluginStore(tempFile));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SnakePlugin_IsNotInstalledByDefault_AndInstallsAndUninstallsViaMarketplace()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var overlayReg = sp.GetRequiredService<OverlayRegistry>();
        var commandReg = sp.GetRequiredService<ICommandPaletteRegistry>();
        var statusReg = sp.GetRequiredService<IStatusBarRegistry>();
        var ribbonReg = sp.GetRequiredService<IRibbonRegistry>();
        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();

        // 1. Mount standard profile bundles (does NOT include ShellOverlaysBundle)
        var bundles = new IFryPluginBundle[]
        {
            new ToolsOrganizeBundle(),
            new StatusBarBundle(),
            new CommandPaletteBundle()
        };

        var profile = new PluginProfile
        {
            ProfileName = "desktop",
            Bundles = bundles.Select(b => b.Id).ToList()
        };
        ProfileLoader.ApplyProfile(profile, host, bundles);
        await host.StartAsync();

        // 2. Verify Snake Game is NOT installed or active by default
        Assert.False(host.IsPluginActive("frypdf.overlay.snake"));
        Assert.False(marketplace.IsPluginInstalled("frypdf.overlay.snake"));
        Assert.Null(overlayReg.GetOverlay("frypdf.overlay.snake"));
        Assert.False(overlayReg.IsOverlayVisible("frypdf.overlay.snake"));

        // 3. Verify Snake Game and companion overlays are available in the Marketplace catalog
        var catalog = await marketplace.GetCatalogAsync();
        Assert.Equal(3, catalog.Count);
        var snakeItem = catalog.First(c => c.Id == "frypdf.overlay.snake");
        Assert.Equal(MarketplacePluginStatus.Available, snakeItem.Status);
        Assert.Equal("Retro Arcade Snake Game (Shell Overlay)", snakeItem.Name);

        Assert.Contains(catalog, c => c.Id == "frypdf.overlay.scratchpad");
        Assert.Contains(catalog, c => c.Id == "frypdf.overlay.telemetry");

        // 4. Install plugin via Marketplace
        bool installed = await marketplace.InstallPluginAsync("frypdf.overlay.snake");
        Assert.True(installed);

        // 5. Verify plugin is now active in host and overlay is registered and visible
        Assert.True(host.IsPluginActive("frypdf.overlay.snake"));
        Assert.True(marketplace.IsPluginInstalled("frypdf.overlay.snake"));
        Assert.NotNull(overlayReg.GetOverlay("frypdf.overlay.snake"));
        Assert.True(overlayReg.IsOverlayVisible("frypdf.overlay.snake"));
        Assert.Single(overlayReg.ActiveOverlays);

        // Verify status in catalog updated to Installed
        catalog = await marketplace.GetCatalogAsync();
        Assert.Equal(MarketplacePluginStatus.Installed, catalog.First(c => c.Id == "frypdf.overlay.snake").Status);

        // Verify capabilities registered
        Assert.Contains(commandReg.GetAllCommands(), c => c.Id == "cmd.overlay.snake");
        Assert.Contains(statusReg.GetWidgets(StatusBarAlignment.Right), w => w.WidgetId == "frypdf.status.snake");
        Assert.Contains(ribbonReg.GetActionsForTab("view"), a => a.Id == "frypdf.ribbon.action.snake");

        // 6. Test Persistence & Auto-Restore on simulated App Restart
        var store = sp.GetRequiredService<IInstalledPluginStore>();
        Assert.True(store.IsInstalled("frypdf.overlay.snake"));

        var newHost = new PluginHost(new FryPluginContext(sp));
        var newMarketplace = new PluginMarketplaceService(newHost, overlayReg, store);
        Assert.True(newMarketplace.IsPluginInstalled("frypdf.overlay.snake"));
        Assert.True(newHost.IsPluginActive("frypdf.overlay.snake"));

        // 7. Uninstall plugin via Marketplace
        bool uninstalled = await marketplace.UninstallPluginAsync("frypdf.overlay.snake");
        Assert.True(uninstalled);
        Assert.False(store.IsInstalled("frypdf.overlay.snake"));

        // 8. Verify plugin deactivated and cleanly unmounted
        Assert.False(host.IsPluginActive("frypdf.overlay.snake"));
        Assert.False(marketplace.IsPluginInstalled("frypdf.overlay.snake"));
        Assert.False(overlayReg.IsOverlayVisible("frypdf.overlay.snake"));
        Assert.Empty(overlayReg.ActiveOverlays);

        // Verify status in catalog returned to Available
        catalog = await marketplace.GetCatalogAsync();
        Assert.Equal(MarketplacePluginStatus.Available, catalog.First(c => c.Id == "frypdf.overlay.snake").Status);

        // Verify effects rolled back
        Assert.DoesNotContain(commandReg.GetAllCommands(), c => c.Id == "cmd.overlay.snake");
        Assert.DoesNotContain(statusReg.GetWidgets(StatusBarAlignment.Right), w => w.WidgetId == "frypdf.status.snake");
        Assert.DoesNotContain(ribbonReg.GetActionsForTab("view"), a => a.Id == "frypdf.ribbon.action.snake");

        await host.StopAsync();
    }
}
