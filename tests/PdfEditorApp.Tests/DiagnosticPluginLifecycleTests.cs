using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Models;
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

public class DiagnosticPluginLifecycleTests
{
    private ServiceProvider CreateTestServices()
    {
        var services = new ServiceCollection();
        App.ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task DesktopProfile_DisablesDiagnosticPluginByDefault()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var navRegistry = sp.GetRequiredService<INavigationRegistry>();

        var profilePath = Path.Combine(AppContext.BaseDirectory, "profiles", "desktop.profile.json");
        if (!File.Exists(profilePath))
        {
            profilePath = "profiles/desktop.profile.json";
        }

        Assert.True(File.Exists(profilePath), $"Profile file must exist at {profilePath}");
        var profile = ProfileLoader.LoadFromFile(profilePath);

        Assert.Contains("frypdf.page.diagnosticlogs", profile.DisabledPlugins);
        Assert.False(profile.IsPluginEnabled("frypdf.page.diagnosticlogs"));

        var bundle = new WorkspacePagesBundle();
        ProfileLoader.ApplyProfile(profile, host, new IFryPluginBundle[] { bundle });

        await host.StartAsync();

        // DiagnosticLogs plugin is registered but suspended/inactive
        Assert.Contains(host.RegisteredPlugins, p => p.Id == "frypdf.page.diagnosticlogs");
        Assert.False(host.IsPluginActive("frypdf.page.diagnosticlogs"));
        Assert.Equal(PluginState.Suspended, host.GetPluginState("frypdf.page.diagnosticlogs"));

        // NavigationRegistry must NOT contain DiagnosticLogs by default
        Assert.Null(navRegistry.GetItem("DiagnosticLogs"));

        await host.StopAsync();
    }

    [Fact]
    public async Task EnableDiagnosticPlugin_MountsAndAddsToNavigation()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var navRegistry = sp.GetRequiredService<INavigationRegistry>();

        var profile = new PluginProfile
        {
            ProfileName = "desktop",
            Bundles = new[] { "FryPdf.Bundle.WorkspacePages" }.ToList(),
            DisabledPlugins = new[] { "frypdf.page.diagnosticlogs" }.ToList()
        };

        var bundle = new WorkspacePagesBundle();
        ProfileLoader.ApplyProfile(profile, host, new IFryPluginBundle[] { bundle });
        await host.StartAsync();

        Assert.False(host.IsPluginActive("frypdf.page.diagnosticlogs"));
        Assert.Null(navRegistry.GetItem("DiagnosticLogs"));

        // Enable the plugin dynamically
        await host.EnablePluginAsync("frypdf.page.diagnosticlogs");

        Assert.True(host.IsPluginActive("frypdf.page.diagnosticlogs"));
        Assert.Equal(PluginState.Active, host.GetPluginState("frypdf.page.diagnosticlogs"));

        // Must now be present in NavigationRegistry with correct metadata
        var navItem = navRegistry.GetItem("DiagnosticLogs");
        Assert.NotNull(navItem);
        Assert.Equal("Diagnostic Logs", navItem!.Title);
        Assert.Equal("Library", navItem.Group);
        Assert.Equal(155, navItem.Order);

