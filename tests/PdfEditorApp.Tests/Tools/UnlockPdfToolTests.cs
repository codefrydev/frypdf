using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class UnlockPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public UnlockPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void UnlockPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (UnlockPdfToolViewModel)_fixture.Factory.Create(PdfToolId.UnlockPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.UnlockPdf, vm.Tool.Id);
        Assert.Empty(vm.Password);
    }

    [Fact]
    public async Task UnlockPdfTool_UnlocksDocumentSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("UnlockSample", 2);
        try
        {
            var vm = (UnlockPdfToolViewModel)_fixture.Factory.Create(PdfToolId.UnlockPdf);
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
