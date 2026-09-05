using System;
using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Corporate;

/// <summary>
/// High-impact, multi-page landscape presentation deck designed specifically for the .frypdf interactive viewer.
/// Features capabilities impossible in static binary PDFs:
/// - Interactive live data tables with real-time substring search, multi-column sorting, and CSV export
/// - Dynamic animated charts with 600ms cubic ease-out entry, hover tooltips, and live data inspector toggle
/// - Interactive form field compliance checklist and verifiable digital signature blocks
/// - Real QR codes, tracking barcodes, security redaction blocks, and executive sticky notes
/// - 16:10 / landscape presentation geometry (1131 x 800 pt) ideal for laptop and widescreen displays
/// </summary>
public class InteractiveExecutiveBriefTemplate : ITemplateDefinition
{
    public string Id => "interactive_executive_deck";
    public string Name => "Executive Strategy & Interactive Deck";
    public string Description => "Landscape presentation deck showcasing interactive living tables with CSV export, animated charts, live checklist form fields, and KPI telemetry";
    public string Category => "Corporate";
    public string IconKind => "PresentationPlay";
    public string AccentColorHex => "#6366F1";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "QuantumScale_Global_Executive_Strategic_Brief_2026.frypdf",
            Author = "QuantumScale Systems Global Corp",
            Subject = "Interactive Board Presentation Deck • Live Tables, Animated Charts, and Security Governance Ledger",
            CreatedDate = DateTime.Now
        };

        // =========================================================================
        // PAGE 1: EXECUTIVE INTELLIGENCE & TELEMETRY DASHBOARD (1131 x 800 pt)
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Landscape,
            Width = 1131,
            Height = 800,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "QUANTUMSCALE CLOUD SYSTEMS • GLOBAL STRATEGIC BRIEF",
            HeaderCenter = "CONFIDENTIAL • BOARD OF DIRECTORS PRESENTATION",
            HeaderRight = "FY 2026-2027",
            FooterLeft = "QUANTUMSCALE ENTERPRISE TELEMETRY • LIVE PRESENTATION DECK",
            FooterCenter = "CONFIDENTIAL & PROPRIETARY",
            FooterRight = "Slide 1 of 3",
            Elements = new List<PdfElementBase>
            {
                // Top Edge Accent Brand Band
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 1131,
                    Height = 6,
                    FillColorHex = "#4F46E5",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                // QuantumScale Logo Badge
                new PdfShapeElement
                {
                    X = 50,
                    Y = 28,
                    Width = 48,
                    Height = 48,
                    CornerRadius = 12,
                    FillColorHex = "#4F46E5",
                    StrokeColorHex = "#3730A3",
                    StrokeThickness = 0,
                    Label = "QS",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 20
                },

                // Header Titles
                new PdfTextElement
                {
                    X = 110,
                    Y = 30,
                    Width = 600,
                    Height = 18,
                    Text = "QUANTUMSCALE CLOUD ENTERPRISE • EXECUTIVE STRATEGIC BRIEFING",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#6366F1",
                    CharacterSpacing = 1.0
                },
                new PdfTextElement
                {
                    X = 110,
                    Y = 48,
                    Width = 600,
                    Height = 32,
                    Text = "Global Enterprise Telemetry & Growth Outlook",
                    FontSize = 22,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // Right Live Deck Pill Badge
                new PdfShapeElement
                {
                    X = 930,
                    Y = 34,
                    Width = 151,
                    Height = 34,
                    CornerRadius = 17,
                    FillColorHex = "#EEF2FF",
                    StrokeColorHex = "#C7D2FE",
                    StrokeThickness = 1,
                    Label = "● LIVE INTERACTIVE",
                    LabelColorHex = "#4F46E5",
                    LabelFontSize = 10
                },
                new PdfTextElement
                {
                    X = 850,
                    Y = 74,
                    Width = 231,
                    Height = 18,
                    Text = "Fiscal Year 2026-2027 • Board Review",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Right
                },

                // Top Accent Divider
                new PdfDividerElement
                {
                    X = 50,
                    Y = 94,
                    Width = 1031,
                    Height = 2,
                    ColorHex = "#E2E8F0"
                },

                // -----------------------------------------------------------------
                // ROW 1: FOUR HIGH-IMPACT EXECUTIVE KPI CARDS (Y = 110, Height = 90)
                // -----------------------------------------------------------------
                // Card 1: Annual Recurring Revenue
                new PdfShapeElement
                {
                    X = 50,
                    Y = 110,
                    Width = 242,
                    Height = 90,
                    CornerRadius = 12,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfShapeElement
                {
                    X = 50,
                    Y = 110,
                    Width = 4,
                    Height = 90,
                    CornerRadius = 2,
                    FillColorHex = "#4F46E5",
                    StrokeThickness = 0
                },
                new PdfTextElement
                {
                    X = 64,
                    Y = 122,
                    Width = 210,
                    Height = 18,
                    Text = "ANNUAL RECURRING REVENUE (ARR)",
                    FontSize = 8.5,
                    IsBold = true,
                    TextColorHex = "#64748B"
                },
                new PdfTextElement
                {
                    X = 64,
                    Y = 140,
                    Width = 120,
                    Height = 36,
                    Text = "$148.6M",
                    FontSize = 24,
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfShapeElement
                {
                    X = 180,
                    Y = 146,
                    Width = 98,
                    Height = 24,
                    CornerRadius = 12,
                    FillColorHex = "#ECFDF5",
                    StrokeColorHex = "#A7F3D0",
                    StrokeThickness = 1,
                    Label = "▲ +38.2% YoY",
                    LabelColorHex = "#059669",
                    LabelFontSize = 9.5
                },

                // Card 2: Net Dollar Retention (NDR)
                new PdfShapeElement
                {
                    X = 313,
                    Y = 110,
                    Width = 242,
                    Height = 90,
                    CornerRadius = 12,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfShapeElement
                {
                    X = 313,
                    Y = 110,
                    Width = 4,
                    Height = 90,
                    CornerRadius = 2,
                    FillColorHex = "#0284C7",
                    StrokeThickness = 0
                },
                new PdfTextElement
                {
                    X = 327,
                    Y = 122,
                    Width = 210,
                    Height = 18,
                    Text = "NET DOLLAR RETENTION (NDR)",
                    FontSize = 8.5,
                    IsBold = true,
                    TextColorHex = "#64748B"
                },
                new PdfTextElement
                {
                    X = 327,
                    Y = 140,
                    Width = 110,
                    Height = 36,
                    Text = "142.4%",
                    FontSize = 24,
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfShapeElement
                {
                    X = 438,
                    Y = 146,
                    Width = 104,
                    Height = 24,
                    CornerRadius = 12,
                    FillColorHex = "#F0F9FF",
                    StrokeColorHex = "#BAE6FD",
                    StrokeThickness = 1,
                    Label = "★ Top 5% Decile",
                    LabelColorHex = "#0284C7",
                    LabelFontSize = 9.5
                },

                // Card 3: Blended Gross Margin
                new PdfShapeElement
                {
                    X = 576,
                    Y = 110,
                    Width = 242,
                    Height = 90,
                    CornerRadius = 12,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfShapeElement
                {
                    X = 576,
                    Y = 110,
                    Width = 4,
                    Height = 90,
                    CornerRadius = 2,
                    FillColorHex = "#10B981",
                    StrokeThickness = 0
                },
                new PdfTextElement
                {
                    X = 590,
                    Y = 122,
                    Width = 210,
                    Height = 18,
                    Text = "BLENDED GROSS MARGIN",
                    FontSize = 8.5,
                    IsBold = true,
                    TextColorHex = "#64748B"
                },
                new PdfTextElement
                {
                    X = 590,
                    Y = 140,
                    Width = 110,
                    Height = 36,
                    Text = "79.8%",
                    FontSize = 24,
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfShapeElement
                {
                    X = 702,
                    Y = 146,
                    Width = 102,
                    Height = 24,
                    CornerRadius = 12,
                    FillColorHex = "#ECFDF5",
                    StrokeColorHex = "#A7F3D0",
                    StrokeThickness = 1,
                    Label = "+340 bps YoY",
                    LabelColorHex = "#059669",
                    LabelFontSize = 9.5
                },

                // Card 4: Active Enterprise Mesh Nodes
                new PdfShapeElement
                {
                    X = 839,
                    Y = 110,
                    Width = 242,
                    Height = 90,
                    CornerRadius = 12,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfShapeElement
                {
                    X = 839,
                    Y = 110,
                    Width = 4,
                    Height = 90,
                    CornerRadius = 2,
                    FillColorHex = "#8B5CF6",
                    StrokeThickness = 0
                },
                new PdfTextElement
                {
                    X = 853,
                    Y = 122,
                    Width = 210,
                    Height = 18,
                    Text = "ACTIVE ENTERPRISE MESH NODES",
                    FontSize = 8.5,
                    IsBold = true,
                    TextColorHex = "#64748B"
                },
                new PdfTextElement
                {
                    X = 853,
                    Y = 140,
                    Width = 110,
                    Height = 36,
                    Text = "42,850",
                    FontSize = 24,
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfShapeElement
                {
                    X = 964,
                    Y = 146,
                    Width = 104,
                    Height = 24,
                    CornerRadius = 12,
                    FillColorHex = "#F5F3FF",
                    StrokeColorHex = "#DDD6FE",
                    StrokeThickness = 1,
                    Label = "38 Cloud Zones",
                    LabelColorHex = "#7C3AED",
                    LabelFontSize = 9.5
                },

                // -----------------------------------------------------------------
                // ROW 2: LEFT COLUMN: INTERACTIVE REVENUE CHART (Y = 218, H = 512)
                // -----------------------------------------------------------------
                new PdfShapeElement
                {
                    X = 50,
                    Y = 218,
                    Width = 590,
                    Height = 512,
                    CornerRadius = 16,
                    FillColorHex = "#FFFFFF",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 72,
                    Y = 236,
                    Width = 400,
                    Height = 24,
                    Text = "Quarterly Revenue Acceleration & Runway ($M)",
                    FontSize = 14,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 72,
                    Y = 260,
                    Width = 520,
                    Height = 18,
                    Text = "Interactive chart: Hover bars for details, replay animation, or toggle data table view.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B"
                },

                // Interactive Chart Element (BarColumn)
                new PdfChartElement
                {
                    X = 72,
                    Y = 286,
                    Width = 546,
                    Height = 345,
                    ZIndex = 400,
                    Title = "Quarterly Revenue Acceleration ($M)",
                    ChartType = ChartType.BarColumn,
                    BackgroundColorHex = "#FFFFFF",
                    BorderColorHex = "#E2E8F0",
                    Categories = new List<string> { "Q1 2025", "Q2 2025", "Q3 2025", "Q4 2025", "Q1 2026", "Q2 2026" },
                    Values = new List<double> { 18.4, 24.1, 29.8, 36.5, 42.2, 48.6 },
                    ValueLabels = new List<string> { "$18.4M", "$24.1M", "$29.8M", "$36.5M", "$42.2M", "$48.6M" },
                    BarColorsHex = new List<string> { "#818CF8", "#6366F1", "#4F46E5", "#4338CA", "#3730A3", "#312E81" }
                },

                // Takeaway insight banner inside Chart card
                new PdfShapeElement
                {
                    X = 72,
                    Y = 645,
                    Width = 546,
                    Height = 65,
                    CornerRadius = 10,
                    FillColorHex = "#EEF2FF",
                    StrokeColorHex = "#C7D2FE",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 86,
                    Y = 655,
                    Width = 518,
                    Height = 48,
                    Text = "💡 Board Takeaway: The compounding trajectory demonstrates sustainable 40%+ expansion with accelerating enterprise net retention. Interactive presentation mode permits live exploration and real-time hover inspection.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#3730A3",
                    TextWrap = true
                },

                // -----------------------------------------------------------------
                // ROW 2: RIGHT COLUMN: STRATEGIC GROWTH PILLARS & STICKY NOTE
                // -----------------------------------------------------------------
                new PdfShapeElement
                {
                    X = 660,
                    Y = 218,
                    Width = 421,
                    Height = 512,
                    CornerRadius = 16,
                    FillColorHex = "#FFFFFF",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 682,
                    Y = 236,
                    Width = 380,
                    Height = 22,
                    Text = "Global ARR Contribution by Pillar",
                    FontSize = 14,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 682,
                    Y = 258,
                    Width = 380,
                    Height = 18,
                    Text = "Interactive Donut: Hover segments for share % or switch to table view.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B"
                },

                // Interactive Donut / Pie Chart Element
                new PdfChartElement
                {
                    X = 680,
                    Y = 282,
                    Width = 381,
                    Height = 225,
                    ZIndex = 400,
                    Title = "ARR by Product Pillar ($M)",
                    ChartType = ChartType.DonutPie,
                    BackgroundColorHex = "#FFFFFF",
                    BorderColorHex = "#E2E8F0",
                    Categories = new List<string> { "AI Agent Mesh", "Sovereign Enclaves", "Edge Storage", "Post-Quantum Security" },
                    Values = new List<double> { 62.4, 41.6, 26.8, 17.8 },
                    ValueLabels = new List<string> { "$62.4M", "$41.6M", "$26.8M", "$17.8M" },
                    BarColorsHex = new List<string> { "#4F46E5", "#0284C7", "#10B981", "#8B5CF6" }
                },

                // Divider inside Right Card
                new PdfDividerElement
                {
                    X = 680,
                    Y = 516,
                    Width = 381,
                    Height = 1,
                    ColorHex = "#E2E8F0"
                },

                // Executive Sticky Note Callout
                new PdfStickyNoteElement
                {
                    X = 680,
                    Y = 526,
                    Width = 381,
                    Height = 184,
                    ZIndex = 600,
                    Author = "Elena Vance, CEO & Co-Founder",
                    Timestamp = "Sep 2026",
                    NoteText = "Our transition to interactive .frypdf presentations empowers our board, stakeholders, and partners to engage directly with living data rather than static snapshots. All numbers here are verifiable directly against our cloud warehouse.",
                    Status = "Verified Executive Note"
                }
            }
        };

        // =========================================================================
        // PAGE 2: INTERACTIVE OPERATIONAL LEDGER & SEGMENT MATRIX (1131 x 800 pt)
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Landscape,
            Width = 1131,
            Height = 800,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "QUANTUMSCALE CLOUD SYSTEMS • OPERATIONAL LEDGER",
            HeaderCenter = "LIVE AUDITABLE SEGMENT BREAKDOWN",
            HeaderRight = "FY 2026-2027",
            FooterLeft = "QUANTUMSCALE ENTERPRISE TELEMETRY • LIVE PRESENTATION DECK",
            FooterCenter = "CONFIDENTIAL & PROPRIETARY",
            FooterRight = "Slide 2 of 3",
            Elements = new List<PdfElementBase>
            {
                // Top Edge Accent Brand Band
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 1131,
                    Height = 6,
                    FillColorHex = "#0284C7",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                // Header Titles
                new PdfTextElement
                {
                    X = 50,
                    Y = 30,
                    Width = 550,
                    Height = 18,
                    Text = "FINANCE & OPERATIONS • INTERACTIVE SEGMENT LEDGER",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0284C7",
                    CharacterSpacing = 1.0
                },
                new PdfTextElement
                {
                    X = 50,
                    Y = 48,
                    Width = 680,
                    Height = 32,
                    Text = "Segment Revenue, Operating Expenditure & Gross Margin Matrix",
                    FontSize = 22,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // Interactive Instruction Pill Badge
                new PdfShapeElement
                {
                    X = 760,
                    Y = 36,
                    Width = 321,
                    Height = 34,
                    CornerRadius = 17,
                    FillColorHex = "#F0F9FF",
                    StrokeColorHex = "#BAE6FD",
                    StrokeThickness = 1,
                    Label = "🔍 Live Table: Type in search or click headers to sort",
                    LabelColorHex = "#0284C7",
                    LabelFontSize = 9.5
                },

                // Top Accent Divider
                new PdfDividerElement
                {
                    X = 50,
                    Y = 94,
                    Width = 1031,
                    Height = 2,
                    ColorHex = "#E2E8F0"
                },

                // -----------------------------------------------------------------
                // MAIN LIVING DATA TABLE (Centerpiece Feature, ZIndex = 500)
                // -----------------------------------------------------------------
                new PdfTableElement
                {
                    X = 50,
                    Y = 108,
                    Width = 1031,
                    Height = 348,
                    ZIndex = 500,
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#CBD5E1",
                    Headers = new List<string>
                    {
                        "Business Unit & Segment",
                        "Primary Region",
                        "Contract Tier",
                        "FY26 ARR ($M)",
                        "Blended Margin",
                        "YoY Growth",
                        "Health Status"
                    },
                    Rows = new List<List<string>>
                    {
                        new() { "Enterprise Cloud Infrastructure", "North America", "Fortune 500", "$52.4M", "78.4%", "+42.1%", "Prime Exceeding" },
                        new() { "Autonomous AI Agent Mesh", "Global / Multi-Region", "Enterprise Core", "$34.8M", "84.1%", "+115.4%", "Hyper-Growth" },
                        new() { "Edge Compute & Telemetry Mesh", "Asia-Pacific (APAC)", "Strategic Tier", "$28.6M", "71.2%", "+38.5%", "On Track" },
                        new() { "Zero-Trust Security & Vaults", "Europe / Middle East", "Government / FinServ", "$18.2M", "81.0%", "+27.8%", "Strong Retention" },
                        new() { "Developer API & Neural Embeddings", "Global Direct", "Self-Serve & Scale", "$14.6M", "88.5%", "+64.2%", "Expanding" },
                        new() { "Hardware Security Enclaves", "North America", "Defense & Healthcare", "$9.8M", "76.3%", "+19.0%", "Stable" },
                        new() { "Global Managed Support SLA", "All Geographies", "Mission Critical", "$7.2M", "68.9%", "+14.5%", "High Satisfaction" }
                    }
                },

                // -----------------------------------------------------------------
                // BOTTOM ROW: GEOGRAPHIC ARR CHART + EFFICIENCY SCORECARD
                // -----------------------------------------------------------------
                // Left Card: Regional Share Animated Chart
                new PdfShapeElement
                {
                    X = 50,
                    Y = 472,
                    Width = 560,
                    Height = 258,
                    CornerRadius = 14,
                    FillColorHex = "#FFFFFF",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 72,
                    Y = 490,
                    Width = 400,
                    Height = 22,
                    Text = "Global Cloud Infrastructure Capacity & Node Load",
                    FontSize = 13,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfChartElement
                {
                    X = 72,
                    Y = 518,
                    Width = 516,
                    Height = 195,
                    ZIndex = 400,
                    Title = "Regional Mesh Load (% Capacity)",
                    ChartType = ChartType.HorizontalBar,
                    BackgroundColorHex = "#FFFFFF",
                    BorderColorHex = "#E2E8F0",
                    Categories = new List<string> { "Virginia US-East", "Frankfurt EU-Central", "Tokyo AP-Northeast", "Dubai ME-Central", "São Paulo SA-East" },
                    Values = new List<double> { 94.0, 88.5, 91.2, 76.4, 68.0 },
                    ValueLabels = new List<string> { "94% Load", "88% Load", "91% Load", "76% Load", "68% Load" },
                    BarColorsHex = new List<string> { "#3B82F6", "#10B981", "#8B5CF6", "#F59E0B", "#EC4899" }
                },

                // Right Card: Capital Efficiency & Rule of 40 Scorecard
                new PdfShapeElement
                {
                    X = 635,
                    Y = 472,
                    Width = 446,
                    Height = 258,
                    CornerRadius = 14,
                    FillColorHex = "#FFFFFF",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 657,
                    Y = 490,
                    Width = 400,
                    Height = 22,
                    Text = "Capital Efficiency & Rule of 40 Metrics",
                    FontSize = 13,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // Metric 1: Rule of 40 Score
                new PdfShapeElement
                {
                    X = 657,
                    Y = 525,
                    Width = 72,
                    Height = 26,
                    CornerRadius = 13,
                    FillColorHex = "#ECFDF5",
                    StrokeThickness = 0,
                    Label = "58.6%",
                    LabelColorHex = "#059669",
                    LabelFontSize = 11
                },
                new PdfTextElement
                {
                    X = 739,
                    Y = 529,
                    Width = 325,
                    Height = 20,
                    Text = "Rule of 40 Rating: +38.2% Growth + 20.4% Free Cash Flow",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // Metric 2: Payback Period
                new PdfShapeElement
                {
                    X = 657,
                    Y = 565,
                    Width = 72,
                    Height = 26,
                    CornerRadius = 13,
                    FillColorHex = "#EFF6FF",
                    StrokeThickness = 0,
                    Label = "6.4 Mo",
                    LabelColorHex = "#0284C7",
                    LabelFontSize = 11
                },
                new PdfTextElement
                {
                    X = 739,
                    Y = 569,
                    Width = 325,
                    Height = 20,
                    Text = "Customer Acquisition Payback Period (Industry avg: 14 mo)",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // Metric 3: Magic Number
                new PdfShapeElement
                {
                    X = 657,
                    Y = 605,
                    Width = 72,
                    Height = 26,
                    CornerRadius = 13,
                    FillColorHex = "#F5F3FF",
                    StrokeThickness = 0,
                    Label = "1.42x",
                    LabelColorHex = "#7C3AED",
                    LabelFontSize = 11
                },
                new PdfTextElement
                {
                    X = 739,
                    Y = 609,
                    Width = 325,
                    Height = 20,
                    Text = "Sales Efficiency Magic Number (Top decile tier > 1.0x)",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // Export Callout Box
                new PdfShapeElement
                {
                    X = 657,
                    Y = 648,
                    Width = 404,
                    Height = 65,
                    CornerRadius = 8,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 670,
                    Y = 658,
                    Width = 378,
                    Height = 48,
                    Text = "📋 Instant Export: In .frypdf interactive mode, you can copy the full financial table directly as clean CSV to Excel or Google Sheets by clicking 'Copy CSV' in the table header above.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#475569",
                    TextWrap = true
                }
            }
        };

        // =========================================================================
        // PAGE 3: PRODUCT ROADMAP, GOVERNANCE AUDIT & SIGN-OFF (1131 x 800 pt)
        // =========================================================================
        var page3 = new PdfPageModel
        {
            PageNumber = 3,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Landscape,
            Width = 1131,
            Height = 800,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "QUANTUMSCALE CLOUD SYSTEMS • GOVERNANCE & COMPLIANCE",
            HeaderCenter = "AUDITED ENTERPRISE CLEARANCE",
            HeaderRight = "FY 2026-2027",
            FooterLeft = "QUANTUMSCALE ENTERPRISE TELEMETRY • LIVE PRESENTATION DECK",
            FooterCenter = "CONFIDENTIAL & PROPRIETARY",
            FooterRight = "Slide 3 of 3",
            Elements = new List<PdfElementBase>
            {
                // Top Edge Accent Brand Band
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 1131,
                    Height = 6,
                    FillColorHex = "#059669",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                // Header Titles
                new PdfTextElement
                {
                    X = 50,
                    Y = 30,
                    Width = 600,
                    Height = 18,
                    Text = "ROADMAP & GOVERNANCE • INTERACTIVE COMPLIANCE CLEARANCE",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#059669",
                    CharacterSpacing = 1.0
                },
                new PdfTextElement
                {
                    X = 50,
                    Y = 48,
                    Width = 680,
                    Height = 32,
                    Text = "Horizon Product Milestones & Enterprise Security Audit",
                    FontSize = 22,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // Verified Pill Badge
                new PdfShapeElement
                {
                    X = 870,
                    Y = 36,
                    Width = 211,
                    Height = 34,
                    CornerRadius = 17,
                    FillColorHex = "#ECFDF5",
                    StrokeColorHex = "#A7F3D0",
                    StrokeThickness = 1,
                    Label = "🛡️ ISO-27001 / SOC-2 CERTIFIED",
                    LabelColorHex = "#059669",
                    LabelFontSize = 9.5
                },

                // Top Accent Divider
                new PdfDividerElement
                {
                    X = 50,
                    Y = 94,
                    Width = 1031,
                    Height = 2,
                    ColorHex = "#E2E8F0"
                },

                // -----------------------------------------------------------------
                // LEFT SECTION: 3-HORIZON PRODUCT ROADMAP (X = 50, Y = 108, W = 540)
                // -----------------------------------------------------------------
                new PdfShapeElement
                {
                    X = 50,
                    Y = 108,
                    Width = 540,
                    Height = 412,
                    CornerRadius = 16,
                    FillColorHex = "#FFFFFF",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 72,
                    Y = 126,
                    Width = 490,
                    Height = 22,
                    Text = "Strategic Technology Roadmap Horizons",
                    FontSize = 13,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 72,
                    Y = 148,
                    Width = 490,
                    Height = 18,
                    Text = "Planned releases and R&D capital allocation for the next 18 months",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B"
                },

                // Horizon 1: Now (Q4 2026)
                new PdfShapeElement
                {
                    X = 72,
                    Y = 176,
                    Width = 496,
                    Height = 65,
                    CornerRadius = 8,
                    FillColorHex = "#EFF6FF",
                    StrokeColorHex = "#BFDBFE",
                    StrokeThickness = 1
                },
                new PdfShapeElement
                {
                    X = 84,
                    Y = 186,
                    Width = 110,
                    Height = 22,
                    CornerRadius = 11,
                    FillColorHex = "#0284C7",
                    StrokeThickness = 0,
                    Label = "HORIZON 1 (NOW)",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 8.5
                },
                new PdfTextElement
                {
                    X = 204,
                    Y = 184,
                    Width = 350,
                    Height = 48,
                    Text = "Neural Vector Search Index v3.0 • Multi-Cluster Telemetry Streaming • Auto-Scaling Cryptographic Enclaves",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0369A1",
                    TextWrap = true
                },

                // Horizon 2: Next 6-12 Months
                new PdfShapeElement
                {
                    X = 72,
                    Y = 252,
                    Width = 496,
                    Height = 65,
                    CornerRadius = 8,
                    FillColorHex = "#F5F3FF",
                    StrokeColorHex = "#DDD6FE",
                    StrokeThickness = 1
                },
                new PdfShapeElement
                {
                    X = 84,
                    Y = 262,
                    Width = 110,
                    Height = 22,
                    CornerRadius = 11,
                    FillColorHex = "#7C3AED",
                    StrokeThickness = 0,
                    Label = "HORIZON 2 (H1 27)",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 8.5
                },
                new PdfTextElement
                {
                    X = 204,
                    Y = 260,
                    Width = 350,
                    Height = 48,
                    Text = "Zero-Latency Distributed Mesh Cache • Self-Healing Cloud Edge • Autonomous Cross-Cloud Orchestrator",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#5B21B6",
                    TextWrap = true
                },

                // Horizon 3: 12-18 Months
                new PdfShapeElement
                {
                    X = 72,
                    Y = 328,
                    Width = 496,
                    Height = 65,
                    CornerRadius = 8,
                    FillColorHex = "#ECFDF5",
                    StrokeColorHex = "#A7F3D0",
                    StrokeThickness = 1
                },
                new PdfShapeElement
                {
                    X = 84,
                    Y = 338,
                    Width = 110,
                    Height = 22,
                    CornerRadius = 11,
                    FillColorHex = "#059669",
                    StrokeThickness = 0,
                    Label = "HORIZON 3 (H2 27)",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 8.5
                },
                new PdfTextElement
                {
                    X = 204,
                    Y = 336,
                    Width = 350,
                    Height = 48,
                    Text = "Quantum-Resistant Key Exchange (NIST Post-Quantum) • Sovereign Autonomous Data Mesh Orchestration",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#047857",
                    TextWrap = true
                },

                // R&D Commitment Box
                new PdfShapeElement
                {
                    X = 72,
                    Y = 405,
                    Width = 496,
                    Height = 98,
                    CornerRadius = 8,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 86,
                    Y = 416,
                    Width = 468,
                    Height = 78,
                    Text = "🔬 R&D Investment Commitment: 22.4% of top-line revenue ($33.2M annualized) is committed to forward-looking systems research, distributed consensus algorithms, and zero-knowledge privacy protocols to maintain competitive moat.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#334155",
                    TextWrap = true
                },

                // -----------------------------------------------------------------
                // RIGHT SECTION: COMPLIANCE CHECKLIST & DIGITAL SIGNATURES (W = 466)
                // -----------------------------------------------------------------
                new PdfShapeElement
                {
                    X = 615,
                    Y = 108,
                    Width = 466,
                    Height = 412,
                    CornerRadius = 16,
                    FillColorHex = "#FFFFFF",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 637,
                    Y = 126,
                    Width = 420,
                    Height = 22,
                    Text = "Interactive Governance & Compliance Checklist",
                    FontSize = 13,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 637,
                    Y = 148,
                    Width = 420,
                    Height = 18,
                    Text = "Living checklist for regulatory compliance and audit validation",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B"
                },

                // Interactive Form Field Checkboxes
                new PdfFormFieldElement
                {
                    X = 637,
                    Y = 176,
                    Width = 420,
                    Height = 26,
                    FieldType = FormFieldType.Checkbox,
                    Label = "SOC-2 Type II & ISO/IEC 27001:2022 Recertification Completed",
                    Value = "true"
                },
                new PdfFormFieldElement
                {
                    X = 637,
                    Y = 208,
                    Width = 420,
                    Height = 26,
                    FieldType = FormFieldType.Checkbox,
                    Label = "EU-US Data Privacy Framework & GDPR Chapter V Enforced",
                    Value = "true"
                },
                new PdfFormFieldElement
                {
                    X = 637,
                    Y = 240,
                    Width = 420,
                    Height = 26,
                    FieldType = FormFieldType.Checkbox,
                    Label = "Hardware-Rooted Zero-Trust Cryptographic Enclave Attestation",
                    Value = "true"
                },
                new PdfFormFieldElement
                {
                    X = 637,
                    Y = 272,
                    Width = 420,
                    Height = 26,
                    FieldType = FormFieldType.Checkbox,
                    Label = "FedRAMP High In-Process Baseline Security Controls Validated",
                    Value = "true"
                },
                new PdfFormFieldElement
                {
                    X = 637,
                    Y = 304,
                    Width = 420,
                    Height = 26,
                    FieldType = FormFieldType.Checkbox,
                    Label = "NIST Post-Quantum Cryptography Migration Plan Formally Approved",
                    Value = "false"
                },

                // Digital Signatures Header
                new PdfTextElement
                {
                    X = 637,
                    Y = 344,
                    Width = 420,
                    Height = 18,
                    Text = "AUTHORIZED EXECUTIVE SIGN-OFF & CRYPTOGRAPHIC ATTESTATION",
                    FontSize = 8,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#64748B"
                },

                // Digital Signatures
                new PdfFormFieldElement
                {
                    X = 637,
                    Y = 366,
                    Width = 200,
                    Height = 44,
                    FieldType = FormFieldType.Signature,
                    Label = "Elena Vance, CEO",
                    Value = "DIGITALLY VERIFIED: E. Vance (QS-SEC-9842)"
                },
                new PdfFormFieldElement
                {
                    X = 852,
                    Y = 366,
                    Width = 205,
                    Height = 44,
                    FieldType = FormFieldType.Signature,
                    Label = "Marcus Chen, CFO",
                    Value = "DIGITALLY VERIFIED: M. Chen (QS-FIN-1402)"
                },

                new PdfTextElement
                {
                    X = 637,
                    Y = 420,
                    Width = 420,
                    Height = 18,
                    Text = "Cryptographically signed & anchored to internal audit ledger • Sep 2026",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B"
                },

                // Proprietary Commercial Secret Redaction Demo
                new PdfRedactionElement
                {
                    X = 637,
                    Y = 448,
                    Width = 420,
                    Height = 30,
                    ExemptionCode = "[FOIA (b)(4) PROPRIETARY COMMERCIAL SECRET]",
                    FillColorHex = "#1E293B",
                    TextColorHex = "#F8FAFC",
                    BorderColorHex = "#0F172A",
                    BorderThickness = 1
                },

                // -----------------------------------------------------------------
                // BOTTOM BANNER: QR CODE, TRACKING BARCODE & PORTAL VERIFICATION
                // -----------------------------------------------------------------
                new PdfShapeElement
                {
                    X = 50,
                    Y = 538,
                    Width = 1031,
                    Height = 192,
                    CornerRadius = 14,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },

                // Left Information & Barcode
                new PdfTextElement
                {
                    X = 72,
                    Y = 552,
                    Width = 310,
                    Height = 20,
                    Text = "Living Document Verification & Ledger",
                    FontSize = 12.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 72,
                    Y = 574,
                    Width = 310,
                    Height = 56,
                    Text = "Active presentation format with instant CSV export, interactive checklists, dynamic filtering, and cryptographic audit proofs.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#475569",
                    TextWrap = true
                },
                new PdfBarcodeElement
                {
                    X = 72,
                    Y = 636,
                    Width = 300,
                    Height = 74,
                    CodeValue = "QS-SEC-2026-FRYPDF-9842",
                    BarcodeFormat = "Code128",
                    BarColorHex = "#0F172A",
                    BackgroundColorHex = "#FFFFFF",
                    ShowText = true
                },

                // Center Area Trend Chart (Multi-Chart Variety)
                new PdfChartElement
                {
                    X = 398,
                    Y = 550,
                    Width = 380,
                    Height = 166,
                    ZIndex = 400,
                    Title = "3-Year R&D Capital Investment ($M)",
                    ChartType = ChartType.Area,
                    BackgroundColorHex = "#FFFFFF",
                    BorderColorHex = "#E2E8F0",
                    Categories = new List<string> { "2024", "2025", "2026", "2027 (P)", "2028 (P)" },
                    Values = new List<double> { 14.2, 22.8, 33.2, 48.0, 65.5 },
                    ValueLabels = new List<string> { "$14.2M", "$22.8M", "$33.2M", "$48.0M", "$65.5M" },
                    BarColorsHex = new List<string> { "#059669", "#10B981", "#34D399", "#6EE7B7", "#A7F3D0" }
                },

                // Right Live QR Code & Portal Link
                new PdfQrCodeElement
                {
                    X = 800,
                    Y = 554,
                    Width = 120,
                    Height = 120,
                    Content = "https://quantumscale.cloud/verify/doc?id=QS-2026-FRYPDF-9842",
                    Label = "SCAN TO VERIFY"
                },
                new PdfTextElement
                {
                    X = 934,
                    Y = 556,
                    Width = 135,
                    Height = 75,
                    Text = "Scan QR code to verify immutable ledger hash & cloud telemetry mirror.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#475569",
                    TextWrap = true
                },
                new PdfShapeElement
                {
                    X = 934,
                    Y = 642,
                    Width = 135,
                    Height = 32,
                    CornerRadius = 16,
                    FillColorHex = "#4F46E5",
                    StrokeThickness = 0,
                    Label = "🔗 SHA-256 SECURED",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 9.5
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);
        doc.Pages.Add(page3);

        return doc;
    }
}
