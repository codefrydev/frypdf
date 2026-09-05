using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Pipelines;

namespace PdfEditorApp.Core.Plugins;

/// <summary>
/// The central capability context provided to plugins during their lifecycle.
/// Acts as a dynamic service bus, pipeline orchestrator, and effect tracker.
/// </summary>
public interface IFryPluginContext : IServiceProvider
{
    // --- 1. Service Bus ---

    /// <summary>
    /// Registers a service contract and implementation into the context.
    /// </summary>
    void RegisterService<TService>(TService implementation) where TService : class;

    /// <summary>
    /// Registers a service contract by type and implementation into the context.
    /// </summary>
    void RegisterService(Type serviceType, object implementation);

    /// <summary>
    /// Retrieves a required registered service. Throws if the service is not found.
    /// </summary>
    TService GetService<TService>() where TService : class;

    /// <summary>
    /// Attempts to retrieve a registered service.
    /// </summary>
    bool TryGetService<TService>([NotNullWhen(true)] out TService? service) where TService : class;

    /// <summary>
    /// Checks whether a service contract has been registered.
    /// </summary>
    bool HasService<TService>() where TService : class;

    /// <summary>
    /// Checks whether a service contract of the given type has been registered.
    /// </summary>
    bool HasService(Type serviceType);

    // --- 2. Reversible Effects ---

    /// <summary>
    /// Registers a cleanup action that will be executed when the current plugin unloads.
    /// </summary>
    IDisposable RegisterEffect(Action onDispose);

    /// <summary>
    /// Registers an <see cref="IDisposable"/> to be disposed when the current plugin unloads.
    /// </summary>
    IDisposable RegisterDisposable(IDisposable disposable);

    // --- 3. Pipeline Operations ---

    /// <summary>
    /// Gets the pipeline manager for registering and executing typed pipelines.
    /// </summary>
    IPipelineManager Pipelines { get; }

    /// <summary>
    /// Registers a middleware into a named Waterfall pipeline.
    /// </summary>
    IDisposable RegisterWaterfall<TContext>(string pipelineName, Func<TContext, Func<Task>, Task> middleware);

    /// <summary>
    /// Executes a named Waterfall pipeline.
    /// </summary>
    Task ExecuteWaterfallAsync<TContext>(string pipelineName, TContext context, Func<Task>? terminal = null);

    /// <summary>
    /// Registers a handler into a named Bail pipeline.
    /// </summary>
    IDisposable RegisterBail<TContext, TResult>(string pipelineName, Func<TContext, Task<TResult?>> handler) where TResult : class;

    /// <summary>
    /// Executes a named Bail pipeline.
    /// </summary>
    Task<TResult?> ExecuteBailAsync<TContext, TResult>(string pipelineName, TContext context) where TResult : class;

    /// <summary>
    /// Registers a handler into a named Parallel pipeline.
    /// </summary>
    IDisposable RegisterParallel<TContext>(string pipelineName, Func<TContext, Task> handler);

    /// <summary>
    /// Executes a named Parallel pipeline.
    /// </summary>
    Task ExecuteParallelAsync<TContext>(string pipelineName, TContext context);

    /// <summary>
    /// Registers a handler into a named Serial pipeline.
    /// </summary>
    IDisposable RegisterSerial<TContext>(string pipelineName, Func<TContext, Task> handler);

    /// <summary>
    /// Executes a named Serial pipeline.
    /// </summary>
    Task ExecuteSerialAsync<TContext>(string pipelineName, TContext context);

    // --- 4. Tool Registry ---

    /// <summary>
    /// Event fired when a tool is dynamically registered or unregistered.
    /// </summary>
    event Action? ToolsChanged;

    /// <summary>
    /// Registers a PDF tool contributed by a plugin.
    /// </summary>
    IDisposable RegisterTool(PdfToolDescriptor tool);

    /// <summary>
    /// Gets all currently registered PDF tools.
    /// </summary>
    IReadOnlyList<PdfToolDescriptor> GetRegisteredTools();

    /// <summary>
    /// Finds a tool by its unique string identifier.
    /// </summary>
    PdfToolDescriptor? GetTool(string id);

    // --- 5. Canvas Element Registry ---

    /// <summary>
    /// Registers a custom canvas element type contributed by a plugin.
    /// </summary>
    IDisposable RegisterCanvasElement(CanvasElementDescriptor descriptor);

    /// <summary>
    /// Gets all registered canvas element types.
    /// </summary>
    IReadOnlyList<CanvasElementDescriptor> GetRegisteredCanvasElements();

    /// <summary>
    /// Finds a canvas element descriptor by its unique element type identifier.
    /// </summary>
    CanvasElementDescriptor? GetCanvasElement(string elementTypeId);

    // --- 6. Ribbon Action Registry ---

    /// <summary>
    /// Registers a dynamic Ribbon action button or menu item contributed by a plugin.
    /// </summary>
    IDisposable RegisterRibbonAction(RibbonActionDescriptor descriptor);

