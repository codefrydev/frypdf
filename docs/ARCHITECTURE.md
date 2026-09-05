# FryPDF Technical Architecture

FryPDF is a modern, high-performance, cross-platform PDF creation and editing studio built with **Avalonia UI** (v12.x), **C# / .NET 10**, and **QuestPDF**.

---

## 1. Directory Structure

```
PDFCreator/
├── src/
│   └── PdfEditorApp/             # Main Avalonia Application
│       ├── Assets/               # Embedded icons, fonts, sample assets
│       ├── Converters/           # XAML value converters
│       ├── Models/               # Domain entities, element models, security/audit
│       ├── Services/             # Business logic, export engines, audit, undo/redo
│       ├── Styles/               # Avalonia Fluent theme styles & control themes
│       ├── Templates/            # Built-in document templates (Annual Report, Resume, etc.)
│       ├── ViewModels/           # MVVM CommunityToolkit ViewModels
│       ├── Views/                # Avalonia XAML Views, UserControls, and Dialogs
│       ├── App.axaml / .cs       # Application root & resource dictionary
│       ├── Program.cs            # Entry point & Avalonia initialization
│       ├── ViewLocator.cs        # View-ViewModel resolver
│       └── app.manifest          # Windows DPI-awareness and OS compatibility
├── tests/
│   └── PdfEditorApp.Tests/       # Unit & Integration Tests (xUnit)
├── packaging/
│   ├── macos/                    # macOS bundle templates, icons, codesign
│   └── windows/                  # Inno Setup installer (.iss) & MSIX packaging scripts
├── docs/                         # Developer and user documentation
├── .github/workflows/            # GitHub Actions CI/CD release pipelines
├── PdfEditorApp.slnx             # Solution configuration
└── README.md
```

---

## 2. Core Architectural Layers

### MVVM Presentation Layer
- **`MainViewModel`**: Orchestrates top-level state: active view (Home vs Editor), current page, ribbon commands, tool modes, modal dialogs, and persistence commands.
- **`InspectorViewModel`**: Provides two-way property binding for the contextual sidebar (geometry, typography, fill colors, stroke, borders, rotation, opacity, form settings).
- **`HomeViewModel`**: Powers the Start screen with recent documents, quick action cards, and the interactive Template Gallery.
- **`ElementViewModelBase` & Subclasses**: Rich observable view models for every canvas element type (Text, Heading, Image, Shape, Table, Chart, FormField, StickyNote, Redaction, Stamp, BatesNumber, Measurement, QR Code, Ink).

### Domain & Document Model
- **`PdfDocumentModel`**: Represents the root document (metadata, security settings, page collection, outlines, comments).
- **`PdfPageModel`**: Holds page dimensions, margins, header/footer definitions, and collection of `PdfElementModelBase`.
- **`PdfElementModelBase`**: Pure POCO domain models for serialization and export.

### Services Layer
- **`PdfExportService` (`IPdfExportService`)**: Compiles the document hierarchy and elements into high-fidelity PDF binaries using QuestPDF.
- **`PdfImportService` (`IPdfImportService`)**: Imports existing PDFs, delegating to `PdfDeconstructionEngine` for born-digital parsing and embedded JSON detection.
- **`TemplateService` (`ITemplateService`)**: Provides starter templates (Executive Resume, Annual Report, Invoice, Certificate, Academic Paper) and allows community template extensions.
- **`ProjectPersistenceService` (`IProjectPersistenceService`)**: Handles lossless JSON-based project saving (`.frypdf`) and loading.
- **`SmartPlacementService` (`ISmartPlacementService`)**: Computes viewport-aware positions, cursor drop-points, and cascading offsets for newly inserted elements.
- **`UndoRedoService`**: Command-pattern undo/redo manager supporting atomic reversible actions.
- **`DocumentAuditService` (`IDocumentAuditService`)**: Preflight diagnostics engine checking for accessibility, contrast, empty fields, low-res images, and security flaws.
- **`SignatureService` (`ISignatureService`)**: Procedural vector and cursive signature generation.
- **`RecentDocumentsService` (`IRecentDocumentsService`)**: Manages LRU recent files history.

---

## 3. PDF Deconstruction & Layout Analysis Subsystem

Located in `src/PdfEditorApp.Core/`:
- **`PdfDeconstructionEngine`**: Deconstructs 3rd-party PDFs into live editable elements with deterministic layered Z-indexing (`Z=0..99` background cards, `Z=100..499` images, `Z=500..599` tables, `Z=600..999` shapes, `Z=1000..1999` text, `Z=2000+` form fields) and Skia multi-format image decoding.
- **`PdfLayoutAnalyzer`**: Intelligent layout clustering, script-aware gap thresholding, and paragraph indent recognition.
- **`UnicodeScriptDetector`**: Codepoint classifier covering 30+ world scripts with font fallback resolution.

---

## 4. Data Studio & LiveCharts2 Ingestion Subsystem

