using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class OrganizePdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public OrganizePdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void OrganizePdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (OrganizePdfToolViewModel)_fixture.Factory.Create(PdfToolId.OrganizePdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.OrganizePdf, vm.Tool.Id);
    }

    [Fact]
    public async Task OrganizePdfTool_ReordersPagesSuccessfully()
    {
        string sample = ToolTestFixture.CreateSamplePdf("OrganizeSample", 3);
        try
        {
            var vm = (OrganizePdfToolViewModel)_fixture.Factory.Create(PdfToolId.OrganizePdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();
            // This standalone view has no reorder UI yet — set a real order directly to
            // exercise the underlying reorder logic (see the honesty-fix test below for
            // what happens without one).
            vm.PageOrder = new System.Collections.Generic.List<int> { 2, 1, 0 };

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
    public async Task OrganizePdfTool_NoOrderOrDeletionSet_ReportsHonestFailure_NotFakeSuccess()
    {
        // Regression guard: this standalone view has no reorder/delete/rotate controls, so
        // PageOrder/PagesToDelete are always empty here. Before the fix, running the tool
        // still claimed "Organized PDF to N pages" success while silently saving an
        // unchanged copy of the input.
        string sample = ToolTestFixture.CreateSamplePdf("OrganizeNoOpSample", 3);
        try
        {
            var vm = (OrganizePdfToolViewModel)_fixture.Factory.Create(PdfToolId.OrganizePdf);
            vm.SelectedFiles.Add(sample);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.HasError);
            Assert.False(vm.IsComplete);
            Assert.Contains("Pages Sidebar", vm.ErrorMessage);
        }
        finally
        {
            if (File.Exists(sample)) File.Delete(sample);
        }
    }
}
