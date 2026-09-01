using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class RedactPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public RedactPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RedactPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.RedactPdf, vm.Tool.Id);
        Assert.True(vm.PermanentScrubText);
        Assert.Equal("CONFIDENTIAL", vm.SearchPattern);
        Assert.Empty(vm.Marks);
        Assert.False(vm.HasMarks);
        Assert.Equal(1.0, vm.ZoomLevel);
    }

    [Fact]
    public void RedactPdfTool_ZoomIn_And_ZoomOut_ScaleDisplaySizeAndClamp()
    {
        var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
        double baseWidth = vm.DisplayPageWidth;

        vm.ZoomInCommand.Execute(null);
        Assert.True(vm.ZoomLevel > 1.0);
        Assert.True(vm.DisplayPageWidth > baseWidth);

        // Clamp at the top.
        for (int i = 0; i < 20; i++) vm.ZoomInCommand.Execute(null);
        Assert.Equal(3.0, vm.ZoomLevel);
        Assert.False(vm.CanZoomIn);

        for (int i = 0; i < 20; i++) vm.ZoomOutCommand.Execute(null);
        Assert.Equal(0.5, vm.ZoomLevel);
        Assert.False(vm.CanZoomOut);
    }

    [Fact]
    public async Task RedactPdfTool_ZoomChange_RepositionsExistingMarkHighlights()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RedactZoomRepositionSample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            await WaitForPreviewAsync(vm);

            vm.SearchPattern = "CONFIDENTIAL";
            await vm.FindAndMarkCommand.ExecuteAsync(null);
            Assert.True(vm.CurrentPageMarks.Count > 0);
            double originalDisplayX = vm.CurrentPageMarks[0].DisplayX;

            vm.ZoomInCommand.Execute(null);

            // Recomputed synchronously as soon as ZoomLevel changes, ahead of the
            // debounced re-render — the highlight must track the new scale immediately.
            Assert.True(vm.CurrentPageMarks.Count > 0);
            Assert.NotEqual(originalDisplayX, vm.CurrentPageMarks[0].DisplayX);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task RedactPdfTool_FindAndMark_PopulatesMarksList()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RedactFindMarkSample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SearchPattern = "CONFIDENTIAL";

            await vm.FindAndMarkCommand.ExecuteAsync(null);

            Assert.True(vm.HasMarks);
            Assert.NotEmpty(vm.Marks);
            Assert.Contains("Marked", vm.SearchStatusMessage);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task RedactPdfTool_FindAndMark_DoesNotDuplicateOnRepeatedSearch()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RedactNoDupSample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SearchPattern = "CONFIDENTIAL";

            await vm.FindAndMarkCommand.ExecuteAsync(null);
            int firstCount = vm.Marks.Count;
            Assert.True(firstCount > 0);

            await vm.FindAndMarkCommand.ExecuteAsync(null);

            Assert.Equal(firstCount, vm.Marks.Count);
            Assert.Contains("already marked", vm.SearchStatusMessage);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task RedactPdfTool_RemoveMark_And_ClearMarks_Work()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RedactRemoveClearSample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SearchPattern = "CONFIDENTIAL";
            await vm.FindAndMarkCommand.ExecuteAsync(null);
            Assert.True(vm.Marks.Count > 0);

            var firstMark = vm.Marks[0];
            vm.RemoveMarkCommand.Execute(firstMark);
            Assert.DoesNotContain(firstMark, vm.Marks);

            vm.ClearMarksCommand.Execute(null);
            Assert.Empty(vm.Marks);
            Assert.False(vm.HasMarks);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task RedactPdfTool_NoMarks_BlocksExecution_ViaValidation()
    {
        // Regression guard for the new interactive flow: searching alone no longer
        // commits anything — the user must review matches (now in Marks) before
        // Execute is allowed to run at all.
        string sample = ToolTestFixture.CreateSamplePdf("RedactNoMarksSample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.False(vm.IsComplete);
            Assert.Contains("Search for text to redact", vm.ErrorMessage);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task RedactPdfTool_SearchWithNoMatches_SetsStatusMessage_DoesNotMark()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RedactSearchNoMatchSample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SearchPattern = "ThisTextDoesNotExistAnywhereInTheDocument";

            await vm.FindAndMarkCommand.ExecuteAsync(null);

            Assert.Empty(vm.Marks);
            Assert.False(vm.HasMarks);
            Assert.Contains("No matches", vm.SearchStatusMessage);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task RedactPdfTool_FullFlow_MarkThenExecute_ActuallyRemovesMatchedText()
    {
        // End-to-end regression guard: search finds matches → user reviews them in
        // Marks → Execute commits exactly those marks. Proves the matched text is
        // genuinely gone from the output, not just visually covered.
        string sample = ToolTestFixture.CreateSamplePdf("RedactFullFlowSample", 1);
        string? outputPath = null;
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SearchPattern = "CONFIDENTIAL";
            vm.PermanentScrubText = true;

            await vm.FindAndMarkCommand.ExecuteAsync(null);
            Assert.True(vm.HasMarks);

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            outputPath = vm.LastOutputFilePath;
            Assert.True(File.Exists(outputPath));

            using var outputDoc = UglyToad.PdfPig.PdfDocument.Open(outputPath);
            string extractedText = outputDoc.GetPage(1).Text;
            Assert.DoesNotContain("CONFIDENTIAL", extractedText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
            if (outputPath != null && File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RedactPdfTool_LoadingDocument_ResolvesPageCountAndDimensions()
    {
        // Note: actual Bitmap decoding needs Avalonia's platform rendering services,
        // which this headless xUnit host doesn't bootstrap — so PageBitmap itself isn't
        // asserted here. Page count/dimensions come from PdfPig directly and are
        // deliberately captured independent of bitmap decode success (see
        // RenderCurrentPageAsync), so they're verifiable in this environment and are
        // exactly what the highlight-overlay math depends on.
        string sample = ToolTestFixture.CreateSamplePdf("RedactPreviewSample", 3);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

            // Loading is fire-and-forget off the SelectedFiles collection change; give it
            // a moment to finish (waits on PageWidthPoints, set last in the load chain —
            // polling TotalPages instead would race ahead of the render actually finishing).
            await WaitForPreviewAsync(vm);

            Assert.Equal(3, vm.TotalPages);
            Assert.True(vm.PageWidthPoints > 0);
            Assert.True(vm.PageHeightPoints > 0);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    private static async Task WaitForPreviewAsync(RedactPdfToolViewModel vm)
    {
        for (int i = 0; i < 50 && vm.PageWidthPoints == 0; i++)
        {
            await Task.Delay(50);
        }
    }

    [Fact]
    public async Task RedactPdfTool_ForceDrawBox_AddsRawRectangleAsManualMark()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RedactDrawBoxSample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            await WaitForPreviewAsync(vm);
            Assert.True(vm.PageWidthPoints > 0);

            vm.AddManualMark(new Rect(50, 60, 120, 30), forceDrawBox: true);

            Assert.Single(vm.Marks);
            var mark = vm.Marks[0];
            Assert.Equal("Manual selection", mark.Label);
            Assert.Equal(0, mark.Region.PageIndex);
            Assert.True(mark.Region.Width > 0 && mark.Region.Height > 0);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task RedactPdfTool_DefaultDrag_SnapsToTouchedWordsLikeThePdfReader()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RedactSelectTextSample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            await WaitForPreviewAsync(vm);
            Assert.True(vm.PageWidthPoints > 0);

            // A drag covering the whole page must touch every word on it (default mode
            // snaps to text — no mode switch needed, matching the PDF Reader's behavior).
            vm.AddManualMark(new Rect(0, 0, vm.DisplayPageWidth, vm.DisplayPageHeight));

            Assert.Single(vm.Marks);
            var mark = vm.Marks[0];
            Assert.NotEqual("Manual selection", mark.Label);
            Assert.Contains("CONFIDENTIAL", mark.Label, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task RedactPdfTool_DefaultDrag_EmptyAreaAddsNothing()
    {
        // A drag that touches no words shouldn't create a mark by default — unlike
        // forceDrawBox: true, which always uses the raw rectangle.
        string sample = ToolTestFixture.CreateSamplePdf("RedactSelectTextEmptySample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            await WaitForPreviewAsync(vm);

            // Bottom-right corner is blank in the fixture's generated layout.
            vm.AddManualMark(new Rect(vm.DisplayPageWidth - 5, vm.DisplayPageHeight - 5, 3, 3));

            Assert.Empty(vm.Marks);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task FindRedactionMatchesAsync_FindsMatchesWithoutWritingAnyFile()
    {
        // The preview-support backend method: read-only, used by the interactive UI to
        // show matches before the user commits to redacting. Must not create/modify any file.
        string sample = ToolTestFixture.CreateSamplePdf("RedactFindOnlySample", 2);
        try
        {
            var matches = await _fixture.OperationsService.SecurityService.FindRedactionMatchesAsync(sample, "CONFIDENTIAL", caseSensitive: false);

            Assert.NotEmpty(matches);
            Assert.All(matches, m =>
            {
                Assert.True(m.PageIndex >= 0);
                Assert.True(m.Width > 0);
                Assert.True(m.Height > 0);
            });

            // No sibling output file should exist — this call must be side-effect free.
            string dir = Path.GetDirectoryName(sample)!;
            string baseName = Path.GetFileNameWithoutExtension(sample);
            Assert.False(File.Exists(Path.Combine(dir, $"{baseName}_Redacted.pdf")));
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task FindRedactionMatchesAsync_NoMatches_ReturnsEmptyList_NotAnError()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RedactFindNoneSample", 1);
        try
        {
            var matches = await _fixture.OperationsService.SecurityService.FindRedactionMatchesAsync(sample, "NothingLikeThisExistsHere", caseSensitive: false);
            Assert.Empty(matches);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }
}
