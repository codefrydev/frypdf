using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class PdfFormsToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PdfFormsToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PdfFormsTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PdfFormsToolViewModel)_fixture.Factory.Create(PdfToolId.PdfForms);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PdfForms, vm.Tool.Id);
        Assert.False(vm.FlattenFields);
    }

    [Fact]
    public async Task PdfFormsTool_FlattensFormsSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("FormSample", 1);
        try
        {
            var vm = (PdfFormsToolViewModel)_fixture.Factory.Create(PdfToolId.PdfForms);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.FlattenFields = true;

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
