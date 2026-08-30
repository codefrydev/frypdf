using System.Threading.Tasks;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public interface IPdfExportService
{
    byte[] GeneratePdfBytes(PdfDocumentModel model);
    Task<byte[]> ExportToBytesAsync(PdfDocumentModel model);
    Task ExportToFileAsync(PdfDocumentModel model, string filePath);
}
