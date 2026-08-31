using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class CertificateNavyGoldTemplate : ITemplateDefinition
{
    public string Id => "certificatenavygold";
    public string Name => "Executive Certificate of Honor";
    public string Description => "Prestigious corporate & executive recognition with navy & gold geometric crests";
    public string Category => "Certificates";
    public string IconKind => "ShieldCrownOutline";
    public string AccentColorHex => "#1E3A8A";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Executive_Certificate_of_Honor.pdf",
            Author = "CodeFryDev Leadership Institute",
            Subject = "Official Certificate of Distinction and Honor"
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
                // Outer Navy Frame
                new PdfShapeElement
                {
                    X = 35,
                    Y = 35,
                    Width = 1061,
                    Height = 730,
                    FillColorHex = "#00000000",
                    StrokeColorHex = "#0F172A",
                    StrokeThickness = 4,
                    CornerRadius = 8,
                    ZIndex = 1
                },
                // Inner Gold Thin Inset Frame
                new PdfShapeElement
                {
                    X = 45,
                    Y = 45,
                    Width = 1041,
                    Height = 710,
                    FillColorHex = "#00000000",
                    StrokeColorHex = "#D97706",
                    StrokeThickness = 1.5,
                    CornerRadius = 6,
                    ZIndex = 2
                },

                // Top Geometric Navy & Gold Corner Accents
                new PdfShapeElement
                {
                    X = 35,
                    Y = 35,
                    Width = 180,
                    Height = 180,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 0,0 L 180,0 L 0,180 Z",
                    FillColorHex = "#0F172A",
                    StrokeColorHex = "#00000000",
                    ZIndex = 3
                },
                new PdfShapeElement
                {
                    X = 35,
                    Y = 35,
                    Width = 205,
                    Height = 205,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 180,0 L 205,0 L 0,205 L 0,180 Z",
                    FillColorHex = "#D97706",
                    StrokeColorHex = "#00000000",
                    ZIndex = 4
                },
                new PdfShapeElement
                {
                    X = 916,
                    Y = 585,
                    Width = 180,
                    Height = 180,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 180,180 L 0,180 L 180,0 Z",
                    FillColorHex = "#0F172A",
                    StrokeColorHex = "#00000000",
                    ZIndex = 3
                },
                new PdfShapeElement
                {
                    X = 891,
                    Y = 560,
                    Width = 205,
                    Height = 205,
                    ShapeType = ShapeType.CustomSvgPath,
                    CustomPathData = "M 0,180 L 0,205 L 205,0 L 180,0 Z",
                    FillColorHex = "#D97706",
                    StrokeColorHex = "#00000000",
                    ZIndex = 4
                },

                // Golden Shield Heraldic Crest
                new PdfShapeElement
                {
                    X = 525,
                    Y = 70,
                    Width = 80,
                    Height = 90,
                    ShapeType = ShapeType.ShieldBadge,
                    FillColorHex = "#FEF3C7",
                    StrokeColorHex = "#D97706",
                    StrokeThickness = 2.5,
                    Label = "CFD",
                    LabelColorHex = "#B45309",
                    LabelFontSize = 14,
                    ZIndex = 10
                },

                // Main Title
                new PdfTextElement
                {
                    X = 150,
                    Y = 175,
                    Width = 831,
                    Height = 54,
                    Text = "CERTIFICATE OF DISTINCTION",
                    FontSize = 38,
                    FontFamily = "Cinzel",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 150,
                    Y = 230,
                    Width = 831,
                    Height = 26,
                    Text = "IN RECOGNITION OF EXCEPTIONAL EXECUTIVE LEADERSHIP & STRATEGIC VISION",
                    FontSize = 11.5,
                    FontFamily = "Montserrat",
                    IsBold = true,
                    TextColorHex = "#B45309",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // Recipient Name
                new PdfTextElement
                {
                    X = 150,
                    Y = 280,
                    Width = 831,
                    Height = 90,
                    Text = "Jane Doe, MBA",
                    FontSize = 48,
                    FontFamily = "Great Vibes",
                    IsItalic = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 6
                },

                // Gold Divider
                new PdfDividerElement
                {
                    X = 300,
                    Y = 370,
                    Width = 531,
                    Height = 2,
                    Thickness = 2.0,
                    ColorHex = "#D97706",
                    ZIndex = 5
                },

                // Body Text
                new PdfTextElement
                {
                    X = 180,
                    Y = 395,
                    Width = 771,
                    Height = 72,
                    Text = "Having demonstrated supreme governance, transformational strategy execution, and unrelenting commitment to corporate stewardship throughout the Global Enterprise Leadership Fellowship.",
                    FontSize = 15.5,
                    FontFamily = "Lora",
                    LineHeight = 1.5,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // Conferred Date & Motto
                new PdfTextElement
                {
                    X = 180,
                    Y = 470,
                    Width = 771,
                    Height = 26,
                    Text = "Conferred this 28th day of October, 2026 at the Annual Convocation • London, United Kingdom",
                    FontSize = 11.5,
                    FontFamily = "Montserrat",
                    IsBold = true,
                    TextColorHex = "#B45309",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // Gold 16-Point Rosette Star Seal
                new PdfShapeElement
                {
                    X = 525,
                    Y = 498,
                    Width = 80,
                    Height = 80,
                    ShapeType = ShapeType.RosetteSeal,
                    FillColorHex = "#FEF3C7",
                    StrokeColorHex = "#D97706",
                    StrokeThickness = 2,
                    Label = "HONOR",
                    LabelColorHex = "#92400E",
                    LabelFontSize = 11,
                    ZIndex = 8
                },

                // Motto Text Below Seal
                new PdfTextElement
                {
                    X = 450,
                    Y = 584,
                    Width = 231,
                    Height = 22,
                    Text = "— VIRTUS ET EXCELLENTIA —",
                    FontSize = 10,
                    FontFamily = "Cinzel",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 8
                },

                // Signatory Left
                new PdfTextElement
                {
                    X = 180,
                    Y = 620,
                    Width = 260,
                    Height = 48,
                    Text = "John Doe",
                    FontSize = 26,
                    FontFamily = "Dancing Script",
                    IsItalic = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 6
                },
                new PdfDividerElement
                {
                    X = 180,
                    Y = 670,
                    Width = 260,
                    Height = 1,
                    Thickness = 1.5,
                    ColorHex = "#0F172A",
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 180,
                    Y = 678,
                    Width = 260,
                    Height = 26,
                    Text = "Chancellor of the Board",
                    FontSize = 13,
                    FontFamily = "Montserrat",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // Signatory Right
                new PdfTextElement
                {
                    X = 691,
                    Y = 620,
                    Width = 260,
                    Height = 48,
                    Text = "Alex Doe",
                    FontSize = 26,
                    FontFamily = "Dancing Script",
                    IsItalic = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 6
                },
                new PdfDividerElement
                {
                    X = 691,
                    Y = 670,
                    Width = 260,
                    Height = 1,
                    Thickness = 1.5,
                    ColorHex = "#0F172A",
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 691,
                    Y = 678,
                    Width = 260,
                    Height = 26,
                    Text = "Managing Director & Fellow",
                    FontSize = 13,
                    FontFamily = "Montserrat",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
