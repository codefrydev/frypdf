using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

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

        await vm.ExecuteToolCommand.ExecuteAsync(null);

        Assert.True(vm.IsComplete);
        Assert.False(vm.HasError);
        Assert.True(File.Exists(vm.LastOutputFilePath));
    }
}