        await host.StopAsync();
    }

    [Fact]
    public async Task DisableDiagnosticPlugin_UnmountsAndRemovesFromNavigation()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var navRegistry = sp.GetRequiredService<INavigationRegistry>();

        var profile = new PluginProfile
        {
            ProfileName = "desktop",
            Bundles = new[] { "FryPdf.Bundle.WorkspacePages" }.ToList(),
            DisabledPlugins = new[] { "frypdf.page.diagnosticlogs" }.ToList()
        };

        var bundle = new WorkspacePagesBundle();
        ProfileLoader.ApplyProfile(profile, host, new IFryPluginBundle[] { bundle });
        await host.StartAsync();

        // Enable then disable
        await host.EnablePluginAsync("frypdf.page.diagnosticlogs");
        Assert.NotNull(navRegistry.GetItem("DiagnosticLogs"));

        await host.DisablePluginAsync("frypdf.page.diagnosticlogs");

        Assert.False(host.IsPluginActive("frypdf.page.diagnosticlogs"));
        Assert.Equal(PluginState.Suspended, host.GetPluginState("frypdf.page.diagnosticlogs"));
        Assert.Null(navRegistry.GetItem("DiagnosticLogs"));

        await host.StopAsync();
    }

    [Fact]
    public async Task PluginsManager_ShowsDiagnosticPluginAsDisabledByDefault_AndCanToggle()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var navRegistry = sp.GetRequiredService<INavigationRegistry>();
        var toolRegistry = sp.GetRequiredService<IPdfToolRegistry>();
        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();

        var profile = new PluginProfile
        {
            ProfileName = "desktop",
            Bundles = new[] { "FryPdf.Bundle.WorkspacePages" }.ToList(),
            DisabledPlugins = new[] { "frypdf.page.diagnosticlogs" }.ToList()
        };

        var bundle = new WorkspacePagesBundle();
        ProfileLoader.ApplyProfile(profile, host, new IFryPluginBundle[] { bundle });
        await host.StartAsync();

        var vm = new PluginsManagerViewModel(host, marketplace, toolRegistry);
        await vm.LoadAllDataAsync();

        // Plugin should be listed under Installed plugins
        var diagItem = vm.FilteredInstalledPlugins.FirstOrDefault(p => p.Id == "frypdf.page.diagnosticlogs");
        Assert.NotNull(diagItem);
        Assert.False(diagItem!.IsActive);
        Assert.Equal("Workspace Pages", diagItem.Category);

        // Toggle on
        diagItem.IsActive = true;
        // Small delay for async toggle task
        await Task.Delay(50);

        Assert.True(host.IsPluginActive("frypdf.page.diagnosticlogs"));
        Assert.NotNull(navRegistry.GetItem("DiagnosticLogs"));

        // Toggle off
        diagItem.IsActive = false;
        await Task.Delay(50);

        Assert.False(host.IsPluginActive("frypdf.page.diagnosticlogs"));
        Assert.Null(navRegistry.GetItem("DiagnosticLogs"));

        await host.StopAsync();
    }

    [Fact]
    public async Task HomeViewModel_FallsBackToHome_WhenDiagnosticPluginDisabledWhileActive()
    {
        var sp = CreateTestServices();
        var host = sp.GetRequiredService<PluginHost>();
        var navRegistry = sp.GetRequiredService<INavigationRegistry>();

        var profile = new PluginProfile
        {
            ProfileName = "desktop",
            Bundles = new[] { "FryPdf.Bundle.WorkspacePages" }.ToList(),
            DisabledPlugins = new[] { "frypdf.page.diagnosticlogs" }.ToList()
        };

        var bundle = new WorkspacePagesBundle();
        ProfileLoader.ApplyProfile(profile, host, new IFryPluginBundle[] { bundle });
        await host.StartAsync();

        // Enable diagnostic plugin
        await host.EnablePluginAsync("frypdf.page.diagnosticlogs");

        var homeVm = sp.GetRequiredService<HomeViewModel>();
        homeVm.RefreshDynamicNavigationItems();

        // Navigate to DiagnosticLogs
        homeVm.SelectNavSection("DiagnosticLogs");
        Assert.Equal(HomeNavSection.DiagnosticLogs, homeVm.SelectedNavSection);

        // Now disable the plugin while user was on DiagnosticLogs
        await host.DisablePluginAsync("frypdf.page.diagnosticlogs");

        // HomeViewModel must have gracefully fallen back to Home
        Assert.Equal(HomeNavSection.Home, homeVm.SelectedNavSection);
        Assert.Null(homeVm.DynamicPageView);

        await host.StopAsync();
    }

    [Fact]
    public void ProfileLoader_SaveToFile_PersistsDisabledAndEnabledState()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_profile_{Guid.NewGuid():N}.json");
        try
        {
            var profile = new PluginProfile
            {
                ProfileName = "desktop",
                Bundles = new[] { "FryPdf.Bundle.WorkspacePages" }.ToList(),
                DisabledPlugins = new[] { "frypdf.page.diagnosticlogs" }.ToList()
            };

            ProfileLoader.SaveToFile(profile, tempFile);

            var reloaded = ProfileLoader.LoadFromFile(tempFile);
            Assert.Equal("desktop", reloaded.ProfileName);
            Assert.Contains("frypdf.page.diagnosticlogs", reloaded.DisabledPlugins);
            Assert.False(reloaded.IsPluginEnabled("frypdf.page.diagnosticlogs"));

            // Toggle plugin to enabled
            reloaded.DisabledPlugins.Remove("frypdf.page.diagnosticlogs");
            reloaded.EnabledPlugins.Add("frypdf.page.diagnosticlogs");
            ProfileLoader.SaveToFile(reloaded, tempFile);

            var reloaded2 = ProfileLoader.LoadFromFile(tempFile);
            Assert.DoesNotContain("frypdf.page.diagnosticlogs", reloaded2.DisabledPlugins);
            Assert.Contains("frypdf.page.diagnosticlogs", reloaded2.EnabledPlugins);
            Assert.True(reloaded2.IsPluginEnabled("frypdf.page.diagnosticlogs"));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
