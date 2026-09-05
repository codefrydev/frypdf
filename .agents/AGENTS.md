# FryPDF Agent Operating Guide (AGENTS.md)

Welcome to the **FryPDF** repository. This guide provides AI agents and human contributors with the architectural guidelines, core workflows, conventions, and debugging procedures needed to work effectively in this codebase.

---

## 1. Project Overview & Tech Stack

**FryPDF** is a high-performance, cross-platform PDF creation, editing, and document analysis studio.

- **Framework**: .NET 10 / C# 13+
- **Architecture**: Microkernel Plugin System ("Everything is a Plugin") inspired by DeepSeek Harness and Cordis
- **UI Toolkit**: Avalonia UI (v12.x) with Google Material Design 3 (M3) Expressive styling (macOS, Windows, Linux)
- **MVVM**: `CommunityToolkit.Mvvm` (Observable Objects, Relay Commands, Source Generators)
- **PDF Generation & Export**: `QuestPDF`
- **PDF Parsing & Deconstruction**: `UglyToad.PdfPig` & `PdfPig.DocumentLayoutAnalysis`
- **Raster & Graphics Engine**: `SkiaSharp`
- **Testing**: `xUnit` + Skia visual verification testing suite (770+ unit and integration tests)

---

## 2. Essential Commands

### Build Solution
```bash
dotnet build
```

### Run Full Test Suite (770+ Unit & Integration Tests)
```bash
dotnet test
```

### Run Specific / Filtered Tests
```bash
dotnet test --filter "FullyQualifiedName~GestureAndNavigationTests"
dotnet test --filter "FullyQualifiedName~PluginKernelTests"
dotnet test --filter "FullyQualifiedName~PluginsStudioUiTests"
dotnet test --filter "FullyQualifiedName~Material3ExpressiveThemeTests"
dotnet test --filter "FullyQualifiedName~GenerateVisualComparison_SideBySide"
```

### Run Desktop App Locally
```bash
dotnet run --project src/PdfEditorApp/PdfEditorApp.csproj
```

---

## 3. Project Structure

```
PDFCreator/
├── src/
│   ├── PdfEditorApp.Core/          # Pure headless core engine (cross-platform, zero UI dependencies)
│   │   ├── Analysis/               # Layout analysis, typography, Unicode script detection
│   │   ├── Deconstruction/         # PDF deconstruction engine (PDF stream to live editable model)
│   │   ├── Models/                 # Domain element POCOs (Text, Image, Shape, Table, Chart, Math, Form)
│   │   └── Plugins/                # Microkernel plugin infrastructure (IFryPluginContext, DAG, Pipelines)
│   │       ├── Descriptors/        # Extension descriptors (Tools, Ribbon, Sidebars, Inspector, Navigation)
│   │       ├── Pipelines/          # Typed dispatch pipelines (Waterfall, Bail, Parallel, Serial, Emit)
│   │       ├── Profiles/           # App composition profiles (desktop, headless-cli, sdk) and bundle contracts
│   │       └── Settings/           # Typed plugin settings schema and persistent file store
│   └── PdfEditorApp/               # Main Avalonia desktop application
│       ├── Assets/                 # Embedded fonts (Noto Sans, Roboto, Inter, etc.) and SVG icons
│       ├── Plugins/                # Desktop plugin implementations and modular bundles
│       │   ├── Bundles/            # 16 standard bundles (Tools, Pages, Sidebars, AI, CanvasElements, etc.)
│       │   └── Loader/             # Collectible AssemblyLoadContext and .fryplugin package extractor
│       ├── Services/               # Dynamic registries (Tools, Ribbon, Sidebars, Inspector, Navigation, AI)
│       ├── ViewModels/             # MVVM ViewModels (MainViewModel, HomeViewModel, ElementViewModels)
│       └── Views/                  # Avalonia XAML Views, canvas controls, and M3 dialogs
├── tests/
│   └── PdfEditorApp.Tests/         # Comprehensive xUnit test suite (770+ tests) & visual verification suite
├── docs/                           # Architecture, contributing, M3 guidelines, and plugin manuals
├── AGENTS.md                       # Root link to this operating guide
├── GEMINI.md                       # Immediate critical agent mandate instructions
└── README.md                       # Repository overview
```

---

## 4. Architectural Rules & Subsystem Guidelines

