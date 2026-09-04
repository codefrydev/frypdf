using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using PdfEditorApp.Services;
using PdfEditorApp.Templates;
using PdfEditorApp.ViewModels;
using PdfEditorApp.Views.Controls;
using Xunit;

namespace PdfEditorApp.Tests;

public class TextSelectionAndOcrTests
{
    private readonly IPdfExportService _exportService = new PdfExportService();
    private readonly ITemplateService _templateService = new TemplateService();

    private byte[] CreateSamplePdfBytes()
    {
        var model = _templateService.CreateAnnualReportTemplate();
        return _exportService.GeneratePdfBytes(model);
    }

    [Fact]
    public void SelectionMode_DefaultIsText_CanToggleToAreaMode()
    {
        var vm = new PdfViewerViewModel();

        Assert.Equal(PdfViewerSelectionMode.Text, vm.SelectionMode);
        Assert.True(vm.IsTextSelectionMode);
        Assert.False(vm.IsAreaSelectionMode);

        vm.SetAreaSelectionModeCommand.Execute(null);
        Assert.Equal(PdfViewerSelectionMode.Area, vm.SelectionMode);
        Assert.False(vm.IsTextSelectionMode);
        Assert.True(vm.IsAreaSelectionMode);

        vm.SetTextSelectionModeCommand.Execute(null);
        Assert.Equal(PdfViewerSelectionMode.Text, vm.SelectionMode);
        Assert.True(vm.IsTextSelectionMode);
        Assert.False(vm.IsAreaSelectionMode);

        vm.ToggleSelectionModeCommand.Execute(null);
        Assert.Equal(PdfViewerSelectionMode.Area, vm.SelectionMode);
        Assert.True(vm.IsAreaSelectionMode);
    }

    [Fact]
    public void ScannedDocumentDetection_BannerState_TransitionsProperly()
    {
        var vm = new PdfViewerViewModel();

        // Initially no document
        Assert.False(vm.ShowScannedDocumentBanner);

        // When document is loaded and page has 0 words
        vm.IsScannedDocument = true;
        vm.IsCurrentPageScanned = true;
        vm.HasDocument = true;

        Assert.True(vm.ShowScannedDocumentBanner);

        // User dismisses banner
        vm.DismissScannedBannerCommand.Execute(null);
        Assert.False(vm.ShowScannedDocumentBanner);
        Assert.True(vm.IsScannedBannerDismissed);

        // Re-enabling or OCR running suppresses banner
        vm.IsScannedBannerDismissed = false;
        vm.IsOcrRunning = true;
        Assert.False(vm.ShowScannedDocumentBanner);
    }

    [Fact]
    public async Task AreaSelection_ExtractsWordsWithinRegion()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes();
        await vm.LoadDocumentBytesAsync(pdfBytes, "Test.pdf");

        Assert.NotNull(vm.SelectedPage);
        var page = vm.SelectedPage!;

        // Clear any auto-extracted template words and set specific test words
        page.Words.Clear();
        page.Words.Add(new PdfViewerWordItem
        {
            Text = "Hello",
            Bounds = new Rect(10, 10, 40, 15),
            WordIndex = 0
        });
        page.Words.Add(new PdfViewerWordItem
        {
            Text = "World",
            Bounds = new Rect(60, 10, 45, 15),
            WordIndex = 1
        });
        page.Words.Add(new PdfViewerWordItem
        {
            Text = "FarAway",
            Bounds = new Rect(300, 300, 60, 15),
            WordIndex = 2
        });

        // Region enclosing only "Hello World"
        var region = new Rect(5, 5, 120, 30);
        string extracted = await vm.ExtractOcrTextFromRegionAsync(page, region);

