using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.Services.MathEngine;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.Tests;

public class MathEngineTests
{
    [Fact]
    public void Tokenizer_Should_Tokenize_Basic_Formula()
    {
        string latex = @"x^2 + \frac{1}{2} = 0";
        var tokens = MathLayoutEngine.Tokenize(latex);

        Assert.NotEmpty(tokens);
        Assert.Contains(tokens, t => t.Type == MathLayoutEngine.TokenType.Command && t.Value == "frac");
        Assert.Contains(tokens, t => t.Type == MathLayoutEngine.TokenType.Superscript);
        Assert.Contains(tokens, t => t.Type == MathLayoutEngine.TokenType.Operator && t.Value == "+");
    }

    [Fact]
    public void Tokenizer_Should_Tokenize_Greek_And_Special_Operators()
    {
        string latex = @"\int_{-\infty}^{\infty} e^{-\alpha x^2} \, dx = \sqrt{\frac{\pi}{\alpha}}";
        var tokens = MathLayoutEngine.Tokenize(latex);

        Assert.Contains(tokens, t => t.Type == MathLayoutEngine.TokenType.Command && t.Value == "int");
        Assert.Contains(tokens, t => t.Type == MathLayoutEngine.TokenType.Command && t.Value == "alpha");
        Assert.Contains(tokens, t => t.Type == MathLayoutEngine.TokenType.Command && t.Value == "sqrt");
        Assert.Contains(tokens, t => t.Type == MathLayoutEngine.TokenType.Command && t.Value == "pi");
        Assert.Contains(tokens, t => t.Type == MathLayoutEngine.TokenType.Command && t.Value == "infty");
    }

    [Fact]
    public void Parser_Should_Parse_Fractions_And_Radicals()
    {
        string latex = @"\frac{-b \pm \sqrt{b^2 - 4ac}}{2a}";
        var tokens = MathLayoutEngine.Tokenize(latex);
        var parser = new MathLayoutEngine.MathParser(tokens);
        var ast = parser.ParseExpression();

        Assert.NotNull(ast);
        var ctx = new MathLayoutEngine.LayoutContext(16, "#000000", MathDisplayStyle.DisplayBlock);
        ast.Measure(ctx);

        Assert.True(ast.Width > 0);
        Assert.True(ast.Height > 0);
    }

    [Fact]
    public void Parser_Should_Parse_Large_Operators_With_Limits()
    {
        string latex = @"\sum_{i=1}^{n} i^2 = \frac{n(n+1)(2n+1)}{6}";
        var tokens = MathLayoutEngine.Tokenize(latex);
        var parser = new MathLayoutEngine.MathParser(tokens);
        var ast = parser.ParseExpression();

        Assert.NotNull(ast);
        var ctx = new MathLayoutEngine.LayoutContext(16, "#000000", MathDisplayStyle.DisplayBlock);
        ast.Measure(ctx);

        Assert.True(ast.Width > 20);
        Assert.True(ast.Height > 20);
    }

    [Fact]
    public void Parser_Should_Parse_Matrices_And_Cases()
    {
        string matrixLatex = @"\begin{pmatrix} a & b \\ c & d \end{pmatrix}";
        var tokens = MathLayoutEngine.Tokenize(matrixLatex);
        var parser = new MathLayoutEngine.MathParser(tokens);
        var ast = parser.ParseExpression();

        Assert.NotNull(ast);
        var ctx = new MathLayoutEngine.LayoutContext(16, "#000000", MathDisplayStyle.DisplayBlock);
        ast.Measure(ctx);

        Assert.True(ast.Width > 20);
        Assert.True(ast.Height > 20);
    }

