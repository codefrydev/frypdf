using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Intelligence;
using PdfEditorApp.ViewModels.Tools.Core;
using PdfEditorApp.ViewModels.Tools.Organize;
using PdfEditorApp.ViewModels.Tools.Security;
using PdfEditorApp.ViewModels.Tools.Conversion;
using PdfEditorApp.ViewModels.Tools.Intelligence;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Services.Ocr;
using PdfEditorApp.ViewModels;
using UglyToad.PdfPig;
using Xunit;
using Xunit.Abstractions;

namespace PdfEditorApp.Tests;

[Collection("OcrTests")]
public class OcrEngineTests
{
    private readonly ITestOutputHelper _output;

    public OcrEngineTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TesseractModelService_ListsCatalog_AndDetectsLocalFiles()
    {
        var service = new TesseractModelService();
        Assert.NotEmpty(service.AvailableLanguages);

        var eng = service.AvailableLanguages.FirstOrDefault(l => l.Code == "eng");
        Assert.NotNull(eng);
        Assert.Equal("English", eng.DisplayName);
        Assert.Contains("eng.traineddata", eng.DownloadUrl);
    }

    [Fact]
    public void CompositeOcrProvider_InitializesAvailableEngines()
    {
        var provider = new CompositeOcrProvider();
        Assert.NotNull(provider.ActiveEngine);
        Assert.NotEmpty(provider.AvailableEngines);

        if (OperatingSystem.IsMacOS())
        {
            Assert.Contains(provider.AvailableEngines, e => e.EngineType == OcrEngineType.OsNative);
            Assert.Equal("Apple Vision OCR (macOS)", provider.ActiveEngine.EngineName);
        }
    }

    [Fact]
    public async Task AppleVisionOcr_RecognizesText_WhenOnMacOS()
    {
        if (!OperatingSystem.IsMacOS()) return;

        string? pdfPath = FindTestPdfPath();
        if (pdfPath == null || !File.Exists(pdfPath))
        {
            _output.WriteLine("PDF not found, skipping.");
            return;
        }

        // Render page 1 to PNG bytes using PdfViewerViewModel
        var vm = new PdfViewerViewModel();
        await vm.LoadDocumentAsync(pdfPath);

        byte[]? pngBytes = vm.RenderPageBytesAtScale(1, 1.0f);
        Assert.NotNull(pngBytes);
        Assert.NotEmpty(pngBytes);

        var engine = new AppleVisionOcrEngine();
        Assert.True(engine.IsAvailable);

        var result = await engine.RecognizeTextAsync(pngBytes, "eng");
        Assert.True(result.Success, $"OCR error: {result.ErrorMessage}");
        Assert.NotEmpty(result.Words);
        Assert.NotEmpty(result.Lines);
        Assert.False(string.IsNullOrWhiteSpace(result.FullText));

        _output.WriteLine($"Recognized {result.Words.Count} words and {result.Lines.Count} lines in {result.DurationMs} ms.");
        _output.WriteLine($"Sample line: {result.Lines[0].Text}");
        _output.WriteLine($"Sample word bounds: {result.Words[0].Text} -> [{result.Words[0].NormalizedBounds.X:F3}, {result.Words[0].NormalizedBounds.Y:F3}]");
    }