    /// <summary>
    /// Gets all registered Ribbon actions.
    /// </summary>
    IReadOnlyList<RibbonActionDescriptor> GetRegisteredRibbonActions();

    // --- 7. Document Importer Registry ---

    /// <summary>
    /// Registers a document importer contributed by a plugin.
    /// </summary>
    IDisposable RegisterImporter(IDocumentImporter importer);

    /// <summary>
    /// Gets all registered document importers.
    /// </summary>
    IReadOnlyList<IDocumentImporter> GetRegisteredImporters();

    // --- 8. Document Exporter Registry ---

    /// <summary>
    /// Registers a document exporter contributed by a plugin.
    /// </summary>
    IDisposable RegisterExporter(IDocumentExporter exporter);

    /// <summary>
    /// Gets all registered document exporters.
    /// </summary>
    IReadOnlyList<IDocumentExporter> GetRegisteredExporters();

    // --- 9. Inspector Panel Registry ---

    /// <summary>
    /// Registers an inspector section property panel contributed by a plugin.
    /// </summary>
    IDisposable RegisterInspectorSection(InspectorSectionDescriptor descriptor);

    /// <summary>
    /// Gets all registered inspector sections.
    /// </summary>
    IReadOnlyList<InspectorSectionDescriptor> GetRegisteredInspectorSections();

    // --- 10. Document Template Registry ---

    /// <summary>
    /// Registers a document template contributed by a plugin.
    /// </summary>
    IDisposable RegisterTemplate(ITemplateDescriptor template);

    /// <summary>
    /// Gets all registered document templates.
    /// </summary>
    IReadOnlyList<ITemplateDescriptor> GetRegisteredTemplates();

    // --- 12. OCR Engine Registry ---

    /// <summary>
    /// Registers an OCR engine contributed by a plugin.
    /// </summary>
    IDisposable RegisterOcrEngine(IOcrEngine engine);

    /// <summary>
    /// Gets all registered OCR engines.
    /// </summary>
    IReadOnlyList<IOcrEngine> GetRegisteredOcrEngines();

    // --- 13. Data Connector Registry ---

    /// <summary>
    /// Registers a data connector contributed by a plugin.
    /// </summary>
    IDisposable RegisterDataConnector(IDataConnector connector);

    /// <summary>
    /// Gets all registered data connectors.
    /// </summary>
    IReadOnlyList<IDataConnector> GetRegisteredDataConnectors();

    // --- 14. Status Bar Registry ---

    /// <summary>
    /// Registers a status bar widget contributed by a plugin.
    /// </summary>
    IDisposable RegisterStatusBarWidget(StatusBarWidgetDescriptor widget);

    /// <summary>
    /// Gets all registered status bar widgets.
    /// </summary>
    IReadOnlyList<StatusBarWidgetDescriptor> GetRegisteredStatusBarWidgets();

    // --- 15. Command Palette Registry ---

    /// <summary>
    /// Registers a command searchable in the Command Palette (⌘K / Ctrl+K).
    /// </summary>
    IDisposable RegisterCommand(CommandPaletteDescriptor descriptor);

    /// <summary>
    /// Gets all registered command palette descriptors.
    /// </summary>
    IReadOnlyList<CommandPaletteDescriptor> GetRegisteredCommands();

    // --- 16. Navigation Registry ---

    /// <summary>
    /// Registers a workspace navigation section or full-page view contributed by a plugin.
    /// </summary>
    IDisposable RegisterNavigationItem(NavigationItemDescriptor descriptor);

    /// <summary>
    /// Gets all registered navigation item descriptors.
    /// </summary>
    IReadOnlyList<NavigationItemDescriptor> GetRegisteredNavigationItems();

    // --- 17. Dialog Registry ---

    /// <summary>
    /// Registers a modal dialog or floating studio contributed by a plugin.
    /// </summary>
    IDisposable RegisterDialog(DialogDescriptor descriptor);

    /// <summary>
    /// Gets all registered modal dialog descriptors.
    /// </summary>
    IReadOnlyList<DialogDescriptor> GetRegisteredDialogs();

    // --- 18. Sidebar Registry ---

    /// <summary>
    /// Registers an editor sidebar panel contributed by a plugin.
    /// </summary>
    IDisposable RegisterSidebarTab(SidebarTabDescriptor descriptor);

    /// <summary>
    /// Gets all registered editor sidebar tab descriptors.
    /// </summary>
    IReadOnlyList<SidebarTabDescriptor> GetRegisteredSidebarTabs();

    // --- 19. Dynamic Ribbon Tabs & Groups ---

    /// <summary>
    /// Registers a dynamic Ribbon Tab contributed by a plugin.
    /// </summary>
    IDisposable RegisterRibbonTab(RibbonTabDescriptor descriptor);

    /// <summary>
    /// Registers a dynamic Ribbon tool group inside a Ribbon tab.
    /// </summary>
    IDisposable RegisterRibbonGroup(RibbonGroupDescriptor descriptor);
}

