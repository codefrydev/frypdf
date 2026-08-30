using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
}


