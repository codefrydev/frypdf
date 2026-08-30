using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class RedactPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public RedactPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RedactPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.RedactPdf, vm.Tool.Id);
        Assert.True(vm.PermanentScrubText);
        Assert.Equal("CONFIDENTIAL", vm.SearchPattern);
    }

    [Fact]
    public async Task RedactPdfTool_SanitizesDocumentSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RedactSample", 1);
        try
        {
            var vm = (RedactPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RedactPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SearchPattern = "confidential";

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
