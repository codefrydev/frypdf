using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Contract for a document importer plugin that converts raw file streams or bytes
/// into editable, multi-layered <see cref="PdfDocumentModel"/> instances.
/// </summary>
public interface IDocumentImporter
{
    /// <summary>
    /// Unique identifier for this importer, e.g. "frypdf.importer.pdfpig" or "frypdf.importer.image".
    /// </summary>
    string ImporterId { get; }

    /// <summary>
    /// User-facing display name, e.g. "Adobe PDF Deconstruction Engine" or "Raster Image Converter".
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Supported file extensions including leading dot, e.g. [".pdf"] or [".png", ".jpg", ".jpeg", ".webp"].
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Priority order for resolution when multiple importers claim the same extension (higher = higher priority).
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Imports a document stream and returns an editable <see cref="PdfDocumentModel"/>.
    /// </summary>
    Task<PdfDocumentModel> ImportAsync(Stream stream, string fileName, string? password = null, CancellationToken ct = default);
}

/// <summary>
/// Registry for discovering and dispatching document importers contributed by plugins.
/// </summary>
public interface IDocumentImporterRegistry
{
    /// <summary>
    /// Registers an importer into the system.
    /// </summary>
    IDisposable RegisterImporter(IDocumentImporter importer);

    /// <summary>
    /// Finds the highest-priority importer capable of handling the specified file path or extension.
    /// </summary>
    IDocumentImporter? FindImporter(string filePathOrExtension);

    /// <summary>
    /// Retrieves an importer by its unique ID.
    /// </summary>
    IDocumentImporter? GetImporter(string importerId);

    /// <summary>
    /// Gets all registered document importers.
    /// </summary>
    IReadOnlyList<IDocumentImporter> GetAllImporters();

    /// <summary>
    /// Raised whenever an importer is registered or unregistered.
    /// </summary>
    event Action? RegistryChanged;
}
