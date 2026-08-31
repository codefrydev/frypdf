# PDF Deconstruction & Editing Engine Guide

This document explains the architecture, low-level algorithms, and continuous improvement workflow for the **FryPDF Deconstruction & Layout Analysis Engine**.

---

## 1. Overview & Core Philosophy

When a user opens an existing PDF in FryPDF for editing, the application transforms static, flattened PDF streams (text drawing operators, raw image dictionaries, and vector path drawing commands) into a **live, fully editable, structured document model (`PdfDocumentModel`)**.

### Three Import Pathways

```
                                  [ Open PDF File ]
                                         │
                 ┌───────────────────────┴───────────────────────┐
                 ▼                                               ▼
     [ FryPDF Native Document? ]                     [ 3rd-Party External PDF ]
      (Contains Embedded JSON)                                   │
                 │                                               │
                 ▼                                               ▼
     [ 100% Lossless Model ]                     [ PdfDeconstructionEngine ]
     - Exact original elements                                   │
     - Full undo/redo history                                    ▼
     - Live charts, tables, math                ┌────────────────┴────────────────┐
                                                ▼                                 ▼
                                     [ Born-Digital / Mixed ]              [ Scanned / Image-Only ]
                                     - Multi-format Skia images            - High-res raster canvas
                                     - Layered Z-Index architecture        - Interactive overlay layer
                                     - Script-aware text clustering        - OCR ready
                                     - Modular Extractors & WCAG contrast
```

1. **Native FryPDF Roundtrip (100% Lossless)**: If the PDF was authored and exported by FryPDF, embedded metadata restores the exact vector objects, charts, math equations, tables, and form fields losslessly.
2. **Born-Digital / Mixed 3rd-Party PDF**: The deconstruction engine parses text glyphs, vector paths, and embedded raster images, reconstructing them into editable canvas elements with structured layering.
3. **Scanned / Raster-Only PDF**: When no text or vector operators exist, the engine loads the page raster image cleanly as an editable base image layer.

---

## 2. Modular Architecture & Subsystems

The PDF Deconstruction Engine is decoupled into dedicated, single-responsibility extractor subsystems coordinated by `PdfDeconstructionEngine`:

```mermaid
graph TD
    A[PdfDeconstructionEngine] --> B[PdfDeconstructionOptions & ILogger]
    A --> C[PdfImageExtractor]
    A --> D[PdfTextExtractor]
    A --> E[PdfVectorExtractor]
    A --> F[PdfFormExtractor]
    A --> G[TableGridDetector]
    
    C --> C1[Raw byte[] ImageData - Zero LOH Bloat]
    C --> C2[Unsafe byte* Skia Pointers]
    C --> C3[CmykColorConverter]
    
    D --> D1[PdfLayoutAnalyzer]
    D --> D2[ColorContrastHelper / WCAG 2.1 Luminance]
    
    E --> E1[SvgPathBuilder / Excess Vector Grouping]
    F --> F1[Strongly-typed AcroFieldBase - Zero Reflection]
```

### A. Layered Z-Index Architecture & Vector Occlusion Resolution

Elements are partitioned into deterministic Z-index layers:

| Layer Range | Elements | Classification Rules |
| :--- | :--- | :--- |
| **`ZIndex = 0..99`** | Background Cards & Watermarks | Shapes with $W \ge 120\text{ pt}$ and $H \ge 80\text{ pt}$, full-page backgrounds, and faint centered watermarks ($W \ge 65\%, H \ge 55\%$) |
| **`ZIndex = 100..499`** | Content Images & Logos | Photos, QR codes, emblems, government banners, icons, and diagrams |
| **`ZIndex = 500..599`** | Structured Tables & Grids | Tabular grid cells, borders, and invoice items |
| **`ZIndex = 600..999`** | Foreground Shapes & Dividers | Divider lines, badges, small accents, and SVG vector clusters |
| **`ZIndex = 1000..1999`** | Text Blocks & Marginalia | Headings, multi-line paragraphs, labels, and rotated vertical text |
| **`ZIndex = 2000+`** | Form Controls & Signatures | Interactive form fields, checkboxes, digital signature widgets |

