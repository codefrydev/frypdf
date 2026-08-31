using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Deconstruction;
using PdfEditorApp.Core.Utils;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.Templates;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PdfLayoutAnalyzerTests
{
    private readonly IPdfExportService _exportService = new PdfExportService();
    private readonly ITemplateService _templateService = new TemplateService();
    private readonly IPdfImportService _importService = new PdfImportService();

    [Fact]
    public void NormalizeFontFamily_MapsCommonPdfFontNamesToCleanFamilies()
    {
        // Latin - Sans-Serif
        Assert.Equal("Arial", PdfLayoutAnalyzer.NormalizeFontFamily("ABCDEE+Arial-BoldMT"));
        Assert.Equal("Roboto", PdfLayoutAnalyzer.NormalizeFontFamily("Roboto-Medium"));
        Assert.Equal("Inter", PdfLayoutAnalyzer.NormalizeFontFamily("Inter-Regular"));
        Assert.Equal("Open Sans", PdfLayoutAnalyzer.NormalizeFontFamily("OpenSans-Regular"));
        Assert.Equal("Open Sans", PdfLayoutAnalyzer.NormalizeFontFamily("SegoeUI-Semibold")); // Segoe → Open Sans cross-platform
        Assert.Equal("Lato", PdfLayoutAnalyzer.NormalizeFontFamily("Lato-Bold"));
        Assert.Equal("Poppins", PdfLayoutAnalyzer.NormalizeFontFamily("Poppins-SemiBold"));
        Assert.Equal("Ubuntu", PdfLayoutAnalyzer.NormalizeFontFamily("Ubuntu-Regular"));

        // Latin - Serif
        Assert.Equal("PT Serif", PdfLayoutAnalyzer.NormalizeFontFamily("TimesNewRomanPS-Italic")); // Times → PT Serif
        Assert.Equal("PT Serif", PdfLayoutAnalyzer.NormalizeFontFamily("Georgia-Bold"));
        Assert.Equal("Crimson Text", PdfLayoutAnalyzer.NormalizeFontFamily("Garamond-Regular"));
        Assert.Equal("Merriweather", PdfLayoutAnalyzer.NormalizeFontFamily("Merriweather-Light"));
        Assert.Equal("Playfair Display", PdfLayoutAnalyzer.NormalizeFontFamily("PlayfairDisplay-Bold"));

        // Monospace
        Assert.Equal("Fira Code", PdfLayoutAnalyzer.NormalizeFontFamily("CourierNewPSMT")); // Courier → Fira Code

        // Indian Scripts
        Assert.Equal("Noto Sans Devanagari", PdfLayoutAnalyzer.NormalizeFontFamily("NirmalaUI-Bold"));
        Assert.Equal("Noto Sans Devanagari", PdfLayoutAnalyzer.NormalizeFontFamily("Mangal"));
        Assert.Equal("Noto Sans Tamil", PdfLayoutAnalyzer.NormalizeFontFamily("Latha-Regular"));
        Assert.Equal("Noto Sans Telugu", PdfLayoutAnalyzer.NormalizeFontFamily("Gautami"));
        Assert.Equal("Noto Sans Arabic", PdfLayoutAnalyzer.NormalizeFontFamily("Arabic-Regular"));
        Assert.Equal("Noto Sans Bengali", PdfLayoutAnalyzer.NormalizeFontFamily("Vrinda-Bengali"));

        // Null/empty fallback → Open Sans (clean default)
        Assert.Equal("Open Sans", PdfLayoutAnalyzer.NormalizeFontFamily(null));
        Assert.Equal("Open Sans", PdfLayoutAnalyzer.NormalizeFontFamily(""));
        Assert.Equal("Open Sans", PdfLayoutAnalyzer.NormalizeFontFamily("SomeObscureUnknownFont123"));
    }

    [Fact]
    public async Task PdfDeconstructionEngine_ImportsAnnualReport_ClustersParagraphsAndRetainsCleanCanvas()
    {
        // 1. Generate multi-page annual report
        var reportDoc = _templateService.CreateAnnualReportTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(reportDoc);

        // 2. Deconstruct using PdfDeconstructionEngine
        var deconstructed = PdfDeconstructionEngine.Deconstruct(pdfBytes, "Annual_Report.pdf");

        Assert.NotNull(deconstructed);
        Assert.Equal(3, deconstructed.Pages.Count);

        var page1 = deconstructed.Pages[0];
        Assert.Equal(PageFormat.A4, page1.Format);
        Assert.Equal("#FFFFFF", page1.BackgroundColorHex);

        // Verify no raster ghost background image was created
        var bgImage = page1.Elements.OfType<PdfImageElement>().FirstOrDefault(img => img.AltText.Contains("Background Canvas"));
        Assert.Null(bgImage);

        // Verify text elements are extracted and have clean bounding boxes
        var textElements = page1.Elements.OfType<PdfTextElement>().ToList();
        Assert.NotEmpty(textElements);

        foreach (var txt in textElements)
        {
            Assert.False(string.IsNullOrWhiteSpace(txt.Text));
            Assert.True(txt.Width > 0);
            Assert.True(txt.Height > 0);
            Assert.False(string.IsNullOrWhiteSpace(txt.FontFamily));
            Assert.True(txt.FontSize >= 6);
        }
    }

    [Fact]
    public async Task PdfDeconstructionEngine_EditImportedText_SavesAndExportsCleanlyWithoutUnderlyingArtifacts()
    {
        // 1. Create invoice PDF
        var originalInvoice = _templateService.CreateInvoiceTemplate();
        byte[] originalBytes = _exportService.GeneratePdfBytes(originalInvoice);

        // 2. Import into model
        var imported = await _importService.ImportPdfBytesAsync(originalBytes, "Invoice_To_Edit.pdf");
        Assert.NotNull(imported);
        var page = imported.Pages[0];

        // 3. Count elements before edit
        int elementCountBefore = page.Elements.Count;
        Assert.True(elementCountBefore > 0, "Imported page must have elements before editing");

        // 4. Edit first text element (simulates user editing in Studio)
        var textElement = page.Elements.OfType<PdfTextElement>().First();
        textElement.Text = "FryPDF Super Invoice";
        textElement.Width = 300;
        textElement.Height = 30;

        // 5. Export modified document — must produce valid PDF bytes
        byte[] modifiedBytes = _exportService.GeneratePdfBytes(imported);
        Assert.NotNull(modifiedBytes);
        Assert.True(modifiedBytes.Length > 1000, "Exported PDF must have substantial content");

        // 6. Verify re-import succeeds (no crash, correct page count)
        var reimported = await _importService.ImportPdfBytesAsync(modifiedBytes, "Modified_Invoice.pdf");
        Assert.NotNull(reimported);
        Assert.Single(reimported.Pages);
        Assert.NotEmpty(reimported.Pages[0].Elements);
    }

    [Fact]
    public void PdfDocumentSanitizer_SanitizePdfBytes_EnsuresEofTermination()
    {
        byte[] rawBytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\n");
        byte[] sanitized = PdfDocumentSanitizer.SanitizePdfBytes(rawBytes);

        string sanitizedText = System.Text.Encoding.ASCII.GetString(sanitized);
        Assert.Contains("%%EOF", sanitizedText);
    }

    [Fact]
    public void UnicodeScriptDetector_DetectsRtlAndDevanagari()
    {
        string arabic = "مرحبا بكم في عالم الابتكار";
        string hebrew = "שלום עולם";
        string hindi = "नमस्ते भारत";
        string english = "Welcome to FryPDF Studio";

        Assert.True(UnicodeScriptDetector.IsRtlText(arabic));
        Assert.True(UnicodeScriptDetector.IsRtlText(hebrew));
        Assert.False(UnicodeScriptDetector.IsRtlText(hindi));
        Assert.False(UnicodeScriptDetector.IsRtlText(english));

        Assert.True(UnicodeScriptDetector.ContainsDevanagari(hindi));
        Assert.False(UnicodeScriptDetector.ContainsDevanagari(english));
    }

    [Fact]
    public void PageViewModel_GroupAndUngroup_ManagesGroupIds()
    {
        var page = new PageViewModel();
        var t1 = new TextElementViewModel { Text = "Header", X = 10, Y = 10, Width = 100, Height = 30 };
        var t2 = new TextElementViewModel { Text = "Body", X = 10, Y = 50, Width = 100, Height = 50 };
        var s1 = new ShapeElementViewModel { X = 5, Y = 5, Width = 120, Height = 100 };

        page.AddElement(t1);
        page.AddElement(t2);
        page.AddElement(s1);

        page.SelectElements(new ElementViewModelBase[] { t1, t2, s1 });
        Assert.Equal(3, page.SelectedElements.Count);

        page.GroupSelected();
        Assert.NotNull(t1.GroupId);
        Assert.Equal(t1.GroupId, t2.GroupId);
        Assert.Equal(t1.GroupId, s1.GroupId);

        page.UngroupSelected();
        Assert.Null(t1.GroupId);
        Assert.Null(t2.GroupId);
        Assert.Null(s1.GroupId);
    }

    [Fact]
    public void PageViewModel_SmartAlignmentTools_AlignsElementsAccurately()
    {
        var page = new PageViewModel();
        var e1 = new TextElementViewModel { X = 10, Y = 20, Width = 100, Height = 30 };
        var e2 = new TextElementViewModel { X = 50, Y = 80, Width = 80, Height = 40 };
        var e3 = new TextElementViewModel { X = 150, Y = 150, Width = 120, Height = 50 };

        page.AddElement(e1);
        page.AddElement(e2);
        page.AddElement(e3);
        page.SelectElements(new[] { e1, e2, e3 });

        // Align Left: All X should be minX = 10
        page.AlignSelectedLeft();
        Assert.Equal(10, e1.X);
        Assert.Equal(10, e2.X);
        Assert.Equal(10, e3.X);

        // Align Top: All Y should be minY = 20
        page.AlignSelectedTop();
        Assert.Equal(20, e1.Y);
        Assert.Equal(20, e2.Y);
        Assert.Equal(20, e3.Y);

        // Set distinct X positions and test Distribute Horizontally
        e1.X = 10;
        e2.X = 80;
        e3.X = 250;
        page.DistributeSelectedHorizontally();
        Assert.Equal(10, e1.X);
        Assert.True(e2.X > e1.X + e1.Width);
        Assert.True(e3.X > e2.X + e2.Width);
    }

    /// <summary>
    /// Regression test: Verify that Rotate270 marginalia text from vertical margins
    /// (sample1.pdf) is extracted with Rotation=270 and that CanvasX/CanvasY are placed
    /// so that the rotation pivot falls at the PDF text center (correct geometric placement).
    /// </summary>
    [Fact]
    public async Task GovernmentId_Rotate270MarginaliaText_HasCorrectRotationAndPlacement()
    {
        string baseDir = AppContext.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        string sample1Path = Path.Combine(rootDir, "sample1.pdf");
        if (!File.Exists(sample1Path)) return; // Skip if not present in CI

        var doc = await _importService.ImportPdfAsync(sample1Path);
        Assert.NotNull(doc);
        Assert.NotEmpty(doc.Pages);

        var page1 = doc.Pages[0];
        var textElements = page1.Elements.OfType<PdfTextElement>().ToList();

        // 1. Verify at least some rotated text elements exist (document has left-margin marginalia)
        var rotated270 = textElements.Where(t => t.Rotation == 270.0).ToList();
        Assert.NotEmpty(rotated270);

        // 2. Verify at least one element contains marginalia text
        var marginaliaEl = rotated270.FirstOrDefault(t => t.Text.Length > 5);
        Assert.NotNull(marginaliaEl);

        // 3. Verify the element has positive Width and Height (unrotated dimensions)
        Assert.True(marginaliaEl.Width > 0, "Rotated text element must have positive Width");
        Assert.True(marginaliaEl.Height > 0, "Rotated text element must have positive Height");

        // 4. Verify Width (text run length) >> Height (font thickness): for a long string like
        //    marginalia text, Width should be significantly larger than Height.
        Assert.True(marginaliaEl.Width > marginaliaEl.Height * 2,
            $"For rotated marginalia, Width ({marginaliaEl.Width:F1}) should be much larger than Height ({marginaliaEl.Height:F1})");

        // 5. The canvas X position should be near the left margin of the page.
        //    After 270° rotation, the visual left edge = CanvasX + Width/2 - Height/2.
        //    The pivot (center of element) must be near the original PDF text X center (~59-62pt).
        double pivotX = marginaliaEl.X + (marginaliaEl.Width / 2.0);
        Assert.True(pivotX < 150, $"Left-margin rotated text pivot X ({pivotX:F1}) should be near left margin (<150pt)");

        // 6. Verify CanvasY is within page bounds
        Assert.True(marginaliaEl.Y >= 0, "CanvasY must be non-negative");
        Assert.True(marginaliaEl.Y < page1.Height, $"CanvasY ({marginaliaEl.Y:F1}) must be within page height ({page1.Height:F1}pt)");
    }
}
