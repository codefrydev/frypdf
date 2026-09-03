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

public class PdfToMarkdownToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public PdfToMarkdownToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void PdfToMarkdownTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (PdfToMarkdownToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToMarkdown);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.PdfToMarkdown, vm.Tool.Id);
        Assert.True(vm.IncludeTables);
        Assert.True(vm.IncludeMetadataHeader);
    }

    [Fact]
    public async Task PdfToMarkdownTool_ConvertsPdfToMarkdownSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("MarkdownSample", 2);
        try
        {
            var vm = (PdfToMarkdownToolViewModel)_fixture.Factory.Create(PdfToolId.PdfToMarkdown);
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
