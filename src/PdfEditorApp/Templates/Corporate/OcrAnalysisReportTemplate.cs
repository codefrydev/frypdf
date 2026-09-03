using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Corporate;

/// <summary>
/// Executive Document Intelligence & OCR Analysis Report Template.
/// Demonstrates automated optical character recognition, confidence heatmaps,
/// extracted multi-span entity streams, structured table reconstruction, and QR validation.
/// </summary>
public class OcrAnalysisReportTemplate : ITemplateDefinition
{
    public string Id => "ocranalysisreport";
    public string Name => "OCR Document Audit & Recognition Report";
    public string Description => "Automated document intelligence audit featuring OCR confidence metrics, extracted entity streams, structured table recovery, and QR verification";
    public string Category => "Corporate & Finance";
    public string IconKind => "TextBoxCheckOutline";
    public string AccentColorHex => "#059669";

    public PdfDocumentModel Create()
    {
        return GenerateDocument();
    }

    public static PdfDocumentModel GenerateDocument()
    {
        var doc = new PdfDocumentModel
        {
            Title = "OCR_Document_Audit_Report.pdf",
            Author = "FryPDF Document Intelligence Subsystem",
            Subject = "Optical Character Recognition & Structured Layout Deconstruction Audit",
            Keywords = "OCR, Document Intelligence, Extraction, Layout Analysis, Confidence, Audit"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 595.28,
            Height = 841.89,
            BackgroundColorHex = "#F8FAFC",
            ShowHeaderFooter = true,
            HeaderLeft = "FRYPDF DOCUMENT INTELLIGENCE • OCR AUDIT",
            HeaderRight = "CONFIDENCE: 99.4% (HIGH)",
            FooterLeft = "AUTOMATED OCR PIPELINE AUDIT",
            FooterCenter = "SHA-256: 8f9a2b1c4e6d3f0a",
            FooterRight = "PAGE 1 OF 1"
        };

        // ==========================================
        // 1. TOP HEADER & METRIC BANNER
        // ==========================================

        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 24,
            Width = 547.28,
            Height = 105,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 8,
            FillColorHex = "#064E3B",
            StrokeColorHex = "#065F46",
            StrokeThickness = 1.5,
            ZIndex = 0
        });

        // Top Emerald Accent Stripe
        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 24,
            Width = 547.28,
            Height = 4,
            ShapeType = ShapeType.Rectangle,
            FillColorHex = "#10B981",
            ZIndex = 1
        });

        // Category Tag
        page.Elements.Add(new PdfTextElement
        {
            X = 44,
            Y = 38,
            Width = 260,
            Height = 18,
            Text = "NEURAL OCR V4.2 • LAYOUT ANALYSIS",
            FontFamily = "Segoe UI",
            FontSize = 9.0,
            IsBold = true,
            TextColorHex = "#6EE7B7",
            CharacterSpacing = 1.2,
            ZIndex = 2
        });

        // Main Report Title with Multi-Span
        var mainTitle = new PdfTextElement
        {
            X = 44,
            Y = 56,
            Width = 480,
            Height = 32,
            FontFamily = "Segoe UI",
            FontSize = 18,
            TextColorHex = "#FFFFFF",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "OCR Document Recognition ", IsBold = true, TextColorHex = "#FFFFFF" },
                new() { Text = "& Audit Report", IsBold = true, TextColorHex = "#6EE7B7" }
            },
            ZIndex = 2
        };
        mainTitle.SynchronizePlainTextFromSpans();
        page.Elements.Add(mainTitle);

        // Document Meta Subtitle
        var docMeta = new PdfTextElement
        {
            X = 44,
            Y = 90,
            Width = 500,
            Height = 22,
            FontFamily = "Segoe UI",
            FontSize = 9.5,
            TextColorHex = "#D1FAE5",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Target: " },
                new() { Text = "Commercial_Invoice_Scan_300DPI.pdf", IsBold = true, TextColorHex = "#FFFFFF" },
                new() { Text = " • Engine Accuracy: " },
                new() { Text = "99.4% (Pass)", IsBold = true, TextColorHex = "#6EE7B7" },
                new() { Text = " • Status: " },
                new() { Text = "Verified", IsBold = true, IsUnderline = true, TextColorHex = "#34D399" }
            },
            ZIndex = 2
        };
        docMeta.SynchronizePlainTextFromSpans();
        page.Elements.Add(docMeta);

        // ==========================================
        // 2. OCR STATS ROW (3 CARDS)
        // ==========================================

        // Card 1: High Confidence
        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 140,
            Width = 175,
            Height = 65,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        page.Elements.Add(new PdfTextElement
        {
            X = 34,
            Y = 148,
            Width = 155,
            Height = 48,
            FontFamily = "Segoe UI",
            FontSize = 8.5,
            TextColorHex = "#64748B",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "1,215 Words (97.4%)\n", IsBold = true, FontSize = 12.0, TextColorHex = "#059669" },
                new() { Text = "High Confidence (≥ 98%)" }
            },
            ZIndex = 2
        });

        // Card 2: Review Threshold
        page.Elements.Add(new PdfShapeElement
        {
            X = 210,
            Y = 140,
            Width = 175,
            Height = 65,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        page.Elements.Add(new PdfTextElement
        {
            X = 220,
            Y = 148,
            Width = 155,
            Height = 48,
            FontFamily = "Segoe UI",
            FontSize = 8.5,
            TextColorHex = "#64748B",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "33 Words (2.6%)\n", IsBold = true, FontSize = 12.0, TextColorHex = "#D97706" },
                new() { Text = "Low Contrast / Script Review" }
            },
            ZIndex = 2
        });

        // Card 3: Deconstructed Blocks
        page.Elements.Add(new PdfShapeElement
        {
            X = 396,
            Y = 140,
            Width = 175,
            Height = 65,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        page.Elements.Add(new PdfTextElement
        {
            X = 406,
            Y = 148,
            Width = 155,
            Height = 48,
            FontFamily = "Segoe UI",
            FontSize = 8.5,
            TextColorHex = "#64748B",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "18 Structured Blocks\n", IsBold = true, FontSize = 12.0, TextColorHex = "#0F6CBD" },
                new() { Text = "1 Table • 1 QR • 16 Texts" }
            },
            ZIndex = 2
        });

        // ==========================================
        // 3. SECTION A: EXTRACTED ENTITY STREAMS (MULTI-SPAN)
        // ==========================================

        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 216,
            Width = 547.28,
            Height = 145,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 216,
            Width = 4,
            Height = 145,
            ShapeType = ShapeType.Rectangle,
            FillColorHex = "#059669",
            ZIndex = 1
        });

        var entityHeader = new PdfTextElement
        {
            X = 40,
            Y = 228,
            Width = 450,
            Height = 20,
            FontFamily = "Segoe UI",
            FontSize = 11.5,
            TextColorHex = "#0F172A",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "1. Recognized Key-Value Entities & ", IsBold = true },
                new() { Text = "Confidence Annotations", IsBold = true, TextColorHex = "#059669" }
            },
            ZIndex = 2
        };
        entityHeader.SynchronizePlainTextFromSpans();
        page.Elements.Add(entityHeader);

        var entityStreamCol1 = new PdfTextElement
        {
            X = 40,
            Y = 252,
            Width = 250,
            Height = 98,
            FontFamily = "Segoe UI",
            FontSize = 9.0,
            LineHeight = 1.45,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "• Vendor / Merchant: " },
                new() { Text = "Acme Technologies Global Ltd.", IsBold = true, TextColorHex = "#0F172A" },
                new() { Text = " [99.8%]\n• Invoice Number: " },
                new() { Text = "INV-2026-8842", IsBold = true, TextColorHex = "#0F6CBD" },
                new() { Text = " [99.9%]\n• Issue Date: " },
                new() { Text = "2026-08-31", IsBold = true },
                new() { Text = " • Due Date: " },
                new() { Text = "2026-09-30", IsBold = true, TextColorHex = "#DC2626" },
                new() { Text = "\n• Tax Registration / VAT: " },
                new() { Text = "GB-984210547", IsBold = true, TextColorHex = "#059669" }
            },
            ZIndex = 2
        };
        entityStreamCol1.SynchronizePlainTextFromSpans();
        page.Elements.Add(entityStreamCol1);

        var entityStreamCol2 = new PdfTextElement
        {
            X = 305,
            Y = 252,
            Width = 255,
            Height = 98,
            FontFamily = "Segoe UI",
            FontSize = 9.0,
            LineHeight = 1.45,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "• Subtotal (Net): " },
                new() { Text = "$14,500.00 USD", IsBold = true },
                new() { Text = " [99.9%]\n• Tax Rate (VAT 20%): " },
                new() { Text = "$2,900.00 USD", IsBold = true },
                new() { Text = " [99.7%]\n• Total Payable: " },
                new() { Text = "$17,400.00 USD", IsBold = true, FontSize = 10.0, TextColorHex = "#059669" },
                new() { Text = "\n• Payment Method: " },
                new() { Text = "SWIFT / Direct Deposit (Verified)", IsBold = true, IsUnderline = true }
            },
            ZIndex = 2
        };
        entityStreamCol2.SynchronizePlainTextFromSpans();
        page.Elements.Add(entityStreamCol2);

        // ==========================================
        // 4. SECTION B: RECONSTRUCTED STRUCTURED TABLE
        // ==========================================

        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 372,
            Width = 547.28,
            Height = 185,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        var tableHeader = new PdfTextElement
        {
            X = 40,
            Y = 384,
            Width = 450,
            Height = 20,
            FontFamily = "Segoe UI",
            FontSize = 11.5,
            TextColorHex = "#0F172A",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "2. Reconstructed Line Items & ", IsBold = true },
                new() { Text = "Tabular Data Grid", IsBold = true, TextColorHex = "#0F6CBD" }
            },
            ZIndex = 2
        };
        tableHeader.SynchronizePlainTextFromSpans();
        page.Elements.Add(tableHeader);

        // Table Element
        var ocrTable = new PdfTableElement
        {
            X = 40,
            Y = 410,
            Width = 515,
            Height = 130,
            Headers = new List<string> { "SKU / Code", "Description", "Qty", "Unit Price", "Total (USD)", "OCR Match" },
            Rows = new List<List<string>>
            {
                new() { "CLD-SRV-01", "Enterprise Cloud Instance (48 vCPU, 192GB RAM)", "2", "$4,200.00", "$8,400.00", "99.8%" },
                new() { "AI-API-INF", "Neural OCR Document Intelligence API Tier", "1", "$3,500.00", "$3,500.00", "99.9%" },
                new() { "STG-NVME-04", "Dedicated NVMe High-IOPS Document Storage (4TB)", "4", "$650.00", "$2,600.00", "99.5%" },
                new() { "SUP-24X7-PRM", "24/7 Dedicated SLA Technical Support", "1", "$0.00", "$0.00", "100.0%" }
            },
            HeaderBackgroundHex = "#0F172A",
            HeaderTextHex = "#FFFFFF",
            AlternateRowBackgroundHex = "#F8FAFC",
            BorderColorHex = "#CBD5E1",
            ZIndex = 2
        };
        page.Elements.Add(ocrTable);

        // ==========================================
        // 5. SECTION C: SECURITY, QR VALIDATION & AUDIT SIGNATURE
        // ==========================================

        page.Elements.Add(new PdfShapeElement
        {
            X = 24,
            Y = 568,
            Width = 350,
            Height = 170,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        var secHeader = new PdfTextElement
        {
            X = 38,
            Y = 580,
            Width = 320,
            Height = 20,
            FontFamily = "Segoe UI",
            FontSize = 11.0,
            TextColorHex = "#0F172A",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "3. Security & ", IsBold = true },
                new() { Text = "Compliance Validation", IsBold = true, TextColorHex = "#7C3AED" }
            },
            ZIndex = 2
        };
        secHeader.SynchronizePlainTextFromSpans();
        page.Elements.Add(secHeader);

        var secBody = new PdfTextElement
        {
            X = 38,
            Y = 604,
            Width = 320,
            Height = 120,
            FontFamily = "Segoe UI",
            FontSize = 9.0,
            LineHeight = 1.45,
            TextColorHex = "#334155",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "• Digital Signature: " },
                new() { Text = "VALID (RSA-4096 / SHA-256)", IsBold = true, TextColorHex = "#059669" },
                new() { Text = "\n• Signer: " },
                new() { Text = "Acme Corp Authorized Officer", IsItalic = true },
                new() { Text = "\n• Timestamp: " },
                new() { Text = "2026-08-31T22:45:10 UTC (RFC 3161)" },
                new() { Text = "\n• PII Redaction Audit: " },
                new() { Text = "Zero Leaks Detected (Clean)", IsBold = true, TextColorHex = "#059669" },
                new() { Text = "\n• Forensic Verification: " },
                new() { Text = "Pass (Unaltered PDF Bitstream)", IsBold = true, IsUnderline = true }
            },
            ZIndex = 2
        };
        secBody.SynchronizePlainTextFromSpans();
        page.Elements.Add(secBody);

        // QR Code Verification Card
        page.Elements.Add(new PdfShapeElement
        {
            X = 385,
            Y = 568,
            Width = 186.28,
            Height = 170,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = "#E2E8F0",
            StrokeThickness = 1.0,
            ZIndex = 0
        });

        // QR Code Element
        page.Elements.Add(new PdfQrCodeElement
        {
            X = 428,
            Y = 582,
            Width = 100,
            Height = 100,
            Content = "https://verify.frypdf.dev/audit/inv-2026-8842?sig=8f9a2b1c",
            Label = "Scan to Verify Audit",
            DarkColorHex = "#064E3B",
            LightColorHex = "#FFFFFF",
            ZIndex = 2
        });

        page.Elements.Add(new PdfTextElement
        {
            X = 395,
            Y = 690,
            Width = 166,
            Height = 40,
            Alignment = TextAlignmentMode.Center,
            FontFamily = "Segoe UI",
            FontSize = 8.5,
            LineHeight = 1.3,
            TextColorHex = "#64748B",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Verified by " },
                new() { Text = "FryPDF OCR Engine\n", IsBold = true, TextColorHex = "#064E3B" },
                new() { Text = "Cryptographically Signed" }
            },
            ZIndex = 2
        });

        // Bottom Decorative Divider
        page.Elements.Add(new PdfDividerElement
        {
            X = 24,
            Y = 748,
            Width = 547.28,
            Height = 10,
            Style = DividerStyle.Straight,
            ColorHex = "#059669",
            Thickness = 1.5,
            ZIndex = 1
        });

        // Bottom Audit Sign-off Note
        var auditFooter = new PdfTextElement
        {
            X = 24,
            Y = 764,
            Width = 547.28,
            Height = 22,
            Alignment = TextAlignmentMode.Center,
            FontFamily = "Segoe UI",
            FontSize = 8.5,
            TextColorHex = "#64748B",
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Document Intelligence Engine • Generated by " },
                new() { Text = "FryPDF Studio", IsBold = true, TextColorHex = "#059669" },
                new() { Text = " • Automated Optical Verification Suite" }
            },
            ZIndex = 2
        };
        auditFooter.SynchronizePlainTextFromSpans();
        page.Elements.Add(auditFooter);

        doc.Pages.Add(page);
        return doc;
    }
}
