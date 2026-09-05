using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Converters;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Manifests;
using PdfEditorApp.Core.Plugins.Settings;
using PdfEditorApp.Plugins.Loader;
using PdfEditorApp.Services.Inspector;
using PdfEditorApp.Services.Ribbon;
using PdfEditorApp.Services.Sidebar;
using PdfEditorApp.Services.StatusBar;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PluginEcosystemTests
{
    [Fact]
    public void DynamicRibbon_Tabs_Groups_And_Actions_Are_Reactive()
    {
        var registry = new RibbonRegistry();
        bool changedFired = false;
        registry.RegistryChanged += () => changedFired = true;

        // Register tab
        using var tabReg = registry.RegisterTab(new RibbonTabDescriptor
        {
            Id = "CustomAiTab",
            Title = "AI Studio",
            Order = 10
        });

        Assert.True(changedFired);
        Assert.Single(registry.GetAllTabs());
        Assert.Equal("CustomAiTab", registry.GetAllTabs()[0].Id);

        // Register group
        using var groupReg = registry.RegisterGroup(new RibbonGroupDescriptor
        {
            Id = "AiGenGroup",
            TabId = "CustomAiTab",
            Title = "Generation",
            Order = 1
        });

        // Register action
        bool actionExecuted = false;
        registry.RegisterAction(new RibbonActionDescriptor
        {
            Id = "action.generate.summary",
            TabId = "CustomAiTab",
            GroupId = "AiGenGroup",
            Label = "Summarize",
            IconKind = "Brain",
            Action = _ => actionExecuted = true
        });

        var groups = registry.GetGroupsForTab("CustomAiTab");
        Assert.Single(groups);
        Assert.Equal("Generation", groups[0].Title);

        var actions = registry.GetActionsForGroup("CustomAiTab", "AiGenGroup");
        Assert.Single(actions);
        Assert.Equal("Summarize", actions[0].Label);

        // Execute action
        actions[0].Action!(null!);
        Assert.True(actionExecuted);

        // Unregister action
        Assert.True(registry.UnregisterAction("action.generate.summary"));
        Assert.Empty(registry.GetActionsForGroup("CustomAiTab", "AiGenGroup"));
    }

    [Fact]
    public void DynamicSidebar_Registration_And_Selection_Works()
    {
        var registry = new SidebarRegistry();
        bool changed = false;
        registry.RegistryChanged += () => changed = true;

        var sidebarDesc = new SidebarTabDescriptor
        {
            Id = "sidebar.ai.copilot",
            Title = "AI Copilot",
            IconKind = "Robot",
            Tooltip = "Interactive PDF Assistant",
            ViewFactory = sp => "CopilotViewInstance"
        };

        using var unreg = registry.RegisterTab(sidebarDesc);

        Assert.True(changed);
        Assert.Single(registry.GetAllTabs());
        Assert.Equal("sidebar.ai.copilot", registry.GetTab("sidebar.ai.copilot")?.Id);

        var view = sidebarDesc.ViewFactory(null!);
        Assert.Equal("CopilotViewInstance", view);
    }

    [Fact]
    public void DynamicInspector_Reacts_To_RegistryChanged()
    {
        var registry = new InspectorRegistry();
        var inspectorVm = new InspectorViewModel(inspectorRegistry: registry);

        Assert.Empty(inspectorVm.DynamicSections);

        // Register section
        var secDesc = new InspectorSectionDescriptor
        {
            SectionId = "section.crypto.verify",
            Title = "Signature Verification",
            AppliesTo = target => target is string s && s == "Signature",
            Factory = (sp, el) => $"VerifiedPanelFor_{el}"
        };

        using var unreg = registry.RegisterSection(secDesc);

        // Target matching
        inspectorVm.RefreshDynamicSections("Signature");
        Assert.Contains(inspectorVm.DynamicSections, s => s is DynamicInspectorSectionViewModel d && (string?)d.Content == "VerifiedPanelFor_Signature");

        // Target not matching
        inspectorVm.RefreshDynamicSections("Text");
        Assert.DoesNotContain(inspectorVm.DynamicSections, s => s is DynamicInspectorSectionViewModel d && (string?)d.Content == "VerifiedPanelFor_Signature");

        // Target null
        inspectorVm.RefreshDynamicSections(null);
        Assert.Empty(inspectorVm.DynamicSections);
    }

    [Fact]
    public void StatusBarRegistry_ZeroBaseline_And_WidgetViewModel_Rendering()
    {
        var registry = new StatusBarRegistry(seedDefaults: false);
        Assert.Empty(registry.GetAllWidgets());

        var desc = new StatusBarWidgetDescriptor
        {
            WidgetId = "test.widget.zoom",
            Alignment = StatusBarAlignment.Right,
            Order = 10,
            ToolTip = "Current Zoom Level",
            Factory = sp => new StatusBarWidgetViewModel
            {
                WidgetId = "test.widget.zoom",
                Label = "100%",
                IconKind = "Magnify",
                ToolTip = "Current Zoom Level",
                IsActive = true
            }
        };

        using var unreg = registry.RegisterWidget(desc);
        var widgets = registry.GetWidgets(StatusBarAlignment.Right);
        Assert.Single(widgets);

        var vm = widgets[0].Factory(null!) as StatusBarWidgetViewModel;
        Assert.NotNull(vm);
        Assert.Equal("100%", vm.Label);
        Assert.Equal("Magnify", vm.IconKind);
        Assert.Equal("Current Zoom Level", vm.ToolTip);
        Assert.True(vm.IsActive);
    }

    [Fact]
    public void PluginSettingsStore_Persists_And_Retrieves_Typed_Values()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"plugin_settings_{Guid.NewGuid():N}.json");
        try
        {
            var store = new FilePluginSettingsStore(tempFile);

            store.SetSetting("frypdf.ai.groq", "ApiKey", "gsk_test_12345");
            store.SetSetting("frypdf.ai.groq", "MaxTokens", 4096);
            store.SetSetting("frypdf.ai.groq", "StreamingEnabled", true);
            store.Save();

            // Reload from file
            var store2 = new FilePluginSettingsStore(tempFile);
            Assert.Equal("gsk_test_12345", store2.GetSetting("frypdf.ai.groq", "ApiKey", ""));
            Assert.Equal(4096, store2.GetSetting("frypdf.ai.groq", "MaxTokens", 0));
            Assert.True(store2.GetSetting("frypdf.ai.groq", "StreamingEnabled", false));
            Assert.Equal("defaultVal", store2.GetSetting("frypdf.ai.groq", "NonExistent", "defaultVal"));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void FryPluginPackageLoader_Creates_And_Unpacks_Package()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fry_test_pkg_{Guid.NewGuid():N}");
        var outputPkg = Path.Combine(Path.GetTempPath(), $"test_plugin_{Guid.NewGuid():N}.fryplugin");
        var extractDir = Path.Combine(Path.GetTempPath(), $"fry_extract_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);

            var manifest = new PluginManifest
            {
                Id = "com.sample.ocr",
                Name = "Sample OCR Plugin",
                Version = "1.2.0",
                EntryPoint = "SampleOcr.dll",
                Description = "High accuracy OCR package",
                SettingsSchema = new Dictionary<string, PluginSettingDefinition>
                {
                    ["Language"] = new() { Type = "select", Label = "Language", DefaultValue = "eng", Options = new() { "eng", "deu", "fra" } }
                }
            };

            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(tempDir, "plugin.json"), manifestJson);

            // Copy a valid current assembly DLL to act as entry point
            var dummyDllPath = typeof(PluginEcosystemTests).Assembly.Location;
            File.Copy(dummyDllPath, Path.Combine(tempDir, "SampleOcr.dll"));

            // Pack
            FryPluginPackageLoader.CreatePackage(tempDir, outputPkg);
            Assert.True(File.Exists(outputPkg));

            // Unpack and load
            var result = FryPluginPackageLoader.UnpackAndLoad(outputPkg, extractDir);
            Assert.NotNull(result);
            Assert.Equal("com.sample.ocr", result.Manifest.Id);
            Assert.Equal("Sample OCR Plugin", result.Manifest.Name);
            Assert.True(Directory.Exists(result.InstallDirectory));

            result.Dispose();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            if (File.Exists(outputPkg)) File.Delete(outputPkg);
        }
    }

    [Fact]
    public void ValuesEqualMultiConverter_Evaluates_Correctly()
    {
        var converter = ValuesEqualMultiConverter.Instance;

        // Equal strings (case-insensitive)
        Assert.True((bool)converter.Convert(new object?[] { "CustomTab", "customtab" }, typeof(bool), null, null!)!);

        // Different strings
        Assert.False((bool)converter.Convert(new object?[] { "CustomTab", "OtherTab" }, typeof(bool), null, null!)!);

        // Null handling
        Assert.True((bool)converter.Convert(new object?[] { null, null }, typeof(bool), null, null!)!);
        Assert.False((bool)converter.Convert(new object?[] { "Tab", null }, typeof(bool), null, null!)!);
        Assert.False((bool)converter.Convert(new object?[] { null, "Tab" }, typeof(bool), null, null!)!);

        // Less than 2 values
        Assert.False((bool)converter.Convert(new object?[] { "Tab" }, typeof(bool), null, null!)!);
    }

    [Fact]
    public void PluginAssemblyLoader_DiscoverAndLoadDirectory_DiscoversPackagesAndSubdirectories()
    {
        var tempPluginsDir = Path.Combine(Path.GetTempPath(), $"fry_plugins_discovery_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempPluginsDir);

            // 1. Create a subdirectory with a dummy plugin manifest & DLL
            var subDir = Path.Combine(tempPluginsDir, "sample_subplugin");
            Directory.CreateDirectory(subDir);
            var manifest = new PluginManifest
            {
                Id = "com.sample.subplugin",
                Name = "Sample Subplugin",
                Version = "1.0.0",
                EntryPoint = "PluginAssembly.dll"
            };
            File.WriteAllText(Path.Combine(subDir, "plugin.json"), JsonSerializer.Serialize(manifest));
            File.Copy(typeof(PluginEcosystemTests).Assembly.Location, Path.Combine(subDir, "PluginAssembly.dll"));

            // 2. Discover
            var packages = PluginAssemblyLoader.DiscoverAndLoadDirectory(tempPluginsDir);
            Assert.NotEmpty(packages);

            foreach (var pkg in packages)
            {
                pkg.Dispose();
            }
        }
        finally
        {
            if (Directory.Exists(tempPluginsDir)) Directory.Delete(tempPluginsDir, true);
        }
    }
}
