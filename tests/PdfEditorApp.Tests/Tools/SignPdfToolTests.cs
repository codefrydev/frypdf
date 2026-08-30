using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class SignPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public SignPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SignPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (SignPdfToolViewModel)_fixture.Factory.Create(PdfToolId.SignPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.SignPdf, vm.Tool.Id);
        Assert.Equal("Jane Doe", vm.SignerName);
        Assert.Equal(SignatureStyle.CursiveElegance, vm.Style);
    }

    [Fact]
    public async Task SignPdfTool_SignsDocumentSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("SignSample", 1);
        try
        {
            var vm = (SignPdfToolViewModel)_fixture.Factory.Create(PdfToolId.SignPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SignerName = "Alex Morgan";

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
