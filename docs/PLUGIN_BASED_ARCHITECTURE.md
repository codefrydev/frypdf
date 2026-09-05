# FryPDF Plugin-Based Architecture ("Everything is a Plugin")

> **Architectural Specification and Design Manual**  
> Inspired by **DeepSeek Harness (`deepseek-harness`)** and the **Cordis** Spatiotemporal Composability Framework.

---

## 1. Executive Summary & Vision

In traditional desktop and document software architectures, core features are compiled into monolithic service collections, fixed enum registries, and rigid UI templates. Adding a new file conversion tool, AI assistant, or custom canvas element requires modifying numerous central files.

**DeepSeek Harness (`deepseek-harness`)** pioneered an **"Everything is a Plugin"** paradigm:
- The core runtime does not privilege any single subsystem.
- The agent loop, LLM providers, tool execution pipelines, memory logging, and UI shells are all plugins mounted on a unified context.
- Plugins declare their dependencies, register capabilities dynamically, participate in execution pipelines via typed middleware, and clean up their state via reversible effects.

This document defines how **FryPDF** adopts this paradigm using modern **C# 13, .NET 10, SkiaSharp, and Avalonia UI (Material Design 3 Expressive)**.

---

## 2. Core Architectural Principles

```
                               ┌──────────────────────────────────────────────┐
                               │           FryPDF Plugin Kernel               │
                               │  - Service Bus (ctx.Register / ctx.Get)      │
                               │  - Pipeline Engine (Waterfall, Bail, etc.)   │
                               │  - Dependency Graph Solver (DAG)             │
                               │  - Reversible Effect Tracker (LIFO Rollback) │
                               └──────────────────────┬───────────────────────┘
                                                      │
         ┌─────────────────────────┬──────────────────┴──────────────┬─────────────────────────┐
         │                         │                                 │                         │
┌────────┴────────┐       ┌────────┴────────┐               ┌────────┴────────┐       ┌────────┴────────┐
│   Core Engines  │       │    PDF Tools    │               │   Data Studio   │       │   Avalonia UI   │
│ - SkiaRaster    │       │ - MergePlugin   │               │ - ExcelParser   │       │ - RibbonContrib │
│ - UglyToadParse │       │ - CompressPlugin│               │ - CsvParser     │       │ - DynamicCanvas │
│ - QuestPdfGen   │       │ - OcrPlugin     │               │ - BatchGenEngine│       │ - InspectorSec  │
└─────────────────┘       └─────────────────┘               └─────────────────┘       └─────────────────┘
```

The FryPDF Plugin Engine is founded on **5 core pillars**:

### A. The Inverted Capability Context (`IFryPluginContext`)
Rather than relying on static `ServiceCollection` compile-time binding, plugins communicate via a dynamic, thread-safe capability context. Any plugin can contribute services or query them.

### B. Declarative Dependency Graphs (`[DependsOn]` / `RequiredServices`)
Plugins declare the services or capabilities they depend upon. The kernel constructs a Directed Acyclic Graph (DAG) and executes a topological sort, ensuring dependencies activate before consumers without requiring manual boot orchestration.

### C. Reversible Effects (`PluginScope` & LIFO Teardown)
Every registration—whether an event listener, a tool descriptor, a canvas element template, or an unmanaged Skia bitmap—is recorded as an "effect". When a plugin unloads or is disabled, its effect scope unwinds in reverse order, ensuring **zero memory leaks**, clean garbage collection, and no dangling handlers.

### D. Typed Pipeline Dispatch Modes
FryPDF adopts the 5 dispatch modes of DeepSeek Harness / Cordis:
1. **`Waterfall`**: Around-middleware with `next()` delegation (e.g. Export pipelines, page rendering filters).
2. **`Bail`**: Short-circuits on the first non-null/handled result (e.g. File format converters, OCR engines).
3. **`Parallel`**: Concurrent asynchronous dispatch via `Task.WhenAll` (e.g. Analytics, auto-save notifications).
4. **`Serial`**: Sequentially awaited execution (e.g. Validation rules, digital signature verification).
5. **`Emit`**: Fire-and-forget notification across decoupled viewmodels.

### E. Composable Profiles & Bundles
Capabilities are organized into self-contained **Bundles** and composed into runtime **Profiles** (`desktop`, `headless-cli`, `sdk`). A JSON configuration overlay can enable, disable, or replace any plugin without recompiling the application.

---

## 3. Kernel Abstractions (C# 13 / .NET 10)

### 3.1 The Plugin Interface (`IFryPlugin`)

