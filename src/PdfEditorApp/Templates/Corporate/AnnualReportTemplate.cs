using System;
using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class AnnualReportTemplate : ITemplateDefinition
{
    public string Id => "annualreport";
    public string Name => "Annual Corporate Report";
    public string Description => "Comprehensive 3-page corporate report with CEO address, segment metrics, financial statements, and charts";
    public string Category => "Corporate";
    public string IconKind => "ChartBoxOutline";
    public string AccentColorHex => "#0F6CBD";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Acme_Corp_Annual_Report_2026.pdf",
            Author = "Acme Corporation Inc. (NYSE: ACME)",
            Subject = "Fiscal Year 2026 Annual Report & Consolidated Financial Statements",
            CreatedDate = DateTime.Now
        };

        // =========================================================================
        // PAGE 1: Executive Summary, KPI Cards, Revenue Chart & Strategic Outlook
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "ACME CORPORATION • NYSE: ACME • FISCAL YEAR 2026 ANNUAL REPORT",
            FooterCenter = "CONFIDENTIAL & PROPRIETARY",
            FooterRight = "Page 1 of 3",
            Elements = new List<PdfElementBase>
            {
                // Top Brand Accent Bar
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 800,
                    Height = 6,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                // Acme Logo Badge Top Right
                new PdfShapeElement
                {
                    X = 680,
                    Y = 35,
                    Width = 55,
                    Height = 55,
                    CornerRadius = 10,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#0C599B",
                    StrokeThickness = 0,
                    Label = "AC",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 24
                },
                new PdfTextElement
                {
                    X = 600,
                    Y = 95,
                    Width = 135,
                    Height = 24,
                    Text = "ACME CORP. (NYSE: ACME)",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Right
                },

                // Title "ANNUAL REPORT"
                new PdfTextElement
                {
                    X = 55,
                    Y = 35,
                    Width = 530,
                    Height = 40,
                    Text = "ANNUAL CORPORATE REPORT",
                    FontSize = 26,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Left
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 78,
                    Width = 530,
                    Height = 22,
                    Text = "FISCAL YEAR ENDED DECEMBER 31, 2026 • GLOBAL ENTERPRISE SOLUTIONS",
                    FontSize = 10.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD",
                    Alignment = TextAlignmentMode.Left
                },

                // Header Divider Line
                new PdfDividerElement
                {
                    X = 55,
                    Y = 122,
                    Width = 690,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#0F6CBD"
                },

                // 1.0 Letter from the Chief Executive Officer
                new PdfTextElement
                {
                    X = 55,
                    Y = 132,
                    Width = 690,
                    Height = 22,
                    Text = "1.0 Executive Letter to Shareholders & Partners",
                    FontSize = 14,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 158,
                    Width = 690,
                    Height = 105,
                    Text = "Fiscal year 2026 has been a transformative period of record performance, disciplined capital allocation, and technological breakthroughs for Acme Corporation. In an evolving global macro environment, our cloud-native enterprise architecture, automated AI publishing engines, and mission-critical cybersecurity suites generated record top-line revenue of $3.10 Billion (+24% YoY) and net income of $540 Million (+33% YoY). We expanded gross margins to 63.8% and generated $412 Million in free cash flow.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.45,
                    TextColorHex = "#1E293B",
                    BackgroundColorHex = "#F8FAFC",
                    BorderColorHex = "#E2E8F0",
                    BorderThickness = 1.0,
                    Padding = 10,
                    CornerRadius = 6,
                    Alignment = TextAlignmentMode.Justify
                },

                // KPI 4-Card Summary Grid
                new PdfShapeElement
                {
                    X = 55,
                    Y = 274,
                    Width = 160,
                    Height = 68,
                    CornerRadius = 6,
                    FillColorHex = "#F0F7FF",
                    StrokeColorHex = "#BFDBFE",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 65,
                    Y = 282,
                    Width = 140,
                    Height = 52,
                    Text = "$3.10 BILLION\nNet Revenue (+24% YoY)",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD",
                    Alignment = TextAlignmentMode.Center
                },

                new PdfShapeElement
                {
                    X = 232,
                    Y = 274,
                    Width = 160,
                    Height = 68,
                    CornerRadius = 6,
                    FillColorHex = "#F0FDF4",
                    StrokeColorHex = "#BBF7D0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 242,
                    Y = 282,
                    Width = 140,
                    Height = 52,
                    Text = "$685 MILLION\nOperating EBITDA (+31.7%)",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#166534",
                    Alignment = TextAlignmentMode.Center
                },

                new PdfShapeElement
                {
                    X = 408,
                    Y = 274,
                    Width = 160,
                    Height = 68,
                    CornerRadius = 6,
                    FillColorHex = "#FAF5FF",
                    StrokeColorHex = "#E9D5FF",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 418,
                    Y = 282,
                    Width = 140,
                    Height = 52,
                    Text = "$412 MILLION\nFree Cash Flow (+28.4%)",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#7E22CE",
                    Alignment = TextAlignmentMode.Center
                },

                new PdfShapeElement
                {
                    X = 585,
                    Y = 274,
                    Width = 160,
                    Height = 68,
                    CornerRadius = 6,
                    FillColorHex = "#FFFBEB",
                    StrokeColorHex = "#FDE68A",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 595,
                    Y = 282,
                    Width = 140,
                    Height = 52,
                    Text = "99.995% SLA\nGlobal Cloud Reliability",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#B45309",
                    Alignment = TextAlignmentMode.Center
                },

                // Section 2: Highlights Left & Visual Chart Right
                new PdfTextElement
                {
                    X = 55,
                    Y = 356,
                    Width = 330,
                    Height = 20,
                    Text = "Strategic & Operational Milestones",
                    FontSize = 13,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 378,
                    Width = 330,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 386,
                    Width = 330,
                    Height = 175,
                    Text = "✔  Acquisition of TechNova AI: Seamlessly integrated real-time vector document intelligence into all flagship offerings.\n✔  Enterprise Customer Expansion: Surpassed 8,420+ global enterprise accounts with net revenue retention (NRR) of 128%.\n✔  Multiplatform Engine Upgrade: Deployed Avalonia UI & SkiaSharp cross-platform architecture across 2.5M active daily seats.\n✔  Patent Portfolio: Granted 14 new international patents in distributed document synchronization and zero-knowledge encryption.",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Revenue Growth Chart (Q1-Q4)
                new PdfChartElement
                {
                    X = 405,
                    Y = 356,
                    Width = 340,
                    Height = 205,
                    Title = "FY 2026 Quarterly Revenue Trajectory ($ Billions)",
                    Categories = new List<string> { "Q1 2026", "Q2 2026", "Q3 2026", "Q4 2026" },
                    Values = new List<double> { 0.65, 0.74, 0.82, 0.89 },
                    ValueLabels = new List<string> { "$0.65B", "$0.74B", "$0.82B", "$0.89B" },
                    BarColorsHex = new List<string> { "#93C5FD", "#60A5FA", "#3B82F6", "#0F6CBD" },
                    BackgroundColorHex = "#FAFAFA",
                    BorderColorHex = "#E2E8F0"
                },

                // Section 3: Strategic Outlook & Long-Term Value Creation
                new PdfTextElement
                {
                    X = 55,
                    Y = 575,
                    Width = 690,
                    Height = 20,
                    Text = "2.0 Strategic Outlook & 2027 Growth Priorities",
                    FontSize = 13,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 597,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 605,
                    Width = 330,
                    Height = 180,
                    Text = "HYBRID CLOUD & EDGE SCALING\nWe are expanding our Kubernetes multi-region fabric to 14 additional Tier-4 colocation hubs across Europe and Asia-Pacific. This will decrease latency for global collaborative document editing to under 15 milliseconds worldwide.\n\nAI AUTOMATION & WORKFLOWS\nOur R&D pipeline will commercialize zero-shot tabular extraction and autonomous report generation, unlocking substantial productivity for Fortune 500 financial institutions.",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },
                new PdfTextElement
                {
                    X = 405,
                    Y = 605,
                    Width = 340,
                    Height = 180,
                    Text = "SECURITY & ENTERPRISE COMPLIANCE\nMaintaining SOC2 Type II, ISO 27001, and HIPAA compliance remains central to our governance model. We have allocated $65M toward continuous zero-trust authorization audits.\n\nCAPITAL ALLOCATION & BUYBACKS\nThe Board authorized a $250M share repurchase program for FY 2027, underscoring confidence in sustainable compounding cash generation and shareholder returns.",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section 4: Risk Management & Governance Framework
                new PdfShapeElement
                {
                    X = 55,
                    Y = 795,
                    Width = 690,
                    Height = 120,
                    CornerRadius = 6,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 70,
                    Y = 805,
                    Width = 660,
                    Height = 18,
                    Text = "ENTERPRISE RISK MANAGEMENT & COMPLIANCE SUMMARY",
                    FontSize = 10.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 70,
                    Y = 828,
                    Width = 660,
                    Height = 78,
                    Text = "Our Enterprise Risk Management (ERM) committee regularly audits cybersecurity protocols, foreign exchange volatility exposures, and supply chain redundancies. Hedging instruments protect over 85% of foreign-currency cash receivables, and multi-cloud failovers ensure zero single-point-of-failure risks for mission-critical client services.",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#475569"
                },

                // Sign-off Strip
                new PdfTextElement
                {
                    X = 55,
                    Y = 930,
                    Width = 330,
                    Height = 50,
                    Text = "Jonathan R. Vance\nChief Executive Officer & Chair of the Board",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 930,
                    Width = 330,
                    Height = 50,
                    Text = "Marcus Aurelius Thorne\nChief Financial Officer & Executive VP of Operations",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A",
                    Alignment = TextAlignmentMode.Right
                }
            }
        };

        // =========================================================================
        // PAGE 2: Operations, Regional Segments, Business Lines & Sustainability
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "ACME CORPORATION • NYSE: ACME • GLOBAL OPERATIONS & ESG",
            FooterCenter = "CONFIDENTIAL & PROPRIETARY",
            FooterRight = "Page 2 of 3",
            Elements = new List<PdfElementBase>
            {
                // Top Brand Accent Bar
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 800,
                    Height = 6,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 35,
                    Width = 690,
                    Height = 32,
                    Text = "3.0 Global Operations & Segment Performance",
                    FontSize = 22,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 75,
                    Width = 690,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 85,
                    Width = 690,
                    Height = 45,
                    Text = "Acme operates across 4 core geographical markets supporting over 8,420 enterprise clients in 68 countries. High-availability cloud clustering and automated edge nodes delivered 99.995% uptime across all commercial SLA agreements throughout 2026.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Table 1: Regional Operational Performance
                new PdfTableElement
                {
                    X = 55,
                    Y = 135,
                    Width = 690,
                    Height = 190,
                    Headers = new List<string> { "Operating Region", "Data Hubs", "Enterprise Clients", "Uptime SLA", "2026 Revenue", "YoY Growth" },
                    Rows = new List<List<string>>
                    {
                        new() { "North America (HQ)", "14", "4,350", "99.998%", "$1,420 Million", "+22.4%" },
                        new() { "EMEA (London / Frankfurt)", "9", "2,420", "99.995%", "$980 Million", "+26.1%" },
                        new() { "APAC (Tokyo / Singapore)", "7", "1,280", "99.992%", "$540 Million", "+28.5%" },
                        new() { "Latin America (São Paulo)", "3", "370", "99.985%", "$160 Million", "+18.5%" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#E2E8F0"
                },

                // Section 3.2: Product Segment Performance Table
                new PdfTextElement
                {
                    X = 55,
                    Y = 340,
                    Width = 690,
                    Height = 22,
                    Text = "3.1 Revenue & Contribution by Business Segment",
                    FontSize = 13,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 364,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },

                new PdfTableElement
                {
                    X = 55,
                    Y = 374,
                    Width = 690,
                    Height = 190,
                    Headers = new List<string> { "Product Line", "Revenue ($M)", "Gross Margin", "Active Seats", "Key Capabilities" },
                    Rows = new List<List<string>>
                    {
                        new() { "Enterprise Cloud Platform", "$1,450.0", "72.4%", "1,850,000", "Microservices, API Ingestion & AKS" },
                        new() { "Desktop Publishing Suite", "$920.0", "64.8%", "2,450,000", "Cross-Platform Avalonia / SkiaSharp" },
                        new() { "AI Vector Intelligence", "$510.0", "58.2%", "680,000", "LLM Document OCR & Synthesis" },
                        new() { "Professional Advisory & Sec", "$220.0", "42.0%", "N/A", "SOC2 Architecture & Custom Integrations" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#E2E8F0"
                },

                // Section 4: Environmental, Social & Governance (ESG)
                new PdfTextElement
                {
                    X = 55,
                    Y = 580,
                    Width = 690,
                    Height = 22,
                    Text = "4.0 Environmental, Social & Governance (ESG) Stewardship",
                    FontSize = 13,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 604,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#0F6CBD"
                },

                // ESG 3-Pillar Cards
                new PdfShapeElement
                {
                    X = 55,
                    Y = 614,
                    Width = 218,
                    Height = 180,
                    CornerRadius = 6,
                    FillColorHex = "#F0FDF4",
                    StrokeColorHex = "#BBF7D0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 68,
                    Y = 624,
                    Width = 192,
                    Height = 160,
                    Text = "🌿 ENVIRONMENTAL\n• 100% Renewable Energy: Sourced green energy across 100% of direct data facilities.\n• Zero Waste Hardware: 94% of retired server components recycled or refurbished.\n• Carbon Neutrality Goal: On track for net-zero Scope 1 & 2 emissions by 2028.",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#166534"
                },

                new PdfShapeElement
                {
                    X = 291,
                    Y = 614,
                    Width = 218,
                    Height = 180,
                    CornerRadius = 6,
                    FillColorHex = "#F0F7FF",
                    StrokeColorHex = "#BFDBFE",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 304,
                    Y = 624,
                    Width = 192,
                    Height = 160,
                    Text = "🤝 SOCIAL & TALENT\n• Global Workforce: 3,450 employees across 18 development centers.\n• STEM Mentorship: $12M invested in youth coding fellowships and university scholarships.\n• Retention & Culture: 91% employee retention with top-quartile Glassdoor rating.",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#0F6CBD"
                },

                new PdfShapeElement
                {
                    X = 527,
                    Y = 614,
                    Width = 218,
                    Height = 180,
                    CornerRadius = 6,
                    FillColorHex = "#FAF5FF",
                    StrokeColorHex = "#E9D5FF",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 540,
                    Y = 624,
                    Width = 192,
                    Height = 160,
                    Text = "⚖️ GOVERNANCE\n• Independent Board: 8 of 10 directors are fully independent.\n• Whistleblower Protections: 24/7 confidential reporting channel.\n• Audit & Risk: Quarterly external cybersecurity penetration testing and comprehensive controls.",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#7E22CE"
                },

                // Section 5: Board of Directors & Corporate Officers Roster
                new PdfTextElement
                {
                    X = 55,
                    Y = 810,
                    Width = 690,
                    Height = 22,
                    Text = "5.0 Executive Leadership & Board of Directors",
                    FontSize = 13,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 834,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 842,
                    Width = 330,
                    Height = 130,
                    Text = "EXECUTIVE COMMITTEE:\n• Jonathan R. Vance — Chief Executive Officer & Chair\n• Marcus Aurelius Thorne — Chief Financial Officer\n• Dr. Elena Rostova — Chief Technology Officer\n• Sarah Jenkins, J.D. — Chief Legal & Compliance Officer\n• Kevin Zhao — Chief Product Officer",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },
                new PdfTextElement
                {
                    X = 405,
                    Y = 842,
                    Width = 340,
                    Height = 130,
                    Text = "INDEPENDENT DIRECTORS:\n• Dame Victoria Sterling — Lead Independent Director\n• Robert C. Henderson — Former CFO, Microsoft Azure\n• Dr. Aris Thorne — Dean of Engineering, Stanford University\n• Amara Patel — Managing Partner, Silicon Horizon Capital\n• Henrik Lindqvist — Chair of Audit Committee",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                }
            }
        };

        // =========================================================================
        // PAGE 3: Consolidated Financials, Balance Sheet, Cash Flow & Audit Opinion
        // =========================================================================
        var page3 = new PdfPageModel
        {
            PageNumber = 3,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "ACME CORPORATION • CONSOLIDATED FINANCIAL STATEMENTS",
            FooterCenter = "CONFIDENTIAL & PROPRIETARY",
            FooterRight = "Page 3 of 3",
            Elements = new List<PdfElementBase>
            {
                // Top Brand Accent Bar
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 800,
                    Height = 6,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 35,
                    Width = 690,
                    Height = 32,
                    Text = "6.0 Consolidated Financial Statements & Audit Report",
                    FontSize = 22,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 75,
                    Width = 690,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#0F6CBD"
                },

                // Statement of Operations (P&L) Table
                new PdfTextElement
                {
                    X = 55,
                    Y = 85,
                    Width = 690,
                    Height = 20,
                    Text = "CONSOLIDATED STATEMENTS OF OPERATIONS (IN MILLIONS, EXCEPT PER SHARE DATA)",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },
                new PdfTableElement
                {
                    X = 55,
                    Y = 108,
                    Width = 690,
                    Height = 230,
                    Headers = new List<string> { "Line Item", "FY 2024", "FY 2025", "FY 2026", "YoY %" },
                    Rows = new List<List<string>>
                    {
                        new() { "Total Net Revenue", "$1,980.0", "$2,500.0", "$3,100.0", "+24.0%" },
                        new() { "Cost of Goods Sold (COGS)", "$820.0", "$990.0", "$1,120.0", "+13.1%" },
                        new() { "Gross Profit", "$1,160.0", "$1,510.0", "$1,980.0", "+31.1%" },
                        new() { "Research & Development (R&D)", "$290.0", "$380.0", "$480.0", "+26.3%" },
                        new() { "Sales, Marketing & General (SG&A)", "$460.0", "$610.0", "$815.0", "+33.6%" },
                        new() { "Operating Income (EBITDA)", "$410.0", "$520.0", "$685.0", "+31.7%" },
                        new() { "Provision for Income Taxes (21%)", "$95.0", "$115.0", "$145.0", "+26.1%" },
                        new() { "Net Income After Taxes", "$315.0", "$405.0", "$540.0", "+33.3%" },
                        new() { "Diluted Earnings Per Share (EPS)", "$1.82", "$2.34", "$3.12", "+33.3%" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#E2E8F0"
                },

                // Condensed Balance Sheet & Cash Flow Table
                new PdfTextElement
                {
                    X = 55,
                    Y = 350,
                    Width = 690,
                    Height = 20,
                    Text = "CONDENSED CONSOLIDATED BALANCE SHEET & CASH FLOW (AS OF DEC 31, 2026)",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },
                new PdfTableElement
                {
                    X = 55,
                    Y = 372,
                    Width = 690,
                    Height = 190,
                    Headers = new List<string> { "Balance Sheet Metric", "FY 2025", "FY 2026", "YoY Change ($M)", "Health Indicator" },
                    Rows = new List<List<string>>
                    {
                        new() { "Cash, Equivalents & Short-Term Marketable Securities", "$840.0", "$1,180.0", "+$340.0", "Robust Liquidity" },
                        new() { "Total Current Assets", "$1,450.0", "$1,980.0", "+$530.0", "Current Ratio: 2.4x" },
                        new() { "Property, Plant & Cloud Infrastructure Equipment", "$620.0", "$890.0", "+$270.0", "Capacity Expanded" },
                        new() { "Total Assets", "$3,280.0", "$4,420.0", "+$1,140.0", "Asset Growth +34.8%" },
                        new() { "Total Long-Term Debt & Liabilities", "$650.0", "$580.0", "-$70.0", "Debt-to-Equity: 0.18" },
                        new() { "Total Stockholders' Equity", "$2,150.0", "$3,180.0", "+$1,030.0", "Book Value / Share: $18.40" }
                    },
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#E2E8F0"
                },

                // Section 7: Independent Auditor's Report Box
                new PdfShapeElement
                {
                    X = 55,
                    Y = 575,
                    Width = 690,
                    Height = 145,
                    CornerRadius = 6,
                    FillColorHex = "#F0F7FF",
                    StrokeColorHex = "#BFDBFE",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 70,
                    Y = 585,
                    Width = 660,
                    Height = 20,
                    Text = "REPORT OF INDEPENDENT REGISTERED PUBLIC ACCOUNTING FIRM",
                    FontSize = 10.5,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 70,
                    Y = 608,
                    Width = 660,
                    Height = 102,
                    Text = "To the Shareholders and Board of Directors of Acme Corporation Inc.:\n\nOpinion on the Financial Statements: We have audited the accompanying consolidated balance sheets of Acme Corporation as of December 31, 2026 and 2025, and the related consolidated statements of operations, stockholders' equity, and cash flows. In our opinion, the consolidated financial statements present fairly, in all material respects, the financial position of Acme Corporation in conformity with U.S. GAAP.\n\nHorizon Global Assurance LLP • Certified Public Accountants • New York, NY • February 24, 2027",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B"
                },

                // Section 8: Board Signatures & Corporate Approval
                new PdfTextElement
                {
                    X = 55,
                    Y = 735,
                    Width = 690,
                    Height = 20,
                    Text = "7.0 Corporate Signatories & Board Approvals",
                    FontSize = 13,
                    FontFamily = "Georgia",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 758,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },

                // Signatory 1 (CEO)
                new PdfTextElement
                {
                    X = 55,
                    Y = 770,
                    Width = 210,
                    Height = 40,
                    Text = "Jonathan R. Vance",
                    FontSize = 20,
                    FontFamily = "Great Vibes",
                    IsItalic = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 812,
                    Width = 200,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#94A3B8"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 818,
                    Width = 200,
                    Height = 35,
                    Text = "Jonathan R. Vance\nChief Executive Officer",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#475569"
                },

                // Signatory 2 (CFO)
                new PdfTextElement
                {
                    X = 295,
                    Y = 770,
                    Width = 210,
                    Height = 40,
                    Text = "Marcus Aurelius Thorne",
                    FontSize = 20,
                    FontFamily = "Great Vibes",
                    IsItalic = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 295,
                    Y = 812,
                    Width = 200,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#94A3B8"
                },
                new PdfTextElement
                {
                    X = 295,
                    Y = 818,
                    Width = 200,
                    Height = 35,
                    Text = "Marcus Aurelius Thorne\nChief Financial Officer",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#475569"
                },

                // Signatory 3 (Lead Director)
                new PdfTextElement
                {
                    X = 535,
                    Y = 770,
                    Width = 210,
                    Height = 40,
                    Text = "Victoria Sterling",
                    FontSize = 20,
                    FontFamily = "Great Vibes",
                    IsItalic = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 535,
                    Y = 812,
                    Width = 200,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#94A3B8"
                },
                new PdfTextElement
                {
                    X = 535,
                    Y = 818,
                    Width = 200,
                    Height = 35,
                    Text = "Dame Victoria Sterling\nLead Independent Director",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#475569"
                },

                // SEC EDGAR & Investor Relations Verification Notice
                new PdfDividerElement
                {
                    X = 55,
                    Y = 868,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 878,
                    Width = 690,
                    Height = 30,
                    Text = "SEC EDGAR Form 10-K CIK #0001894218 • Stock Exchange Listing: New York Stock Exchange (NYSE: ACME)\nInvestor Relations: ir@acmecorp.com • Web: https://investors.acmecorp.com • Transfer Agent: Computershare Trust Co.",
                    FontSize = 8,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);
        doc.Pages.Add(page3);

        return doc;
    }
}
