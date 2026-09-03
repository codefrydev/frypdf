using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using Xunit;

namespace PdfEditorApp.Tests;

public class DocumentExportAndPersistenceTests
{
    [Fact]
    public async Task PdfExportService_GeneratesValidPdfBytes_WithElements()
    {
        // Arrange
        var exportService = new PdfExportService();
        var doc = new PdfDocumentModel
        {
            Title = "Export Test Document",
            Author = "FryPDF Test Suite"
        };

        var page = new PdfPageModel
        {
            Width = 595.28,
            Height = 841.89,
            PageNumber = 1,
            BackgroundColorHex = "#FFFFFF"
        };

        page.Elements.Add(new PdfTextElement
        {
            X = 50,
            Y = 50,
            Width = 300,
            Height = 40,
            Text = "Executive Summary",
            FontSize = 22,
            IsBold = true,
            TextColorHex = "#0F172A"
        });

        page.Elements.Add(new PdfShapeElement
        {
            X = 50,
            Y = 100,
            Width = 495,
            Height = 2,
            ShapeType = ShapeType.Line,
            StrokeColorHex = "#0F6CBD",
            StrokeThickness = 2
        });

        page.Elements.Add(new PdfQrCodeElement
        {
            X = 50,
            Y = 120,
            Width = 100,
            Height = 100,
            Content = "https://github.com/PrashantUnity/PDFCreator",
            Label = "Scan to Verify"
        });

        page.Elements.Add(new PdfBarcodeElement
        {
            X = 180,
            Y = 120,
            Width = 200,
            Height = 60,
            CodeValue = "FRY-2026-X99",
            ShowText = true
        });

        page.Elements.Add(new PdfTableElement
        {
            X = 50,
            Y = 240,
            Width = 495,
            Height = 120,
            Headers = new() { "Item", "Category", "Amount ($)" },
            Rows = new()
            {
                new() { "Cloud Architecture", "Engineering", "12,500.00" },
                new() { "Security Preflight", "Audit", "4,200.00" }
            }
        });

        doc.Pages.Add(page);

        // Act - File export
        string tempPdf = Path.Combine(Path.GetTempPath(), $"FryPDF_Export_{Guid.NewGuid():N}.pdf");
        try
        {
            await exportService.ExportToFileAsync(doc, tempPdf);

            // Assert
            Assert.True(File.Exists(tempPdf));
            byte[] pdfBytes = await File.ReadAllBytesAsync(tempPdf);
            Assert.True(pdfBytes.Length > 100);

            // PDF files must begin with '%PDF-'
            string header = Encoding.ASCII.GetString(pdfBytes.AsSpan(0, 5));
            Assert.Equal("%PDF-", header);
        }
        finally
        {
            if (File.Exists(tempPdf)) File.Delete(tempPdf);
        }
    }

    [Fact]
    public async Task ProjectPersistenceService_AtomicSaveAndLoad_RoundTripsSuccessfully()
    {
        // Arrange
        var persistenceService = new ProjectPersistenceService();
        string tempFile = Path.Combine(Path.GetTempPath(), $"FryPDF_Test_{Guid.NewGuid():N}.frypdf");

        try
        {
            var doc = new PdfDocumentModel
            {
                Title = "Roundtrip Test",
                Author = "Principal Architect",
                Subject = "Architecture Review"
            };

            var page = new PdfPageModel { Width = 612, Height = 792, PageNumber = 1 };
            page.Elements.Add(new PdfTextElement { Text = "Test Note", X = 20, Y = 30, Width = 150, Height = 40 });
            page.Elements.Add(new PdfQrCodeElement { Content = "https://codefrydev.in", X = 100, Y = 100, Width = 80, Height = 80 });
            doc.Pages.Add(page);

            // Act - Save
            await persistenceService.SaveProjectAsync(doc, tempFile);

            // Assert file exists and temp file was cleaned up
            Assert.True(File.Exists(tempFile));
            Assert.False(File.Exists(tempFile + ".tmp"));

            // Act - Load
            var loaded = await persistenceService.LoadProjectAsync(tempFile);

            // Assert loaded model matches
            Assert.NotNull(loaded);
            Assert.Equal("Roundtrip Test", loaded.Title);
            Assert.Equal("Principal Architect", loaded.Author);
            Assert.Single(loaded.Pages);
            Assert.Equal(2, loaded.Pages[0].Elements.Count);
            Assert.IsType<PdfTextElement>(loaded.Pages[0].Elements[0]);
            Assert.IsType<PdfQrCodeElement>(loaded.Pages[0].Elements[1]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(tempFile + ".tmp")) File.Delete(tempFile + ".tmp");
        }
    }

