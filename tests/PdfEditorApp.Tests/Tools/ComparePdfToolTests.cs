using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

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
}
