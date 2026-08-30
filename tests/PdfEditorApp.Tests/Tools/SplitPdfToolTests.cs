using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

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
}
