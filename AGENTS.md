# FryPDF Agent Operating Guide (AGENTS.md)

Welcome to the **FryPDF** repository. This guide provides AI agents and human contributors with the architectural guidelines, core workflows, conventions, and debugging procedures needed to work effectively in this codebase.

---

## 1. Project Overview & Tech Stack

**FryPDF** is a high-performance, cross-platform PDF creation, editing, and document analysis studio.

- **Framework**: .NET 10 / C# 13+
- **UI Toolkit**: Avalonia UI (v12.x) with Fluent styling and Desktop platform targets (macOS, Windows, Linux)
- **MVVM**: `CommunityToolkit.Mvvm` (Observable Objects, Relay Commands, Source Generators)
- **PDF Generation & Export**: `QuestPDF`
- **PDF Parsing & Deconstruction**: `UglyToad.PdfPig` & `PdfPig.DocumentLayoutAnalysis`
- **Raster & Graphics Engine**: `SkiaSharp`
- **Testing**: `xUnit` + Skia visual verification testing suite

---

## 2. Essential Commands

### Build Solution
```bash
dotnet build
```

### Run Full Test Suite (370+ Unit & Integration Tests)
```bash
dotnet test
```

### Run Specific / Filtered Tests
```bash
dotnet test --filter "FullyQualifiedName~PdfImportAndViewerTests"
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
│   │   └── Models/                 # Domain element POCOs (Text, Image, Shape, Table, Chart, Math, Form)
│   └── PdfEditorApp/               # Main Avalonia desktop application
│       ├── Assets/                 # Embedded fonts (Noto Sans, Roboto, Inter, etc.) and SVG icons
│       ├── Services/               # Export, import, undo/redo, smart placement, audit, persistence
│       ├── ViewModels/             # MVVM ViewModels (MainViewModel, PageViewModel, ElementViewModels)
│       └── Views/                  # Avalonia XAML Views, canvas controls, and ribbon panels
├── tests/
│   └── PdfEditorApp.Tests/         # Comprehensive xUnit test suite & visual verification tests
├── docs/                           # Architecture, contributing, and PDF deconstruction manuals
├── AGENTS.md                       # This operating guide
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

### C. MVVM Canvas & ViewModels

1. Every canvas element inherits from `ElementViewModelBase` and implements `LoadFromModel(PdfElementModelBase)` and `ToModel()`.
2. Property changes must notify via `SetProperty(ref field, value)` to support live canvas rendering, property sidebar binding, and atomic undo/redo records.
3. Keep business logic and coordinate conversions inside `Core` and `Services`, leaving ViewModels focused on presentation state.

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

## 5. Continuous PDF Scenario Development Workflow

When adding support for a new or complex PDF type:

1. **Place Sample PDF Locally**: Save the test PDF in the project root or test directory. Ensure it matches `.gitignore` patterns (`*.pdf`, `sample*.pdf`) and is **never** staged or committed to Git.
2. **Run Visual Verification Test**:
   ```bash
   dotnet test --filter "FullyQualifiedName~GenerateVisualComparison_SideBySide"
   ```
3. **Inspect Output Bitmaps**: Open the generated side-by-side PNG in `VisualArtifacts/` (automatically ignored by Git) to compare original PDF ground truth against the deconstructed canvas.
4. **Tune Algorithms**: Adjust clustering, Z-indexing, or shape heuristics in `PdfDeconstructionEngine` or `PdfLayoutAnalyzer`.
5. **Enforce Regression Invariance**: Run `dotnet test` and confirm all 450+ unit tests pass.

---

## 6. Coding & Quality Standards

- **Nullable Reference Types**: Enabled across all projects (`<Nullable>enable</Nullable>`).
- **Treat Warnings As Errors**: All builds must be 100% warning-free.
- **Unit Testing**: Every new service, converter, or deconstruction heuristic must include unit tests with deterministic assertions.
- **Documentation**: Keep [`docs/PDF_DECONSTRUCTION_AND_EDITING.md`](docs/PDF_DECONSTRUCTION_AND_EDITING.md) and [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) updated when making structural changes.

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
