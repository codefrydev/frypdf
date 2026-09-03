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

public class JpgToPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public JpgToPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void JpgToPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (JpgToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.JpgToPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.JpgToPdf, vm.Tool.Id);
        Assert.Equal(PageFormat.A4, vm.PageFormat);
        Assert.True(vm.AutoOrientation);
    }

    [Fact]
    public async Task JpgToPdfTool_CombinesImagesToPdfSuccessfully()
    {
        string imgPath = ToolTestFixture.CreateSampleImage("TestJpg");
        try
        {
            var vm = (JpgToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.JpgToPdf);
            vm.SelectedFiles.Add(imgPath);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
        }
        finally
        {
            if (File.Exists(imgPath)) File.Delete(imgPath);
        }
    }
}