    [Fact]
    public void RenderToSvg_Should_Generate_Valid_Svg()
    {
        string latex = @"\int_{-\infty}^{\infty} e^{-x^2} \, dx = \sqrt{\pi}";
        var options = new MathRenderOptions
        {
            FontSize = 16,
            TextColorHex = "#0F172A",
            ShowEquationNumber = true,
            EquationNumber = "(1.1)",
            Alignment = TextAlignmentMode.Center
        };

        string svg = MathLayoutEngine.RenderToSvg(latex, options).SvgMarkup;

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg.TrimStart());
        Assert.Contains("</svg>", svg);
        Assert.Contains("viewBox=", svg);
        Assert.Contains("(1.1)", svg);
    }

    [Fact]
    public void PresetsLibrary_All_Presets_Should_Be_Valid_And_Renderable()
    {
        var presets = MathPresetsLibrary.GetAllPresets();
        Assert.True(presets.Count >= 25, "Expected at least 25 standard mathematical presets");

        foreach (var preset in presets)
        {
            Assert.False(string.IsNullOrWhiteSpace(preset.Id), $"Preset ID should not be empty for {preset.Name}");
            Assert.False(string.IsNullOrWhiteSpace(preset.Name), $"Preset Name should not be empty for {preset.Id}");
            Assert.False(string.IsNullOrWhiteSpace(preset.Formula), $"Formula should not be empty for {preset.Id}");

            // Verify Svg rendering works without throwing
            string svg = MathLayoutEngine.RenderToSvg(preset.Formula, new MathRenderOptions { FontSize = 14 }).SvgMarkup;
            Assert.NotNull(svg);
            Assert.Contains("<svg", svg);
        }
    }

    [Fact]
    public void PresetsLibrary_Category_Filtering_Should_Work()
    {
        var calculus = MathPresetsLibrary.GetByCategory(MathCategory.Calculus);
        Assert.NotEmpty(calculus);
        Assert.All(calculus, p => Assert.Equal(MathCategory.Calculus, p.Category));

        var physics = MathPresetsLibrary.GetByCategory(MathCategory.Physics);
        Assert.NotEmpty(physics);

        var finance = MathPresetsLibrary.GetByCategory(MathCategory.Finance);
        Assert.NotEmpty(finance);
    }

    [Fact]
    public void PdfMathElement_Polymorphic_Json_Serialization_Should_RoundTrip()
    {
        var mathEl = new PdfMathElement
        {
            Id = "math-101",
            X = 120,
            Y = 240,
            Width = 350,
            Height = 65,
            Formula = @"e^{i\pi} + 1 = 0",
            FontSize = 18,
            TextColorHex = "#1E293B",
            BackgroundColorHex = "#F8FAFC",
            BorderColorHex = "#E2E8F0",
            BorderThickness = 1.0,
            CornerRadius = 6,
            ShowBackground = true,
            ShowBorder = true,
            ShowEquationNumber = true,
            EquationNumber = "(Euler)",
            Category = MathCategory.Algebra,
            PresetName = "Euler's Identity"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Elements = { mathEl }
        };

        var doc = new PdfDocumentModel
        {
            Title = "Math Test Doc",
            Pages = { page }
        };

        string json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        Assert.Contains("\"$type\": \"math\"", json);
        Assert.Contains("Euler", json);

        var deserializedDoc = JsonSerializer.Deserialize<PdfDocumentModel>(json);
        Assert.NotNull(deserializedDoc);
        Assert.Single(deserializedDoc.Pages[0].Elements);

        var loadedEl = deserializedDoc.Pages[0].Elements[0] as PdfMathElement;
        Assert.NotNull(loadedEl);
        Assert.Equal(ElementKind.Math, loadedEl.Kind);
        Assert.Equal(@"e^{i\pi} + 1 = 0", loadedEl.Formula);
        Assert.Equal(18, loadedEl.FontSize);
        Assert.Equal("(Euler)", loadedEl.EquationNumber);
        Assert.Equal(MathCategory.Algebra, loadedEl.Category);
        Assert.Equal("Euler's Identity", loadedEl.PresetName);
    }

    [Fact]
    public void MathElementViewModel_Should_Sync_With_Model_And_RenderSvg()
    {
        var vm = new MathElementViewModel();
        vm.Formula = @"\frac{d}{dx}\left[ \int_{a}^{x} f(t) \, dt \right] = f(x)";
        vm.FontSize = 14;
        vm.ShowEquationNumber = true;
        vm.EquationNumber = "(FTC)";
        vm.RenderSvg();

        Assert.NotNull(vm.SvgSource);
        Assert.Contains("<svg", vm.SvgSource);
        Assert.Contains("(FTC)", vm.SvgSource);
        Assert.False(vm.HasError);

        var model = (PdfMathElement)vm.ToModel();
        Assert.Equal(vm.Formula, model.Formula);
        Assert.Equal(vm.EquationNumber, model.EquationNumber);
        Assert.True(model.ShowEquationNumber);

        var newVm = new MathElementViewModel();
        newVm.LoadFromModel(model);
        Assert.Equal(vm.Formula, newVm.Formula);
        Assert.Equal(vm.EquationNumber, newVm.EquationNumber);
        Assert.True(newVm.ShowEquationNumber);
    }

    [Fact]
    public void MathElementViewModel_InsertSymbol_And_ApplyPreset_Commands_Should_Work()
    {
        var vm = new MathElementViewModel();
        vm.Formula = "x";
        vm.InsertSymbolCommand.Execute(@"\alpha");
        Assert.Equal(@"x \alpha", vm.Formula);

        vm.ApplyPresetCommand.Execute("black_scholes_pde");
        Assert.Contains("partial", vm.Formula);
        Assert.Equal("Black-Scholes-Merton PDE", vm.PresetName);
        Assert.Equal(MathCategory.Finance, vm.Category);
    }

    [Fact]
    public async Task PdfExportService_Should_Export_Document_With_Math_Elements_To_Valid_Pdf()
    {
        var exportService = new PdfExportService();
        var tempFile = Path.Combine(Path.GetTempPath(), $"math_export_test_{Guid.NewGuid():N}.pdf");

        try
        {
            var doc = new PdfDocumentModel
            {
                Title = "Quantum & Financial Mathematics",
                Author = "FryPDF Test Suite"
            };

            var page = new PdfPageModel
            {
                PageNumber = 1,
                Format = PageFormat.A4,
                Orientation = PageOrientation.Portrait,
                Width = 595,
                Height = 842
            };

            // Heading
            page.Elements.Add(new PdfTextElement
            {
                X = 40,
                Y = 40,
                Width = 515,
                Height = 30,
                Text = "Advanced Mathematical Physics & Finance",
                FontSize = 16,
                IsBold = true,
                TextColorHex = "#0F172A"
            });

            // 1. Quantum Schrödinger Equation
            page.Elements.Add(new PdfMathElement
            {
                X = 40,
                Y = 90,
                Width = 515,
                Height = 60,
                Formula = @"i\hbar \frac{\partial}{\partial t} \Psi(\mathbf{r}, t) = \hat{H} \Psi(\mathbf{r}, t)",
                FontSize = 14,
                TextColorHex = "#0369A1",
                BackgroundColorHex = "#F0F9FF",
                BorderColorHex = "#BAE6FD",
                BorderThickness = 1,
                CornerRadius = 6,
                ShowBackground = true,
                ShowBorder = true,
                ShowEquationNumber = true,
                EquationNumber = "(1)",
                Alignment = TextAlignmentMode.Center,
                Category = MathCategory.QuantumMechanics,
                PresetName = "Schrödinger Equation"
            });

            // 2. Black-Scholes PDE
            page.Elements.Add(new PdfMathElement
            {
                X = 40,
                Y = 170,
                Width = 515,
                Height = 60,
                Formula = @"\frac{\partial V}{\partial t} + \frac{1}{2}\sigma^2 S^2 \frac{\partial^2 V}{\partial S^2} + rS \frac{\partial V}{\partial S} - rV = 0",
                FontSize = 13,
                TextColorHex = "#15803D",
                BackgroundColorHex = "#F0FDF4",
                BorderColorHex = "#BBF7D0",
                BorderThickness = 1,
                CornerRadius = 6,
                ShowBackground = true,
                ShowBorder = true,
                ShowEquationNumber = true,
                EquationNumber = "(2)",
                Alignment = TextAlignmentMode.Center,
                Category = MathCategory.Finance,
                PresetName = "Black-Scholes-Merton PDE"
            });

            // 3. Matrix & System
            page.Elements.Add(new PdfMathElement
            {
                X = 40,
                Y = 250,
                Width = 515,
                Height = 70,
                Formula = @"\begin{pmatrix} a_{11} & a_{12} \\ a_{21} & a_{22} \end{pmatrix} \begin{pmatrix} x_1 \\ x_2 \end{pmatrix} = \begin{pmatrix} b_1 \\ b_2 \end{pmatrix}",
                FontSize = 13,
                TextColorHex = "#7C3AED",
                BackgroundColorHex = "#FAF5FF",
                BorderColorHex = "#E9D5FF",
                BorderThickness = 1,
                CornerRadius = 6,
                ShowBackground = true,
                ShowBorder = true,
                ShowEquationNumber = true,
                EquationNumber = "(3)",
                Alignment = TextAlignmentMode.Center,
                Category = MathCategory.Algebra,
                PresetName = "Matrix Equation"
            });

            doc.Pages.Add(page);

            await exportService.ExportToFileAsync(doc, tempFile);

            Assert.True(File.Exists(tempFile));
            var fileInfo = new FileInfo(tempFile);
            Assert.True(fileInfo.Length > 1000, $"Exported PDF should have non-trivial size (actual: {fileInfo.Length} bytes)");

            // Check PDF header
            byte[] header = new byte[5];
            using (var fs = File.OpenRead(tempFile))
            {
                fs.ReadExactly(header, 0, 5);
            }
            string headerStr = System.Text.Encoding.ASCII.GetString(header);
            Assert.Equal("%PDF-", headerStr);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }
}
