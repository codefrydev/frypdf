using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class AiSummarizerToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public AiSummarizerToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void AiSummarizerTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (AiSummarizerToolViewModel)_fixture.Factory.Create(PdfToolId.AiSummarizer);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.AiSummarizer, vm.Tool.Id);
        Assert.True(vm.IncludeActionItems);
        Assert.Equal(7, vm.MaxBulletPoints);
    }

    [Fact]
    public async Task AiSummarizerTool_SummarizesDocumentSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("AiSample", 2);
        try
        {
            var vm = (AiSummarizerToolViewModel)_fixture.Factory.Create(PdfToolId.AiSummarizer);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }
}
