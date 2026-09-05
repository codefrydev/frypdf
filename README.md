# FryPDF — Modern Desktop PDF Studio

<div align="center">

<img src="src/PdfEditorApp/Assets/app-logo.png" alt="FryPDF App Logo" width="160" height="160" />

### **Privacy-First, Professional Desktop PDF Creator, Editor & Document Studio**
*Engineered with .NET 10, Avalonia UI, QuestPDF, SkiaSharp & Modular Microkernel Plugins.*

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia UI](https://img.shields.io/badge/Avalonia-12.1-7029E6?style=flat-square&logo=avalonia&logoColor=white)](https://avaloniaui.net/)
[![QuestPDF](https://img.shields.io/badge/QuestPDF-2026.8-FF4500?style=flat-square)](https://www.questpdf.com/)
[![Platforms](https://img.shields.io/badge/Platforms-macOS%20%7C%20Windows%20%7C%20Linux-4B5563?style=flat-square)](https://github.com/CodeFryDev/FryPDF)
[![Tests](https://img.shields.io/badge/Tests-788%20Passed-10B981?style=flat-square)](tests/PdfEditorApp.Tests)
[![Architecture](https://img.shields.io/badge/Architecture-Microkernel%20Plugins-0284C7?style=flat-square)](docs/PLUGIN_BASED_ARCHITECTURE.md)
[![UI System](https://img.shields.io/badge/Design-Material%203%20Expressive-EE4B32?style=flat-square)](docs/MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)

</div>

---

> ⚠️ **Active Development Stage**: FryPDF is currently in an active stage of development. While core document engines, offline tools, vector editing, and plugin systems are fully operational, new plugin capabilities and complex edge cases are actively being refined. Feedback & issue reports are warmly welcomed!

---

## ✨ Key Capabilities & Highlights

- 🧩 **Modular Microkernel Plugin Architecture ("Everything is a Plugin")**: Inspired by DeepSeek Harness and Cordis. Every tool, canvas element, ribbon action, sidebar panel, inspector section, AI provider, and file connector is an isolated, dynamic plugin mounted through composable plugin bundles with reversible LIFO rollback and zero memory leaks.
- 🎨 **Google Material Design 3 (M3) Expressive UI**: Beautiful cross-platform user experience across macOS, Windows, and Linux. Features expressive pill shapes, segmented button capsules, floating action buttons (FABs), chubby tactile sliders (8–10px track, 20–22px thumbs), elevation shadows, and dynamic light/dark tonal themes.
- ⚡ **Zero-Lag, 60+ FPS Real-Time Performance**: Multi-tier view caching (`_dynamicViewCache`), virtualized UI recycling (`ItemsRepeater`, `VirtualizingStackPanel`), debounced high-frequency inputs (pinch-to-zoom deltas, search filters), and asynchronous non-blocking worker thread pipelines (`Task.Run`).
- 📕 **Dedicated PDF Reader Mode**: Pure reading mode with continuous vertical scroll, single-page fit, and two-page book spreads. Includes eye-comfort color themes (Daylight, Warm Sepia, Dark Night, High Contrast), real page thumbnail strip, bookmarks/TOC tree, live search with match jumping, review annotations (multi-color highlights, sticky notes, approval stamps), and a 1-click bridge to the visual editor.
- 📖 **Universal PDF Deconstruction Engine**: Ingest and deconstruct 3rd-party binary `.pdf` streams into live editable vector canvas elements (grouped text blocks, fonts, embedded imagery, and AcroForms) with strict layered Z-index ordering and multi-format SkiaSharp decoding (DCT, JPEG2000, uncompressed CMYK/RGB, monochrome 1-bit).
- 🌐 **Script-Aware Typography & Font Fallback**: Sub-threshold token clustering and script-aware font matching (Indic scripts such as Devanagari, Tamil, Telugu; CJK; and Latin) to eliminate missing character tofu boxes (`□□□`).
- 📝 **Interactive AcroForms with Formula Engine**: Text inputs, checkboxes, radio buttons, dropdowns, combo boxes, and signature boxes with real-time formula dependency recalculation and field validation.
- 🖋️ **Vector Signature Studio**: Type-to-sign with curated cursive calligraphy styles or draw custom vector signatures with smooth bezier curve interpolation.
- 📊 **Dynamic Vector Charts & Math**: Built-in sub-pixel LaTeX mathematical typesetting and 13+ dynamic vector chart varieties (Horizontal Bar, Column, Line, Smooth Curve, Area, Donut, Pie, Radar, Scatter, Bubble, Candlestick, Waterfall, Polar).
- 🤖 **Offline AI Studio Assistant**: Integrated with `Microsoft.Extensions.AI` and `OllamaSharp` for 100% offline local LLM inference (Llama, Mistral, Gemma) or cloud endpoints (OpenAI, Groq) to generate and edit document layouts, tables, and callouts from natural language prompts.
- 🔍 **Local OCR PDF Engine**: Convert scanned documents and image-only PDFs into searchable text layers locally using Tesseract OCR and native platform vision APIs without cloud uploads.
- 🔢 **Legal Bates Numbering & FOIA Redaction**: Automated sequential page numbering with customizable prefixes, suffixes, digit padding, and 6-anchor positioning. Permanent pattern-based redaction with irreversible pixel destruction.
- 🛠️ **32 Dedicated Full-Page PDF Tools**: Complete offline toolchain across 6 suites (Organize & Page, Optimize & Security, Convert from PDF, Convert to PDF, Edit & Forms, AI & Automation) with zero popup dialogs.
- 💾 **Dual-Format Persistence**: Native `.frypdf` JSON-based project files & standard compiled binary `.pdf` documents via QuestPDF.

---

## 📁 Repository Structure

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
│       ├── Assets/                 # Embedded fonts (Inter, Roboto, Noto Sans) and SVG icons
│       ├── Converters/             # Avalonia XAML data-binding and transform converters
│       ├── Plugins/                # Desktop plugin implementations and 16 modular bundles
│       │   ├── Bundles/            # Standard bundles (Tools, Pages, Sidebars, AI, CanvasElements, etc.)
│       │   └── Loader/             # Collectible AssemblyLoadContext and .fryplugin package extractor
│       ├── Services/               # Dynamic registries (Tools, Ribbon, Sidebars, Inspector, Navigation, AI)
│       ├── Styles/                 # Material Design 3 Expressive tokens & component themes
│       ├── ViewModels/             # Reactive MVVM ViewModels (CommunityToolkit.Mvvm source generators)
│       └── Views/                  # Avalonia XAML Windows, Canvas controls, and M3 dialogs
├── tests/
│   └── PdfEditorApp.Tests/         # Comprehensive test suite (788 unit & integration tests)
├── packaging/
│   ├── macos/                      # macOS bundle templates, AppIcon.icns, & DMG creator
│   └── windows/                    # Inno Setup installer & Windows MSIX packaging with full store assets
├── docs/                           # Architecture, M3 Expressive guidelines, and plugin manuals
│   ├── PLUGIN_BASED_ARCHITECTURE.md
│   ├── MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md
│   ├── ARCHITECTURE.md
│   ├── FEATURES.md
│   ├── CONTRIBUTING.md
│   └── THIRD_PARTY_LICENSES.md
├── index.html                      # Official FryPDF web portal & interactive tool catalog
├── run                             # Quick CLI runner script (./run)
└── watch                           # Quick CLI hot-reload script (./watch)
```

---

## 🚀 Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (v10.0+)

### Build Solution
```bash
dotnet build
```

### Run Full Test Suite (788 Tests)
```bash
dotnet test
```

### Run Application Locally
```bash
# Using root helper script
./run

# Or using dotnet CLI directly
dotnet run --project src/PdfEditorApp
```

### Run with Hot Reload
```bash
./watch
```

---

## 📦 Packaging & Installers

- **macOS**: Standalone `FryPDF.app` bundle and `.dmg` disk image (Universal Apple Silicon & Intel) with high-resolution `AppIcon.icns`.
  > **Note**: If macOS Gatekeeper displays an unverified developer notice after downloading from GitHub Releases, run this command in Terminal:
  > ```bash
  > xattr -dr com.apple.quarantine "/Applications/FryPDF.app"
  > ```
- **Windows**: Official **Microsoft Store** verified app (`ms-windows-store://pdp/?productid=9P5GW2Q81B33`), **Inno Setup** executable (`FryPDF-Setup.exe`), and modern **MSIX** package with full tile assets.
- **Linux**: Portable `.tar.gz` archive, AppImage, or direct .NET 10 compilation.

---

## 📚 Architectural Guides & Documentation

- [Plugin Architecture Manual](docs/PLUGIN_BASED_ARCHITECTURE.md) — Comprehensive guide to the microkernel plugin system, 12 dynamic registries, pipelines, and settings schema.
- [Material Design 3 Expressive Guidelines](docs/MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md) — Google M3 Expressive tokens, shape scale hierarchy, and styling rules.
- [Architecture & Technical Design](docs/ARCHITECTURE.md) — MVVM architecture, headless core engine, and Avalonia data binding.
- [Feature Catalog & Capabilities](docs/FEATURES.md) — In-depth breakdown of all 32 tools, vector engines, and document tools.
- [Contributing Guide](docs/CONTRIBUTING.md) — Developer setup, code conventions, and pull request workflow.
- [Third-Party & Font Licenses](docs/THIRD_PARTY_LICENSES.md) — Open-source dependency attributions and typography licenses.

---

## 📄 License & Credits

FryPDF Desktop Studio is open-source software licensed under the [MIT License](LICENSE).  
Copyright © 2026 **Code Fry Dev**. All rights reserved.

Developed with ❤️ using Avalonia UI, QuestPDF, and SkiaSharp.
