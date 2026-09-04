using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Services;

namespace PdfEditorApp.Templates.Education;

public class QuadrilateralsGuideTemplate : ITemplateDefinition
{
    public string Id => "quadrilaterals_guide";
    public string Name => "Geometry Guide (Quadrilaterals)";
    public string Description => "Comprehensive bilingual geometry guide with vector quadrilateral illustrations, English & Hindi properties, and area/perimeter formula reference";
    public string Category => "Education";
    public string IconKind => "VectorSquare";
    public string AccentColorHex => "#7C3AED";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Types_of_Quadrilaterals_Guide.pdf",
            Author = "Mathematics & Geometry Department",
            Subject = "Bilingual Illustrated Guide to Quadrilaterals and Geometric Properties"
        };

        // =========================================================================
        // PAGE 1: Overview, Vector Diagrams, Parallelogram & Rectangle
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "Geometry Visual Reference Series",
            HeaderCenter = "Polygons & Quadrilaterals • चतुर्भुज",
            HeaderRight = "Study Sheet #04",
            FooterLeft = "Types of Quadrilaterals • चतुर्भुज के प्रकार (Bilingual Guide)",
            FooterCenter = "Licensed for Classroom & Educational Distribution",
            FooterRight = "Page 1 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Brand Stripe
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 800, Height = 8,
                    FillColorHex = "#7C3AED", StrokeColorHex = "#00000000"
                },

                // Title Banner Box
                new PdfShapeElement
                {
                    X = 40, Y = 35, Width = 720, Height = 80,
                    CornerRadius = 8, FillColorHex = "#F5F3FF",
                    StrokeColorHex = "#DDD6FE", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 45, Width = 690, Height = 28,
                    Text = "Types of Quadrilaterals • चतुर्भुज के प्रकार",
                    FontSize = 17, IsBold = true, TextColorHex = "#5B21B6",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 75, Width = 690, Height = 22,
                    Text = "Bilingual Visual Guide in English & Hindi with Geometric Properties, Angle Rules & Formulas",
                    FontSize = 10.5, IsItalic = true, TextColorHex = "#6D28D9",
                    FontFamily = "Inter"
                },

                // Definition Card
                new PdfShapeElement
                {
                    X = 40, Y = 125, Width = 720, Height = 65,
                    CornerRadius = 6, FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#CBD5E1", StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 55, Y = 133, Width = 690, Height = 18,
                    Text = "WHAT IS A QUADRILATERAL? (चतुर्भुज क्या है?)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 153, Width = 690, Height = 32,
                    Text = "• English: A closed 2D plane geometric figure bounded by four straight sides, four angles, and four vertices.\n• Hindi: चार सीधी भुजाओं, चार कोणों और चार शीर्षों से घिरी हुई बंद समतल आकृति को चतुर्भुज कहते हैं।",
                    FontSize = 9.5, TextColorHex = "#475569",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Angle Sum Property Card
                new PdfShapeElement
                {
                    X = 40, Y = 200, Width = 720, Height = 65,
                    CornerRadius = 6, FillColorHex = "#FFFBEB",
                    StrokeColorHex = "#FDE68A", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 208, Width = 690, Height = 18,
                    Text = "ANGLE SUM PROPERTY OF A QUADRILATERAL (कोण योग गुणधर्म):",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#92400E",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfMathElement
                {
                    X = 55, Y = 228, Width = 690, Height = 32,
                    Formula = @"\angle A + \angle B + \angle C + \angle D = 360^\circ \quad \text{(Four right angles: } 4 \times 90^\circ = 360^\circ\text{)}",
                    FontSize = 12, TextColorHex = "#78350F",
                    Category = MathCategory.Geometry,
                    Alignment = TextAlignmentMode.Center
                },

                // Quadrilateral Visual Diagram Set
                new PdfSvgElement
                {
                    X = 50, Y = 275, Width = 700, Height = 200,
                    SvgSource = SvgOrnamentLibrary.GetQuadrilateralSetDiagramSvg(),
                    PresetName = "Quadrilaterals Geometry Diagram"
                },

                // 1. Parallelogram Card
                new PdfShapeElement { X = 40, Y = 490, Width = 720, Height = 135, CornerRadius = 6, FillColorHex = "#F8FAFC", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 498, Width = 690, Height = 22,
                    Text = "1. Parallelogram (समांतर चतुर्भुज)",
                    FontSize = 12, IsBold = true, TextColorHex = "#1E40AF",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 522, Width = 690, Height = 95,
                    Text = "• English Properties:\n   - Opposite sides are equal and parallel (AB = CD, AD = BC).\n   - Opposite angles are equal (\u2220A = \u2220C, \u2220B = \u2220D). Adjacent angles are supplementary (\u2220A + \u2220B = 180\u00b0).\n   - Diagonals bisect each other into two equal halves.\n• Hindi Properties (हिंदी गुणधर्म):\n   - आमने-सामने की भुजाएं समान और समानांतर होती हैं। सम्मुख कोण बराबर होते हैं और आसन्न कोण संपूरक (180\u00b0) होते हैं।\n   - विकर्ण एक-दूसरे को समद्विभाजित (bisect) करते हैं।  |  क्षेत्रफल (Area) = b \u00d7 h",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // 2. Rectangle Card
                new PdfShapeElement { X = 40, Y = 635, Width = 720, Height = 135, CornerRadius = 6, FillColorHex = "#F8FAFC", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 643, Width = 690, Height = 22,
                    Text = "2. Rectangle (आयत)",
                    FontSize = 12, IsBold = true, TextColorHex = "#1E40AF",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 667, Width = 690, Height = 95,
                    Text = "• English Properties:\n   - A parallelogram whose each internal angle is exactly 90\u00b0 (\u2220A = \u2220B = \u2220C = \u2220D = 90\u00b0).\n   - Opposite sides are equal and parallel.\n   - Both diagonals are equal in length and bisect each other (AC = BD).\n• Hindi Properties (हिंदी गुणधर्म):\n   - प्रत्येक अंतःकोण समकोण (90\u00b0) होता है। सम्मुख भुजाएं समान एवं समानांतर होती हैं।\n   - दोनों विकर्ण लंबाई में समान होते हैं।  |  क्षेत्रफल (Area) = l \u00d7 b  |  परिमाप (Perimeter) = 2(l + b)",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                }
            }
        };

        // =========================================================================
        // PAGE 2: Rhombus, Square, Trapezoid, Kite & Formulas Table
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "Geometry Visual Reference Series",
            HeaderCenter = "Polygons & Quadrilaterals • चतुर्भुज",
            HeaderRight = "Study Sheet #04",
            FooterLeft = "Types of Quadrilaterals • चतुर्भुज के प्रकार (Bilingual Guide)",
            FooterCenter = "Licensed for Classroom & Educational Distribution",
            FooterRight = "Page 2 of 2",
            Elements = new List<PdfElementBase>
            {
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 800, Height = 8,
                    FillColorHex = "#7C3AED", StrokeColorHex = "#00000000"
                },

                // 3. Rhombus Card
                new PdfShapeElement { X = 40, Y = 35, Width = 720, Height = 115, CornerRadius = 6, FillColorHex = "#F8FAFC", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 43, Width = 690, Height = 22,
                    Text = "3. Rhombus (समचतुर्भुज)",
                    FontSize = 12, IsBold = true, TextColorHex = "#991B1B",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 67, Width = 690, Height = 75,
                    Text = "• English: All four sides are equal (AB = BC = CD = DA). Diagonals bisect each other at right angles (90\u00b0).\n• Hindi: चारों भुजाएं बराबर होती हैं। विकर्ण एक-दूसरे को परस्पर समकोण (90\u00b0) पर समद्विभाजित करते हैं।\n• सूत्र (Formulas): Area = \u00bd \u00d7 d\u2081 \u00d7 d\u2082   |   Perimeter = 4a",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // 4. Square Card
                new PdfShapeElement { X = 40, Y = 160, Width = 720, Height = 115, CornerRadius = 6, FillColorHex = "#F8FAFC", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 168, Width = 690, Height = 22,
                    Text = "4. Square (वर्ग)",
                    FontSize = 12, IsBold = true, TextColorHex = "#15803D",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 192, Width = 690, Height = 75,
                    Text = "• English: A regular quadrilateral where all 4 sides are equal and all 4 angles are 90\u00b0. Diagonals are equal and perpendicular.\n• Hindi: सभी 4 भुजाएं बराबर और सभी कोण 90\u00b0 के होते हैं। दोनों विकर्ण बराबर होते हैं और 90\u00b0 पर काटते हैं।\n• सूत्र (Formulas): Area = a\u00b2   |   Perimeter = 4a   |   Diagonal = a\u221a2",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // 5. Trapezoid Card
                new PdfShapeElement { X = 40, Y = 285, Width = 720, Height = 115, CornerRadius = 6, FillColorHex = "#F8FAFC", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 293, Width = 690, Height = 22,
                    Text = "5. Trapezoid / Trapezium (समलंब चतुर्भुज)",
                    FontSize = 12, IsBold = true, TextColorHex = "#047857",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 317, Width = 690, Height = 75,
                    Text = "• English: Has only one pair of parallel opposite sides called bases. Non-parallel sides are called legs.\n• Hindi: सम्मुख भुजाओं का केवल एक युग्म समानांतर होता है। समानांतर भुजाओं के बीच लंबवत दूरी ऊंचाई (h) कहलाती है।\n• सूत्र (Formulas): Area = \u00bd(a + b) \u00d7 h",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // 6. Kite Card
                new PdfShapeElement { X = 40, Y = 410, Width = 720, Height = 115, CornerRadius = 6, FillColorHex = "#F8FAFC", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 418, Width = 690, Height = 22,
                    Text = "6. Kite (पतंग रूपी चतुर्भुज)",
                    FontSize = 12, IsBold = true, TextColorHex = "#B45309",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 442, Width = 690, Height = 75,
                    Text = "• English: Two distinct pairs of adjacent sides are equal. Diagonals intersect at right angles, and the longer diagonal bisects the shorter.\n• Hindi: आसन्न भुजाओं के दो अलग-अलग जोड़े बराबर होते हैं। विकर्ण परस्पर समकोण (90\u00b0) पर प्रतिच्छेद करते हैं।\n• सूत्र (Formulas): Area = \u00bd \u00d7 d\u2081 \u00d7 d\u2082",
                    FontSize = 9.5, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Summary Table Header
                new PdfShapeElement
                {
                    X = 40, Y = 540, Width = 720, Height = 26,
                    CornerRadius = 4, FillColorHex = "#1E293B",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 545, Width = 700, Height = 18,
                    Text = "FORMULA SUMMARY: PERIMETER & AREA (सूत्र संक्षेप तालिका)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Summary Table
                new PdfTableElement
                {
                    X = 40, Y = 575, Width = 720, Height = 260,
                    HeaderBackgroundHex = "#5B21B6",
                    HeaderTextHex = "#FFFFFF",
                    BorderColorHex = "#CBD5E1",
                    AlternateRowBackgroundHex = "#F5F3FF",
                    Headers = new List<string> { "Quadrilateral (आकृति)", "Opposite Sides", "Diagonals (विकर्ण)", "Perimeter (परिमाप)", "Area (क्षेत्रफल)" },
                    Rows = new List<List<string>>
                    {
                        new() { "Square (वर्ग)", "Parallel & Equal", "Equal & \u22a5 (90\u00b0)", "4a", "a\u00b2" },
                        new() { "Rectangle (आयत)", "Parallel & Equal", "Equal, not \u22a5", "2(l + b)", "l \u00d7 b" },
                        new() { "Parallelogram (समांतर)", "Parallel & Equal", "Bisect each other", "2(a + b)", "b \u00d7 h" },
                        new() { "Rhombus (समचतुर्भुज)", "Parallel & Equal (4)", "Unequal & \u22a5 (90\u00b0)", "4a", "\u00bd \u00d7 d\u2081 \u00d7 d\u2082" },
                        new() { "Trapezoid (समलंब)", "1 Pair Parallel", "Unequal", "a + b + c + d", "\u00bd(a + b) \u00d7 h" },
                        new() { "Kite (पतंग)", "Adjacent Equal", "Perpendicular (\u22a5)", "2(a + b)", "\u00bd \u00d7 d\u2081 \u00d7 d\u2082" }
                    }
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);

        return doc;
    }
}
