using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using Xunit;

namespace PdfEditorApp.Tests;

public class PluginKernelTests
{
    private interface ITestServiceA { string GetValue(); }
    private interface ITestServiceB { int GetCount(); }

    private class TestServiceA : ITestServiceA
    {
        public string GetValue() => "A";
    }

    private class TestServiceB : ITestServiceB
    {
        public int GetCount() => 42;
    }

    private class PluginA : IFryPlugin
    {
        public string Id => "plugin.a";
        public string Name => "Plugin A";
        public Version Version => new(1, 0, 0);
        public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

        public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
        {
            ctx.RegisterService<ITestServiceA>(new TestServiceA());
            ctx.RegisterTool(new PdfToolDescriptor
            {
                Id = "tool.test.a",
                Name = "Tool A",
                Description = "Description A",
                Category = "Test",
                IconKind = "TestIcon"
            });
            return Task.CompletedTask;
        }
    }

    private class PluginB : IFryPlugin
    {
        public string Id => "plugin.b";
        public string Name => "Plugin B";
        public Version Version => new(1, 0, 0);
        public IReadOnlyList<Type> RequiredServices => new[] { typeof(ITestServiceA) };

        public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
        {
            var svc = ctx.GetService<ITestServiceA>();
            ctx.RegisterService<ITestServiceB>(new TestServiceB());
            return Task.CompletedTask;
        }
    }

    private class CyclicPlugin1 : IFryPlugin
    {
        public string Id => "cyclic.1";
        public string Name => "Cyclic 1";
        public Version Version => new(1, 0, 0);
        public IReadOnlyList<Type> RequiredServices => new[] { typeof(ITestServiceB) };
        public IReadOnlyList<Type> ProvidedServices => new[] { typeof(ITestServiceA) };

        public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
        {
            ctx.RegisterService<ITestServiceA>(new TestServiceA());
            return Task.CompletedTask;
        }
    }

    private class CyclicPlugin2 : IFryPlugin
    {
        public string Id => "cyclic.2";
        public string Name => "Cyclic 2";
        public Version Version => new(1, 0, 0);
        public IReadOnlyList<Type> RequiredServices => new[] { typeof(ITestServiceA) };
        public IReadOnlyList<Type> ProvidedServices => new[] { typeof(ITestServiceB) };

