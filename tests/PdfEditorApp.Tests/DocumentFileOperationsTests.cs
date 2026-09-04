using System;
using System.IO;
using System.Linq;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class DocumentFileOperationsTests : IDisposable
{
    private readonly string _tempTestDir;

    public DocumentFileOperationsTests()
    {
        _tempTestDir = Path.Combine(Path.GetTempPath(), "FryPdf_Test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempTestDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempTestDir))
            {
                Directory.Delete(_tempTestDir, recursive: true);
            }
        }
        catch { }
    }

    [Theory]
    [InlineData("ValidName", true)]
    [InlineData("Report_2026", true)]
    [InlineData("Document-Final v2", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(".", false)]
    [InlineData("..", false)]
    [InlineData("Bad/Name", false)]
    [InlineData("Bad\\Name", false)]
    [InlineData("Bad:Name", false)]
    [InlineData("Bad*Name", false)]
    [InlineData("Bad?Name", false)]
    [InlineData("Bad\"Name", false)]
    [InlineData("Bad<Name", false)]
    [InlineData("Bad>Name", false)]
    [InlineData("Bad|Name", false)]
    public void ValidateFileName_EvaluatesCorrectly(string candidate, bool expectedValid)
    {
        var isValid = FileOperationHelper.ValidateFileName(candidate, out var error);
        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
        {
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
    }

    [Fact]
    public void RenameFile_RenamesExistingFileSuccessfully()
    {
        // Arrange
        var originalPath = Path.Combine(_tempTestDir, "original_sample.pdf");
        File.WriteAllText(originalPath, "PDF content mock");

        // Act
        var success = FileOperationHelper.RenameFile(originalPath, "renamed_sample", out var newPath, out var error);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(newPath);
        Assert.False(File.Exists(originalPath));
        Assert.True(File.Exists(newPath));
        Assert.Equal("renamed_sample.pdf", Path.GetFileName(newPath));
        Assert.Equal("PDF content mock", File.ReadAllText(newPath));
    }

    [Fact]
    public void RenameFile_PreservesOrAppendsExtension()
    {
        // Arrange
        var originalPath = Path.Combine(_tempTestDir, "test_ext.frypdf");
        File.WriteAllText(originalPath, "Project mock");

        // Act - user types "my_new_project.frypdf" explicitly
        var success = FileOperationHelper.RenameFile(originalPath, "my_new_project.frypdf", out var newPath, out _);

        // Assert
        Assert.True(success);
        Assert.NotNull(newPath);
        Assert.True(File.Exists(newPath));
        Assert.Equal("my_new_project.frypdf", Path.GetFileName(newPath));
    }

    [Fact]
    public void RenameFile_FailsIfTargetAlreadyExists()
    {
        // Arrange
        var fileA = Path.Combine(_tempTestDir, "file_alpha.pdf");
        var fileB = Path.Combine(_tempTestDir, "file_beta.pdf");
        File.WriteAllText(fileA, "Alpha");
        File.WriteAllText(fileB, "Beta");

        // Act
        var success = FileOperationHelper.RenameFile(fileA, "file_beta", out var newPath, out var error);

        // Assert
        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("already exists", error, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(fileA));
        Assert.True(File.Exists(fileB));
    }

    [Fact]
    public void DuplicateFile_CreatesIndexedCopiesCorrectly()
    {
        // Arrange
        var originalPath = Path.Combine(_tempTestDir, "quarterly_summary.pdf");
        File.WriteAllText(originalPath, "Quarterly data mock");

        // Act 1: First copy
        var success1 = FileOperationHelper.DuplicateFile(originalPath, out var copyPath1, out var error1);

        // Assert 1
        Assert.True(success1);
        Assert.Null(error1);
        Assert.NotNull(copyPath1);
        Assert.True(File.Exists(copyPath1));
        Assert.Equal("quarterly_summary (Copy).pdf", Path.GetFileName(copyPath1));

        // Act 2: Second copy of original
        var success2 = FileOperationHelper.DuplicateFile(originalPath, out var copyPath2, out var error2);

        // Assert 2
        Assert.True(success2);
        Assert.Null(error2);
        Assert.NotNull(copyPath2);
        Assert.True(File.Exists(copyPath2));
        Assert.Equal("quarterly_summary (Copy 2).pdf", Path.GetFileName(copyPath2));
    }

    [Fact]
    public void DeleteFile_RemovesFileFromDiskSafely()
    {
        // Arrange
        var filePath = Path.Combine(_tempTestDir, "file_to_delete.pdf");
        File.WriteAllText(filePath, "Mock content");
        Assert.True(File.Exists(filePath));

        // Act
        var success = FileOperationHelper.DeleteFile(filePath, out var error);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.False(File.Exists(filePath));

        // Act 2: Deleting already non-existent file succeeds gracefully
        var success2 = FileOperationHelper.DeleteFile(filePath, out var error2);
        Assert.True(success2);
        Assert.Null(error2);
    }

    [Fact]
    public void HomeViewModel_RenameDialogFlow_WorksEndToEnd()
    {
        // Arrange
        var filePath = Path.Combine(_tempTestDir, "home_rename_sample.pdf");
        File.WriteAllText(filePath, "Home test content");

        var recentItem = new RecentDocumentItem
        {
            FilePath = filePath,
            Title = "home_rename_sample.pdf",
            LastOpened = DateTime.UtcNow
        };

        var mockRecentService = new Mocks.MockRecentDocumentsService();
        mockRecentService.Add(recentItem);

        var vm = new HomeViewModel(
            mockRecentService,
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry());
        vm.RefreshRecent();

        string? renamedOld = null;
        string? renamedNew = null;
        vm.DocumentRenamed += (oldP, newP) =>
        {
            renamedOld = oldP;
            renamedNew = newP;
        };

        // Act 1: Prompt Rename
        vm.PromptRenameCommand.Execute(filePath);
        Assert.True(vm.IsRenameDialogOpen);
        Assert.Equal("home_rename_sample", vm.RenameNewName);
        Assert.Equal(".pdf", vm.RenameExtension);
        Assert.Equal(filePath, vm.RenameTargetFilePath);

        // Act 2: Confirm Rename with new name
        vm.RenameNewName = "home_renamed_final";
        vm.ConfirmRenameCommand.Execute(null);

        // Assert
        Assert.False(vm.IsRenameDialogOpen);
        Assert.NotNull(renamedOld);
        Assert.NotNull(renamedNew);
        Assert.Equal(filePath, renamedOld);
        Assert.EndsWith("home_renamed_final.pdf", renamedNew);
        Assert.False(File.Exists(filePath));
        Assert.True(File.Exists(renamedNew));

        // Check RecentDocuments updated
        var updatedItem = vm.RecentDocuments.FirstOrDefault();
        Assert.NotNull(updatedItem);
        Assert.Equal(renamedNew, updatedItem.FilePath);
        Assert.Equal("home_renamed_final.pdf", updatedItem.Title);
    }

    [Fact]
    public void HomeViewModel_DeleteDialogFlow_WorksEndToEnd()
    {
        // Arrange
        var filePath = Path.Combine(_tempTestDir, "home_delete_sample.pdf");
        File.WriteAllText(filePath, "Home delete test");

        var recentItem = new RecentDocumentItem
        {
            FilePath = filePath,
            Title = "home_delete_sample.pdf",
            LastOpened = DateTime.UtcNow
        };

        var mockRecentService = new Mocks.MockRecentDocumentsService();
        mockRecentService.Add(recentItem);

        var vm = new HomeViewModel(
            mockRecentService,
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry());
        vm.RefreshRecent();

        string? deletedPath = null;
        vm.DocumentDeleted += p => deletedPath = p;

        // Act 1: Prompt Delete
        vm.PromptDeleteCommand.Execute(filePath);
        Assert.True(vm.IsDeleteConfirmDialogOpen);
        Assert.Equal(filePath, vm.DeleteTargetFilePath);
        Assert.Equal("home_delete_sample.pdf", vm.DeleteTargetFileName);

        // Act 2: Confirm Delete
        vm.ConfirmDeleteCommand.Execute(null);

        // Assert
        Assert.False(vm.IsDeleteConfirmDialogOpen);
        Assert.Equal(filePath, deletedPath);
        Assert.False(File.Exists(filePath));
        Assert.Empty(vm.RecentDocuments);
    }

    [Fact]
    public void HomeViewModel_DuplicateCommand_CreatesDuplicateAndUpdatesRecents()
    {
        // Arrange
        var filePath = Path.Combine(_tempTestDir, "sample_to_dup.pdf");
        File.WriteAllText(filePath, "Duplicate test");

        var recentItem = new RecentDocumentItem
        {
            FilePath = filePath,
            Title = "sample_to_dup.pdf",
            LastOpened = DateTime.UtcNow
        };

        var mockRecentService = new Mocks.MockRecentDocumentsService();
        mockRecentService.Add(recentItem);

        var vm = new HomeViewModel(
            mockRecentService,
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry());
        vm.RefreshRecent();

        // Act
        vm.DuplicateDocumentCommand.Execute(filePath);

        // Assert
        Assert.Equal(2, vm.RecentDocuments.Count);
        var copyItem = vm.RecentDocuments.FirstOrDefault(x => x.Title.Contains("(Copy)"));
        Assert.NotNull(copyItem);
        Assert.True(File.Exists(copyItem.FilePath));
    }
}
