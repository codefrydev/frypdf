using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.ViewModels.Tools;
using PdfEditorApp.ViewModels.Tools.Core;
using Xunit;

namespace PdfEditorApp.Tests;

public class DynamicToolRegistryTests
{
    private class DummyCustomToolViewModel : PdfToolViewModelBase
    {
        public DummyCustomToolViewModel(PdfToolDefinition toolDef) : base(null!, toolDef)
        {
        }

        protected override Task<ToolExecutionResult> ExecuteCoreAsync(IProgress<double> progress, CancellationToken ct)
        {
            return Task.FromResult(new ToolExecutionResult { Success = true });
        }
    }

    [Fact]
    public void PdfToolRegistry_ExposesBuiltInToolsByDefault()
    {
        var registry = new PdfToolRegistry();
        var allTools = registry.GetAllTools();

        Assert.NotEmpty(allTools);
        Assert.Contains(allTools, t => t.Id == PdfToolId.MergePdf);
        Assert.Contains(allTools, t => t.Id == PdfToolId.CompressPdf);
    }

    [Fact]
    public async Task PdfToolRegistry_MergesPluginToolsDynamically()
    {
        var context = new FryPluginContext();
        var host = new PluginHost(context);
        var registry = new PdfToolRegistry(context);

        var initialCount = registry.GetAllTools().Count;

        // Register a dynamic tool plugin
        var dynamicToolPlugin = new DynamicToolTestPlugin();
        host.RegisterPlugin(dynamicToolPlugin);
        await host.StartAsync();

        var toolsAfterMount = registry.GetAllTools();
        Assert.Equal(initialCount + 1, toolsAfterMount.Count);

        var retrievedByStringId = registry.GetTool("vendor.custom.stamp");
        Assert.NotNull(retrievedByStringId);
        Assert.Equal("Custom Vector Stamp", retrievedByStringId.Name);
        Assert.Equal("RubberStamp", retrievedByStringId.IconKind);

        // Teardown the plugin
        await host.StopAsync();

        var toolsAfterUnmount = registry.GetAllTools();
        Assert.Equal(initialCount, toolsAfterUnmount.Count);
        Assert.Null(registry.GetTool("vendor.custom.stamp"));
    }

    [Fact]
    public async Task PdfToolRegistry_SeamlesslyUpgradesBuiltInToolsWhenPluginsMount()
    {
        var context = new FryPluginContext();
        var host = new PluginHost(context);
        var registry = new PdfToolRegistry(context);

        var initialCount = registry.GetAllTools().Count;
        var initialMerge = registry.GetTool(PdfToolId.MergePdf);
        Assert.NotNull(initialMerge);
        Assert.Equal("MergePdf", initialMerge.StringId);
        Assert.Null(initialMerge.ViewModelFactory);

        // Mount the built-in Merge plugin
        var mergePlugin = new PdfEditorApp.Plugins.Bundles.MergePdfToolPlugin();
        host.RegisterPlugin(mergePlugin);
        await host.StartAsync();

        // The tool count does NOT increase because it upgrades the built-in tool in place!
        var toolsAfterMount = registry.GetAllTools();
        Assert.Equal(initialCount, toolsAfterMount.Count);

        var upgradedMerge = registry.GetTool(PdfToolId.MergePdf);
        Assert.NotNull(upgradedMerge);
        Assert.Equal("frypdf.tool.merge", upgradedMerge.StringId);
        Assert.NotNull(upgradedMerge.ViewModelFactory);

        // Can also retrieve it by its new StringId
        var byStringId = registry.GetTool("frypdf.tool.merge");
        Assert.NotNull(byStringId);
        Assert.Equal(PdfToolId.MergePdf, byStringId.Id);

        await host.StopAsync();
    }

    [Fact]
    public void PdfToolViewModelFactory_ExecutesPluginViewModelFactoryDelegate()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var factory = new PdfToolViewModelFactory(null!, new PdfToolRegistry(), sp);

        var toolDef = new PdfToolDefinition
        {
            Id = PdfToolId.EditPdf,
            StringId = "custom.plugin.tool",
            Name = "Plugin Tool"
        };
        toolDef.ViewModelFactory = _ => new DummyCustomToolViewModel(toolDef);

        var vm = factory.CreateToolViewModel(toolDef);
        Assert.IsType<DummyCustomToolViewModel>(vm);
        Assert.Equal("Plugin Tool", vm.Tool.Name);
    }

    private class DynamicToolTestPlugin : IFryPlugin
    {
        public string Id => "plugin.dynamic.tool.test";
        public string Name => "Dynamic Tool Test Plugin";
        public Version Version => new(1, 0, 0);
        public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

        public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
        {
            ctx.RegisterTool(new PdfToolDescriptor
            {
                Id = "vendor.custom.stamp",
                Name = "Custom Vector Stamp",
                Description = "Apply custom vector stamps.",
                Category = "OrganizeAndPage",
                IconKind = "RubberStamp",
                IconColorHex = "#9333EA",
                CreateViewModel = sp => new DummyCustomToolViewModel(new PdfToolDefinition { StringId = "vendor.custom.stamp", Name = "Custom Vector Stamp" })
            });

            return Task.CompletedTask;
        }
    }
}
