# Third-Party Software & Font Licensing

FryPDF relies on high-quality open-source software libraries, document engines, graphics frameworks, and typographic font collections. This document details each dependency, its version, license, copyright holders, and purpose.

---

## Summary Matrix

| Library / Tool / Font | Version | License | Category | Primary Maintainer |
|---|---|---|---|---|
| **Avalonia UI** | 12.1.1 | MIT | UI & Graphics Frameworks | AvaloniaUI OÜ & Community |
| **QuestPDF** | 2026.8.0 | Community / MIT | PDF & Document Engines | Marcin Ziąbek |
| **PdfPig & Skia Rendering** | 0.1.16.1 | Apache 2.0 | PDF & Document Engines | Eli White (UglyToad) |
| **PdfSharpCore** | 1.3.67 | MIT | PDF & Document Engines | empira Software & Stefan Lange |
| **SkiaSharp** | 3.116.1 | MIT | UI & Graphics Frameworks | Microsoft & Mono Project |
| **LiveChartsCore & SkiaSharpView** | 2.0.5 | MIT | UI & Graphics Frameworks | Alberto Rodriguez (beto-rodriguez) |
| **Material.Icons.Avalonia** | 3.0.2 | MIT | UI & Graphics Frameworks | SKFox5330 & Pictogrammers |
| **Tabula Table Extractor** | 1.0.1 | MIT | Office & Data Formats | Manuel Aristarán & Contributors |
| **DocumentFormat.OpenXml** | 3.5.1 | MIT | Office & Data Formats | Microsoft Corporation |
| **QRCoder** | 1.8.0 | MIT | Office & Data Formats | Raffael Herrmann (codebude) |
| **CommunityToolkit.Mvvm** | 8.4.2 | MIT | Architecture & Runtime | Microsoft & .NET Foundation |
| **Microsoft.Extensions.DependencyInjection** | 10.0.11 | MIT | Architecture & Runtime | Microsoft & .NET Runtime Team |
| **Microsoft.Extensions.Logging.Abstractions** | 10.0.1 | MIT | Architecture & Runtime | Microsoft & .NET Foundation |
| **.NET 10 Runtime & Base Libraries** | 10.0 | MIT | Architecture & Runtime | Microsoft & .NET Foundation |
| **SIL Open Font License (OFL 1.1) Typefaces** | 1.1 | SIL OFL 1.1 | Typography & Typefaces | SIL International & Google Fonts |
| **Roboto & Roboto Mono Font Family** | 2.138 | Apache 2.0 | Typography & Typefaces | Google LLC & Christian Robertson |
| **Ubuntu Font Family** | 0.83 | Ubuntu Font Licence 1.0 | Typography & Typefaces | Canonical Ltd & Dalton Maag |

---

## Detailed Licensing & Attributions

### 1. Avalonia UI (MIT License)
- **Maintainer**: AvaloniaUI OÜ & .NET Foundation Community
- **Website**: https://avaloniaui.net
- **Purpose**: Core cross-platform modern XAML UI application framework providing high-performance GPU-accelerated graphics, styling, and native window management on macOS, Windows, and Linux.

### 2. QuestPDF (Community / MIT License)
- **Maintainer**: Marcin Ziąbek & QuestPDF Contributors
- **Website**: https://www.questpdf.com
- **Purpose**: Vector PDF generation engine utilizing fluent layout APIs, smart pagination, table structures, vector shapes, and sub-millimeter typographical accuracy.

### 3. UglyToad.PdfPig & Skia Rendering (Apache License 2.0)
- **Maintainer**: Eli White (UglyToad) & Open Source Contributors
- **Website**: https://github.com/UglyToad/PdfPig
- **Purpose**: Low-level PDF parsing engine. Extracts structured text glyphs, font matrices, bounding boxes, vector curves, and rasterizes pages to Skia surfaces.

### 4. PdfSharpCore (MIT License)
- **Maintainer**: Stefan Lange, empira Software & Community
- **Website**: https://github.com/stefan-lange/pdfsharpcore
- **Purpose**: Low-level PDF manipulation handling direct binary page extraction, PDF merging, page splitting, rotation, AES-128/256 security encryption, and Bates stamping.

### 5. SkiaSharp (MIT License)
- **Maintainer**: Microsoft Corporation & Mono Project
- **Website**: https://github.com/mono/SkiaSharp
- **Purpose**: Cross-platform 2D graphics API based on Google's Skia Graphics Library powering canvas zooming, Bézier curves, and sub-pixel text rendering.

