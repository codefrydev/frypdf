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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;
using Xunit;

namespace PdfEditorApp.Tests.Tools.Organize;

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
        string tempDir = ToolTestFixture.CreateIsolatedDirectory("MergeExec");
        try
        {
            string file1 = ToolTestFixture.CreateSamplePdf("Doc1", 2, tempDir);
            string file2 = ToolTestFixture.CreateSamplePdf("Doc2", 3, tempDir);

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
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MergePdfTool_MergesPaddedAndWebPdfsSuccessfully()
    {
        string tempDir = ToolTestFixture.CreateIsolatedDirectory("MergePadded");
        try
        {
            string file1 = ToolTestFixture.CreatePaddedWebPdf("WebDoc1", 2, tempDir);
            string file2 = ToolTestFixture.CreatePaddedWebPdf("WebDoc2", 3, tempDir);

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
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
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

    [Fact]
    public async Task MergePdfTool_SaveOutputFileAs_DoesNotThrowWhenSavingToSamePath()
    {
        string tempDir = ToolTestFixture.CreateIsolatedDirectory("MergeSamePath");
        try
        {
            string file1 = ToolTestFixture.CreateSamplePdf("Doc1", 1, tempDir);
            string file2 = ToolTestFixture.CreateSamplePdf("Doc2", 1, tempDir);

            var vm = (MergePdfToolViewModel)_fixture.Factory.Create(PdfToolId.MergePdf);
            vm.SelectedFiles.Add(file1);
            vm.SelectedFiles.Add(file2);
            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);
            Assert.True(File.Exists(vm.LastOutputFilePath));

            // Saving to the exact same path must NOT throw IOException (self-copy)
            vm.SaveOutputFileToPath(vm.LastOutputFilePath);

            Assert.True(vm.HasSavedNotification);
            Assert.False(vm.HasError);
            Assert.Contains("Saved successfully", vm.SavedNotificationMessage);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task MergePdfTool_SaveOutputFileAs_HandlesLockedTargetGracefully()
    {
        string tempDir = ToolTestFixture.CreateIsolatedDirectory("MergeLocked");
        string file1 = ToolTestFixture.CreateSamplePdf("Doc1", 1, tempDir);
        string file2 = ToolTestFixture.CreateSamplePdf("Doc2", 1, tempDir);
        string lockedTarget = Path.Combine(tempDir, $"locked_target_{Guid.NewGuid():N}.pdf");
        File.WriteAllText(lockedTarget, "locked content");

        try
        {
            using var lockStream = new FileStream(lockedTarget, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var vm = (MergePdfToolViewModel)_fixture.Factory.Create(PdfToolId.MergePdf);
            vm.SelectedFiles.Add(file1);
            vm.SelectedFiles.Add(file2);
            await vm.ExecuteToolCommand.ExecuteAsync(null);

            Assert.True(vm.IsComplete);

            // Saving to an externally locked file must NOT crash with an unhandled exception
            vm.SaveOutputFileToPath(lockedTarget);

            Assert.False(vm.HasSavedNotification);
            Assert.True(vm.HasError);
            Assert.Contains("currently open or in use", vm.ErrorMessage);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
