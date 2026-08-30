using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools;

public class MergePdfToolTests : IClassFixture<ToolTestFixture>
{
    private readonly ToolTestFixture _fixture;

    public MergePdfToolTests(ToolTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void MergePdfTool_InstantiatesWithCorrectDefaults()
    {
        var vm = (MergePdfToolViewModel)_fixture.Factory.Create(PdfToolId.MergePdf);
        Assert.NotNull(vm);
        Assert.Equal(PdfToolId.MergePdf, vm.Tool.Id);
        Assert.True(vm.PreserveBookmarks);
        Assert.False(vm.NormalizePageSizes);
        Assert.Empty(vm.SelectedFiles);
    }

    [Fact]
    public void MergePdfTool_ReordersAndSyncsPreviewItems()
    {
        var vm = (MergePdfToolViewModel)_fixture.Factory.Create(PdfToolId.MergePdf);
        vm.SelectedFiles.Add("fileA.pdf");
        vm.SelectedFiles.Add("fileB.pdf");
        vm.SelectedFiles.Add("fileC.pdf");
        vm.SyncPreviewItems();

        Assert.Equal(3, vm.SelectedFilePreviewItems.Count);
        Assert.Equal("fileA.pdf", vm.SelectedFilePreviewItems[0].FileName);
        Assert.Equal("#1", vm.SelectedFilePreviewItems[0].OrderIndexText);

        vm.MoveFileDownCommand.Execute("fileA.pdf");
        Assert.Equal("fileB.pdf", vm.SelectedFilePreviewItems[0].FileName);
        Assert.Equal("fileA.pdf", vm.SelectedFilePreviewItems[1].FileName);

        vm.MoveFileUpCommand.Execute("fileC.pdf");
        Assert.Equal("fileC.pdf", vm.SelectedFilePreviewItems[1].FileName);
        Assert.Equal("fileA.pdf", vm.SelectedFilePreviewItems[2].FileName);

        vm.RemoveFileCommand.Execute("fileC.pdf");
        Assert.Equal(2, vm.SelectedFilePreviewItems.Count);

        vm.ClearFilesCommand.Execute(null);
        Assert.Empty(vm.SelectedFilePreviewItems);
        Assert.Empty(vm.SelectedFiles);
    }

    [Fact]
    public async Task MergePdfTool_ExecutesMergeSuccessfully()
    {
        string file1 = ToolTestFixture.CreateSamplePdf("Doc1", 2);
        string file2 = ToolTestFixture.CreateSamplePdf("Doc2", 3);
        try
        {
            var vm = (MergePdfToolViewModel)_fixture.Factory.Create(PdfToolId.MergePdf);
            vm.SelectedFiles.Add(file1);
            vm.SelectedFiles.Add(file2);
            vm.SyncPreviewItems();

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));

            // Verify in-app output preview generation
            Assert.NotNull(vm.OutputFilePreview);
            Assert.Equal(5, vm.OutputFilePreview.PageCount);
            Assert.Equal(5, vm.OutputPageThumbnails.Count);
            Assert.Equal("Page 1 of 5", vm.OutputPageThumbnails[0].PageLabel);
            Assert.Equal(1, vm.OutputPageThumbnails[0].PageNumber);
        }
        finally
        {
            if (File.Exists(file1)) File.Delete(file1);
            if (File.Exists(file2)) File.Delete(file2);
        }
    }

    [Fact]
    public async Task MergePdfTool_MergesPaddedAndWebPdfsSuccessfully()
    {
        string file1 = ToolTestFixture.CreatePaddedWebPdf("WebDoc1", 2);
        string file2 = ToolTestFixture.CreatePaddedWebPdf("WebDoc2", 3);
        try
        {
            var vm = (MergePdfToolViewModel)_fixture.Factory.Create(PdfToolId.MergePdf);
            vm.SelectedFiles.Add(file1);
            vm.SelectedFiles.Add(file2);
            vm.SyncPreviewItems();

            Assert.Equal(2, vm.SelectedFilePreviewItems.Count);
            Assert.Equal(5, vm.TotalSelectedPages);

            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.False(vm.HasError);
            Assert.True(File.Exists(vm.LastOutputFilePath));
            Assert.Equal(5, vm.OutputPageThumbnails.Count);
        }
        finally
        {
            if (File.Exists(file1)) File.Delete(file1);
            if (File.Exists(file2)) File.Delete(file2);
        }
    }

    [Fact]
    public void MergePdfTool_StartOver_ResetsStateAndPreviews()
    {
        var vm = (MergePdfToolViewModel)_fixture.Factory.Create(PdfToolId.MergePdf);
        vm.SelectedFiles.Add("sample.pdf");
        vm.SyncPreviewItems();
        Assert.True(vm.HasSelectedFiles);

        vm.StartOverCommand.Execute(null);
        Assert.False(vm.HasSelectedFiles);
        Assert.Empty(vm.SelectedFilePreviewItems);
        Assert.Empty(vm.OutputPageThumbnails);
        Assert.Null(vm.OutputFilePreview);
        Assert.False(vm.IsComplete);
    }
}