### 6. LiveChartsCore & LiveChartsCore.SkiaSharpView (MIT License)
- **Maintainer**: Alberto Rodriguez (beto-rodriguez) & LiveCharts Contributors
- **Website**: https://github.com/beto-rodriguez/LiveCharts2
- **Purpose**: Modern charting engine for .NET. Renders high-performance bar charts, line graphs, and pie charts with SkiaSharp integration directly into PDF documents.

### 7. Material.Icons.Avalonia (MIT License)
- **Maintainer**: SKFox5330 & Pictogrammers Community
- **Website**: https://github.com/SKFox5330/Material.Icons.Avalonia
- **Purpose**: Vector iconography library supplying Material Design icons across toolbars, ribbon bars, inspectors, and tool cards.

### 8. Tabula (MIT License)
- **Maintainer**: Manuel Aristarán & Tabula Contributors
- **Website**: https://github.com/tabulapdf/tabula-java
- **Purpose**: Spatial table extraction engine that detects table boundaries, cell grids, columns, and data coordinates from unstructured PDF documents.

### 9. DocumentFormat.OpenXml (MIT License)
- **Maintainer**: Microsoft Corporation & .NET Foundation
- **Website**: https://github.com/dotnet/Open-XML-SDK
- **Purpose**: High-speed OpenXML engine for reading, parsing, and exporting Microsoft Word (.docx), Excel (.xlsx), and PowerPoint (.pptx) documents.

### 10. QRCoder (MIT License)
- **Maintainer**: Raffael Herrmann (codebude) & Contributors
- **Website**: https://github.com/codebude/QRCoder
- **Purpose**: QR code generator powering dynamic vector URL, Wi-Fi, vCard, SMS, Email, and plain text QR code elements in FryPDF.

### 11. Microsoft.Extensions.DependencyInjection (MIT License)
- **Maintainer**: Microsoft Corporation & .NET Runtime Team
- **Website**: https://github.com/dotnet/runtime
- **Purpose**: Inversion of Control (IoC) dependency injection container powering modular service registration and decoupled testing.

### 12. Microsoft.Extensions.Logging.Abstractions (MIT License)
- **Maintainer**: Microsoft Corporation & .NET Foundation
- **Website**: https://github.com/dotnet/runtime
- **Purpose**: Structured logging abstractions and diagnostic interfaces powering error reporting and pipeline telemetry across core PDF engines.

### 13. CommunityToolkit.Mvvm (MIT License)
- **Maintainer**: Microsoft Corporation & Community Toolkit Team
- **Website**: https://github.com/CommunityToolkit/dotnet
- **Purpose**: Microsoft .NET Community Toolkit MVVM framework providing source-generated ObservableObject, RelayCommand, and reactive property notifications.

### 14. .NET 10 Runtime & Base Libraries (MIT License)
- **Maintainer**: Microsoft Corporation & .NET Foundation
- **Website**: https://dotnet.microsoft.com
- **Purpose**: Cross-platform .NET 10 runtime engine providing hardware-accelerated SIMD (AVX/NEON) vector mathematics, AES-256 cryptography, Task-based asynchronous I/O, and garbage collection.

### 15. SIL Open Font License (OFL 1.1) Typeface Collection
- **Maintainer**: SIL International, Google Fonts & Type Designers
- **Website**: https://scripts.sil.org/OFL
- **Purpose**: Open-source multilingual font collection powering FryPDF typography, including Google Noto Sans (Devanagari, Tamil, Telugu, Bengali, Gujarati, Kannada, Malayalam, Arabic, Urdu, Hebrew, Thai, Lao, Khmer, Myanmar, Sinhala, CJK), Inter, Poppins, Montserrat, Raleway, Lato, Fira Code, Caveat, Orbitron, and Oswald.

### 16. Roboto & Roboto Mono Font Family (Apache License 2.0)
- **Maintainer**: Google LLC & Christian Robertson
- **Website**: https://github.com/googlefonts/roboto
- **Purpose**: Google's sans-serif and monospaced typefaces. Serves as the primary document reader font, technical tables, code spans, and corporate report templates.

### 17. Ubuntu Font Family (Ubuntu Font Licence 1.0)
- **Maintainer**: Canonical Ltd & Dalton Maag
- **Website**: https://design.ubuntu.com/font/
- **Purpose**: Humanist sans-serif typeface designed by Dalton Maag for Canonical Ltd. Included in the Creative & Design typography package.
