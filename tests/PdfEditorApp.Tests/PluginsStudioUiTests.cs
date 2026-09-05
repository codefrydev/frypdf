using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PluginsStudioUiTests
{
    [Fact]
    public async Task PluginsDialog_PopulatesAndFiltersPluginsCorrectly()
    {
        // 1. Setup DI container with plugin host and bundles
        var services = new ServiceCollection();
        App.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        var host = sp.GetRequiredService<PluginHost>();

        // Mount all 5 domain bundles
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

        // 2. Resolve MainViewModel
        var mainVm = sp.GetRequiredService<MainViewModel>();
        mainVm.PluginHost = host;

        // 3. Open Plugins Dialog
        mainVm.OpenPluginsDialogCommand.Execute(null);

        Assert.True(mainVm.IsPluginsDialogOpen);
        Assert.NotEmpty(mainVm.FilteredPluginsList);
        Assert.Equal(33, mainVm.FilteredPluginsList.Count);

        // Verify specific plugins
        Assert.Contains(mainVm.FilteredPluginsList, p => p.Id == "frypdf.tool.merge");
        Assert.Contains(mainVm.FilteredPluginsList, p => p.Id == "frypdf.tool.compress");
        Assert.Contains(mainVm.FilteredPluginsList, p => p.Id == "frypdf.tool.aisummarizer");

        // 4. Test Search Filtering
        mainVm.PluginSearchQuery = "merge";
        Assert.Equal(2, mainVm.FilteredPluginsList.Count); // Merge PDF and Batch Mail Merge
        Assert.Contains(mainVm.FilteredPluginsList, p => p.Id == "frypdf.tool.merge");
        Assert.Contains(mainVm.FilteredPluginsList, p => p.Id == "frypdf.tool.batchgeneration");

        mainVm.PluginSearchQuery = "compress";
        Assert.Single(mainVm.FilteredPluginsList);
        Assert.Equal("frypdf.tool.compress", mainVm.FilteredPluginsList[0].Id);

        // 5. Clear Search
        mainVm.PluginSearchQuery = "";
        Assert.Equal(33, mainVm.FilteredPluginsList.Count);

        // 6. Close Dialog
        mainVm.ClosePluginsDialogCommand.Execute(null);
        Assert.False(mainVm.IsPluginsDialogOpen);

        await host.StopAsync();
    }

    [Fact]
    public async Task PluginsDialog_WithAll16Bundles_DisplaysRichCategorizedPlugins()
    {
        var services = new ServiceCollection();
        App.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        var host = sp.GetRequiredService<PluginHost>();

        var bundles = new IFryPluginBundle[]
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

        var profile = new PluginProfile
        {
            ProfileName = "desktop",
            Bundles = bundles.Select(b => b.Id).ToList()
        };
        ProfileLoader.ApplyProfile(profile, host, bundles);
        await host.StartAsync();

        var mainVm = sp.GetRequiredService<MainViewModel>();
        mainVm.PluginHost = host;

        mainVm.OpenPluginsDialogCommand.Execute(null);

        Assert.True(mainVm.IsPluginsDialogOpen);
        Assert.True(mainVm.FilteredPluginsList.Count >= 70, $"Expected >= 70 plugins but found {mainVm.FilteredPluginsList.Count}");

        // Verify categories exist across diverse subsystems
        var categories = mainVm.FilteredPluginsList.Select(p => p.Category).Distinct().ToList();
        Assert.Contains("Workspace Navigation", categories);
        Assert.Contains("Modal Studios & Dialogs", categories);
        Assert.Contains("Editor Sidebars", categories);
        Assert.Contains("Canvas Elements", categories);
        Assert.Contains("Document I/O", categories);
        Assert.Contains("AI Providers", categories);
        Assert.Contains("OCR Engines", categories);
        Assert.Contains("Document Templates", categories);
        Assert.Contains("Status Bar Widgets", categories);
        Assert.Contains("Property Inspector", categories);
        Assert.Contains("Command Palette", categories);

        // Test searching for specific subsystems
        mainVm.PluginSearchQuery = "ai";
        Assert.NotEmpty(mainVm.FilteredPluginsList);
        Assert.Contains(mainVm.FilteredPluginsList, p => p.Id.Contains("ai"));

        mainVm.PluginSearchQuery = "sidebar";
        Assert.NotEmpty(mainVm.FilteredPluginsList);
        Assert.Contains(mainVm.FilteredPluginsList, p => p.Category == "Editor Sidebars");

        mainVm.PluginSearchQuery = "dialog";
        Assert.NotEmpty(mainVm.FilteredPluginsList);
        Assert.Contains(mainVm.FilteredPluginsList, p => p.Category == "Modal Studios & Dialogs");

        mainVm.ClosePluginsDialogCommand.Execute(null);
        Assert.False(mainVm.IsPluginsDialogOpen);

        await host.StopAsync();
    }
}