### A. PDF Deconstruction Engine ([`PdfDeconstructionEngine.cs`](src/PdfEditorApp.Core/Deconstruction/PdfDeconstructionEngine.cs))

When importing 3rd-party PDFs for live editing:

1. **Strict Layered Z-Index Allocation**:
   - **`ZIndex = 0..99`**: Background container shapes ($W \ge 120\text{ pt}, H \ge 80\text{ pt}$) and faint watermarks.
   - **`ZIndex = 100..499`**: Content images (photos, QR codes, emblems, government banners, logos).
   - **`ZIndex = 500..599`**: Structured tables and data grids.
   - **`ZIndex = 600..999`**: Foreground shapes, badges, and divider lines.
   - **`ZIndex = 1000..1999`**: Text blocks, headings, labels, and rotated vertical marginalia.
   - **`ZIndex = 2000+`**: Interactive form fields, checkboxes, and digital signatures.
   > **CRITICAL RULE**: Background shapes must NEVER be placed at a higher Z-index than content images. Otherwise, white container cards will occlude images underneath.

2. **Multi-Format Skia Image Extraction**:
   - Always use SkiaSharp multi-format decoding (`ExtractImageBytes`) to decode raw DCT/JPEG, JPEG2000, uncompressed CMYK/RGB, and monochrome 1-bit samples before re-encoding to standardized PNG bytes.

3. **Watermark Classification**:
   - Only classify an image as a background watermark if it spans $\ge 65\%$ page width and $\ge 55\%$ page height. Sub-page content images (such as information cards) must retain $100\%$ opacity.

---

### B. Layout Analysis & Typography Engine ([`PdfLayoutAnalyzer.cs`](src/PdfEditorApp.Core/Analysis/PdfLayoutAnalyzer.cs))

1. **Script-Aware Token Joining**:
   - For Indic scripts (Devanagari, Tamil, Telugu, etc.) and CJK, evaluate horizontal bounding-box gaps. Sub-threshold gaps must be joined without artificial spaces to preserve natural words.

2. **Paragraph Indent & Clustering**:
   - In `ShouldClusterLines`, check vertical line pitch ($\le 0.95 \times \text{FontSize}$).
   - If the next line has a positive left indent ($>8\text{ pt}$), break to start a new paragraph.

3. **Line Height Multipliers**:
   - `LineHeight` is a proportional multiplier ($1.25\times$ to $1.4\times\text{FontSize}$), not fixed points.

4. **Script-Aware Font Fallback ([`UnicodeScriptDetector.cs`](src/PdfEditorApp.Core/Analysis/UnicodeScriptDetector.cs))**:
   - Match detected Unicode script ranges to system/embedded font families (e.g. `Noto Sans Devanagari`, `Kohinoor Devanagari`, `Noto Sans SC`, `PingFang SC`) to eliminate tofu boxes (`□□□`).

---

### C. MVVM Architecture & Maximum CommunityToolkit.Mvvm Utilization

Always take maximum advantage of **`CommunityToolkit.Mvvm`** features and source generators across all ViewModels:

1. **Source Generators Over Boilerplate**:
   - **`[ObservableProperty]`**: Always prefer `[ObservableProperty]` attributes on private fields (`[ObservableProperty] private string _title;`) to let Roslyn source generators generate clean, type-safe public properties and partial change methods (`OnTitleChanged`).
   - **`[NotifyPropertyChangedFor]`**: Use `[NotifyPropertyChangedFor(nameof(DependentProperty))]` on backing fields to automatically cascade dependent property notifications instead of manually calling `OnPropertyChanged` in property bodies.
   - **`[NotifyCanExecuteChangedFor]`**: Use `[NotifyCanExecuteChangedFor(nameof(MyCommand))]` on state-driving properties so buttons bound to commands automatically refresh their enabled/disabled visual state without manual polling.

2. **Relay Commands & Async Operations**:
   - **`[RelayCommand]`**: Always use `[RelayCommand]` on methods instead of declaring manual `IRelayCommand` fields and instantiating `new RelayCommand(...)`.
   - **Async Relay Commands**: Declare asynchronous operations directly as `[RelayCommand] private async Task LoadDocumentAsync(...)`. CommunityToolkit automatically manages `IsRunning`, concurrent execution locking, and cancellation token propagation.
   - **CanExecute Wiring**: Use `[RelayCommand(CanExecute = nameof(CanPerformAction))]` for clean condition-based execution guards.

