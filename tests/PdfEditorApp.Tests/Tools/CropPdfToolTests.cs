using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class CropPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public CropPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CropPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (CropPdfToolViewModel)_fixture.Factory.Create(PdfToolId.CropPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.CropPdf, vm.Tool.Id);
        Assert.Equal(36, vm.CropMarginTop);
        Assert.Equal(36, vm.CropMarginBottom);
    }

    [Fact]
    public async Task CropPdfTool_CropsPdfPagesSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("CropSample", 2);
        try
        {
            var vm = (CropPdfToolViewModel)_fixture.Factory.Create(PdfToolId.CropPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.CropMarginTop = 15;
            vm.CropMarginBottom = 15;
            vm.CropMarginLeft = 10;
            vm.CropMarginRight = 10;

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
