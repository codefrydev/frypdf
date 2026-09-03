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

public class WordToPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public WordToPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void WordToPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (WordToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.WordToPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.WordToPdf, vm.Tool.Id);
        Assert.Equal(PageOrientation.Portrait, vm.Orientation);
    }

    [Fact]
    public async Task WordToPdfTool_ConvertsDocumentSuccessfully()
    {
        string sampleDoc = ToolTestFixture.CreateSampleDocx("TestWord");
        try
        {
            var vm = (WordToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.WordToPdf);
            vm.SelectedFiles.Add(sampleDoc);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
        }
        finally
        {
            if (File.Exists(sampleDoc)) File.Delete(sampleDoc);
        }
    }
}
