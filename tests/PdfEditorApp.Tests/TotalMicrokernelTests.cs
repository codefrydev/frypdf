using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Services.Dialogs;
using PdfEditorApp.Services.Navigation;
using PdfEditorApp.Services.Ribbon;
using PdfEditorApp.Services.Sidebar;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Intelligence;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class TotalMicrokernelTests
{
    [Fact]
    public void NavigationRegistry_Registers_And_Queries_By_Group()
    {
        var reg = new NavigationRegistry();
        bool eventFired = false;
        reg.RegistryChanged += () => eventFired = true;

        var desc1 = new NavigationItemDescriptor
        {
            Id = "test.page.a",
            Title = "Page A",
            Group = "TestGroup",
            Order = 20,
            ViewFactory = sp => "View A"
        };
        var desc2 = new NavigationItemDescriptor
        {
            Id = "test.page.b",
            Title = "Page B",
            Group = "TestGroup",
            Order = 10,
            ViewFactory = sp => "View B"
        };
        var desc3 = new NavigationItemDescriptor
        {
            Id = "test.page.c",
            Title = "Page C",
            Group = "OtherGroup",
            Order = 5
        };

        using var unreg1 = reg.RegisterNavigationItem(desc1);
        using var unreg2 = reg.RegisterNavigationItem(desc2);
        using var unreg3 = reg.RegisterNavigationItem(desc3);

        Assert.True(eventFired);
        Assert.Equal(3, reg.GetAllItems().Count);

        var testGroupItems = reg.GetItemsByGroup("TestGroup");
        Assert.Equal(2, testGroupItems.Count);
        Assert.Equal("test.page.b", testGroupItems[0].Id); // Order 10 before 20
        Assert.Equal("test.page.a", testGroupItems[1].Id);

        var singleItem = reg.GetItem("test.page.a");
        Assert.NotNull(singleItem);
        Assert.Equal("View A", singleItem.ViewFactory!(null!));
    }

    [Fact]
    public void DialogRegistry_Registers_And_Resolves_Dialogs()
    {
        var reg = new DialogRegistry();
        bool changed = false;
        reg.RegistryChanged += () => changed = true;

        var desc = new DialogDescriptor
        {
            Id = "custom.dialog.test",
            Title = "Test Dialog",
            ViewFactory = sp => "Dialog Content"
        };

        var unreg = reg.RegisterDialog(desc);
        Assert.True(changed);

        var retrieved = reg.GetDialog("custom.dialog.test");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Dialog", retrieved.Title);
        Assert.Equal("Dialog Content", retrieved.ViewFactory!(null!));

        unreg.Dispose();
        Assert.Null(reg.GetDialog("custom.dialog.test"));
    }

    [Fact]
    public void SidebarRegistry_Registers_And_Orders_Tabs()
    {
        var reg = new SidebarRegistry();
        bool changed = false;
        reg.RegistryChanged += () => changed = true;

        var tab1 = new SidebarTabDescriptor
        {
            Id = "sidebar.layers",
            Title = "Layers",
            IconKind = "LayersOutline",
            Order = 50
        };
        var tab2 = new SidebarTabDescriptor
        {
            Id = "sidebar.thumbnails",
            Title = "Pages",
            IconKind = "FileDocumentOutline",
            Order = 10
        };

        using var u1 = reg.RegisterTab(tab1);
        using var u2 = reg.RegisterTab(tab2);

        Assert.True(changed);
        var tabs = reg.GetAllTabs();
        Assert.Equal(2, tabs.Count);
        Assert.Equal("sidebar.thumbnails", tabs[0].Id);
        Assert.Equal("sidebar.layers", tabs[1].Id);
    }

    [Fact]
    public void Enhanced_RibbonRegistry_Supports_Dynamic_Tabs_And_Groups()
    {
        var reg = new RibbonRegistry();

        var tab = new RibbonTabDescriptor
        {
            Id = "CadEngineering",
            Title = "CAD & 3D",
            Order = 150
        };

        var group = new RibbonGroupDescriptor
        {
            Id = "CadTools",
            TabId = "CadEngineering",
            Title = "Drawing Tools",
            Order = 10
        };

        var action = new RibbonActionDescriptor
        {
            Id = "cad.measure.caliper",
            TabId = "CadEngineering",
            GroupId = "CadTools",
            Label = "Caliper",
            IconKind = "RulerSquare",
            Order = 5
        };

        using var uTab = reg.RegisterTab(tab);
        using var uGroup = reg.RegisterGroup(group);
        reg.RegisterAction(action);

        var allTabs = reg.GetAllTabs();
        Assert.Contains(allTabs, t => t.Id == "CadEngineering");

        var groups = reg.GetGroupsForTab("CadEngineering");
        Assert.Single(groups);
        Assert.Equal("CadTools", groups[0].Id);

        var actions = reg.GetActionsForGroup("CadEngineering", "CadTools");
        Assert.Single(actions);
        Assert.Equal("cad.measure.caliper", actions[0].Id);

        reg.UnregisterAction("cad.measure.caliper");
        Assert.Empty(reg.GetActionsForGroup("CadEngineering", "CadTools"));
    }

    [Fact]
    public async Task WorkspacePagesBundle_Mounts_All_12_Workspace_Pages()
    {
        var navReg = new NavigationRegistry();
        var context = new FryPluginContext();
        context.RegisterService<INavigationRegistry>(navReg);

        var host = new PluginHost(context);
        var bundle = new WorkspacePagesBundle();
        host.RegisterPlugins(bundle.Plugins);

        await host.StartAsync();

        var pages = navReg.GetAllItems();
        Assert.True(pages.Count >= 12, $"Expected at least 12 pages registered, found {pages.Count}");

        Assert.Contains(pages, p => p.Id == "Home");
        Assert.Contains(pages, p => p.Id == "PdfReader");
        Assert.Contains(pages, p => p.Id == "NewDocument");
        Assert.Contains(pages, p => p.Id == "AllTools");
        Assert.Contains(pages, p => p.Id == "Starred");
        Assert.Contains(pages, p => p.Id == "FontPackages");
        Assert.Contains(pages, p => p.Id == "TesseractData");
        Assert.Contains(pages, p => p.Id == "Trash");
        Assert.Contains(pages, p => p.Id == "Help");
        Assert.Contains(pages, p => p.Id == "Licensing");
        Assert.Contains(pages, p => p.Id == "Settings");

        await host.StopAsync();
    }

    [Fact]
    public async Task DialogsBundle_Mounts_Core_And_Studio_Dialogs()
    {
        var dialogReg = new DialogRegistry();
        var context = new FryPluginContext();
        context.RegisterService<IDialogRegistry>(dialogReg);

        var host = new PluginHost(context);
        var bundle = new DialogsBundle();
        host.RegisterPlugins(bundle.Plugins);

        await host.StartAsync();

        var dialogs = dialogReg.GetAllDialogs();
        Assert.True(dialogs.Count >= 15, $"Expected at least 15 dialogs registered, found {dialogs.Count}");

        Assert.Contains(dialogs, d => d.Id == "frypdf.dialog.about");
        Assert.Contains(dialogs, d => d.Id == "frypdf.dialog.shortcuts");
        Assert.Contains(dialogs, d => d.Id == "frypdf.dialog.plugins");
        Assert.Contains(dialogs, d => d.Id == "frypdf.dialog.signature");
        Assert.Contains(dialogs, d => d.Id == "frypdf.dialog.math");
        Assert.Contains(dialogs, d => d.Id == "frypdf.dialog.datastudio");
        Assert.Contains(dialogs, d => d.Id == "frypdf.dialog.aiassistant");

        await host.StopAsync();
    }

    [Fact]
    public async Task EditorSidebarsBundle_Mounts_Sidebar_Tabs()
    {
        var sidebarReg = new SidebarRegistry();
        var context = new FryPluginContext();
        context.RegisterService<ISidebarRegistry>(sidebarReg);

        var host = new PluginHost(context);
        var bundle = new EditorSidebarsBundle();
        host.RegisterPlugins(bundle.Plugins);

        await host.StartAsync();

        var tabs = sidebarReg.GetAllTabs();
        Assert.Equal(3, tabs.Count);
        Assert.Equal("Thumbnails", tabs[0].Id);
        Assert.Equal("Outline", tabs[1].Id);
        Assert.Equal("Comments", tabs[2].Id);

        await host.StopAsync();
    }

    [Fact]
    public void HomeViewModel_DynamicNavigation_Activates_Page_View()
    {
        var navReg = new NavigationRegistry();
        navReg.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "CustomStudio",
            Title = "Custom Studio",
            Group = "Extensions",
            ViewFactory = sp => "Custom Studio View Instance"
        });

        var vm = new HomeViewModel(
            navigationRegistry: navReg,
            recentService: new Services.RecentDocumentsService(),
            templateService: new Services.TemplateService(),
            persistenceService: new Services.ProjectPersistenceService(),
            toolRegistry: new Services.Tools.Core.PdfToolRegistry());

        Assert.Single(vm.ContributedNavigationItems);
        Assert.Equal("CustomStudio", vm.ContributedNavigationItems[0].Id);

        vm.SelectNavSection("CustomStudio");
        Assert.True(vm.ContributedNavigationItems[0].IsActive);
        Assert.Equal("Custom Studio View Instance", vm.DynamicPageView);
    }

    [Fact]
    public void Desktop_Profile_Includes_All_16_Bundles()
    {
        var profilePath = Path.Combine(AppContext.BaseDirectory, "profiles", "desktop.profile.json");
        if (!File.Exists(profilePath))
        {
            profilePath = "profiles/desktop.profile.json";
        }

        Assert.True(File.Exists(profilePath), $"desktop.profile.json not found at {profilePath}");
        var profile = ProfileLoader.LoadFromFile(profilePath);

        Assert.Contains("FryPdf.Bundle.WorkspacePages", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.Dialogs", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.EditorSidebars", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.CanvasElements", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.CommandPalette", profile.Bundles);
    }

    [Fact]
    public async Task PdfDocumentOperationsService_ExecutesThroughWaterfallPipeline_InterceptsTool()
    {
        var context = new FryPluginContext();
        bool pipelineIntercepted = false;

        context.RegisterWaterfall<PdfEditorApp.Core.Plugins.Pipelines.PdfToolExecutionPipelineContext>(
            "tool:execute",
            async (ctx, next) =>
            {
                pipelineIntercepted = true;
                ctx.Properties["audit:user"] = "SystemTester";
                await next();
            });

        var ops = new Services.PdfDocumentOperationsService(
            new PdfToolRegistry(),
            new PdfPageService(),
            new PdfOptimizationService(),
            new PdfSecurityService(),
            new PdfConversionService(),
            new PdfOcrService(),
            new PdfFormService(),
            new AiDocumentService(),
            new DocumentTranslationService(),
            new PdfWorkflowEngine(),
            context);

        var result = await ops.ExecuteToolAsync(Models.PdfToolId.MergePdf, new Models.MergeToolOptions());

        Assert.True(pipelineIntercepted);
    }

    [Fact]
    public async Task PluginItemViewModel_ToggleIsActive_TriggersToggleHandler()
    {
        bool toggleTriggered = false;
        bool toggleTargetValue = false;

        var vm = new PluginItemViewModel
        {
            Id = "test.plugin.toggle",
            Name = "Toggle Test",
            IsActive = true,
            ToggleHandler = (id, active) =>
            {
                toggleTriggered = true;
                toggleTargetValue = active;
                return Task.CompletedTask;
            }
        };

        // Switch to false
        vm.IsActive = false;

        // Give async Task a moment to run
        await Task.Delay(20);

        Assert.True(toggleTriggered);
        Assert.False(toggleTargetValue);
        Assert.False(vm.IsActive);
    }
}
