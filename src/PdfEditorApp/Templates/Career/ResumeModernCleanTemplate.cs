using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Career;

public class ResumeModernCleanTemplate : ITemplateDefinition
{
    public string Id => "resumemodern";
    public string Name => "Modern Tech & Product Resume";
    public string Description => "Two-column tech resume with skills sidebar, verified metrics, and interactive GitHub QR code";
    public string Category => "Career";
    public string IconKind => "AccountTieOutline";
    public string AccentColorHex => "#0284C7";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "John_Doe_Product_Engineer_Resume.pdf",
            Author = "John Doe",
            Subject = "Lead Product & Full-Stack Engineer Resume"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "John Doe • Lead Product Engineer | Portfolio: codefrydev.in",
            FooterCenter = "CONFIDENTIAL",
            FooterRight = "Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                // Left Column Background Sidebar (Width: 245)
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 245,
                    Height = 1131,
                    FillColorHex = "#F1F5F9",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                // Sidebar Right Border Line
                new PdfDividerElement
                {
                    IsVertical = true,
                    X = 245,
                    Y = 0,
                    Width = 1,
                    Height = 1131,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },

                // ==========================================
                // SIDEBAR CONTENT (Left Column: X = 20, Width = 205)
                // ==========================================

                // Monogram Initials Avatar Badge
                new PdfShapeElement
                {
                    X = 85,
                    Y = 45,
                    Width = 75,
                    Height = 75,
                    CornerRadius = 38,
                    FillColorHex = "#0284C7",
                    StrokeColorHex = "#0369A1",
                    StrokeThickness = 2,
                    Label = "JD",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 26
                },

                // Contact Details Header
                new PdfTextElement
                {
                    X = 20,
                    Y = 135,
                    Width = 205,
                    Height = 22,
                    Text = "CONTACT",
                    FontSize = 11.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 20,
                    Y = 157,
                    Width = 205,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#0284C7"
                },
                new PdfTextElement
                {
                    X = 20,
                    Y = 162,
                    Width = 205,
                    Height = 100,
                    Text = "📧 john.doe@codefrydev.in\n📱 +1 (555) 019-2834\n📍 San Francisco, CA\n🌐 codefrydev.in\n🐙 github.com/codefrydev\n💼 linkedin.com/in/codefrydev",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.45,
                    TextColorHex = "#334155"
                },

