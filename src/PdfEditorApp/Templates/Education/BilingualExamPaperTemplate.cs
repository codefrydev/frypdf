using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Education;

public class BilingualExamPaperTemplate : ITemplateDefinition
{
    public string Id => "bilingual_exam_paper";
    public string Name => "Bilingual Exam Paper (Hindi & English)";
    public string Description => "Dual-language school & competitive test paper with Devanagari typography, formula reference box, numbered question cards, and answer key matrix";
    public string Category => "Education";
    public string IconKind => "Translate";
    public string AccentColorHex => "#B91C1C";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Simple_Interest_Bilingual_Exam_Paper.pdf",
            Author = "Department of Competitive Examinations",
            Subject = "Bilingual Practice Exam: Simple Interest & Quantitative Aptitude"
        };

        // =========================================================================
        // PAGE 1: Header, Formulas, Questions 1 to 10
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "अभ्यास प्रश्नमाला • Practice Test Series",
            HeaderCenter = "साधारण ब्याज • Simple Interest",
            HeaderRight = "Paper Code: SI-2026",
            FooterLeft = "ACME Academy • आदर्श विद्यालय",
            FooterCenter = "Bilingual Examination Series (Hindi & English)",
            FooterRight = "Page 1 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Accent Stripe
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 800, Height = 8,
                    FillColorHex = "#B91C1C", StrokeColorHex = "#00000000"
                },

                // Institution & Exam Title Box
                new PdfShapeElement
                {
                    X = 40, Y = 35, Width = 720, Height = 95,
                    CornerRadius = 8, FillColorHex = "#FEF2F2",
                    StrokeColorHex = "#FECACA", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 45, Width = 690, Height = 28,
                    Text = "ACME Academy • आदर्श विद्यालय",
                    FontSize = 17, IsBold = true, TextColorHex = "#991B1B",
                    Alignment = TextAlignmentMode.Center,
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 75, Width = 690, Height = 24,
                    Text = "साधारण ब्याज (Simple Interest) : प्रतियोगी परीक्षाओं हेतु 50 महत्वपूर्ण प्रश्न",
                    FontSize = 13, IsBold = true, TextColorHex = "#B91C1C",
                    Alignment = TextAlignmentMode.Center,
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 102, Width = 690, Height = 20,
                    Text = "समय: 2 घण्टे (Time: 2 Hours)  |  पूर्णांक: 100 अंक (Max Marks: 100)  |  ऋणात्मक अंकन: 0.25",
                    FontSize = 10, TextColorHex = "#7F1D1D",
                    Alignment = TextAlignmentMode.Center,
                    FontFamily = "Noto Sans Devanagari"
                },

                // Student Metadata Strip
                new PdfShapeElement
                {
                    X = 40, Y = 140, Width = 720, Height = 42,
                    CornerRadius = 6, FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#CBD5E1", StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 55, Y = 152, Width = 380, Height = 20,
                    Text = "छात्र/छात्रा का नाम (Student Name): __________________________",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 450, Y = 152, Width = 150, Height = 20,
                    Text = "रोल नं (Roll No): _________",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 615, Y = 152, Width = 130, Height = 20,
                    Text = "दिनांक (Date): _______",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Important Formulas Callout Card
                new PdfShapeElement
                {
                    X = 40, Y = 192, Width = 720, Height = 88,
                    CornerRadius = 6, FillColorHex = "#FFFBEB",
                    StrokeColorHex = "#FDE68A", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 200, Width = 690, Height = 20,
                    Text = "महत्वपूर्ण सूत्र (Important Formulas):",
                    FontSize = 11, IsBold = true, TextColorHex = "#92400E",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfMathElement
                {
                    X = 55, Y = 222, Width = 690, Height = 50,
                    Formula = @"SI = \frac{P \times R \times T}{100}, \quad A = P + SI = P\left(1 + \frac{RT}{100}\right), \quad P = \frac{100 \times SI}{R \times T}, \quad R = \frac{100 \times SI}{P \times T}",
                    FontSize = 13, TextColorHex = "#78350F",
                    Category = MathCategory.SchoolArithmetic,
                    Alignment = TextAlignmentMode.Center
                },

                // Section 1 Header
                new PdfShapeElement
                {
                    X = 40, Y = 290, Width = 720, Height = 26,
                    CornerRadius = 4, FillColorHex = "#1E293B",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 295, Width = 700, Height = 18,
                    Text = "भाग 1: मूल संकल्पना एवं अभ्यास प्रश्न (Part 1: Basic Concept Problems)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Question 1 Card
                new PdfShapeElement { X = 40, Y = 325, Width = 720, Height = 70, CornerRadius = 6, FillColorHex = "#FFFFFF", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 333, Width = 690, Height = 36,
                    Text = "प्रश्न 1. \u20b95,000 की राशि पर 10% वार्षिक साधारण ब्याज की दर से 3 वर्ष का साधारण ब्याज और मिश्रधन कितना होगा?\n(Find the simple interest and total amount on \u20b95,000 at 10% per annum for 3 years.)",
                    FontSize = 10.5, TextColorHex = "#0F172A",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 372, Width = 690, Height = 18,
                    Text = "(A) \u20b91,500, \u20b96,500          (B) \u20b91,200, \u20b96,200          (C) \u20b91,800, \u20b96,800          (D) \u20b92,000, \u20b97,000",
                    FontSize = 10, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Question 2 Card
                new PdfShapeElement { X = 40, Y = 405, Width = 720, Height = 70, CornerRadius = 6, FillColorHex = "#FFFFFF", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 413, Width = 690, Height = 36,
                    Text = "प्रश्न 2. \u20b98,000 पर कितने प्रतिशत वार्षिक दर से 5 वर्ष का साधारण ब्याज \u20b92,400 हो जाएगा?\n(At what rate of simple interest per annum will \u20b98,000 yield an interest of \u20b92,400 in 5 years?)",
                    FontSize = 10.5, TextColorHex = "#0F172A",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 452, Width = 690, Height = 18,
                    Text = "(A) 5% वार्षिक                     (B) 6% वार्षिक                     (C) 8% वार्षिक                     (D) 10% वार्षिक",
                    FontSize = 10, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Question 3 Card
                new PdfShapeElement { X = 40, Y = 485, Width = 720, Height = 70, CornerRadius = 6, FillColorHex = "#FFFFFF", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 493, Width = 690, Height = 36,
                    Text = "प्रश्न 3. कोई राशि 8% वार्षिक साधारण ब्याज की दर से कितने वर्षों में स्वयं की दोगुनी हो जाएगी?\n(In how many years will a sum of money double itself at 8% simple interest per annum?)",
                    FontSize = 10.5, TextColorHex = "#0F172A",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 532, Width = 690, Height = 18,
                    Text = "(A) 10 वर्ष                        (B) 12.5 वर्ष                     (C) 15 वर्ष                        (D) 16 वर्ष",
                    FontSize = 10, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Question 4 Card
                new PdfShapeElement { X = 40, Y = 565, Width = 720, Height = 70, CornerRadius = 6, FillColorHex = "#FFFFFF", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 573, Width = 690, Height = 36,
                    Text = "प्रश्न 4. \u20b99,000 की राशि 5 वर्ष में किस ब्याज दर से \u20b913,500 हो जाएगी?\n(At what interest rate will a principal of \u20b99,000 amount to \u20b913,500 in 5 years?)",
                    FontSize = 10.5, TextColorHex = "#0F172A",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 612, Width = 690, Height = 18,
                    Text = "(A) 10%                           (B) 12%                           (C) 15%                           (D) 8%",
                    FontSize = 10, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Question 5 Card
                new PdfShapeElement { X = 40, Y = 645, Width = 720, Height = 70, CornerRadius = 6, FillColorHex = "#FFFFFF", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 653, Width = 690, Height = 36,
                    Text = "प्रश्न 5. एक राशि 3 वर्ष में \u20b98,500 और 5 वर्ष में \u20b99,500 हो जाती है। मूलधन एवं वार्षिक दर ज्ञात कीजिए।\n(A sum amounts to \u20b98,500 in 3 years and \u20b99,500 in 5 years at simple interest. Find principal and rate.)",
                    FontSize = 10.5, TextColorHex = "#0F172A",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 692, Width = 690, Height = 18,
                    Text = "(A) \u20b97,000; 7.14%             (B) \u20b97,200; 6.5%              (C) \u20b96,800; 8%                (D) \u20b97,500; 5%",
                    FontSize = 10, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                }
            }
        };

        // =========================================================================
        // PAGE 2: Part II Advanced Problems & Answer Key Table
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            HeaderLeft = "अभ्यास प्रश्नमाला • Practice Test Series",
            HeaderCenter = "उत्तरमाला एवं व्याख्या • Solutions Matrix",
            HeaderRight = "Paper Code: SI-2026",
            FooterLeft = "ACME Academy • आदर्श विद्यालय",
            FooterCenter = "Bilingual Examination Series (Hindi & English)",
            FooterRight = "Page 2 of 2",
            Elements = new List<PdfElementBase>
            {
                new PdfShapeElement
                {
                    X = 0, Y = 0, Width = 800, Height = 8,
                    FillColorHex = "#B91C1C", StrokeColorHex = "#00000000"
                },

                // Section 2 Header
                new PdfShapeElement
                {
                    X = 40, Y = 35, Width = 720, Height = 26,
                    CornerRadius = 4, FillColorHex = "#1E293B",
                    StrokeColorHex = "#00000000"
                },
                new PdfTextElement
                {
                    X = 50, Y = 40, Width = 700, Height = 18,
                    Text = "भाग 2: अनुप्रयोग एवं विभाजन समस्याएँ (Part 2: Applied Ratio & Division Problems)",
                    FontSize = 10.5, IsBold = true, TextColorHex = "#FFFFFF",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Question 6 Card
                new PdfShapeElement { X = 40, Y = 70, Width = 720, Height = 70, CornerRadius = 6, FillColorHex = "#FFFFFF", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 78, Width = 690, Height = 36,
                    Text = "प्रश्न 6. \u20b95,200 की राशि को दो भागों में इस प्रकार बांटा गया कि पहले भाग पर 8% दर से 5 वर्ष का ब्याज दूसरे भाग पर 10% दर से 4 वर्ष के ब्याज के बराबर है।\n(\u20b95,200 is divided into two parts such that the interest on the first part at 8% for 5 years equals interest on the second at 10% for 4 years.)",
                    FontSize = 10.5, TextColorHex = "#0F172A",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 117, Width = 690, Height = 18,
                    Text = "(A) \u20b92,600, \u20b92,600          (B) \u20b93,000, \u20b92,200          (C) \u20b92,800, \u20b92,400          (D) \u20b93,200, \u20b92,000",
                    FontSize = 10, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Question 7 Card
                new PdfShapeElement { X = 40, Y = 150, Width = 720, Height = 70, CornerRadius = 6, FillColorHex = "#FFFFFF", StrokeColorHex = "#E2E8F0", StrokeThickness = 1 },
                new PdfTextElement
                {
                    X = 55, Y = 158, Width = 690, Height = 36,
                    Text = "प्रश्न 7. यदि किसी धन पर 3 वर्ष का साधारण ब्याज मूलधन के 3/5 के बराबर है, तो ब्याज की वार्षिक दर क्या होगी?\n(If the simple interest on a sum of money for 3 years is equal to 3/5 of the principal, what is the annual rate?)",
                    FontSize = 10.5, TextColorHex = "#0F172A",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 197, Width = 690, Height = 18,
                    Text = "(A) 15% वार्षिक                    (B) 20% वार्षिक                    (C) 18% वार्षिक                    (D) 25% वार्षिक",
                    FontSize = 10, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Answer Key & Solutions Matrix Banner
                new PdfShapeElement
                {
                    X = 40, Y = 235, Width = 720, Height = 55,
                    CornerRadius = 6, FillColorHex = "#ECFDF5",
                    StrokeColorHex = "#A7F3D0", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 243, Width = 690, Height = 22,
                    Text = "उत्तरमाला एवं हल सारांश (Answer Key & Solutions Summary Matrix)",
                    FontSize = 13, IsBold = true, TextColorHex = "#065F46",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 267, Width = 690, Height = 18,
                    Text = "प्रतियोगी परीक्षा अभ्यास पत्रक हेतु प्रामाणिक उत्तर एवं संक्षिप्त चरण",
                    FontSize = 9.5, TextColorHex = "#047857",
                    FontFamily = "Noto Sans Devanagari"
                },

                // Answer Matrix Table
                new PdfTableElement
                {
                    X = 40, Y = 300, Width = 720, Height = 320,
                    HeaderBackgroundHex = "#065F46",
                    HeaderTextHex = "#FFFFFF",
                    BorderColorHex = "#D1D5DB",
                    AlternateRowBackgroundHex = "#F9FAFB",
                    Headers = new List<string> { "प्रश्न #", "उत्तर", "मुख्य सूत्र / चरण (Formula & Key Steps)", "अंतिम मान" },
                    Rows = new List<List<string>>
                    {
                        new() { "1", "(A)", "SI = (5000 \u00d7 10 \u00d7 3) / 100 = 1500; A = 5000 + 1500", "SI=\u20b91,500; A=\u20b96,500" },
                        new() { "2", "(B)", "R = (100 \u00d7 2400) / (8000 \u00d7 5) = 240000 / 40000 = 6%", "R = 6% वार्षिक" },
                        new() { "3", "(B)", "SI = P; T = (100 \u00d7 P) / (P \u00d7 8) = 100 / 8 = 12.5 वर्ष", "T = 12.5 वर्ष" },
                        new() { "4", "(A)", "SI = 13500 - 9000 = 4500; R = (100 \u00d7 4500) / (9000 \u00d7 5) = 10%", "R = 10% वार्षिक" },
                        new() { "5", "(A)", "2 वर्ष का ब्याज = 9500 - 8500 = 1000; 1 वर्ष का ब्याज = 500; P = 8500 - 1500", "P = \u20b97,000; R = 7.14%" },
                        new() { "6", "(A)", "x \u00d7 8 \u00d7 5 = (5200 - x) \u00d7 10 \u00d7 4 \u2192 40x = 40(5200 - x) \u2192 2x = 5200", "x = \u20b92,600 प्रत्येक" },
                        new() { "7", "(B)", "SI = 3P/5; R = (100 \u00d7 (3P/5)) / (P \u00d7 3) = (60P) / (3P) = 20%", "R = 20% वार्षिक" }
                    }
                },

                // Student Score & Performance Card
                new PdfShapeElement
                {
                    X = 40, Y = 640, Width = 720, Height = 130,
                    CornerRadius = 8, FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#CBD5E1", StrokeThickness = 1.5
                },
                new PdfTextElement
                {
                    X = 55, Y = 655, Width = 300, Height = 22,
                    Text = "मूल्यांकन एवं अंक तालिका (Evaluation & Score):",
                    FontSize = 11, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 685, Width = 690, Height = 35,
                    Text = "कुल प्रश्न: 7   |   सही उत्तर: ______   |   गलत उत्तर: ______   |   प्राप्तांक: _______ / 100\nपरीक्षक की टिप्पणी: ____________________________________________________________________",
                    FontSize = 10, TextColorHex = "#64748B",
                    FontFamily = "Noto Sans Devanagari"
                },
                new PdfTextElement
                {
                    X = 55, Y = 735, Width = 300, Height = 22,
                    Text = "परीक्षक हस्ताक्षर (Evaluator Signature): ____________________",
                    FontSize = 10, IsBold = true, TextColorHex = "#334155",
                    FontFamily = "Noto Sans Devanagari"
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);

        return doc;
    }
}
