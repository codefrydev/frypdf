using System;
using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Core.Deconstruction;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Pipelines;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Import;

namespace PdfEditorApp.Services;

public interface IPdfImportService
{
    Task<PdfDocumentModel> ImportPdfAsync(string filePath, string? password = null);
    Task<PdfDocumentModel> ImportPdfFromBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null);
    Task<PdfDocumentModel> ImportPdfBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null);
}

/// <summary>
/// Professional Multi-Format Document Import and Deconstruction Engine.
/// Parses PDF, raster images, markdown, and text files into an editable, multi-layered PdfDocumentModel
/// with pluggable format discovery and Waterfall pipeline interception.
/// </summary>
public class PdfImportService : IPdfImportService
{
    private readonly IDocumentImporterRegistry _importerRegistry;
    private readonly IFryPluginContext? _pluginContext;

    public PdfImportService(IDocumentImporterRegistry? importerRegistry = null, IFryPluginContext? pluginContext = null)
    {
        _importerRegistry = importerRegistry ?? new DocumentImporterRegistry();
        _pluginContext = pluginContext;
    }

    public Task<PdfDocumentModel> ImportPdfBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null)
        => ImportPdfFromBytesAsync(pdfBytes, title, password);

    public async Task<PdfDocumentModel> ImportPdfAsync(string filePath, string? password = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Document file not found: {filePath}");

        byte[] bytes = await File.ReadAllBytesAsync(filePath);
        string title = Path.GetFileName(filePath);
        return await ImportDocumentInternalAsync(bytes, filePath, title, password);
    }

    public async Task<PdfDocumentModel> ImportPdfFromBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null)
    {
        return await ImportDocumentInternalAsync(pdfBytes, title, title, password);
    }

    private async Task<PdfDocumentModel> ImportDocumentInternalAsync(byte[] bytes, string pathOrExtension, string title, string? password)
    {
        using var stream = new MemoryStream(bytes);
        var pipelineContext = new PdfImportPipelineContext(stream, title, password);

        if (_pluginContext != null)
        {
            await _pluginContext.ExecuteWaterfallAsync("document:import", pipelineContext, async () =>
            {
                pipelineContext.Document = await ExecuteImportCoreAsync(stream, pathOrExtension, title, password);
            });
        }
        else
        {
            pipelineContext.Document = await ExecuteImportCoreAsync(stream, pathOrExtension, title, password);
        }

        return pipelineContext.Document ?? throw new InvalidOperationException($"Failed to import document: {title}");
    }

    private async Task<PdfDocumentModel> ExecuteImportCoreAsync(Stream stream, string pathOrExtension, string title, string? password)
    {
        var importer = _importerRegistry.FindImporter(pathOrExtension);
        if (importer != null)
        {
            stream.Position = 0;
            return await importer.ImportAsync(stream, title, password);
        }

        // Direct fallback to PDF deconstruction engine
        stream.Position = 0;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return await Task.Run(() => PdfDeconstructionEngine.Deconstruct(ms.ToArray(), title, password));
    }
}
