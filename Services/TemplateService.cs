using System;
using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Services;

public class TemplateService : ITemplateService
{
    public PdfDocumentModel CreateAnnualReportTemplate()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Annual_Report_2026.pdf",
            Author = "ACME CORP.",
            Subject = "Fiscal Year 2026 Annual Report",
            CreatedDate = DateTime.Now
        };

        // --- PAGE 1: Executive Summary & Highlights ---
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "CONFIDENTIAL & PROPRIETARY",
            FooterRight = "Page 1 of 3",
            Elements = new List<PdfElementBase>
            {
                // Acme Logo Badge Top Right (Right aligned to 740)
                new PdfShapeElement
                {
                    X = 680,
                    Y = 50,
                    Width = 60,
                    Height = 60,
                    CornerRadius = 8,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#0C599B",
                    StrokeThickness = 0,
                    Label = "Ac",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 26
                },
                // Acme Corp Text
                new PdfTextElement
                {
                    X = 620,
                    Y = 118,
                    Width = 120,
                    Height = 24,
                    Text = "ACME CORP.",
                    FontSize = 11,
                    IsBold = true,
                    TextColorHex = "#201F1E",
                    Alignment = TextAlignmentMode.Right
                },
                // Title "ANNUAL REPORT"
                new PdfTextElement
                {
                    X = 60,
                    Y = 50,
                    Width = 540,
                    Height = 46,
                    Text = "ANNUAL REPORT",
                    FontSize = 32,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#111827",
                    Alignment = TextAlignmentMode.Left
                },
                // Subtitle "Fiscal Year 2026"
                new PdfTextElement
                {
                    X = 60,
                    Y = 100,
                    Width = 400,
                    Height = 26,
                    Text = "FISCAL YEAR 2026",
                    FontSize = 13.5,
                    IsBold = false,
                    TextColorHex = "#6B7280",
                    Alignment = TextAlignmentMode.Left
                },
                // Header accent divider line
                new PdfDividerElement
                {
                    X = 60,
                    Y = 145,
                    Width = 680,
                    Height = 3,
                    Thickness = 3,
                    ColorHex = "#0F6CBD"
                },
                // Section 1.0 Executive Summary Title
                new PdfTextElement
                {
                    X = 60,
                    Y = 175,
                    Width = 680,
                    Height = 36,
                    Text = "1.0 Executive Summary",
                    FontSize = 20,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                // Executive Summary Paragraph
                new PdfTextElement
                {
                    X = 60,
                    Y = 220,
                    Width = 680,
                    Height = 110,
                    Text = "The fiscal year 2026 has been a period of remarkable growth and strategic transformation for Acme Corp. Navigating through complex global market dynamics, we have successfully expanded our footprint in emerging sectors while consolidating our core operations. Our commitment to innovation and sustainable practices has yielded unprecedented financial results, marking this year as the most profitable in our decade-long history.",
                    FontSize = 13.5,
                    LineHeight = 1.6,
                    TextColorHex = "#1F2937",
                    Alignment = TextAlignmentMode.Justify,
                    BackgroundColorHex = "#F0F7FD",
                    BorderColorHex = "#BFDBFE",
                    BorderThickness = 1.0,
                    Padding = 12,
                    CornerRadius = 6
                },
                // Financial Highlights Section Title
                new PdfTextElement
                {
                    X = 60,
                    Y = 360,
                    Width = 330,
                    Height = 30,
                    Text = "Financial Highlights",
                    FontSize = 16,
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                // Financial Highlights Divider
                new PdfDividerElement
                {
                    X = 60,
                    Y = 395,
                    Width = 330,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#E5E7EB"
                },
                // Financial Highlights List
                new PdfTextElement
                {
                    X = 60,
                    Y = 410,
                    Width = 330,
                    Height = 160,
                    Text = "✔  Revenue increased by 24% YoY.\n\n✔  Operating margin expanded to 18.5%.\n\n✔  Successful acquisition of TechNova Inc.\n\n✔  Launched three new flagship AI products.",
                    FontSize = 12.5,
                    LineHeight = 1.5,
                    TextColorHex = "#374151"
                },
                // Visual Chart Component
                new PdfChartElement
                {
                    X = 410,
                    Y = 360,
                    Width = 330,
                    Height = 210,
                    Title = "Revenue Growth (Q1-Q4)",
                    Categories = new List<string> { "Q1", "Q2", "Q3", "Q4" },
                    Values = new List<double> { 1.2, 1.8, 2.5, 3.1 },
                    ValueLabels = new List<string> { "$1.2B", "$1.8B", "$2.5B", "$3.1B" },
                    BarColorsHex = new List<string> { "#93C5FD", "#60A5FA", "#3B82F6", "#0F6CBD" },
                    BackgroundColorHex = "#FAFAFA",
                    BorderColorHex = "#E2E8F0"
                },
                // Section 2.0 Strategic Outlook Title
                new PdfTextElement
                {
                    X = 60,
                    Y = 600,
                    Width = 680,
                    Height = 32,
                    Text = "2.0 Strategic Outlook",
                    FontSize = 16,
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                // Strategic Outlook Divider
                new PdfDividerElement
                {
                    X = 60,
                    Y = 635,
                    Width = 680,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#E5E7EB"
                },
                // Strategic Outlook Paragraph
                new PdfTextElement
                {
                    X = 60,
                    Y = 650,
                    Width = 680,
                    Height = 90,
                    Text = "Looking ahead to 2027, Acme Corp is poised to leverage artificial intelligence across our entire product suite. We anticipate significant efficiency gains and new revenue streams derived from data-centric services. Our primary objective remains delivering exceptional value to our shareholders while upholding our commitment to corporate social responsibility and environmental stewardship.",
                    FontSize = 13,
                    LineHeight = 1.6,
                    TextColorHex = "#374151",
                    Alignment = TextAlignmentMode.Justify
                }
            }
        };

        // --- PAGE 2: Operations & Product Lines ---
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "CONFIDENTIAL & PROPRIETARY",
            FooterRight = "Page 2 of 3",
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement
                {
                    X = 60,
                    Y = 50,
                    Width = 680,
                    Height = 36,
                    Text = "3.0 Global Operations & Infrastructure",
                    FontSize = 22,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfDividerElement
                {
                    X = 60,
                    Y = 95,
                    Width = 680,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 115,
                    Width = 680,
                    Height = 60,
                    Text = "Our global operations expanded across North America, Europe, and Asia-Pacific with three new state-of-the-art research centers and zero downtime recorded during the 2026 infrastructure migration.",
                    FontSize = 13,
                    TextColorHex = "#374151"
                },
                // Table of regional operational metrics
                new PdfTableElement
                {
                    X = 60,
                    Y = 190,
                    Width = 680,
                    Height = 200,
                    Headers = new List<string> { "Region", "Data Centers", "Active Clients", "Uptime SLA", "2026 Revenue" },
                    Rows = new List<List<string>>
                    {
                        new() { "North America (HQ)", "12", "4,200+", "99.995%", "$1.42 Billion" },
                        new() { "EMEA (London/Frankfurt)", "8", "2,850+", "99.992%", "$980 Million" },
                        new() { "APAC (Tokyo/Singapore)", "6", "1,920+", "99.990%", "$540 Million" },
                        new() { "Latin America (São Paulo)", "3", "650+", "99.980%", "$160 Million" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8F9FA"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 420,
                    Width = 680,
                    Height = 32,
                    Text = "4.0 Sustainability & ESG Commitments",
                    FontSize = 18,
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfDividerElement
                {
                    X = 60,
                    Y = 455,
                    Width = 680,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#E5E7EB"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 470,
                    Width = 680,
                    Height = 80,
                    Text = "Acme Corp reduced carbon emissions by 35% across all operational hubs in 2026 through 100% renewable power purchase agreements and closed-loop hardware recycling initiatives.",
                    FontSize = 13,
                    TextColorHex = "#374151"
                }
            }
        };

        // --- PAGE 3: Financial Balance Sheet & Approval ---
        var page3 = new PdfPageModel
        {
            PageNumber = 3,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "CONFIDENTIAL & PROPRIETARY",
            FooterRight = "Page 3 of 3",
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement
                {
                    X = 60,
                    Y = 50,
                    Width = 680,
                    Height = 36,
                    Text = "5.0 Consolidated Financial Summary",
                    FontSize = 22,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfDividerElement
                {
                    X = 60,
                    Y = 95,
                    Width = 680,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#0F6CBD"
                },
                new PdfTableElement
                {
                    X = 60,
                    Y = 120,
                    Width = 680,
                    Height = 240,
                    Headers = new List<string> { "Financial Metric ($ in Millions)", "FY 2024", "FY 2025", "FY 2026", "YoY Change" },
                    Rows = new List<List<string>>
                    {
                        new() { "Total Net Revenue", "$1,980.0", "$2,500.0", "$3,100.0", "+24.0%" },
                        new() { "Cost of Goods Sold (COGS)", "$820.0", "$990.0", "$1,120.0", "+13.1%" },
                        new() { "Gross Profit", "$1,160.0", "$1,510.0", "$1,980.0", "+31.1%" },
                        new() { "Research & Development", "$290.0", "$380.0", "$480.0", "+26.3%" },
                        new() { "Operating Income (EBITDA)", "$410.0", "$520.0", "$685.0", "+31.7%" },
                        new() { "Net Income After Taxes", "$315.0", "$405.0", "$540.0", "+33.3%" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 400,
                    Width = 680,
                    Height = 30,
                    Text = "Auditor & Board Approval",
                    FontSize = 16,
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfDividerElement
                {
                    X = 60,
                    Y = 435,
                    Width = 680,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#E5E7EB"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 450,
                    Width = 680,
                    Height = 60,
                    Text = "Approved unanimously by the Board of Directors on February 24, 2027. Independent audit performed by Horizon Global Assurance LLP.",
                    FontSize = 12.5,
                    TextColorHex = "#4B5563"
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);
        doc.Pages.Add(page3);

        return doc;
    }

    public PdfDocumentModel CreateInvoiceTemplate()
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

    public PdfDocumentModel CreateResumeTemplate()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Alex_Morgan_Resume.pdf",
            Author = "Alex Morgan",
            Subject = "Senior Software Architect Resume"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "Alex Morgan • Portfolio: alexmorgan.dev",
            FooterRight = "Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement
                {
                    X = 60,
                    Y = 50,
                    Width = 680,
                    Height = 40,
                    Text = "ALEX MORGAN",
                    FontSize = 26,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 92,
                    Width = 680,
                    Height = 25,
                    Text = "Principal Software Architect | Cross-Platform Systems & .NET Specialist",
                    FontSize = 13,
                    TextColorHex = "#0F6CBD",
                    IsBold = true
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 120,
                    Width = 680,
                    Height = 22,
                    Text = "📧 alex.morgan@example.com   |   📱 +1 (555) 234-5678   |   📍 Seattle, WA",
                    FontSize = 11,
                    TextColorHex = "#6B7280"
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
                new PdfTextElement
                {
                    X = 60,
                    Y = 170,
                    Width = 680,
                    Height = 28,
                    Text = "Professional Summary",
                    FontSize = 15,
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 200,
                    Width = 680,
                    Height = 65,
                    Text = "Over 12 years of experience leading the design and development of mission-critical enterprise applications, desktop suites using Avalonia and .NET, high-throughput backend services, and scalable cloud architectures.",
                    FontSize = 12,
                    LineHeight = 1.5,
                    TextColorHex = "#374151"
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }

    public PdfDocumentModel CreateAcademicPaperTemplate()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Research_Paper_2026.pdf",
            Author = "Dr. Elena Vance, Ph.D.",
            Subject = "High-Performance Cross-Platform Desktop Vector Rendering"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "JOURNAL OF MODERN COMPUTING | ISSN 2410-982X",
            FooterRight = "Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement
                {
                    X = 60,
                    Y = 50,
                    Width = 680,
                    Height = 60,
                    Text = "High-Performance Cross-Platform Vector Graphics Architecture in Modern .NET Environments",
                    FontSize = 20,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#111827",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 120,
                    Width = 680,
                    Height = 24,
                    Text = "Elena Vance, Marcus Thorne, and Sarah Jenkins — Institute for Advanced Systems",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#4B5563",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 60,
                    Y = 150,
                    Width = 680,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#9CA3AF"
                },
                new PdfTextElement
                {
                    X = 80,
                    Y = 165,
                    Width = 640,
                    Height = 70,
                    Text = "ABSTRACT: We present an optimized rendering pipeline for interactive desktop publishing software running on Avalonia and SkiaSharp. Benchmark results indicate up to a 4.2x reduction in garbage collection pressure and 60 FPS smooth multi-touch canvas manipulation.",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    TextColorHex = "#1F2937",
                    Alignment = TextAlignmentMode.Justify
                },
                new PdfDividerElement
                {
                    X = 60,
                    Y = 245,
                    Width = 680,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#9CA3AF"
                },
                // Column 1
                new PdfTextElement
                {
                    X = 60,
                    Y = 260,
                    Width = 325,
                    Height = 24,
                    Text = "1. Introduction",
                    FontSize = 13,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 290,
                    Width = 325,
                    Height = 200,
                    Text = "Desktop publishing workflows require strict color space management, sub-pixel text rasterization, and rapid spatial index queries. Traditional frameworks suffer from cross-platform discrepancies between Windows Direct2D and macOS CoreGraphics. Our modular architecture abstracts the rendering surface directly through high-performance GPU shaders.",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.4,
                    TextColorHex = "#374151",
                    Alignment = TextAlignmentMode.Justify
                },
                // Column 2
                new PdfTextElement
                {
                    X = 415,
                    Y = 260,
                    Width = 325,
                    Height = 24,
                    Text = "2. Empirical Methodology",
                    FontSize = 13,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 290,
                    Width = 325,
                    Height = 200,
                    Text = "We evaluated document compilation across 1,000 synthetic test files ranging from 10 to 500 pages. Metrics recorded included memory allocation per frame, layout recalculation latency, and export fidelity against the ISO 32000-2 PDF standard.",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.4,
                    TextColorHex = "#374151",
                    Alignment = TextAlignmentMode.Justify
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }

    public PdfDocumentModel CreateCertificateTemplate()
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

    public PdfDocumentModel CreateBlankDocument(PageFormat format = PageFormat.A4, PageOrientation orientation = PageOrientation.Portrait)
    {
        var doc = new PdfDocumentModel
        {
            Title = "Untitled_Document.pdf",
            Author = "User",
            Subject = "New PDF Document"
        };

        var width = format switch
        {
            PageFormat.A4 => 800.0,
            PageFormat.Letter => 800.0,
            PageFormat.Legal => 800.0,
            _ => 800.0
        };

        var height = format switch
        {
            PageFormat.A4 => 1131.0,
            PageFormat.Letter => 1035.0,
            PageFormat.Legal => 1318.0,
            _ => 1131.0
        };

        if (orientation == PageOrientation.Landscape)
        {
            (width, height) = (height, width);
        }

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = format,
            Orientation = orientation,
            Width = width,
            Height = height,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "CONFIDENTIAL",
            FooterRight = "Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement
                {
                    X = 60,
                    Y = 60,
                    Width = 680,
                    Height = 40,
                    Text = "Click to Edit Heading",
                    FontSize = 24,
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 110,
                    Width = 680,
                    Height = 60,
                    Text = "Start typing your content here, or use the Ribbon toolbar above to insert text blocks, images, tables, shapes, and watermarks.",
                    FontSize = 13,
                    TextColorHex = "#4B5563"
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
