using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Services;

public interface IProjectPersistenceService
{
    Task SaveProjectAsync(PdfDocumentModel model, string filePath);
    Task<PdfDocumentModel?> LoadProjectAsync(string filePath);
}

public class ProjectPersistenceService : IProjectPersistenceService
{
    private readonly JsonSerializerOptions _options;

    public ProjectPersistenceService()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task SaveProjectAsync(PdfDocumentModel model, string filePath)
    {
        var json = JsonSerializer.Serialize(model, _options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<PdfDocumentModel?> LoadProjectAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<PdfDocumentModel>(json, _options);
    }
}
