using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class InvoiceTemplate : ITemplateDefinition
{
    public string Id => "invoice";
    public string Name => "Commercial Enterprise Invoice";
    public string Description => "Full-page itemized billing invoice with bank wire details, tax breakdown, and instant payment QR code";
    public string Category => "Finance";
    public string IconKind => "ReceiptTextOutline";
    public string AccentColorHex => "#0F6CBD";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Invoice_INV-2026-8492.pdf",
            Author = "Apex Digital Solutions LLC",
            Subject = "Enterprise Software & Cloud Engineering Services Invoice"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "Apex Digital Solutions LLC • Tax ID: US-EIN 94-3829104 • support@apexdigital.io",
            FooterCenter = "Payment Terms: Net 30 Days",
            FooterRight = "Invoice #INV-2026-8492 • Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                // Top Brand Accent Header Bar
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 800,
                    Height = 8,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                // 1. Company Logo Badge
                new PdfShapeElement
                {
                    X = 55,
                    Y = 38,
                    Width = 52,
                    Height = 52,
                    CornerRadius = 10,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#0C599B",
                    StrokeThickness = 0,
                    Label = "AP",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 20
                },

                // 2. Seller Company Name & Address
                new PdfTextElement
                {
                    X = 118,
                    Y = 38,
                    Width = 320,
                    Height = 26,
                    Text = "APEX DIGITAL SOLUTIONS LLC",
                    FontSize = 15,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 118,
                    Y = 64,
                    Width = 320,
                    Height = 58,
                    Text = "742 Montgomery St, Suite 1200 • Financial District\nSan Francisco, CA 94111 • United States\nPhone: +1 (415) 890-2300 • billing@apexdigital.io",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#64748B"
                },

                // 3. Right Invoice Title & Metadata Card
                new PdfTextElement
                {
                    X = 490,
                    Y = 36,
                    Width = 255,
                    Height = 32,
                    Text = "COMMERCIAL INVOICE",
                    FontSize = 18,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD",
                    Alignment = TextAlignmentMode.Right
                },
                new PdfTextElement
                {
                    X = 470,
                    Y = 68,
                    Width = 275,
                    Height = 68,
                    Text = "Invoice Number:   INV-2026-8492\nIssue Date:   August 29, 2026\nPayment Due:   September 28, 2026\nPO Reference:   PO-ENTERPRISE-9841",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Right
                },

                // Top Separator Line
                new PdfDividerElement
                {
                    X = 55,
                    Y = 138,
                    Width = 690,
                    Height = 2,
                    Thickness = 1.5,
                    ColorHex = "#E2E8F0"
                },

                // 4. Client Billing & Project Information
                // Left: Bill To
                new PdfTextElement
                {
                    X = 55,
                    Y = 146,
                    Width = 320,
                    Height = 20,
                    Text = "BILLED TO (CLIENT):",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 166,
                    Width = 320,
                    Height = 72,
                    Text = "Acme Global Enterprise Inc.\nAttn: Accounts Payable & Financial Operations\n500 Technology Square, Floor 8\nCambridge, MA 02139 • United States\nClient Tax ID: US-EIN 04-2918471",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B"
                },

                // Right: Project / Contract Details
                new PdfTextElement
                {
                    X = 440,
                    Y = 146,
                    Width = 305,
                    Height = 20,
                    Text = "CONTRACT & ENGAGEMENT:",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 440,
                    Y = 166,
                    Width = 305,
                    Height = 72,
                    Text = "Project: Cloud Architecture & High-Performance Publishing Suite\nEngagement Period: July 01, 2026 – August 28, 2026\nPayment Terms: Net 30 Days (Direct Wire / ACH)\nCurrency: USD ($)",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B"
                },

                // 5. Itemized Billing Table
                new PdfTableElement
                {
                    X = 55,
                    Y = 246,
                    Width = 690,
                    Height = 310,
                    Headers = new List<string> { "Deliverable / Description", "Qty / Unit", "Rate (USD)", "Discount", "Line Total" },
                    Rows = new List<List<string>>
                    {
                        new() { "Enterprise UI/UX Design System & Material Tokens", "60 Hours", "$150.00", "$0.00", "$9,000.00" },
                        new() { "Avalonia Multiplatform Desktop Architecture & Skia Engine", "140 Hours", "$160.00", "$500.00", "$21,900.00" },
                        new() { "High-Fidelity QuestPDF Vector Document Pipeline", "45 Hours", "$160.00", "$0.00", "$7,200.00" },
                        new() { "Cloud Synchronization & Scalable Microservices (AKS)", "35 Hours", "$150.00", "$0.00", "$5,250.00" },
                        new() { "SOC2 Security Hardening, Audit & Automated CI/CD Setup", "25 Hours", "$170.00", "$250.00", "$4,000.00" },
                        new() { "Performance Profiling & Memory Leak Optimization (60 FPS)", "15 Hours", "$150.00", "$0.00", "$2,250.00" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#E2E8F0"
                },

                // 6. Financial Summary Box (Right Side)
                new PdfShapeElement
                {
                    X = 430,
                    Y = 570,
                    Width = 315,
                    Height = 160,
                    CornerRadius = 8,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 445,
                    Y = 582,
                    Width = 150,
                    Height = 135,
                    Text = "Subtotal (Gross):\nPrompt Payment Discount (3%):\nNet Taxable Amount:\nState & Local Tax (8.5%):\n\nTOTAL BALANCE DUE:",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.6,
                    TextColorHex = "#475569"
                },
                new PdfTextElement
                {
                    X = 595,
                    Y = 582,
                    Width = 135,
                    Height = 135,
                    Text = "$49,600.00\n-$1,488.00\n$48,112.00\n$4,089.52\n\n$52,201.52",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    LineHeight = 1.6,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Right
                },

                // 7. Payment Instructions Box (Left Side)
                new PdfShapeElement
                {
                    X = 55,
                    Y = 570,
                    Width = 360,
                    Height = 160,
                    CornerRadius = 8,
                    FillColorHex = "#F0F7FF",
                    StrokeColorHex = "#BFDBFE",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 70,
                    Y = 582,
                    Width = 330,
                    Height = 20,
                    Text = "WIRE / ACH PAYMENT INSTRUCTIONS",
                    FontSize = 10.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 70,
                    Y = 604,
                    Width = 330,
                    Height = 115,
                    Text = "Bank Name: Silicon Valley Commercial Bank, N.A.\nAccount Holder: Apex Digital Solutions LLC\nRouting / ABA (Domestic): 121000358\nAccount Number: 98402941829\nSWIFT / BIC (International): SVCBUS6SXXX\nPayment Reference: INV-2026-8492",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#1E293B"
                },

                // 8. Instant Payment QR Code & Terms
                new PdfQrCodeElement
                {
                    X = 55,
                    Y = 746,
                    Width = 110,
                    Height = 110,
                    Content = "https://pay.apexdigital.io/invoice/INV-2026-8492",
                    Label = "SCAN TO PAY INVOICE",
                    DarkColorHex = "#0F6CBD",
                    LightColorHex = "#FFFFFF"
                },

                // 9. Terms & Late Fee Policy
                new PdfTextElement
                {
                    X = 180,
                    Y = 746,
                    Width = 565,
                    Height = 110,
                    Text = "TERMS & CONDITIONS:\n1. Payment is due strictly within thirty (30) calendar days from the invoice issuance date.\n2. Overdue balances are subject to a late service fee of 1.5% per month or the maximum permitted by law.\n3. All deliverables are licensed upon full settlement of this invoice according to Master Services Agreement #MSA-2026-11.\n4. For billing inquiries, discrepancies, or receipt confirmations, contact finance@apexdigital.io.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#475569"
                },

                // Bottom Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 870,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },

                // 10. Authorized Corporate Signatory & Official Stamp
                new PdfTextElement
                {
                    X = 55,
                    Y = 885,
                    Width = 320,
                    Height = 45,
                    Text = "Marcus Aurelius Vance",
                    FontSize = 22,
                    FontFamily = "Great Vibes",
                    IsItalic = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 932,
                    Width = 280,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#94A3B8"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 938,
                    Width = 280,
                    Height = 35,
                    Text = "Authorized Corporate Signatory\nChief Financial Officer • Apex Digital Solutions LLC",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B"
                },

                // Official Verification Seal Badge (Right)
                new PdfShapeElement
                {
                    X = 580,
                    Y = 880,
                    Width = 165,
                    Height = 85,
                    CornerRadius = 8,
                    FillColorHex = "#F0FDF4",
                    StrokeColorHex = "#86EFAC",
                    StrokeThickness = 1,
                    Label = "VERIFIED INVOICE",
                    LabelColorHex = "#166534",
                    LabelFontSize = 10
                },
                new PdfTextElement
                {
                    X = 590,
                    Y = 920,
                    Width = 145,
                    Height = 38,
                    Text = "Cryptographically signed &\nverified by Apex Trust PKI",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#166534",
                    Alignment = TextAlignmentMode.Center
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