3. **Loose Coupling & Memory-Safe Messaging (`WeakReferenceMessenger`)**:
   - Use `WeakReferenceMessenger.Default.Send(new Message(...))` and `Register<Message>(this, ...)` for decoupled communication between services, ribbons, sidebars, and dialogs.
   - Never pass strong references to ViewModels or Views to prevent memory retention cycles.
   - For ViewModels requiring lifetime-bound messaging, inherit from `ObservableRecipient` and activate/deactivate with `IsActive = true/false`.

4. **Input Validation with `ObservableValidator`**:
   - Inherit from `ObservableValidator` for dialog forms and data entry ViewModels.
   - Use standard DataAnnotations (`[Required]`, `[Range]`, `[RegularExpression]`, `[CustomValidation]`) and call `ValidateProperty(value, nameof(...))` or `ValidateAllProperties()`.

5. **Canvas Element ViewModels**:
   - Every canvas element inherits from `ElementViewModelBase` and implements `LoadFromModel(PdfElementModelBase)` and `ToModel()`.
   - Property changes must notify to support live canvas rendering, property sidebar binding, and atomic undo/redo records.
   - Keep business logic and coordinate conversions inside `Core` and `Services`, leaving ViewModels focused on presentation state and commands.

---

### D. Avalonia UI Data Binding & Value Converters

When binding ViewModels to Avalonia XAML Views:

1. **Verify Converter Requirement for Data Types & Transforms**:
   - Always check if a ViewModel property type differs from the View property type (e.g. `double` $\to$ `ITransform`, `string` hex $\to$ `IBrush`/`Color`, `long` bytes $\to$ formatted size, `enum` $\to$ display title, `double` points $\to$ mm/inches).
   - **Critical Avalonia Rule**: Never instantiate an `AvaloniaObject` transform directly inside a Style/ControlTheme Setter (e.g. `<Setter Property="RenderTransform"><RotateTransform Angle="{Binding Rotation}" /></Setter>`). Avalonia `Transform` instances do **not** inherit `DataContext` inside Setter values, causing `{Binding}` to fail silently to 0. Always bind through an `IValueConverter`:
     ```xml
     <Setter Property="RenderTransformOrigin" Value="50%,50%" />
     <Setter Property="RenderTransform" Value="{Binding Rotation, Converter={StaticResource RotationToTransformConverter}}" />
     ```

2. **Check Existing Converter Registrations**:
   - Inspect [`CommonConverters.cs`](src/PdfEditorApp/Converters/CommonConverters.cs) and [`ExtendedConverters.cs`](src/PdfEditorApp/Converters/ExtendedConverters.cs) before writing ad-hoc inline bindings.
   - When adding a new converter, register it in [`App.axaml`](src/PdfEditorApp/App.axaml) for global application availability.

3. **Converter Quality & Testing**:
   - Implement `ConvertBack` whenever two-way binding is required (e.g. numeric adjustments, unit conversions, color views).
   - Handle `null` and invalid input gracefully without throwing unhandled exceptions.
   - Add deterministic unit test assertions in [`tests/PdfEditorApp.Tests/ConverterTests.cs`](tests/PdfEditorApp.Tests/ConverterTests.cs).

---

### E. Memory Leak Prevention, Resource Disposal & LOH Management

To maintain high responsiveness and prevent memory leaks during long editing sessions and large PDF processing:

1. **Large Object Heap (LOH) & Base64 Bloat Prevention**:
   - **Never** store Base64 strings for raw image or vector assets inside document models (`PdfDocumentModel`, `PdfPageModel`, `PdfImageElement`) or ViewModels.
   - Store raw `byte[] ImageData` directly. Any object $\ge 85,000\text{ bytes}$ is allocated on the LOH, which is not compacted in standard GC cycles and causes memory fragmentation.
   - `Base64Data` must only be computed lazily on demand for JSON serialization.

