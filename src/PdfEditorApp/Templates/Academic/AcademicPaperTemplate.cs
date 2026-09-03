using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Academic;

public class AcademicPaperTemplate : ITemplateDefinition
{
    public string Id => "academic";
    public string Name => "Academic Research Paper";
    public string Description => "Full 2-page peer-reviewed research paper with mathematical equations, benchmark data table, author bios, and citations";
    public string Category => "Academic";
    public string IconKind => "BookOpenPageVariantOutline";
    public string AccentColorHex => "#D97706";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Research_Paper_Vector_Graphics_2026.pdf",
            Author = "Dr. John Doe, Jane Doe, and Alex Doe",
            Subject = "High-Performance Cross-Platform Desktop Vector Rendering"
        };

        // =========================================================================
        // PAGE 1: Title, Authors, Abstract, Introduction & Mathematical Theory
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "IEEE TRANSACTIONS ON SOFTWARE ENGINEERING • ISSN 0098-5589 • DOI: 10.1109/TSE.2026.892014",
            FooterRight = "Page 1 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Header Metadata Strip
                new PdfTextElement
                {
                    X = 55,
                    Y = 32,
                    Width = 690,
                    Height = 18,
                    Text = "IEEE TRANSACTIONS ON SOFTWARE ENGINEERING, VOL. 52, NO. 4, AUGUST 2026",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 52,
                    Width = 690,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#CBD5E1"
                },

                // Main Paper Title
                new PdfTextElement
                {
                    X = 55,
                    Y = 58,
                    Width = 690,
                    Height = 48,
                    Text = "High-Performance Cross-Platform Vector Graphics Architecture in Modern .NET Environments",
                    FontSize = 17,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center
                },

                // Authors & Institutional Affiliation
                new PdfTextElement
                {
                    X = 55,
                    Y = 108,
                    Width = 690,
                    Height = 34,
                    Text = "John Doe, Ph.D.¹,   Jane Doe, M.Sc.²,   Alex Doe, Ph.D.¹\n¹Institute for Advanced Computing, CodeFryDev Institute   •   ²Department of Computer Science, CodeFryDev University",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Center
                },

                // Abstract & Keywords Callout Card
                new PdfShapeElement
                {
                    X = 75,
                    Y = 145,
                    Width = 650,
                    Height = 104,
                    CornerRadius = 4,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 150,
                    Width = 620,
                    Height = 68,
                    Text = "Abstract— We present a unified, hardware-accelerated rendering pipeline for interactive desktop publishing software running on Avalonia and SkiaSharp within .NET 9. By decoupling scene graph state from rendering primitives and implementing a zero-allocation spatial indexing tree, we achieve a 4.2× reduction in garbage collection stalls and consistent 60 FPS multi-touch canvas manipulation across Windows, macOS, and Linux.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Justify
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 222,
                    Width = 620,
                    Height = 22,
                    Text = "Index Terms— Vector graphics, SkiaSharp, Avalonia UI, cross-platform publishing, GPU shaders, garbage collection.",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },

                // Column Split Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 254,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },

                // ==========================================
                // COLUMN 1 (Left: X = 55, Width = 330)
                // ==========================================
                new PdfTextElement
                {
                    X = 55,
                    Y = 262,
                    Width = 330,
                    Height = 22,
                    Text = "I. INTRODUCTION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 286,
                    Width = 330,
                    Height = 135,
                    Text = "Desktop publishing workflows impose rigorous demands on color space management, sub-pixel glyph rasterization, and interactive transform matrices [1]. Historically, applications relied on platform-specific APIs such as Direct2D on Windows and CoreGraphics on macOS, leading to divergent rendering artifacts and doubled maintenance overhead. Our framework leverages Skia-backed hardware surfaces unified under a reactive MVVM state model.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 425,
                    Width = 330,
                    Height = 22,
                    Text = "II. MATHEMATICAL FORMULATION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 449,
                    Width = 330,
                    Height = 70,
                    Text = "Let Ω ⊂ ℝ² define the bounded document viewport. For each vector primitive P_i with boundary curve ∂P_i, the continuous antialiased rasterization intensity I(p) at coordinate p = (x, y) is governed by the signed distance field filter:",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Formal Equation Box 1
                new PdfShapeElement
                {
                    X = 55,
                    Y = 522,
                    Width = 330,
                    Height = 36,
                    CornerRadius = 3,
                    FillColorHex = "#F1F5F9",
                    StrokeColorHex = "#CBD5E1",
                    StrokeThickness = 0.5
                },
                new PdfTextElement
                {
                    X = 65,
                    Y = 528,
                    Width = 310,
                    Height = 24,
                    Text = "I(p) = clamp( 1/2 - dist(p, ∂P_i) / w_pixel,  0,  1 )      (1)",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 562,
                    Width = 330,
                    Height = 120,
                    Text = "Equation (1) enables constant-time sub-pixel coverage evaluation per fragment without requiring expensive multi-sample supersampling (MSAA) buffers [2]. The spatial complexity of the scene bounding hierarchy scales logarithmically with depth: O(log N) for hit testing across N = 10⁴ interactive vector shapes.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 686,
                    Width = 330,
                    Height = 22,
                    Text = "III. SCENE GRAPH & ZERO-COPY BUFFERING",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 710,
                    Width = 330,
                    Height = 145,
                    Text = "Managed runtime garbage collection cycles introduce non-deterministic micro-stutters during 60 Hz drag-and-drop operations. To overcome this, our scene graph uses contiguous struct spans (ReadOnlySpan<VectorVertex>) allocated through unmanaged NativeMemory pools. Viewport updates trigger differential buffer invalidations rather than total scene reconstructs.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 858,
                    Width = 330,
                    Height = 22,
                    Text = "IV. ADAPTIVE SHADER RASTERIZATION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 882,
                    Width = 330,
                    Height = 120,
                    Text = "On high-DPI displays (Apple Retina and 4K Windows monitors), pixel fill rate dominates frame time. We introduce an adaptive tessellation shader that subdivides cubic Bézier curves based on local curvature κ(t) = |x'y'' - y'x''| / (x'^2 + y'^2)^{3/2}, minimizing emitted vertex counts.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // ==========================================
                // COLUMN 2 (Right: X = 415, Width = 330)
                // ==========================================
                new PdfTextElement
                {
                    X = 415,
                    Y = 262,
                    Width = 330,
                    Height = 22,
                    Text = "V. GPU PIPELINE & BACKEND TOPOLOGY",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 286,
                    Width = 330,
                    Height = 135,
                    Text = "The backend abstracts DirectX 12 on Windows, Metal on macOS, and Vulkan on Linux through Skia's GRContext handle. Document layout matrices are pre-computed in SIMD registers (Vector256<float>), which batches up to 1,024 vector paths into a single GPU draw call invocation.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 425,
                    Width = 330,
                    Height = 22,
                    Text = "VI. EMPIRICAL BENCHMARK EVALUATION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 449,
                    Width = 330,
                    Height = 55,
                    Text = "We evaluated rendering throughput across 1,000 synthetic multi-layer vector documents containing complex paths, radial gradients, and typography on Apple M3 Max (Metal) and Intel Core i9 (DirectX 12).",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Benchmark Data Table
                new PdfTableElement
                {
                    X = 415,
                    Y = 508,
                    Width = 330,
                    Height = 165,
                    Headers = new List<string> { "Framework", "FPS", "RAM", "P99 GC", "Draw Lat." },
                    Rows = new List<List<string>>
                    {
                        new() { "WPF Direct2D", "34.2", "420 MB", "18.4 ms", "4.2 ms" },
                        new() { "Web Electron", "28.5", "680 MB", "24.1 ms", "6.8 ms" },
                        new() { "Qt QPainter", "52.1", "210 MB", "0.0 ms", "1.9 ms" },
                        new() { "Proposed .NET 9", "59.8", "148 MB", "1.2 ms", "0.8 ms" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#CBD5E1"
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 686,
                    Width = 330,
                    Height = 22,
                    Text = "VII. MEMORY PRESSURE & CACHE COHERENCE",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 710,
                    Width = 330,
                    Height = 145,
                    Text = "As documented in Table I, the zero-allocation pipeline avoids Gen2 GC collections during rapid zooming and pan gestures. Cache miss rates for the spatial R-tree dropped from 14.2% in pointer-heavy structures to 2.1% in flat contiguous memory blocks.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 858,
                    Width = 330,
                    Height = 22,
                    Text = "VIII. ACKNOWLEDGMENTS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 882,
                    Width = 330,
                    Height = 120,
                    Text = "This research was supported by National Science Foundation Grant CCF-2490182, the MIT Center for Advanced Computing, and the Open Source Cross-Platform Software Foundation. Hardware testbeds were generously provided by Microsoft Corporation and Apple Computer.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#475569",
                    Alignment = TextAlignmentMode.Justify
                }
            }
        };

        // =========================================================================
        // PAGE 2: Discussion, Multi-Device Scalability, Author Bios & References
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "IEEE TRANSACTIONS ON SOFTWARE ENGINEERING • VOL. 52, NO. 4",
            FooterRight = "Page 2 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Header Metadata Strip
                new PdfTextElement
                {
                    X = 55,
                    Y = 32,
                    Width = 690,
                    Height = 18,
                    Text = "VANCE ET AL.: HIGH-PERFORMANCE VECTOR GRAPHICS ARCHITECTURE IN .NET",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 52,
                    Width = 690,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#CBD5E1"
                },

                // ==========================================
                // COLUMN 1 (Left: X = 55, Width = 330)
                // ==========================================
                new PdfTextElement
                {
                    X = 55,
                    Y = 62,
                    Width = 330,
                    Height = 22,
                    Text = "IX. EXTENSIBILITY & VECTOR PLUGINS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 86,
                    Width = 330,
                    Height = 130,
                    Text = "Our architecture exposes a modular interface IPdfElementRenderer<T> enabling custom procedural shaders, SVG path decoders, and hardware-accelerated QR matrix synthesis. Because element state is decoupled from rendering logic, extensions can execute on worker threads using task parallelism without acquiring UI thread locks [3].",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 222,
                    Width = 330,
                    Height = 22,
                    Text = "X. MULTI-DEVICE RESPONSIVE LATENCY",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 246,
                    Width = 330,
                    Height = 65,
                    Text = "Table II benchmarks touch drag latency across diverse hardware form factors (Apple Silicon MacBook, Intel Surface Laptop, and ARM64 Linux tablets).",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Multi-device Latency Table
                new PdfTableElement
                {
                    X = 55,
                    Y = 315,
                    Width = 330,
                    Height = 150,
                    Headers = new List<string> { "Platform", "OS Backend", "Touch Lat.", "FPS" },
                    Rows = new List<List<string>>
                    {
                        new() { "MacBook Pro M3", "Metal 3", "6.2 ms", "120 FPS" },
                        new() { "Surface Laptop 5", "DirectX 12", "7.8 ms", "60 FPS" },
                        new() { "Dell XPS 15", "Vulkan", "8.1 ms", "60 FPS" },
                        new() { "Raspberry Pi 5", "GLES 3.1", "14.5 ms", "54 FPS" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#CBD5E1"
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 478,
                    Width = 330,
                    Height = 22,
                    Text = "XI. CONCLUSION & FUTURE DIRECTIONS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 502,
                    Width = 330,
                    Height = 135,
                    Text = "We demonstrated that unified cross-platform desktop publishing in managed .NET runtimes achieves near-native performance through hardware-accelerated Skia surfaces and memory-conscious data structures. Future work will investigate real-time collaborative document synchronization via Conflict-Free Replicated Data Types (CRDTs) and neural font upscaling on WebGPU.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // ==========================================
                // COLUMN 2 (Right: X = 415, Width = 330)
                // ==========================================

                // References Section
                new PdfTextElement
                {
                    X = 415,
                    Y = 62,
                    Width = 330,
                    Height = 22,
                    Text = "REFERENCES",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 415,
                    Y = 85,
                    Width = 330,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#CBD5E1"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 90,
                    Width = 330,
                    Height = 220,
                    Text = "[1] J. D. Foley, A. van Dam, et al., Computer Graphics: Principles and Practice, 3rd ed., Addison-Wesley, 2021.\n[2] M. Kilgard and J. Bolz, \"GPU-accelerated path rendering,\" ACM Trans. Graph., vol. 31, no. 6, pp. 172:1–172:10, 2022.\n[3] Google Skia Engine Architecture Whitepaper, Google LLC, 2025.\n[4] D. E. Knuth, The TeXbook, Addison-Wesley, 1984.\n[5] ISO 32000-2:2020, Document Management — Portable Document Format — Part 2: PDF 2.0.\n[6] E. Gamma et al., Design Patterns: Elements of Reusable Object-Oriented Software, Addison-Wesley, 1994.\n[7] Avalonia UI Architecture & Reactive Binding Framework Specification, 2026.\n[8] Microsoft .NET 9 Performance and Garbage Collector Improvements Guide, Microsoft Press, 2025.",
                    FontSize = 8,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#475569",
                    Alignment = TextAlignmentMode.Justify
                },

                // Author Biographies Header
                new PdfTextElement
                {
                    X = 415,
                    Y = 320,
                    Width = 330,
                    Height = 22,
                    Text = "AUTHOR BIOGRAPHIES",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 415,
                    Y = 340,
                    Width = 330,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#CBD5E1"
                },

                // Bio 1: John Doe
                new PdfShapeElement
                {
                    X = 415,
                    Y = 350,
                    Width = 36,
                    Height = 36,
                    CornerRadius = 18,
                    FillColorHex = "#0F6CBD",
                    Label = "JD",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 460,
                    Y = 348,
                    Width = 285,
                    Height = 85,
                    Text = "John Doe received the Ph.D. in Computer Science in 2020. He is currently a Principal Research Scientist specializing in GPU compilers and vector rendering systems.",
                    FontSize = 8,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.3,
                    TextColorHex = "#334155"
                },

                // Bio 2: Jane Doe
                new PdfShapeElement
                {
                    X = 415,
                    Y = 445,
                    Width = 36,
                    Height = 36,
                    CornerRadius = 18,
                    FillColorHex = "#0284C7",
                    Label = "JD",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 460,
                    Y = 443,
                    Width = 285,
                    Height = 85,
                    Text = "Jane Doe received the M.Sc. in Electrical Engineering in 2022. Her research focuses on zero-allocation memory pools and low-latency user interfaces.",
                    FontSize = 8,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.3,
                    TextColorHex = "#334155"
                },

                // Bio 3: Alex Doe
                new PdfShapeElement
                {
                    X = 415,
                    Y = 540,
                    Width = 36,
                    Height = 36,
                    CornerRadius = 18,
                    FillColorHex = "#7E22CE",
                    Label = "AD",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 460,
                    Y = 538,
                    Width = 285,
                    Height = 85,
                    Text = "Alex Doe is an Associate Professor at CodeFryDev Institute. His research interests include human-computer interaction, digital document security, and typography rendering algorithms.",
                    FontSize = 8,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.3,
                    TextColorHex = "#334155"
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);

        return doc;
    }
}