```csharp
namespace PdfEditorApp.Core.Plugins;

public interface IFryPlugin
{
    /// <summary>Unique identifier, e.g. "frypdf.tool.merge" or "com.vendor.watermark"</summary>
    string Id { get; }

    /// <summary>Human-readable display name</summary>
    string Name { get; }

    /// <summary>Semantic version</summary>
    Version Version { get; }

    /// <summary>Required service contracts (equivalent to Cordis 'inject')</summary>
    IReadOnlyList<Type> RequiredServices { get; }

    /// <summary>Mounts the plugin into the active context.</summary>
    Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default);
}
```

### 3.2 The Plugin Context (`IFryPluginContext`)

```csharp
namespace PdfEditorApp.Core.Plugins;

public interface IFryPluginContext
{
    // 1. Service Bus
    void RegisterService<TService>(TService implementation) where TService : class;
    TService GetService<TService>() where TService : class;
    bool TryGetService<TService>([NotNullWhen(true)] out TService? service) where TService : class;

    // 2. Reversible Effects
    IDisposable RegisterEffect(Action onDispose);

    // 3. Typed Dispatch Pipelines
    void RegisterWaterfall<TContext>(string pipelineName, Func<TContext, Func<Task>, Task> middleware);
    Task ExecuteWaterfallAsync<TContext>(string pipelineName, TContext context);

    void RegisterBail<TContext, TResult>(string pipelineName, Func<TContext, Task<TResult?>> handler);
    Task<TResult?> ExecuteBailAsync<TContext, TResult>(string pipelineName, TContext context);

    void RegisterParallel<TContext>(string pipelineName, Func<TContext, Task> handler);
    Task ExecuteParallelAsync<TContext>(string pipelineName, TContext context);

    void RegisterSerial<TContext>(string pipelineName, Func<TContext, Task> handler);
    Task ExecuteSerialAsync<TContext>(string pipelineName, TContext context);

    // 4. Extensibility Registries
    void RegisterTool(PdfToolDescriptor tool);
    void RegisterCanvasElement(CanvasElementDescriptor element);
    void RegisterRibbonContribution(RibbonContribution contribution);
    void RegisterInspectorSection(InspectorSectionDescriptor section);
}
```

### 3.3 Reversible Effect Scope (`PluginScope`)

```csharp
namespace PdfEditorApp.Core.Plugins;

public sealed class PluginScope : IDisposable
{
    private readonly Stack<Action> _disposers = new();
    private bool _isDisposed;

    public IDisposable RegisterEffect(Action onDispose)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _disposers.Push(onDispose);
        return new DisposableAction(() => { /* idempotent unregister */ });
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        while (_disposers.TryPop(out var disposeAction))
        {
            try
            {
                disposeAction();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PluginScope] Error during effect rollback: {ex.Message}");
            }
        }
    }
}
```

---

## 4. Pipeline Execution Mechanics

### 4.1 The Waterfall Pipeline (Around-Middleware)

The Waterfall pattern allows plugins to intercept and wrap core operations. For example, during PDF Export:

```
[Request: Export PDF]
   │
   ▼
[AuditPlugin (Pre-check)] ──► Calls next()
   │
   ▼
[WatermarkPlugin (Injects Stamp)] ──► Calls next()
   │
   ▼
[QuestPdfEngine (Generates Binary Stream)]
   │
   ▼
[EncryptionPlugin (Encrypts AES-256)] ◄── Returning up the stack
   │
   ▼
[AuditPlugin (Logs completion)] ◄── Returning up the stack
   │
   ▼
[Completed File Stream]
```

Implementation signature:
```csharp
ctx.RegisterWaterfall<PdfExportContext>("pdf:export", async (context, next) =>
{
    // Pre-processing
    context.Document.Metadata.Title += " (Watermarked)";
    
    // Delegate to inner pipeline
    await next();
    
    // Post-processing
    RecordTelemetry(context.TargetStream.Length);
});
```

### 4.2 The Bail Pipeline (First Handled Result)

Used when multiple plugins could potentially handle an operation, but only the first matching one should execute (e.g., converting `.docx`, `.xlsx`, `.md`, or `.html` to PDF):

```csharp
ctx.RegisterBail<FileConversionRequest, Stream>("convert:to_pdf", async req =>
{
    if (!req.SourceFilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        return null; // Bail to next listener

    return await ConvertMarkdownToPdfStreamAsync(req.SourceFilePath);
});
```

---

## 5. Avalonia Material Design 3 UI Extensibility

FryPDF's UI adheres strictly to **Google Material Design 3 Expressive**. The plugin system extends the UI seamlessly without breaking M3 design tokens:

### 5.1 Dynamic Tool Registry
Instead of a fixed `enum PdfToolId`, tools register as `PdfToolDescriptor`:
```csharp
public record PdfToolDescriptor
{
    public required string Id { get; init; }
    public PdfToolId? LegacyId { get; init; } // Backwards compatibility
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string IconKind { get; init; }
    public string IconColorHex { get; init; } = "#2563EB";
    public string BackgroundAccentHex { get; init; } = "#EFF6FF";
    public bool SupportsMultiFile { get; init; }
    public string AcceptedFileExtensions { get; init; } = ".pdf";
    public required Func<IServiceProvider, PdfToolViewModelBase> CreateViewModel { get; init; }
}
```
`HomeViewModel` and `PdfToolRegistry` bind directly to this dynamic registry.