    [Fact]
    public async Task PdfViewer_ScannedDocument_AutomaticallyPopulatesWordsViaOcr()
    {
        if (!OperatingSystem.IsMacOS()) return;

        string? pdfPath = FindTestPdfPath();
        if (pdfPath == null || !File.Exists(pdfPath)) return;

        var vm = new PdfViewerViewModel();
        await vm.LoadDocumentAsync(pdfPath);

        Assert.Single(vm.Pages);
        var page = vm.Pages[0];

        _output.WriteLine($"Page dimensions: {page.WidthPoints}x{page.HeightPoints}, CurrentPdfBytes length: {vm.CurrentPdfBytes?.Length}");

        var (ocrTxt, ocrW, ocrL) = vm.ExtractOcrPageTextGeometry(1, page.WidthPoints, page.HeightPoints);
        _output.WriteLine($"Direct ExtractOcrPageTextGeometry: words={ocrW.Count}, textLength={ocrTxt.Length}");

        // Ensure geometry runs
        vm.EnsurePageGeometry(page);

        // Wait up to 3 seconds for async OCR extraction
        for (int i = 0; i < 30 && page.Words.Count == 0; i++)
        {
            await Task.Delay(100);
        }

        _output.WriteLine($"After EnsurePageGeometry: page.Words.Count={page.Words.Count}, IsGeometryLoading={page.IsGeometryLoading}");
        Assert.True(page.Words.Count > 100, $"Expected > 100 words extracted via OCR, but got {page.Words.Count}");
        Assert.NotEmpty(page.TextLines);
        Assert.False(string.IsNullOrWhiteSpace(page.ExtractedText));

        // Verify word bounds are valid in page coordinate space
        var firstWord = page.Words[0];
        Assert.True(firstWord.Bounds.Width > 0);
        Assert.True(firstWord.Bounds.Height > 0);
        Assert.True(firstWord.Bounds.Left >= 0 && firstWord.Bounds.Left <= page.WidthPoints);
        Assert.True(firstWord.Bounds.Top >= 0 && firstWord.Bounds.Top <= page.HeightPoints);

        // Verify word selection works (just as PdfTextOverlayControl would do)
        page.SelectWord(firstWord);
        Assert.True(page.HasSelection);
        Assert.Equal(firstWord.Text, page.SelectedText);
        Assert.Single(page.SelectionRects);
    }

    [Fact]
    public async Task PdfOcrService_CreatesSearchablePdf_FromScannedDocument()
    {
        if (!OperatingSystem.IsMacOS()) return;

        string? pdfPath = FindTestPdfPath();
        if (pdfPath == null || !File.Exists(pdfPath)) return;

        string tempOutPdf = Path.Combine(Path.GetTempPath(), $"Searchable_Test_{Guid.NewGuid():N}.pdf");

        try
        {
            var ocrService = new PdfOcrService();
            var options = new Models.OcrToolOptions
            {
                InputFilePath = pdfPath,
                OutputFilePath = tempOutPdf,
                GenerateSearchablePdf = true
            };

            var result = await ocrService.OcrPdfAsync(options);
            Assert.True(result.Success, $"OCR failed: {result.ErrorMessage}");
            Assert.True(File.Exists(tempOutPdf));
            Assert.True(new FileInfo(tempOutPdf).Length > 1000);

            // Open the resulting PDF with PdfPig and verify it now contains actual vector words!
            using var pigDoc = UglyToad.PdfPig.PdfDocument.Open(tempOutPdf);
            Assert.Single(pigDoc.GetPage(1).Letters.Take(1)); // Has letters
            var words = pigDoc.GetPage(1).GetWords().ToList();
            Assert.True(words.Count > 100, $"Expected searchable PDF to have > 100 words, but got {words.Count}");
        }
        finally
        {
            try { if (File.Exists(tempOutPdf)) File.Delete(tempOutPdf); } catch { }
        }
    }

