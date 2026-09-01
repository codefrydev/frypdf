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

    [Fact]
    public async Task PdfToWordTool_MultiFileBatch_ProcessesEveryFile_NotJustTheFirst()
    {
        string sample1 = ToolTestFixture.CreateSamplePdf("WordBatch1", 1);
        string sample2 = ToolTestFixture.CreateSamplePdf("WordBatch2", 1);
        try
        {
            var vm = (PdfToWordToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToWord);
            vm.SelectedFiles.Add(sample1);
            vm.SelectedFiles.Add(sample2);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            // Regression guard: before the fix, only the first selected file was ever
            // converted and the second was silently dropped with no error.
            Assert.Contains("2 of 2", vm.ResultSummaryMessage);
        }
        finally
        {
            if (File.Exists(sample1)) File.Delete(sample1);
            if (File.Exists(sample2)) File.Delete(sample2);
        }
    }
}
