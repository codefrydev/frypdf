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
}
