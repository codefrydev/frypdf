using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PdfEngineTests
{
    private readonly IPdfExportService _exportService = new PdfExportService();
    private readonly ITemplateService _templateService = new TemplateService();
    private readonly IProjectPersistenceService _persistenceService = new ProjectPersistenceService();

    [Fact]
    public void GenerateAnnualReport_ProducesValidPdfBytes()
    {
        var model = _templateService.CreateAnnualReportTemplate();
        Assert.NotNull(model);
        Assert.Equal(3, model.Pages.Count);

        byte[] pdfBytes = _exportService.GeneratePdfBytes(model);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);

        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void GenerateInvoice_ProducesValidPdfBytes()
    {
        var model = _templateService.CreateInvoiceTemplate();
        Assert.NotNull(model);
        Assert.Single(model.Pages);

        byte[] pdfBytes = _exportService.GeneratePdfBytes(model);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 500);

        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void GenerateResume_ProducesValidPdfBytes()
    {
        var model = _templateService.CreateResumeTemplate();
        Assert.NotNull(model);
        Assert.Single(model.Pages);

        byte[] pdfBytes = _exportService.GeneratePdfBytes(model);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 500);

        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void GenerateAcademicPaperAndCertificate_ProducesValidPdfBytes()
    {
        var paper = _templateService.CreateAcademicPaperTemplate();
        Assert.NotNull(paper);
        byte[] paperBytes = _exportService.GeneratePdfBytes(paper);
        Assert.NotNull(paperBytes);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(paperBytes, 0, 5));

        var cert = _templateService.CreateCertificateTemplate();
        Assert.NotNull(cert);
        byte[] certBytes = _exportService.GeneratePdfBytes(cert);
        Assert.NotNull(certBytes);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(certBytes, 0, 5));
    }

    [Fact]
    public async Task ProjectPersistence_RoundTripMatches()
    {
        var original = _templateService.CreateAnnualReportTemplate();
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_project_{Guid.NewGuid():N}.pdfproj");

        try
        {
            await _persistenceService.SaveProjectAsync(original, tempPath);
            Assert.True(File.Exists(tempPath));

            var loaded = await _persistenceService.LoadProjectAsync(tempPath);
            Assert.NotNull(loaded);
            Assert.Equal(original.Title, loaded.Title);
            Assert.Equal(original.Pages.Count, loaded.Pages.Count);
            Assert.Equal(original.Pages[0].Elements.Count, loaded.Pages[0].Elements.Count);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void UndoRedo_StackOperations_FunctionCorrectly()
    {
        var undoRedo = new UndoRedoService();
        int val = 0;

        Assert.False(undoRedo.CanUndo);
        Assert.False(undoRedo.CanRedo);

        undoRedo.RecordAction("Increment", () => val = 0, () => val = 10);
        val = 10;
        Assert.True(undoRedo.CanUndo);
        Assert.False(undoRedo.CanRedo);

        undoRedo.Undo();
        Assert.Equal(0, val);
        Assert.False(undoRedo.CanUndo);
        Assert.True(undoRedo.CanRedo);

        undoRedo.Redo();
        Assert.Equal(10, val);
        Assert.True(undoRedo.CanUndo);
        Assert.False(undoRedo.CanRedo);
    }

    [Fact]
    public void MainViewModel_FullFeatureSuite_WorksCorrectly()
    {
        var vm = new MainViewModel(_exportService, _templateService, _persistenceService);
        Assert.Equal(3, vm.Pages.Count);
        Assert.NotNull(vm.CurrentPage);

        // Add page
        vm.AddPageCommand.Execute(null);
        Assert.Equal(4, vm.Pages.Count);

        // Test Element Additions
        int initialCount = vm.CurrentPage.Elements.Count;
        vm.AddTextElementCommand.Execute(null);
        vm.AddHeadingElementCommand.Execute(null);
        vm.AddShapeElementCommand.Execute("Circle");
        vm.AddTableElementCommand.Execute(null);
        vm.AddChartElementCommand.Execute(null);
        vm.AddWatermarkElementCommand.Execute(null);
        vm.AddStampElementCommand.Execute("Approved");
        vm.AddStickyNoteElementCommand.Execute(null);

        Assert.Equal(initialCount + 8, vm.CurrentPage.Elements.Count);

        // Clipboard Copy & Paste & Duplicate
        vm.CopyCommand.Execute(null);
        vm.PasteCommand.Execute(null);
        Assert.Equal(initialCount + 9, vm.CurrentPage.Elements.Count);

        vm.DuplicateCommand.Execute(null);
        Assert.Equal(initialCount + 10, vm.CurrentPage.Elements.Count);

        // Reordering & Rotation
        int rotation = vm.CurrentPage.RotationAngle;
        vm.RotateCurrentPageCommand.Execute(null);
        Assert.Equal((rotation + 90) % 360, vm.CurrentPage.RotationAngle);

        vm.RotateCurrentPageCounterClockwiseCommand.Execute(null);
        Assert.Equal(rotation, vm.CurrentPage.RotationAngle);
    }
}
