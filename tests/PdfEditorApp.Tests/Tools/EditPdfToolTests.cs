using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class EditPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public EditPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void EditPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (EditPdfToolViewModel)_fixture.Factory.Create(PdfToolId.EditPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.EditPdf, vm.Tool.Id);
    }

    [Fact]
    public async Task EditPdfTool_OpensEditorSessionSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("EditSample", 1);
        try
        {
            var vm = (EditPdfToolViewModel)_fixture.Factory.Create(PdfToolId.EditPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

            bool openInEditorTriggered = false;
            string targetPath = "";
            vm.OpenInEditorRequested += path =>
            {
                openInEditorTriggered = true;
                targetPath = path;
            };

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(openInEditorTriggered);
            Assert.Equal(sample, targetPath);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }
}
