using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;

namespace PdfEditorApp.Templates;

/// <summary>
/// High-end Corporate Gala &amp; Charity Award Night Invitation with Gold Art Deco styling.
/// </summary>
public class GalaInvitationTemplate : ITemplateDefinition
{
    public string Id => "galainvitation";
    public string Name => "Charity Gala & Award Night Invitation";
    public string Description => "Black-tie executive event with gold Art Deco geometric frame, schedule, and VIP RSVP QR Code";
    public string Category => "Events & Invitations";
    public string IconKind => "TicketConfirmationOutline";
    public string AccentColorHex => "#F59E0B";

    public PdfDocumentModel Create()
    {
        return GenerateDocument();
    }

    public static PdfDocumentModel GenerateDocument()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Annual_Charity_Gala_Invitation.pdf",
            Author = "CodeFryDev Foundation",
            Subject = "Annual Charity Gala & Award Night"
        };

        var page = new PdfPageModel
        {
            Width = 600,
            Height = 850,
            Orientation = PageOrientation.Portrait,
            ShowHeaderFooter = false
        };

        // 1. Sleek Midnight Obsidian Background
        page.Elements.Add(new PdfShapeElement
        {
            ShapeType = ShapeType.Rectangle,
            X = 0,
            Y = 0,
            Width = 600,
            Height = 850,
            FillColorHex = "#0B0F19", // Deep obsidian navy
            StrokeColorHex = "#D97706",
            StrokeThickness = 2.0,
            ZIndex = 0
        });

        // 2. Art Deco Gold Frame
        page.Elements.Add(new PdfSvgElement
        {
            PresetName = "ArtDecoFrame",
            SvgSource = SvgOrnamentLibrary.GetArtDecoFrameSvg("#D97706"),
            X = 20,
            Y = 20,
            Width = 560,
            Height = 810,
            ZIndex = 1
        });

        // 3. Organization Host
        page.Elements.Add(new PdfTextElement
        {
            Text = "THE CODEFRYDEV FOUNDATION CORDIALLY INVITES YOU TO THE",
            FontFamily = "Montserrat",
            FontSize = 9.5,
            IsBold = true,
            TextColorHex = "#9CA3AF",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 80,
            Width = 500,
            Height = 22,
            ZIndex = 5
        });

        // 4. Main Event Title
        page.Elements.Add(new PdfTextElement
        {
            Text = "ANNUAL CHARITY\nGALA & AWARDS",
            FontFamily = "Cinzel",
            FontSize = 32,
            IsBold = true,
            TextColorHex = "#F59E0B",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 110,
            Width = 500,
            Height = 90,
            ZIndex = 6
        });

        // 5. Year & Theme Subtitle
        page.Elements.Add(new PdfTextElement
        {
            Text = "— NIGHT OF ILLUMINATION 2026 —",
            FontFamily = "Montserrat",
            FontSize = 11,
            IsBold = true,
            TextColorHex = "#E5E7EB",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 205,
            Width = 500,
            Height = 22,
            ZIndex = 6
        });

        // 6. Gold Laurel Seal Emblem
        page.Elements.Add(new PdfShapeElement
        {
            ShapeType = ShapeType.LaurelWreathSeal,
            X = 260,
            Y = 240,
            Width = 80,
            Height = 80,
            FillColorHex = "#F59E0B",
            StrokeColorHex = "#78350F",
            StrokeThickness = 2.0,
            ZIndex = 6
        });

        // 7. Event Highlights & Keynote
        page.Elements.Add(new PdfTextElement
        {
            Text = "AN EXCLUSIVE EVENING CELEBRATING GLOBAL HUMANITARIAN LEADERSHIP,\nFEATURING LIVE PHILHARMONIC ORCHESTRA & CHARITY AUCTION",
            FontFamily = "Montserrat",
            FontSize = 10,
            IsBold = false,
            TextColorHex = "#D1D5DB",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 340,
            Width = 500,
            Height = 44,
            ZIndex = 6
        });

        // 8. Schedule & Location Card
        page.Elements.Add(new PdfShapeElement
        {
            ShapeType = ShapeType.RoundedRectangle,
            X = 60,
            Y = 400,
            Width = 480,
            Height = 180,
            FillColorHex = "#111827",
            StrokeColorHex = "#D97706",
            StrokeThickness = 1.0,
            CornerRadius = 8,
            ZIndex = 5
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "SATURDAY, NOVEMBER 14, 2026",
            FontFamily = "Cinzel",
            FontSize = 15,
            IsBold = true,
            TextColorHex = "#F59E0B",
            Alignment = TextAlignmentMode.Center,
            X = 70,
            Y = 415,
            Width = 460,
            Height = 26,
            ZIndex = 6
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "6:00 PM Champagne Reception  |  7:30 PM Three-Course Banquet & Awards\n9:30 PM Charity Auction & Live Jazz",
            FontFamily = "Montserrat",
            FontSize = 10,
            IsBold = false,
            TextColorHex = "#E5E7EB",
            Alignment = TextAlignmentMode.Center,
            X = 70,
            Y = 445,
            Width = 460,
            Height = 40,
            ZIndex = 6
        });

        page.Elements.Add(new PdfDividerElement
        {
            Thickness = 1,
            ColorHex = "#374151",
            X = 100,
            Y = 490,
            Width = 400,
            Height = 1,
            ZIndex = 6
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "THE METROPOLITAN OPERA HALL\n150 LINCOLN CENTER PLAZA, NEW YORK, NY",
            FontFamily = "Cinzel",
            FontSize = 12,
            IsBold = true,
            TextColorHex = "#F3F4F6",
            Alignment = TextAlignmentMode.Center,
            X = 70,
            Y = 505,
            Width = 460,
            Height = 40,
            ZIndex = 6
        });

        page.Elements.Add(new PdfTextElement
        {
            Text = "DRESS CODE: BLACK TIE & EVENING GOWN",
            FontFamily = "Montserrat",
            FontSize = 10,
            IsBold = true,
            TextColorHex = "#F59E0B",
            Alignment = TextAlignmentMode.Center,
            X = 70,
            Y = 548,
            Width = 460,
            Height = 22,
            ZIndex = 6
        });

        // 9. RSVP QR Code
        page.Elements.Add(new PdfQrCodeElement
        {
            Content = "https://gala.codefrydev.in/rsvp/vip",
            Label = "SCAN FOR VIP TABLE RESERVATION",
            DarkColorHex = "#0B0F19",
            LightColorHex = "#F59E0B",
            X = 240,
            Y = 610,
            Width = 120,
            Height = 140,
            ZIndex = 7
        });

        // 10. Footer Note
        page.Elements.Add(new PdfTextElement
        {
            Text = "RSVP BY OCTOBER 15, 2026  •  PROCEEDS BENEFIT GLOBAL CLEAN WATER INITIATIVE",
            FontFamily = "Montserrat",
            FontSize = 8.5,
            IsBold = true,
            TextColorHex = "#9CA3AF",
            Alignment = TextAlignmentMode.Center,
            X = 50,
            Y = 770,
            Width = 500,
            Height = 22,
            ZIndex = 6
        });

        doc.Pages.Add(page);
        return doc;
    }
}
