using System.Collections.Generic;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Services;

namespace PdfEditorApp.Tests.Mocks;

public class MockProjectPersistenceService : IProjectPersistenceService
{
    public List<(PdfDocumentModel Model, string FilePath)> SavedProjects { get; } = new();
    public List<(PdfDocumentModel Model, string? FilePath)> AutoSavedProjects { get; } = new();
    public bool HasRecoverable { get; set; }
    public string RecoverablePath { get; set; } = "mock.autosave.frypdf";
    public PdfDocumentModel? ModelToReturnOnLoad { get; set; }

    public Task SaveProjectAsync(PdfDocumentModel model, string filePath)
    {
        SavedProjects.Add((model, filePath));
        return Task.CompletedTask;
    }

    public Task<PdfDocumentModel?> LoadProjectAsync(string filePath)
    {
        return Task.FromResult(ModelToReturnOnLoad);
    }

    public Task SaveAutoSaveAsync(PdfDocumentModel model, string? currentFilePath)
    {
        AutoSavedProjects.Add((model, currentFilePath));
        return Task.CompletedTask;
    }

    public bool HasRecoverableAutoSave(string? currentFilePath, out string autoSavePath)
    {
        autoSavePath = RecoverablePath;
        return HasRecoverable;
    }

    public Task<PdfDocumentModel?> LoadAutoSaveAsync(string autoSavePath)
    {
        return Task.FromResult(ModelToReturnOnLoad);
    }

    public void CleanAutoSave(string? currentFilePath)
    {
        AutoSavedProjects.Clear();
    }
}