2. **Unmanaged Graphics Handle & Bitmap Disposal**:
   - `SKBitmap`, `SKImage`, `SKSurface`, `SKPixmap`, `SKData`, `SKCodec`, and Avalonia `Bitmap`/`WriteableBitmap` wrap native unmanaged graphics handles (`IntPtr`).
   - Always wrap short-lived Skia graphics objects in `using` statements.
   - In ViewModels ([`ImageElementViewModel.cs`](src/PdfEditorApp/ViewModels/ElementViewModels/ImageElementViewModel.cs), [`ChartElementViewModel.cs`](src/PdfEditorApp/ViewModels/ElementViewModels/ChartElementViewModel.cs)), always dispose previous bitmap instances before assigning a new `PreviewBitmap` or `ChartBitmap`:
     ```csharp
     var oldBitmap = _previewBitmap;
     _previewBitmap = newBitmap;
     OnPropertyChanged(nameof(PreviewBitmap));
     oldBitmap?.Dispose();
     ```
   - When closing pages or clearing canvas items, ensure child element view models dispose their bitmaps.

3. **Event Handler & Subscription Lifecycle**:
   - Do not attach event listeners (`PropertyChanged`, `CollectionChanged`, timers) to singleton services from transient ViewModels without an unsubscription mechanism.
   - Prefer `CommunityToolkit.Mvvm` `WeakReferenceMessenger` for cross-component communication to avoid strong reference cycles that prevent garbage collection.

4. **PDF Streams & UglyToad.PdfPig Lifecycle**:
   - `PdfDocument.Open(...)` parses internal object tables and font dictionaries. Always wrap `PdfDocument` instances in `using` blocks in [`PdfDeconstructionEngine.cs`](src/PdfEditorApp.Core/Deconstruction/PdfDeconstructionEngine.cs) and services.
   - Always dispose input/output `MemoryStream` and `FileStream` objects promptly.

5. **Bounded Undo/Redo History**:
   - The undo/redo command stack must remain bounded (e.g. max 50 actions) to prevent unlimited memory growth from document state snapshots.

---

### F. Google Material Design 3 (M3) Expressive UI Mandate (STRICT)

**CRITICAL MANDATE**: ALL future UI changes, new views, dialogs, sidebars, controls, or visual enhancements across FryPDF **MUST strictly adhere to Google Material Design 3 (M3) Expressive** design principles. Flat, rigid, or rectangular legacy styles are strictly forbidden.

1. **Expressive Shape Scale Hierarchy**:
   - Never hardcode arbitrary small corner radii (`CornerRadius="2"`, `"4"`, etc.) on major interactive elements.
   - Always reference the centralized M3 shape scale tokens defined in [`Material3ExpressiveTokens.axaml`](src/PdfEditorApp/Styles/Material3ExpressiveTokens.axaml):
     - **`M3ShapeCornerNone` (0px)**: Edge-to-edge full-bleed panels and splitters.
     - **`M3ShapeCornerExtraSmall` (4px)**: Micro badges, code spans, keyboard shortcut badges (`kbd-badge`).
     - **`M3ShapeCornerSmall` (8px)**: Badges, thumbnail cards, compact chips, small status pills.
     - **`M3ShapeCornerMedium` (12px)**: Context menus, tooltips, list items, card inner sections.
     - **`M3ShapeCornerLarge` (16px)**: Content cards (`m3-card-elevated`), `TextBox.m3-outlined`, `ComboBox`, `NumericUpDown`, inspector section groups.
     - **`M3ShapeCornerExtraLarge` (28px)**: Modal dialog cards (`Border.m3-dialog-card`, `AboutDialog`, `CommandPaletteDialog`, `AiAssistantDialog`), hero promo banners.
     - **`M3ShapeCornerFull` (9999px)**: Tactile pill buttons (`primary-btn`, `accent-btn`, `action-pill-btn`, `m3-filled-btn`, `m3-tonal-btn`), segmented button capsules (`m3-segmented-container`), global search bars (`TextBox.m3-search`), Floating Action Buttons (`m3-fab`), and chubby slider thumbs.

2. **Tonal Palette & Dynamic Color Roles**:
   - Never hardcode light/dark hex colors (e.g. `#FFFFFF`, `#000000`, `#F1F5F9`) in templates or hover triggers that break dark mode adaptability.
   - Always reference semantic M3 color roles via `{DynamicResource ...}`:
     - **Primary Actions**: `M3PrimaryBrush`, `M3OnPrimaryBrush`, `M3PrimaryContainerBrush`, `M3OnPrimaryContainerBrush`.
     - **Secondary / Active Accents**: `M3SecondaryBrush`, `M3SecondaryContainerBrush`, `M3OnSecondaryContainerBrush`.
     - **Surfaces & Layers**: `M3SurfaceBrush`, `M3SurfaceDimBrush`, `M3SurfaceContainerLowestBrush` through `M3SurfaceContainerHighestBrush`.
     - **Outlines & Dividers**: `M3OutlineBrush`, `M3OutlineVariantBrush`.
     - **Legacy Aliases**: When maintaining existing views, legacy aliases (`WinBgBrush`, `WinPanelBrush`, `WinBorderBrush`, `WinAccentBrush`, `WinTextBrush`, `WinMutedBrush`, `WinHoverBrush`, `WinActiveBrush`, `WinInputBgBrush`) are supported and automatically map to M3 Expressive tokens.

