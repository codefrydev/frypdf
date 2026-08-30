using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class PageNumbersToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PageNumbersToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PageNumbersTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PageNumbersToolViewModel)_fixture.Factory.Create(PdfToolId.PageNumbers);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PageNumbers, vm.Tool.Id);
        Assert.Equal(PageNumberPosition.BottomCenter, vm.Position);
        Assert.Equal("Page {n} of {total}", vm.Template);
    }

    [Fact]
    public async Task PageNumbersTool_StampsPageNumbersSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("PageNumSample", 3);
        try
        {
            var vm = (PageNumbersToolViewModel)_fixture.Factory.Create(PdfToolId.PageNumbers);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }
}
