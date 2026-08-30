using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;

namespace PdfEditorApp.Templates;

/// <summary>
/// Authentic Traditional Indian Wedding Invitation Card template featuring Marigold Torans,
/// Lord Ganesha sacred crest, calligraphic couple names, dual Muhurtham &amp; Reception schedule columns,
/// brass deepams, and ceremonial plantain tree accents.
/// </summary>
public class WeddingInvitationTraditionalTemplate : ITemplateDefinition
{
    public string Id => "weddingtraditional";
    public string Name => "Traditional Indian Wedding Invitation";
    public string Description => "Festive marigold toran, sacred Ganesha crest, dual Muhurtham & Reception schedule, and brass deepams";
    public string Category => "Events & Invitations";
    public string IconKind => "CardsHeartOutline";
    public string AccentColorHex => "#8B0000";

    public PdfDocumentModel Create()
    {
        return GenerateDocument();
    }

    public static PdfDocumentModel GenerateDocument()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Wedding_Invitation_Aarav_and_Ananya.pdf",
            Author = "Sharma & Iyer Families",
            Subject = "Traditional Indian Wedding Invitation Card"
        };

        // Standard Portrait Invitation (600 x 900 pt)
        var page = new PdfPageModel
        {
            Width = 600,
            Height = 900,
            Orientation = PageOrientation.Portrait,
            ShowHeaderFooter = false
        };

        // 1. Warm Luxury Parchment Cream Background
        page.Elements.Add(new PdfShapeElement
        {
            ShapeType = ShapeType.Rectangle,
            X = 0,
            Y = 0,
            Width = 600,
            Height = 900,
            FillColorHex = "#FFFDF7", // Warm parchment ivory
            StrokeColorHex = "#F59E0B",
            StrokeThickness = 2.0,
            ZIndex = 0
        });

        // 2. Inner Golden Delicate Border
        page.Elements.Add(new PdfShapeElement
        {
            ShapeType = ShapeType.Rectangle,
            X = 14,
            Y = 14,
            Width = 572,
            Height = 872,
            FillColorHex = "#00000000",
            StrokeColorHex = "#D97706",
            StrokeThickness = 1.0,
            CornerRadius = 4,
            ZIndex = 1
        });

        // 3. Top Decorative Marigold Floral Toran Garland & Hanging Lamps
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "MarigoldToran",
            SvgSource = SvgOrnamentLibrary.GetMarigoldToranSvg(),
            X = 15,
            Y = 15,
            Width = 570,
            Height = 85,
            ZIndex = 5
        });

        // 4. Auspicious Sacred Lord Ganesha Vector Crest
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "GaneshaCrest",
            SvgSource = SvgOrnamentLibrary.GetGaneshaCrestSvg("#8B0000", "#D97706"),
            X = 260,
            Y = 100,
            Width = 80,
            Height = 85,
            ZIndex = 6
        });

        // 5. Sanskrit Sacred Invocation
        page.Elements.Add(new PdfTextElement
        {
            Text = "|| Shree Ganeshay Namah ||",
            FontFamily = "Cinzel",
            FontSize = 13,
            IsBold = true,
            TextColorHex = "#8B0000", // Auspicious Crimson Maroon
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 190,
            Width = 500,
            Height = 22,
            ZIndex = 7
        });

        // 6. Invitation Announcement Header
        page.Elements.Add(new PdfTextElement
        {
            Text = "Together with their families, cordially invite you to celebrate the wedding of",
            FontFamily = "Cinzel",
            FontSize = 12.5,
            IsBold = false,
            TextColorHex = "#451A03", // Deep rich walnut
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 220,
            Width = 500,
            Height = 44,
            ZIndex = 7
        });

        // 7. Groom & Bride Names in Calligraphic Typography with Ampersand
        // Groom Name
        page.Elements.Add(new PdfTextElement
        {
            Text = "Aarav",
            FontFamily = "Great Vibes",
            FontSize = 42,
            IsBold = true,
            TextColorHex = "#78350F", // Royal Amber Bronze
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 270,
            Width = 220,
            Height = 52,
            ZIndex = 8
        });

        // Groom Parentage
        page.Elements.Add(new PdfTextElement
        {
            Text = "Son of\nMrs. Sunita & Mr. Rajesh Sharma\nGrandson of Late Smt. Kamala Sharma",
            FontFamily = "Montserrat",
            FontSize = 9.5,
            IsBold = true,
            TextColorHex = "#451A03",
            Alignment = TextAlignmentMode.Center,
            X = 35,
            Y = 325,
            Width = 250,
            Height = 55,
            ZIndex = 8
        });

        // Center Calligraphic Ampersand
        page.Elements.Add(new PdfTextElement
        {
            Text = "&",
            FontFamily = "Great Vibes",
            FontSize = 36,
            IsBold = true,
            TextColorHex = "#D97706", // Gold
            Alignment = TextAlignmentMode.Center,
            X = 280,
            Y = 295,
            Width = 40,
            Height = 40,
            ZIndex = 8
        });

        // Bride Name
        page.Elements.Add(new PdfTextElement
        {
            Text = "Ananya",
            FontFamily = "Great Vibes",
            FontSize = 42,
            IsBold = true,
            TextColorHex = "#78350F",
            Alignment = TextAlignmentMode.Center,
            X = 330,
            Y = 270,
            Width = 220,
            Height = 52,
            ZIndex = 8
        });

        // Bride Parentage
        page.Elements.Add(new PdfTextElement
        {
            Text = "Daughter of\nMrs. Meenakshi & Mr. Suresh Iyer\nGranddaughter of Late Shri K. Iyer",
            FontFamily = "Montserrat",
            FontSize = 9.5,
            IsBold = true,
            TextColorHex = "#451A03",
            Alignment = TextAlignmentMode.Center,
            X = 315,
            Y = 325,
            Width = 250,
            Height = 55,
            ZIndex = 8
        });

        // 8. Center Dotted Floral Lotus Divider
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "DottedFloralDivider",
            SvgSource = SvgOrnamentLibrary.GetDottedFloralDividerSvg("#D97706", "#8B0000"),
            X = 80,
            Y = 390,
            Width = 440,
            Height = 24,
            ZIndex = 8
        });

        // 9. Dual Event Schedules (MUHURTHAM & RECEPTION)
        // Left: MUHURTHAM
        page.Elements.Add(new PdfTextElement
        {
            Text = "MUHURTHAM",
            FontFamily = "Cinzel",
            FontSize = 14,
            IsBold = true,
            TextColorHex = "#8B0000",
            Alignment = TextAlignmentMode.Center,
            X = 40,
            Y = 420,
            Width = 240,
            Height = 22,
            ZIndex = 8
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "Wednesday, 11th November 2026\n9:30 AM – 11:30 AM (Dhanu Lagna)",
            FontFamily = "Montserrat",
            FontSize = 10,
            IsBold = true,
            TextColorHex = "#451A03",
            Alignment = TextAlignmentMode.Center,
            X = 40,
            Y = 445,
            Width = 240,
            Height = 36,
            ZIndex = 8
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "- Venue -\nThe Leela Palace, Grand Ballroom\nMG Road, Bengaluru, Karnataka",
            FontFamily = "Montserrat",
            FontSize = 9.5,
            IsBold = false,
            TextColorHex = "#522504",
            Alignment = TextAlignmentMode.Center,
            X = 40,
            Y = 485,
            Width = 240,
            Height = 48,
            ZIndex = 8
        });

        // Vertical Dotted Divider between Muhurtham and Reception
        page.Elements.Add(new PdfDividerElement
        {
            IsVertical = true,
            Thickness = 1.5,
            ColorHex = "#D97706",
            X = 299,
            Y = 420,
            Width = 2,
            Height = 115,
            ZIndex = 8
        });

        // Right: RECEPTION
        page.Elements.Add(new PdfTextElement
        {
            Text = "RECEPTION",
            FontFamily = "Cinzel",
            FontSize = 14,
            IsBold = true,
            TextColorHex = "#8B0000",
            Alignment = TextAlignmentMode.Center,
            X = 320,
            Y = 420,
            Width = 240,
            Height = 22,
            ZIndex = 8
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "Thursday, 12th November 2026\n7:00 PM Onwards",
            FontFamily = "Montserrat",
            FontSize = 10,
            IsBold = true,
            TextColorHex = "#451A03",
            Alignment = TextAlignmentMode.Center,
            X = 320,
            Y = 445,
            Width = 240,
            Height = 36,
            ZIndex = 8
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "- Venue -\nThe Leela Palace, Royal Convention Lawn\nMG Road, Bengaluru, Karnataka",
            FontFamily = "Montserrat",
            FontSize = 9.5,
            IsBold = false,
            TextColorHex = "#522504",
            Alignment = TextAlignmentMode.Center,
            X = 320,
            Y = 485,
            Width = 240,
            Height = 48,
            ZIndex = 8
        });

        // 10. Secondary Dotted Floral Divider
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "DottedFloralDivider",
            SvgSource = SvgOrnamentLibrary.GetDottedFloralDividerSvg("#D97706", "#8B0000"),
            X = 120,
            Y = 545,
            Width = 360,
            Height = 20,
            ZIndex = 8
        });

        // 11. RSVP / Best Compliments
        page.Elements.Add(new PdfTextElement
        {
            Text = "With Best Compliments From: Sharma & Iyer Families  •  RSVP: +91 98450 12345",
            FontFamily = "Montserrat",
            FontSize = 10.5,
            IsBold = true,
            TextColorHex = "#78350F",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 572,
            Width = 500,
            Height = 22,
            ZIndex = 8
        });

        // 12. Flanking Left & Right Ceremonial Banana Plantain Trees
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "PlantainTrees",
            SvgSource = SvgOrnamentLibrary.GetPlantainTreesSvg(),
            X = 18,
            Y = 640,
            Width = 80,
            Height = 160,
            ZIndex = 6
        });

        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "PlantainTrees",
            SvgSource = SvgOrnamentLibrary.GetPlantainTreesSvg(),
            X = 502,
            Y = 640,
            Width = 80,
            Height = 160,
            ZIndex = 6
        });

        // 13. Auspicious Brass Standing Deepam Lamps (Left & Right)
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "TraditionalDeepam",
            SvgSource = SvgOrnamentLibrary.GetTraditionalDeepamSvg(),
            X = 85,
            Y = 675,
            Width = 55,
            Height = 135,
            ZIndex = 7
        });

        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "TraditionalDeepam",
            SvgSource = SvgOrnamentLibrary.GetTraditionalDeepamSvg(),
            X = 460,
            Y = 675,
            Width = 55,
            Height = 135,
            ZIndex = 7
        });

        // 14. Center Vedic Kalash with Mango Leaves and Coconut
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "AuspiciousKalash",
            SvgSource = SvgOrnamentLibrary.GetAuspiciousKalashSvg(),
            X = 265,
            Y = 665,
            Width = 70,
            Height = 90,
            ZIndex = 7
        });

        // 15. Bottom Ornate Mandap Floor Border
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "MandapArch",
            SvgSource = SvgOrnamentLibrary.GetMandapArchSvg("#D97706", "#8B0000"),
            X = 20,
            Y = 820,
            Width = 560,
            Height = 55,
            ZIndex = 7
        });

        doc.Pages.Add(page);
        return doc;
    }
}
