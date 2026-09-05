# Contributing to FryPDF

Thank you for your interest in contributing to FryPDF!

---

## 1. Prerequisites
- **.NET 10 SDK** (or later)
- IDE: JetBrains Rider, Visual Studio Code (with C# Dev Kit), or Visual Studio 2026

---

## 2. Solution Structure
- `src/PdfEditorApp.Core/`: Pure headless core engine (PDF deconstruction, layout analysis, data models, microkernel plugin context).
- `src/PdfEditorApp/`: Main Avalonia desktop application (Material Design 3 Expressive UI, dynamic registries, modular bundles).
- `tests/PdfEditorApp.Tests/`: Unit and integration tests (xUnit + Skia verification).
- `docs/`: Technical manuals and architecture specifications.
- `packaging/`: Platform-specific installers (macOS .app/.dmg, Windows Inno Setup & MSIX).

---

## 3. Building and Running

### Build Solution
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run Application Locally
```bash
dotnet run --project src/PdfEditorApp/PdfEditorApp.csproj
```

---

## 4. Coding Conventions & Mandates

### A. General Standards
- Use C# 13+ / .NET 10 features (nullable reference types, pattern matching, file-scoped namespaces).
- Follow MVVM design patterns with `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`).
- Ensure no warnings or build errors are introduced (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).
- Add comprehensive unit tests in `tests/PdfEditorApp.Tests/` for all new features, tools, and converters.

### B. Modular Plugin Pattern ("Everything is a Plugin")
- Never write monolithic `switch-case` ladders, god view models, or hardcoded UI instantiations.
- Implement new capabilities (tools, canvas elements, ribbon actions, sidebars, inspector sections, workspace pages, AI providers, and converters) as modular plugins (`IFryPlugin` or `ToolPluginBase`) mounted into an `IFryPluginBundle`.
- Register components through the 12 dynamic capability registries (`IPdfToolRegistry`, `IRibbonRegistry`, `ISidebarRegistry`, `IInspectorRegistry`, etc.).
- Track all side effects via `ctx.RegisterEffect` to guarantee leak-free reversible unmounting.
- For complete specifications, see [Plugin-Based Architecture Manual](PLUGIN_BASED_ARCHITECTURE.md).

### C. Zero-Lag Performance & Responsiveness Mandate
- **Zero UI Thread Blocking**: PDF parsing, Skia rasterization, QuestPDF export, OCR, and AI inference must run asynchronously on background threads via `Task.Run`. Never call `.Result` or `.Wait()` on the UI thread.
- **Instant Navigation & View Caching**: In navigation controllers, cache contributed dynamic views (`_dynamicViewCache` pattern) to ensure 0ms instantaneous page transitions.
- **Virtualization Only**: Always use `ItemsRepeater` or `VirtualizingStackPanel` with `ScrollUnit="Pixel"` for variable-length collections (thumbnails, tool cards, audit logs, data rows).
- **Smooth Continuous Inputs**: Multiplicatively clamp pinch-to-zoom math, debounce search query text changes (150–250ms), and throttle slider repaints during drag.
- **Memory & LOH Hygiene**: Never allocate byte arrays $\ge 85\text{ KB}$ in loops. Immediately dispose unmanaged Skia graphics objects (`SKBitmap`, `SKImage`, `SKSurface`). Use `WeakReferenceMessenger` to prevent retain cycles.

### D. Google Material Design 3 (M3) Expressive UI Mandate
- All UI elements must strictly adhere to Google Material Design 3 Expressive guidelines.
- Use centralized shape tokens (`M3ShapeCornerFull` for pills/buttons/search, `M3ShapeCornerExtraLarge` for dialogs, `M3ShapeCornerLarge` for cards/inputs).
- Never hardcode hex colors; reference dynamic M3 theme brushes (`M3PrimaryBrush`, `M3SurfaceBrush`, etc.).
- See [Material Design 3 Expressive Guidelines](MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md).

### E. Security & Privacy (Strict Zero PII Policy)
- Never commit credentials, API keys, passwords, or Personally Identifiable Information (PII) — including real government IDs, personal names, phone numbers, or addresses. Always use synthetic dummy data.

---

## 5. Developing & Testing PDF Deconstruction Algorithms
When adding support for new PDF document types (invoices, ID cards, tax forms, multi-column articles, complex scripts):
1. Review the detailed architecture and continuous improvement guide: [PDF Deconstruction & Editing Guide](PDF_DECONSTRUCTION_AND_EDITING.md).
2. Run the visual side-by-side verification test (`GenerateVisualComparison_SideBySide_SavesArtifacts`) to compare ground-truth PDF rendering vs deconstructed canvas elements.
3. Validate that all 770+ unit tests pass without regressions (`dotnet test`).