3. **Standard Component Classes ([`Material3ExpressiveStyles.axaml`](src/PdfEditorApp/Styles/Material3ExpressiveStyles.axaml))**:
   - **Buttons**: Use standard M3 hierarchy (`m3-filled-btn`, `m3-tonal-btn`, `m3-elevated-btn`, `m3-outlined-btn`, `m3-text-btn`, `m3-fab`, `m3-fab-extended`, `m3-icon-btn`).
   - **Segmented Capsules**: Wrap segmented buttons in `Border.m3-segmented-container` with child `Button.m3-segment-btn` or `RadioButton.m3-segment-btn`.
   - **Chips**: Use `Button.m3-chip`, `Button.m3-filter-chip`, or `ToggleButton.m3-filter-chip` with `M3ShapeCornerSmall` or pill geometry.
   - **Cards & Dialogs**: Use `Border.m3-card-elevated`, `Border.m3-card-filled`, `Border.m3-card-outlined`, and `Border.m3-dialog-card`.
   - **Inputs & Search**: Use `TextBox.m3-outlined` for forms/inspector and `TextBox.m3-search` for pill search fields.
   - **Tactile Chubby Sliders**: Retain chubby tactile sliders with `SliderTrackThemeHeight` ($\ge 8\text{px}$), `SliderHorizontalThumbWidth` ($\ge 20\text{px}$), and smooth hover scale transitions.

4. **Elevation Shadows & Motion Physics**:
   - Apply elevation on `Border` elements using multi-layered diffuse shadows: `{StaticResource M3ElevationLevel0}` through `{StaticResource M3ElevationLevel5}`. (Note: `BoxShadow` is an Avalonia `Border` property; do not apply it directly to `Button` or `ContextMenu` selectors).
   - Use subtle, springy micro-transitions (`BrushTransition` 150ms, `TransformOperationsTransition` 150ms) on hover and pressed states.

5. **Regression Verification**:
   - Every new token or UI component style must be covered in [`tests/PdfEditorApp.Tests/Material3ExpressiveThemeTests.cs`](tests/PdfEditorApp.Tests/Material3ExpressiveThemeTests.cs) and pass `dotnet test`.
   - Build must always remain 100% warning-free (`TreatWarningsAsErrors=true`).

---

### G. Modular Microkernel Plugin Architecture ("Everything is a Plugin") (STRICT)

**CRITICAL ARCHITECTURAL MANDATE**:
FryPDF implements an **"Everything is a Plugin"** microkernel architecture inspired by **DeepSeek Harness (`deepseek-harness`)** and the **Cordis** framework.
**Monolithic additions, hardcoded switch-case statements, god viewmodels, and hardwired view instantiations are strictly forbidden.**
Whenever you create new tools, add document operations, contribute ribbon actions, add sidebar panels, introduce inspector sections, or add new canvas element types, you **MUST implement them as modular plugins registered through dynamic capability registries**.

#### 1. The 5 Core Architectural Pillars
- **A. Inverted Capability Context (`IFryPluginContext`)**: Plugins register and consume capabilities via a thread-safe context (`ctx.RegisterService<T>`, `ctx.GetService<T>`, `ctx.TryGetService<T>`).
- **B. Directed Acyclic Graph (DAG) Dependency Solver**: Plugins declare dependencies via `RequiredServices` or `[DependsOn]`. The kernel sorts and mounts dependencies in topological order.
- **C. Reversible Effects (`PluginScope` & LIFO Rollback)**: Every registration (listeners, commands, event handlers) is tracked as an effect. Unloading a plugin unwinds its effect stack in reverse order, ensuring 100% clean teardown with zero dangling subscriptions.
- **D. Typed Dispatch Pipelines**:
  - `Waterfall`: Around-middleware with `next()` delegation (e.g. Export pipelines, document watermarking, audit trails).
  - `Bail`: Short-circuits on first handled result (e.g. multi-format document converters, OCR engines).
  - `Parallel`: Concurrent asynchronous dispatch via `Task.WhenAll` (e.g. analytics, autosave).
  - `Serial`: Sequential asynchronous dispatch (e.g. validation chains, signature checks).
  - `Emit`: Decoupled broadcast notifications.
