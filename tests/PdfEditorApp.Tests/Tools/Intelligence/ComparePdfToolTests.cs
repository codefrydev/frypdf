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
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools.Intelligence;

public class ComparePdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public ComparePdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ComparePdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (ComparePdfToolViewModel)_fixture.Factory.Create(PdfToolId.ComparePdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.ComparePdf, vm.Tool.Id);
        Assert.True(vm.DetectTextDiff);
        Assert.True(vm.DetectVisualDiff);
    }

    [Fact]
    public async Task ComparePdfTool_ExecutesComparisonSuccessfully()
    {
        string docA = ToolTestFixture.CreateSamplePdf("CompareA", 2);
        string docB = ToolTestFixture.CreateSamplePdf("CompareB", 2);
        try
        {
            var vm = (ComparePdfToolViewModel)_fixture.Factory.Create(PdfToolId.ComparePdf);
            vm.DocumentAPath = docA;
            vm.DocumentBPath = docB;

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.NotEmpty(vm.ResultSummaryMessage);
        }
        finally
        {
            if (File.Exists(docA)) File.Delete(docA);
            if (File.Exists(docB)) File.Delete(docB);
        }
    }

    [Fact]
    public async Task ComparePdfTool_DetectsRealPageCountDifference_AndWritesReportFile()
    {
        // Regression guard: before the fix, the dispatcher was a stub that only compared
        // file byte-sizes and never wrote an output file — a 2-page and a 5-page document
        // would "succeed" identically with no indication anything differed.
        string docA = ToolTestFixture.CreateSamplePdf("CompareRealA", 2);
        string docB = ToolTestFixture.CreateSamplePdf("CompareRealB", 5);
        string? reportPath = null;
        try
        {
            var vm = (ComparePdfToolViewModel)_fixture.Factory.Create(PdfToolId.ComparePdf);
            vm.DocumentAPath = docA;
            vm.DocumentBPath = docB;

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            reportPath = vm.LastOutputFilePath;
            Assert.True(File.Exists(reportPath), "ComparePdf must produce a real report file, not a silent no-op.");

            string reportContent = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("2 pages", reportContent);
            Assert.Contains("5 pages", reportContent);
        }
        finally
        {
            if (File.Exists(docA)) File.Delete(docA);
            if (File.Exists(docB)) File.Delete(docB);
            if (reportPath != null && File.Exists(reportPath)) File.Delete(reportPath);
        }
    }
}