### 5.2 Dynamic Canvas Elements
Plugins can introduce new canvas element types (e.g., `BarcodeElement`, `LaTeXMathElement`, `RichStickyNoteElement`):
```csharp
ctx.RegisterCanvasElement(new CanvasElementDescriptor
{
    ElementTypeId = "frypdf.element.barcode",
    DisplayName = "Barcode / QR Code",
    ModelType = typeof(PdfBarcodeElementModel),
    ViewModelType = typeof(BarcodeElementViewModel),
    ViewTemplate = new FuncDataTemplate<BarcodeElementViewModel>((vm, _) => new BarcodeElementView { DataContext = vm })
});
```

### 5.3 Dynamic Ribbon Actions & Inspector Panels
Plugins register action pills into the M3 Ribbon and property sections into `InspectorViewModel`:
- Pills automatically receive `CornerRadius="{StaticResource M3ShapeCornerFull}"`.
- Cards and panels automatically inherit `M3ShapeCornerLarge` and dynamic theme brushes (`M3SurfaceContainerBrush`).

---

## 6. Bundles & Composable Application Profiles

Capabilities are partitioned into standard bundles:

| Bundle Name | Contained Capabilities |
| :--- | :--- |
| **`FryPdf.Bundle.Core`** | SkiaSharp raster engine, UglyToad.PdfPig deconstruction, QuestPDF exporter, font package service. |
| **`FryPdf.Bundle.Tools.Organize`** | Merge, Split, Rotate, Organize, Crop, Page Numbers tools. |
| **`FryPdf.Bundle.Tools.Security`** | Compress, Repair, Protect, Unlock, Sign, Redact, Watermark tools. |
| **`FryPdf.Bundle.Tools.Conversion`** | PDF $\leftrightarrow$ Word, Excel, PowerPoint, Markdown, HTML, Images, PDF/A. |
| **`FryPdf.Bundle.Tools.Intelligence`**| AI Summarizer, Document Translation, OCR, Document Compare. |
| **`FryPdf.Bundle.DataStudio`** | Tabular Data Studio, Excel/CSV ingestion, template binding, batch generation. |
| **`FryPdf.Bundle.AvaloniaUI`** | Avalonia M3 Shell, Main Window, Ribbon, Canvas, Inspector, Dialogs. |
| **`FryPdf.Bundle.Cli`** | Headless command-line parser and batch runner for CI/CD environments. |

### Profile Configurations

#### `profiles/desktop.profile.json` (Full Desktop App)
```json
{
  "profile": "desktop",
  "bundles": [
    "FryPdf.Bundle.Core",
    "FryPdf.Bundle.Tools.Organize",
    "FryPdf.Bundle.Tools.Security",
    "FryPdf.Bundle.Tools.Conversion",
    "FryPdf.Bundle.Tools.Intelligence",
    "FryPdf.Bundle.DataStudio",
    "FryPdf.Bundle.AvaloniaUI"
  ]
}
```

#### `profiles/headless.profile.json` (Automated CLI / Server)
```json
{
  "profile": "headless",
  "bundles": [
    "FryPdf.Bundle.Core",
    "FryPdf.Bundle.Tools.Organize",
    "FryPdf.Bundle.Tools.Conversion",
    "FryPdf.Bundle.Cli"
  ]
}
```

---

## 7. Runtime Dynamic Loading (`AssemblyLoadContext`)

For external or community plugins loaded from a `plugins/` directory:

```csharp
public class FryPluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public FryPluginLoadContext(string pluginDllPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginDllPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }
}
```

- **Collectible ALC (`isCollectible: true`)**: Allows unloading a plugin from memory entirely when disabled.
- **Zero Process Restart**: Plugins can be installed, updated, or uninstalled dynamically at runtime.

---

## 8. Real-Time Reactive Dynamic UI Integrations

To achieve seamless desktop integration while maintaining strict decoupling, the UI shell connects to the plugin kernel via dynamic observable registries:

### 8.1 Dynamic Ribbon Tabs & Tool Groups
- **`IRibbonRegistry`**: Plugins register dynamic tabs (`RibbonTabDescriptor`) and groups (`RibbonGroupDescriptor`) containing actionable buttons with command delegates.
- **Presentation Models**: `DynamicRibbonTabViewModel` and `DynamicRibbonGroupViewModel` wrap descriptors with observable `IsActive` states.
- **Seamless Switching**: In `MainViewModel.cs`, selecting a dynamic ribbon tab sets `ActiveRibbonTab = (RibbonTabKind)(-1)`. Avalonia's built-in tab content controls cleanly hide, and the active dynamic group/tool collection is rendered instantly.

