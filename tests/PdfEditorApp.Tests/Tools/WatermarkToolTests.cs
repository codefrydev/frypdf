using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class WatermarkToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public WatermarkToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void WatermarkTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (WatermarkToolViewModel)_fixture.Factory.Create(PdfToolId.Watermark);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.Watermark, vm.Tool.Id);
        Assert.Equal("CONFIDENTIAL", vm.WatermarkText);
        Assert.Equal(0.35, vm.Opacity);
        Assert.Equal(-45, vm.RotationAngle);
    }

    [Fact]
    public async Task WatermarkTool_ExecutesWatermarkingSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("WatermarkSample", 2);
        try
        {
            var vm = (WatermarkToolViewModel)_fixture.Factory.Create(PdfToolId.Watermark);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.WatermarkText = "INTERNAL ONLY";
            vm.Opacity = 0.5;

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