                // Technical Skills Header
                new PdfTextElement
                {
                    X = 20,
                    Y = 272,
                    Width = 205,
                    Height = 22,
                    Text = "TECHNICAL SKILLS",
                    FontSize = 11.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 20,
                    Y = 294,
                    Width = 205,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#0284C7"
                },
                new PdfTextElement
                {
                    X = 20,
                    Y = 298,
                    Width = 205,
                    Height = 200,
                    Text = "LANGUAGES\n• TypeScript, C# / .NET 9, Go, Python, SQL\n\nFRONTEND & UI\n• React, Next.js, Avalonia UI, TailwindCSS, WebGL, Redux, Vite\n\nBACKEND & CLOUD\n• Node.js, ASP.NET Core, GraphQL, gRPC, Kafka, Redis, PostgreSQL\n\nDEVOPS & INFRA\n• AWS, Kubernetes, Docker, Terraform, GitHub Actions CI/CD",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Education Header
                new PdfTextElement
                {
                    X = 20,
                    Y = 508,
                    Width = 205,
                    Height = 22,
                    Text = "EDUCATION",
                    FontSize = 11.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 20,
                    Y = 530,
                    Width = 205,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#0284C7"
                },
                new PdfTextElement
                {
                    X = 20,
                    Y = 535,
                    Width = 205,
                    Height = 90,
                    Text = "B.S. in Computer Science\nUniversity of California, Berkeley\nGPA: 3.89 / 4.00 (2016 – 2020)\n• Regents & Chancellor's Scholar\n• Tau Beta Pi Engineering Honor",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Certifications Header
                new PdfTextElement
                {
                    X = 20,
                    Y = 635,
                    Width = 205,
                    Height = 22,
                    Text = "CERTIFICATIONS",
                    FontSize = 11.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 20,
                    Y = 657,
                    Width = 205,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#0284C7"
                },
                new PdfTextElement
                {
                    X = 20,
                    Y = 662,
                    Width = 205,
                    Height = 70,
                    Text = "• AWS Solutions Architect Professional\n• Certified Kubernetes Application Developer (CKAD)\n• Meta Front-End Certified Professional",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Sidebar Live Portfolio QR Code
                new PdfQrCodeElement
                {
                    X = 72,
                    Y = 745,
                    Width = 100,
                    Height = 100,
                    Content = "https://github.com/codefrydev",
                    Label = "SCAN FOR GITHUB",
                    DarkColorHex = "#0284C7",
                    LightColorHex = "#F1F5F9"
                },

                // Spoken Languages
                new PdfTextElement
                {
                    X = 20,
                    Y = 855,
                    Width = 205,
                    Height = 22,
                    Text = "LANGUAGES",
                    FontSize = 11.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 20,
                    Y = 875,
                    Width = 205,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#0284C7"
                },
                new PdfTextElement
                {
                    X = 20,
                    Y = 882,
                    Width = 205,
                    Height = 50,
                    Text = "• English (Native / Bilingual)\n• Mandarin Chinese (Fluent)\n• German (Conversational B1)",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // ==========================================
                // MAIN BODY CONTENT (Right Column: X = 265, Width = 480)
                // ==========================================

                // Candidate Name
                new PdfTextElement
                {
                    X = 265,
                    Y = 40,
                    Width = 480,
                    Height = 34,
                    Text = "JOHN DOE",
                    FontSize = 26,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // Subtitle / Professional Title
                new PdfTextElement
                {
                    X = 265,
                    Y = 74,
                    Width = 480,
                    Height = 22,
                    Text = "Lead Product Engineer • Full-Stack & Systems Architecture",
                    FontSize = 12.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0284C7"
                },

                // Executive Summary Card
                new PdfTextElement
                {
                    X = 265,
                    Y = 98,
                    Width = 480,
                    Height = 65,
                    Text = "High-velocity Lead Product Engineer with 7+ years of track record architecting mission-critical SaaS web & desktop applications. Proven expertise in zero-to-one product engineering, leading distributed frontend teams, and refactoring core ingestion pipelines to support 10M+ daily active users.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#475569"
                },

                // Section: Professional Experience
                new PdfTextElement
                {
                    X = 265,
                    Y = 170,
                    Width = 480,
                    Height = 22,
                    Text = "PROFESSIONAL EXPERIENCE",
                    FontSize = 13,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 265,
                    Y = 192,
                    Width = 480,
                    Height = 2,
                    Thickness = 1.5,
                    ColorHex = "#0284C7"
                },

                // Job 1
                new PdfTextElement
                {
                    X = 265,
                    Y = 202,
                    Width = 330,
                    Height = 20,
                    Text = "Lead Product Engineer | CodeFryDev Technologies",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 600,
                    Y = 202,
                    Width = 145,
                    Height = 20,
                    Text = "2023 – Present • SF, CA",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0284C7",
                    Alignment = TextAlignmentMode.Right
                },
                new PdfTextElement
                {
                    X = 265,
                    Y = 224,
                    Width = 480,
                    Height = 85,
                    Text = "• Led a squad of 8 engineers architecting real-time collaboration canvas with WebGL & WebSockets, reducing latency by 45% for 2.4M enterprise users.\n• Built automated design token compilation engine across web and desktop clients, cutting designer-to-code iteration time by 60%.\n• Designed multi-tenant billing & seat provisioning microservice processing $35M+ annual recurring revenue with 99.99% uptime.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#334155"
                },

                // Job 2
                new PdfTextElement
                {
                    X = 265,
                    Y = 315,
                    Width = 330,
                    Height = 20,
                    Text = "Senior Full-Stack Engineer | CodeFryDev Solutions",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 600,
                    Y = 315,
                    Width = 145,
                    Height = 20,
                    Text = "2021 – 2023 • SF, CA",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0284C7",
                    Alignment = TextAlignmentMode.Right
                },
                new PdfTextElement
                {
                    X = 265,
                    Y = 337,
                    Width = 480,
                    Height = 85,
                    Text = "• Spearheaded redesign of multi-currency onboarding KYC flow across 35 countries, improving checkout completion rate by +14.2%.\n• Optimized global GraphQL API caching layer with Redis cluster, dropping P95 API response times from 340ms to 48ms.\n• Mentored 5 junior and mid-level engineers through code reviews, tech talks, and comprehensive RFC design document templates.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#334155"
                },

                // Job 3
                new PdfTextElement
                {
                    X = 265,
                    Y = 428,
                    Width = 330,
                    Height = 20,
                    Text = "Software Engineer | CodeFryDev Labs",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 600,
                    Y = 428,
                    Width = 145,
                    Height = 20,
                    Text = "2019 – 2021 • Berkeley, CA",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0284C7",
                    Alignment = TextAlignmentMode.Right
                },
                new PdfTextElement
                {
                    X = 265,
                    Y = 450,
                    Width = 480,
                    Height = 80,
                    Text = "• Engineered interactive data visualization dashboards rendering 500,000+ data points smoothly at 60 FPS using React & D3.js.\n• Migrated legacy monolith backend to containerized Docker services running on AWS ECS, decreasing deployment cycle from 3 days to 15 minutes.\n• Implemented automated Jest and Cypress test suites achieving 91% code coverage.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#334155"
                },

                // Section: Featured Open Source & Projects
                new PdfTextElement
                {
                    X = 265,
                    Y = 538,
                    Width = 480,
                    Height = 22,
                    Text = "FEATURED OPEN SOURCE & KEY PROJECTS",
                    FontSize = 13,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 265,
                    Y = 560,
                    Width = 480,
                    Height = 2,
                    Thickness = 1.5,
                    ColorHex = "#0284C7"
                },

                // Project 1
                new PdfTextElement
                {
                    X = 265,
                    Y = 570,
                    Width = 480,
                    Height = 20,
                    Text = "🚀 FastCanvas-Core (Creator & Maintainer — 3.2k GitHub Stars)",
                    FontSize = 10.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0284C7"
                },
                new PdfTextElement
                {
                    X = 265,
                    Y = 592,
                    Width = 480,
                    Height = 55,
                    Text = "Lightweight zero-allocation 2D vector drawing library built on WebAssembly and Skia. Adopted by over 40,000 active web applications worldwide with sub-millisecond layer caching.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Project 2
                new PdfTextElement
                {
                    X = 265,
                    Y = 652,
                    Width = 480,
                    Height = 20,
                    Text = "⚡ Micro-State-Sync (Co-Author)",
                    FontSize = 10.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0284C7"
                },
                new PdfTextElement
                {
                    X = 265,
                    Y = 674,
                    Width = 480,
                    Height = 55,
                    Text = "CRDT-based real-time state synchronization engine for collaborative multi-user editing with conflict-free vector clocks and end-to-end encrypted payload verification.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section: Awards & Leadership
                new PdfTextElement
                {
                    X = 265,
                    Y = 736,
                    Width = 480,
                    Height = 22,
                    Text = "HONORS & LEADERSHIP",
                    FontSize = 13,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 265,
                    Y = 758,
                    Width = 480,
                    Height = 2,
                    Thickness = 1.5,
                    ColorHex = "#0284C7"
                },
                new PdfTextElement
                {
                    X = 265,
                    Y = 766,
                    Width = 480,
                    Height = 70,
                    Text = "• Winner, Stripe Global Hackathon 2022 (Real-time Settlement Engine)\n• Keynote Speaker, React Summit SF 2024 (\"Building 60 FPS Canvas Apps\")\n• Mentor, Women in Tech Engineering Cohort (Mentored 18 women engineers)",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#334155"
                },

                // Section: Patents & Publications
                new PdfTextElement
                {
                    X = 265,
                    Y = 845,
                    Width = 480,
                    Height = 22,
                    Text = "PATENTS & TECHNICAL PUBLICATIONS",
                    FontSize = 13,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfDividerElement
                {
                    X = 265,
                    Y = 867,
                    Width = 480,
                    Height = 2,
                    Thickness = 1.5,
                    ColorHex = "#0284C7"
                },
                new PdfTextElement
                {
                    X = 265,
                    Y = 875,
                    Width = 480,
                    Height = 90,
                    Text = "• US Patent 11,842,109: \"Zero-Allocation Vector Tile Streaming in Distributed Browser Clients\"\n• Doe, J. (2024). \"Sub-Millisecond Canvas Geometry Rendering in Modern WebGL2 Environments.\" ACM SIGGRAPH Web3D Proceedings, pp. 45–56.\n• Available for advisory and engineering leadership consultations via codefrydev.in.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#334155"
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
