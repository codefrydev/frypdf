using System;
using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class AnnualReportTemplate : ITemplateDefinition
{
    public string Id => "annualreport";
    public string Name => "Annual Report";
    public string Description => "Executive summary, financial metrics, and chart";
    public string Category => "Corporate";
    public string IconKind => "ChartBoxOutline";
    public string AccentColorHex => "#0F6CBD";

    public PdfDocumentModel Create()
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
}
