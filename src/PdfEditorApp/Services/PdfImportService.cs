using System;
using System.IO;
using System.Threading.Tasks;
using PdfEditorApp.Core.Deconstruction;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public interface IPdfImportService
{
    Task<PdfDocumentModel> ImportPdfAsync(string filePath, string? password = null);
    Task<PdfDocumentModel> ImportPdfFromBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null);
    Task<PdfDocumentModel> ImportPdfBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null);
}

/// <summary>
/// Professional PDF Import and Deconstruction Engine (Adobe Acrobat & Wondershare PDFelement Architecture).
/// Parses any standard or complex PDF file into an editable, multi-layered PdfDocumentModel
/// with intelligent paragraph clustering and zero duplicate raster ghosting.
/// </summary>
public class PdfImportService : IPdfImportService
{
    public Task<PdfDocumentModel> ImportPdfBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null)
        => ImportPdfFromBytesAsync(pdfBytes, title, password);

    public async Task<PdfDocumentModel> ImportPdfAsync(string filePath, string? password = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"PDF file not found: {filePath}");

        byte[] bytes = await File.ReadAllBytesAsync(filePath);
        string title = Path.GetFileName(filePath);
        return await ImportPdfFromBytesAsync(bytes, title, password);
    }

    public async Task<PdfDocumentModel> ImportPdfFromBytesAsync(byte[] pdfBytes, string title = "Imported_Document.pdf", string? password = null)
    {
        return await Task.Run(() =>
        {
            return PdfDeconstructionEngine.Deconstruct(pdfBytes, title, password);
        });
    }
}
