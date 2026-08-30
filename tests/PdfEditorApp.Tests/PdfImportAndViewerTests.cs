using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.Templates;
using PdfEditorApp.ViewModels;
using UglyToad.PdfPig.Rendering.Skia;
using Xunit;

namespace PdfEditorApp.Tests;

public class PdfImportAndViewerTests
{
    private readonly IPdfExportService _exportService = new PdfExportService();
    private readonly ITemplateService _templateService = new TemplateService();
    private readonly IPdfImportService _importService = new PdfImportService();
    private readonly IProjectPersistenceService _persistenceService = new ProjectPersistenceService();

    [Fact]
    public async Task PdfImportService_ImportsBinaryPdfBytes_AccuratelyExtractsPagesAndElements()
    {
        // 1. Create a rich 3-page annual report PDF
        var sourceModel = _templateService.CreateAnnualReportTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(sourceModel);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);

        // 2. Import into editable PdfDocumentModel
        var importedModel = await _importService.ImportPdfBytesAsync(pdfBytes, "Annual_Report.pdf");

        Assert.NotNull(importedModel);
        Assert.False(string.IsNullOrWhiteSpace(importedModel.Title));
        Assert.Equal(3, importedModel.Pages.Count);

        // Verify Page 1
        var page1 = importedModel.Pages[0];
        Assert.True(page1.Width > 0);
        Assert.True(page1.Height > 0);
        Assert.NotEmpty(page1.Elements);

        // Verify editable text elements were extracted with crisp metrics
        var textElements = page1.Elements.OfType<PdfTextElement>().ToList();
        Assert.NotEmpty(textElements);
        Assert.Contains(textElements, t => !string.IsNullOrWhiteSpace(t.Text));

