using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests;

public class PdfToolViewModelsTests
{
    private readonly IPdfDocumentOperationsService _operationsService;
    private readonly IPdfToolRegistry _toolRegistry;
    private readonly IPdfToolViewModelFactory _factory;

    public PdfToolViewModelsTests()
    {
        _toolRegistry = new PdfToolRegistry();
        var pageService = new PdfPageService();
        var optService = new PdfOptimizationService();
        var secService = new PdfSecurityService();
        var convService = new PdfConversionService();
        var ocrService = new PdfOcrService();
        var formService = new PdfFormService();
        var aiService = new AiDocumentService();
        var transService = new DocumentTranslationService();
        var workflowEngine = new PdfWorkflowEngine(pageService, optService, secService, convService, ocrService);

        _operationsService = new PdfDocumentOperationsService(
            _toolRegistry, pageService, optService, secService, convService, ocrService, formService, aiService, transService, workflowEngine);

        _factory = new PdfToolViewModelFactory(_operationsService, _toolRegistry);
    }

    [Fact]
    public void Factory_CreatesDedicatedViewModelsForAllTools()
    {
        var allTools = _toolRegistry.GetAllTools();
        Assert.NotEmpty(allTools);

        foreach (var def in allTools)
        {
            if (def.Id == PdfToolId.WorkflowBuilder) continue;

            var vm = _factory.Create(def.Id);
            Assert.NotNull(vm);
            Assert.Equal(def.Id, vm.Tool.Id);
            Assert.Equal(def.Name, vm.Tool.Name);
            Assert.False(vm.IsRunning);
            Assert.False(vm.IsComplete);
            Assert.False(vm.HasError);
        }
    }

    [Fact]
    public void Factory_InstantiatesExactDerivedTypeForSpecificTools()
    {
        Assert.IsType<MergePdfToolViewModel>(_factory.Create(PdfToolId.MergePdf));
        Assert.IsType<SplitPdfToolViewModel>(_factory.Create(PdfToolId.SplitPdf));
        Assert.IsType<CompressPdfToolViewModel>(_factory.Create(PdfToolId.CompressPdf));
        Assert.IsType<WatermarkToolViewModel>(_factory.Create(PdfToolId.Watermark));
        Assert.IsType<RotatePdfToolViewModel>(_factory.Create(PdfToolId.RotatePdf));
        Assert.IsType<ProtectPdfToolViewModel>(_factory.Create(PdfToolId.ProtectPdf));
        Assert.IsType<UnlockPdfToolViewModel>(_factory.Create(PdfToolId.UnlockPdf));
        Assert.IsType<ComparePdfToolViewModel>(_factory.Create(PdfToolId.ComparePdf));
        Assert.IsType<HtmlToPdfToolViewModel>(_factory.Create(PdfToolId.HtmlToPdf));
        Assert.IsType<SignPdfToolViewModel>(_factory.Create(PdfToolId.SignPdf));
        Assert.IsType<OcrPdfToolViewModel>(_factory.Create(PdfToolId.OcrPdf));
        Assert.IsType<AiSummarizerToolViewModel>(_factory.Create(PdfToolId.AiSummarizer));
        Assert.IsType<TranslatePdfToolViewModel>(_factory.Create(PdfToolId.TranslatePdf));
        Assert.IsType<PdfToMarkdownToolViewModel>(_factory.Create(PdfToolId.PdfToMarkdown));
        Assert.IsType<PageNumbersToolViewModel>(_factory.Create(PdfToolId.PageNumbers));
        Assert.IsType<ScanToPdfToolViewModel>(_factory.Create(PdfToolId.ScanToPdf));
        Assert.IsType<CropPdfToolViewModel>(_factory.Create(PdfToolId.CropPdf));
        Assert.IsType<PdfFormsToolViewModel>(_factory.Create(PdfToolId.PdfForms));
        Assert.IsType<RedactPdfToolViewModel>(_factory.Create(PdfToolId.RedactPdf));
        Assert.IsType<OrganizePdfToolViewModel>(_factory.Create(PdfToolId.OrganizePdf));
        Assert.IsType<PdfToPdfAToolViewModel>(_factory.Create(PdfToolId.PdfToPdfA));
        Assert.IsType<RepairPdfToolViewModel>(_factory.Create(PdfToolId.RepairPdf));
        Assert.IsType<PdfToJpgToolViewModel>(_factory.Create(PdfToolId.PdfToJpg));
        Assert.IsType<JpgToPdfToolViewModel>(_factory.Create(PdfToolId.JpgToPdf));
        Assert.IsType<PdfToWordToolViewModel>(_factory.Create(PdfToolId.PdfToWord));
        Assert.IsType<WordToPdfToolViewModel>(_factory.Create(PdfToolId.WordToPdf));
        Assert.IsType<PdfToExcelToolViewModel>(_factory.Create(PdfToolId.PdfToExcel));
        Assert.IsType<ExcelToPdfToolViewModel>(_factory.Create(PdfToolId.ExcelToPdf));
        Assert.IsType<PdfToPowerPointToolViewModel>(_factory.Create(PdfToolId.PdfToPowerPoint));
        Assert.IsType<PowerPointToPdfToolViewModel>(_factory.Create(PdfToolId.PowerPointToPdf));
        Assert.IsType<EditPdfToolViewModel>(_factory.Create(PdfToolId.EditPdf));
    }

