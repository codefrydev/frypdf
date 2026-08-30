using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class ResumeCreativeMinimalistTemplate : ITemplateDefinition
{
    public string Id => "resumecreative";
    public string Name => "Creative UI/UX Design Director Resume";
    public string Description => "Minimalist design resume featuring creative philosophy, case study outcomes, and interactive Figma QR code";
    public string Category => "Career";
    public string IconKind => "PaletteOutline";
    public string AccentColorHex => "#0D9488";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Maya_Lin_Design_Director_Resume.pdf",
            Author = "Maya Lin",
            Subject = "Senior Design Director & UI/UX Specialist Resume"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FCFCFD",
            FooterLeft = "Maya Lin • Creative Director & Product Designer | mayalin.design",
            FooterCenter = "PORTFOLIO & CASE STUDIES",
            FooterRight = "Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                // Top Teal Accent Bar
                new PdfShapeElement
                {
                    X = 55,
                    Y = 40,
                    Width = 690,
                    Height = 4,
                    FillColorHex = "#0D9488",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                // Candidate Name & Creative Title
                new PdfTextElement
                {
                    X = 55,
                    Y = 54,
                    Width = 480,
                    Height = 38,
                    Text = "MAYA LIN",
                    FontSize = 28,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 92,
                    Width = 480,
                    Height = 24,
                    Text = "Design Director • Principal Product & Interaction Designer",
                    FontSize = 12.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0D9488"
                },

                // Contact Strip
                new PdfTextElement
                {
                    X = 55,
                    Y = 116,
                    Width = 520,
                    Height = 20,
                    Text = "🌐 mayalin.design   •   ✉️ maya@mayalin.design   •   📍 New York, NY   •   📱 +1 (917) 482-9011",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B"
                },

                // Top Right Interactive Portfolio QR Code
                new PdfQrCodeElement
                {
                    X = 645,
                    Y = 48,
                    Width = 100,
                    Height = 85,
                    Content = "https://mayalin.design/case-studies",
                    Label = "VIEW CASE STUDIES",
                    DarkColorHex = "#0D9488",
                    LightColorHex = "#FCFCFD"
                },

                // Header Divider Line
                new PdfDividerElement
                {
                    X = 55,
                    Y = 142,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },

                // Creative Statement / Design Philosophy Box
                new PdfShapeElement
                {
                    X = 55,
                    Y = 152,
                    Width = 690,
                    Height = 72,
                    CornerRadius = 8,
                    FillColorHex = "#F0FDFA",
                    StrokeColorHex = "#99F6E4",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 72,
                    Y = 160,
                    Width = 656,
                    Height = 56,
                    Text = "\"Designing at the intersection of emotional elegance, strict accessibility, and data-driven usability. 10+ years shaping high-growth digital products adopted by 50M+ users globally, transforming complex enterprise workflows into intuitive, joyous experiences.\"",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsItalic = true,
                    LineHeight = 1.45,
                    TextColorHex = "#115E59"
                },

                // Section: Core Design Disciplines
                new PdfTextElement
                {
                    X = 55,
                    Y = 236,
                    Width = 690,
                    Height = 22,
                    Text = "CORE DESIGN DISCIPLINES & EXPERTISE",
                    FontSize = 12,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 258,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 265,
                    Width = 330,
                    Height = 65,
                    Text = "• Design Systems Architecture & Tokenization\n• End-to-End Product & Mobile App Strategy\n• User Research, Ethnography & Usability Testing\n• WCAG AAA Accessibility & Inclusive Design",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 265,
                    Width = 330,
                    Height = 65,
                    Text = "• Interactive Prototyping & Motion Choreography\n• 3D Spatial & Canvas Interface Engineering\n• Cross-Functional Leadership (Design & Engineering)\n• Brand Identity, Editorial Direction & Storytelling",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section: Design Leadership Experience
                new PdfTextElement
                {
                    X = 55,
                    Y = 340,
                    Width = 690,
                    Height = 22,
                    Text = "DESIGN LEADERSHIP EXPERIENCE",
                    FontSize = 12,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 362,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },

                // Role 1
                new PdfTextElement
                {
                    X = 55,
                    Y = 370,
                    Width = 490,
                    Height = 20,
                    Text = "Design Director | Studio Horizon — New York, NY",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 550,
                    Y = 370,
                    Width = 195,
                    Height = 20,
                    Text = "2022 – Present",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0D9488",
                    Alignment = TextAlignmentMode.Right
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 390,
                    Width = 690,
                    Height = 85,
                    Text = "✦  Built and mentored a high-performing team of 14 product designers, motion artists, and design system engineers.\n✦  Directed the redesign of an enterprise fintech suite handling $12B+ in annual transaction volume, lifting Task Success Rate by +28% and NPS by +34 points.\n✦  Created the unified 'Aura' multiplatform design system deployed across iOS, Android, Web, and Desktop.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#334155"
                },

                // Role 2
                new PdfTextElement
                {
                    X = 55,
                    Y = 485,
                    Width = 490,
                    Height = 20,
                    Text = "Principal Product Designer | Airbnb (Host Experience) — San Francisco, CA",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 550,
                    Y = 485,
                    Width = 195,
                    Height = 20,
                    Text = "2018 – 2022",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0D9488",
                    Alignment = TextAlignmentMode.Right
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 505,
                    Width = 690,
                    Height = 85,
                    Text = "✦  Led end-to-end UX vision for new Host Onboarding flow across 220 countries, resulting in a +19.4% increase in completed active listings within the first 60 days.\n✦  Conducted in-depth contextual inquiry user studies in Tokyo, London, and Berlin to identify friction points in multi-calendar management.\n✦  Collaborated closely with VP of Design on strategic brand evolutionary guidelines.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#334155"
                },

                // Role 3
                new PdfTextElement
                {
                    X = 55,
                    Y = 600,
                    Width = 490,
                    Height = 20,
                    Text = "Senior Interaction Designer | Pentagram Design — New York, NY",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 550,
                    Y = 600,
                    Width = 195,
                    Height = 20,
                    Text = "2015 – 2018",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0D9488",
                    Alignment = TextAlignmentMode.Right
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 620,
                    Width = 690,
                    Height = 75,
                    Text = "✦  Crafted award-winning digital experiences and spatial interactive installations for clients including MoMA, The Atlantic, and Spotify.\n✦  Authored interactive brand design guides and dynamic typography specifications.\n✦  Recipient of Red Dot Best of the Best Award 2017.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#334155"
                },

                // Section: Featured Case Studies & Quantified Impact
                new PdfTextElement
                {
                    X = 55,
                    Y = 705,
                    Width = 690,
                    Height = 22,
                    Text = "FEATURED CASE STUDIES & MEASURED IMPACT",
                    FontSize = 12,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 727,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },

                // Case Study Cards
                new PdfShapeElement
                {
                    X = 55,
                    Y = 735,
                    Width = 330,
                    Height = 90,
                    CornerRadius = 6,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 68,
                    Y = 742,
                    Width = 304,
                    Height = 78,
                    Text = "🏆 Global Fintech Workspace Reimagination\nRole: Lead Design Strategist • Client: Fortune 100\nImpact: +38% time savings on trades, $45M revenue impact, 0 accessibility violations across 48 modules.",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                new PdfShapeElement
                {
                    X = 415,
                    Y = 735,
                    Width = 330,
                    Height = 90,
                    CornerRadius = 6,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 428,
                    Y = 742,
                    Width = 304,
                    Height = 78,
                    Text = "🎨 Spatial Museum Interactive Experience\nRole: Creative Director • Client: Whitney Museum\nImpact: 1.4M visitors engaged, Featured in Fast Company Innovation by Design, Awwwards Site of the Year.",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section: Education & Recognition
                new PdfTextElement
                {
                    X = 55,
                    Y = 835,
                    Width = 330,
                    Height = 22,
                    Text = "EDUCATION & TRAINING",
                    FontSize = 12,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 857,
                    Width = 330,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 864,
                    Width = 330,
                    Height = 65,
                    Text = "Master of Fine Arts (MFA) in Interaction Design\nSchool of Visual Arts (SVA), New York (2013 – 2015)\n\nB.A. in Graphic Design & Human-Computer Interaction\nCarnegie Mellon University • With University Honors",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 835,
                    Width = 330,
                    Height = 22,
                    Text = "AWARDS & JURIES",
                    FontSize = 12,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 415,
                    Y = 857,
                    Width = 330,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 864,
                    Width = 330,
                    Height = 65,
                    Text = "• D&AD Yellow Pencil Winner (Digital Design 2023)\n• Red Dot: Best of the Best Award (2017, 2021)\n• Jury Member, AIGA National Design Competitions\n• Fast Company 100 Most Creative People in Business",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
