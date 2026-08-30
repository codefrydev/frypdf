using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class PdfToPowerPointToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PdfToPowerPointToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PdfToPowerPointTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PdfToPowerPointToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToPowerPoint);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PdfToPowerPoint, vm.Tool.Id);
        Assert.True(vm.EditableText);
    }

    [Fact]
    public async Task PdfToPowerPointTool_ConvertsPdfToPptxSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("PptSample", 2);
        try
        {
            var vm = (PdfToPowerPointToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToPowerPoint);
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