---

### B. High-Performance Image Extraction & Raw Byte Storage

1. **Zero Large Object Heap (LOH) Fragmentation**:
   - `PdfImageElement` stores decoded pixel payloads directly as `byte[]? ImageData`.
   - Base64 encoding is performed lazily on demand (`Base64Data`), preventing multi-megabyte string bloat during deconstruction.
2. **Unsafe SkiaSharp Pointer Manipulation**:
   - Uses `unsafe` pointer loops (`byte*`) and `bitmap.GetPixels()` for high-throughput decoding of 24-bit RGB, 8-bit Grayscale, 32-bit CMYK, and 1-bit Monochrome pixel samples.
3. **Calibrated CMYK-to-sRGB Conversion**:
   - `CmykColorConverter` applies subtractive color model with black point compensation and gamma correction to eliminate washed-out colors.

---

### C. Reflection-Free AcroForms Extraction

`PdfFormExtractor` uses strongly typed pattern matching on PdfPig's `AcroFieldBase` hierarchy (`AcroTextField`, `AcroCheckboxField`, `AcroRadioButtonField`, `AcroComboBoxField`, `AcroListBoxField`, `AcroSignatureField`), eliminating slow reflection lookups and supporting read-only flag evaluation.

---

### D. Zero-Loss Vector Extraction & Smart SVG Grouping

When vector paths exceed `MaxVectorShapesPerPage` (default 300):
- Rather than silently dropping excess paths, `PdfVectorExtractor` and `SvgPathBuilder` group them into a single resolution-independent `PdfSvgElement`.
- Charts, diagrams, and illustrations are preserved with zero data loss while keeping canvas element counts optimal for smooth 60fps interaction.

---

### E. Dynamic WCAG 2.1 Text Contrast Protection

`ColorContrastHelper` calculates relative luminance ($L = 0.2126R_{\text{linear}} + 0.7152G_{\text{linear}} + 0.0722B_{\text{linear}}$) and contrast ratio $(L_1+0.05)/(L_2+0.05)$:
- Dynamically evaluates text color against any overlapping background shape (dark slate, navy, burgundy, yellow, or pure white).
- Automatically flips text color to high-contrast dark or light if contrast falls below threshold ($<3.0$).

---

## 3. Configurable Options (`PdfDeconstructionOptions`)

All heuristics are fully configurable via `PdfDeconstructionOptions`:

```csharp
var options = new PdfDeconstructionOptions
{
    PureScannedImageCoverageThreshold = 0.85,
    PureScannedWordCountMax = 5,
    WatermarkWidthRatio = 0.65,
    WatermarkHeightRatio = 0.55,
    WatermarkOpacity = 0.35,
    LargeContainerMinWidth = 120.0,
    LargeContainerMinHeight = 80.0,
    DividerMaxHeight = 3.5,
    DividerMinWidth = 6.0,
    MaxVectorShapesPerPage = 300,
    GroupExcessVectorsAsSvg = true,
    MinContrastRatio = 3.0,
    HighContrastDarkTextColor = "#0F172A",
    HighContrastLightTextColor = "#FFFFFF"
};

var model = PdfDeconstructionEngine.Deconstruct(pdfBytes, options, logger);
```

---

## 4. Continuous PDF Scenario Development Workflow

When adding support for a new or complex PDF type:

1. **Place Sample PDF**: Save the test PDF in the project root or test directory.
2. **Run Visual Verification Test**:
   ```bash
   dotnet test --filter "FullyQualifiedName~GenerateVisualComparison_SideBySide"
   ```
3. **Inspect Output Bitmaps**: Open `<sample_name>_side_by_side.png` to compare original PDF ground truth against the deconstructed canvas.
4. **Tune Algorithms**: Adjust clustering, Z-indexing, or shape heuristics in the modular extractors or `PdfDeconstructionOptions`.
5. **Enforce Regression Invariance**: Run `dotnet test` and confirm all 440+ unit tests pass.
