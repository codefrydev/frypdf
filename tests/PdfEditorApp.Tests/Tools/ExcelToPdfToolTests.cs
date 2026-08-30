using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class ExcelToPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public ExcelToPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ExcelToPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (ExcelToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.ExcelToPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.ExcelToPdf, vm.Tool.Id);
        Assert.Equal(PageOrientation.Landscape, vm.Orientation);
    }

    [Fact]
    public async Task ExcelToPdfTool_ConvertsDocumentSuccessfully()
    {
        string sampleXlsx = ToolTestFixture.CreateSampleXlsx("TestExcel");
        try
        {
            var vm = (ExcelToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.ExcelToPdf);
            vm.SelectedFiles.Add(sampleXlsx);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
        }
        finally
        {
            if (File.Exists(sampleXlsx)) File.Delete(sampleXlsx);
        }
    }
}
