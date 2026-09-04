using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Education;

public class MathBODMASWorksheetTemplate : ITemplateDefinition
{
    public string Id => "bodmas_worksheet";
    public string Name => "Math Practice Worksheet (BODMAS)";
    public string Description => "Comprehensive school math worksheet with student header, score card, formula callout, 30 problems, and complete answer key table";
    public string Category => "Education";
    public string IconKind => "Calculator";
    public string AccentColorHex => "#2563EB";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Class_6_BODMAS_Worksheet.pdf",
            Author = "Mathematics Department",
            Subject = "Order of Operations (BODMAS) Comprehensive Practice Worksheet"
        };

        // =========================================================================
        // PAGE 1: Header, Rule Callout, Section A & B
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "Mathematics Practice Series",
            HeaderCenter = "Grade 6 • Order of Operations",
            HeaderRight = "Worksheet #01",
            FooterLeft = "Class 6 Mathematics: BODMAS Practice Worksheet",
            FooterCenter = "Confidential - For Student Practice Only",
            FooterRight = "Page 1 of 3",
            Elements = new List<PdfElementBase>
            {
                // Top Brand Accent Header
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 800, Height = 8,
                    FillColorHex = "#2563EB", StrokeColorHex = "#00000000"
                },

                // Title Banner Box
                new PdfShapeElement
                {
                    X = 40, Y = 35, Width = 720, Height = 85,
                    CornerRadius = 8, FillColorHex = "#EFF6FF",
                    StrokeColorHex = "#BFDBFE", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 45, Width = 690, Height = 30,
                    Text = "Class 6 Mathematics: BODMAS Practice Worksheet",
                    FontSize = 18, IsBold = true, TextColorHex = "#1E3A8A",
                    FontFamily = "Inter"
                },
                new PdfTextElement
                {
                    X = 55, Y = 78, Width = 690, Height = 22,
                    Text = "Topic: Order of Operations (Brackets, Orders/Of, Division, Multiplication, Addition, Subtraction)",
                    FontSize = 11, IsItalic = true, TextColorHex = "#1D4ED8",
                    FontFamily = "Inter"
                },

                // Student Metadata Strip
                new PdfShapeElement
                {
                    X = 40, Y = 130, Width = 720, Height = 48,
                    CornerRadius = 6, FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#CBD5E1", StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 55, Y = 144, Width = 310, Height = 22,
                    Text = "Name: ___________________________________",
                    FontSize = 11, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Inter"
                },
                new PdfTextElement
                {
                    X = 380, Y = 144, Width = 180, Height = 22,
                    Text = "Date: ______________",
                    FontSize = 11, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Inter"
                },
                new PdfShapeElement
                {
                    X = 580, Y = 138, Width = 165, Height = 32,
                    CornerRadius = 4, FillColorHex = "#FEF2F2",
                    StrokeColorHex = "#F87171", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 585, Y = 145, Width = 155, Height = 20,
                    Text = "Score: _______ / 30",
                    FontSize = 11, IsBold = true, TextColorHex = "#B91C1C",
                    Alignment = TextAlignmentMode.Center,
                    FontFamily = "Inter"
                },

                // BODMAS Rule Card
                new PdfShapeElement
                {
                    X = 40, Y = 190, Width = 720, Height = 80,
                    CornerRadius = 6, FillColorHex = "#FFFBEB",
                    StrokeColorHex = "#FCD34D", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 200, Width = 690, Height = 20,
                    Text = "BODMAS RULE & ORDER OF PRECEDENCE:",
                    FontSize = 11, IsBold = true, TextColorHex = "#92400E",
                    FontFamily = "Inter"
                },
                new PdfMathElement
                {
                    X = 55, Y = 222, Width = 690, Height = 40,
                    Formula = @"\text{Brackets } [ \{ ( ) \} ] \;\longrightarrow\; \text{Orders (Powers/Roots)} \;\longrightarrow\; \text{Division } (\div) \;\longrightarrow\; \text{Multiplication } (\times) \;\longrightarrow\; \text{Addition } (+) \;\longrightarrow\; \text{Subtraction } (-)",
                    FontSize = 13, TextColorHex = "#78350F",
                    Category = MathCategory.SchoolArithmetic,
                    Alignment = TextAlignmentMode.Center
                },

                // Section A Header
                new PdfShapeElement
                {
                    X = 40, Y = 285, Width = 720, Height = 28,
                    CornerRadius = 4, FillColorHex = "#1E293B",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 290, Width = 700, Height = 20,
                    Text = "SECTION A: Basic Operations (Problems 1 to 10)",
                    FontSize = 11, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Inter"
                },

                // Questions Column 1 (Left)
                new PdfTextElement { X = 55, Y = 325, Width = 320, Height = 40, Text = "1.  12 + 8 \u00f7 4 = ______________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 55, Y = 375, Width = 320, Height = 40, Text = "2.  25 - 3 \u00d7 6 = ______________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 55, Y = 425, Width = 320, Height = 40, Text = "3.  (15 + 5) \u00d7 2 = ______________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 55, Y = 475, Width = 320, Height = 40, Text = "4.  40 \u00f7 (8 - 3) = ______________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 55, Y = 525, Width = 320, Height = 40, Text = "5.  18 + 12 \u00f7 3 \u00d7 2 = ___________", FontSize = 12, TextColorHex = "#0F172A" },

                // Questions Column 2 (Right)
                new PdfTextElement { X = 415, Y = 325, Width = 320, Height = 40, Text = "6.   36 \u00f7 4 + 5 \u00d7 3 = ___________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 375, Width = 320, Height = 40, Text = "7.   50 - 4 \u00d7 (6 + 2) = __________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 425, Width = 320, Height = 40, Text = "8.   (24 \u00f7 6) + (18 \u00f7 3) = ________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 475, Width = 320, Height = 40, Text = "9.   100 - 50 \u00f7 5 + 10 = _________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 525, Width = 320, Height = 40, Text = "10.  7 \u00d7 (12 - 4) \u00f7 2 = ___________", FontSize = 12, TextColorHex = "#0F172A" },

                // Section B Header
                new PdfShapeElement
                {
                    X = 40, Y = 580, Width = 720, Height = 28,
                    CornerRadius = 4, FillColorHex = "#1E293B",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 585, Width = 700, Height = 20,
                    Text = "SECTION B: Nested Operations & Parentheses (Problems 11 to 20)",
                    FontSize = 11, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Inter"
                },

                // Section B Column 1
                new PdfTextElement { X = 55, Y = 620, Width = 320, Height = 42, Text = "11. 30 - [12 + (8 - 5)] = _____________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 55, Y = 675, Width = 320, Height = 42, Text = "12. 4 \u00d7 [15 - (6 + 3)] = _____________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 55, Y = 730, Width = 320, Height = 42, Text = "13. 48 \u00f7 [16 - (4 \u00d7 2)] = ____________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 55, Y = 785, Width = 320, Height = 42, Text = "14. 20 + 2 \u00d7 [18 - (10 - 2)] = _________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 55, Y = 840, Width = 320, Height = 42, Text = "15. 75 - [25 + (15 - 5 \u00d7 2)] = ________", FontSize = 12, TextColorHex = "#0F172A" },

                // Section B Column 2
                new PdfTextElement { X = 415, Y = 620, Width = 320, Height = 42, Text = "16. [40 - (18 + 2)] \u00f7 4 = ____________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 675, Width = 320, Height = 42, Text = "17. 60 \u00f7 [5 + (10 - 7)] = ____________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 730, Width = 320, Height = 42, Text = "18. [8 \u00d7 (9 - 4)] - (12 + 6) = _________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 785, Width = 320, Height = 42, Text = "19. 90 - [30 + 5 \u00d7 (8 - 4)] = _________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 840, Width = 320, Height = 42, Text = "20. 2 \u00d7 [50 - (20 + 15)] + 10 = ________", FontSize = 12, TextColorHex = "#0F172A" }
            }
        };

        // =========================================================================
        // PAGE 2: Section C: Advanced Brackets & Step-by-Step Workspace
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "Mathematics Practice Series",
            HeaderCenter = "Grade 6 • Order of Operations",
            HeaderRight = "Worksheet #01",
            FooterLeft = "Class 6 Mathematics: BODMAS Practice Worksheet",
            FooterCenter = "Confidential - For Student Practice Only",
            FooterRight = "Page 2 of 3",
            Elements = new List<PdfElementBase>
            {
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 800, Height = 8,
                    FillColorHex = "#2563EB", StrokeColorHex = "#00000000"
                },

                // Section C Header
                new PdfShapeElement
                {
                    X = 40, Y = 35, Width = 720, Height = 28,
                    CornerRadius = 4, FillColorHex = "#1E293B",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 40, Width = 700, Height = 20,
                    Text = "SECTION C: Advanced Multi-Level Brackets & Complex Expressions (Problems 21 to 30)",
                    FontSize = 11, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Inter"
                },

                // Questions 21 to 30 with working space
                new PdfTextElement { X = 55, Y = 75, Width = 320, Height = 45, Text = "21.  45 - [18 + (12 - 6)] = ___________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 75, Width = 320, Height = 45, Text = "22.  2 \u00d7 [30 - (5 + 3 \u00d7 4)] = _________", FontSize = 12, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 55, Y = 130, Width = 320, Height = 45, Text = "23.  100 - {20 + [15 - (8 - 3)]} = ______", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 130, Width = 320, Height = 45, Text = "24.  [64 \u00f7 (16 - 8)] \u00d7 [3 + 2] = ________", FontSize = 12, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 55, Y = 185, Width = 320, Height = 45, Text = "25.  {50 - [10 + (25 - 15)]} \u00f7 6 = ______", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 185, Width = 320, Height = 45, Text = "26.  120 \u00f7 {15 + [12 - (7 - 2)]} = ______", FontSize = 12, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 55, Y = 240, Width = 320, Height = 45, Text = "27.  [80 - (25 + 15)] \u00f7 [4 \u00d7 (5 - 3)] = __", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 240, Width = 320, Height = 45, Text = "28.  25 + {35 - [20 + (10 - 5)]} = ______", FontSize = 12, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 55, Y = 295, Width = 320, Height = 45, Text = "29.  3 \u00d7 {18 - [14 - (8 - 4)]} = ________", FontSize = 12, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 415, Y = 295, Width = 320, Height = 45, Text = "30.  {100 - [40 + (15 - 5) \u00d7 2]} = _____", FontSize = 12, TextColorHex = "#0F172A" },

                // Rough Working Space Box
                new PdfShapeElement
                {
                    X = 40, Y = 360, Width = 720, Height = 580,
                    CornerRadius = 8, FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#CBD5E1", StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 55, Y = 375, Width = 690, Height = 22,
                    Text = "ROUGH WORKING SPACE / STEP-BY-STEP SOLUTION SCRATCHPAD",
                    FontSize = 11, IsBold = true, TextColorHex = "#64748B",
                    FontFamily = "Inter"
                },
                new PdfDividerElement
                {
                    X = 55, Y = 405, Width = 690, Height = 2,
                    ColorHex = "#E2E8F0"
                }
            }
        };

        // =========================================================================
        // PAGE 3: Answer Key & Solutions Summary Table
        // =========================================================================
        var page3 = new PdfPageModel
        {
            PageNumber = 3,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "Mathematics Practice Series",
            HeaderCenter = "Grade 6 • Solutions Summary",
            HeaderRight = "Worksheet #01 - Key",
            FooterLeft = "Class 6 Mathematics: BODMAS Practice Worksheet",
            FooterCenter = "Confidential - Teacher & Grading Key",
            FooterRight = "Page 3 of 3",
            Elements = new List<PdfElementBase>
            {
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 800, Height = 8,
                    FillColorHex = "#10B981", StrokeColorHex = "#00000000"
                },

                // Answer Key Banner
                new PdfShapeElement
                {
                    X = 40, Y = 35, Width = 720, Height = 65,
                    CornerRadius = 8, FillColorHex = "#ECFDF5",
                    StrokeColorHex = "#A7F3D0", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 45, Width = 690, Height = 26,
                    Text = "Answer Key & Solutions Summary",
                    FontSize = 16, IsBold = true, TextColorHex = "#065F46",
                    FontFamily = "Inter"
                },
                new PdfTextElement
                {
                    X = 55, Y = 73, Width = 690, Height = 20,
                    Text = "Complete numerical answers and evaluation guide for Class 6 BODMAS Practice Worksheet",
                    FontSize = 10, TextColorHex = "#047857",
                    FontFamily = "Inter"
                },

                // Solutions Table
                new PdfTableElement
                {
                    X = 40, Y = 120, Width = 720, Height = 620,
                    HeaderBackgroundHex = "#047857",
                    HeaderTextHex = "#FFFFFF",
                    BorderColorHex = "#D1D5DB",
                    AlternateRowBackgroundHex = "#F9FAFB",
                    Headers = new List<string> { "Q#", "Expression", "Answer", "Q#", "Expression", "Answer" },
                    Rows = new List<List<string>>
                    {
                        new() { "1", "12 + 8 \u00f7 4", "14", "16", "[40 - (18 + 2)] \u00f7 4", "5" },
                        new() { "2", "25 - 3 \u00d7 6", "7", "17", "60 \u00f7 [5 + (10 - 7)]", "7.5" },
                        new() { "3", "(15 + 5) \u00d7 2", "40", "18", "[8 \u00d7 (9 - 4)] - (12 + 6)", "22" },
                        new() { "4", "40 \u00f7 (8 - 3)", "8", "19", "90 - [30 + 5 \u00d7 (8 - 4)]", "40" },
                        new() { "5", "18 + 12 \u00f7 3 \u00d7 2", "26", "20", "2 \u00d7 [50 - 35] + 10", "40" },
                        new() { "6", "36 \u00f7 4 + 5 \u00d7 3", "24", "21", "45 - [18 + (12 - 6)]", "21" },
                        new() { "7", "50 - 4 \u00d7 (6 + 2)", "18", "22", "2 \u00d7 [30 - (5 + 12)]", "26" },
                        new() { "8", "(24 \u00f7 6) + (18 \u00f7 3)", "10", "23", "100 - {20 + [15 - 5]}", "70" },
                        new() { "9", "100 - 50 \u00f7 5 + 10", "100", "24", "[64 \u00f7 8] \u00d7 5", "40" },
                        new() { "10", "7 \u00d7 (12 - 4) \u00f7 2", "28", "25", "{50 - [10 + 10]} \u00f7 6", "5" },
                        new() { "11", "30 - [12 + 3]", "15", "26", "120 \u00f7 {15 + [12 - 5]}", "5.45" },
                        new() { "12", "4 \u00d7 [15 - 9]", "24", "27", "[80 - 40] \u00f7 [4 \u00d7 2]", "5" },
                        new() { "13", "48 \u00f7 [16 - 8]", "6", "28", "25 + {35 - [20 + 5]}", "35" },
                        new() { "14", "20 + 2 \u00d7 [18 - 8]", "40", "29", "3 \u00d7 {18 - [14 - 4]}", "24" },
                        new() { "15", "75 - [25 + 5]", "45", "30", "100 - [40 + 20]", "40" }
                    }
                },

                // Teacher Feedback & Signature Card
                new PdfShapeElement
                {
                    X = 40, Y = 770, Width = 720, Height = 150,
                    CornerRadius = 8, FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#CBD5E1", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 785, Width = 300, Height = 22,
                    Text = "Teacher Remarks & Feedback:",
                    FontSize = 11, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Inter"
                },
                new PdfTextElement
                {
                    X = 55, Y = 815, Width = 690, Height = 45,
                    Text = "[  ] Excellent Mastery       [  ] Needs Practice with Nested Brackets       [  ] Re-test Recommended\nComments: ___________________________________________________________________________________",
                    FontSize = 10, TextColorHex = "#64748B",
                    FontFamily = "Inter"
                },
                new PdfTextElement
                {
                    X = 55, Y = 880, Width = 300, Height = 22,
                    Text = "Teacher Signature: _______________________",
                    FontSize = 11, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Inter"
                },
                new PdfTextElement
                {
                    X = 450, Y = 880, Width = 280, Height = 22,
                    Text = "Grading Date: ____________________",
                    FontSize = 11, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Inter"
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);
        doc.Pages.Add(page3);

        return doc;
    }
}
