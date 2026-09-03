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
using PdfEditorApp.Tests.Tools.Core;
using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools.Organize;

public class SplitPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public SplitPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SplitPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (SplitPdfToolViewModel)_fixture.Factory.Create(PdfToolId.SplitPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.SplitPdf, vm.Tool.Id);
        Assert.Equal(SplitExtractMode.SplitEveryNPages, vm.SplitMode);
        Assert.Equal(1, vm.SplitPagesInterval);
    }

    [Fact]
    public async Task SplitPdfTool_SplitsEveryPageSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("SplitSample", 4);
        try
        {
            var vm = (SplitPdfToolViewModel)_fixture.Factory.Create(PdfToolId.SplitPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SplitPagesInterval = 2;

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }

    [Fact]
    public async Task SplitPdfTool_SplitByPageRangesMode_HonorsRangeExpression()
    {
        // Regression guard: SplitMode had no UI control to set it, so the tool always
        // fell back to "every N pages" regardless of the (also-visible) range textbox.
        string sample = ToolTestFixture.CreateSamplePdf("SplitRangeSample", 10);
        try
        {
            var vm = (SplitPdfToolViewModel)_fixture.Factory.Create(PdfToolId.SplitPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SplitMode = SplitExtractMode.SplitByPageRanges;
            vm.SplitRangeExpression = "1-3, 5, 7-10";

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            // Three distinct range groups (1-3 / 5 / 7-10) => 3 output files, not 10.
            Assert.Contains("into 3 files", vm.ResultSummaryMessage);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }
}
