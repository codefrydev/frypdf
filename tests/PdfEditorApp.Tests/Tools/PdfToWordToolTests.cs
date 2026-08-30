using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class PdfToWordToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PdfToWordToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PdfToWordTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PdfToWordToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToWord);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PdfToWord, vm.Tool.Id);
        Assert.True(vm.ExtractTables);
    }

    [Fact]
    public async Task PdfToWordTool_ConvertsPdfToWordSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("WordSample", 2);
        try
        {
            var vm = (PdfToWordToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToWord);
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
