using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class TranslatePdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public TranslatePdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void TranslatePdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (TranslatePdfToolViewModel)_fixture.Factory.Create(PdfToolId.TranslatePdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.TranslatePdf, vm.Tool.Id);
        Assert.Equal("Auto", vm.SourceLanguage);
        Assert.Equal("Spanish", vm.TargetLanguage);
        Assert.True(vm.PreserveLayout);
    }

    [Fact]
    public async Task TranslatePdfTool_TranslatesDocumentSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("TranslateSample", 1);
        try
        {
            var vm = (TranslatePdfToolViewModel)_fixture.Factory.Create(PdfToolId.TranslatePdf);
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
