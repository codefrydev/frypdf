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
├── sample1.pdf                     # Standard test fixture: Government e-Aadhaar ID Card (Images + Hindi)
├── Class_6_Math_Chapter_1...pdf    # Standard test fixture: Textbook Chapter (Multi-paragraph + Watermark)
├── AGENTS.md                       # This operating guide
└── README.md                       # Repository overview
```

---

## 4. Architectural Rules & Subsystem Guidelines

### A. PDF Deconstruction Engine ([`PdfDeconstructionEngine.cs`](file:///Users/codefrydev/Desktop/SourceCode/PDFCreator/src/PdfEditorApp.Core/Deconstruction/PdfDeconstructionEngine.cs))

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

### B. Layout Analysis & Typography Engine ([`PdfLayoutAnalyzer.cs`](file:///Users/codefrydev/Desktop/SourceCode/PDFCreator/src/PdfEditorApp.Core/Analysis/PdfLayoutAnalyzer.cs))

1. **Script-Aware Token Joining**:
   - For Indic scripts (Devanagari, Tamil, Telugu, etc.) and CJK, evaluate horizontal bounding-box gaps. Sub-threshold gaps must be joined without artificial spaces to preserve natural words.

2. **Paragraph Indent & Clustering**:
   - In `ShouldClusterLines`, check vertical line pitch ($\le 0.95 \times \text{FontSize}$).
   - If the next line has a positive left indent ($>8\text{ pt}$), break to start a new paragraph.

3. **Line Height Multipliers**:
   - `LineHeight` is a proportional multiplier ($1.25\times$ to $1.4\times\text{FontSize}$), not fixed points.

4. **Script-Aware Font Fallback ([`UnicodeScriptDetector.cs`](file:///Users/codefrydev/Desktop/SourceCode/PDFCreator/src/PdfEditorApp.Core/Analysis/UnicodeScriptDetector.cs))**:
   - Match detected Unicode script ranges to system/embedded font families (e.g. `Noto Sans Devanagari`, `Kohinoor Devanagari`, `Noto Sans SC`, `PingFang SC`) to eliminate tofu boxes (`□□□`).

---

### C. MVVM Canvas & ViewModels

1. Every canvas element inherits from `ElementViewModelBase` and implements `LoadFromModel(PdfElementModelBase)` and `ToModel()`.
2. Property changes must notify via `SetProperty(ref field, value)` to support live canvas rendering, property sidebar binding, and atomic undo/redo records.
3. Keep business logic and coordinate conversions inside `Core` and `Services`, leaving ViewModels focused on presentation state.

---

## 5. Continuous PDF Scenario Development Workflow

When adding support for a new or complex PDF type:

1. **Place Sample PDF**: Save the test PDF in the project root or test directory.
2. **Run Visual Verification Test**:
   ```bash
   dotnet test --filter "FullyQualifiedName~GenerateVisualComparison_SideBySide"
   ```
3. **Inspect Output Bitmaps**: Open `<sample_name>_side_by_side.png` to compare original PDF ground truth against the deconstructed canvas.
4. **Tune Algorithms**: Adjust clustering, Z-indexing, or shape heuristics in `PdfDeconstructionEngine` or `PdfLayoutAnalyzer`.
5. **Enforce Regression Invariance**: Run `dotnet test` and confirm all 370+ unit tests pass.

---

## 6. Coding & Quality Standards

- **Nullable Reference Types**: Enabled across all projects (`<Nullable>enable</Nullable>`).
- **Treat Warnings As Errors**: All builds must be 100% warning-free.
- **Unit Testing**: Every new service or deconstruction heuristic must include unit tests with deterministic assertions.
- **Documentation**: Keep [`docs/PDF_DECONSTRUCTION_AND_EDITING.md`](file:///Users/codefrydev/Desktop/SourceCode/PDFCreator/docs/PDF_DECONSTRUCTION_AND_EDITING.md) and [`docs/ARCHITECTURE.md`](file:///Users/codefrydev/Desktop/SourceCode/PDFCreator/docs/ARCHITECTURE.md) updated when making structural changes.
