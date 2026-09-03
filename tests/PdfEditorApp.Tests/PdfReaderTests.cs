using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Templates;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PdfReaderTests
{
    private readonly IPdfExportService _exportService = new PdfExportService();
    private readonly ITemplateService _templateService = new TemplateService();

    private byte[] CreateSamplePdfBytes(int pageCount = 3)
    {
        var model = _templateService.CreateAnnualReportTemplate();
        return _exportService.GeneratePdfBytes(model);
    }

    [Fact]
    public async Task PdfViewer_LoadsDocument_PopulatesMetadataAndSpreads()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes();

        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        Assert.True(vm.HasDocument);
        Assert.False(vm.IsLoading);
        Assert.Equal("SampleReport.pdf", vm.DocumentTitle);
        Assert.Equal(3, vm.TotalPagesCount);
        Assert.Equal(1, vm.CurrentPageNumber);
        Assert.NotNull(vm.SelectedPage);
        Assert.Equal(3, vm.Pages.Count);
        Assert.NotEmpty(vm.MetadataItems);
        Assert.NotEmpty(vm.PageSpreads);
        Assert.Equal("Page 1 (Cover)", vm.PageSpreads[0].SpreadLabel);
    }

    [Fact]
    public async Task PdfViewer_PageNavigationAndDirectJump_UpdatesStateCorrectly()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes();
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        // Next page
        vm.NextPageCommand.Execute(null);
        Assert.Equal(2, vm.CurrentPageNumber);
        Assert.Equal("2", vm.JumpPageText);
        Assert.Equal(2, vm.SelectedPage?.PageNumber);

        // Previous page
        vm.PreviousPageCommand.Execute(null);
        Assert.Equal(1, vm.CurrentPageNumber);

        // Last page
        vm.LastPageCommand.Execute(null);
        Assert.Equal(3, vm.CurrentPageNumber);

        // First page
        vm.FirstPageCommand.Execute(null);
        Assert.Equal(1, vm.CurrentPageNumber);

        // Direct Page Jump
        vm.JumpPageText = "3";
        vm.CommitJumpPageCommand.Execute(null);
        Assert.Equal(3, vm.CurrentPageNumber);

        // Invalid Page Jump resets to current
        vm.JumpPageText = "99";
        vm.CommitJumpPageCommand.Execute(null);
        Assert.Equal(3, vm.CurrentPageNumber);
        Assert.Equal("3", vm.JumpPageText);
    }

    [Fact]
    public async Task PdfViewer_LayoutModes_TogglesContinuousSingleAndSpread()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes();
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        Assert.Equal(PdfViewLayoutMode.ContinuousScroll, vm.SelectedLayoutMode);
        Assert.True(vm.IsContinuousScroll);
        Assert.False(vm.IsSinglePageMode);
        Assert.False(vm.IsTwoPageSpreadMode);

        // Switch to Single Page
        vm.SetLayoutModeCommand.Execute("SinglePage");
        Assert.Equal(PdfViewLayoutMode.SinglePage, vm.SelectedLayoutMode);
        Assert.False(vm.IsContinuousScroll);
        Assert.True(vm.IsSinglePageMode);
        Assert.False(vm.IsTwoPageSpreadMode);

        // Switch to Two-Page Spread
        vm.SetLayoutModeCommand.Execute("TwoPageSpread");
        Assert.Equal(PdfViewLayoutMode.TwoPageSpread, vm.SelectedLayoutMode);
        Assert.False(vm.IsContinuousScroll);
        Assert.False(vm.IsSinglePageMode);
        Assert.True(vm.IsTwoPageSpreadMode);
        Assert.Equal(2, vm.PageSpreads.Count); // Cover (p1) + Spread (p2-p3)
    }

    [Fact]
    public async Task PdfViewer_ReadingThemes_SwitchesThemesAndUpdatesColors()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes();
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        // Default theme
        Assert.Equal(PdfReaderTheme.Default, vm.ReadingTheme);
        Assert.True(vm.IsThemeDefault);
        Assert.Equal("#FFFFFF", vm.ThemePaperBackgroundHex);

        // Sepia
        vm.SetReadingThemeCommand.Execute("Sepia");
        Assert.Equal(PdfReaderTheme.Sepia, vm.ReadingTheme);
        Assert.True(vm.IsThemeSepia);
        Assert.Equal("#FBF0D9", vm.ThemePaperBackgroundHex);
        Assert.Equal("#433422", vm.ThemeTextColorHex);

        // Dark Night Mode
        vm.SetReadingThemeCommand.Execute("Dark");
        Assert.Equal(PdfReaderTheme.Dark, vm.ReadingTheme);
        Assert.True(vm.IsThemeDark);
        Assert.Equal("#1E293B", vm.ThemePaperBackgroundHex);
        Assert.Equal("#F1F5F9", vm.ThemeTextColorHex);

        // High Contrast
        vm.SetReadingThemeCommand.Execute("HighContrast");
        Assert.Equal(PdfReaderTheme.HighContrast, vm.ReadingTheme);
        Assert.True(vm.IsThemeHighContrast);
        Assert.Equal("#000000", vm.ThemePaperBackgroundHex);
        Assert.Equal("#FFFF00", vm.ThemeTextColorHex);
    }

    [Fact]
    public async Task PdfViewer_ZoomOperations_ClampsWithinSafeBounds()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes();
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        Assert.Equal(1.0, vm.ZoomLevel);
        Assert.Equal("100%", vm.ZoomPercentageText);

        vm.ZoomInCommand.Execute(null);
        Assert.Equal(1.25, vm.ZoomLevel);
        Assert.Equal("125%", vm.ZoomPercentageText);

        vm.FitToWidthCommand.Execute(null);
        Assert.Equal(1.35, vm.ZoomLevel);

        vm.FitToPageCommand.Execute(null);
        Assert.Equal(0.95, vm.ZoomLevel);

        vm.SetZoomPresetCommand.Execute("200%");
        Assert.Equal(2.0, vm.ZoomLevel);

        vm.ResetZoomCommand.Execute(null);
        Assert.Equal(1.0, vm.ZoomLevel);
    }

    [Fact]
    public async Task PdfViewer_ReviewAnnotations_AddsHighlightsStickyNotesAndStamps()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes();
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        // 1. Add Highlight
        vm.AddHighlightAnnotationCommand.Execute("#A7F3D0");
        Assert.Single(vm.Annotations);
        Assert.Equal("Highlight", vm.Annotations[0].Type);
        Assert.Equal("#A7F3D0", vm.Annotations[0].ColorHex);
        Assert.Single(vm.SelectedPage!.PageAnnotations);

        // 2. Add Sticky Note
        vm.NewNoteText = "Check Q3 audit numbers.";
        vm.ConfirmAddNoteCommand.Execute(null);
        Assert.Equal(2, vm.Annotations.Count);
        Assert.Equal("StickyNote", vm.Annotations[1].Type);
        Assert.Equal("Check Q3 audit numbers.", vm.Annotations[1].Content);

        // 3. Add Stamp
        vm.AddStampCommand.Execute("APPROVED");
        Assert.Equal(3, vm.Annotations.Count);
        Assert.Equal("Stamp", vm.Annotations[2].Type);
        Assert.Contains("APPROVED", vm.Annotations[2].Content);

        // 4. Delete Annotation
        var toDelete = vm.Annotations[0];
        vm.DeleteAnnotationCommand.Execute(toDelete);
        Assert.Equal(2, vm.Annotations.Count);
        Assert.DoesNotContain(toDelete, vm.Annotations);
    }

    [Fact]
    public async Task PdfViewer_VisualRotation_RotatesPageAndAllPages()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes();
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        Assert.Equal(0, vm.SelectedPage?.RotationAngle);

        vm.RotateClockwiseCommand.Execute(null);
        Assert.Equal(90, vm.SelectedPage?.RotationAngle);

        vm.RotateCounterClockwiseCommand.Execute(null);
        Assert.Equal(0, vm.SelectedPage?.RotationAngle);

        vm.RotateAllPagesClockwiseCommand.Execute(null);
        Assert.All(vm.Pages, p => Assert.Equal(90, p.RotationAngle));
    }

    [Fact]
    public void HomeViewModel_NavigatesToPdfReaderSection()
    {
        var home = new HomeViewModel();
        Assert.True(home.IsHomeSection);
        Assert.False(home.IsPdfReaderSection);

        home.SelectNavSectionCommand.Execute("PdfReader");
        Assert.Equal(HomeNavSection.PdfReader, home.SelectedNavSection);
        Assert.True(home.IsPdfReaderSection);
        Assert.False(home.IsHomeSection);
    }

    [Fact]
    public async Task PdfViewer_RenderPageAtScale_RendersCustomResolutionBitmap()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes();
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        // Render Page 1 bytes at standard scale 2.75
        var bytes1 = vm.RenderPageBytesAtScale(1, 2.75f);
        Assert.NotNull(bytes1);
        Assert.True(bytes1.Length > 0);

        // Render Page 1 bytes at high-DPI zoom scale 4.0
        var bytesHigh = vm.RenderPageBytesAtScale(1, 4.0f);
        Assert.NotNull(bytesHigh);
        Assert.True(bytesHigh.Length > bytes1.Length);
    }

    [Fact]
    public async Task PdfViewer_PageSelectionAndThumbnailClick_TriggersScrollToPageAndUpdatesIsSelected()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(3);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        int requestedPage = 0;
        vm.ScrollToPageRequested += (pageNum) =>
        {
            requestedPage = pageNum;
        };

        // Click Page 2 Thumbnail
        vm.SelectPage(vm.Pages[1]);

        Assert.Equal(2, vm.CurrentPageNumber);
        Assert.Equal(2, vm.SelectedPage?.PageNumber);
        Assert.Equal(2, requestedPage);
        Assert.False(vm.Pages[0].IsSelected);
        Assert.True(vm.Pages[1].IsSelected);
        Assert.False(vm.Pages[2].IsSelected);

        // Click Page 3 Thumbnail
        vm.SelectPage(vm.Pages[2]);

        Assert.Equal(3, vm.CurrentPageNumber);
        Assert.Equal(3, vm.SelectedPage?.PageNumber);
        Assert.Equal(3, requestedPage);
        Assert.False(vm.Pages[0].IsSelected);
        Assert.False(vm.Pages[1].IsSelected);
        Assert.True(vm.Pages[2].IsSelected);

        // Click Page 1 Thumbnail
        vm.SelectPage(vm.Pages[0]);

        Assert.Equal(1, vm.CurrentPageNumber);
        Assert.Equal(1, vm.SelectedPage?.PageNumber);
        Assert.Equal(1, requestedPage);
        Assert.True(vm.Pages[0].IsSelected);
        Assert.False(vm.Pages[1].IsSelected);
        Assert.False(vm.Pages[2].IsSelected);
    }

    [Fact]
    public async Task PdfViewer_JumpToBookmark_ScrollsToTargetPage()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(3);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        int requestedPage = 0;
        vm.ScrollToPageRequested += (pageNum) =>
        {
            requestedPage = pageNum;
        };

        var bookmark = new PdfViewerBookmarkItem { Title = "Chapter 2", PageNumber = 2 };
        vm.JumpToBookmarkCommand.Execute(bookmark);

        Assert.Equal(2, vm.CurrentPageNumber);
        Assert.Equal(2, vm.SelectedPage?.PageNumber);
        Assert.Equal(2, requestedPage);
        Assert.True(vm.Pages[1].IsSelected);
    }

    [Fact]
    public async Task PdfViewer_JumpToAnnotation_ScrollsToTargetPage()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(3);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        int requestedPage = 0;
        vm.ScrollToPageRequested += (pageNum) =>
        {
            requestedPage = pageNum;
        };

        var annotation = new PdfViewerAnnotationItem { Type = "StickyNote", PageNumber = 3, Content = "Important remark", ColorHex = "#0284C7" };
        vm.JumpToAnnotationCommand.Execute(annotation);

        Assert.Equal(3, vm.CurrentPageNumber);
        Assert.Equal(3, vm.SelectedPage?.PageNumber);
        Assert.Equal(3, requestedPage);
        Assert.True(vm.Pages[2].IsSelected);
    }

    [Fact]
    public async Task PdfViewer_JumpToSearchMatch_ScrollsToTargetPage()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(3);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        int requestedPage = 0;
        vm.ScrollToPageRequested += (pageNum) =>
        {
            requestedPage = pageNum;
        };

        var match = new PdfViewerSearchMatch { PageNumber = 2, Snippet = "Search match snippet", MatchIndex = 0 };
        vm.JumpToMatchCommand.Execute(match);

        Assert.Equal(2, vm.CurrentPageNumber);
        Assert.Equal(2, vm.SelectedPage?.PageNumber);
        Assert.Equal(2, requestedPage);
        Assert.True(vm.Pages[1].IsSelected);
    }

    [Fact]
    public async Task PdfViewer_TwoPageSpreadNavigation_NavigatesInTwoPageSteps()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(3);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        vm.SetLayoutModeCommand.Execute("TwoPageSpread");
        Assert.True(vm.IsTwoPageSpreadMode);
        Assert.Equal(1, vm.CurrentPageNumber);

        int requestedPage = 0;
        vm.ScrollToPageRequested += (pageNum) =>
        {
            requestedPage = pageNum;
        };

        // Next page in 2-page spread
        vm.NextPageCommand.Execute(null);
        Assert.Equal(3, vm.CurrentPageNumber);
        Assert.Equal(3, requestedPage);

        // Previous page in 2-page spread
        vm.PreviousPageCommand.Execute(null);
        Assert.Equal(1, vm.CurrentPageNumber);
        Assert.Equal(1, requestedPage);
    }

    [Fact]
    public async Task PdfViewer_FitToWidth_WithViewportProvider_DynamicallyScalesToAvailableWidth()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(1);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        Assert.True(vm.HasDocument);
        var page = vm.Pages[0];
        Assert.True(page.WidthPoints > 0);

        // Simulate viewport of 1200 width with 64px padding (32 on each side)
        vm.ViewportSizeProvider = () => (1200.0, 800.0, 64.0, 64.0);

        vm.FitToWidthCommand.Execute(null);

        // Expected available width = 1200 - 64 - 8 = 1128
        // Expected zoom = 1128 / page.WidthPoints clamped and rounded to 2 decimals
        double expectedZoom = Math.Clamp(Math.Round(1128.0 / page.WidthPoints, 2), 0.25, 5.0);
        if (expectedZoom * page.WidthPoints > 1128.0)
        {
            expectedZoom = Math.Max(0.25, Math.Round(expectedZoom - 0.01, 2));
        }

        Assert.Equal(expectedZoom, vm.ZoomLevel);
        Assert.True(vm.IsFitToWidthActive);
        Assert.False(vm.IsFitToPageActive);
        Assert.Equal(PdfViewerZoomMode.FitWidth, vm.ZoomMode);

        // Verify page width at zoom fits comfortably within available width without horizontal overflow
        double renderedWidth = page.WidthPoints * vm.ZoomLevel;
        Assert.True(renderedWidth <= (1200.0 - 64.0));
    }

    [Fact]
    public async Task PdfViewer_FitToPage_WithViewportProvider_DynamicallyScalesToFitBothDimensions()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(1);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        var page = vm.Pages[0];

        // Simulate viewport of 800x600 with 64px horizontal and vertical padding
        vm.ViewportSizeProvider = () => (800.0, 600.0, 64.0, 64.0);

        vm.FitToPageCommand.Execute(null);

        Assert.True(vm.IsFitToPageActive);
        Assert.False(vm.IsFitToWidthActive);
        Assert.Equal(PdfViewerZoomMode.FitPage, vm.ZoomMode);

        // Verify both dimensions fit within viewport minus padding
        double renderedWidth = page.WidthPoints * vm.ZoomLevel;
        double renderedHeight = page.HeightPoints * vm.ZoomLevel;
        Assert.True(renderedWidth <= (800.0 - 64.0));
        Assert.True(renderedHeight <= (600.0 - 64.0));
    }

    [Fact]
    public async Task PdfViewer_TwoPageSpread_FitToWidth_AccountsForSpreadWidth()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(3);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        vm.SetLayoutModeCommand.Execute("TwoPageSpread");
        Assert.True(vm.IsTwoPageSpreadMode);

        // Navigate to spread with two pages (page 2 & 3)
        vm.NextPageCommand.Execute(null);
        Assert.NotNull(vm.SelectedSpread);

        vm.ViewportSizeProvider = () => (1600.0, 900.0, 64.0, 64.0);
        vm.FitToWidthCommand.Execute(null);

        Assert.True(vm.IsFitToWidthActive);
        Assert.True(vm.ZoomLevel > 0);

        if (vm.SelectedSpread.LeftPage != null && vm.SelectedSpread.RightPage != null)
        {
            double combinedPageWidth = (vm.SelectedSpread.LeftPage.WidthPoints + vm.SelectedSpread.RightPage.WidthPoints) * vm.ZoomLevel;
            // Combined width + 16px gap must fit inside 1600 - 64
            Assert.True(combinedPageWidth + 16.0 <= (1600.0 - 64.0));
        }
    }

    [Fact]
    public async Task PdfViewer_ManualZoom_ExitsFitModes()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(1);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");

        vm.ViewportSizeProvider = () => (1200.0, 800.0, 64.0, 64.0);
        vm.FitToWidthCommand.Execute(null);
        Assert.True(vm.IsFitToWidthActive);

        // Manual zoom in
        vm.ZoomInCommand.Execute(null);
        Assert.False(vm.IsFitToWidthActive);
        Assert.False(vm.IsFitToPageActive);
        Assert.Equal(PdfViewerZoomMode.Custom, vm.ZoomMode);

        // Fit to page
        vm.FitToPageCommand.Execute(null);
        Assert.True(vm.IsFitToPageActive);

        // Manual reset zoom
        vm.ResetZoomCommand.Execute(null);
        Assert.False(vm.IsFitToPageActive);
        Assert.Equal(PdfViewerZoomMode.Custom, vm.ZoomMode);
    }

    [Fact]
    public async Task PdfViewer_DedicatedLayoutCommands_SwitchModesAndAutoFit()
    {
        var vm = new PdfViewerViewModel();
        byte[] pdfBytes = CreateSamplePdfBytes(3);
        await vm.LoadDocumentBytesAsync(pdfBytes, "SampleReport.pdf");
        vm.ViewportSizeProvider = () => (1000.0, 700.0, 64.0, 64.0);

        // Initially Continuous Scroll
        Assert.True(vm.IsContinuousScroll);
        Assert.False(vm.IsSinglePageMode);
        Assert.False(vm.IsTwoPageSpreadMode);

        // Switch to Single Page
        vm.SetSinglePageLayoutCommand.Execute(null);
        Assert.False(vm.IsContinuousScroll);
        Assert.True(vm.IsSinglePageMode);
        Assert.False(vm.IsTwoPageSpreadMode);
        Assert.Equal(PdfViewLayoutMode.SinglePage, vm.SelectedLayoutMode);
        Assert.NotNull(vm.SelectedPage);
        Assert.True(vm.SelectedPage.WidthPoints > 0);
        Assert.True(vm.SelectedPage.HeightPoints > 0);
        Assert.True(vm.ZoomLevel > 0);
        Assert.True(vm.IsFitToPageActive);

        // Switch to Two Page Spread
        vm.SetTwoPageSpreadLayoutCommand.Execute(null);
        Assert.False(vm.IsContinuousScroll);
        Assert.False(vm.IsSinglePageMode);
        Assert.True(vm.IsTwoPageSpreadMode);
        Assert.Equal(PdfViewLayoutMode.TwoPageSpread, vm.SelectedLayoutMode);
        Assert.NotNull(vm.SelectedSpread);
        Assert.True(vm.ZoomLevel > 0);
        Assert.True(vm.SelectedSpread.LeftPage != null || vm.SelectedSpread.RightPage != null);
        Assert.True(vm.IsFitToPageActive);

        // Switch back to Continuous Scroll
        vm.SetContinuousScrollLayoutCommand.Execute(null);
        Assert.True(vm.IsContinuousScroll);
        Assert.False(vm.IsSinglePageMode);
        Assert.False(vm.IsTwoPageSpreadMode);
        Assert.Equal(PdfViewLayoutMode.ContinuousScroll, vm.SelectedLayoutMode);
    }

    [Fact]
    public void PdfViewer_ReadingThemeCommands_SwitchThemesAndColors()
    {
        var vm = new PdfViewerViewModel();

        // Default state
        Assert.True(vm.IsThemeDefault);
        Assert.False(vm.IsThemeSepia);
        Assert.False(vm.IsThemeDark);
        Assert.False(vm.IsThemeHighContrast);
        Assert.Equal("#F1F5F9", vm.ThemeBackgroundHex);
        Assert.Equal("#FFFFFF", vm.ThemePaperBackgroundHex);

        // Switch to Sepia
        vm.SetSepiaReadingThemeCommand.Execute(null);
        Assert.False(vm.IsThemeDefault);
        Assert.True(vm.IsThemeSepia);
        Assert.False(vm.IsThemeDark);
        Assert.False(vm.IsThemeHighContrast);
        Assert.Equal(PdfReaderTheme.Sepia, vm.ReadingTheme);
        Assert.Equal("#EDE3C9", vm.ThemeBackgroundHex);
        Assert.Equal("#FBF0D9", vm.ThemePaperBackgroundHex);
        Assert.Equal("#433422", vm.ThemeTextColorHex);
        Assert.Equal("#E6D5B8", vm.ThemeBorderColorHex);

        // Switch to Dark Night
        vm.SetDarkReadingThemeCommand.Execute(null);
        Assert.False(vm.IsThemeDefault);
        Assert.False(vm.IsThemeSepia);
        Assert.True(vm.IsThemeDark);
        Assert.False(vm.IsThemeHighContrast);
        Assert.Equal(PdfReaderTheme.Dark, vm.ReadingTheme);
        Assert.Equal("#0F172A", vm.ThemeBackgroundHex);
        Assert.Equal("#1E293B", vm.ThemePaperBackgroundHex);
        Assert.Equal("#F1F5F9", vm.ThemeTextColorHex);
        Assert.Equal("#334155", vm.ThemeBorderColorHex);

        // Switch to High Contrast
        vm.SetHighContrastReadingThemeCommand.Execute(null);
        Assert.False(vm.IsThemeDefault);
        Assert.False(vm.IsThemeSepia);
        Assert.False(vm.IsThemeDark);
        Assert.True(vm.IsThemeHighContrast);
        Assert.Equal(PdfReaderTheme.HighContrast, vm.ReadingTheme);
        Assert.Equal("#000000", vm.ThemeBackgroundHex);
        Assert.Equal("#000000", vm.ThemePaperBackgroundHex);
        Assert.Equal("#FFFF00", vm.ThemeTextColorHex);

        // Switch back to Daylight
        vm.SetDefaultReadingThemeCommand.Execute(null);
        Assert.True(vm.IsThemeDefault);
        Assert.False(vm.IsThemeSepia);
        Assert.False(vm.IsThemeDark);
        Assert.False(vm.IsThemeHighContrast);
        Assert.Equal(PdfReaderTheme.Default, vm.ReadingTheme);
        Assert.Equal("#F1F5F9", vm.ThemeBackgroundHex);
        Assert.Equal("#FFFFFF", vm.ThemePaperBackgroundHex);
    }

    [Fact]
    public void PdfViewer_ApplyThemeToSkBitmap_TransformsPixels()
    {
        using var source = new SkiaSharp.SKBitmap(10, 10);
        using (var canvas = new SkiaSharp.SKCanvas(source))
        {
            canvas.Clear(SkiaSharp.SKColors.White);
        }

        // Apply Dark mode
        using var darkBmp = PdfViewerViewModel.ApplyThemeToSkBitmap(source, PdfReaderTheme.Dark);
        Assert.NotNull(darkBmp);
        Assert.Equal(10, darkBmp.Width);
        Assert.Equal(10, darkBmp.Height);

        // Pure white should be darkened to dark slate
        var pixel = darkBmp.GetPixel(5, 5);
        Assert.True(pixel.Red < 100);
        Assert.True(pixel.Green < 100);
        Assert.True(pixel.Blue < 100);

        // Apply Sepia
        using var sepiaBmp = PdfViewerViewModel.ApplyThemeToSkBitmap(source, PdfReaderTheme.Sepia);
        Assert.NotNull(sepiaBmp);
        var sepiaPixel = sepiaBmp.GetPixel(5, 5);
        Assert.True(sepiaPixel.Red > sepiaPixel.Blue); // warm tint has more red than blue
    }

    [Fact]
    public void PdfViewer_RotateCommands_UpdateRotationAngleAndDynamicFit()
    {
        var vm = new PdfViewerViewModel();
        var page1 = new PdfViewerPageItem { PageNumber = 1, WidthPoints = 595, HeightPoints = 842, RotationAngle = 0 };
        var page2 = new PdfViewerPageItem { PageNumber = 2, WidthPoints = 595, HeightPoints = 842, RotationAngle = 0 };
        vm.Pages.Add(page1);
        vm.Pages.Add(page2);
        vm.SelectedPage = page1;

        // Initial state
        Assert.Equal(0, page1.RotationAngle);

        // Rotate clockwise: 0 -> 90 -> 180 -> 270 -> 0
        vm.RotateClockwiseCommand.Execute(null);
        Assert.Equal(90, page1.RotationAngle);

        vm.RotateClockwiseCommand.Execute(null);
        Assert.Equal(180, page1.RotationAngle);

        vm.RotateClockwiseCommand.Execute(null);
        Assert.Equal(270, page1.RotationAngle);

        vm.RotateClockwiseCommand.Execute(null);
        Assert.Equal(0, page1.RotationAngle);

        // Rotate counter-clockwise: 0 -> 270 -> 180 -> 90 -> 0
        vm.RotateCounterClockwiseCommand.Execute(null);
        Assert.Equal(270, page1.RotationAngle);

        vm.RotateCounterClockwiseCommand.Execute(null);
        Assert.Equal(180, page1.RotationAngle);

        vm.RotateCounterClockwiseCommand.Execute(null);
        Assert.Equal(90, page1.RotationAngle);

        vm.RotateCounterClockwiseCommand.Execute(null);
        Assert.Equal(0, page1.RotationAngle);

        // Rotate all pages clockwise
        vm.RotateAllPagesClockwiseCommand.Execute(null);
        Assert.Equal(90, page1.RotationAngle);
        Assert.Equal(90, page2.RotationAngle);

        // Dynamic fit to width with 90 degree rotation should use HeightPoints (842) as target width instead of WidthPoints (595)
        vm.FitToWidthDynamic(viewportWidth: 1000, horizontalPadding: 64);
        double expectedZoom90 = Math.Clamp(Math.Round((1000 - 64 - 8) / 842.0, 2), 0.25, 5.0);
        Assert.Equal(expectedZoom90, vm.ZoomLevel);

        // Rotate back to 0
        page1.RotationAngle = 0;
        vm.FitToWidthDynamic(viewportWidth: 1000, horizontalPadding: 64);
        double zoomAt0 = vm.ZoomLevel;
        Assert.True(zoomAt0 > expectedZoom90); // Portrait fit zoom (1.55) is larger than landscape fit zoom (1.10)
    }
}


