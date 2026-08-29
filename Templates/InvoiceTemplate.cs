using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class InvoiceTemplate : ITemplateDefinition
{
    public string Id => "invoice";
    public string Name => "Modern Invoice";
    public string Description => "Itemized billing table and payment terms";
    public string Category => "Finance";
    public string IconKind => "ReceiptTextOutline";
    public string AccentColorHex => "#16A34A";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Invoice_INV-2026-001.pdf",
            Author = "Design & Tech Solutions Ltd",
            Subject = "Service Invoice INV-2026-001"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "Payment Terms: Net 30 Days | Thank you for your business!",
            FooterRight = "Invoice INV-2026-001",
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement
                {
                    X = 60,
                    Y = 50,
                    Width = 350,
                    Height = 40,
                    Text = "INVOICE",
                    FontSize = 28,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 95,
                    Width = 350,
                    Height = 45,
                    Text = "Invoice #: INV-2026-001\nIssue Date: August 29, 2026\nDue Date: September 28, 2026",
                    FontSize = 11,
                    TextColorHex = "#4B5563"
                },
                new PdfTextElement
                {
                    X = 450,
                    Y = 50,
                    Width = 290,
                    Height = 85,
                    Text = "Billed To:\nAcme Enterprise Corporation\n100 Innovation Way, Suite 400\nSan Francisco, CA 94107",
                    FontSize = 11.5,
                    Alignment = TextAlignmentMode.Right,
                    TextColorHex = "#201F1E"
                },
                new PdfDividerElement
                {
                    X = 60,
                    Y = 150,
                    Width = 680,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#0F6CBD"
                },
                new PdfTableElement
                {
                    X = 60,
                    Y = 175,
                    Width = 680,
                    Height = 220,
                    Headers = new List<string> { "Description", "Quantity / Hours", "Unit Price", "Total Amount" },
                    Rows = new List<List<string>>
                    {
                        new() { "Enterprise UI/UX Design System", "40 hrs", "$150.00", "$6,000.00" },
                        new() { "Avalonia Multiplatform Implementation", "80 hrs", "$140.00", "$11,200.00" },
                        new() { "QuestPDF High-Fidelity Engine Integration", "30 hrs", "$160.00", "$4,800.00" },
                        new() { "Cloud Sync & Automated PDF Services", "20 hrs", "$150.00", "$3,000.00" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF"
                },
                new PdfTextElement
                {
                    X = 450,
                    Y = 410,
                    Width = 290,
                    Height = 100,
                    Text = "Subtotal:   $25,000.00\nTax (8.5%):   $2,125.00\n\nTotal Due:  $27,125.00",
                    FontSize = 13.5,
                    IsBold = true,
                    Alignment = TextAlignmentMode.Right,
                    TextColorHex = "#0F6CBD"
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