- **E. Composable Profiles & Bundles**: Capabilities are grouped into self-contained `IFryPluginBundle` instances and composed into profiles (`desktop`, `headless-cli`, `sdk`).

#### 2. The 12 Dynamic Registry Pillars
Never write hardcoded UI wiring. Mount capabilities through the designated registry:

| Pillar / Registry | Descriptor | Typical Usage |
| :--- | :--- | :--- |
| **1. PDF Tools** | `IPdfToolRegistry` / `PdfToolDescriptor` | New tools (Merge, Split, Watermark, Compress, etc.). Inherit from `ToolPluginBase`. |
| **2. Dynamic Ribbon** | `IRibbonRegistry` / `RibbonContribution` | Custom ribbon tabs (`RibbonTabDescriptor`), groups, and action pill buttons. |
| **3. Extensible Sidebars** | `ISidebarRegistry` / `SidebarTabDescriptor` | Custom sidebars (signatures, bookmarks, AI search, metadata trees). |
| **4. Contextual Inspector** | `IInspectorRegistry` / `InspectorSectionDescriptor` | Custom property sections targeting specific canvas element types. |
| **5. Workspace Pages** | `INavigationRegistry` / `NavigationItemDescriptor` | New workspace pages and navigation sections. |
| **6. Canvas Elements** | `ICanvasElementRegistry` / `CanvasElementDescriptor` | Custom element types (Barcode, LaTeX Math, Form, Stamp, Ink). |
| **7. Document Importers** | `IDocumentImporterRegistry` / `DocumentImporterDescriptor` | Born-digital PDF, DOCX, XLSX, Markdown, HTML ingestion. |
| **8. Document Exporters** | `IDocumentExporterRegistry` / `DocumentExporterDescriptor` | QuestPDF static PDF, PDF/A, image sequences, CSV data export. |
| **9. AI Providers** | `IAiProviderRegistry` / `AiProviderDescriptor` | Local Ollama, OpenAI, Groq, Anthropic, custom LLM endpoints. |
| **10. OCR Engines** | `IOcrEngineRegistry` / `OcrEngineDescriptor` | Tesseract, Apple Vision OCR, Windows OCR, cloud vision engines. |
| **11. Data Connectors** | `IDataConnectorRegistry` / `DataConnectorDescriptor` | Excel, CSV, JSON REST APIs, SQL database ingestion. |
| **12. Status & Dialogs** | `IStatusBarRegistry`, `IDialogRegistry` | Contextual status indicators, modals, and command palette actions. |

#### 3. Standard Tool Plugin Pattern Example
When implementing a new tool, inherit from `ToolPluginBase` and register it inside a bundle:

```csharp
public class CustomWatermarkToolPlugin : ToolPluginBase
{
    public override string Id => "frypdf.tool.watermark";
    public override string Name => "Watermark PDF";

    protected override PdfToolDescriptor CreateDescriptor() => new()
    {
        Id = Id,
        Name = Name,
        Description = "Apply custom text and image stamps with opacity control.",
        Category = "OptimizeAndSecurity",
        IconKind = "Watermark",
        IconColorHex = "#0284C7",
        BackgroundAccentHex = "#F0F9FF",
        SupportsMultiFile = true,
        AcceptedFileExtensions = ".pdf",
        CreateViewModel = sp => ActivatorUtilities.CreateInstance<WatermarkToolViewModel>(sp)
    };
}
```

#### 4. Reversible Effects Rule
Any plugin subscribing to events or registering UI elements must use `ctx.RegisterEffect`:
```csharp
public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
{
    var subscription = WeakReferenceMessenger.Default.Register<DocumentChangedMessage>(this, HandleDocumentChanged);
    ctx.RegisterEffect(() => WeakReferenceMessenger.Default.Unregister<DocumentChangedMessage>(this));
    return Task.CompletedTask;
}
```

