using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Pipelines;

namespace PdfEditorApp.Core.Plugins;

/// <summary>
/// Root implementation of <see cref="IFryPluginContext"/>.
/// Provides dynamic service discovery, pipeline orchestration, tool registration, and scoped lifecycle wrapping.
/// </summary>
public class FryPluginContext : IFryPluginContext
{
    private readonly ConcurrentDictionary<Type, object> _services = new();
    private readonly ConcurrentDictionary<string, PdfToolDescriptor> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CanvasElementDescriptor> _canvasElements = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RibbonActionDescriptor> _ribbonActions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IDocumentImporter> _importers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IDocumentExporter> _exporters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InspectorSectionDescriptor> _inspectorSections = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ITemplateDescriptor> _templates = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IOcrEngine> _ocrEngines = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IDataConnector> _dataConnectors = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, StatusBarWidgetDescriptor> _statusBarWidgets = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CommandPaletteDescriptor> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, NavigationItemDescriptor> _navigationItems = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DialogDescriptor> _dialogs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SidebarTabDescriptor> _sidebarTabs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RibbonTabDescriptor> _ribbonTabs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RibbonGroupDescriptor> _ribbonGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly IPipelineManager _pipelines;

    private readonly IServiceProvider? _fallbackServiceProvider;
    private readonly PluginScope? _scope;

    public IPipelineManager Pipelines => _pipelines;

    public FryPluginContext(
        IServiceProvider? fallbackServiceProvider = null,
        IPipelineManager? pipelines = null,
        PluginScope? scope = null)
    {
        _fallbackServiceProvider = fallbackServiceProvider;
        _pipelines = pipelines ?? new PipelineManager();
        _scope = scope;
    }

    /// <summary>
    /// Creates a child context scoped to a specific plugin.
    /// Registrations in the scoped context automatically attach their disposal to the plugin's <see cref="PluginScope"/>.
    /// </summary>
    public FryPluginContext CreateScopedContext(PluginScope pluginScope)
    {
        ArgumentNullException.ThrowIfNull(pluginScope);
        return new ScopedPluginContext(this, pluginScope);
    }

    // --- Service Registration & Discovery ---

    public virtual void RegisterService<TService>(TService implementation) where TService : class
    {
        ArgumentNullException.ThrowIfNull(implementation);
        _services[typeof(TService)] = implementation;
    }

