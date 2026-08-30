using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class AcademicPaperTemplate : ITemplateDefinition
{
    public string Id => "academic";
    public string Name => "Academic Paper";
    public string Description => "2-column formatted research paper layout";
    public string Category => "Academic";
    public string IconKind => "BookOpenPageVariantOutline";
    public string AccentColorHex => "#D97706";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Research_Paper_2026.pdf",
            Author = "Dr. Elena Vance, Ph.D.",
            Subject = "High-Performance Cross-Platform Desktop Vector Rendering"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "JOURNAL OF MODERN COMPUTING | ISSN 2410-982X",
            FooterRight = "Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement
                {
                    X = 60,
                    Y = 50,
                    Width = 680,
                    Height = 60,
                    Text = "High-Performance Cross-Platform Vector Graphics Architecture in Modern .NET Environments",
                    FontSize = 20,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#111827",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 120,
                    Width = 680,
                    Height = 24,
                    Text = "Elena Vance, Marcus Thorne, and Sarah Jenkins — Institute for Advanced Systems",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#4B5563",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 60,
                    Y = 150,
                    Width = 680,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#9CA3AF"
                },
                new PdfTextElement
                {
                    X = 80,
                    Y = 165,
                    Width = 640,
                    Height = 70,
                    Text = "ABSTRACT: We present an optimized rendering pipeline for interactive desktop publishing software running on Avalonia and SkiaSharp. Benchmark results indicate up to a 4.2x reduction in garbage collection pressure and 60 FPS smooth multi-touch canvas manipulation.",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    TextColorHex = "#1F2937",
                    Alignment = TextAlignmentMode.Justify
                },
                new PdfDividerElement
                {
                    X = 60,
                    Y = 245,
                    Width = 680,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#9CA3AF"
                },
                // Column 1
                new PdfTextElement
                {
                    X = 60,
                    Y = 260,
                    Width = 325,
                    Height = 24,
                    Text = "1. Introduction",
                    FontSize = 13,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 290,
                    Width = 325,
                    Height = 200,
                    Text = "Desktop publishing workflows require strict color space management, sub-pixel text rasterization, and rapid spatial index queries. Traditional frameworks suffer from cross-platform discrepancies between Windows Direct2D and macOS CoreGraphics. Our modular architecture abstracts the rendering surface directly through high-performance GPU shaders.",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.4,
                    TextColorHex = "#374151",
                    Alignment = TextAlignmentMode.Justify
                },
                // Column 2
                new PdfTextElement
                {
                    X = 415,
                    Y = 260,
                    Width = 325,
                    Height = 24,
                    Text = "2. Empirical Methodology",
                    FontSize = 13,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 290,
                    Width = 325,
                    Height = 200,
                    Text = "We evaluated document compilation across 1,000 synthetic test files ranging from 10 to 500 pages. Metrics recorded included memory allocation per frame, layout recalculation latency, and export fidelity against the ISO 32000-2 PDF standard.",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.4,
                    TextColorHex = "#374151",
                    Alignment = TextAlignmentMode.Justify
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
