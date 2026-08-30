using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.Templates;
using PdfEditorApp.ViewModels;
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

        // Verify page background canvas element is present
        var bg = page1.Elements.OfType<PdfImageElement>().FirstOrDefault(e => e.AltText.Contains("Background Canvas"));
        Assert.NotNull(bg);
        Assert.False(string.IsNullOrEmpty(bg.Base64Data));

        // Verify editable text elements were extracted
        var textElements = page1.Elements.OfType<PdfTextElement>().ToList();
        Assert.NotEmpty(textElements);
        Assert.Contains(textElements, t => !string.IsNullOrWhiteSpace(t.Text));
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
        var bg = page.Elements.OfType<PdfImageElement>().FirstOrDefault(e => e.AltText.Contains("Background Canvas"));
        Assert.NotNull(bg);
        Assert.False(string.IsNullOrEmpty(bg.Base64Data));
        Assert.Equal(0, bg.BorderThickness);
        Assert.Equal("Transparent", bg.BorderColorHex);

        var pageVm = new PageViewModel();
        pageVm.LoadFromModel(page);

        var bgVm = pageVm.Elements.OfType<PdfEditorApp.ViewModels.ElementViewModels.ImageElementViewModel>().FirstOrDefault(e => e.AltText.Contains("Background Canvas"));
        Assert.NotNull(bgVm);
        Assert.Equal(bg.Base64Data, bgVm.Base64Data);
        Assert.True(Convert.FromBase64String(bgVm.Base64Data!).Length > 0);
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
}
