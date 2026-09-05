# FryPDF Agent Operating Instructions (GEMINI.md)

## 1. CRITICAL UI MANDATE: Google Material Design 3 (M3) Expressive (STRICT)

**ATTENTION AI AGENT**:
You MUST strictly adhere to **Google Material Design 3 (M3) Expressive** whenever modifying or creating any UI elements, views, sidebars, buttons, dialogs, or styles in this repository.

1. **Shape Scale Hierarchy**:
   - **Pills (`CornerRadius="{StaticResource M3ShapeCornerFull}"` or `9999`)**: All buttons (`primary-btn`, `accent-btn`, `action-pill-btn`, `m3-filled-btn`, `m3-tonal-btn`), search inputs (`TextBox.m3-search`), segmented capsules (`m3-segmented-container`), FABs (`m3-fab`), and chubby slider thumbs.
   - **28px Extra-Large (`CornerRadius="{StaticResource M3ShapeCornerExtraLarge}"`)**: All modal dialog cards (`Border.m3-dialog-card`, `AboutDialog`, `CommandPaletteDialog`, `AiAssistantDialog`) and hero banners.
   - **16px Large (`CornerRadius="{StaticResource M3ShapeCornerLarge}"`)**: Cards (`m3-card-elevated`), inputs (`TextBox.m3-outlined`), `ComboBox`, `NumericUpDown`, inspector section groups.
   - **12px Medium (`CornerRadius="{StaticResource M3ShapeCornerMedium}"`)**: Context menus, tooltips, list items.
   - **8px Small (`CornerRadius="{StaticResource M3ShapeCornerSmall}"`)**: Badges, thumbnail cards, chips (`m3-chip`, `m3-filter-chip`).
   - **NEVER** use small arbitrary corner radii (like `2` or `4`) on interactive elements.

2. **Colors & Dark Mode**:
   - **NEVER** hardcode hex colors in templates or hover triggers.
   - Always reference `{DynamicResource ...}` keys: `M3PrimaryBrush`, `M3SecondaryContainerBrush`, `M3SurfaceBrush`, `M3SurfaceContainer...`, `M3OutlineBrush`, or legacy aliases (`WinBgBrush`, `WinAccentBrush`, etc.).

3. **Chubby Tactile Sliders**:
   - Always maintain chubby tactile sliders with 8-10px tracks and 20-22px thumbs with hover transitions.

4. **CommunityToolkit.Mvvm Mandate**:
   - Maximize use of `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`, `[AsyncRelayCommand]`).
   - Use `[NotifyPropertyChangedFor]` and `[NotifyCanExecuteChangedFor]` to cleanly cascade property and command updates.
   - Use `WeakReferenceMessenger.Default` for decoupled pub/sub messaging across ViewModels and services.
   - Use `ObservableValidator` with DataAnnotations for dialogs and user forms.

---

## 2. CRITICAL ARCHITECTURE MANDATE: Modular Plugin Pattern ("Everything is a Plugin") (STRICT)

**ATTENTION AI AGENT**:
FryPDF uses a modular microkernel plugin architecture inspired by DeepSeek Harness and Cordis.
You MUST adhere to the **Plugin Pattern** whenever adding, refactoring, or extending application capabilities:

1. **No Monolithic Switches or Hardcoded Wiring**:
   - **NEVER** hardcode switch-case ladders, god view models, or monolithic enum registries when adding features.
   - Every tool, canvas element, ribbon action, sidebar panel, inspector section, workspace page, dialog, AI provider, OCR engine, and file connector MUST be an isolated plugin (`IFryPlugin` or `ToolPluginBase`) mounted through an `IFryPluginBundle`.

2. **The 12 Dynamic Registry Pillars**:
   - Always register capabilities via the corresponding registry:
     - Tools $\to$ `IPdfToolRegistry` / `PdfToolDescriptor`
     - Ribbon Actions & Tabs $\to$ `IRibbonRegistry` / `RibbonContribution` / `RibbonTabDescriptor`
     - Sidebars $\to$ `ISidebarRegistry` / `SidebarTabDescriptor`
     - Inspector Sections $\to$ `IInspectorRegistry` / `InspectorSectionDescriptor`
     - Workspace Navigation $\to$ `INavigationRegistry` / `NavigationItemDescriptor`
     - Canvas Elements $\to$ `ICanvasElementRegistry` / `CanvasElementDescriptor`
     - Importers / Exporters $\to$ `IDocumentImporterRegistry` / `IDocumentExporterRegistry`
     - AI Providers / OCR $\to$ `IAiProviderRegistry` / `IOcrEngineRegistry`
     - Status Bar / Dialogs $\to$ `IStatusBarRegistry` / `IDialogRegistry`