    [Fact]
    public void MergePdfToolViewModel_ReordersFilesCorrectly()
    {
        var vm = (MergePdfToolViewModel)_factory.Create(PdfToolId.MergePdf);
        vm.SelectedFiles.Add("file1.pdf");
        vm.SelectedFiles.Add("file2.pdf");
        vm.SelectedFiles.Add("file3.pdf");

        Assert.Equal(3, vm.SelectedFiles.Count);
        Assert.Equal("file1.pdf", vm.SelectedFiles[0]);

        // Move item down
        vm.MoveFileDownCommand.Execute("file1.pdf");
        Assert.Equal("file2.pdf", vm.SelectedFiles[0]);
        Assert.Equal("file1.pdf", vm.SelectedFiles[1]);

        // Move item up
        vm.MoveFileUpCommand.Execute("file3.pdf");
        Assert.Equal("file3.pdf", vm.SelectedFiles[1]);
        Assert.Equal("file1.pdf", vm.SelectedFiles[2]);

        // Remove item
        vm.RemoveFileCommand.Execute("file3.pdf");
        Assert.Equal(2, vm.SelectedFiles.Count);
        Assert.DoesNotContain("file3.pdf", vm.SelectedFiles);

        // Clear
        vm.ClearFilesCommand.Execute(null);
        Assert.Empty(vm.SelectedFiles);
    }

    [Fact]
    public void SplitPdfToolViewModel_ConfiguresSplitModes()
    {
        var vm = (SplitPdfToolViewModel)_factory.Create(PdfToolId.SplitPdf);
        Assert.Equal(SplitExtractMode.SplitEveryNPages, vm.SplitMode);

        vm.SplitPagesInterval = 3;
        Assert.Equal(3, vm.SplitPagesInterval);

        vm.SplitOddEven = true;
        vm.ExtractOddPages = true;
        Assert.True(vm.SplitOddEven);
        Assert.True(vm.ExtractOddPages);
    }

    [Fact]
    public void CompressPdfToolViewModel_UpdatesDpiBasedOnPreset()
    {
        var vm = (CompressPdfToolViewModel)_factory.Create(PdfToolId.CompressPdf);
        Assert.Equal(PdfCompressionLevel.Balanced, vm.CompressionLevel);
        Assert.Equal(150, vm.ImageQualityDpi);

        vm.CompressionLevel = PdfCompressionLevel.MaximumCompression;
        vm.ImageQualityDpi = 72;
        Assert.Equal(PdfCompressionLevel.MaximumCompression, vm.CompressionLevel);
        Assert.Equal(72, vm.ImageQualityDpi);
    }

    [Fact]
    public void WatermarkToolViewModel_MaintainsFormattingAndPosition()
    {
        var vm = (WatermarkToolViewModel)_factory.Create(PdfToolId.Watermark);
        vm.WatermarkText = "CONFIDENTIAL DRAFT";
        vm.Opacity = 0.45;
        vm.RotationAngle = 30;
        vm.ColorHex = "#DC2626";
        vm.Position = WatermarkPosition.TopLeft;

        Assert.Equal("CONFIDENTIAL DRAFT", vm.WatermarkText);
        Assert.Equal(0.45, vm.Opacity);
        Assert.Equal(30, vm.RotationAngle);
        Assert.Equal("#DC2626", vm.ColorHex);
        Assert.Equal(WatermarkPosition.TopLeft, vm.Position);
    }

    [Fact]
    public void RotatePdfToolViewModel_CyclesRotationDegrees()
    {
        var vm = (RotatePdfToolViewModel)_factory.Create(PdfToolId.RotatePdf);
        Assert.Equal(90, vm.RotationDegrees);

        vm.RotationDegrees = 180;
        Assert.Equal(180, vm.RotationDegrees);

        vm.RotationDegrees = 270;
        Assert.Equal(270, vm.RotationDegrees);
    }

    [Fact]
    public void ProtectPdfToolViewModel_ConfiguresPasswordsAndPermissions()
    {
        var vm = (ProtectPdfToolViewModel)_factory.Create(PdfToolId.ProtectPdf);
        vm.UserPassword = "UserPass123";
        vm.OwnerPassword = "OwnerPass456";
        vm.AllowPrinting = false;
        vm.AllowCopying = false;

        Assert.Equal("UserPass123", vm.UserPassword);
        Assert.Equal("OwnerPass456", vm.OwnerPassword);
        Assert.False(vm.AllowPrinting);
        Assert.False(vm.AllowCopying);
    }

