using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Certificates;

public class CertificateTemplate : ITemplateDefinition
{
    public string Id => "certificate";
    public string Name => "Certificate of Achievement";
    public string Description => "National Olympiad award of distinction with gold medal ribbon seal, ornate crimson borders, and official verification signatures";
    public string Category => "Certificates";
    public string IconKind => "CertificateOutline";
    public string AccentColorHex => "#990000";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Certificate_of_Achievement.pdf",
            Author = "CodeFryDev Academy of Computing & Science",
            Subject = "Official Certificate of Outstanding Achievement & First-Place Distinction"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Landscape,
            Width = 1131,
            Height = 800,
            BackgroundColorHex = "#FFFFFF",
            ShowHeaderFooter = false,
            Elements = new List<PdfElementBase>
            {
                // ==========================================
                // 1. LEFT CORNER VECTOR POLYGONAL ART
                // ==========================================

                // Main Top-Left Deep Crimson Polygon Wing
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 260,
                    Height = 800,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 0,0 L 220,0 L 160,380 L 0,720 Z",
                    FillColorHex = "#990000",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0,
                    ZIndex = 1
                },

                // Top-Left Gold Accent Stripe
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 285,
                    Height = 800,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 220,0 L 245,0 L 180,380 L 0,770 L 0,720 L 160,380 Z",
                    FillColorHex = "#F59E0B",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0,
                    ZIndex = 2
                },

                // Bottom-Left Sharp Dark Red Polygonal Wedge
                new PdfShapeElement
                {
                    X = 0,
                    Y = 480,
                    Width = 160,
                    Height = 320,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 0,520 L 140,800 L 0,800 Z",
                    FillColorHex = "#800000",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0,
                    ZIndex = 3
                },

                // ==========================================
                // 2. RIGHT CORNER VECTOR POLYGONAL ART
                // ==========================================

                // Top-Right Gold Accent Strip
                new PdfShapeElement
                {
                    X = 1040,
                    Y = 0,
                    Width = 91,
                    Height = 160,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 1060,0 L 1131,0 L 1131,140 Z",
                    FillColorHex = "#F59E0B",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0,
                    ZIndex = 1
                },

                // Top-Right Crimson Corner Wedge
                new PdfShapeElement
                {
                    X = 1080,
                    Y = 0,
                    Width = 51,
                    Height = 90,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 1090,0 L 1131,0 L 1131,80 Z",
                    FillColorHex = "#990000",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0,
                    ZIndex = 2
                },

                // Bottom-Right Gold Accent Border Stripe
                new PdfShapeElement
                {
                    X = 820,
                    Y = 420,
                    Width = 311,
                    Height = 380,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 1131,430 L 1131,470 L 960,630 L 865,800 L 835,800 L 935,620 Z",
                    FillColorHex = "#F59E0B",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0,
                    ZIndex = 1
                },

                // Bottom-Right Bold Crimson Polygon Wing
                new PdfShapeElement
                {
                    X = 860,
                    Y = 460,
                    Width = 271,
                    Height = 340,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 1131,470 L 1131,800 L 865,800 L 960,630 Z",
                    FillColorHex = "#990000",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0,
                    ZIndex = 2
                },

                // ==========================================
                // 3. GOLD MEDAL SEAL BADGE WITH RIBBON TAILS
                // ==========================================
                new PdfShapeElement
                {
                    X = 165,
                    Y = 95,
                    Width = 160,
                    Height = 195,
                    ShapeType = ShapeType.MedalRibbonBadge,
                    FillColorHex = "#F59E0B",
                    StrokeColorHex = "#B45309",
                    StrokeThickness = 2.5,
                    SecondaryFillColorHex = "#990000",
                    ZIndex = 10
                },

                // ==========================================
                // 4. MAIN HEADINGS & CERTIFICATE TITLE
                // ==========================================
                new PdfTextElement
                {
                    X = 280,
                    Y = 42,
                    Width = 700,
                    Height = 68,
                    Text = "CERTIFICATE",
                    FontSize = 46,
                    FontFamily = "Montserrat",
                    IsBold = true,
                    TextColorHex = "#990000",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 280,
                    Y = 110,
                    Width = 700,
                    Height = 40,
                    Text = "OF OUTSTANDING ACHIEVEMENT",
                    FontSize = 22,
                    FontFamily = "Montserrat",
                    IsBold = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 280,
                    Y = 165,
                    Width = 700,
                    Height = 28,
                    Text = "THIS CERTIFICATE IS PROUDLY PRESENTED TO",
                    FontSize = 12.5,
                    FontFamily = "Montserrat",
                    IsBold = true,
                    TextColorHex = "#475569",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // ==========================================
                // 5. RECIPIENT NAME IN CURSIVE CALLIGRAPHY
                // ==========================================
                new PdfTextElement
                {
                    X = 250,
                    Y = 205,
                    Width = 760,
                    Height = 95,
                    Text = "John Doe",
                    FontSize = 52,
                    FontFamily = "Great Vibes",
                    IsItalic = true,
                    TextColorHex = "#990000",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 6
                },

                // Red Horizontal Accent Rule
                new PdfDividerElement
                {
                    X = 240,
                    Y = 310,
                    Width = 760,
                    Height = 2,
                    Thickness = 2.0,
                    ColorHex = "#990000",
                    ZIndex = 5
                },

                // ==========================================
                // 6. CITATION & ACHIEVEMENT DESCRIPTION
                // ==========================================
                new PdfTextElement
                {
                    X = 240,
                    Y = 330,
                    Width = 760,
                    Height = 115,
                    Text = "For distinguished excellence, exceptional analytical problem-solving, and achieving First Place Honors in the CodeFryDev Mathematics & Computational Science Olympiad. Conferred on the 18th of July, 2026 by the Executive Board of Examiners.",
                    FontSize = 15.5,
                    FontFamily = "Lora",
                    IsBold = false,
                    LineHeight = 1.55,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // Verification Badge (Center)
                new PdfShapeElement
                {
                    X = 525,
                    Y = 460,
                    Width = 80,
                    Height = 80,
                    ShapeType = ShapeType.RosetteSeal,
                    FillColorHex = "#FEF3C7",
                    StrokeColorHex = "#D97706",
                    StrokeThickness = 2,
                    Label = "1ST PLACE",
                    LabelColorHex = "#92400E",
                    LabelFontSize = 10,
                    ZIndex = 8
                },

                // ==========================================
                // 7. DATE & SIGNATURE COLUMNS
                // ==========================================

                // Date Signature (Left)
                new PdfTextElement
                {
                    X = 210,
                    Y = 575,
                    Width = 260,
                    Height = 40,
                    Text = "July 18, 2026",
                    FontSize = 18,
                    FontFamily = "Playfair Display",
                    IsBold = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfDividerElement
                {
                    X = 210,
                    Y = 620,
                    Width = 260,
                    Height = 2,
                    Thickness = 1.5,
                    ColorHex = "#1E293B",
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 210,
                    Y = 628,
                    Width = 260,
                    Height = 26,
                    Text = "Date of Issuance",
                    FontSize = 12.5,
                    FontFamily = "Montserrat",
                    IsBold = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 210,
                    Y = 654,
                    Width = 260,
                    Height = 22,
                    Text = "Credential ID: CFD-2026-89412",
                    FontSize = 10,
                    FontFamily = "Montserrat",
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // Cursive Handwritten Signature (Right)
                new PdfTextElement
                {
                    X = 660,
                    Y = 555,
                    Width = 280,
                    Height = 60,
                    Text = "Jane Doe",
                    FontSize = 32,
                    FontFamily = "Great Vibes",
                    IsItalic = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 6
                },
                new PdfDividerElement
                {
                    X = 660,
                    Y = 620,
                    Width = 280,
                    Height = 2,
                    Thickness = 1.5,
                    ColorHex = "#1E293B",
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 660,
                    Y = 628,
                    Width = 280,
                    Height = 26,
                    Text = "Dr. Jane Doe, Ph.D.",
                    FontSize = 12.5,
                    FontFamily = "Montserrat",
                    IsBold = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 660,
                    Y = 654,
                    Width = 280,
                    Height = 22,
                    Text = "President, CodeFryDev Academy",
                    FontSize = 10,
                    FontFamily = "Montserrat",
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
