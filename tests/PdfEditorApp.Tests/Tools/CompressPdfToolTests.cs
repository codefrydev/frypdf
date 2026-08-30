using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class CompressPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public CompressPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CompressPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (CompressPdfToolViewModel)_fixture.Factory.Create(PdfToolId.CompressPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.CompressPdf, vm.Tool.Id);
        Assert.Equal(PdfCompressionLevel.Balanced, vm.CompressionLevel);
        Assert.Equal(150, vm.ImageQualityDpi);
    }

    [Fact]
    public async Task CompressPdfTool_ExecutesCompressionSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("CompressSample", 3);
        try
        {
            var vm = (CompressPdfToolViewModel)_fixture.Factory.Create(PdfToolId.CompressPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.CompressionLevel = PdfCompressionLevel.MaximumCompression;

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
