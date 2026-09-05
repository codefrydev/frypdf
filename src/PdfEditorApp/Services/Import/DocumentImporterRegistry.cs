using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Deconstruction;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Import;

public class DocumentImporterRegistry : IDocumentImporterRegistry
{
    private readonly ConcurrentDictionary<string, IDocumentImporter> _importers = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public DocumentImporterRegistry()
    {
        RegisterBuiltInImporters();
    }

    private void RegisterBuiltInImporters()
    {
        RegisterImporter(new PdfDeconstructionImporter());
        RegisterImporter(new ImageDocumentImporter());
        RegisterImporter(new PlainTextDocumentImporter());
    }

    public IDisposable RegisterImporter(IDocumentImporter importer)
    {
        ArgumentNullException.ThrowIfNull(importer);
        _importers[importer.ImporterId] = importer;
        RegistryChanged?.Invoke();

        return new DisposableAction(() =>
        {
            _importers.TryRemove(importer.ImporterId, out _);
            RegistryChanged?.Invoke();
        });
    }

    public IDocumentImporter? FindImporter(string filePathOrExtension)
    {
        if (string.IsNullOrWhiteSpace(filePathOrExtension)) return null;

        var ext = Path.GetExtension(filePathOrExtension);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = filePathOrExtension.StartsWith('.') ? filePathOrExtension : "." + filePathOrExtension;
        }

        return _importers.Values
            .Where(i => i.SupportedExtensions.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(i => i.Priority)
            .FirstOrDefault();
    }

    public IDocumentImporter? GetImporter(string importerId)
    {
        if (string.IsNullOrWhiteSpace(importerId)) return null;
        return _importers.GetValueOrDefault(importerId);
    }

    public IReadOnlyList<IDocumentImporter> GetAllImporters()
    {
        return _importers.Values.OrderByDescending(i => i.Priority).ToList();
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        private Action? _action = action;
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }
}

/// <summary>
/// Default native PDF deconstruction engine importer using UglyToad.PdfPig + Skia.
/// </summary>
public class PdfDeconstructionImporter : IDocumentImporter
{
    public string ImporterId => "frypdf.importer.pdfpig";
    public string DisplayName => "Adobe PDF Deconstruction Engine";
    public IReadOnlyList<string> SupportedExtensions => new[] { ".pdf" };
    public int Priority => 100;

    public async Task<PdfDocumentModel> ImportAsync(Stream stream, string fileName, string? password = null, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        return await Task.Run(() => PdfDeconstructionEngine.Deconstruct(bytes, fileName, password), ct);
    }
}

/// <summary>
/// Image importer converting raster images into editable full-bleed PDF pages.
/// </summary>
public class ImageDocumentImporter : IDocumentImporter
{
    public string ImporterId => "frypdf.importer.image";
    public string DisplayName => "Raster Image Importer";
    public IReadOnlyList<string> SupportedExtensions => new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tiff" };
    public int Priority => 50;

    public async Task<PdfDocumentModel> ImportAsync(Stream stream, string fileName, string? password = null, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var doc = new PdfDocumentModel
        {
            Title = Path.GetFileNameWithoutExtension(fileName),
            Author = "FryPDF Image Importer"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Width = 595.28,  // Standard A4 portrait
            Height = 841.89
        };

        var imgElement = new PdfImageElement
        {
            X = 20,
            Y = 20,
            Width = 555.28,
            Height = 801.89,
            ImageData = bytes,
            ZIndex = 100
        };

        page.Elements.Add(imgElement);
        doc.Pages.Add(page);
        return doc;
    }
}

/// <summary>
/// Plain text / Markdown document importer.
/// </summary>
public class PlainTextDocumentImporter : IDocumentImporter
{
    public string ImporterId => "frypdf.importer.text";
    public string DisplayName => "Text & Markdown Importer";
    public IReadOnlyList<string> SupportedExtensions => new[] { ".txt", ".md" };
    public int Priority => 50;

    public async Task<PdfDocumentModel> ImportAsync(Stream stream, string fileName, string? password = null, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync(ct);

        var doc = new PdfDocumentModel
        {
            Title = Path.GetFileNameWithoutExtension(fileName),
            Author = "FryPDF Text Importer"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Width = 595.28,
            Height = 841.89
        };

        var textElement = new PdfTextElement
        {
            X = 50,
            Y = 50,
            Width = 495.28,
            Height = 741.89,
            Text = text,
            FontSize = 12,
            FontFamily = "Inter",
            ZIndex = 1000
        };

        page.Elements.Add(textElement);
        doc.Pages.Add(page);
        return doc;
    }
}
