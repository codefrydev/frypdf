using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class PdfToExcelToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PdfToExcelToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PdfToExcelTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PdfToExcelToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToExcel);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PdfToExcel, vm.Tool.Id);
        Assert.True(vm.DetectAllTables);
    }

    [Fact]
    public async Task PdfToExcelTool_ConvertsPdfToExcelSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("ExcelSample", 2);
        try
        {
            var vm = (PdfToExcelToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToExcel);
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
