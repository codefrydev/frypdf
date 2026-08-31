using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;

namespace PdfEditorApp.Core.Data;

public interface IBatchPdfGenerator
{
    /// <summary>
    /// Executes batch generation across all records in the data matrix, populating the document template and
    /// outputting separate PDF files, a single merged multi-page PDF, or a compressed ZIP archive.
    /// </summary>
    Task<BatchGenerationResult> GenerateBatchAsync(
        PdfDocumentModel template,
        DataMatrix matrix,
        IReadOnlyList<FieldMappingItem>? mappings,
        BatchGenerationConfig config,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}