        public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
        {
            ctx.RegisterService<ITestServiceB>(new TestServiceB());
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PluginHost_TopologicalSort_ActivatesDependenciesBeforeConsumers()
    {
        var host = new PluginHost();
        // Register in reverse order: B (consumer) before A (provider)
        host.RegisterPlugin(new PluginB());
        host.RegisterPlugin(new PluginA());

        await host.StartAsync();

        Assert.Equal(2, host.LoadedPlugins.Count);
        Assert.Equal("plugin.a", host.LoadedPlugins[0].Id);
        Assert.Equal("plugin.b", host.LoadedPlugins[1].Id);

        Assert.True(host.Context.HasService<ITestServiceA>());
        Assert.True(host.Context.HasService<ITestServiceB>());

        var tool = host.Context.GetTool("tool.test.a");
        Assert.NotNull(tool);
        Assert.Equal("Tool A", tool.Name);
    }

    [Fact]
    public async Task PluginHost_MissingDependency_ThrowsPluginMissingDependencyException()
    {
        var host = new PluginHost();
        // Register only PluginB, which requires ITestServiceA (not provided)
        host.RegisterPlugin(new PluginB());

        var ex = await Assert.ThrowsAsync<PluginMissingDependencyException>(() => host.StartAsync());
        Assert.Equal("plugin.b", ex.PluginId);
        Assert.Equal(typeof(ITestServiceA), ex.MissingServiceType);
    }

    [Fact]
    public async Task PluginHost_CyclicDependency_ThrowsPluginCircularDependencyException()
    {
        var host = new PluginHost();
        host.RegisterPlugin(new CyclicPlugin1());
        host.RegisterPlugin(new CyclicPlugin2());

        await Assert.ThrowsAsync<PluginCircularDependencyException>(() => host.StartAsync());
    }

    [Fact]
    public async Task PluginHost_StopAsync_UnwindsEffectsAndRemovesServicesAndTools()
    {
        var host = new PluginHost();
        var disposedOrder = new List<string>();

        var pluginWithDisposal = new CustomDisposablePlugin(disposedOrder);
        host.RegisterPlugin(new PluginA());
        host.RegisterPlugin(pluginWithDisposal);

        await host.StartAsync();
        Assert.True(host.Context.HasService<ITestServiceA>());
        Assert.NotNull(host.Context.GetTool("tool.test.a"));

        await host.StopAsync();

        Assert.Empty(host.LoadedPlugins);
        Assert.False(host.Context.HasService<ITestServiceA>());
        Assert.Null(host.Context.GetTool("tool.test.a"));

        // Verify LIFO effect disposal occurred
        Assert.Contains("custom.disposed", disposedOrder);
    }

    [Fact]
    public async Task PluginHost_DisablePluginAsync_CascadesToDependentConsumers()
    {
        var host = new PluginHost();
        host.RegisterPlugin(new PluginA()); // provides ITestServiceA
        host.RegisterPlugin(new PluginB()); // requires ITestServiceA, provides ITestServiceB

        await host.StartAsync();
        Assert.Equal(PluginState.Active, host.GetPluginState("plugin.a"));
        Assert.Equal(PluginState.Active, host.GetPluginState("plugin.b"));

        // Disabling PluginA must cascade and suspend PluginB as well!
        await host.DisablePluginAsync("plugin.a");

        Assert.Equal(PluginState.Suspended, host.GetPluginState("plugin.a"));
        Assert.Equal(PluginState.Suspended, host.GetPluginState("plugin.b"));
        Assert.False(host.Context.HasService<ITestServiceA>());
        Assert.False(host.Context.HasService<ITestServiceB>());
        Assert.Null(host.Context.GetTool("tool.test.a"));
    }

    [Fact]
    public async Task PluginHost_EnablePluginAsync_ReactivatesAndAutoMountsSuspendedConsumers()
    {
        var host = new PluginHost();
        host.RegisterPlugin(new PluginA());
        host.RegisterPlugin(new PluginB());

        await host.StartAsync();
        await host.DisablePluginAsync("plugin.a");

        Assert.Equal(PluginState.Suspended, host.GetPluginState("plugin.a"));
        Assert.Equal(PluginState.Suspended, host.GetPluginState("plugin.b"));

        // Re-enabling PluginA must automatically auto-mount PluginB because its dependency is now met!
        await host.EnablePluginAsync("plugin.a");

        Assert.Equal(PluginState.Active, host.GetPluginState("plugin.a"));
        Assert.Equal(PluginState.Active, host.GetPluginState("plugin.b"));
        Assert.True(host.Context.HasService<ITestServiceA>());
        Assert.True(host.Context.HasService<ITestServiceB>());
        Assert.NotNull(host.Context.GetTool("tool.test.a"));
    }

    [Fact]
    public async Task PluginHost_ReloadPluginAsync_CyclesStateAndRebuildsEffects()
    {
        var host = new PluginHost();
        var effectsLog = new List<string>();

        var testPlugin = new ActionPlugin("plugin.reload.test",
            apply: ctx =>
            {
                effectsLog.Add("applied");
                ctx.RegisterEffect(() => effectsLog.Add("disposed"));
                return Task.CompletedTask;
            });

        host.RegisterPlugin(testPlugin);
        await host.StartAsync();

        Assert.Equal(new[] { "applied" }, effectsLog);

        await host.ReloadPluginAsync("plugin.reload.test");

        Assert.Equal(new[] { "applied", "disposed", "applied" }, effectsLog);
        Assert.Equal(PluginState.Active, host.GetPluginState("plugin.reload.test"));
    }

    [Fact]
    public void PdfToolRegistry_ZeroBaseline_StartsEmptyAndReactsToPluginContext()
    {
        var ctx = new FryPluginContext();
        var registry = new PdfEditorApp.Services.Tools.Core.PdfToolRegistry(ctx, seedDefaults: false);

        // Initially empty
        Assert.Empty(registry.GetAllTools());

        bool changedFired = false;
        registry.RegistryChanged += () => changedFired = true;

        // Register a tool dynamically in context
        using (ctx.RegisterTool(new PdfToolDescriptor
        {
            Id = "frypdf.tool.custom",
            Name = "Custom Tool",
            Description = "Custom dynamic tool",
            Category = "General",
            IconKind = "Wrench",
            CreateViewModel = sp => null!
        }))
        {
            Assert.True(changedFired);
            Assert.Single(registry.GetAllTools());
            Assert.Equal("Custom Tool", registry.GetAllTools()[0].Name);
        }

        // After disposing the registration, the tool is cleanly gone!
        Assert.Empty(registry.GetAllTools());
    }

    [Fact]
    public void CanvasElementRegistry_ZeroBaseline_StartsEmptyAndPopulatesViaPlugins()
    {
        var registry = new PdfEditorApp.Services.Canvas.CanvasElementRegistry(null, seedBuiltIns: false);

        Assert.Empty(registry.GetAllDescriptors());

        var desc = new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.custom",
            DisplayName = "Custom Element",
            ModelType = typeof(CustomTestElementModel),
            ViewModelType = typeof(PdfEditorApp.ViewModels.ElementViewModels.TextElementViewModel),
            IconKind = "Star",
            Factory = (sp, m) => new PdfEditorApp.ViewModels.ElementViewModels.TextElementViewModel()
        };

        registry.RegisterElement(desc);
        Assert.Single(registry.GetAllDescriptors());
        Assert.NotNull(registry.GetDescriptor("frypdf.element.custom"));

        registry.UnregisterElement("frypdf.element.custom");
        Assert.Empty(registry.GetAllDescriptors());
    }

