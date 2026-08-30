using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class PowerPointToPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PowerPointToPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PowerPointToPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PowerPointToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.PowerPointToPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PowerPointToPdf, vm.Tool.Id);
        Assert.Equal(PageOrientation.Landscape, vm.Orientation);
    }

    [Fact]
    public async Task PowerPointToPdfTool_ConvertsDocumentSuccessfully()
    {
        string samplePptx = ToolTestFixture.CreateSamplePptx("TestPpt");
        try
        {
            var vm = (PowerPointToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.PowerPointToPdf);
            vm.SelectedFiles.Add(samplePptx);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
        }
        finally
        {
            if (File.Exists(samplePptx)) File.Delete(samplePptx);
        }
    }
}
