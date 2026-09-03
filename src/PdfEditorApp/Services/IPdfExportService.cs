using System;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public interface IPdfExportService
{
    byte[] GeneratePdfBytes(PdfDocumentModel model);
    Task<byte[]> ExportToBytesAsync(PdfDocumentModel model, IProgress<double>? progress = null, CancellationToken ct = default);
    Task ExportToFileAsync(PdfDocumentModel model, string filePath, IProgress<double>? progress = null, CancellationToken ct = default);
}