        Assert.Contains("Hello", extracted);
        Assert.Contains("World", extracted);
        Assert.DoesNotContain("FarAway", extracted);
    }

    [Fact]
    public void MarqueeSelection_IntersectsWords_CalculatesBoundingBoxCorrectly()
    {
        var word1 = new PdfViewerWordItem { Text = "Revenue", Bounds = new Rect(50, 100, 60, 16) };
        var word2 = new PdfViewerWordItem { Text = "Growth", Bounds = new Rect(115, 100, 50, 16) };
        var word3 = new PdfViewerWordItem { Text = "Footer", Bounds = new Rect(50, 700, 40, 12) };

        var words = new List<PdfViewerWordItem> { word1, word2, word3 };
        var marqueeBox = new Rect(40, 90, 150, 30);

        var selected = words.Where(w => w.Bounds.Intersects(marqueeBox)).ToList();

        Assert.Equal(2, selected.Count);
        Assert.Equal("Revenue", selected[0].Text);
        Assert.Equal("Growth", selected[1].Text);
    }

    [Fact]
    public async Task ScannedPdf_LoadsAndInitializesOcrCapabilities()
    {
        string? samplePdf = FindTestPdf();
        if (samplePdf == null || !File.Exists(samplePdf)) return;

        var vm = new PdfViewerViewModel();
        await vm.LoadDocumentAsync(samplePdf);

        Assert.True(vm.HasDocument);
        Assert.NotEmpty(vm.Pages);

        var firstPage = vm.Pages[0];
        Assert.True(firstPage.WidthPoints > 0);
        Assert.True(firstPage.HeightPoints > 0);

        // Ensure page geometry can be populated via OCR if needed
        if (OperatingSystem.IsMacOS() && firstPage.Words.Count == 0)
        {
            await vm.RecognizeActivePageOcrAsync();
            Assert.NotEmpty(firstPage.Words);
            Assert.False(string.IsNullOrWhiteSpace(firstPage.ExtractedText));
            Assert.False(vm.IsCurrentPageScanned);
        }
    }

    [Fact]
    public void ShiftClick_ExtendsSelectionRange_OnPageItem()
    {
        var page = new PdfViewerPageItem
        {
            PageNumber = 1,
            WidthPoints = 612,
            HeightPoints = 792
        };

        var w1 = new PdfViewerWordItem { Text = "First", Bounds = new Rect(50, 50, 30, 12), LineIndex = 0, WordIndex = 0 };
        var w2 = new PdfViewerWordItem { Text = "Second", Bounds = new Rect(85, 50, 40, 12), LineIndex = 0, WordIndex = 1 };
        var w3 = new PdfViewerWordItem { Text = "Third", Bounds = new Rect(130, 50, 30, 12), LineIndex = 0, WordIndex = 2 };

        var line = new PdfViewerTextLineItem { LineIndex = 0, Bounds = new Rect(50, 50, 110, 12) };
        line.Words.AddRange(new[] { w1, w2, w3 });

        page.Words.AddRange(new[] { w1, w2, w3 });
        page.TextLines.Add(line);

        // Select from start of First to end of Third
        page.SetSelectionRange(new Point(50, 55), new Point(160, 55));

        Assert.True(page.HasSelection);
        Assert.Contains("First", page.SelectedText);
        Assert.Contains("Third", page.SelectedText);
        Assert.NotEmpty(page.SelectionRects);
    }

    [Fact]
    public void GarbledText_Detection_IdentifiesCorruptedFontsCorrectly()
    {
        // Unmapped / corrupt font encodings from custom PDF fonts
        Assert.True(PdfViewerViewModel.IsGarbledText("? FT * FtJlTiur"));
        Assert.True(PdfViewerViewModel.IsGarbledText("s | l | r | i | a | a | t | a | e | h | d | D | a | A"));
        Assert.True(PdfViewerViewModel.IsGarbledText("??? ?? * ??"));

        // Clean normal texts (Latin, Indic Unicode, CJK)
        Assert.False(PdfViewerViewModel.IsGarbledText("Hello World, this is a clean financial report."));
        Assert.False(PdfViewerViewModel.IsGarbledText("इस आधार पत्र को यूआईडीएआई द्वारा नियुक्त प्रमाणीकरण"));
        Assert.False(PdfViewerViewModel.IsGarbledText("Annual Revenue Growth 2026"));
        Assert.False(PdfViewerViewModel.IsGarbledText(""));
        Assert.False(PdfViewerViewModel.IsGarbledText(null));
    }

    [Fact]
    public void IsSelectionTextGarbled_UpdatesAutomatically_WhenSelectionChanges()
    {
        var vm = new PdfViewerViewModel();

        Assert.False(vm.IsSelectionTextGarbled);

        vm.ActiveSelectedText = "? FT * FtJlTiur";
        Assert.True(vm.IsSelectionTextGarbled);

        vm.ActiveSelectedText = "Standard Heading Text";
        Assert.False(vm.IsSelectionTextGarbled);
    }

    private static string? FindTestPdf()
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