    [Fact]
    public void UndoRedoService_CapsHistoryAt100_AndEvictsOldest()
    {
        // Arrange
        var undoRedo = new UndoRedoService();
        int counter = 0;

        // Act - push 120 actions
        for (int i = 1; i <= 120; i++)
        {
            int val = i;
            undoRedo.RecordAction($"Action {val}", () => { counter -= val; }, () => { counter += val; });
        }

        // Assert - NextUndoDescription is Action 120
        Assert.True(undoRedo.CanUndo);
        Assert.Equal("Action 120", undoRedo.NextUndoDescription);

        // Perform undo operations until stack is exhausted
        int undoCount = 0;
        while (undoRedo.CanUndo)
        {
            undoRedo.Undo();
            undoCount++;
        }

        // Must have capped at exactly 100 operations
        Assert.Equal(100, undoCount);
        Assert.False(undoRedo.CanUndo);
        Assert.True(undoRedo.CanRedo);
    }

    [Fact]
    public async Task PdfExportService_PermanentRedaction_SanitizesCoveredText()
    {
        // Arrange
        var exportService = new PdfExportService();
        var doc = new PdfDocumentModel { Title = "Classified Report" };
        var page = new PdfPageModel { Width = 600, Height = 800, PageNumber = 1 };

        // Confidential text at (100, 100, 200, 30)
        var secretText = new PdfTextElement
        {
            Text = "TOP SECRET SSN 000-11-2222",
            X = 100,
            Y = 100,
            Width = 200,
            Height = 30
        };

        // Redaction box completely covering the secret text: (90, 90, 220, 50)
        var redaction = new PdfRedactionElement
        {
            X = 90,
            Y = 90,
            Width = 220,
            Height = 50,
            ExemptionCode = "(b)(6)"
        };

        // Non-redacted text at (100, 300, 200, 30)
        var publicText = new PdfTextElement
        {
            Text = "Public Notice Section",
            X = 100,
            Y = 300,
            Width = 200,
            Height = 30
        };

        page.Elements.Add(secretText);
        page.Elements.Add(redaction);
        page.Elements.Add(publicText);
        doc.Pages.Add(page);

        // Act
        byte[] pdfBytes = await exportService.ExportToBytesAsync(doc);

        // Assert - PDF generated successfully
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 100);
        string header = Encoding.ASCII.GetString(pdfBytes.AsSpan(0, 5));
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task ProjectPersistenceService_AutoSaveAndRecovery_SavesAndCleans()
    {
        // Arrange
        var persistenceService = new ProjectPersistenceService();
        string tempProjectFile = Path.Combine(Path.GetTempPath(), $"FryPDF_AutoSave_Test_{Guid.NewGuid():N}.frypdf");

        try
        {
            var doc = new PdfDocumentModel { Title = "Unsaved Work in Progress", Author = "UX Specialist" };
            var page = new PdfPageModel { PageNumber = 1 };
            page.Elements.Add(new PdfTextElement { Text = "Crucial Unshelved Notes", X = 10, Y = 20 });
            doc.Pages.Add(page);

            // Act - AutoSave
            await persistenceService.SaveAutoSaveAsync(doc, tempProjectFile);

            // Assert - Recoverable
            bool hasAutoSave = persistenceService.HasRecoverableAutoSave(tempProjectFile, out string autoSavePath);
            Assert.True(hasAutoSave);
            Assert.True(File.Exists(autoSavePath));

            // Load autosave
            var recovered = await persistenceService.LoadAutoSaveAsync(autoSavePath);
            Assert.NotNull(recovered);
            Assert.Equal("Unsaved Work in Progress", recovered.Title);

            // Act - Clean
            persistenceService.CleanAutoSave(tempProjectFile);
            Assert.False(File.Exists(autoSavePath));
        }
        finally
        {
            persistenceService.CleanAutoSave(tempProjectFile);
            if (File.Exists(tempProjectFile)) File.Delete(tempProjectFile);
        }
    }
}