3. **Reversible Effects & Zero Dangling Handlers**:
   - Every side effect, event listener, or resource subscription registered during `ApplyAsync` MUST be registered through `PluginScope` (`ctx.RegisterEffect(...)`) to ensure clean LIFO rollback and zero memory leaks on plugin unload or reload.

4. **Declarative Settings Schema**:
   - User-configurable options must be declared via `IFryPlugin.SettingsSchema` using `PluginSettingDefinition` so that M3 Expressive configuration interfaces are auto-generated without manual UI boilerplate.

---

## 3. CRITICAL PERFORMANCE MANDATE: Zero-Lag, 60+ FPS & Memory Safety (STRICT)

**ATTENTION AI AGENT**:
FryPDF is an interactive real-time document studio. **LAG, FRAME DROPS, AND UI THREAD STALLS ARE UNACCEPTABLE**. You MUST design and write code thinking in terms of performance optimization:

1. **Zero UI Thread Blocking**:
   - **NEVER** run CPU-bound parsing, PDF deconstruction, Skia rasterization, QuestPDF document compilation, OCR, AI inference, or file/network I/O on the Avalonia UI thread.
   - **NEVER** call `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` on asynchronous operations.
   - Always offload heavy tasks to the thread pool (`Task.Run`) and use `[RelayCommand]` with asynchronous `Task` signatures and `CancellationToken`.

2. **Instant Navigation & View Caching (0ms Transitions)**:
   - For plugin-driven views, always cache instantiated views (e.g. `_dynamicViewCache` pattern in `HomeViewModel`) instead of destroying and re-instantiating heavy visual trees during navigation.
   - Use pre-mounted XAML views for high-frequency screens (Home dashboard, tools studios, reader landing) to ensure instantaneous tab switching without layout latency.

3. **UI Virtualization & Recycling**:
   - Always use virtualized layout containers (`ItemsRepeater`, `VirtualizingStackPanel` with `ScrollUnit="Pixel"`) for document page thumbnails, tool cards, audit logs, and tabular data rows. Never use unvirtualized `StackPanel` for dynamic or unbound collections.

4. **Input Throttling & Debouncing**:
   - Smooth all high-frequency continuous user inputs:
     - Trackpad pinch-to-zoom: Use proportional multiplicative deltas with clamping (`Math.Clamp(old * (1 + delta), 0.1, 5.0)`) to eliminate exponential explosion.
     - Search boxes & filters: Debounce text changes (150–250ms) before filtering collections.
     - Sliders: Throttle redraw updates during drag events; do not trigger full document re-renders on every micro-tick.

5. **Memory Management, LOH Avoidance & Skia Disposal**:
   - **Large Object Heap (LOH)**: Never allocate byte arrays $\ge 85\text{ KB}$ repeatedly in tight loops. Use `ArrayPool<byte>.Shared` and streams. Never store Base64 strings in memory for images; store raw `byte[]` and compute Base64 lazily for JSON only.
   - **Unmanaged Graphics Handles**: `SKBitmap`, `SKImage`, `SKSurface`, `SKData`, and Avalonia `WriteableBitmap` wrap native pointers. Always dispose previous bitmap instances immediately when replacing them.
   - **Subscription Hygiene**: Use `WeakReferenceMessenger` for pub/sub messaging. Detach all events on view unloading to prevent GC roots from retaining ViewModels.

6. **Frame Budgeting & Render Transforms**:
   - Keep render updates within 16ms (60 FPS) and 8ms (120 FPS).
   - Use `RenderTransform` (GPU-accelerated) rather than mutating layout properties (`Margin`, `Width`, `Height`) during animations or gesture scaling.

---

## 4. Reference Files & Documentation

- **Operating Guide**: [`.agents/AGENTS.md`](.agents/AGENTS.md)
- **Plugin Architecture Manual**: [`docs/PLUGIN_BASED_ARCHITECTURE.md`](docs/PLUGIN_BASED_ARCHITECTURE.md)
- **M3 Expressive Guidelines**: [`docs/MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md`](docs/MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md)
- **M3 Tokens**: [`src/PdfEditorApp/Styles/Material3ExpressiveTokens.axaml`](src/PdfEditorApp/Styles/Material3ExpressiveTokens.axaml)
- **M3 Styles**: [`src/PdfEditorApp/Styles/Material3ExpressiveStyles.axaml`](src/PdfEditorApp/Styles/Material3ExpressiveStyles.axaml)
