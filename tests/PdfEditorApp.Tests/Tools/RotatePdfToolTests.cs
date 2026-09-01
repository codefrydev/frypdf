using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class RotatePdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public RotatePdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RotatePdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (RotatePdfToolViewModel)_fixture.Factory.Create(PdfToolId.RotatePdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.RotatePdf, vm.Tool.Id);
        Assert.Equal(90, vm.RotationDegrees);
        Assert.Equal(PageFilterTarget.All, vm.TargetFilter);
    }

    [Fact]
    public async Task RotatePdfTool_ExecutesRotationSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("RotateSample", 2);
        try
        {
            var vm = (RotatePdfToolViewModel)_fixture.Factory.Create(PdfToolId.RotatePdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.RotationDegrees = 180;

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
    public async Task RotatePdfTool_MultiFileBatch_ProcessesEveryFile_NotJustTheFirst()
    {
        string sample1 = ToolTestFixture.CreateSamplePdf("RotateBatch1", 1);
        string sample2 = ToolTestFixture.CreateSamplePdf("RotateBatch2", 1);
        try
        {
            var vm = (RotatePdfToolViewModel)_fixture.Factory.Create(PdfToolId.RotatePdf);
            vm.SelectedFiles.Add(sample1);
            vm.SelectedFiles.Add(sample2);
            vm.SyncPreviewItems();
            vm.RotationDegrees = 90;

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            // Regression guard: before the fix, only the first selected file was ever
            // processed and the batch outcome was indistinguishable from a single-file run.
            Assert.Contains("2 of 2", vm.ResultSummaryMessage);
        }
        finally
        {
            if (File.Exists(sample1)) File.Delete(sample1);
            if (File.Exists(sample2)) File.Delete(sample2);
        }
    }
}