    [Fact]
    public async Task PdfOcrService_CreatesTextFileAndSearchablePdf_WhenBothRequested()
    {
        if (!OperatingSystem.IsMacOS()) return;

        string? pdfPath = FindTestPdfPath();
        if (pdfPath == null || !File.Exists(pdfPath)) return;

        string tempOutPdf = Path.Combine(Path.GetTempPath(), $"Searchable_Test_{Guid.NewGuid():N}.pdf");
        string expectedTxt = Path.ChangeExtension(tempOutPdf, ".txt");

        try
        {
            var ocrService = new PdfOcrService();
            var options = new Models.OcrToolOptions
            {
                InputFilePath = pdfPath,
                OutputFilePath = tempOutPdf,
                GenerateSearchablePdf = true,
                GenerateTextFile = true
            };

            var result = await ocrService.OcrPdfAsync(options);
            Assert.True(result.Success, $"OCR failed: {result.ErrorMessage}");
            Assert.True(File.Exists(tempOutPdf), "Searchable PDF should exist");
            Assert.True(File.Exists(expectedTxt), "Extracted text file (.txt) should exist");
            Assert.Equal(2, result.OutputFiles.Count);

            string textContent = await File.ReadAllTextAsync(expectedTxt);
            Assert.True(textContent.Length > 500, "Text content should contain recognized OCR text");
            Assert.Contains("Arduino", textContent, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { if (File.Exists(tempOutPdf)) File.Delete(tempOutPdf); } catch { }
            try { if (File.Exists(expectedTxt)) File.Delete(expectedTxt); } catch { }
        }
    }

    [Fact]
    public async Task OcrPdfToolViewModel_SideBySide_ExtractsPageTextAndUpdatesStats()
    {
        if (!OperatingSystem.IsMacOS()) return;

        string? pdfPath = FindTestPdfPath();
        if (pdfPath == null || !File.Exists(pdfPath)) return;

        var ops = new PdfEditorApp.Services.PdfDocumentOperationsService();
        var toolDef = new PdfToolRegistry().GetTool(Models.PdfToolId.OcrPdf)!;

        var vm = new OcrPdfToolViewModel(ops, toolDef);
        Assert.True(vm.IsSideBySideVisible, "Side-by-side panel should be visible by default");
        Assert.True(vm.GenerateTextFile, "GenerateTextFile should be enabled by default");

        vm.SelectedFiles.Add(pdfPath);

        // Extract Page 1 text
        await vm.ExtractTextForPageAsync(1);
        for (int i = 0; i < 30 && (string.IsNullOrWhiteSpace(vm.CurrentPageExtractedText) || vm.IsExtractingPageText); i++)
        {
            await Task.Delay(100);
        }

        _output.WriteLine($"CurrentPageExtractedText length={vm.CurrentPageExtractedText?.Length}: '{vm.CurrentPageExtractedText?.Substring(0, Math.Min(60, vm.CurrentPageExtractedText.Length))}'");

        Assert.False(string.IsNullOrWhiteSpace(vm.CurrentPageExtractedText), $"CurrentPageExtractedText should be populated, got: '{vm.CurrentPageExtractedText}'");
        Assert.Contains("Arduino", vm.CurrentPageExtractedText, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual("0 words", vm.WordCountText);
        Assert.NotEqual("0 chars", vm.CharCountText);

        // Test active text update
        vm.ActiveDisplayedText = "Test modified text for side-by-side";
        Assert.Equal("Test modified text for side-by-side", vm.CurrentPageExtractedText);
    }

    [Fact]
    public async Task PdfLivePreviewViewModel_LoadsTextDocument_SetsTextPropertiesAndHasDocument()
    {
        string tempTxt = Path.Combine(Path.GetTempPath(), $"Preview_Test_{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(tempTxt, "Line 1: Arduino Programming\nLine 2: Digital I/O Pins\nLine 3: Serial Communication");

            var preview = new PdfEditorApp.ViewModels.Shell.PdfLivePreviewViewModel();
            Assert.False(preview.HasDocument);

            await preview.LoadDocumentAsync(tempTxt);

            Assert.True(preview.HasDocument, "HasDocument should be true for text files");
            Assert.True(preview.IsTextDocument, "IsTextDocument should be true for text files");
            Assert.Equal(3, preview.TextDocumentLinesCount);
            Assert.Equal(13, preview.TextDocumentWordsCount);
            Assert.Contains("Digital I/O Pins", preview.TextDocumentContent);
            Assert.Equal("3 lines", preview.PageIndicatorText);
        }
        finally
        {
            try { if (File.Exists(tempTxt)) File.Delete(tempTxt); } catch { }
        }
    }

    [Fact]
    public async Task OcrPdfToolViewModel_PreviewTextFile_SwitchesPreviewToTextDocument()
    {
        var ops = new PdfEditorApp.Services.PdfDocumentOperationsService();
        var toolDef = new PdfToolRegistry().GetTool(Models.PdfToolId.OcrPdf)!;
        var vm = new OcrPdfToolViewModel(ops, toolDef);

        string tempTxt = Path.Combine(Path.GetTempPath(), $"Ocr_Preview_Test_{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(tempTxt, "Sample extracted OCR text from scanned card.");
            vm.LastGeneratedTextFilePath = tempTxt;

            await vm.PreviewTextFileAsync();

            Assert.True(vm.Preview.HasDocument);
            Assert.True(vm.Preview.IsTextDocument);
            Assert.Contains("Sample extracted OCR text", vm.Preview.TextDocumentContent);
        }
        finally
        {
            try { if (File.Exists(tempTxt)) File.Delete(tempTxt); } catch { }
        }
    }

    private static string? FindTestPdfPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Arduino Cheat Sheet_Redacted_Redacted.pdf");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
