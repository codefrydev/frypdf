using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class RotatePdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public RotatePdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RotatePdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (RotatePdfToolViewModel)_fixture.Factory.Create(PdfToolId.RotatePdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.RotatePdf, vm.Tool.Id);
        Assert.Equal(90, vm.RotationDegrees);
        Assert.Equal(PageFilterTarget.All, vm.TargetFilter);
    }

    [Fact]
    public async Task RotatePdfTool_ExecutesRotationSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RotateSample", 2);
        try
        {
            var vm = (RotatePdfToolViewModel)_fixture.Factory.Create(PdfToolId.RotatePdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.RotationDegrees = 180;

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
