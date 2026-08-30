using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class JpgToPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public JpgToPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void JpgToPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (JpgToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.JpgToPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.JpgToPdf, vm.Tool.Id);
        Assert.Equal(PageFormat.A4, vm.PageFormat);
        Assert.True(vm.AutoOrientation);
    }

    [Fact]
    public async Task JpgToPdfTool_CombinesImagesToPdfSuccessfully()
    {
        string imgPath = ToolTestFixture.CreateSampleImage("TestJpg");
        try
        {
            var vm = (JpgToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.JpgToPdf);
            vm.SelectedFiles.Add(imgPath);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
        }
        finally
        {
            if (File.Exists(imgPath)) File.Delete(imgPath);
        }
    }
}