    [Fact]
    public void SignPdfToolViewModel_ConfiguresSignerDetails()
    {
        var vm = (SignPdfToolViewModel)_factory.Create(PdfToolId.SignPdf);
        vm.SignerName = "John Doe";
        vm.Reason = "Approved Contract";
        vm.Location = "San Francisco, CA";
        vm.Style = SignatureStyle.CursiveElegance;

        Assert.Equal("John Doe", vm.SignerName);
        Assert.Equal("Approved Contract", vm.Reason);
        Assert.Equal("San Francisco, CA", vm.Location);
        Assert.Equal(SignatureStyle.CursiveElegance, vm.Style);
    }

    [Fact]
    public void HomeViewModel_OpensToolPageWithDedicatedViewModel()
    {
        var vm = new HomeViewModel(
            new MockRecentService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            _toolRegistry,
            null,
            _factory);

        Assert.Null(vm.ActiveToolViewModel);
        Assert.False(vm.IsToolPageActive);

        // Act: Open Watermark tool
        vm.OpenToolPageCommand.Execute(PdfToolId.Watermark);

        // Assert
        Assert.True(vm.IsToolPageActive);
        Assert.NotNull(vm.ActiveToolViewModel);
        Assert.IsType<WatermarkToolViewModel>(vm.ActiveToolViewModel);
        Assert.Equal(PdfToolId.Watermark, vm.ActiveToolViewModel.Tool.Id);

        // Act: Go back
        vm.BackToToolsCommand.Execute(null);

        // Assert
        Assert.False(vm.IsToolPageActive);
        Assert.Null(vm.ActiveToolViewModel);
    }

    [Fact]
    public void HomeViewModel_ToolStarringSynchronizesBidirectionally()
    {
        var vm = new HomeViewModel(
            new MockRecentService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            _toolRegistry,
            null,
            _factory);

        // Open Rotate tool
        vm.OpenToolPageCommand.Execute(PdfToolId.RotatePdf);
        Assert.NotNull(vm.ActiveToolViewModel);
        Assert.False(vm.ActiveToolViewModel.IsToolStarred);

        // Toggle star via ViewModel
        vm.ActiveToolViewModel.ToggleStarCommand.Execute(null);
        Assert.True(vm.ActiveToolViewModel.IsToolStarred);
        Assert.NotNull(vm.ActiveToolCard);
        Assert.True(vm.ActiveToolCard.IsStarred);
    }

    [Fact]
    public void PdfToolViewModelBase_SynchronizesPreviewItemsAndReordering()
    {
        var vm = (MergePdfToolViewModel)_factory.Create(PdfToolId.MergePdf);
        vm.SelectedFiles.Add("doc1.pdf");
        vm.SelectedFiles.Add("doc2.pdf");
        vm.SyncPreviewItems();

        Assert.Equal(2, vm.SelectedFilePreviewItems.Count);
        Assert.Equal("doc1.pdf", vm.SelectedFilePreviewItems[0].FileName);
        Assert.Equal("#1", vm.SelectedFilePreviewItems[0].OrderIndexText);
        Assert.Equal("doc2.pdf", vm.SelectedFilePreviewItems[1].FileName);
        Assert.Equal("#2", vm.SelectedFilePreviewItems[1].OrderIndexText);

        // Reorder
        vm.MoveFileDownCommand.Execute("doc1.pdf");
        Assert.Equal("doc2.pdf", vm.SelectedFilePreviewItems[0].FileName);
        Assert.Equal("#1", vm.SelectedFilePreviewItems[0].OrderIndexText);
        Assert.Equal("doc1.pdf", vm.SelectedFilePreviewItems[1].FileName);
        Assert.Equal("#2", vm.SelectedFilePreviewItems[1].OrderIndexText);
    }

    [Fact]
    public void PdfFileHelper_SanitizesTrailingJunkBytesAfterEof()
    {
        string rawPdf = "%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\ntrailer\n<< >>\nstartxref\n0\n%%EOF\n<!-- trailing web server junk HTML -->\n\0\0\0";
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(rawPdf);
        byte[] sanitized = PdfFileHelper.SanitizePdfBytes(bytes);

        string sanitizedText = System.Text.Encoding.ASCII.GetString(sanitized);
        Assert.EndsWith("%%EOF\r\n", sanitizedText);
        Assert.DoesNotContain("trailing web server junk", sanitizedText);
    }

    private class MockRecentService : IRecentDocumentsService
    {
        public List<RecentDocumentItem> Items { get; } = new();
        public List<RecentDocumentItem> Load() => new(Items);
        public void Add(RecentDocumentItem item) => Items.Insert(0, item);
        public void Remove(string filePath) => Items.RemoveAll(x => x.FilePath == filePath);
        public void Clear() => Items.Clear();
    }
}
