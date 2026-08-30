using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class OcrPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public OcrPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void OcrPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (OcrPdfToolViewModel)_fixture.Factory.Create(PdfToolId.OcrPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.OcrPdf, vm.Tool.Id);
        Assert.Equal("eng", vm.Language);
        Assert.True(vm.GenerateSearchablePdf);
    }

    [Fact]
    public async Task OcrPdfTool_ExecutesOcrSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("OcrSample", 1);
        try
        {
            var vm = (OcrPdfToolViewModel)_fixture.Factory.Create(PdfToolId.OcrPdf);
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
