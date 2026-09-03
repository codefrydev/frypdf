using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.ViewModels;
using QuestPDF.Fluent;
using UglyToad.PdfPig;
using Xunit;

namespace PdfEditorApp.Tests;

public class DocumentMetadataTests
{
    [Fact]
    public void PdfDocumentModel_DefaultValues_AreFryPDFAndCodefrydevIn()
    {
        var model = new PdfDocumentModel();

        Assert.Equal("FryPDF", model.Creator);
        Assert.Equal("codefrydev.in", model.Producer);

        var clone = model.Clone();
        Assert.Equal("FryPDF", clone.Creator);
        Assert.Equal("codefrydev.in", clone.Producer);
    }

    [Fact]
    public async Task PdfExportService_EmbedsFryPDFCreator_AndCodefrydevInProducer()
    {
        var exportService = new PdfExportService();
        var model = new PdfDocumentModel
        {
            Title = "Metadata_Verification_Doc.pdf",
            Author = "Code Fry Dev Unit Test",
            Subject = "PDF Metadata Producer and Creator Verification",
            Keywords = "test, metadata, pdf",
            Creator = "FryPDF",
            Producer = "codefrydev.in"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Width = 595.28,
            Height = 841.89
        };
        page.Elements.Add(new PdfTextElement
        {
            Text = "Testing Producer: codefrydev.in and Creator: FryPDF",
            X = 50,
            Y = 50,
            Width = 400,
            Height = 30
        });
        model.Pages.Add(page);

        byte[] pdfBytes = await exportService.ExportToBytesAsync(model);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);

        using var pigDoc = UglyToad.PdfPig.PdfDocument.Open(pdfBytes);
        Assert.NotNull(pigDoc.Information);
        Assert.Equal("FryPDF", pigDoc.Information.Creator);
        Assert.Equal("codefrydev.in", pigDoc.Information.Producer);
        Assert.Equal("Metadata_Verification_Doc.pdf", pigDoc.Information.Title);
        Assert.Equal("Code Fry Dev Unit Test", pigDoc.Information.Author);
    }

    [Fact]
    public async Task PdfViewerViewModel_MetadataInspection_ShowsCreatorFryPDFAndProducerCodefrydevIn()
    {
        var exportService = new PdfExportService();
        var model = new PdfDocumentModel
        {
            Title = "Annual_Report_2026.pdf",
            Author = "ACME CORP.",
            Subject = "Fiscal Year 2026 Annual Report"
        };
        var page = new PdfPageModel { PageNumber = 1, Width = 800, Height = 1131 };
        page.Elements.Add(new PdfTextElement { Text = "Annual Report Overview", X = 50, Y = 50 });
        model.Pages.Add(page);

        byte[] pdfBytes = await exportService.ExportToBytesAsync(model);

        var viewerVm = new PdfViewerViewModel();
        await viewerVm.LoadDocumentBytesAsync(pdfBytes, "Annual_Report_2026.pdf");

        var creatorItem = viewerVm.MetadataItems.FirstOrDefault(m => m.Label == "Creator Application");
        var producerItem = viewerVm.MetadataItems.FirstOrDefault(m => m.Label == "PDF Producer");

        Assert.NotNull(creatorItem);
        Assert.Equal("FryPDF", creatorItem.Value);

        Assert.NotNull(producerItem);
        Assert.Equal("codefrydev.in", producerItem.Value);
    }

    [Fact]
    public void FryPdfDocument_GeneratesPdf_WithBrandedMetadata()
    {
        var doc = FryPdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Content().Text("Branded PDF Document");
            });
        }, title: "Branded Document", author: "Code Fry Dev");

        byte[] pdfBytes = doc.GeneratePdf();
        Assert.NotNull(pdfBytes);

        using var pigDoc = UglyToad.PdfPig.PdfDocument.Open(pdfBytes);
        Assert.Equal("FryPDF", pigDoc.Information.Creator);
        Assert.Equal("codefrydev.in", pigDoc.Information.Producer);
    }

    [Fact]
    public async Task PdfPageService_Merge_SetsFryPDFCreatorAndCodefrydevInProducer()
    {
        var exportService = new PdfExportService();
        var pageService = new PdfPageService();

        string temp1 = Path.Combine(Path.GetTempPath(), $"merge1_{Guid.NewGuid():N}.pdf");
        string temp2 = Path.Combine(Path.GetTempPath(), $"merge2_{Guid.NewGuid():N}.pdf");
        string tempMerged = Path.Combine(Path.GetTempPath(), $"merged_{Guid.NewGuid():N}.pdf");

        try
        {
            var doc1 = new PdfDocumentModel { Title = "Doc 1" };
            doc1.Pages.Add(new PdfPageModel { PageNumber = 1 });
            var doc2 = new PdfDocumentModel { Title = "Doc 2" };
            doc2.Pages.Add(new PdfPageModel { PageNumber = 1 });

            await exportService.ExportToFileAsync(doc1, temp1);
            await exportService.ExportToFileAsync(doc2, temp2);

            var options = new MergeToolOptions
            {
                InputFiles = new() { temp1, temp2 },
                OutputFilePath = tempMerged
            };

            var result = await pageService.MergePdfAsync(options);
            Assert.True(result.Success);
            Assert.True(File.Exists(tempMerged));

            using var pigDoc = UglyToad.PdfPig.PdfDocument.Open(tempMerged);
            Assert.Equal("FryPDF", pigDoc.Information.Creator);
            Assert.Equal("codefrydev.in", pigDoc.Information.Producer);
        }
        finally
        {
            if (File.Exists(temp1)) File.Delete(temp1);
            if (File.Exists(temp2)) File.Delete(temp2);
            if (File.Exists(tempMerged)) File.Delete(tempMerged);
        }
    }
}