#### 5. Declarative Settings Schema
Plugins with configurable settings declare typed schemas via `IFryPlugin.SettingsSchema`. The UI automatically generates an M3 Expressive configurator in `PluginsDialog.axaml`:
```csharp
public IReadOnlyDictionary<string, PluginSettingDefinition>? SettingsSchema => new Dictionary<string, PluginSettingDefinition>
{
    ["ApiKey"] = new() { Key = "ApiKey", Label = "API Key", Type = PluginSettingType.Secret, IsRequired = true },
    ["Model"] = new() { Key = "Model", Label = "Model", Type = PluginSettingType.Select, Options = ["gpt-4o", "claude-3-5-sonnet"] }
};
```

---

### H. High-Performance, Zero-Lag Responsiveness & Resource Lifecycle (STRICT)

**CRITICAL PERFORMANCE MANDATE**:
FryPDF is designed to feel instantaneous, smooth, and lightweight. **UI lag, dropped frames, stuttering animations, and memory leaks are completely unacceptable.**
Always think in terms of performance optimization when designing and writing code:

#### 1. Zero UI Thread Blocking
- **Never Run Heavy Work on the UI Thread**: PDF parsing, deconstruction, Skia rasterization, QuestPDF compilation, OCR recognition, AI completions, JSON serialization, and disk/network I/O MUST run on background threads via `Task.Run` or asynchronous pipelines.
- **Never Block Asynchronously**: Never call `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` on the Avalonia UI thread.
- **Cancellation Token Propagation**: Always accept and respect `CancellationToken` in long-running jobs to allow instant aborts when the user switches pages or cancels operations.

#### 2. Instant Navigation & View Caching (0ms Latency)
- **Plugin View Caching**: In view models managing navigation (e.g. [`HomeViewModel.cs`](src/PdfEditorApp/ViewModels/HomeViewModel.cs)), always cache dynamically instantiated plugin views in an instance dictionary (`_dynamicViewCache`). Never destroy and re-instantiate heavy visual trees on every tab switch:
  ```csharp
  if (!_dynamicViewCache.TryGetValue(sectionName, out var cachedView))
  {
      cachedView = desc.ViewFactory(serviceProvider);
      _dynamicViewCache[sectionName] = cachedView;
  }
  DynamicPageView = cachedView;
  ```
- **Pre-Mounted Built-In Trees**: For primary screens (Dashboard, Reader Landing, PDF Tools Studios), pre-mount compiled XAML templates in the parent view (`HomeView.axaml`) so navigation occurs in 0ms with zero layout recalculation overhead.

#### 3. UI Virtualization & Recycling
- **Virtualizing Panels Only**: Always use virtualized layout containers (`ItemsRepeater`, `VirtualizingStackPanel` with `ScrollUnit="Pixel"`) for document page thumbnails, tool cards, audit logs, and tabular data rows.
- **Never use unvirtualized `StackPanel`** inside a `ScrollViewer` for collections with more than 10 items.
- **Element Recycling**: Ensure templates in `ItemsRepeater` reuse container elements rather than continually allocating visual tree nodes during scrolling.

#### 4. Gesture & Continuous Input Smoothing
- **Proportional Clamped Zooming**: For trackpad pinch-to-zoom and gestures, calculate zoom multiplicatively with bounds clamping to prevent exponential explosion:
  ```csharp
  double newZoom = Math.Clamp(Math.Round(currentZoom * (1.0 + delta), 3), 0.1, 5.0);
  ```
- **Reactive Debounce & Throttling**:
  - Debounce search queries and text filters (150–250ms) before filtering in-memory collections.
  - Throttle slider scrubbing events during interactive drag; do not trigger expensive Skia canvas repaints on every micro-pixel increment.
  - Throttle canvas drag and snap-line calculation loops.

#### 5. SkiaSharp Rendering & Frame Budgets
- **Frame Budgets**: Maintain $\le 16\text{ms}$ (60 FPS) and $\le 8\text{ms}$ (120 FPS) frame render times.
- **RenderTransform Over Layout Mutation**: Animate transformations using `RenderTransform` (GPU-accelerated compositing). Never animate `Margin`, `Width`, or `Height`, as mutating layout properties triggers full synchronous measure/arrange passes across the visual tree.
- **Minimize Visual Tree Depth**: Avoid unnecessary nested `Border`, `Grid`, and `Panel` elements. Keep visual hierarchy shallow.