    [Fact]
    public void DynamicElementJsonResolver_SerializesAndDeserializesCustomPluginElement()
    {
        var descriptors = new List<CanvasElementDescriptor>
        {
            new CanvasElementDescriptor
            {
                ElementTypeId = "frypdf.element.custom_test",
                DisplayName = "Custom Test",
                ModelType = typeof(CustomTestElementModel),
                ViewModelType = typeof(PdfEditorApp.ViewModels.ElementViewModels.TextElementViewModel),
                IconKind = "Star"
            }
        };

        var options = PdfEditorApp.Core.Models.Elements.DynamicElementJsonResolver.CreateOptions(() => descriptors);

        PdfEditorApp.Core.Models.Elements.PdfElementBase element = new CustomTestElementModel
        {
            CustomProperty = "DeepSeekParity"
        };

        string json = System.Text.Json.JsonSerializer.Serialize(element, options);
        Assert.Contains("\"$type\":\"frypdf.element.custom_test\"", json);
        Assert.Contains("\"CustomProperty\":\"DeepSeekParity\"", json);

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<PdfEditorApp.Core.Models.Elements.PdfElementBase>(json, options);
        Assert.NotNull(deserialized);
        Assert.IsType<CustomTestElementModel>(deserialized);
        Assert.Equal("DeepSeekParity", ((CustomTestElementModel)deserialized).CustomProperty);
    }

    private class ActionPlugin : IFryPlugin
    {
        private readonly Func<IFryPluginContext, Task> _apply;
        public ActionPlugin(string id, Func<IFryPluginContext, Task> apply)
        {
            Id = id;
            _apply = apply;
        }

        public string Id { get; }
        public string Name => Id;
        public Version Version => new(1, 0, 0);
        public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();
        public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default) => _apply(ctx);
    }

    private class CustomTestElementModel : PdfEditorApp.Core.Models.Elements.PdfElementBase
    {
        public override PdfEditorApp.Core.Models.ElementKind Kind => PdfEditorApp.Core.Models.ElementKind.Text;
        public string CustomProperty { get; set; } = string.Empty;
    }

    private class CustomDisposablePlugin : IFryPlugin
    {
        private readonly List<string> _disposedLog;

        public CustomDisposablePlugin(List<string> disposedLog)
        {
            _disposedLog = disposedLog;
        }

        public string Id => "plugin.custom.disposable";
        public string Name => "Custom Disposable";
        public Version Version => new(1, 0, 0);
        public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

        public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
        {
            ctx.RegisterEffect(() => _disposedLog.Add("custom.disposed"));
            return Task.CompletedTask;
        }
    }
}