Located in `src/PdfEditorApp.Core/Data/` and `src/PdfEditorApp/ViewModels/DataStudio/`:
- **`DataMatrix`**: Unified in-memory tabular data structure with column type inference (Text, Number, Currency, Percentage, Date), row/column mutations, and matrix accessors.
- **`IDataSourceService` / `DataSourceService`**: Multi-source data ingestion pipeline:
  - **Excel (`.xlsx`)**: High-performance OpenXml workbook parser supporting multi-sheet inspection and shared strings.
  - **CSV / TSV**: RFC-4180 compliant parser with auto-delimiter detection (comma, tab, semicolon, pipe) and quoted multiline cell handling.
  - **REST API / JSON**: Async HTTP client with custom headers, Bearer tokens, and flexible JsonPath array navigation.
  - **Clipboard & Manual Entry**: Instant tabular clipboard paste and live spreadsheet grid editor.
- **`IDataBindingService` / `DataBindingService`**: Maps tabular datasets to LiveCharts2 series and Table elements, plus bidirectional Table $\leftrightarrow$ Chart conversion with full Undo/Redo tracking.
- **`DataStudioViewModel` & `DataStudioDialog`**: Modal Studio providing tabbed connector workflows, interactive column mapping, and live Skia-rendered chart previews.

---

## 5. UI Architecture & Material Design 3 (M3) Expressive System

FryPDF strictly implements **Google Material Design 3 (M3) Expressive** design principles across all screens, controls, and dialogs.

- **Tokens (`Material3ExpressiveTokens.axaml`)**:
  - Full M3 Expressive shape scale (`M3ShapeCornerNone` (0px) to `M3ShapeCornerFull` (9999px), with `M3ShapeCornerExtraLarge` (28px) for dialogs and `M3ShapeCornerLarge` (16px) for cards/inputs).
  - Semantic tonal color palettes for Light and Dark themes (`Primary`, `SecondaryContainer`, `Surface`, `SurfaceContainer`, etc.).
  - Multi-level elevation shadows (`M3ElevationLevel0` through `M3ElevationLevel5`).
  - Tactile chubby slider tokens with interactive hover scaling.
- **Control Styles (`Material3ExpressiveStyles.axaml`)**:
  - M3 button hierarchy (`m3-filled-btn`, `m3-tonal-btn`, `m3-elevated-btn`, `m3-outlined-btn`, `m3-text-btn`, `m3-fab`, `m3-icon-btn`).
  - Segmented capsules (`m3-segmented-container` + `m3-segment-btn`), filter/assist chips (`m3-chip`, `m3-filter-chip`), and cards (`m3-card-elevated`, `m3-dialog-card`).
  - Pill search bars (`TextBox.m3-search`) and outlined inputs (`TextBox.m3-outlined`).
- **Mandate**: All future UI additions and modifications must strictly conform to the [Material Design 3 Expressive Guidelines](MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md) and Section 4.F of [`.agents/AGENTS.md`](../.agents/AGENTS.md).

---

## 6. AI Studio & Document Generation Subsystem

Located in `src/PdfEditorApp/Services/AI/` and `src/PdfEditorApp/ViewModels/`:
- **`IPdfStudioAgentService` / `PdfStudioAgentService`**: Autonomous document studio agent translating natural language prompts into live, native, and editable FryPDF canvas elements (headers, invoices, tables, warning cards, certificates, badges, grids).
- **`IAiService` / `AiService`**: Unified multi-provider AI gateway powered by `Microsoft.Extensions.AI`:
  - **Local Offline Inference (`OllamaLocal`)**: Connects to locally hosted Ollama instances via `OllamaSharp` (Llama 3.2, Mistral, Gemma, Phi-3) with 100% privacy, zero cloud data transfer, and zero network sockets.
  - **Cloud Providers (`OpenAiCloud`, `CustomOpenAiCompatible`)**: Direct integration with OpenAI, Groq, OpenRouter, LM Studio, or vLLM endpoints via standard OpenAI-compatible REST protocols.
- **`AiAssistantViewModel` & `AiAssistantDialog`**: Interactive M3 Expressive dialog supporting prompt execution, in-place element modification, suggested prompts, live model switching, activity logging, and atomic canvas undo/redo.
- **`AiDocumentService`**: Extractive NLP summarization, keyword extraction, and document translation engine.


---

## 7. Plugin-Based Architecture ("Everything is a Plugin")

FryPDF incorporates an extensible, plugin-first architecture inspired by **DeepSeek Harness (`deepseek-harness`)** and the **Cordis** framework.
- **Dynamic Capability Context (`IFryPluginContext`)**: Swappable services and extension points without hardcoded compile-time dependencies.
- **Declarative Dependency Resolution**: Directed Acyclic Graph (DAG) topological loader.
- **Reversible Effects (`PluginScope`)**: LIFO registration disposal eliminating memory leaks.
- **Multi-Mode Dispatch Pipelines**: `Waterfall` (around-middleware), `Bail` (first handled result), `Parallel`, `Serial`, and `Emit`.
- **Profiles & Bundles**: Target-specific application assemblies (`desktop`, `headless-cli`, `sdk`).

For the full architectural specification, interface designs, and pipeline mechanics, see the [Plugin-Based Architecture Manual](PLUGIN_BASED_ARCHITECTURE.md).

For detailed algorithms, testing procedures, and the continuous improvement workflow, see the [PDF Deconstruction & Editing Guide](PDF_DECONSTRUCTION_AND_EDITING.md).


