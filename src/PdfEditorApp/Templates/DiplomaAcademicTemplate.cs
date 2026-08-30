using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class DiplomaAcademicTemplate : ITemplateDefinition
{
    public string Id => "diploma";
    public string Name => "Collegiate Academic Diploma";
    public string Description => "Formal university degree with ornamental borders, Latin crest, and official seal";
    public string Category => "Certificates";
    public string IconKind => "SchoolOutline";
    public string AccentColorHex => "#7C2D12";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Collegiate_Diploma.pdf",
            Author = "University of Advanced Sciences",
            Subject = "Degree of Master of Computer Science"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Landscape,
            Width = 1131,
            Height = 800,
            BackgroundColorHex = "#FFFDF9",
            ShowHeaderFooter = false,
            Elements = new List<PdfElementBase>
            {
                // Formal Multi-Border Inset
                new PdfShapeElement
                {
                    X = 30,
                    Y = 30,
                    Width = 1071,
                    Height = 740,
                    FillColorHex = "#00000000",
                    StrokeColorHex = "#78350F",
                    StrokeThickness = 3.5,
                    CornerRadius = 4,
                    ZIndex = 1
                },
                new PdfShapeElement
                {
                    X = 40,
                    Y = 40,
                    Width = 1051,
                    Height = 720,
                    FillColorHex = "#00000000",
                    StrokeColorHex = "#D97706",
                    StrokeThickness = 1.0,
                    CornerRadius = 2,
                    ZIndex = 2
                },

                // University Name
                new PdfTextElement
                {
                    X = 100,
                    Y = 80,
                    Width = 931,
                    Height = 45,
                    Text = "THE TRUSTEES AND FACULTY OF",
                    FontSize = 14,
                    FontFamily = "Cinzel",
                    IsBold = true,
                    TextColorHex = "#78350F",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 100,
                    Y = 125,
                    Width = 931,
                    Height = 55,
                    Text = "UNIVERSITY OF ADVANCED SCIENCES",
                    FontSize = 36,
                    FontFamily = "Cinzel",
                    IsBold = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 100,
                    Y = 195,
                    Width = 931,
                    Height = 25,
                    Text = "UPON THE RECOMMENDATION OF THE FACULTY HAVE CONFERRED UPON",
                    FontSize = 11,
                    FontFamily = "Playfair Display",
                    IsItalic = true,
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // Graduate Name
                new PdfTextElement
                {
                    X = 100,
                    Y = 235,
                    Width = 931,
                    Height = 90,
                    Text = "Jonathan Edward Reynolds",
                    FontSize = 52,
                    FontFamily = "Great Vibes",
                    IsItalic = true,
                    TextColorHex = "#78350F",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 6
                },

                // Degree Conferred
                new PdfTextElement
                {
                    X = 100,
                    Y = 330,
                    Width = 931,
                    Height = 25,
                    Text = "THE DEGREE OF",
                    FontSize = 11,
                    FontFamily = "Cinzel",
                    IsBold = true,
                    TextColorHex = "#78350F",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 100,
                    Y = 360,
                    Width = 931,
                    Height = 45,
                    Text = "MASTER OF SCIENCE IN ARTIFICIAL INTELLIGENCE",
                    FontSize = 24,
                    FontFamily = "Cinzel",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 150,
                    Y = 415,
                    Width = 831,
                    Height = 50,
                    Text = "WITH ALL THE RIGHTS, PRIVILEGES, AND HONORS THEREUNTO APPERTAINING",
                    FontSize = 11,
                    FontFamily = "Playfair Display",
                    IsItalic = true,
                    TextColorHex = "#475569",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // Official University Gold Seal
                new PdfShapeElement
                {
                    X = 515,
                    Y = 480,
                    Width = 100,
                    Height = 100,
                    ShapeType = ShapeType.LaurelWreathSeal,
                    FillColorHex = "#FEF3C7",
                    StrokeColorHex = "#B45309",
                    StrokeThickness = 2.5,
                    Label = "SEAL",
                    LabelColorHex = "#92400E",
                    LabelFontSize = 12,
                    ZIndex = 8
                },

                // Date
                new PdfTextElement
                {
                    X = 150,
                    Y = 610,
                    Width = 250,
                    Height = 25,
                    Text = "June 15, 2026",
                    FontSize = 14,
                    FontFamily = "Playfair Display",
                    IsBold = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },
                new PdfDividerElement
                {
                    X = 150,
                    Y = 640,
                    Width = 250,
                    Height = 1,
                    Thickness = 1.5,
                    ColorHex = "#78350F",
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 150,
                    Y = 648,
                    Width = 250,
                    Height = 22,
                    Text = "President of the University",
                    FontSize = 12,
                    FontFamily = "Cinzel",
                    IsBold = true,
                    TextColorHex = "#78350F",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                },

                // Dean Signature
                new PdfTextElement
                {
                    X = 731,
                    Y = 590,
                    Width = 250,
                    Height = 50,
                    Text = "Eleanor Thorne",
                    FontSize = 30,
                    FontFamily = "Great Vibes",
                    IsItalic = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 6
                },
                new PdfDividerElement
                {
                    X = 731,
                    Y = 640,
                    Width = 250,
                    Height = 1,
                    Thickness = 1.5,
                    ColorHex = "#78350F",
                    ZIndex = 5
                },
                new PdfTextElement
                {
                    X = 731,
                    Y = 648,
                    Width = 250,
                    Height = 22,
                    Text = "Dean of Graduate Studies",
                    FontSize = 12,
                    FontFamily = "Cinzel",
                    IsBold = true,
                    TextColorHex = "#78350F",
                    Alignment = TextAlignmentMode.Center,
                    ZIndex = 5
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
