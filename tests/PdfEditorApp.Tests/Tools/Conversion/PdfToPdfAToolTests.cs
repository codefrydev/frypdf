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
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools.Conversion;

public class PdfToPdfAToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PdfToPdfAToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PdfToPdfATool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PdfToPdfAToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToPdfA);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PdfToPdfA, vm.Tool.Id);
        Assert.Equal(PdfAStandard.PdfA2b, vm.Standard);
    }

    [Fact]
    public async Task PdfToPdfATool_ConvertsToPdfASuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("PdfASample", 1);
        try
        {
            var vm = (PdfToPdfAToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToPdfA);
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
