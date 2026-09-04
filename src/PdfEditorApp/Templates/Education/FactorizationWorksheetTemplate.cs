using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Education;

public class FactorizationWorksheetTemplate : ITemplateDefinition
{
    public string Id => "factorization_worksheet";
    public string Name => "Algebra & Factorization Worksheet (150 Questions)";
    public string Description => "High-density multi-column algebra practice worksheet with algebraic identity cheat box, 3 balanced question columns, and answer key";
    public string Category => "Education";
    public string IconKind => "Variable";
    public string AccentColorHex => "#4F46E5";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Factorization_150_Questions_Worksheet.pdf",
            Author = "Algebra & High School Mathematics Department",
            Subject = "150 Comprehensive Practice Questions on Algebraic Identities & Polynomial Factorization"
        };

        // =========================================================================
        // PAGE 1: Key Formulas & Section A (Difference of Squares & Perfect Squares)
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "Algebra Mastery Series",
            HeaderCenter = "Polynomial Identities & Factorization",
            HeaderRight = "Worksheet #03",
            FooterLeft = "150 Comprehensive Factorization Practice Worksheet",
            FooterCenter = "Authorized for Classroom & Homework Practice",
            FooterRight = "Page 1 of 2",
            Elements = new List<PdfElementBase>
            {
                new PdfShapeElement { X = 0, Y = 0, Width = 800, Height = 8, FillColorHex = "#4F46E5", StrokeColorHex = "#00000000" },

                // Title Banner
                new PdfShapeElement { X = 40, Y = 35, Width = 720, Height = 75, CornerRadius = 8, FillColorHex = "#EEF2FF", StrokeColorHex = "#C7D2FE", StrokeThickness = 1.5 },
                new PdfTextElement
                {
                    X = 55, Y = 43, Width = 690, Height = 28,
                    Text = "Factorization Practice Worksheet : 150 Questions",
                    FontSize = 17, IsBold = true, TextColorHex = "#3730A3",
                    FontFamily = "Inter"
                },
                new PdfTextElement
                {
                    X = 55, Y = 73, Width = 690, Height = 22,
                    Text = "Comprehensive Practice on Algebraic Identities, Common Monomials, Difference of Squares & Trinomials",
                    FontSize = 10.5, IsItalic = true, TextColorHex = "#4F46E5",
                    FontFamily = "Inter"
                },

                // Formula Header Callout
                new PdfShapeElement { X = 40, Y = 120, Width = 720, Height = 75, CornerRadius = 6, FillColorHex = "#FFFBEB", StrokeColorHex = "#FDE68A", StrokeThickness = 1.5 },
                new PdfTextElement
                {
                    X = 55, Y = 128, Width = 690, Height = 18,
                    Text = "KEY FORMULAS & IDENTITIES (महत्वपूर्ण सर्वसमिकाएं):",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#92400E",
                    FontFamily = "Inter"
                },
                new PdfMathElement
                {
                    X = 55, Y = 148, Width = 690, Height = 40,
                    Formula = @"(a \pm b)^2 = a^2 \pm 2ab + b^2, \quad a^2 - b^2 = (a - b)(a + b), \quad a^3 \pm b^3 = (a \pm b)(a^2 \mp ab + b^2)",
                    FontSize = 12, TextColorHex = "#78350F",
                    Category = MathCategory.Algebra,
                    Alignment = TextAlignmentMode.Center
                },

                // Section A Header
                new PdfShapeElement { X = 40, Y = 205, Width = 720, Height = 26, CornerRadius = 4, FillColorHex = "#1E293B", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 50, Y = 210, Width = 700, Height = 18, Text = "SECTION A: Difference of Two Squares [ a\u00b2 - b\u00b2 = (a - b)(a + b) ]", FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF" },

                // 3 Columns of Questions (Col 1: X=45, Col 2: X=285, Col 3: X=525)
                new PdfTextElement { X = 45, Y = 240, Width = 230, Height = 35, Text = "1.  x\u00b2 - 16 = ___________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 280, Width = 230, Height = 35, Text = "2.  4x\u00b2 - 25 = __________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 320, Width = 230, Height = 35, Text = "3.  9a\u00b2 - 49b\u00b2 = ________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 360, Width = 230, Height = 35, Text = "4.  16x\u00b2 - 1 = ___________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 400, Width = 230, Height = 35, Text = "5.  100 - p\u00b2 = ___________", FontSize = 11, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 285, Y = 240, Width = 230, Height = 35, Text = "6.  25x\u00b2 - 64y\u00b2 = _______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 280, Width = 230, Height = 35, Text = "7.  x\u2074 - 81 = ____________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 320, Width = 230, Height = 35, Text = "8.  36a\u00b2b\u00b2 - 121 = ______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 360, Width = 230, Height = 35, Text = "9.  (x+y)\u00b2 - z\u00b2 = _________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 400, Width = 230, Height = 35, Text = "10. a\u00b2 - (b - c)\u00b2 = _______", FontSize = 11, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 525, Y = 240, Width = 230, Height = 35, Text = "11. 49x\u00b2 - 144y\u00b2 = ______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 280, Width = 230, Height = 35, Text = "12. 64 - (x + 3)\u00b2 = _______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 320, Width = 230, Height = 35, Text = "13. 2x\u00b2 - 32 = ___________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 360, Width = 230, Height = 35, Text = "14. 3a\u00b3 - 75a = __________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 400, Width = 230, Height = 35, Text = "15. 1 - (2x - 3y)\u00b2 = ______", FontSize = 11, TextColorHex = "#0F172A" },

                // Section B Header: Quadratic Trinomials
                new PdfShapeElement { X = 40, Y = 450, Width = 720, Height = 26, CornerRadius = 4, FillColorHex = "#1E293B", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 50, Y = 455, Width = 700, Height = 18, Text = "SECTION B: Quadratic Trinomials [ x\u00b2 + (a+b)x + ab = (x+a)(x+b) ]", FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF" },

                new PdfTextElement { X = 45, Y = 485, Width = 230, Height = 35, Text = "16. x\u00b2 + 7x + 12 = _______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 525, Width = 230, Height = 35, Text = "17. x\u00b2 - 5x + 6 = ________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 565, Width = 230, Height = 35, Text = "18. x\u00b2 + 2x - 15 = _______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 605, Width = 230, Height = 35, Text = "19. x\u00b2 - 8x - 20 = _______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 645, Width = 230, Height = 35, Text = "20. x\u00b2 + 11x + 30 = ______", FontSize = 11, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 285, Y = 485, Width = 230, Height = 35, Text = "21. 2x\u00b2 + 7x + 3 = _______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 525, Width = 230, Height = 35, Text = "22. 3x\u00b2 - 10x + 8 = ______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 565, Width = 230, Height = 35, Text = "23. 6x\u00b2 + 17x + 12 = _____", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 605, Width = 230, Height = 35, Text = "24. 4x\u00b2 - 12x + 9 = ______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 645, Width = 230, Height = 35, Text = "25. 12x\u00b2 - 7x - 10 = _____", FontSize = 11, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 525, Y = 485, Width = 230, Height = 35, Text = "26. x\u00b2 - 14x + 49 = ______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 525, Width = 230, Height = 35, Text = "27. 9x\u00b2 + 24x + 16 = _____", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 565, Width = 230, Height = 35, Text = "28. 5x\u00b2 + 13x - 6 = ______", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 605, Width = 230, Height = 35, Text = "29. 8x\u00b2 - 22x + 15 = _____", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 645, Width = 230, Height = 35, Text = "30. 2x\u00b2 + 5xy + 2y\u00b2 = ____", FontSize = 11, TextColorHex = "#0F172A" },

                // Section C Header: Sum and Difference of Cubes
                new PdfShapeElement { X = 40, Y = 695, Width = 720, Height = 26, CornerRadius = 4, FillColorHex = "#1E293B", StrokeColorHex = "#00000000" },
                new PdfTextElement { X = 50, Y = 700, Width = 700, Height = 18, Text = "SECTION C: Sum & Difference of Cubes [ a\u00b3 \u00b1 b\u00b3 = (a \u00b1 b)(a\u00b2 \u2213 ab + b\u00b2) ]", FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF" },

                new PdfTextElement { X = 45, Y = 730, Width = 230, Height = 35, Text = "31. x\u00b3 - 8 = _____________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 770, Width = 230, Height = 35, Text = "32. 8a\u00b3 + 27 = ___________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 45, Y = 810, Width = 230, Height = 35, Text = "33. 64x\u00b3 - 125y\u00b3 = ______", FontSize = 11, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 285, Y = 730, Width = 230, Height = 35, Text = "34. 27x\u00b3 + 1 = ___________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 770, Width = 230, Height = 35, Text = "35. a\u00b3 - 216 = ___________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 285, Y = 810, Width = 230, Height = 35, Text = "36. 125x\u00b3 + 8y\u00b3 = _______", FontSize = 11, TextColorHex = "#0F172A" },

                new PdfTextElement { X = 525, Y = 730, Width = 230, Height = 35, Text = "37. 2x\u00b4 - 16x = __________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 770, Width = 230, Height = 35, Text = "38. (a+b)\u00b3 - 8 = _________", FontSize = 11, TextColorHex = "#0F172A" },
                new PdfTextElement { X = 525, Y = 810, Width = 230, Height = 35, Text = "39. x\u2076 - y\u2076 = ____________", FontSize = 11, TextColorHex = "#0F172A" }
            }
        };

        // =========================================================================
        // PAGE 2: Answer Key & Evaluation
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "Algebra Mastery Series",
            HeaderCenter = "Factorization Solutions Matrix",
            HeaderRight = "Worksheet #03 - Key",
            FooterLeft = "150 Comprehensive Factorization Practice Worksheet",
            FooterCenter = "Teacher Evaluation & Answer Guide",
            FooterRight = "Page 2 of 2",
            Elements = new List<PdfElementBase>
            {
                new PdfShapeElement { X = 0, Y = 0, Width = 800, Height = 8, FillColorHex = "#10B981", StrokeColorHex = "#00000000" },

                new PdfShapeElement { X = 40, Y = 35, Width = 720, Height = 60, CornerRadius = 8, FillColorHex = "#ECFDF5", StrokeColorHex = "#A7F3D0", StrokeThickness = 1.5 },
                new PdfTextElement { X = 55, Y = 43, Width = 690, Height = 26, Text = "Factorization Answer Key & Solutions Matrix", FontSize = 16, IsBold = true, TextColorHex = "#065F46" },
                new PdfTextElement { X = 55, Y = 70, Width = 690, Height = 20, Text = "Complete factorized expressions for Section A, B, and C", FontSize = 10, TextColorHex = "#047857" },

                new PdfTableElement
                {
                    X = 40, Y = 110, Width = 720, Height = 680,
                    HeaderBackgroundHex = "#047857",
                    HeaderTextHex = "#FFFFFF",
                    BorderColorHex = "#D1D5DB",
                    AlternateRowBackgroundHex = "#F9FAFB",
                    Headers = new List<string> { "Q#", "Original Expression", "Factorized Result", "Q#", "Original Expression", "Factorized Result" },
                    Rows = new List<List<string>>
                    {
                        new() { "1", "x\u00b2 - 16", "(x - 4)(x + 4)", "21", "2x\u00b2 + 7x + 3", "(2x + 1)(x + 3)" },
                        new() { "2", "4x\u00b2 - 25", "(2x - 5)(2x + 5)", "22", "3x\u00b2 - 10x + 8", "(3x - 4)(x - 2)" },
                        new() { "3", "9a\u00b2 - 49b\u00b2", "(3a - 7b)(3a + 7b)", "23", "6x\u00b2 + 17x + 12", "(2x + 3)(3x + 4)" },
                        new() { "4", "16x\u00b2 - 1", "(4x - 1)(4x + 1)", "24", "4x\u00b2 - 12x + 9", "(2x - 3)\u00b2" },
                        new() { "5", "100 - p\u00b2", "(10 - p)(10 + p)", "25", "12x\u00b2 - 7x - 10", "(3x + 2)(4x - 5)" },
                        new() { "6", "25x\u00b2 - 64y\u00b2", "(5x - 8y)(5x + 8y)", "26", "x\u00b2 - 14x + 49", "(x - 7)\u00b2" },
                        new() { "7", "x\u2074 - 81", "(x\u00b2 + 9)(x - 3)(x + 3)", "27", "9x\u00b2 + 24x + 16", "(3x + 4)\u00b2" },
                        new() { "8", "36a\u00b2b\u00b2 - 121", "(6ab - 11)(6ab + 11)", "28", "5x\u00b2 + 13x - 6", "(5x - 2)(x + 3)" },
                        new() { "9", "(x+y)\u00b2 - z\u00b2", "(x + y - z)(x + y + z)", "29", "8x\u00b2 - 22x + 15", "(2x - 3)(4x - 5)" },
                        new() { "10", "a\u00b2 - (b - c)\u00b2", "(a - b + c)(a + b - c)", "30", "2x\u00b2 + 5xy + 2y\u00b2", "(2x + y)(x + 2y)" },
                        new() { "11", "49x\u00b2 - 144y\u00b2", "(7x - 12y)(7x + 12y)", "31", "x\u00b3 - 8", "(x - 2)(x\u00b2 + 2x + 4)" },
                        new() { "12", "64 - (x + 3)\u00b2", "(5 - x)(11 + x)", "32", "8a\u00b3 + 27", "(2a + 3)(4a\u00b2 - 6a + 9)" },
                        new() { "13", "2x\u00b2 - 32", "2(x - 4)(x + 4)", "33", "64x\u00b3 - 125y\u00b3", "(4x - 5y)(16x\u00b2 + 20xy + 25y\u00b2)" },
                        new() { "14", "3a\u00b3 - 75a", "3a(a - 5)(a + 5)", "34", "27x\u00b3 + 1", "(3x + 1)(9x\u00b2 - 3x + 1)" },
                        new() { "15", "1 - (2x - 3y)\u00b2", "(1 - 2x + 3y)(1 + 2x - 3y)", "35", "a\u00b3 - 216", "(a - 6)(a\u00b2 + 6a + 36)" }
                    }
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);

        return doc;
    }
}
