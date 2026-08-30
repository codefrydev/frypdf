using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class ProtectPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public ProtectPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ProtectPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (ProtectPdfToolViewModel)_fixture.Factory.Create(PdfToolId.ProtectPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.ProtectPdf, vm.Tool.Id);
        Assert.True(vm.AllowPrinting);
        Assert.False(vm.AllowCopying);
    }

    [Fact]
    public async Task ProtectPdfTool_ProtectsWithPasswordSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("ProtectSample", 2);
        try
        {
            var vm = (ProtectPdfToolViewModel)_fixture.Factory.Create(PdfToolId.ProtectPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.UserPassword = "Password123";

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
