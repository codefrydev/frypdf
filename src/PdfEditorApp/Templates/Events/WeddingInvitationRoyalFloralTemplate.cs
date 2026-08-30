using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;

namespace PdfEditorApp.Templates;

/// <summary>
/// Luxury Royal Botanical Floral Wedding Invitation with gold leaf wreath, cursive script, and RSVP block.
/// </summary>
public class WeddingInvitationRoyalFloralTemplate : ITemplateDefinition
{
    public string Id => "weddingroyalfloral";
    public string Name => "Royal Botanical Wedding Invitation";
    public string Description => "Luxury gold foil botanical laurel wreath, calligraphy couple script, and RSVP QR Code";
    public string Category => "Events & Invitations";
    public string IconKind => "FlowerTulipOutline";
    public string AccentColorHex => "#D97706";

    public PdfDocumentModel Create()
    {
        return GenerateDocument();
    }

    public static PdfDocumentModel GenerateDocument()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Wedding_Invitation_Royal_Floral.pdf",
            Author = "Royal Occasions",
            Subject = "Luxury Floral Wedding Invitation Card"
        };

        var page = new PdfPageModel
        {
            Width = 600,
            Height = 850,
            Orientation = PageOrientation.Portrait,
            ShowHeaderFooter = false
        };

        // 1. Soft Linen White Background
        page.Elements.Add(new PdfShapeElement
        {
            ShapeType = ShapeType.Rectangle,
            X = 0,
            Y = 0,
            Width = 600,
            Height = 850,
            FillColorHex = "#FAFAFA",
            StrokeColorHex = "#D97706",
            StrokeThickness = 2.0,
            ZIndex = 0
        });

        // 2. Inner Double Gold Border
        page.Elements.Add(new PdfShapeElement
        {
            ShapeType = ShapeType.Rectangle,
            X = 18,
            Y = 18,
            Width = 564,
            Height = 814,
            FillColorHex = "#00000000",
            StrokeColorHex = "#D97706",
            StrokeThickness = 1.0,
            ZIndex = 1
        });

        // 3. Botanical Floral Wreath Monogram Crest at Top
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "BotanicalWreath",
            SvgSource = SvgOrnamentLibrary.GetBotanicalWreathSvg("#D97706", "#059669"),
            X = 230,
            Y = 40,
            Width = 140,
            Height = 140,
            ZIndex = 4
        });

        // Monogram Initial inside wreath
        page.Elements.Add(new PdfTextElement
        {
            Text = "E & J",
            FontFamily = "Great Vibes",
            FontSize = 32,
            IsBold = true,
            TextColorHex = "#D97706",
            Alignment = TextAlignmentMode.Center,
            X = 230,
            Y = 90,
            Width = 140,
            Height = 40,
            ZIndex = 5
        });

        // 4. Header Text
        page.Elements.Add(new PdfTextElement
        {
            Text = "TOGETHER WITH THEIR FAMILIES",
            FontFamily = "Montserrat",
            FontSize = 11,
            IsBold = true,
            TextColorHex = "#6B7280",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 200,
            Width = 500,
            Height = 20,
            ZIndex = 5
        });

        // 5. Couple Names in Great Vibes Cursive Calligraphy
        page.Elements.Add(new PdfTextElement
        {
            Text = "Eleanor Vance",
            FontFamily = "Great Vibes",
            FontSize = 48,
            IsBold = true,
            TextColorHex = "#1F2937",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 235,
            Width = 500,
            Height = 60,
            ZIndex = 6
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "and",
            FontFamily = "Cinzel",
            FontSize = 14,
            IsItalic = true,
            TextColorHex = "#D97706",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 298,
            Width = 500,
            Height = 22,
            ZIndex = 6
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "Julian Montgomery",
            FontFamily = "Great Vibes",
            FontSize = 48,
            IsBold = true,
            TextColorHex = "#1F2937",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 325,
            Width = 500,
            Height = 60,
            ZIndex = 6
        });

        // 6. Center Calligraphic Flourish
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "CalligraphicFlourish",
            SvgSource = SvgOrnamentLibrary.GetCalligraphicFlourishSvg("#D97706"),
            X = 175,
            Y = 390,
            Width = 250,
            Height = 35,
            ZIndex = 6
        });

        // 7. Request Line
        page.Elements.Add(new PdfTextElement
        {
            Text = "REQUEST THE PLEASURE OF YOUR COMPANY\nAT THE CELEBRATION OF THEIR MARRIAGE",
            FontFamily = "Montserrat",
            FontSize = 11,
            IsBold = true,
            TextColorHex = "#4B5563",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 440,
            Width = 500,
            Height = 38,
            ZIndex = 6
        });

        // 8. Date & Time
        page.Elements.Add(new PdfTextElement
        {
            Text = "SATURDAY, SEPTEMBER 19, 2026",
            FontFamily = "Cinzel",
            FontSize = 16,
            IsBold = true,
            TextColorHex = "#D97706",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 495,
            Width = 500,
            Height = 24,
            ZIndex = 6
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "AT FOUR O'CLOCK IN THE AFTERNOON",
            FontFamily = "Montserrat",
            FontSize = 11,
            IsBold = false,
            TextColorHex = "#374151",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 525,
            Width = 500,
            Height = 20,
            ZIndex = 6
        });

        // 9. Venue & Location
        page.Elements.Add(new PdfTextElement
        {
            Text = "THE GRAND ORANGERIE ESTATE\n1042 HIGHLAND VALLEY ROAD\nNAPA VALLEY, CALIFORNIA",
            FontFamily = "Cinzel",
            FontSize = 12.5,
            IsBold = true,
            TextColorHex = "#1F2937",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 560,
            Width = 500,
            Height = 60,
            ZIndex = 6
        });

        // 10. Reception to follow & RSVP Block
        page.Elements.Add(new PdfTextElement
        {
            Text = "RECEPTION, DINNER & DANCING TO FOLLOW",
            FontFamily = "Montserrat",
            FontSize = 11,
            IsBold = true,
            TextColorHex = "#D97706",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 640,
            Width = 500,
            Height = 20,
            ZIndex = 6
        });

        page.Elements.Add(new PdfQrCodeElement
        {
            Content = "https://www.weddingwire.com/eleanor-and-julian-2026",
            Label = "SCAN TO RSVP ONLINE",
            DarkColorHex = "#D97706",
            LightColorHex = "#FAFAFA",
            X = 245,
            Y = 675,
            Width = 110,
            Height = 125,
            ZIndex = 7
        });

        doc.Pages.Add(page);
        return doc;
    }
}
