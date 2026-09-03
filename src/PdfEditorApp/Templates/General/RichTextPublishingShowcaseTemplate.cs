using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.General;

/// <summary>
/// Professional Publishing Specimen Template demonstrating inline rich text typography,
/// multi-span runs, scientific notations (H₂O, x²), highlighted legal clauses, custom colors,
/// and clickable hyperlinks.
/// </summary>
public class RichTextPublishingShowcaseTemplate : ITemplateDefinition
{
    public string Id => "richtextshowcase";
    public string Name => "Rich Typography & Publishing Specimen";
    public string Description => "Executive multi-span typography showcase featuring inline bold, italics, highlights, sub/superscript notations, custom colors, and links";
    public string Category => "Design & Creative";
    public string IconKind => "FormatColorText";
    public string AccentColorHex => "#0F6CBD";

    public PdfDocumentModel Create()
    {
        return GenerateDocument();
    }

    public static PdfDocumentModel GenerateDocument()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Rich_Text_Publishing_Specimen.pdf",
            Author = "FryPDF Publishing Studio",
            Subject = "Official Specimen Demonstrating Multi-Span Inline Typography and Rich Formatting",
            Keywords = "RichText, Typography, Publishing, Specimen, Markdown, Superscript, Subscript"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 595.28,
            Height = 841.89,
            BackgroundColorHex = "#F8FAFC",
            ShowHeaderFooter = true,
            HeaderLeft = "FRYPDF TYPOGRAPHY SPECIMEN",
            HeaderRight = "VOLUME IV • ISSUE 1",
            FooterLeft = "CONFIDENTIAL & PROPRIETARY",
            FooterCenter = "www.frypdf.dev",
            FooterRight = "PAGE 1 OF 1"
        };

        // ==========================================
        // 1. TOP HERO HEADER & BRANDING BANNER
        // ==========================================

        // Header Background Banner Card
        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 24,
            Width = 547.28,
            Height = 110,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 8,
            FillColorHex = "#0F172A",
            StrokeColorHex = "#1E293B",
            StrokeThickness = 1.5,
            ZIndex = 0
        });

        // Top Accent Stripe
        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 24,
            Width = 547.28,
            Height = 4,
            ShapeType = ShapeType.Rectangle,
            FillColorHex = "#0F6CBD",
            ZIndex = 1
        });

        // Category Badge
        page.Elements.Add(new PdfTextElement
        {
            X = 44,
            Y = 40,
            Width = 220,
            Height = 18,
            Text = "DESKTOP PUBLISHING ENGINE • V2.0",
            FontFamily = "Segoe UI",
            FontSize = 9.5,
            IsBold = true,
            TextColorHex = "#38BDF8",
            CharacterSpacing = 1.2,
            ZIndex = 2
        });

        // Main Title with Multi-Span Rich Text (Mixed White and Cyan)
        var heroTitle = new PdfTextElement
        {
            X = 44,
            Y = 60,
            Width = 480,
            Height = 34,
            FontFamily = "Segoe UI",
            FontSize = 20,
            TextColorHex = "#FFFFFF",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Rich Typography ", IsBold = true, TextColorHex = "#FFFFFF" },
                new() { Text = "& Inline Formatting ", IsBold = true, TextColorHex = "#38BDF8" },
                new() { Text = "Studio", IsBold = false, IsItalic = true, TextColorHex = "#94A3B8" }
            },
            ZIndex = 2
        };
        heroTitle.SynchronizePlainTextFromSpans();
        page.Elements.Add(heroTitle);

        // Subtitle Description
        var heroSub = new PdfTextElement
        {
            X = 44,
            Y = 96,
            Width = 500,
            Height = 22,
            FontFamily = "Segoe UI",
            FontSize = 10,
            TextColorHex = "#CBD5E1",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "High-fidelity " },
                new() { Text = "multi-span runs", IsBold = true, TextColorHex = "#FFFFFF" },
                new() { Text = " with granular control over weights, italics, colors, subscripts, and links." }
            },
            ZIndex = 2
        };
        heroSub.SynchronizePlainTextFromSpans();
        page.Elements.Add(heroSub);

        // ==========================================
        // 2. SECTION A: EXECUTIVE SUMMARY & CALLOUT
        // ==========================================

        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 146,
            Width = 547.28,
            Height = 90,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 146,
            Width = 4,
            Height = 90,
            ShapeType = ShapeType.Rectangle,
            FillColorHex = "#0F6CBD",
            ZIndex = 1
        });

        var execHeading = new PdfTextElement
        {
            X = 40,
            Y = 158,
            Width = 400,
            Height = 20,
            FontFamily = "Segoe UI",
            FontSize = 12,
            TextColorHex = "#0F172A",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "1. Executive Summary & ", IsBold = true },
                new() { Text = "Inline Style Highlights", IsBold = true, TextColorHex = "#0F6CBD" }
            },
            ZIndex = 2
        };
        execHeading.SynchronizePlainTextFromSpans();
        page.Elements.Add(execHeading);

        var execBody = new PdfTextElement
        {
            X = 40,
            Y = 180,
            Width = 515,
            Height = 46,
            FontFamily = "Segoe UI",
            FontSize = 10,
            LineHeight = 1.45,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "FryPDF empowers authors to craft documents with " },
                new() { Text = "extreme typographic precision", IsBold = true, TextColorHex = "#0F172A" },
                new() { Text = ". Sentences can effortlessly combine " },
                new() { Text = "bold emphasis", IsBold = true },
                new() { Text = ", " },
                new() { Text = "italic nuance", IsItalic = true },
                new() { Text = ", " },
                new() { Text = "underline anchors", IsUnderline = true, TextColorHex = "#0F6CBD" },
                new() { Text = ", and " },
                new() { Text = "strikethrough revisions", IsStrikethrough = true, TextColorHex = "#94A3B8" },
                new() { Text = " inside a single, unified text box without creating disjointed fragments." }
            },
            ZIndex = 2
        };
        execBody.SynchronizePlainTextFromSpans();
        page.Elements.Add(execBody);

        // ==========================================
        // 3. SECTION B: 2-COLUMN CARDS (TECHNICAL & LEGAL)
        // ==========================================

        // Left Card: Scientific & Mathematical Notations
        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 248,
            Width = 265,
            Height = 220,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        // Left Card Top Badge
        page.Elements.Add(new PdfTextElement
        {
            X = 38,
            Y = 260,
            Width = 240,
            Height = 18,
            FontFamily = "Segoe UI",
            FontSize = 11,
            TextColorHex = "#0F172A",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "🔬 Scientific & ", IsBold = true },
                new() { Text = "Math Scripts", IsBold = true, TextColorHex = "#0284C7" }
            },
            ZIndex = 2
        });

        // Chemical Formulas
        var chemText = new PdfTextElement
        {
            X = 38,
            Y = 286,
            Width = 238,
            Height = 50,
            FontFamily = "Segoe UI",
            FontSize = 9.5,
            LineHeight = 1.4,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "• Chemical Reactions:\n  " },
                new() { Text = "2H", IsBold = true },
                new() { Text = "2", Script = TextScriptMode.Subscript },
                new() { Text = " + O", IsBold = true },
                new() { Text = "2", Script = TextScriptMode.Subscript },
                new() { Text = " → 2H", IsBold = true },
                new() { Text = "2", Script = TextScriptMode.Subscript },
                new() { Text = "O (Combustion)\n  Photosynthesis: 6CO", TextColorHex = "#0284C7" },
                new() { Text = "2", Script = TextScriptMode.Subscript, TextColorHex = "#0284C7" },
                new() { Text = " + 6H", TextColorHex = "#0284C7" },
                new() { Text = "2", Script = TextScriptMode.Subscript, TextColorHex = "#0284C7" },
                new() { Text = "O → C", TextColorHex = "#0284C7" },
                new() { Text = "6", Script = TextScriptMode.Subscript, TextColorHex = "#0284C7" },
                new() { Text = "H", TextColorHex = "#0284C7" },
                new() { Text = "12", Script = TextScriptMode.Subscript, TextColorHex = "#0284C7" },
                new() { Text = "O", TextColorHex = "#0284C7" },
                new() { Text = "6", Script = TextScriptMode.Subscript, TextColorHex = "#0284C7" }
            },
            ZIndex = 2
        };
        chemText.SynchronizePlainTextFromSpans();
        page.Elements.Add(chemText);

        // Mathematical Physics Exponents
        var mathText = new PdfTextElement
        {
            X = 38,
            Y = 344,
            Width = 238,
            Height = 55,
            FontFamily = "Segoe UI",
            FontSize = 9.5,
            LineHeight = 1.4,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "• Physics & Algebra:\n  " },
                new() { Text = "E = mc", IsBold = true },
                new() { Text = "2", Script = TextScriptMode.Superscript, IsBold = true },
                new() { Text = " (Mass-Energy Equivalence)\n  Pythagoras: " },
                new() { Text = "a", IsItalic = true },
                new() { Text = "2", Script = TextScriptMode.Superscript },
                new() { Text = " + " },
                new() { Text = "b", IsItalic = true },
                new() { Text = "2", Script = TextScriptMode.Superscript },
                new() { Text = " = " },
                new() { Text = "c", IsItalic = true },
                new() { Text = "2", Script = TextScriptMode.Superscript },
                new() { Text = "\n  Ordinal Ranks: " },
                new() { Text = "1", IsBold = true },
                new() { Text = "st", Script = TextScriptMode.Superscript },
                new() { Text = ", 2", IsBold = true },
                new() { Text = "nd", Script = TextScriptMode.Superscript },
                new() { Text = ", 3", IsBold = true },
                new() { Text = "rd", Script = TextScriptMode.Superscript },
                new() { Text = ", and n", IsBold = true },
                new() { Text = "th", Script = TextScriptMode.Superscript },
                new() { Text = " degree." }
            },
            ZIndex = 2
        };
        mathText.SynchronizePlainTextFromSpans();
        page.Elements.Add(mathText);

        // Right Card: Legal, Financial & Revision Highlights
        page.Elements.Add(new PdfShapeElement
        {
            X = 306,
            Y = 248,
            Width = 265,
            Height = 220,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        // Right Card Top Badge
        page.Elements.Add(new PdfTextElement
        {
            X = 320,
            Y = 260,
            Width = 240,
            Height = 18,
            FontFamily = "Segoe UI",
            FontSize = 11,
            TextColorHex = "#0F172A",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "⚖️ Legal & ", IsBold = true },
                new() { Text = "Contract Revisions", IsBold = true, TextColorHex = "#EA580C" }
            },
            ZIndex = 2
        });

        // Contract Clauses with Redactions & Revisions
        var legalText = new PdfTextElement
        {
            X = 320,
            Y = 286,
            Width = 238,
            Height = 110,
            FontFamily = "Segoe UI",
            FontSize = 9.5,
            LineHeight = 1.42,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "• Clause 4.2 (Payment Terms):\n  The total consideration of " },
                new() { Text = "$850,000.00", IsStrikethrough = true, TextColorHex = "#94A3B8" },
                new() { Text = " " },
                new() { Text = "$1,250,000.00 USD", IsBold = true, TextColorHex = "#16A34A" },
                new() { Text = " shall be payable within " },
                new() { Text = "30 calendar days", IsBold = true, IsUnderline = true },
                new() { Text = " from notice delivery.\n\n• Governing Jurisdiction:\n  All arbitration shall proceed in " },
                new() { Text = "London, UK", IsBold = true, TextColorHex = "#0F6CBD" },
                new() { Text = " under LCIA rules." }
            },
            ZIndex = 2
        };
        legalText.SynchronizePlainTextFromSpans();
        page.Elements.Add(legalText);

        // Hyperlink Reference
        var linkText = new PdfTextElement
        {
            X = 320,
            Y = 412,
            Width = 238,
            Height = 35,
            FontFamily = "Segoe UI",
            FontSize = 9.5,
            LineHeight = 1.35,
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "🔗 Full Document: " },
                new() { Text = "frypdf.dev/legal/terms", IsUnderline = true, TextColorHex = "#0F6CBD", LinkUrl = "https://frypdf.dev" }
            },
            ZIndex = 2
        };
        linkText.SynchronizePlainTextFromSpans();
        page.Elements.Add(linkText);

        // ==========================================
        // 4. SECTION C: IN-PLACE MARKDOWN SYNTAX GUIDE
        // ==========================================

        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 480,
            Width = 547.28,
            Height = 175,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#F1F5F9",
            StrokeColorHex = "#CBD5E1",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        var guideHeading = new PdfTextElement
        {
            X = 40,
            Y = 494,
            Width = 450,
            Height = 20,
            FontFamily = "Segoe UI",
            FontSize = 11.5,
            TextColorHex = "#0F172A",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "⌨️ Live Canvas ", IsBold = true },
                new() { Text = "Markdown & Tag Syntax Guide", IsBold = true, TextColorHex = "#0F6CBD" }
            },
            ZIndex = 2
        };
        guideHeading.SynchronizePlainTextFromSpans();
        page.Elements.Add(guideHeading);

        var guideCol1 = new PdfTextElement
        {
            X = 40,
            Y = 520,
            Width = 245,
            Height = 120,
            FontFamily = "Consolas",
            FontSize = 9.0,
            LineHeight = 1.45,
            TextColorHex = "#0F172A",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "**Bold Text**           → ", TextColorHex = "#64748B" },
                new() { Text = "Bold Text\n", IsBold = true },
                new() { Text = "*Italic Text*          → ", TextColorHex = "#64748B" },
                new() { Text = "Italic Text\n", IsItalic = true },
                new() { Text = "<u>Underline</u>        → ", TextColorHex = "#64748B" },
                new() { Text = "Underline\n", IsUnderline = true },
                new() { Text = "~~Strikethrough~~      → ", TextColorHex = "#64748B" },
                new() { Text = "Strikethrough", IsStrikethrough = true }
            },
            ZIndex = 2
        };
        guideCol1.SynchronizePlainTextFromSpans();
        page.Elements.Add(guideCol1);

        var guideCol2 = new PdfTextElement
        {
            X = 300,
            Y = 520,
            Width = 260,
            Height = 120,
            FontFamily = "Consolas",
            FontSize = 9.0,
            LineHeight = 1.45,
            TextColorHex = "#0F172A",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Formula: H~2~O          → ", TextColorHex = "#64748B" },
                new() { Text = "H" },
                new() { Text = "2", Script = TextScriptMode.Subscript },
                new() { Text = "O\n" },
                new() { Text = "Exponent: E=mc^2^       → ", TextColorHex = "#64748B" },
                new() { Text = "E=mc" },
                new() { Text = "2\n", Script = TextScriptMode.Superscript },
                new() { Text = "<color=#DC2626>Red</color> → ", TextColorHex = "#64748B" },
                new() { Text = "Red\n", IsBold = true, TextColorHex = "#DC2626" },
                new() { Text = "[Link](https://...)     → ", TextColorHex = "#64748B" },
                new() { Text = "Link", IsUnderline = true, TextColorHex = "#0F6CBD" }
            },
            ZIndex = 2
        };
        guideCol2.SynchronizePlainTextFromSpans();
        page.Elements.Add(guideCol2);

        // ==========================================
        // 5. SECTION D: KEY METRICS ROW & FOOTER
        // ==========================================

        // Metric 1: 100% Lossless Roundtrip
        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 668,
            Width = 175,
            Height = 65,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        page.Elements.Add(new PdfTextElement
        {
            X = 34,
            Y = 678,
            Width = 155,
            Height = 45,
            FontFamily = "Segoe UI",
            FontSize = 9.0,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "100% Lossless\n", IsBold = true, FontSize = 12.0, TextColorHex = "#0F6CBD" },
                new() { Text = "JSON & PDF roundtrip fidelity" }
            },
            ZIndex = 2
        });

        // Metric 2: QuestPDF Native
        page.Elements.Add(new PdfShapeElement
        {
            X = 210,
            Y = 668,
            Width = 175,
            Height = 65,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        page.Elements.Add(new PdfTextElement
        {
            X = 220,
            Y = 678,
            Width = 155,
            Height = 45,
            FontFamily = "Segoe UI",
            FontSize = 9.0,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "QuestPDF Native\n", IsBold = true, FontSize = 12.0, TextColorHex = "#059669" },
                new() { Text = "Zero rasterization vector export" }
            },
            ZIndex = 2
        });

        // Metric 3: Deconstruction Clean
        page.Elements.Add(new PdfShapeElement
        {
            X = 396,
            Y = 668,
            Width = 175,
            Height = 65,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        page.Elements.Add(new PdfTextElement
        {
            X = 406,
            Y = 678,
            Width = 155,
            Height = 45,
            FontFamily = "Segoe UI",
            FontSize = 9.0,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "No Fragmenting\n", IsBold = true, FontSize = 12.0, TextColorHex = "#7C3AED" },
                new() { Text = "Merged baseline paragraphs" }
            },
            ZIndex = 2
        });

        // Bottom Decorative Wave Divider
        page.Elements.Add(new PdfDividerElement
        {
            X = 24,
            Y = 746,
            Width = 547.28,
            Height = 12,
            Style = DividerStyle.Wave,
            ColorHex = "#0F6CBD",
            Thickness = 1.5,
            ZIndex = 1
        });

        // Bottom Publication Note
        var pubNote = new PdfTextElement
        {
            X = 24,
            Y = 764,
            Width = 547.28,
            Height = 22,
            Alignment = TextAlignmentMode.Center,
            FontFamily = "Segoe UI",
            FontSize = 9.0,
            TextColorHex = "#64748B",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Designed with " },
                new() { Text = "FryPDF Studio", IsBold = true, TextColorHex = "#0F6CBD" },
                new() { Text = " • Cross-Platform High Performance Document Suite" }
            },
            ZIndex = 2
        };
        pubNote.SynchronizePlainTextFromSpans();
        page.Elements.Add(pubNote);

        doc.Pages.Add(page);
        return doc;
    }
}