#### 6. Large Object Heap (LOH) & GC Hygiene
- **Never allocate $\ge 85\text{ KB}$ byte arrays in loops**: Reuse memory buffers via `ArrayPool<byte>.Shared` and `MemoryStream`.
- **Zero Base64 in Models**: Store raw `byte[]` for image and vector payloads. Compute Base64 only lazily during JSON persistence export.
- **Immediate Unmanaged Disposal**: Wrap short-lived `SKBitmap`, `SKImage`, `SKSurface`, `SKData`, and `SKCodec` instances in `using` blocks. Dispose previous bitmaps before assigning replacement preview bitmaps.
- **Weak Subscriptions**: Use `WeakReferenceMessenger` for pub/sub messaging to ensure garbage collection is never blocked by forgotten event subscriptions.

---

## 5. Continuous PDF Scenario Development Workflow

When adding support for a new or complex PDF type:

1. **Place Sample PDF Locally**: Save the test PDF in the project root or test directory. Ensure it matches `.gitignore` patterns (`*.pdf`, `sample*.pdf`) and is **never** staged or committed to Git.
2. **Run Visual Verification Test**:
   ```bash
   dotnet test --filter "FullyQualifiedName~GenerateVisualComparison_SideBySide"
   ```
3. **Inspect Output Bitmaps**: Open the generated side-by-side PNG in `VisualArtifacts/` (automatically ignored by Git) to compare original PDF ground truth against the deconstructed canvas.
4. **Tune Algorithms**: Adjust clustering, Z-indexing, or shape heuristics in `PdfDeconstructionEngine` or `PdfLayoutAnalyzer`.
5. **Enforce Regression Invariance**: Run `dotnet test` and confirm all 770+ unit tests pass.

---

## 6. Coding & Quality Standards

- **Nullable Reference Types**: Enabled across all projects (`<Nullable>enable</Nullable>`).
- **Treat Warnings As Errors**: All builds must be 100% warning-free (`TreatWarningsAsErrors=true`).
- **Plugin-First Architecture**: All new tools, elements, sidebars, and panels must implement `IFryPlugin` and register into the appropriate registry.
- **Zero-Lag Compliance**: Zero UI-thread blocking, view caching on navigation, and virtualized collections are mandatory.
- **Unit Testing**: Every new service, converter, plugin, or deconstruction heuristic must include unit tests with deterministic assertions.
- **Documentation**: Keep [`docs/PLUGIN_BASED_ARCHITECTURE.md`](docs/PLUGIN_BASED_ARCHITECTURE.md), [`docs/PDF_DECONSTRUCTION_AND_EDITING.md`](docs/PDF_DECONSTRUCTION_AND_EDITING.md), and [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) updated when making structural changes.

---

## 7. Security, Privacy & Zero PII Policy (STRICT)

**CRITICAL RULE**: Under no circumstances should sensitive data, credentials, or Personally Identifiable Information (PII) ever be written, hardcoded, or committed to this repository.

1. **Zero PII in Source Code and Tests**:
   - **Never** hardcode real personal names, private addresses, real phone numbers, real email addresses, or personal biometric/signature records in source code, unit test assertions, docstrings, or template defaults.
   - **Never** write real government identification numbers (e.g. Aadhaar numbers, SSNs, PANs, Passport numbers, Driver's License IDs).
   - In test cases, always use **structural assertions** (e.g. element count, dimension bounds, non-empty text) or **generic regular expressions** (e.g. `\b\d{4}\s\d{4}\s\d{4}\b`) rather than asserting specific personal data.

2. **No Credentials, Keys, or Secrets**:
   - **Never** commit API keys, access tokens, passwords, private certificates, or user credentials.
   - Use environment variables or configuration providers for external services where needed.

3. **Synthetic & Dummy Data Only**:
   - When building mock templates, demonstrations, or test fixtures, use only standard synthetic dummy data (e.g. `Jane Doe`, `555-0199`, `user@example.com`, `ACME Corp`, generic sample logos).

4. **Local Artifact & PDF Exclusion**:
   - All real-world test PDFs, generated side-by-side visual comparison bitmaps, and test exports must be excluded via `.gitignore` (`*.pdf`, `VisualArtifacts/`, `*_side_by_side.png`).
   - Prior to making commits or pushing to remote, always verify with `git status` and `git diff` that no untracked PDFs or sensitive data are being introduced.
