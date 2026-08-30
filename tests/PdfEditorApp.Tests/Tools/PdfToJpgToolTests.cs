using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class PdfToJpgToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PdfToJpgToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PdfToJpgTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PdfToJpgToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToJpg);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PdfToJpg, vm.Tool.Id);
        Assert.Equal("jpg", vm.OutputFormat);
        Assert.Equal(300, vm.Dpi);
    }

    [Fact]
    public async Task PdfToJpgTool_ExportsPdfToImagesSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("PdfToJpgSample", 2);
        try
        {
            var vm = (PdfToJpgToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToJpg);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

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
