using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class CertificateTemplate : ITemplateDefinition
{
    public string Id => "certificate";
    public string Name => "Certificate of Achievement";
    public string Description => "Award of excellence and official recognition credential";
    public string Category => "Certificates";
    public string IconKind => "CertificateOutline";
    public string AccentColorHex => "#2563EB";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Certificate_of_Achievement.pdf",
            Author = "Global Technology Academy",
            Subject = "Professional Certificate of Excellence"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Landscape,
            Width = 1131,
            Height = 800,
            BackgroundColorHex = "#FCFDFE",
            FooterLeft = "OFFICIAL CREDENTIAL VERIFICATION ID: GTA-2026-94812",
            FooterRight = "GLOBAL TECHNOLOGY ACADEMY",
            Elements = new List<PdfElementBase>
            {
                // Outer Ornamental Border
                new PdfShapeElement
                {
                    X = 40,
                    Y = 40,
                    Width = 1051,
                    Height = 720,
                    FillColorHex = "#00000000",
                    StrokeColorHex = "#0F6CBD",
                    StrokeThickness = 3,
                    CornerRadius = 12
                },
                // Inner Gold Accent Border
                new PdfShapeElement
                {
                    X = 48,
                    Y = 48,
                    Width = 1035,
                    Height = 704,
                    FillColorHex = "#00000000",
                    StrokeColorHex = "#F59E0B",
                    StrokeThickness = 1,
                    CornerRadius = 8
                },
                // Title
                new PdfTextElement
                {
                    X = 100,
                    Y = 100,
                    Width = 931,
                    Height = 50,
                    Text = "CERTIFICATE OF ACHIEVEMENT",
                    FontSize = 34,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F6CBD",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfTextElement
                {
                    X = 100,
                    Y = 160,
                    Width = 931,
                    Height = 24,
                    Text = "THIS RECOGNITION IS PROUDLY PRESENTED TO",
                    FontSize = 12,
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center
                },
                // Recipient Name
                new PdfTextElement
                {
                    X = 100,
                    Y = 220,
                    Width = 931,
                    Height = 50,
                    Text = "ALEXANDER R. MERCER",
                    FontSize = 32,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 300,
                    Y = 280,
                    Width = 531,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#F59E0B"
                },
                // Description Text
                new PdfTextElement
                {
                    X = 150,
                    Y = 310,
                    Width = 831,
                    Height = 60,
                    Text = "For outstanding dedication, master-level technical proficiency, and exemplary contribution in the completion of the Advanced Systems Architecture & Engineering Masterclass.",
                    FontSize = 14,
                    LineHeight = 1.6,
                    TextColorHex = "#475569",
                    Alignment = TextAlignmentMode.Center
                },
                // Gold Seal Badge
                new PdfShapeElement
                {
                    X = 525,
                    Y = 420,
                    Width = 80,
                    Height = 80,
                    CornerRadius = 40,
                    FillColorHex = "#FEF3C7",
                    StrokeColorHex = "#F59E0B",
                    StrokeThickness = 2,
                    Label = "SEAL",
                    LabelColorHex = "#B45309",
                    LabelFontSize = 14
                },
                // Date & Signature
                new PdfTextElement
                {
                    X = 150,
                    Y = 560,
                    Width = 250,
                    Height = 24,
                    Text = "February 28, 2026",
                    FontSize = 13,
                    IsBold = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 150,
                    Y = 590,
                    Width = 250,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#94A3B8"
                },
                new PdfTextElement
                {
                    X = 150,
                    Y = 598,
                    Width = 250,
                    Height = 20,
                    Text = "Date of Issuance",
                    FontSize = 11,
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfTextElement
                {
                    X = 731,
                    Y = 560,
                    Width = 250,
                    Height = 24,
                    Text = "Arthur Pendelton, Dean",
                    FontSize = 13,
                    IsBold = true,
                    TextColorHex = "#1E293B",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 731,
                    Y = 590,
                    Width = 250,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#94A3B8"
                },
                new PdfTextElement
                {
                    X = 731,
                    Y = 598,
                    Width = 250,
                    Height = 20,
                    Text = "Authorized Signature",
                    FontSize = 11,
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
