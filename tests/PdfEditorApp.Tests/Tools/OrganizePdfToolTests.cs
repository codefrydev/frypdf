using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class OrganizePdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public OrganizePdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void OrganizePdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (OrganizePdfToolViewModel)_fixture.Factory.Create(PdfToolId.OrganizePdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.OrganizePdf, vm.Tool.Id);
    }

    [Fact]
    public async Task OrganizePdfTool_ReordersPagesSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("OrganizeSample", 3);
        try
        {
            var vm = (OrganizePdfToolViewModel)_fixture.Factory.Create(PdfToolId.OrganizePdf);
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
