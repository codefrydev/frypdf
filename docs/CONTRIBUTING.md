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
