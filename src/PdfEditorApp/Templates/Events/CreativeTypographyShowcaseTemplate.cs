using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

/// <summary>
/// Professional Showcase Poster demonstrating Bézier curved typography, text outlines,
/// drop shadows, custom kerning/tracking, decorative wave dividers, curved connectors,
/// and Catmull-Rom smooth calligraphic ink signatures.
/// </summary>
public class CreativeTypographyShowcaseTemplate : ITemplateDefinition
{
    public string Id => "typographyshowcase";
    public string Name => "Bézier Arts & Typography Specimen";
    public string Description => "Executive creative showcase featuring Bézier wave typography, text outlines, drop shadows, decorative wave dividers, and smooth ink signatures";
    public string Category => "Design & Creative";
    public string IconKind => "FormatTextVariantOutline";
    public string AccentColorHex => "#6366F1";

    public PdfDocumentModel Create()
    {
        return GenerateDocument();
    }

    public static PdfDocumentModel GenerateDocument()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Bezier_Typography_Creative_Showcase.pdf",
            Author = "CodeFryDev Typography Institute",
            Subject = "Official Specimen & Showcase of Bézier Curve Typography & Vector Arts"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 595.28,
            Height = 841.89,
            BackgroundColorHex = "#0B0F19",
            ShowHeaderFooter = false
        };

        // ==========================================
        // 1. BACKGROUND CANVAS & AMBIENT ORGANIC ART
        // ==========================================

        // Main Obsidian Card Container
        page.Elements.Add(new PdfShapeElement
        {
            X = 0,
            Y = 0,
            Width = 595.28,
            Height = 841.89,
            ShapeType = ShapeType.Rectangle,
            FillColorHex = "#0B0F19",
            StrokeColorHex = "#1E293B",
            StrokeThickness = 2.0,
            ZIndex = 0
        });

        // Top-Right Ambient Organic Blob (Deep Indigo Accent)
        page.Elements.Add(new PdfShapeElement
        {
            X = 350,
            Y = -20,
            Width = 270,
            Height = 220,
            ShapeType = ShapeType.OrganicBlob,
            FillColorHex = "#1E1B4B",
            StrokeColorHex = "#312E81",
            StrokeThickness = 1.0,
            Opacity = 0.45,
            ZIndex = 1
        });

        // Bottom-Left Ambient Wave Ribbon (Midnight Teal Accent)
        page.Elements.Add(new PdfShapeElement
        {
            X = -40,
            Y = 720,
            Width = 340,
            Height = 140,
            ShapeType = ShapeType.WaveRibbon,
            FillColorHex = "#064E3B",
            StrokeColorHex = "#065F46",
            StrokeThickness = 1.0,
            Opacity = 0.35,
            ZIndex = 1
        });

        // ==========================================
        // 2. HERO HEADER & BÉZIER CURVED TYPOGRAPHY
        // ==========================================

        // Top Tag Badge with Background Box & Character Tracking
        page.Elements.Add(new PdfTextElement
        {
            Text = "✦ INTERNATIONAL TYPOGRAPHY & VECTOR ARTS SHOWCASE 2026 ✦",
            FontFamily = "Montserrat",
            FontSize = 8.5,
            IsBold = true,
            TextColorHex = "#818CF8",
            Alignment = TextAlignmentMode.Center,
            CharacterSpacing = 2.2,
            BackgroundColorHex = "#1E1B4B",
            BorderColorHex = "#4F46E5",
            BorderThickness = 1.0,
            CornerRadius = 12,
            Padding = 6,
            X = 65,
            Y = 28,
            Width = 465,
            Height = 26,
            ZIndex = 5
        });

        // Hero Bézier Wave Curve Typography
        page.Elements.Add(new PdfTextElement
        {
            Text = "SYMPHONY OF BÉZIER CURVES & TYPOGRAPHY",
            FontFamily = "Cinzel",
            FontSize = 20,
            IsBold = true,
            TextColorHex = "#F8FAFC",
            ShapeMode = TextShapeMode.BezierCurve,
            BezierPreset = BezierCurvePreset.Wave,
            CharacterSpacing = 2.0,
            HasShadow = true,
            ShadowColorHex = "#80000000",
            ShadowOffsetX = 2,
            ShadowOffsetY = 3,
            ShadowBlurRadius = 5,
            HasStroke = true,
            StrokeColorHex = "#6366F1",
            StrokeWidth = 0.8,
            X = 40,
            Y = 62,
            Width = 515,
            Height = 72,
            ZIndex = 6
        });

        // Curved Bridge Arc Subtitle
        page.Elements.Add(new PdfTextElement
        {
            Text = "EXPLORING MATHEMATICAL SPLINES, CALLIGRAPHY & ADVANCED TEXT MANIPULATION",
            FontFamily = "Montserrat",
            FontSize = 8.0,
            IsBold = true,
            TextColorHex = "#F59E0B",
            ShapeMode = TextShapeMode.BezierCurve,
            BezierPreset = BezierCurvePreset.Bridge,
            CharacterSpacing = 2.0,
            X = 50,
            Y = 135,
            Width = 495,
            Height = 35,
            ZIndex = 7
        });

        // Calligraphic Flourish Gold Divider
        page.Elements.Add(new PdfDividerElement
        {
            X = 50,
            Y = 170,
            Width = 495,
            Height = 22,
            Style = DividerStyle.CalligraphicFlourish,
            WaveAmplitude = 6.0,
            Thickness = 1.8,
            ColorHex = "#D97706",
            ZIndex = 8
        });

        // ==========================================
        // 3. LEFT COLUMN: TYPOGRAPHY ENGINE CARD
        // ==========================================

        // Card Container
        page.Elements.Add(new PdfShapeElement
        {
            X = 48,
            Y = 198,
            Width = 240,
            Height = 252,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 8,
            FillColorHex = "#0F172A",
            StrokeColorHex = "#334155",
            StrokeThickness = 1.2,
            ZIndex = 10
        });

        // Card Header Title
        page.Elements.Add(new PdfTextElement
        {
            Text = "01. ADVANCED TYPOGRAPHY",
            FontFamily = "Montserrat",
            FontSize = 9.5,
            IsBold = true,
            TextColorHex = "#38BDF8",
            CharacterSpacing = 1.5,
            X = 60,
            Y = 210,
            Width = 216,
            Height = 18,
            ZIndex = 11
        });

        // Drop Shadow Text Demonstration
        page.Elements.Add(new PdfTextElement
        {
            Text = "ELEGANT DROP SHADOWS",
            FontFamily = "Playfair Display",
            FontSize = 13,
            IsBold = true,
            TextColorHex = "#FFFFFF",
            HasShadow = true,
            ShadowColorHex = "#4F46E5",
            ShadowOffsetX = 2,
            ShadowOffsetY = 3,
            ShadowBlurRadius = 4,
            X = 60,
            Y = 232,
            Width = 216,
            Height = 24,
            ZIndex = 12
        });

        // Hollow Outline Stroke Text Demonstration
        page.Elements.Add(new PdfTextElement
        {
            Text = "HOLLOW OUTLINE STROKE",
            FontFamily = "Cinzel",
            FontSize = 12,
            IsBold = true,
            TextColorHex = "#0F172A",
            HasStroke = true,
            StrokeColorHex = "#A855F7",
            StrokeWidth = 1.2,
            X = 60,
            Y = 262,
            Width = 216,
            Height = 22,
            ZIndex = 13
        });

        // Paragraph Formatting, Word Wrapping & Spacing Demonstration
        page.Elements.Add(new PdfTextElement
        {
            Text = "Full typography layout engine with dynamic tracking, word wrapping, 1.4x line multipliers, and per-glyph arc-length parameterization.",
            FontFamily = "Inter",
            FontSize = 8.2,
            TextColorHex = "#94A3B8",
            LineHeight = 1.35,
            X = 60,
            Y = 292,
            Width = 216,
            Height = 50,
            ZIndex = 14
        });

        // Kerning & Tracking Badge
        page.Elements.Add(new PdfTextElement
        {
            Text = "TRACKING & KERNING: +2.5 PT",
            FontFamily = "Fira Code",
            FontSize = 7.5,
            IsBold = true,
            TextColorHex = "#10B981",
            CharacterSpacing = 1.5,
            BackgroundColorHex = "#064E3B",
            BorderColorHex = "#059669",
            BorderThickness = 1.0,
            CornerRadius = 4,
            Padding = 4,
            X = 60,
            Y = 352,
            Width = 216,
            Height = 22,
            ZIndex = 15
        });

        // Wave Sub-divider inside Left Card
        page.Elements.Add(new PdfDividerElement
        {
            X = 60,
            Y = 382,
            Width = 216,
            Height = 12,
            Style = DividerStyle.Wave,
            WaveAmplitude = 3.0,
            WaveFrequency = 4.0,
            Thickness = 1.0,
            ColorHex = "#38BDF8",
            ZIndex = 16
        });

        // Left Card Footer
        page.Elements.Add(new PdfTextElement
        {
            Text = "✔ 100% Vector Quality at Any PDF Scale",
            FontFamily = "Inter",
            FontSize = 7.5,
            IsBold = true,
            TextColorHex = "#38BDF8",
            X = 60,
            Y = 402,
            Width = 216,
            Height = 18,
            ZIndex = 17
        });

        // ==========================================
        // 4. RIGHT COLUMN: BÉZIER VECTOR SHAPES CARD
        // ==========================================

        // Card Container
        page.Elements.Add(new PdfShapeElement
        {
            X = 307,
            Y = 198,
            Width = 240,
            Height = 252,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 8,
            FillColorHex = "#0F172A",
            StrokeColorHex = "#334155",
            StrokeThickness = 1.2,
            ZIndex = 10
        });

        // Card Header Title
        page.Elements.Add(new PdfTextElement
        {
            Text = "02. BÉZIER VECTOR GRAPHICS",
            FontFamily = "Montserrat",
            FontSize = 9.5,
            IsBold = true,
            TextColorHex = "#EC4899",
            CharacterSpacing = 1.5,
            X = 319,
            Y = 210,
            Width = 216,
            Height = 18,
            ZIndex = 11
        });

        // Medal Ribbon Badge Seal
        page.Elements.Add(new PdfShapeElement
        {
            X = 318,
            Y = 236,
            Width = 62,
            Height = 62,
            ShapeType = ShapeType.MedalRibbonBadge,
            FillColorHex = "#F59E0B",
            SecondaryFillColorHex = "#990000",
            StrokeColorHex = "#B45309",
            StrokeThickness = 1.5,
            Label = "GOLD 2026",
            LabelFontSize = 7.5,
            LabelColorHex = "#78350F",
            ZIndex = 12
        });

        // Curved Speech Callout Bubble
        page.Elements.Add(new PdfShapeElement
        {
            X = 390,
            Y = 236,
            Width = 145,
            Height = 58,
            ShapeType = ShapeType.CurvedCallout,
            FillColorHex = "#1E1B4B",
            StrokeColorHex = "#6366F1",
            StrokeThickness = 1.2,
            Label = "Cubic Spline Math",
            LabelFontSize = 8.5,
            LabelColorHex = "#E0E7FF",
            ZIndex = 13
        });

        // Curved Arrow Demonstration
        page.Elements.Add(new PdfShapeElement
        {
            X = 318,
            Y = 308,
            Width = 90,
            Height = 36,
            ShapeType = ShapeType.CurvedArrow,
            StrokeColorHex = "#F43F5E",
            StrokeThickness = 2.0,
            EndCap = LineEndCap.Arrow,
            ZIndex = 14
        });

        // S-Curve Connector with Dashes
        page.Elements.Add(new PdfShapeElement
        {
            X = 422,
            Y = 312,
            Width = 110,
            Height = 30,
            ShapeType = ShapeType.SCurveConnector,
            StrokeColorHex = "#38BDF8",
            StrokeThickness = 1.8,
            DashStyle = LineDashStyle.Dashed,
            ZIndex = 15
        });

        // Calligraphic Curly Brace
        page.Elements.Add(new PdfShapeElement
        {
            X = 318,
            Y = 356,
            Width = 24,
            Height = 62,
            ShapeType = ShapeType.CurlyBrace,
            StrokeColorHex = "#FCD34D",
            StrokeThickness = 1.8,
            ZIndex = 16
        });

        // Grouped Feature Description next to Brace
        page.Elements.Add(new PdfTextElement
        {
            Text = "Smooth C¹ Tangents\nHarmonic Wave Cycles\nParametric Endcaps",
            FontFamily = "Inter",
            FontSize = 8.0,
            TextColorHex = "#CBD5E1",
            LineHeight = 1.3,
            X = 348,
            Y = 362,
            Width = 185,
            Height = 52,
            ZIndex = 17
        });

        // ==========================================
        // 5. MIDDLE HARMONIC WAVE DIVIDER
        // ==========================================

        page.Elements.Add(new PdfDividerElement
        {
            X = 50,
            Y = 460,
            Width = 495,
            Height = 22,
            Style = DividerStyle.DoubleWave,
            WaveAmplitude = 5.5,
            WaveFrequency = 6.0,
            Thickness = 1.5,
            ColorHex = "#6366F1",
            ZIndex = 20
        });

        // ==========================================
        // 6. MASTERCLASSES & SCHEDULE SECTION
        // ==========================================

        // Container Card
        page.Elements.Add(new PdfShapeElement
        {
            X = 48,
            Y = 490,
            Width = 499,
            Height = 175,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 8,
            FillColorHex = "#0F172A",
            StrokeColorHex = "#334155",
            StrokeThickness = 1.2,
            ZIndex = 21
        });

        // Section Title
        page.Elements.Add(new PdfTextElement
        {
            Text = "FEATURED SYMPOSIUM SESSIONS & MASTERCLASSES",
            FontFamily = "Cinzel",
            FontSize = 11,
            IsBold = true,
            TextColorHex = "#F8FAFC",
            Alignment = TextAlignmentMode.Center,
            CharacterSpacing = 1.2,
            X = 60,
            Y = 500,
            Width = 475,
            Height = 18,
            ZIndex = 22
        });

        // Session 1 (Left)
        page.Elements.Add(new PdfTextElement
        {
            Text = "10:00 AM • STAGE A",
            FontFamily = "Montserrat",
            FontSize = 7.5,
            IsBold = true,
            TextColorHex = "#38BDF8",
            BackgroundColorHex = "#0C4A6E",
            BorderColorHex = "#0284C7",
            BorderThickness = 1.0,
            CornerRadius = 3,
            Padding = 3,
            X = 62,
            Y = 526,
            Width = 115,
            Height = 18,
            ZIndex = 23
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "Mathematical Foundations of Bézier Warping",
            FontFamily = "Playfair Display",
            FontSize = 10,
            IsBold = true,
            TextColorHex = "#FFFFFF",
            X = 62,
            Y = 548,
            Width = 220,
            Height = 18,
            ZIndex = 24
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "Dr. Jane Doe — Senior Computational Typographer",
            FontFamily = "Inter",
            FontSize = 7.8,
            TextColorHex = "#94A3B8",
            X = 62,
            Y = 566,
            Width = 220,
            Height = 18,
            ZIndex = 25
        });

        // Session 2 (Right)
        page.Elements.Add(new PdfTextElement
        {
            Text = "02:30 PM • STAGE B",
            FontFamily = "Montserrat",
            FontSize = 7.5,
            IsBold = true,
            TextColorHex = "#F472B6",
            BackgroundColorHex = "#831843",
            BorderColorHex = "#DB2777",
            BorderThickness = 1.0,
            CornerRadius = 3,
            Padding = 3,
            X = 308,
            Y = 526,
            Width = 115,
            Height = 18,
            ZIndex = 23
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "Calligraphic Curves & Catmull-Rom Splines",
            FontFamily = "Playfair Display",
            FontSize = 10,
            IsBold = true,
            TextColorHex = "#FFFFFF",
            X = 308,
            Y = 548,
            Width = 225,
            Height = 18,
            ZIndex = 24
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "Prof. John Doe — Director of Vector Arts Institute",
            FontFamily = "Inter",
            FontSize = 7.8,
            TextColorHex = "#94A3B8",
            X = 308,
            Y = 566,
            Width = 225,
            Height = 18,
            ZIndex = 25
        });

        // Wave Accent Line inside Schedule Card
        page.Elements.Add(new PdfShapeElement
        {
            X = 62,
            Y = 594,
            Width = 471,
            Height = 12,
            ShapeType = ShapeType.WaveLine,
            StrokeColorHex = "#4F46E5",
            StrokeThickness = 1.0,
            ZIndex = 26
        });

        // Footer Note in Schedule Card
        page.Elements.Add(new PdfTextElement
        {
            Text = "✦ All sessions include live hands-on Bézier text and curve manipulation workshops ✦",
            FontFamily = "Inter",
            FontSize = 7.8,
            IsItalic = true,
            TextColorHex = "#64748B",
            Alignment = TextAlignmentMode.Center,
            X = 62,
            Y = 614,
            Width = 471,
            Height = 18,
            ZIndex = 27
        });

        // ==========================================
        // 7. BOTTOM VERIFICATION & FREEHAND INK
        // ==========================================

        // Arch Gold Rule
        page.Elements.Add(new PdfDividerElement
        {
            X = 50,
            Y = 675,
            Width = 495,
            Height = 16,
            Style = DividerStyle.Arch,
            WaveAmplitude = 5.0,
            Thickness = 1.5,
            ColorHex = "#D97706",
            ZIndex = 30
        });

        // QR Code Verification
        page.Elements.Add(new PdfQrCodeElement
        {
            X = 60,
            Y = 698,
            Width = 68,
            Height = 68,
            Content = "https://codefrydev.in/showcase/bezier-typography-2026",
            DarkColorHex = "#F8FAFC",
            LightColorHex = "#0B0F19",
            ZIndex = 31
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "SCAN TO VERIFY\nVECTOR ASSETS",
            FontFamily = "Montserrat",
            FontSize = 6.0,
            IsBold = true,
            TextColorHex = "#64748B",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 770,
            Width = 88,
            Height = 22,
            ZIndex = 32
        });

        // Jury Header
        page.Elements.Add(new PdfTextElement
        {
            Text = "EXECUTIVE CURATORS & TYPOGRAPHY JURY CERTIFICATION",
            FontFamily = "Montserrat",
            FontSize = 7.8,
            IsBold = true,
            TextColorHex = "#94A3B8",
            CharacterSpacing = 1.2,
            X = 175,
            Y = 698,
            Width = 370,
            Height = 18,
            ZIndex = 33
        });

        // Signer 1: Smooth Ink Signature (Jane Doe)
        page.Elements.Add(new PdfInkElement
        {
            X = 175,
            Y = 716,
            Width = 145,
            Height = 40,
            PointsData = "175,745 195,725 215,750 235,722 255,746 280,728 305,755",
            StrokeColorHex = "#38BDF8",
            StrokeThickness = 2.2,
            Opacity = 0.95,
            IsSmoothSpline = true,
            ZIndex = 34
        });

        page.Elements.Add(new PdfDividerElement
        {
            X = 175,
            Y = 758,
            Width = 145,
            Height = 4,
            Style = DividerStyle.Straight,
            Thickness = 1.0,
            ColorHex = "#475569",
            ZIndex = 35
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "Dr. Jane Doe\nLead Typographer & Jury Chair",
            FontFamily = "Inter",
            FontSize = 7.0,
            TextColorHex = "#94A3B8",
            X = 175,
            Y = 766,
            Width = 145,
            Height = 22,
            ZIndex = 36
        });

        // Signer 2: Smooth Ink Signature (John Doe)
        page.Elements.Add(new PdfInkElement
        {
            X = 355,
            Y = 716,
            Width = 145,
            Height = 40,
            PointsData = "355,748 375,726 395,752 420,724 445,750 475,732",
            StrokeColorHex = "#F472B6",
            StrokeThickness = 2.2,
            Opacity = 0.95,
            IsSmoothSpline = true,
            ZIndex = 34
        });

        page.Elements.Add(new PdfDividerElement
        {
            X = 355,
            Y = 758,
            Width = 145,
            Height = 4,
            Style = DividerStyle.Straight,
            Thickness = 1.0,
            ColorHex = "#475569",
            ZIndex = 35
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "Prof. John Doe\nDirector of Vector Arts Institute",
            FontFamily = "Inter",
            FontSize = 7.0,
            TextColorHex = "#94A3B8",
            X = 355,
            Y = 766,
            Width = 145,
            Height = 22,
            ZIndex = 36
        });

        // Final Bottom Subtle Brand Footnote
        page.Elements.Add(new PdfTextElement
        {
            Text = "DESIGNED WITH ADVANCED BÉZIER VECTOR & TYPOGRAPHY ENGINE • PURE VECTOR PDF EXPORT",
            FontFamily = "Montserrat",
            FontSize = 6.2,
            IsBold = true,
            TextColorHex = "#475569",
            Alignment = TextAlignmentMode.Center,
            CharacterSpacing = 1.5,
            X = 40,
            Y = 812,
            Width = 515,
            Height = 18,
            ZIndex = 40
        });

        doc.Pages.Add(page);
        return doc;
    }
}
