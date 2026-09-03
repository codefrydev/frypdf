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

namespace PdfEditorApp.Tests.Tools.Conversion;

public class HtmlToPdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public HtmlToPdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void HtmlToPdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (HtmlToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.HtmlToPdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.HtmlToPdf, vm.Tool.Id);
        Assert.Equal(PageFormat.A4, vm.Format);
        Assert.Equal(PageOrientation.Portrait, vm.Orientation);
    }

    [Fact]
    public async Task HtmlToPdfTool_ConvertsHtmlContentSuccessfully()
    {
        var vm = (HtmlToPdfToolViewModel)_fixture.Factory.Create(PdfToolId.HtmlToPdf);
        vm.HtmlContentOrUrl = "<html><body><h1>Sample Document</h1><p>Test HTML body content.</p></body></html>";

        try
        {
            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete, $"Error: {vm.ErrorMessage}");
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
        }
        finally
        {
            if (!string.IsNullOrEmpty(vm.LastOutputFilePath) && File.Exists(vm.LastOutputFilePath))
            {
                try { File.Delete(vm.LastOutputFilePath); } catch { }
            }
        }
    }
}