### 8.2 Dynamic Sidebar Tabs & Extensible Panes
- **`ISidebarRegistry`**: Plugins contribute custom sidebars (e.g. digital signature audit logs, semantic AI search trees, custom metadata views) via `SidebarTabDescriptor`.
- **View/ViewModel Factories**: Descriptors provide `ViewFactory` and `ViewModelFactory` delegates to instantiate user interfaces lazily.
- **Clean Multiplexing**: Activating a dynamic sidebar tab sets `ActiveSidebarTab = (SidebarTabKind)(-1)` and mounts `ActiveDynamicSidebarView` directly into `<ContentControl>` within `ThumbnailSidebarView.axaml`.

### 8.3 Dynamic Inspector Sections
- **`IInspectorRegistry`**: Allows plugins to contribute custom property panels targeting specific canvas elements or selection states (e.g. cryptographic signature properties, barcode encoding parameters).
- **Reactive Refresh**: `InspectorViewModel` subscribes to `IInspectorRegistry.RegistryChanged` and invokes `RefreshDynamicSections(SelectedElement)` whenever plugins load, unload, or element selections change.

---

## 9. Declarative Plugin Settings Schemas & M3 Expressive Configurator

Plugins often require user-configurable options (such as API keys, custom server endpoints, model selection tags, or toggleable behaviors).

### 9.1 Schema Definition (`PluginSettingDefinition`)
Plugins declare typed setting schemas via `IFryPlugin.SettingsSchema`:

```csharp
public IReadOnlyDictionary<string, PluginSettingDefinition>? SettingsSchema => new Dictionary<string, PluginSettingDefinition>
{
    ["Endpoint"] = new()
    {
        Key = "Endpoint",
        Label = "Ollama Host URL",
        Description = "HTTP endpoint for the Ollama inference server",
        Type = PluginSettingType.String,
        DefaultValue = "http://localhost:11434"
    },
    ["ApiKey"] = new()
    {
        Key = "ApiKey",
        Label = "Groq API Key",
        Description = "Secret API key for high-speed cloud inference",
        Type = PluginSettingType.Secret,
        IsRequired = true
    },
    ["Model"] = new()
    {
        Key = "Model",
        Label = "Language Model",
        Type = PluginSettingType.Select,
        Options = ["llama-3.3-70b-versatile", "mixtral-8x7b-32768"],
        DefaultValue = "llama-3.3-70b-versatile"
    }
};
```

### 9.2 Persistence & Auto-Generated M3 Expressive UI
- **`IPluginSettingsStore`**: Typed values are serialized safely to `plugin_settings.json` under the user configuration directory via `FilePluginSettingsStore`.
- **Live Configuration Dialog**: In `PluginsDialog.axaml`, clicking "Settings" on any plugin renders an auto-generated Material Design 3 Expressive form supporting:
  - **Text**: `TextBox.m3-outlined`
  - **Secrets**: `TextBox.m3-outlined` with `PasswordChar="●"`
  - **Booleans**: Tactile M3 `ToggleSwitch`
  - **Numbers**: `NumericUpDown.m3-outlined`
  - **Options**: `ComboBox.m3-outlined`

---

## 10. Self-Contained `.fryplugin` Package Format & Drag-and-Drop Installation

To support third-party distribution and friction-free user installation:

### 10.1 Package Specification
A `.fryplugin` bundle is a standard ZIP archive containing:
```
my-extension.fryplugin
├── plugin.json               # Manifest (id, name, version, author, entryPoint, etc.)
├── MyExtension.dll           # Entry assembly implementing IFryPlugin
└── dependencies/             # Supplementary assemblies resolved at runtime
```

### 10.2 Drag-and-Drop & Collectible ALC Loading
- **Drag-and-Drop**: Users can drag `.fryplugin` or `.dll` files directly onto `PluginsDialog.axaml`. Avalonia's `DataTransfer` API extracts file paths and initiates automated installation.
- **`FryPluginPackageLoader`**: Unpacks the archive into the isolated user plugin directory (`~/.frypdf/plugins/<id>/`) and loads the entry assembly using a collectible `AssemblyLoadContext`.
- **Instant Hot-Mount**: The plugin is immediately registered, dependency-resolved, and mounted without restarting the application.

---

## 11. Conclusion

By adopting the **DeepSeek Harness / Cordis** architecture:
- **FryPDF transforms from a compiled application into a composable PDF Operating Platform**.
- Core engines, tools, AI services, ribbon tabs, sidebars, inspector panels, and settings become modular, swappable, and testable in total isolation.
- The system achieves infinite extensibility while remaining fast, memory-safe, and warning-free on .NET 10.
