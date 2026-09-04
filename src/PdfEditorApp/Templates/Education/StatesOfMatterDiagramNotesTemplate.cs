using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Services;

namespace PdfEditorApp.Templates.Education;

public class StatesOfMatterDiagramNotesTemplate : ITemplateDefinition
{
    public string Id => "states_of_matter_notes";
    public string Name => "Science Visual Notes (States of Matter)";
    public string Description => "Science diagram study guide featuring triangular phase change cycle, particle arrangement models, comparison tables, and conceptual Q&A";
    public string Category => "Education";
    public string IconKind => "Atom";
    public string AccentColorHex => "#0284C7";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "States_of_Matter_Visual_Notes.pdf",
            Author = "Science & Physics Faculty",
            Subject = "Visual Study Notes: Interconversion of States of Matter & Kinetic Particle Theory"
        };

        // =========================================================================
        // PAGE 1: Triangular Phase Change Cycle & Particle Models
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "Science Visual Notes Series",
            HeaderCenter = "General Science • Chemistry & Thermal Physics",
            HeaderRight = "Topic: Matter & Phase Change",
            FooterLeft = "Comparative Study of Three States of Matter",
            FooterCenter = "Authorized Educational Study Notes",
            FooterRight = "Page 1 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Brand Stripe
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 800, Height = 8,
                    FillColorHex = "#0284C7", StrokeColorHex = "#00000000"
                },

                // Title Banner Box
                new PdfShapeElement
                {
                    X = 40, Y = 35, Width = 720, Height = 80,
                    CornerRadius = 8, FillColorHex = "#F0F9FF",
                    StrokeColorHex = "#BAE6FD", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 45, Width = 690, Height = 28,
                    Text = "Interconversion of States of Matter • ठोस, द्रव और गैस का परस्पर परिवर्तन",
                    FontSize = 16, IsBold = true, TextColorHex = "#0369A1",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 75, Width = 690, Height = 22,
                    Text = "A Comprehensive Study on Phase Transitions, Latent Heat, and Kinetic Particle Theory",
                    FontSize = 10.5, IsItalic = true, TextColorHex = "#0284C7",
                    FontFamily = "Inter"
                },

                // Section 1 Header: Triangular Cycle
                new PdfShapeElement
                {
                    X = 40, Y = 125, Width = 720, Height = 26,
                    CornerRadius = 4, FillColorHex = "#0F172A",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 130, Width = 700, Height = 18,
                    Text = "1. TRIANGULAR INTERCONVERSION DIAGRAM (त्रिभुजीय परिवर्तन चक्र)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Triangular Phase Change Vector Diagram
                new PdfSvgElement
                {
                    X = 50, Y = 160, Width = 700, Height = 350,
                    SvgSource = SvgOrnamentLibrary.GetTriangularPhaseCycleSvg(),
                    PresetName = "Triangular Phase Cycle"
                },

                // Phase Transition Explanatory Box
                new PdfShapeElement
                {
                    X = 40, Y = 525, Width = 720, Height = 75,
                    CornerRadius = 6, FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#CBD5E1", StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 55, Y = 533, Width = 690, Height = 60,
                    Text = "• Melting (गलनांक): Change of solid into liquid by heating.\n• Freezing (हिमीकरण): Liquid changing into solid upon cooling.\n• Vaporization (वाष्पीकरण): Liquid converting to vapor phase.\n• Condensation (संघनन): Gas cooling to liquid phase.\n• Sublimation (ऊर्ध्वपातन): Direct transition of solid to gas without entering liquid state (e.g. Camphor, Dry Ice).\n• Deposition (निक्षेपण): Direct change of gas into solid without becoming a liquid (e.g. Frost formation).",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Section 2 Header: Particle Lattice Models
                new PdfShapeElement
                {
                    X = 40, Y = 615, Width = 720, Height = 26,
                    CornerRadius = 4, FillColorHex = "#0F172A",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 620, Width = 700, Height = 18,
                    Text = "2. PARTICLE ARRANGEMENT & MOLECULAR PACKING (कणों की व्यवस्था)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Particle Arrangement Vector Grid
                new PdfSvgElement
                {
                    X = 50, Y = 650, Width = 700, Height = 175,
                    SvgSource = SvgOrnamentLibrary.GetParticleArrangementGridSvg(),
                    PresetName = "Particle Arrangement"
                },

                // Key Takeaway Card
                new PdfShapeElement
                {
                    X = 40, Y = 840, Width = 720, Height = 65,
                    CornerRadius = 6, FillColorHex = "#FFFBEB",
                    StrokeColorHex = "#FDE68A", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 848, Width = 690, Height = 18,
                    Text = "KEY THERMODYNAMIC TAKEAWAY (मुख्य निष्कर्ष):",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#92400E",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 870, Width = 690, Height = 25,
                    Text = "During phase transitions, temperature remains constant despite heat absorption due to Latent Heat (गुप्त ऊष्मा), which is consumed in breaking intermolecular bonds rather than increasing kinetic energy.",
                    FontSize = 9.5, TextColorHex = "#78350F",
                    FontFamily = "Inter"
                }
            }
        };

        // =========================================================================
        // PAGE 2: Comparison Table & Conceptual Q&A
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "Science Visual Notes Series",
            HeaderCenter = "Properties Comparison & Conceptual Q&A",
            HeaderRight = "Topic: Matter & Phase Change",
            FooterLeft = "Comparative Study of Three States of Matter",
            FooterCenter = "Authorized Educational Study Notes",
            FooterRight = "Page 2 of 2",
            Elements = new List<PdfElementBase>
            {
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 800, Height = 8,
                    FillColorHex = "#0284C7", StrokeColorHex = "#00000000"
                },

                // Section 3 Header: Comparison Table
                new PdfShapeElement
                {
                    X = 40, Y = 35, Width = 720, Height = 26,
                    CornerRadius = 4, FillColorHex = "#0F172A",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 40, Width = 700, Height = 18,
                    Text = "3. COMPARATIVE STUDY TABLE (पदार्थ के तीनों अवस्थाओं का तुलनात्मक अध्ययन)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Comparison Table
                new PdfTableElement
                {
                    X = 40, Y = 70, Width = 720, Height = 340,
                    HeaderBackgroundHex = "#0284C7",
                    HeaderTextHex = "#FFFFFF",
                    BorderColorHex = "#CBD5E1",
                    AlternateRowBackgroundHex = "#F0F9FF",
                    Headers = new List<string> { "Properties (गुण)", "Solid (ठोस)", "Liquid (द्रव)", "Gaseous (गैस)" },
                    Rows = new List<List<string>>
                    {
                        new() { "Shape (आकार)", "निश्चित आकार (Fixed Shape)", "अनिश्चित आकार (Takes container shape)", "अनिश्चित आकार (Indefinite)" },
                        new() { "Volume (आयतन)", "निश्चित आयतन (Fixed Volume)", "निश्चित आयतन (Definite Volume)", "अनिश्चित आयतन (Indefinite Volume)" },
                        new() { "Compressibility (संपीड्यता)", "नगण्य (Negligible)", "बहुत कम (Very Low)", "अत्यधिक उच्च (Highly Compressible)" },
                        new() { "Intermolecular Force (आकर्षण बल)", "प्रबलतम (Strongest)", "मध्यम (Intermediate)", "नगण्य (Negligible / Weakest)" },
                        new() { "Intermolecular Space (रिक्त स्थान)", "न्यूनतम (Minimum)", "मध्यम (Intermediate)", "अधिकतम (Maximum)" },
                        new() { "Kinetic Energy (गतिज ऊर्जा)", "न्यूनतम (Minimum)", "मध्यम (Intermediate)", "अधिकतम (Maximum)" },
                        new() { "Fluidity / Rigidity (प्रवाह)", "दृढ़ (Rigid, Cannot flow)", "प्रवाही (Fluid, Flows downwards)", "प्रवाही (Diffuses in all directions)" },
                        new() { "Density (घनत्व)", "उच्च (High)", "मध्यम (Moderate)", "अति निम्न (Very Low)" }
                    }
                },

                // Section 4 Header: Conceptual Q&A
                new PdfShapeElement
                {
                    X = 40, Y = 430, Width = 720, Height = 26,
                    CornerRadius = 4, FillColorHex = "#0F172A",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 435, Width = 700, Height = 18,
                    Text = "4. CONCEPTUAL QUESTIONS & REASONING (अवधारणात्मक प्रश्न एवं वैज्ञानिक व्याख्या)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Q1 Card
                new PdfShapeElement { X = 40, Y = 465, Width = 720, Height = 80, CornerRadius = 6, FillColorHex = "#F8FAFC", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 472, Width = 690, Height = 22,
                    Text = "Q1. Why does ice float on water despite being a solid? (ठोस होने पर भी बर्फ पानी पर क्यों तैरती है?)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#0369A1",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 496, Width = 690, Height = 42,
                    Text = "Answer: Ice has a 3D cage-like hexagonal crystal structure due to hydrogen bonding with vacant spaces trapped inside. Consequently, its density is lower than that of liquid water (0.917 g/cm³ vs 1.0 g/cm³), allowing ice to float.",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Inter"
                },

                // Q2 Card
                new PdfShapeElement { X = 40, Y = 555, Width = 720, Height = 80, CornerRadius = 6, FillColorHex = "#F8FAFC", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 562, Width = 690, Height = 22,
                    Text = "Q2. Why does steam cause more severe burns than boiling water at 100\u00b0C? (100\u00b0C पर भाप अधिक जलन क्यों पैदा करती है?)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#0369A1",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 586, Width = 690, Height = 42,
                    Text = "Answer: Steam at 100\u00b0C contains an additional 22.6 \u00d7 10\u2075 J/kg of energy stored as Latent Heat of Vaporization (वाष्पीकरण की गुप्त ऊष्मा). When steam condenses upon touching the skin, it releases this extra latent energy before cooling.",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Inter"
                },

                // Q3 Card
                new PdfShapeElement { X = 40, Y = 645, Width = 720, Height = 80, CornerRadius = 6, FillColorHex = "#F8FAFC", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 652, Width = 690, Height = 22,
                    Text = "Q3. Why do camphor and dry ice directly sublime without melting? (कपूर और शुष्क बर्फ सीधे वाष्प क्यों बन जाते हैं?)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#0369A1",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 676, Width = 690, Height = 42,
                    Text = "Answer: The vapor pressure of solid CO\u2082 (dry ice) and camphor exceeds atmospheric pressure (1 atm) at room temperature. Because their triple-point pressure is higher than 1 atm, the liquid phase cannot exist stably at normal atmospheric pressure.",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Inter"
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);

        return doc;
    }
}