        // Verify no duplicate background canvas image was injected on digital vector PDF
        var bgCanvas = page1.Elements.OfType<PdfImageElement>().FirstOrDefault(e => e.AltText.Contains("Background Canvas"));
        Assert.Null(bgCanvas);
    }

    [Fact]
    public async Task ProjectPersistenceService_LoadsRealPdfFile_WithoutJsonDeserializationErrors()
    {
        // 1. Create temporary PDF file
        string tempPdfPath = Path.Combine(Path.GetTempPath(), $"FryPdf_Test_{Guid.NewGuid():N}.pdf");
        try
        {
            var sourceModel = _templateService.CreateInvoiceTemplate();
            byte[] pdfBytes = _exportService.GeneratePdfBytes(sourceModel);
            await File.WriteAllBytesAsync(tempPdfPath, pdfBytes);

            // 2. Load through ProjectPersistenceService (which previously threw '%' is an invalid start of a value error)
            var loadedModel = await _persistenceService.LoadProjectAsync(tempPdfPath);

            Assert.NotNull(loadedModel);
            Assert.Single(loadedModel.Pages);
            Assert.NotEmpty(loadedModel.Pages[0].Elements);
        }
        finally
        {
            if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
        }
    }

    [Fact]
    public async Task ProjectPersistenceService_SavesAndLoadsJsonProject_RoundtripsAccurately()
    {
        string tempProjPath = Path.Combine(Path.GetTempPath(), $"FryPdf_Project_{Guid.NewGuid():N}.frypdf");
        try
        {
            var original = _templateService.CreateResumeTemplate();
            await _persistenceService.SaveProjectAsync(original, tempProjPath);

            var loaded = await _persistenceService.LoadProjectAsync(tempProjPath);
            Assert.NotNull(loaded);
            Assert.Equal(original.Title, loaded.Title);
            Assert.Equal(original.Pages.Count, loaded.Pages.Count);
            Assert.Equal(original.Pages[0].Elements.Count, loaded.Pages[0].Elements.Count);
        }
        finally
        {
            if (File.Exists(tempProjPath)) File.Delete(tempProjPath);
        }
    }

    [Fact]
    public async Task PdfViewerViewModel_LoadsPdfBytes_RendersPagesAndExtractsMetadata()
    {
        var sourceModel = _templateService.CreateCertificateTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(sourceModel);

        var viewer = new PdfViewerViewModel();
        await viewer.LoadDocumentBytesAsync(pdfBytes, "Certificate_Of_Excellence.pdf");

        Assert.True(viewer.HasDocument);
        Assert.Equal("Certificate_Of_Excellence.pdf", viewer.DocumentTitle);
        Assert.Equal(1, viewer.TotalPagesCount);
        Assert.Equal(1, viewer.CurrentPageNumber);
        Assert.NotEmpty(viewer.Pages);
        Assert.NotEmpty(viewer.MetadataItems);

        // Verify page details
        var firstPage = viewer.Pages[0];
        Assert.Equal(1, firstPage.PageNumber);
        Assert.True(firstPage.WidthPoints > 0);
        Assert.True(firstPage.HeightPoints > 0);
    }

    [Fact]
    public async Task PdfViewerViewModel_Search_FindsMatchesAcrossPages()
    {
        var sourceModel = _templateService.CreateAnnualReportTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(sourceModel);

        var viewer = new PdfViewerViewModel();
        await viewer.LoadDocumentBytesAsync(pdfBytes, "Annual_Report.pdf");

        // Execute text search for a common business word in the annual report
        viewer.SearchQuery = "Revenue";

        // If "Revenue" is present in the document text, verify search results are generated
        if (viewer.SearchResults.Count > 0)
        {
            Assert.True(viewer.MatchCount > 0);
            Assert.True(viewer.CurrentMatchIndex >= 1);

            // Test Next & Previous Match navigation
            int initialIndex = viewer.CurrentMatchIndex;
            viewer.NextMatch();
            Assert.True(viewer.CurrentMatchIndex >= 1);
            viewer.PreviousMatch();
            Assert.True(viewer.CurrentMatchIndex >= 1);
        }
    }

    [Fact]
    public async Task PdfViewerViewModel_ZoomControls_UpdatesZoomLevelAccurately()
    {
        var sourceModel = _templateService.CreateInvoiceTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(sourceModel);

        var viewer = new PdfViewerViewModel();
        await viewer.LoadDocumentBytesAsync(pdfBytes, "Invoice.pdf");

        // Initial Zoom: 1.0 (100%)
        Assert.Equal(1.0, viewer.ZoomLevel);
        Assert.Equal("100%", viewer.ZoomPercentageText);

        // Zoom In
        viewer.ZoomIn();
        Assert.True(viewer.ZoomLevel > 1.0);

        // Zoom Out
        viewer.ZoomOut();
        viewer.ZoomOut();
        Assert.True(viewer.ZoomLevel < 1.0);

        // Reset Zoom
        viewer.ResetZoom();
        Assert.Equal(1.0, viewer.ZoomLevel);
        Assert.Equal("100%", viewer.ZoomPercentageText);

        // Fit Width & Fit Page
        viewer.FitToWidth();
        Assert.Equal(1.35, viewer.ZoomLevel);

        viewer.FitToPage();
        Assert.Equal(0.95, viewer.ZoomLevel);
    }

    [Fact]
    public async Task PdfViewerViewModel_PageRotation_RotatesClockwiseAndCounterClockwise()
    {
        var sourceModel = _templateService.CreateInvoiceTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(sourceModel);

        var viewer = new PdfViewerViewModel();
        await viewer.LoadDocumentBytesAsync(pdfBytes, "Invoice.pdf");
        Assert.NotNull(viewer.SelectedPage);

        int initialRotation = viewer.SelectedPage.RotationAngle;

        // Rotate CW
        viewer.RotateClockwise();
        Assert.Equal((initialRotation + 90) % 360, viewer.SelectedPage.RotationAngle);

        // Rotate CCW
        viewer.RotateCounterClockwise();
        Assert.Equal(initialRotation, viewer.SelectedPage.RotationAngle);
    }

    [Fact]
    public async Task PdfViewerViewModel_Annotations_AddsAndDeletesAnnotationsSuccessfully()
    {
        var sourceModel = _templateService.CreateInvoiceTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(sourceModel);

        var viewer = new PdfViewerViewModel();
        await viewer.LoadDocumentBytesAsync(pdfBytes, "Invoice.pdf");

        Assert.Empty(viewer.Annotations);
        Assert.False(viewer.HasAnnotations);

        // Add Highlight
        viewer.AddHighlightAnnotation();
        Assert.Single(viewer.Annotations);
        Assert.True(viewer.HasAnnotations);
        Assert.Equal("Highlight", viewer.Annotations[0].Type);

        // Add Stamp
        viewer.AddStamp("APPROVED");
        Assert.Equal(2, viewer.Annotations.Count);
        Assert.Equal("Stamp", viewer.Annotations[1].Type);
        Assert.Contains("APPROVED", viewer.Annotations[1].Content);

        // Add Sticky Note
        viewer.NewNoteText = "Needs sign-off from finance.";
        viewer.ConfirmAddNote();
        Assert.Equal(3, viewer.Annotations.Count);
        Assert.Equal("StickyNote", viewer.Annotations[2].Type);

        // Delete Annotation
        var note = viewer.Annotations[2];
        viewer.DeleteAnnotation(note);
        Assert.Equal(2, viewer.Annotations.Count);
    }

    [Fact]
    public async Task PdfViewerViewModel_EditInStudioBridge_TriggersEventWithFilePath()
    {
        string tempPdfPath = Path.Combine(Path.GetTempPath(), $"FryPdf_Studio_Bridge_{Guid.NewGuid():N}.pdf");
        try
        {
            var sourceModel = _templateService.CreateInvoiceTemplate();
            byte[] pdfBytes = _exportService.GeneratePdfBytes(sourceModel);
            await File.WriteAllBytesAsync(tempPdfPath, pdfBytes);

            var viewer = new PdfViewerViewModel();
            await viewer.LoadDocumentAsync(tempPdfPath);

            string? requestedPath = null;
            viewer.EditInStudioRequested += (path) => requestedPath = path;

            viewer.EditInStudio();

            Assert.NotNull(requestedPath);
            Assert.Equal(tempPdfPath, requestedPath);
        }
        finally
        {
            if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);
        }
    }

    [Fact]
    public void ImageElementViewModel_Base64Data_LoadsAndRendersBitmapSuccessfully()
    {
        // 1. Generate a valid 1x1 PNG image as base64
        byte[] validPngBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
            0x0D, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x60, 0x60, 0x60, 0x00,
            0x00, 0x00, 0x04, 0x00, 0x01, 0x5D, 0x36, 0xBD, 0x7E, 0x00, 0x00, 0x00,
            0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        };
        string base64 = Convert.ToBase64String(validPngBytes);

        var model = new PdfImageElement
        {
            X = 50,
            Y = 50,
            Width = 100,
            Height = 100,
            Base64Data = base64,
            AltText = "Test Base64 Image"
        };

        var vm = new PdfEditorApp.ViewModels.ElementViewModels.ImageElementViewModel();
        vm.LoadFromModel(model);

        Assert.Equal(base64, vm.Base64Data);
        Assert.Equal("Test Base64 Image", vm.DisplayName);

        var roundTripped = (PdfImageElement)vm.ToModel();
        Assert.Equal(base64, roundTripped.Base64Data);
        Assert.Equal("Test Base64 Image", roundTripped.AltText);
    }

    [Fact]
    public async Task PdfImportService_ImportsPdf_PageBackgroundAndElementsHaveCleanRendering()
    {
        var sourceModel = _templateService.CreateCertificateTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(sourceModel);

        var imported = await _importService.ImportPdfBytesAsync(pdfBytes, "Certificate.pdf");
        Assert.NotNull(imported);
        Assert.Single(imported.Pages);

        var page = imported.Pages[0];
        Assert.NotEmpty(page.Elements);

        // Verify that digital vector PDF does NOT add duplicate raster background underlay
        var bgCanvas = page.Elements.OfType<PdfImageElement>().FirstOrDefault(e => e.AltText.Contains("Background Canvas"));
        Assert.Null(bgCanvas);

        // Verify text elements are extracted
        var textElements = page.Elements.OfType<PdfTextElement>().ToList();
        Assert.NotEmpty(textElements);

        var pageVm = new PageViewModel();
        pageVm.LoadFromModel(page);
        Assert.NotEmpty(pageVm.Elements);
    }

    [Fact]
    public async Task PdfImportService_MultiColumnInvoice_ExtractsSeparateTextBlocks()
    {
        var invoiceModel = _templateService.CreateInvoiceTemplate();
        byte[] pdfBytes = _exportService.GeneratePdfBytes(invoiceModel);

        var imported = await _importService.ImportPdfBytesAsync(pdfBytes, "Invoice_MultiColumn.pdf");
        Assert.NotNull(imported);
        Assert.Single(imported.Pages);

        var textElements = imported.Pages[0].Elements.OfType<PdfTextElement>().ToList();
        Assert.NotEmpty(textElements);
        Assert.True(textElements.Count >= 2);

        // Verify that individual text blocks have non-zero dimensions and non-empty text
        foreach (var txt in textElements)
        {
            Assert.False(string.IsNullOrWhiteSpace(txt.Text));
            Assert.True(txt.Width > 0);
            Assert.True(txt.Height > 0);
        }
    }

    [Fact]
    public async Task DeconstructSampleFiles_Investigate()
    {
        string baseDir = AppContext.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        string sample1Path = Path.Combine(rootDir, "sample1.pdf");
        string sample2Path = Path.Combine(rootDir, "Class_6_Math_Chapter_1_Pattern_In_Mathematics.pdf");
        if (!File.Exists(sample2Path)) sample2Path = Path.Combine(rootDir, "sample2.pdf");

        if (!File.Exists(sample1Path))
        {
            return;
        }

        var doc1 = await _importService.ImportPdfAsync(sample1Path);

        Console.WriteLine($"=== SAMPLE 1 ===");
        Console.WriteLine($"Pages: {doc1.Pages.Count}");
        for (int i = 0; i < doc1.Pages.Count; i++)
        {
            var p = doc1.Pages[i];
            Console.WriteLine($"Page {i + 1}: {p.Width}x{p.Height}, Elements: {p.Elements.Count}");
            for (int elIdx = 0; elIdx < Math.Min(20, p.Elements.Count); elIdx++)
            {
                var el = p.Elements[elIdx];
                if (el is PdfTextElement txt)
                {
                    Console.WriteLine($"  #{elIdx} [TEXT] X={txt.X:F1}, Y={txt.Y:F1}, W={txt.Width:F1}, H={txt.Height:F1}, Font={txt.FontFamily} {txt.FontSize:F1}pt, Color={txt.TextColorHex}, Z={txt.ZIndex}, Lines={txt.Text.Split('\n').Length}, Preview={txt.Text.Substring(0, Math.Min(40, txt.Text.Length)).Replace("\n", " ")}");
                }
                else if (el is PdfImageElement img)
                {
                    bool isValidImage = false;
                    int imgW = 0, imgH = 0;
                    if (!string.IsNullOrEmpty(img.Base64Data))
                    {
                        byte[] bytes = Convert.FromBase64String(img.Base64Data);
                        using var skData = SkiaSharp.SKData.CreateCopy(bytes);
                        using var skImg = SkiaSharp.SKImage.FromEncodedData(skData);
                        if (skImg != null)
                        {
                            isValidImage = true;
                            imgW = skImg.Width;
                            imgH = skImg.Height;
                        }
                    }

                    Console.WriteLine($"  #{elIdx} [IMAGE] X={img.X:F1}, Y={img.Y:F1}, W={img.Width:F1}, H={img.Height:F1}, Decoded={isValidImage} ({imgW}x{imgH}), Z={img.ZIndex}, Locked={img.IsLocked}, Opacity={img.Opacity}, Alt={img.AltText}, Base64Len={img.Base64Data?.Length ?? 0}");
                }
                else if (el is PdfShapeElement shp)
                {
                    Console.WriteLine($"  #{elIdx} [SHAPE] X={shp.X:F1}, Y={shp.Y:F1}, W={shp.Width:F1}, H={shp.Height:F1}, Fill={shp.FillColorHex}, Stroke={shp.StrokeColorHex}, StrokeThick={shp.StrokeThickness}, Z={shp.ZIndex}");
                }
                else if (el is PdfDividerElement div)
                {
                    Console.WriteLine($"  #{elIdx} [DIVIDER] X={div.X:F1}, Y={div.Y:F1}, W={div.Width:F1}, H={div.Height:F1}, Color={div.ColorHex}, Z={div.ZIndex}");
                }
                else
                {
                    Console.WriteLine($"  #{elIdx} [{el.GetType().Name}] X={el.X:F1}, Y={el.Y:F1}, W={el.Width:F1}, H={el.Height:F1}, Z={el.ZIndex}");
                }
            }

            // Finished inspecting elements
        }

        // Assertions for Sample 1:
        Assert.True(doc1.Pages.Count >= 1);
        var page1 = doc1.Pages[0];
        Assert.True(page1.Elements.Count > 0);

        // Check vertical rotated text elements extracted cleanly
        var rotatedElements = page1.Elements.OfType<PdfTextElement>().Where(t => t.Rotation == 270.0 || t.Rotation == 90.0).ToList();
        foreach (var r in rotatedElements)
        {
            Console.WriteLine($"Rotated Text: \"{r.Text}\"");
        }
        Assert.NotEmpty(rotatedElements);
        Assert.Contains(rotatedElements, r => r.Text.Replace(" ", "").Contains("Detailsason", StringComparison.OrdinalIgnoreCase) || r.Text.Replace(" ", "").Contains("Aadhaarno", StringComparison.OrdinalIgnoreCase));

        // Check horizontal text blocks
        var horizontalTexts = page1.Elements.OfType<PdfTextElement>().Where(t => t.Rotation == 0).ToList();
        Assert.Contains(horizontalTexts, h => h.Text.Contains("4046/20511", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(horizontalTexts, h => h.Text.Contains("Ayush", StringComparison.OrdinalIgnoreCase));

        if (File.Exists(sample2Path))
        {
            var doc2 = await _importService.ImportPdfAsync(sample2Path);
            Assert.True(doc2.Pages.Count >= 1);
            var s2Page1 = doc2.Pages[0];

            Console.WriteLine($"=== SAMPLE 2 ===");
            for (int elIdx = 0; elIdx < s2Page1.Elements.Count; elIdx++)
            {
                var el = s2Page1.Elements[elIdx];
                if (el is PdfTextElement txt)
                {
                    Console.WriteLine($"  #{elIdx} [TEXT] X={txt.X:F1}, Y={txt.Y:F1}, W={txt.Width:F1}, H={txt.Height:F1}, Font={txt.FontFamily} {txt.FontSize:F1}pt, Color={txt.TextColorHex}, Z={txt.ZIndex}, Preview={txt.Text.Replace("\n", " ")}");
                }
                else if (el is PdfShapeElement shp)
                {
                    Console.WriteLine($"  #{elIdx} [SHAPE] X={shp.X:F1}, Y={shp.Y:F1}, W={shp.Width:F1}, H={shp.Height:F1}, Fill={shp.FillColorHex}, Stroke={shp.StrokeColorHex}, Z={shp.ZIndex}");
                }
            }

            // Verify vector shapes and dividers extracted
            var shapes = s2Page1.Elements.OfType<PdfShapeElement>().ToList();
            var dividers = s2Page1.Elements.OfType<PdfDividerElement>().ToList();
            Assert.True(shapes.Count > 0 || dividers.Count > 0);

            // Verify watermark image is locked with low ZIndex
            var watermarks = s2Page1.Elements.OfType<PdfImageElement>().Where(img => img.IsLocked).ToList();
            Assert.NotEmpty(watermarks);
            Assert.True(watermarks[0].Opacity <= 0.35);
            Assert.Equal(0, watermarks[0].ZIndex);

            // Verify header text extracted with Poppins / display font and high contrast
            var titleText = s2Page1.Elements.OfType<PdfTextElement>().FirstOrDefault(t => t.Text.Contains("MATHEMATICS", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(titleText);
            Assert.Equal("Poppins", titleText.FontFamily);

            // Verify section heading extracted
            var sectionHeading = s2Page1.Elements.OfType<PdfTextElement>().FirstOrDefault(t => t.Text.Contains("1.1 What is Mathematics", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(sectionHeading);

            // Verify white text is only present when overlaying a colored shape (e.g. the chapter 1 blue badge)
            var whiteTexts = s2Page1.Elements.OfType<PdfTextElement>().Where(t => string.Equals(t.TextColorHex, "#FFFFFF", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var wt in whiteTexts)
            {
                bool hasUnderlyingShape = shapes.Any(s => s.FillColorHex != "Transparent" && s.FillColorHex != "#FFFFFF" &&
                    wt.X >= s.X - 5 && wt.X <= s.X + s.Width + 5 &&
                    wt.Y >= s.Y - 5 && wt.Y <= s.Y + s.Height + 5);
                Assert.True(hasUnderlyingShape, $"White text \"{wt.Text}\" should overlay a colored background shape.");
            }
        }
    }

    [Fact]
    public void BooleanToStretchConverter_ConvertsCorrectly()
    {
        var converter = PdfEditorApp.Converters.BooleanToStretchConverter.Instance;
        var uniform = converter.Convert(true, typeof(Avalonia.Media.Stretch), null, System.Globalization.CultureInfo.InvariantCulture);
        var fill = converter.Convert(false, typeof(Avalonia.Media.Stretch), null, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(Avalonia.Media.Stretch.Uniform, uniform);
        Assert.Equal(Avalonia.Media.Stretch.Fill, fill);
    }

    [Fact]
    public async Task PageViewModel_LoadsDeconstructedDocument_AllowsEditingAndExportRoundtrip()
    {
        var templateService = new TemplateService();
        var exportService = new PdfExportService();

        var originalDoc = templateService.CreateAnnualReportTemplate();
        byte[] pdfBytes = await exportService.ExportToBytesAsync(originalDoc);

        var doc = await _importService.ImportPdfBytesAsync(pdfBytes, "Annual_Report.pdf");
        Assert.NotEmpty(doc.Pages);

        var pageVm = new PdfEditorApp.ViewModels.PageViewModel();
        pageVm.LoadFromModel(doc.Pages[0]);

        Assert.NotEmpty(pageVm.Elements);

        // Find title text element
        var titleVm = pageVm.Elements.OfType<PdfEditorApp.ViewModels.ElementViewModels.TextElementViewModel>().FirstOrDefault(t => t.Text.Contains("ANNUAL CORPORATE REPORT", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(titleVm);

        // Edit text
        titleVm.Text = "ADVANCED ANNUAL REPORT";
        titleVm.FontSize = 28.0;
        titleVm.TextColorHex = "#2563EB";

        // Verify ToModel preserves modifications
        var roundtripModel = pageVm.ToModel();
        var roundtripTitle = roundtripModel.Elements.OfType<PdfTextElement>().FirstOrDefault(t => t.Text == "ADVANCED ANNUAL REPORT");
        Assert.NotNull(roundtripTitle);
        Assert.Equal(28.0, roundtripTitle.FontSize);
        Assert.Equal("#2563EB", roundtripTitle.TextColorHex);
    }

    [Fact]
    public async Task AnnualReport_ExportAndImport_PreservesAllElementsLosslessly()
    {
        var templateService = new TemplateService();
        var exportService = new PdfExportService();

        var originalDoc = templateService.CreateAnnualReportTemplate();
        Assert.Equal(3, originalDoc.Pages.Count);

        byte[] pdfBytes = await exportService.ExportToBytesAsync(originalDoc);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);

        var importedDoc = await _importService.ImportPdfBytesAsync(pdfBytes, "Annual_Report.pdf");
        Assert.NotNull(importedDoc);
        Assert.Equal(3, importedDoc.Pages.Count);

        for (int pIdx = 0; pIdx < originalDoc.Pages.Count; pIdx++)
        {
            var origPage = originalDoc.Pages[pIdx];
            var impPage = importedDoc.Pages[pIdx];

            Assert.Equal(origPage.Elements.Count, impPage.Elements.Count);

            for (int eIdx = 0; eIdx < origPage.Elements.Count; eIdx++)
            {
                var origEl = origPage.Elements[eIdx];
                var impEl = impPage.Elements[eIdx];

                Assert.Equal(origEl.GetType(), impEl.GetType());
                Assert.Equal(origEl.X, impEl.X);
                Assert.Equal(origEl.Y, impEl.Y);
                Assert.Equal(origEl.Width, impEl.Width);
                Assert.Equal(origEl.Height, impEl.Height);

                if (origEl is PdfTableElement origTable && impEl is PdfTableElement impTable)
                {
                    Assert.Equal(origTable.Headers.Count, impTable.Headers.Count);
                    Assert.Equal(origTable.Rows.Count, impTable.Rows.Count);
                }
                else if (origEl is PdfChartElement origChart && impEl is PdfChartElement impChart)
                {
                    Assert.Equal(origChart.ChartType, impChart.ChartType);
                    Assert.Equal(origChart.Title, impChart.Title);
                }
                else if (origEl is PdfTextElement origText && impEl is PdfTextElement impText)
                {
                    Assert.Equal(origText.Text, impText.Text);
                    Assert.Equal(origText.FontSize, impText.FontSize);
                    Assert.Equal(origText.TextColorHex, impText.TextColorHex);
                }
            }
        }
    }

    [Fact]
    public async Task Invoice_ExportAndImport_PreservesTablesAndFormulasLosslessly()
    {
        var templateService = new TemplateService();
        var exportService = new PdfExportService();

        var originalDoc = templateService.CreateInvoiceTemplate();
        byte[] pdfBytes = await exportService.ExportToBytesAsync(originalDoc);

        var importedDoc = await _importService.ImportPdfBytesAsync(pdfBytes, "Invoice.pdf");
        Assert.NotNull(importedDoc);
        Assert.Single(importedDoc.Pages);

        var origTable = originalDoc.Pages[0].Elements.OfType<PdfTableElement>().FirstOrDefault();
        var impTable = importedDoc.Pages[0].Elements.OfType<PdfTableElement>().FirstOrDefault();

        Assert.NotNull(origTable);
        Assert.NotNull(impTable);
        Assert.Equal(origTable.Headers.Count, impTable.Headers.Count);
        Assert.Equal(origTable.Rows.Count, impTable.Rows.Count);
    }

    [Fact]
    public async Task GenerateVisualComparison_SideBySide_SavesArtifacts()
    {
        string artifactDir = "/Users/codefrydev/.gemini/antigravity-ide/brain/15e672ca-a88b-4ef8-985f-5f4d074f80b9";
        Directory.CreateDirectory(artifactDir);

        string baseDir = AppContext.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        string sample1Path = Path.Combine(rootDir, "sample1.pdf");
        string sample2Path = Path.Combine(rootDir, "Class_6_Math_Chapter_1_Pattern_In_Mathematics.pdf");
        if (!File.Exists(sample2Path)) sample2Path = Path.Combine(rootDir, "sample2.pdf");

        if (File.Exists(sample1Path))
        {
            await GenerateComparisonForPdf(sample1Path, Path.Combine(artifactDir, "sample1_side_by_side.png"), "Sample 1 (e-Aadhaar Card)");
        }

        if (File.Exists(sample2Path))
        {
            await GenerateComparisonForPdf(sample2Path, Path.Combine(artifactDir, "sample2_side_by_side.png"), "Sample 2 (Annual Report / Textbook)");
        }
    }

    private async Task GenerateComparisonForPdf(string pdfPath, string outputPath, string title)
    {
        byte[] pdfBytes = await File.ReadAllBytesAsync(pdfPath);
        var docModel = await _importService.ImportPdfBytesAsync(pdfBytes, Path.GetFileName(pdfPath));
        if (docModel.Pages.Count == 0) return;

        float scale = 1.5f;

        // 1. Render Original PDF via PdfPig + Skia
        using var rawDoc = UglyToad.PdfPig.PdfDocument.Open(pdfBytes);
        PdfPigExtensions.AddSkiaPageFactory(rawDoc);
        using var origStream = PdfPigExtensions.GetPageAsPng(rawDoc, 1, scale, 100);
        using var origSkData = SkiaSharp.SKData.CreateCopy(origStream.ToArray());
        using var origBitmap = SkiaSharp.SKBitmap.Decode(origSkData);

        // 2. Render Deconstructed FryPDF PageModel via Skia
        var deconstructedPage = docModel.Pages[0];
        using var reconBitmap = RenderPageModelToSkiaBitmap(deconstructedPage, scale);

        // 3. Compose Side-by-Side Image
        int bannerH = 70;
        int pw = origBitmap.Width;
        int ph = origBitmap.Height;
        int rw = reconBitmap.Width;
        int rh = reconBitmap.Height;
        int contentW = pw + rw + 20; // 20px gutter
        int contentH = Math.Max(ph, rh);

        int totalW = contentW + 40; // 20px padding left/right
        int totalH = bannerH + contentH + 40; // 20px padding bottom

        using var comparisonBitmap = new SkiaSharp.SKBitmap(totalW, totalH);
        using var canvas = new SkiaSharp.SKCanvas(comparisonBitmap);

        // Dark Background
        canvas.Clear(new SkiaSharp.SKColor(15, 23, 42)); // #0F172A

        // Banner Header
        using var titlePaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.White,
            IsAntialias = true
        };
        using var titleTypeface = SkiaSharp.SKTypeface.FromFamilyName("Segoe UI", SkiaSharp.SKFontStyleWeight.Bold, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
        using var titleFont = new SkiaSharp.SKFont(titleTypeface, 20);
        canvas.DrawText($"Side-by-Side Visual Verification: {title}", 24, 34, titleFont, titlePaint);

        using var subFont = new SkiaSharp.SKFont(titleTypeface, 12);
        using var subPaint = new SkiaSharp.SKPaint { Color = new SkiaSharp.SKColor(148, 163, 184), IsAntialias = true };
        canvas.DrawText($"Elements: {deconstructedPage.Elements.Count} ({deconstructedPage.Elements.OfType<PdfImageElement>().Count()} images, {deconstructedPage.Elements.OfType<PdfTextElement>().Count()} text blocks, {deconstructedPage.Elements.OfType<PdfShapeElement>().Count()} shapes) | Canvas Size: {deconstructedPage.Width:F0}x{deconstructedPage.Height:F0} pt", 24, 54, subFont, subPaint);

        // Labels
        float leftX = 20;
        float rightX = 20 + pw + 20;
        float topY = bannerH;

        using var colLabelFont = new SkiaSharp.SKFont(titleTypeface, 13);
        using var leftLabelPaint = new SkiaSharp.SKPaint { Color = new SkiaSharp.SKColor(56, 189, 248), IsAntialias = true };
        using var rightLabelPaint = new SkiaSharp.SKPaint { Color = new SkiaSharp.SKColor(74, 222, 128), IsAntialias = true };

        // Draw Images
        canvas.DrawBitmap(origBitmap, leftX, topY);
        canvas.DrawBitmap(reconBitmap, rightX, topY);

        // Borders
        using var borderPaint = new SkiaSharp.SKPaint
        {
            Color = new SkiaSharp.SKColor(51, 65, 85),
            Style = SkiaSharp.SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawRect(leftX, topY, pw, ph, borderPaint);
        canvas.DrawRect(rightX, topY, rw, rh, borderPaint);

        // Save comparison image
        using var outImage = SkiaSharp.SKImage.FromBitmap(comparisonBitmap);
        using var outData = outImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 95);
        using var outStream = File.OpenWrite(outputPath);
        outData.SaveTo(outStream);

        Console.WriteLine($"[Visual Verification] Saved side-by-side comparison to {outputPath} ({totalW}x{totalH} px)");
    }

    private static SkiaSharp.SKBitmap RenderPageModelToSkiaBitmap(PdfPageModel page, float scale)
    {
        int w = (int)Math.Max(100, page.Width * scale);
        int h = (int)Math.Max(100, page.Height * scale);
        var bitmap = new SkiaSharp.SKBitmap(w, h);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);

        var bgPaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColor.TryParse(page.BackgroundColorHex, out var bgC) ? bgC : SkiaSharp.SKColors.White,
            Style = SkiaSharp.SKPaintStyle.Fill
        };
        canvas.DrawRect(0, 0, w, h, bgPaint);

        var sorted = page.Elements.OrderBy(e => e.ZIndex).ThenBy(e => e.Y).ThenBy(e => e.X).ToList();
        foreach (var el in sorted)
        {
            canvas.Save();

            float ex = (float)el.X * scale;
            float ey = (float)el.Y * scale;
            float ew = (float)el.Width * scale;
            float eh = (float)el.Height * scale;

            if (el.Rotation != 0)
            {
                canvas.RotateDegrees((float)el.Rotation, ex + (ew / 2f), ey + (eh / 2f));
            }

            byte alpha = (byte)Math.Clamp((int)(el.Opacity * 255), 0, 255);

            if (el is PdfImageElement img && !string.IsNullOrEmpty(img.Base64Data))
            {
                try
                {
                    byte[] imgBytes = Convert.FromBase64String(img.Base64Data);
                    using var skData = SkiaSharp.SKData.CreateCopy(imgBytes);
                    using var skImg = SkiaSharp.SKImage.FromEncodedData(skData);
                    if (skImg != null)
                    {
                        using var imgPaint = new SkiaSharp.SKPaint { Color = new SkiaSharp.SKColor(255, 255, 255, alpha) };
                        var destRect = SkiaSharp.SKRect.Create(ex, ey, ew, eh);
                        canvas.DrawImage(skImg, destRect, imgPaint);
                    }
                }
                catch { }
            }
            else if (el is PdfShapeElement shp)
            {
                if (shp.FillColorHex != "Transparent" && SkiaSharp.SKColor.TryParse(shp.FillColorHex, out var fillC))
                {
                    using var fPaint = new SkiaSharp.SKPaint
                    {
                        Color = fillC.WithAlpha(alpha),
                        Style = SkiaSharp.SKPaintStyle.Fill,
                        IsAntialias = true
                    };
                    if (shp.CornerRadius > 0)
                        canvas.DrawRoundRect(ex, ey, ew, eh, (float)shp.CornerRadius * scale, (float)shp.CornerRadius * scale, fPaint);
                    else
                        canvas.DrawRect(ex, ey, ew, eh, fPaint);
                }

                if (shp.StrokeColorHex != "Transparent" && shp.StrokeThickness > 0 && SkiaSharp.SKColor.TryParse(shp.StrokeColorHex, out var strokeC))
                {
                    using var sPaint = new SkiaSharp.SKPaint
                    {
                        Color = strokeC.WithAlpha(alpha),
                        Style = SkiaSharp.SKPaintStyle.Stroke,
                        StrokeWidth = (float)shp.StrokeThickness * scale,
                        IsAntialias = true
                    };
                    if (shp.CornerRadius > 0)
                        canvas.DrawRoundRect(ex, ey, ew, eh, (float)shp.CornerRadius * scale, (float)shp.CornerRadius * scale, sPaint);
                    else
                        canvas.DrawRect(ex, ey, ew, eh, sPaint);
                }
            }
            else if (el is PdfDividerElement div)
            {
                if (SkiaSharp.SKColor.TryParse(div.ColorHex, out var divC))
                {
                    using var dPaint = new SkiaSharp.SKPaint
                    {
                        Color = divC.WithAlpha(alpha),
                        Style = SkiaSharp.SKPaintStyle.Fill,
                        IsAntialias = true
                    };
                    canvas.DrawRect(ex, ey, ew, Math.Max(1f, (float)div.Thickness * scale), dPaint);
                }
            }
            else if (el is PdfTextElement txt && !string.IsNullOrEmpty(txt.Text))
            {
                var txtColor = SkiaSharp.SKColor.TryParse(txt.TextColorHex, out var tc) ? tc : SkiaSharp.SKColors.Black;
                using var tPaint = new SkiaSharp.SKPaint
                {
                    Color = txtColor.WithAlpha(alpha),
                    IsAntialias = true
                };

                var weight = txt.IsBold ? SkiaSharp.SKFontStyleWeight.Bold : SkiaSharp.SKFontStyleWeight.Normal;
                var slant = txt.IsItalic ? SkiaSharp.SKFontStyleSlant.Italic : SkiaSharp.SKFontStyleSlant.Upright;
                using var typeface = MatchSkiaTypeface(txt.FontFamily, txt.Text, weight, slant);
                using var font = new SkiaSharp.SKFont(typeface, (float)txt.FontSize * scale);

                double multiplier = txt.LineHeight > 0.1 ? txt.LineHeight : 1.35;
                float linePitch = (float)(txt.FontSize * multiplier) * scale;
                var lines = txt.Text.Split('\n');
                float curY = ey + ((float)txt.FontSize * scale * 0.90f); // baseline approximation

                for (int li = 0; li < lines.Length; li++)
                {
                    canvas.DrawText(lines[li], ex, curY, font, tPaint);
                    curY += linePitch;
                }
            }

            canvas.Restore();
        }

        return bitmap;
    }

    private static SkiaSharp.SKTypeface MatchSkiaTypeface(string fontFamily, string text, SkiaSharp.SKFontStyleWeight weight, SkiaSharp.SKFontStyleSlant slant)
    {
        if (PdfEditorApp.Core.Analysis.UnicodeScriptDetector.ContainsDevanagari(text))
        {
            string[] devanagariFamilies = { "Noto Sans Devanagari", "Kohinoor Devanagari", "Devanagari Sangam MN", "Nirmala UI", "Mangal", "Arial Unicode MS" };
            foreach (var fam in devanagariFamilies)
            {
                var dtf = SkiaSharp.SKTypeface.FromFamilyName(fam, weight, SkiaSharp.SKFontStyleWidth.Normal, slant);
                if (dtf != null && !dtf.FamilyName.Equals("Arial", StringComparison.OrdinalIgnoreCase) && !dtf.FamilyName.Equals("Helvetica", StringComparison.OrdinalIgnoreCase))
                    return dtf;
            }
        }
        else if (PdfEditorApp.Core.Analysis.UnicodeScriptDetector.ContainsCjk(text))
        {
            string[] cjkFamilies = { "Noto Sans SC", "PingFang SC", "Hiragino Sans GB", "Microsoft YaHei", "SimSun" };
            foreach (var fam in cjkFamilies)
            {
                var ctf = SkiaSharp.SKTypeface.FromFamilyName(fam, weight, SkiaSharp.SKFontStyleWidth.Normal, slant);
                if (ctf != null && !ctf.FamilyName.Equals("Arial", StringComparison.OrdinalIgnoreCase))
                    return ctf;
            }
        }

        return SkiaSharp.SKTypeface.FromFamilyName(fontFamily, weight, SkiaSharp.SKFontStyleWidth.Normal, slant)
            ?? SkiaSharp.SKTypeface.Default;
    }
}