    public virtual void RegisterService(Type serviceType, object implementation)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementation);
        _services[serviceType] = implementation;
    }

    public virtual TService GetService<TService>() where TService : class
    {
        if (TryGetService<TService>(out var service))
        {
            return service;
        }

        throw new KeyNotFoundException($"Service of type '{typeof(TService).FullName}' is not registered.");
    }

    public virtual bool TryGetService<TService>([NotNullWhen(true)] out TService? service) where TService : class
    {
        if (_services.TryGetValue(typeof(TService), out var obj) && obj is TService typedObj)
        {
            service = typedObj;
            return true;
        }

        if (_fallbackServiceProvider?.GetService(typeof(TService)) is TService fallbackObj)
        {
            service = fallbackObj;
            return true;
        }

        service = null;
        return false;
    }

    public virtual bool HasService<TService>() where TService : class => HasService(typeof(TService));

    public virtual bool HasService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return _services.ContainsKey(serviceType) || (_fallbackServiceProvider?.GetService(serviceType) != null);
    }

    public virtual object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (_services.TryGetValue(serviceType, out var obj))
        {
            return obj;
        }

        return _fallbackServiceProvider?.GetService(serviceType);
    }

    // --- Effects ---

    public virtual IDisposable RegisterEffect(Action onDispose)
    {
        if (_scope != null)
        {
            return _scope.RegisterEffect(onDispose);
        }

        return new DisposableAction(onDispose);
    }

    public virtual IDisposable RegisterDisposable(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        return RegisterEffect(disposable.Dispose);
    }

    // --- Pipelines ---

    public virtual IDisposable RegisterWaterfall<TContext>(string pipelineName, Func<TContext, Func<Task>, Task> middleware)
    {
        var registration = _pipelines.RegisterWaterfall(pipelineName, middleware);
        return RegisterDisposable(registration);
    }

    public virtual Task ExecuteWaterfallAsync<TContext>(string pipelineName, TContext context, Func<Task>? terminal = null)
    {
        return _pipelines.ExecuteWaterfallAsync(pipelineName, context, terminal);
    }

    public virtual IDisposable RegisterBail<TContext, TResult>(string pipelineName, Func<TContext, Task<TResult?>> handler) where TResult : class
    {
        var registration = _pipelines.RegisterBail(pipelineName, handler);
        return RegisterDisposable(registration);
    }

    public virtual Task<TResult?> ExecuteBailAsync<TContext, TResult>(string pipelineName, TContext context) where TResult : class
    {
        return _pipelines.ExecuteBailAsync<TContext, TResult>(pipelineName, context);
    }

    public virtual IDisposable RegisterParallel<TContext>(string pipelineName, Func<TContext, Task> handler)
    {
        var registration = _pipelines.RegisterParallel(pipelineName, handler);
        return RegisterDisposable(registration);
    }

    public virtual Task ExecuteParallelAsync<TContext>(string pipelineName, TContext context)
    {
        return _pipelines.ExecuteParallelAsync(pipelineName, context);
    }

    public virtual IDisposable RegisterSerial<TContext>(string pipelineName, Func<TContext, Task> handler)
    {
        var registration = _pipelines.RegisterSerial(pipelineName, handler);
        return RegisterDisposable(registration);
    }

    public virtual Task ExecuteSerialAsync<TContext>(string pipelineName, TContext context)
    {
        return _pipelines.ExecuteSerialAsync(pipelineName, context);
    }

    // --- Tools ---

    public event Action? ToolsChanged;

    public virtual IDisposable RegisterTool(PdfToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentException.ThrowIfNullOrWhiteSpace(tool.Id);

        _tools[tool.Id] = tool;
        ToolsChanged?.Invoke();

        return RegisterEffect(() =>
        {
            _tools.TryRemove(tool.Id, out _);
            ToolsChanged?.Invoke();
        });
    }

    public virtual IReadOnlyList<PdfToolDescriptor> GetRegisteredTools()
    {
        return _tools.Values.ToList();
    }

    public virtual PdfToolDescriptor? GetTool(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _tools.GetValueOrDefault(id);
    }

    // --- Canvas Elements ---

    public virtual IDisposable RegisterCanvasElement(CanvasElementDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.ElementTypeId);

        _canvasElements[descriptor.ElementTypeId] = descriptor;

        if (TryGetService<ICanvasElementRegistry>(out var elementRegistry))
        {
            elementRegistry.RegisterElement(descriptor);
        }

        return RegisterEffect(() =>
        {
            _canvasElements.TryRemove(descriptor.ElementTypeId, out _);
            if (TryGetService<ICanvasElementRegistry>(out var registry))
            {
                registry.UnregisterElement(descriptor.ElementTypeId);
            }
        });
    }

    public virtual IReadOnlyList<CanvasElementDescriptor> GetRegisteredCanvasElements()
    {
        return _canvasElements.Values.ToList();
    }

    public virtual CanvasElementDescriptor? GetCanvasElement(string elementTypeId)
    {
        if (string.IsNullOrWhiteSpace(elementTypeId)) return null;
        return _canvasElements.GetValueOrDefault(elementTypeId);
    }

    // --- Ribbon Actions ---

    public virtual IDisposable RegisterRibbonAction(RibbonActionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.Id);

        _ribbonActions[descriptor.Id] = descriptor;

        if (TryGetService<IRibbonRegistry>(out var ribbonRegistry))
        {
            ribbonRegistry.RegisterAction(descriptor);
        }

        return RegisterEffect(() =>
        {
            _ribbonActions.TryRemove(descriptor.Id, out _);
            if (TryGetService<IRibbonRegistry>(out var reg))
            {
                reg.UnregisterAction(descriptor.Id);
            }
        });
    }

    public virtual IReadOnlyList<RibbonActionDescriptor> GetRegisteredRibbonActions()
    {
        return _ribbonActions.Values.OrderBy(r => r.Order).ToList();
    }

    // --- Document Importers ---

    public virtual IDisposable RegisterImporter(IDocumentImporter importer)
    {
        ArgumentNullException.ThrowIfNull(importer);
        _importers[importer.ImporterId] = importer;

        if (TryGetService<IDocumentImporterRegistry>(out var reg))
        {
            reg.RegisterImporter(importer);
        }

        return RegisterEffect(() =>
        {
            _importers.TryRemove(importer.ImporterId, out _);
        });
    }

    public virtual IReadOnlyList<IDocumentImporter> GetRegisteredImporters()
    {
        return _importers.Values.OrderByDescending(i => i.Priority).ToList();
    }

    // --- Document Exporters ---

    public virtual IDisposable RegisterExporter(IDocumentExporter exporter)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        _exporters[exporter.ExporterId] = exporter;

        if (TryGetService<IDocumentExporterRegistry>(out var reg))
        {
            reg.RegisterExporter(exporter);
        }

        return RegisterEffect(() =>
        {
            _exporters.TryRemove(exporter.ExporterId, out _);
        });
    }

    public virtual IReadOnlyList<IDocumentExporter> GetRegisteredExporters()
    {
        return _exporters.Values.ToList();
    }

    // --- Inspector Panels ---

    public virtual IDisposable RegisterInspectorSection(InspectorSectionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _inspectorSections[descriptor.SectionId] = descriptor;

        if (TryGetService<IInspectorRegistry>(out var reg))
        {
            reg.RegisterSection(descriptor);
        }

        return RegisterEffect(() =>
        {
            _inspectorSections.TryRemove(descriptor.SectionId, out _);
        });
    }

    public virtual IReadOnlyList<InspectorSectionDescriptor> GetRegisteredInspectorSections()
    {
        return _inspectorSections.Values.OrderBy(s => s.Order).ToList();
    }

    // --- Document Templates ---

    public virtual IDisposable RegisterTemplate(ITemplateDescriptor template)
    {
        ArgumentNullException.ThrowIfNull(template);
        _templates[template.Id] = template;

        if (TryGetService<ITemplateRegistry>(out var reg))
        {
            reg.RegisterTemplate(template);
        }

        return RegisterEffect(() =>
        {
            _templates.TryRemove(template.Id, out _);
        });
    }

    public virtual IReadOnlyList<ITemplateDescriptor> GetRegisteredTemplates()
    {
        return _templates.Values.ToList();
    }

    // --- OCR Engines ---

    public virtual IDisposable RegisterOcrEngine(IOcrEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _ocrEngines[engine.EngineName] = engine;

        if (TryGetService<IOcrEngineRegistry>(out var reg))
        {
            reg.RegisterEngine(engine);
        }

        return RegisterEffect(() =>
        {
            _ocrEngines.TryRemove(engine.EngineName, out _);
        });
    }

    public virtual IReadOnlyList<IOcrEngine> GetRegisteredOcrEngines()
    {
        return _ocrEngines.Values.ToList();
    }

    // --- Data Connectors ---

    public virtual IDisposable RegisterDataConnector(IDataConnector connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        _dataConnectors[connector.ConnectorId] = connector;

        if (TryGetService<IDataConnectorRegistry>(out var reg))
        {
            reg.RegisterConnector(connector);
        }

        return RegisterEffect(() =>
        {
            _dataConnectors.TryRemove(connector.ConnectorId, out _);
        });
    }

    public virtual IReadOnlyList<IDataConnector> GetRegisteredDataConnectors()
    {
        return _dataConnectors.Values.ToList();
    }

    // --- Status Bar Widgets ---

    public virtual IDisposable RegisterStatusBarWidget(StatusBarWidgetDescriptor widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        _statusBarWidgets[widget.WidgetId] = widget;

        if (TryGetService<IStatusBarRegistry>(out var reg))
        {
            reg.RegisterWidget(widget);
        }

        return RegisterEffect(() =>
        {
            _statusBarWidgets.TryRemove(widget.WidgetId, out _);
        });
    }

    public virtual IReadOnlyList<StatusBarWidgetDescriptor> GetRegisteredStatusBarWidgets()
    {
        return _statusBarWidgets.Values.OrderBy(w => w.Order).ToList();
    }

    // --- Command Palette ---

    public virtual IDisposable RegisterCommand(CommandPaletteDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _commands[descriptor.Id] = descriptor;

        if (TryGetService<ICommandPaletteRegistry>(out var reg))
        {
            reg.RegisterCommand(descriptor);
        }

        return RegisterEffect(() =>
        {
            _commands.TryRemove(descriptor.Id, out _);
            if (TryGetService<ICommandPaletteRegistry>(out var r))
            {
                r.UnregisterCommand(descriptor.Id);
            }
        });
    }

    public virtual IReadOnlyList<CommandPaletteDescriptor> GetRegisteredCommands()
    {
        return _commands.Values.OrderBy(c => c.Order).ToList();
    }

    // --- Navigation Registry ---

    public virtual IDisposable RegisterNavigationItem(NavigationItemDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _navigationItems[descriptor.Id] = descriptor;

        if (TryGetService<INavigationRegistry>(out var reg))
        {
            reg.RegisterNavigationItem(descriptor);
        }

        return RegisterEffect(() =>
        {
            _navigationItems.TryRemove(descriptor.Id, out _);
            if (TryGetService<INavigationRegistry>(out var r))
            {
                r.UnregisterNavigationItem(descriptor.Id);
            }
        });
    }

    public virtual IReadOnlyList<NavigationItemDescriptor> GetRegisteredNavigationItems()
    {
        return _navigationItems.Values.OrderBy(n => n.Order).ToList();
    }

    // --- Dialog Registry ---

    public virtual IDisposable RegisterDialog(DialogDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _dialogs[descriptor.Id] = descriptor;

        if (TryGetService<IDialogRegistry>(out var reg))
        {
            reg.RegisterDialog(descriptor);
        }

        return RegisterEffect(() =>
        {
            _dialogs.TryRemove(descriptor.Id, out _);
            if (TryGetService<IDialogRegistry>(out var r))
            {
                r.UnregisterDialog(descriptor.Id);
            }
        });
    }

    public virtual IReadOnlyList<DialogDescriptor> GetRegisteredDialogs()
    {
        return _dialogs.Values.ToList();
    }

    // --- Sidebar Registry ---

    public virtual IDisposable RegisterSidebarTab(SidebarTabDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _sidebarTabs[descriptor.Id] = descriptor;

        if (TryGetService<ISidebarRegistry>(out var reg))
        {
            reg.RegisterTab(descriptor);
        }

        return RegisterEffect(() =>
        {
            _sidebarTabs.TryRemove(descriptor.Id, out _);
            if (TryGetService<ISidebarRegistry>(out var r))
            {
                r.UnregisterTab(descriptor.Id);
            }
        });
    }

    public virtual IReadOnlyList<SidebarTabDescriptor> GetRegisteredSidebarTabs()
    {
        return _sidebarTabs.Values.OrderBy(s => s.Order).ToList();
    }

    // --- Dynamic Ribbon Tabs & Groups ---

    public virtual IDisposable RegisterRibbonTab(RibbonTabDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _ribbonTabs[descriptor.Id] = descriptor;

        if (TryGetService<IRibbonRegistry>(out var reg))
        {
            reg.RegisterTab(descriptor);
        }

        return RegisterEffect(() =>
        {
            _ribbonTabs.TryRemove(descriptor.Id, out _);
        });
    }

    public virtual IDisposable RegisterRibbonGroup(RibbonGroupDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _ribbonGroups[descriptor.Id] = descriptor;

        if (TryGetService<IRibbonRegistry>(out var reg))
        {
            reg.RegisterGroup(descriptor);
        }

        return RegisterEffect(() =>
        {
            _ribbonGroups.TryRemove(descriptor.Id, out _);
        });
    }


    /// <summary>
    /// Scoped context subclass wrapping a parent context with a plugin-specific <see cref="PluginScope"/>.
    /// </summary>
    private sealed class ScopedPluginContext : FryPluginContext
    {
        private readonly FryPluginContext _parent;
        private readonly PluginScope _pluginScope;

        public ScopedPluginContext(FryPluginContext parent, PluginScope pluginScope)
            : base(parent._fallbackServiceProvider, parent._pipelines, pluginScope)
        {
            _parent = parent;
            _pluginScope = pluginScope;
        }

        public override void RegisterService<TService>(TService implementation)
        {
            _parent.RegisterService(implementation);
            _pluginScope.RegisterEffect(() =>
            {
                // Remove from parent services dictionary
                _parent._services.TryRemove(typeof(TService), out _);
            });
        }

        public override void RegisterService(Type serviceType, object implementation)
        {
            _parent.RegisterService(serviceType, implementation);
            _pluginScope.RegisterEffect(() =>
            {
                _parent._services.TryRemove(serviceType, out _);
            });
        }

        public override TService GetService<TService>() => _parent.GetService<TService>();
        public override bool TryGetService<TService>([NotNullWhen(true)] out TService? service) where TService : class => _parent.TryGetService(out service);
        public override bool HasService<TService>() where TService : class => _parent.HasService<TService>();
        public override bool HasService(Type serviceType) => _parent.HasService(serviceType);
        public override object? GetService(Type serviceType) => _parent.GetService(serviceType);

        public override IDisposable RegisterEffect(Action onDispose) => _pluginScope.RegisterEffect(onDispose);
        public override IDisposable RegisterDisposable(IDisposable disposable) => _pluginScope.RegisterDisposable(disposable);

        public override IDisposable RegisterWaterfall<TContext>(string pipelineName, Func<TContext, Func<Task>, Task> middleware)
        {
            var reg = _parent.Pipelines.RegisterWaterfall(pipelineName, middleware);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override Task ExecuteWaterfallAsync<TContext>(string pipelineName, TContext context, Func<Task>? terminal = null)
        {
            return _parent.ExecuteWaterfallAsync(pipelineName, context, terminal);
        }

        public override IDisposable RegisterBail<TContext, TResult>(string pipelineName, Func<TContext, Task<TResult?>> handler) where TResult : class
        {
            var reg = _parent.Pipelines.RegisterBail(pipelineName, handler);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override Task<TResult?> ExecuteBailAsync<TContext, TResult>(string pipelineName, TContext context) where TResult : class
        {
            return _parent.ExecuteBailAsync<TContext, TResult>(pipelineName, context);
        }

        public override IDisposable RegisterParallel<TContext>(string pipelineName, Func<TContext, Task> handler)
        {
            var reg = _parent.Pipelines.RegisterParallel(pipelineName, handler);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override Task ExecuteParallelAsync<TContext>(string pipelineName, TContext context)
        {
            return _parent.ExecuteParallelAsync(pipelineName, context);
        }

        public override IDisposable RegisterSerial<TContext>(string pipelineName, Func<TContext, Task> handler)
        {
            var reg = _parent.Pipelines.RegisterSerial(pipelineName, handler);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override Task ExecuteSerialAsync<TContext>(string pipelineName, TContext context)
        {
            return _parent.ExecuteSerialAsync(pipelineName, context);
        }

        public override IDisposable RegisterTool(PdfToolDescriptor tool)
        {
            var reg = _parent.RegisterTool(tool);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<PdfToolDescriptor> GetRegisteredTools() => _parent.GetRegisteredTools();
        public override PdfToolDescriptor? GetTool(string id) => _parent.GetTool(id);

        public override IDisposable RegisterCanvasElement(CanvasElementDescriptor descriptor)
        {
            var reg = _parent.RegisterCanvasElement(descriptor);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<CanvasElementDescriptor> GetRegisteredCanvasElements() => _parent.GetRegisteredCanvasElements();
        public override CanvasElementDescriptor? GetCanvasElement(string elementTypeId) => _parent.GetCanvasElement(elementTypeId);

        public override IDisposable RegisterRibbonAction(RibbonActionDescriptor descriptor)
        {
            var reg = _parent.RegisterRibbonAction(descriptor);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<RibbonActionDescriptor> GetRegisteredRibbonActions() => _parent.GetRegisteredRibbonActions();

        public override IDisposable RegisterImporter(IDocumentImporter importer)
        {
            var reg = _parent.RegisterImporter(importer);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<IDocumentImporter> GetRegisteredImporters() => _parent.GetRegisteredImporters();

        public override IDisposable RegisterExporter(IDocumentExporter exporter)
        {
            var reg = _parent.RegisterExporter(exporter);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<IDocumentExporter> GetRegisteredExporters() => _parent.GetRegisteredExporters();

        public override IDisposable RegisterInspectorSection(InspectorSectionDescriptor descriptor)
        {
            var reg = _parent.RegisterInspectorSection(descriptor);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<InspectorSectionDescriptor> GetRegisteredInspectorSections() => _parent.GetRegisteredInspectorSections();

        public override IDisposable RegisterTemplate(ITemplateDescriptor template)
        {
            var reg = _parent.RegisterTemplate(template);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<ITemplateDescriptor> GetRegisteredTemplates() => _parent.GetRegisteredTemplates();

        public override IDisposable RegisterOcrEngine(IOcrEngine engine)
        {
            var reg = _parent.RegisterOcrEngine(engine);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<IOcrEngine> GetRegisteredOcrEngines() => _parent.GetRegisteredOcrEngines();

        public override IDisposable RegisterDataConnector(IDataConnector connector)
        {
            var reg = _parent.RegisterDataConnector(connector);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<IDataConnector> GetRegisteredDataConnectors() => _parent.GetRegisteredDataConnectors();

        public override IDisposable RegisterStatusBarWidget(StatusBarWidgetDescriptor widget)
        {
            var reg = _parent.RegisterStatusBarWidget(widget);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<StatusBarWidgetDescriptor> GetRegisteredStatusBarWidgets() => _parent.GetRegisteredStatusBarWidgets();

        public override IDisposable RegisterCommand(CommandPaletteDescriptor descriptor)
        {
            var reg = _parent.RegisterCommand(descriptor);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<CommandPaletteDescriptor> GetRegisteredCommands() => _parent.GetRegisteredCommands();

        public override IDisposable RegisterNavigationItem(NavigationItemDescriptor descriptor)
        {
            var reg = _parent.RegisterNavigationItem(descriptor);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<NavigationItemDescriptor> GetRegisteredNavigationItems() => _parent.GetRegisteredNavigationItems();

        public override IDisposable RegisterDialog(DialogDescriptor descriptor)
        {
            var reg = _parent.RegisterDialog(descriptor);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<DialogDescriptor> GetRegisteredDialogs() => _parent.GetRegisteredDialogs();

        public override IDisposable RegisterSidebarTab(SidebarTabDescriptor descriptor)
        {
            var reg = _parent.RegisterSidebarTab(descriptor);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IReadOnlyList<SidebarTabDescriptor> GetRegisteredSidebarTabs() => _parent.GetRegisteredSidebarTabs();

        public override IDisposable RegisterRibbonTab(RibbonTabDescriptor descriptor)
        {
            var reg = _parent.RegisterRibbonTab(descriptor);
            return _pluginScope.RegisterDisposable(reg);
        }

        public override IDisposable RegisterRibbonGroup(RibbonGroupDescriptor descriptor)
        {
            var reg = _parent.RegisterRibbonGroup(descriptor);
            return _pluginScope.RegisterDisposable(reg);
        }
    }


    private sealed class DisposableAction : IDisposable
    {
        private Action? _action;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            var act = System.Threading.Interlocked.Exchange(ref _action, null);
            act?.Invoke();
        }
    }
}
