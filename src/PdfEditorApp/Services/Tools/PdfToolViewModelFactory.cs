using System;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;

namespace PdfEditorApp.Services.Tools;

/// <summary>
/// Default implementation of IPdfToolViewModelFactory.
/// Instantiates typed, dedicated ViewModels for each PDF tool.
/// </summary>
public class PdfToolViewModelFactory : IPdfToolViewModelFactory
{
    private readonly IPdfDocumentOperationsService _operationsService;
    private readonly IPdfToolRegistry _toolRegistry;

    public PdfToolViewModelFactory(IPdfDocumentOperationsService operationsService, IPdfToolRegistry toolRegistry)
    {
        _operationsService = operationsService;
        _toolRegistry = toolRegistry;
    }

    public PdfToolViewModelBase Create(PdfToolId toolId) => CreateToolViewModel(toolId);

    public PdfToolViewModelBase CreateToolViewModel(PdfToolId toolId)
    {
        var toolDef = _toolRegistry.GetTool(toolId) ?? new PdfToolDefinition
        {
            Id = toolId,
            Name = toolId.ToString()
        };

        return CreateToolViewModel(toolDef);
    }

    public PdfToolViewModelBase CreateToolViewModel(PdfToolDefinition toolDef)
    {
        return toolDef.Id switch
        {
            PdfToolId.MergePdf => new MergePdfToolViewModel(_operationsService, toolDef),
            PdfToolId.SplitPdf => new SplitPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.CompressPdf => new CompressPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.PdfToWord => new PdfToWordToolViewModel(_operationsService, toolDef),
            PdfToolId.PdfToPowerPoint => new PdfToPowerPointToolViewModel(_operationsService, toolDef),
            PdfToolId.PdfToExcel => new PdfToExcelToolViewModel(_operationsService, toolDef),
            PdfToolId.WordToPdf => new WordToPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.PowerPointToPdf => new PowerPointToPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.ExcelToPdf => new ExcelToPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.EditPdf => new EditPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.PdfToJpg => new PdfToJpgToolViewModel(_operationsService, toolDef),
            PdfToolId.JpgToPdf => new JpgToPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.SignPdf => new SignPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.Watermark => new WatermarkToolViewModel(_operationsService, toolDef),
            PdfToolId.RotatePdf => new RotatePdfToolViewModel(_operationsService, toolDef),
            PdfToolId.HtmlToPdf => new HtmlToPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.UnlockPdf => new UnlockPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.ProtectPdf => new ProtectPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.OrganizePdf => new OrganizePdfToolViewModel(_operationsService, toolDef),
            PdfToolId.PdfToPdfA => new PdfToPdfAToolViewModel(_operationsService, toolDef),
            PdfToolId.RepairPdf => new RepairPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.PageNumbers => new PageNumbersToolViewModel(_operationsService, toolDef),
            PdfToolId.ScanToPdf => new ScanToPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.OcrPdf => new OcrPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.ComparePdf => new ComparePdfToolViewModel(_operationsService, toolDef),
            PdfToolId.RedactPdf => new RedactPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.CropPdf => new CropPdfToolViewModel(_operationsService, toolDef),
            PdfToolId.PdfForms => new PdfFormsToolViewModel(_operationsService, toolDef),
            PdfToolId.AiSummarizer => new AiSummarizerToolViewModel(_operationsService, toolDef),
            PdfToolId.TranslatePdf => new TranslatePdfToolViewModel(_operationsService, toolDef),
            PdfToolId.PdfToMarkdown => new PdfToMarkdownToolViewModel(_operationsService, toolDef),
            _ => new EditPdfToolViewModel(_operationsService, toolDef)
        };
    }
}
