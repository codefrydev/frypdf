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

namespace PdfEditorApp.Tests.Tools.Security;

public class SignPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public SignPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SignPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (SignPdfToolViewModel)_fixture.Factory.Create(PdfToolId.SignPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.SignPdf, vm.Tool.Id);
        Assert.Equal("Jane Doe", vm.SignerName);
        Assert.Equal(SignatureStyle.CursiveElegance, vm.Style);
    }

    [Fact]
    public async Task SignPdfTool_SignsDocumentSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("SignSample", 1);
        try
        {
            var vm = (SignPdfToolViewModel)_fixture.Factory.Create(PdfToolId.SignPdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            vm.SignerName = "Alex Morgan";

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
