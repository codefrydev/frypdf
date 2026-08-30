using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class RepairPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public RepairPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RepairPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (RepairPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RepairPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.RepairPdf, vm.Tool.Id);
    }

    [Fact]
    public async Task RepairPdfTool_RepairsDocumentSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RepairSample", 2);
        try
        {
            var vm = (RepairPdfToolViewModel)_fixture.Factory.Create(PdfToolId.RepairPdf);
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
