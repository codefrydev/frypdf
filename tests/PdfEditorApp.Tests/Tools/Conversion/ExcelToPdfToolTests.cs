using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Intelligence;
using PdfEditorApp.ViewModels.Tools.Core;
using PdfEditorApp.ViewModels.Tools.Organize;
using PdfEditorApp.ViewModels.Tools.Security;
using PdfEditorApp.ViewModels.Tools.Conversion;
using PdfEditorApp.ViewModels.Tools.Intelligence;
using PdfEditorApp.Tests.Tools.Core;
using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools.Conversion;

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
