using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public interface IProjectPersistenceService
{
    Task SaveProjectAsync(PdfDocumentModel model, string filePath);
    Task<PdfDocumentModel?> LoadProjectAsync(string filePath);
    Task SaveAutoSaveAsync(PdfDocumentModel model, string? currentFilePath);
    bool HasRecoverableAutoSave(string? currentFilePath, out string autoSavePath);
    Task<PdfDocumentModel?> LoadAutoSaveAsync(string autoSavePath);
    void CleanAutoSave(string? currentFilePath);
}

public class ProjectPersistenceService : IProjectPersistenceService
{
    private readonly JsonSerializerOptions _options;
    private readonly string _autoSaveDirectory;
    private readonly IPdfImportService _importService;
    private readonly PdfEditorApp.Core.Plugins.Descriptors.ICanvasElementRegistry? _canvasElementRegistry;

    public ProjectPersistenceService(
        IPdfImportService? importService = null,
        PdfEditorApp.Core.Plugins.Descriptors.ICanvasElementRegistry? canvasElementRegistry = null)
    {
        _importService = importService ?? new PdfImportService();
        _canvasElementRegistry = canvasElementRegistry;
        _options = PdfEditorApp.Core.Models.Elements.DynamicElementJsonResolver.CreateOptions(
            _canvasElementRegistry != null ? () => _canvasElementRegistry.GetAllDescriptors() : null,
            writeIndented: true);

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _autoSaveDirectory = Path.Combine(localAppData, "FryPDF", "AutoSave");
        try
        {
            if (!Directory.Exists(_autoSaveDirectory))
            {
                Directory.CreateDirectory(_autoSaveDirectory);
            }
        }
        catch
        {
            _autoSaveDirectory = Path.GetTempPath();
        }
    }

    public async Task SaveProjectAsync(PdfDocumentModel model, string filePath)
    {
        var json = JsonSerializer.Serialize(model, _options);
        string tempPath = filePath + ".tmp";

        // Write safely to temp file first
        await File.WriteAllTextAsync(tempPath, json);

        // Atomic swap
        if (File.Exists(filePath))
        {
            File.Replace(tempPath, filePath, null);
        }
        else
        {
            File.Move(tempPath, filePath);
        }

        // Clean any staging autosave after successful explicit save
        CleanAutoSave(filePath);
    }

    public async Task<PdfDocumentModel?> LoadProjectAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        // 1. Check if file is binary PDF (by extension or PDF magic header %PDF-)
        bool isPdf = false;
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".pdf")
        {
            isPdf = true;
        }
        else
        {
            try
            {
                using var fs = File.OpenRead(filePath);
                byte[] header = new byte[5];
                int read = await fs.ReadAsync(header, 0, 5);
                if (read >= 4 && header[0] == '%' && header[1] == 'P' && header[2] == 'D' && header[3] == 'F')
                {
                    isPdf = true;
                }
            }
            catch { }
        }

        if (isPdf)
        {
            return await _importService.ImportPdfAsync(filePath);
        }

        // 2. Load as JSON FryPDF project
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<PdfDocumentModel>(json, _options);
    }

    public string GetAutoSaveFilePath(string? currentFilePath)
    {
        string key = string.IsNullOrWhiteSpace(currentFilePath) ? "untitled_session" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(currentFilePath)));
        return Path.Combine(_autoSaveDirectory, $"{key}.autosave.frypdf");
    }

    public async Task SaveAutoSaveAsync(PdfDocumentModel model, string? currentFilePath)
    {
        string autoSaveFile = GetAutoSaveFilePath(currentFilePath);
        string json = JsonSerializer.Serialize(model, _options);
        await File.WriteAllTextAsync(autoSaveFile, json);
    }

    public bool HasRecoverableAutoSave(string? currentFilePath, out string autoSavePath)
    {
        autoSavePath = GetAutoSaveFilePath(currentFilePath);
        if (!File.Exists(autoSavePath)) return false;

        if (string.IsNullOrWhiteSpace(currentFilePath) || !File.Exists(currentFilePath))
        {
            return true;
        }

        // If autosave file is newer than the saved project file
        var autoSaveTime = File.GetLastWriteTimeUtc(autoSavePath);
        var projectFileTime = File.GetLastWriteTimeUtc(currentFilePath);
        return autoSaveTime > projectFileTime.AddSeconds(5);
    }

    public async Task<PdfDocumentModel?> LoadAutoSaveAsync(string autoSavePath)
    {
        return await LoadProjectAsync(autoSavePath);
    }

    public void CleanAutoSave(string? currentFilePath)
    {
        try
        {
            string path = GetAutoSaveFilePath(currentFilePath);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
