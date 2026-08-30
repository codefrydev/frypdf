using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class ResumeTemplate : ITemplateDefinition
{
    public string Id => "resume";
    public string Name => "Executive Resume";
    public string Description => "Complete CV with QR code, competencies, and verified metrics";
    public string Category => "Career";
    public string IconKind => "AccountTieOutline";
    public string AccentColorHex => "#7C3AED";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Alex_Morgan_Executive_Resume.pdf",
            Author = "Alex Morgan",
            Subject = "Principal Software Architect Resume"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "Alex Morgan • Principal Software Architect | Portfolio: alexmorgan.dev",
            FooterCenter = "CONFIDENTIAL & VERIFIED",
            FooterRight = "Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                // 1. Monogram Initials Avatar Badge Top Left
                new PdfShapeElement
                {
                    X = 55,
                    Y = 45,
                    Width = 54,
                    Height = 54,
                    CornerRadius = 12,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#0C599B",
                    StrokeThickness = 0,
                    Label = "AM",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 22
                },

                // 2. Candidate Full Name
                new PdfTextElement
                {
                    X = 120,
                    Y = 45,
                    Width = 510,
                    Height = 32,
                    Text = "ALEXANDER MORGAN, M.Sc.",
                    FontSize = 22,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // 3. Professional Headline / Title
                new PdfTextElement
                {
                    X = 120,
                    Y = 75,
                    Width = 510,
                    Height = 22,
                    Text = "Principal Software Architect | Cloud & Distributed Systems",
                    FontSize = 12,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#0F6CBD",
                    IsBold = true
                },

                // 4. Contact Information Strip (Line 1: Phone, Email, Location)
                new PdfTextElement
                {
                    X = 120,
                    Y = 96,
                    Width = 510,
                    Height = 20,
                    Text = "📧 alex.morgan@techlead.io   •   📱 +1 (555) 234-5678   •   📍 Seattle, WA",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#475569"
                },

                // 5. Contact Information Strip (Line 2: Portfolio, LinkedIn, GitHub)
                new PdfTextElement
                {
                    X = 120,
                    Y = 114,
                    Width = 510,
                    Height = 20,
                    Text = "🔗 linkedin.com/in/alexmorgan-dev   •   🌐 alexmorgan.dev   •   🐙 github.com/alexmorgan",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#475569"
                },

                // 6. Portfolio & Verification QR Code Badge Top Right
                new PdfQrCodeElement
                {
                    X = 645,
                    Y = 45,
                    Width = 100,
                    Height = 85,
                    Content = "https://alexmorgan.dev",
                    Label = "PORTFOLIO QR"
                },

                // 7. Header Accent Divider Line
                new PdfDividerElement
                {
                    X = 55,
                    Y = 142,
                    Width = 690,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#0F6CBD"
                },

                // 8. Section: Executive Summary Title
                new PdfTextElement
                {
                    X = 55,
                    Y = 152,
                    Width = 690,
                    Height = 22,
                    Text = "EXECUTIVE SUMMARY",
                    FontSize = 12.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // 9. Executive Summary Callout Card
                new PdfTextElement
                {
                    X = 55,
                    Y = 174,
                    Width = 690,
                    Height = 68,
                    Text = "Strategic Principal Software Architect with 12+ years of expertise architecting high-throughput distributed systems, cross-platform enterprise desktop suites (.NET / Avalonia UI), and cloud-native microservices. Track record of leading 20+ member cross-functional engineering teams, orchestrating cloud migrations that reduced operational overhead by $1.4M annually, and delivering fault-tolerant platforms processing 100M+ transactions daily.",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.45,
                    TextColorHex = "#334155",
                    BackgroundColorHex = "#F8FAFC",
                    BorderColorHex = "#E2E8F0",
                    BorderThickness = 1.0,
                    CornerRadius = 6,
                    Padding = 8
                },

                // 10. Section: Core Competencies & Skills Title
                new PdfTextElement
                {
                    X = 55,
                    Y = 246,
                    Width = 690,
                    Height = 22,
                    Text = "CORE COMPETENCIES & TECHNICAL EXPERTISE",
                    FontSize = 12.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // 11. Core Competencies Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 268,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },

                // 12. Skills Column Left
                new PdfTextElement
                {
                    X = 55,
                    Y = 276,
                    Width = 335,
                    Height = 64,
                    Text = "• Cloud & Distributed: Microsoft Azure, AWS, Kubernetes, Docker, Kafka, gRPC, Redis, Microservices, CI/CD\n• Architecture & Design: DDD, Event Sourcing, High Availability, Zero-Trust Security, System Resiliency, SOC2",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#374151"
                },

                // 13. Skills Column Right
                new PdfTextElement
                {
                    X = 410,
                    Y = 276,
                    Width = 335,
                    Height = 64,
                    Text = "• Languages & Frameworks: C# / .NET 9, Avalonia UI, WPF, TypeScript, Node.js, Go, Python, PostgreSQL\n• Engineering Leadership: Technical Roadmapping, Team Mentorship (20+ devs), Agile/Scrum, RFC Process",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#374151"
                },

                // 14. Section: Professional Experience Title
                new PdfTextElement
                {
                    X = 55,
                    Y = 348,
                    Width = 690,
                    Height = 22,
                    Text = "PROFESSIONAL EXPERIENCE",
                    FontSize = 12.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // 15. Professional Experience Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 370,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },

                // 16. Role 1: Title & Company
                new PdfTextElement
                {
                    X = 55,
                    Y = 378,
                    Width = 490,
                    Height = 20,
                    Text = "Principal Software Architect | CloudScale Global Inc. — Seattle, WA",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },

                // 17. Role 1: Date Range
                new PdfTextElement
                {
                    X = 550,
                    Y = 378,
                    Width = 195,
                    Height = 20,
                    Text = "Jan 2021 – Present",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD",
                    Alignment = TextAlignmentMode.Right
                },

                // 18. Role 1: Bullets & Achievements
                new PdfTextElement
                {
                    X = 55,
                    Y = 398,
                    Width = 690,
                    Height = 90,
                    Text = "✔  Spearheaded architecture of enterprise multiplatform desktop publishing suite using Avalonia UI and .NET 9, achieving 60 FPS rendering and reducing memory footprint by 45% across macOS, Windows, and Linux.\n✔  Led engineering transition of monolithic billing platform to Azure Kubernetes Service (AKS) microservices, lowering P99 latency by 55% and reducing annual cloud costs by $1.4M.\n✔  Mentored 22 software engineers, established automated CI/CD static security pipelines, and spearheaded RFC design review culture across 4 engineering pods.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#374151"
                },

                // 19. Role 2: Title & Company
                new PdfTextElement
                {
                    X = 55,
                    Y = 498,
                    Width = 490,
                    Height = 20,
                    Text = "Staff Systems Engineer | Horizon Data Systems — Redmond, WA",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },

                // 20. Role 2: Date Range
                new PdfTextElement
                {
                    X = 550,
                    Y = 498,
                    Width = 195,
                    Height = 20,
                    Text = "Mar 2017 – Dec 2020",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD",
                    Alignment = TextAlignmentMode.Right
                },

                // 21. Role 2: Bullets & Achievements
                new PdfTextElement
                {
                    X = 55,
                    Y = 518,
                    Width = 690,
                    Height = 90,
                    Text = "✔  Architected distributed event streaming ingestion engine processing 75M+ transactions/day using Apache Kafka and .NET Core with 99.999% SLA availability.\n✔  Designed high-performance SkiaSharp hardware-accelerated rasterization backend for high-volume document exports and digital signature verification.\n✔  Partnered with Product and InfoSec teams to achieve SOC2 Type II compliance and implement end-to-end payload encryption.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#374151"
                },

                // 22. Role 3: Title & Company
                new PdfTextElement
                {
                    X = 55,
                    Y = 618,
                    Width = 490,
                    Height = 20,
                    Text = "Senior Software Engineer | Apex Solutions Ltd — Seattle, WA",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },

                // 23. Role 3: Date Range
                new PdfTextElement
                {
                    X = 550,
                    Y = 618,
                    Width = 195,
                    Height = 20,
                    Text = "Jun 2014 – Feb 2017",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD",
                    Alignment = TextAlignmentMode.Right
                },

                // 24. Role 3: Bullets & Achievements
                new PdfTextElement
                {
                    X = 55,
                    Y = 638,
                    Width = 690,
                    Height = 76,
                    Text = "✔  Engineered RESTful web services and enterprise desktop clients adopted by 250,000+ business users globally.\n✔  Refactored legacy database queries and introduced distributed Redis caching layer, improving API response times from 850ms to 45ms.\n✔  Introduced automated integration testing with 88% code coverage, cutting QA regression cycles from 2 weeks to 3 days.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#374151"
                },

                // 25. Section: Education & Credentials Title
                new PdfTextElement
                {
                    X = 55,
                    Y = 720,
                    Width = 690,
                    Height = 22,
                    Text = "EDUCATION & PROFESSIONAL CREDENTIALS",
                    FontSize = 12.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // 26. Education Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 742,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },

                // 27. Education Column Subheading
                new PdfTextElement
                {
                    X = 55,
                    Y = 750,
                    Width = 335,
                    Height = 20,
                    Text = "🎓 Academic Background",
                    FontSize = 10.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },

                // 28. Education Details
                new PdfTextElement
                {
                    X = 55,
                    Y = 770,
                    Width = 335,
                    Height = 80,
                    Text = "Master of Science in Computer Science\nUniversity of Washington • GPA: 3.92 (2012 – 2014)\n\nBachelor of Science in Software Engineering\nSeattle University • Magna Cum Laude (2008 – 2012)",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#374151"
                },

                // 29. Certifications Column Subheading
                new PdfTextElement
                {
                    X = 410,
                    Y = 750,
                    Width = 335,
                    Height = 20,
                    Text = "🏆 Key Certifications & Honors",
                    FontSize = 10.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },

                // 30. Certifications Details
                new PdfTextElement
                {
                    X = 410,
                    Y = 770,
                    Width = 335,
                    Height = 80,
                    Text = "• Microsoft Certified: Azure Solutions Architect Expert\n• AWS Certified Solutions Architect – Professional (SAP-C02)\n• Certified Kubernetes Administrator (CKA — Linux Foundation)\n• IEEE Senior Member & Open Source Contributor",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#374151"
                },

                // 31. Section: Notable Projects & Open Source
                new PdfTextElement
                {
                    X = 55,
                    Y = 860,
                    Width = 690,
                    Height = 22,
                    Text = "NOTABLE PROJECTS & OPEN SOURCE CONTRIBUTIONS",
                    FontSize = 12.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },

                // 32. Projects Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 882,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },

                // 33. Projects Callout Box
                new PdfTextElement
                {
                    X = 55,
                    Y = 890,
                    Width = 690,
                    Height = 90,
                    Text = "🚀 OpenPDF & CrossPlatform-UI Engine (Creator & Core Maintainer — 4.8k GitHub Stars)\nEngineered high-performance cross-platform vector rendering library for .NET with SkiaSharp integration.\n\n🌐 Enterprise Distributed Cache Mesh (Co-Author)\nLightweight, low-latency consensus protocol implementation for distributed multi-region memory stores.",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.4,
                    TextColorHex = "#166534",
                    BackgroundColorHex = "#F0FDF4",
                    BorderColorHex = "#BBF7D0",
                    BorderThickness = 1.0,
                    CornerRadius = 6,
                    Padding = 8
                },

                // 34. Bottom Verification Strip Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 992,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },

                // 35. References & Verification Notice
                new PdfTextElement
                {
                    X = 55,
                    Y = 1002,
                    Width = 690,
                    Height = 20,
                    Text = "References, patent publications, and verified code portfolio available upon request at alexmorgan.dev",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
