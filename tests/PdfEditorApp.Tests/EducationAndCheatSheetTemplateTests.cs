using System;
using System.Linq;
using System.Text;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.Services.MathEngine;
using PdfEditorApp.ViewModels.ElementViewModels;
using QuestPDF.Fluent;
using Xunit;

namespace PdfEditorApp.Tests;

public class EducationAndCheatSheetTemplateTests
{
    private readonly ITemplateService _templateService = new TemplateService();
    private readonly IPdfExportService _exportService = new PdfExportService();

    [Fact]
    public void MathBODMASWorksheetTemplate_CreatesValidDocumentAndGeneratesPdf()
    {
        var doc = _templateService.CreateMathBODMASWorksheetTemplate();
        Assert.NotNull(doc);
        Assert.Equal("Class_6_BODMAS_Worksheet.pdf", doc.Title);
        Assert.Equal(3, doc.Pages.Count);

        var allElements = doc.Pages.SelectMany(p => p.Elements).ToList();
        Assert.True(allElements.Count >= 20);

        // Verify key structural components
        Assert.Contains(allElements, e => e is PdfTextElement t && t.Text.Contains("BODMAS"));
        Assert.Contains(allElements, e => e is PdfTextElement t && t.Text.Contains("Score"));
        Assert.Contains(allElements, e => e is PdfMathElement m && m.Formula.Contains("Brackets"));
        Assert.Contains(allElements, e => e is PdfTableElement tbl && tbl.Headers.Contains("Q#"));

        byte[] bytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void BilingualExamPaperTemplate_CreatesValidDocumentAndGeneratesPdf()
    {
        var doc = _templateService.CreateBilingualExamPaperTemplate();
        Assert.NotNull(doc);
        Assert.Equal("Simple_Interest_Bilingual_Exam_Paper.pdf", doc.Title);
        Assert.Equal(2, doc.Pages.Count);

        var allElements = doc.Pages.SelectMany(p => p.Elements).ToList();
        Assert.True(allElements.Count >= 25);

        // Verify bilingual English + Hindi elements
        Assert.Contains(allElements, e => e is PdfTextElement t && t.Text.Contains("Simple Interest"));
        Assert.Contains(allElements, e => e is PdfTextElement t && t.Text.Contains("साधारण ब्याज"));
        Assert.Contains(allElements, e => e is PdfMathElement m && m.Formula.Contains("SI ="));
        Assert.Contains(allElements, e => e is PdfTableElement tbl && tbl.Headers.Contains("उत्तर"));

        byte[] bytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void StatesOfMatterDiagramNotesTemplate_CreatesValidDocumentAndGeneratesPdf()
    {
        var doc = _templateService.CreateStatesOfMatterDiagramNotesTemplate();
        Assert.NotNull(doc);
        Assert.Equal("States_of_Matter_Visual_Notes.pdf", doc.Title);
        Assert.Equal(2, doc.Pages.Count);

        var allElements = doc.Pages.SelectMany(p => p.Elements).ToList();
        Assert.True(allElements.Count >= 20);

        // Verify SVG vector diagrams are present
        var svgElements = allElements.OfType<PdfSvgElement>().ToList();
        Assert.True(svgElements.Count >= 2);
        Assert.Contains(svgElements, s => s.PresetName == "Triangular Phase Cycle" && !string.IsNullOrEmpty(s.SvgSource));
        Assert.Contains(svgElements, s => s.PresetName == "Particle Arrangement" && !string.IsNullOrEmpty(s.SvgSource));

        // Verify that SVG vector elements rasterize to high-DPI PNG bytes cleanly
        foreach (var svgModel in svgElements)
        {
            var pngBytes = PdfEditorApp.Services.Tools.Core.PdfPageRenderer.RenderSvgToPngBytes(svgModel.SvgSource, svgModel.Width, svgModel.Height);
            Assert.NotNull(pngBytes);
            Assert.True(pngBytes.Length > 100);
            // Verify PNG header magic bytes
            Assert.Equal(0x89, pngBytes[0]);
            Assert.Equal((byte)'P', pngBytes[1]);
            Assert.Equal((byte)'N', pngBytes[2]);
            Assert.Equal((byte)'G', pngBytes[3]);

            // Verify ViewModel loads properly
            var svgVm = new SvgElementViewModel();
            svgVm.LoadFromModel(svgModel);
            Assert.Equal(svgModel.Width, svgVm.Width);
            Assert.Equal(svgModel.Height, svgVm.Height);
            Assert.Equal(svgModel.PresetName, svgVm.PresetName);
            Assert.False(string.IsNullOrEmpty(svgVm.SvgSource));
        }

        // Verify comparison table and explanatory notes
        Assert.Contains(allElements, e => e is PdfTableElement tbl && tbl.Headers.Contains("Solid (ठोस)"));
        Assert.Contains(allElements, e => e is PdfTextElement t && t.Text.Contains("Sublimation"));

        byte[] bytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void QuadrilateralsGuideTemplate_CreatesValidDocumentAndGeneratesPdf()
    {
        var doc = _templateService.CreateQuadrilateralsGuideTemplate();
        Assert.NotNull(doc);
        Assert.Equal("Types_of_Quadrilaterals_Guide.pdf", doc.Title);
        Assert.Equal(2, doc.Pages.Count);

        var allElements = doc.Pages.SelectMany(p => p.Elements).ToList();
        Assert.True(allElements.Count >= 20);

        // Verify SVG quadrilateral diagram
        var svg = allElements.OfType<PdfSvgElement>().FirstOrDefault(s => s.PresetName == "Quadrilaterals Geometry Diagram");
        Assert.NotNull(svg);
        Assert.Contains("<svg", svg.SvgSource);

        // Verify math formula for angle sum theorem
        Assert.Contains(allElements, e => e is PdfMathElement m && m.Formula.Contains("360"));

        // Verify quadrilateral property cards across both pages
        Assert.Contains(allElements, e => e is PdfTextElement t && t.Text.Contains("Parallelogram"));
        Assert.Contains(allElements, e => e is PdfTextElement t && t.Text.Contains("Trapezoid"));

        byte[] bytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void FactorizationWorksheetTemplate_CreatesValidDocumentAndGeneratesPdf()
    {
        var doc = _templateService.CreateFactorizationWorksheetTemplate();
        Assert.NotNull(doc);
        Assert.Equal("Factorization_150_Questions_Worksheet.pdf", doc.Title);
        Assert.Equal(2, doc.Pages.Count);

        var allElements = doc.Pages.SelectMany(p => p.Elements).ToList();
        Assert.True(allElements.Count >= 20);

        // Verify 3-column algebra columns
        var textElements = allElements.OfType<PdfTextElement>().ToList();
        Assert.True(textElements.Count >= 6);

        // Verify algebraic identity formulas and solutions table
        Assert.Contains(allElements, e => e is PdfMathElement m && m.Formula.Contains("a^2 - b^2"));
        Assert.Contains(allElements, e => e is PdfTableElement tbl && tbl.Headers.Contains("Factorized Result"));

        byte[] bytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void ArduinoCheatSheetTemplate_CreatesValidPosterDocumentAndGeneratesPdf()
    {
        var doc = _templateService.CreateArduinoCheatSheetTemplate();
        Assert.NotNull(doc);
        Assert.Equal("Arduino_Developer_Reference_CheatSheet.pdf", doc.Title);
        Assert.Single(doc.Pages);

        var page = doc.Pages[0];
        Assert.Equal(PageFormat.Poster, page.Format);
        Assert.Equal(1600, page.Width);
        Assert.Equal(1131, page.Height);
        Assert.Equal(PageOrientation.Landscape, page.Orientation);

        // Verify multi-column cards, code snippets, pinout table
        Assert.Contains(page.Elements, e => e is PdfTextElement t && t.Text.Contains("ARDUINO"));
        Assert.Contains(page.Elements, e => e is PdfTableElement tbl && tbl.Headers.Contains("Port"));
        Assert.Contains(page.Elements, e => e is PdfTextElement t && t.Text.Contains("setup()"));
        Assert.Contains(page.Elements, e => e is PdfTextElement t && t.Text.Contains("attachInterrupt"));

        byte[] bytes = _exportService.GeneratePdfBytes(doc);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void MathPresetsLibrary_ContainsSchoolArithmeticAndGeometry()
    {
        var schoolArithmetic = MathPresetsLibrary.GetByCategory(MathCategory.SchoolArithmetic).ToList();
        Assert.NotEmpty(schoolArithmetic);
        Assert.Contains(schoolArithmetic, p => p.Name.Contains("BODMAS"));
        Assert.Contains(schoolArithmetic, p => p.Name.Contains("Simple Interest"));
        Assert.Contains(schoolArithmetic, p => p.Name.Contains("Compound Interest"));

        var geometry = MathPresetsLibrary.GetByCategory(MathCategory.Geometry).ToList();
        Assert.NotEmpty(geometry);
        Assert.Contains(geometry, p => p.Name.Contains("Parallelogram"));
        Assert.Contains(geometry, p => p.Name.Contains("Rhombus"));

        var algebra = MathPresetsLibrary.GetByCategory(MathCategory.Algebra).ToList();
        Assert.Contains(algebra, p => p.Name.Contains("Square of Binomial"));
        Assert.Contains(algebra, p => p.Name.Contains("Sum and Difference of Cubes"));
        Assert.Contains(algebra, p => p.Name.Contains("Square Root of Complex Number"));
    }

    [Fact]
    public void SvgOrnamentLibrary_GeneratesCleanSvgDiagrams()
    {
        var phaseCycleSvg = SvgOrnamentLibrary.GetTriangularPhaseCycleSvg();
        Assert.NotEmpty(phaseCycleSvg);
        Assert.Contains("<svg", phaseCycleSvg);
        Assert.Contains("Solid", phaseCycleSvg);
        Assert.Contains("Liquid", phaseCycleSvg);
        Assert.Contains("Gas", phaseCycleSvg);
        Assert.Contains("Sublimation", phaseCycleSvg);

        var particleSvg = SvgOrnamentLibrary.GetParticleArrangementGridSvg();
        Assert.NotEmpty(particleSvg);
        Assert.Contains("<svg", particleSvg);
        Assert.Contains("Solid (ठोस)", particleSvg);
        Assert.Contains("Liquid (द्रव)", particleSvg);
        Assert.Contains("Gas (गैस)", particleSvg);

        var quadSvg = SvgOrnamentLibrary.GetQuadrilateralSetDiagramSvg();
        Assert.NotEmpty(quadSvg);
        Assert.Contains("<svg", quadSvg);
        Assert.Contains("Parallelogram", quadSvg);
        Assert.Contains("Rhombus", quadSvg);
        Assert.Contains("Trapezoid", quadSvg);
        Assert.Contains("Kite", quadSvg);
    }
}
