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
- **`TemplateService` (`ITemplateService`)**: Provides starter templates (Executive Resume, Annual Report, Invoice, Certificate, Academic Paper) and allows community template extensions.
- **`ProjectPersistenceService` (`IProjectPersistenceService`)**: Handles lossless JSON-based project saving (`.frypdf`) and loading.
- **`SmartPlacementService` (`ISmartPlacementService`)**: Computes viewport-aware positions, cursor drop-points, and cascading offsets for newly inserted elements.
- **`UndoRedoService`**: Command-pattern undo/redo manager supporting atomic reversible actions.
- **`DocumentAuditService` (`IDocumentAuditService`)**: Preflight diagnostics engine checking for accessibility, contrast, empty fields, low-res images, and security flaws.
- **`SignatureService` (`ISignatureService`)**: Procedural vector and cursive signature generation.
- **`RecentDocumentsService` (`IRecentDocumentsService`)**: Manages LRU recent files history.
