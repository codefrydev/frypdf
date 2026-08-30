using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class ScanToPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public ScanToPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ScanToPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (ScanToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.ScanToPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.ScanToPdf, vm.Tool.Id);
        Assert.True(vm.AutoDeskew);
        Assert.True(vm.EnhanceContrast);
    }

    [Fact]
    public async Task ScanToPdfTool_ProcessesScanFilesSuccessfully()
    {
        string sample = ToolTestFixture.CreateSampleImage("ScanSample");
        try
        {
            var vm = (ScanToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.ScanToPdf);
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
