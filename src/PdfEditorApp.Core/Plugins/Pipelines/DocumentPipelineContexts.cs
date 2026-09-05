using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Core.Plugins.Pipelines;

/// <summary>
/// Execution context for document export waterfall pipelines ("document:export").
/// Allows plugins to intercept document models before generation, and inspect/transform
/// the generated binary PDF bytes afterwards.
/// </summary>
public class PdfExportPipelineContext
{
    /// <summary>The document model to be exported.</summary>
    public PdfDocumentModel Document { get; set; }

    /// <summary>The compiled output PDF bytes. Can be transformed or substituted by pipeline filters.</summary>
    public byte[]? ResultPdfBytes { get; set; }

    /// <summary>Optional progress reporting channel.</summary>
    public IProgress<double>? Progress { get; init; }

    /// <summary>Cancellation token for aborting generation.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Dynamic properties and options passed to pipeline filters.</summary>
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public PdfExportPipelineContext(PdfDocumentModel document, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Progress = progress;
        CancellationToken = cancellationToken;
    }
}

/// <summary>
/// Execution context for document import waterfall pipelines ("document:import").
/// Allows plugins to preprocess input streams, parse proprietary formats, or enrich deconstructed models.
/// </summary>
public class PdfImportPipelineContext
{
    /// <summary>The raw stream of the document being imported.</summary>
    public Stream InputStream { get; init; }

    /// <summary>The resulting deconstructed document model.</summary>
    public PdfDocumentModel? Document { get; set; }

    /// <summary>Cancellation token for aborting import.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Dynamic properties and options passed to pipeline handlers.</summary>
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The file name or title of the document being imported.</summary>
    public string FileName { get; init; } = "Document.pdf";

    /// <summary>Optional password provided for decryption.</summary>
    public string? Password { get; init; }

    public PdfImportPipelineContext(Stream inputStream, string fileName = "Document.pdf", string? password = null, CancellationToken cancellationToken = default)
    {
        InputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
        FileName = fileName;
        Password = password;
        CancellationToken = cancellationToken;
    }
}

/// <summary>
/// Execution context for PDF tool execution waterfall pipelines ("tool:execute").
/// Allows plugins to wrap tool invocations with auditing, telemetry, progress reporting, and output post-processing.
/// </summary>
public class PdfToolExecutionPipelineContext
{
    /// <summary>The identifier of the tool being executed.</summary>
    public string ToolId { get; init; }

    /// <summary>Tool options object.</summary>
    public object Options { get; set; }

    /// <summary>Optional progress reporting channel.</summary>
    public IProgress<double>? Progress { get; init; }

    /// <summary>Cancellation token for aborting the tool.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>The result produced by the tool execution.</summary>
    public object? Result { get; set; }

    /// <summary>Dynamic properties and options passed to pipeline filters.</summary>
    public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public PdfToolExecutionPipelineContext(string toolId, object options, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        ToolId = toolId ?? throw new ArgumentNullException(nameof(toolId));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Progress = progress;
        CancellationToken = cancellationToken;
    }
}
