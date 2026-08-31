# Contributing to FryPDF

Thank you for your interest in contributing to FryPDF!

---

## 1. Prerequisites
- **.NET 10 SDK** (or later)
- IDE: JetBrains Rider, Visual Studio Code (with C# Dev Kit), or Visual Studio 2026

---

## 2. Solution Structure
- `src/PdfEditorApp/`: The main Avalonia desktop application.
- `tests/PdfEditorApp.Tests/`: Unit and integration tests (xUnit).
- `docs/`: Technical and feature documentation.
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

## 4. Coding Conventions
- Use C# 13+ / .NET 10 features (nullable reference types, pattern matching, file-scoped namespaces).
- Follow MVVM design patterns with `CommunityToolkit.Mvvm`.
- Ensure all business logic in `Services` has corresponding unit tests in `tests/PdfEditorApp.Tests/`.
- Ensure no warnings or build errors are introduced (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).
- **Security & Privacy (Strict)**: Never commit credentials, secrets, or Personally Identifiable Information (PII) — including real government IDs, personal names, phone numbers, or addresses. Always use synthetic/dummy test data.

---

## 5. Developing & Testing PDF Deconstruction Algorithms
When adding support for new PDF document types (invoices, ID cards, tax forms, multi-column articles, complex scripts):
1. Review the detailed architecture and continuous improvement guide: [PDF Deconstruction & Editing Guide](PDF_DECONSTRUCTION_AND_EDITING.md).
2. Run the visual side-by-side verification test (`GenerateVisualComparison_SideBySide_SavesArtifacts`) to compare ground-truth PDF rendering vs deconstructed canvas elements.
3. Validate that all 450+ unit tests pass without regressions (`dotnet test`).
