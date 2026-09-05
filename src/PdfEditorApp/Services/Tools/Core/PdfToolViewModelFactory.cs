using PdfEditorApp.ViewModels.Tools.Core;
using PdfEditorApp.ViewModels.Tools.Organize;
using PdfEditorApp.ViewModels.Tools.Security;
using PdfEditorApp.ViewModels.Tools.Conversion;
using PdfEditorApp.ViewModels.Tools.Intelligence;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Intelligence;
using System;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.Tools;

using Microsoft.Extensions.DependencyInjection;

namespace PdfEditorApp.Services.Tools.Core;

/// <summary>
/// Default implementation of IPdfToolViewModelFactory.
/// Instantiates typed, dedicated ViewModels for each PDF tool, taking advantage of Microsoft.Extensions.DependencyInjection when available.
/// </summary>
public class PdfToolViewModelFactory : IPdfToolViewModelFactory
{
    private readonly IPdfDocumentOperationsService _operationsService;
    private readonly IPdfToolRegistry _toolRegistry;
    private readonly IServiceProvider? _serviceProvider;

    public PdfToolViewModelFactory(
        IPdfDocumentOperationsService operationsService,
        IPdfToolRegistry toolRegistry,
        IServiceProvider? serviceProvider = null)
    {
        _operationsService = operationsService;
        _toolRegistry = toolRegistry;
        _serviceProvider = serviceProvider;
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
        if (toolDef.ViewModelFactory != null && _serviceProvider != null)
        {
            try
            {
                if (toolDef.ViewModelFactory(_serviceProvider) is PdfToolViewModelBase vmFromFactory)
                {
                    return vmFromFactory;
                }
            }
            catch
            {
                // Fall back to built-in factory logic
            }
        }

        if (_serviceProvider != null)
        {
            try
            {
                Type? vmType = toolDef.Id switch
                {
                    PdfToolId.MergePdf => typeof(MergePdfToolViewModel),
                    PdfToolId.SplitPdf => typeof(SplitPdfToolViewModel),
                    PdfToolId.CompressPdf => typeof(CompressPdfToolViewModel),
                    PdfToolId.PdfToWord => typeof(PdfToWordToolViewModel),
                    PdfToolId.PdfToPowerPoint => typeof(PdfToPowerPointToolViewModel),
                    PdfToolId.PdfToExcel => typeof(PdfToExcelToolViewModel),
                    PdfToolId.WordToPdf => typeof(WordToPdfToolViewModel),
                    PdfToolId.PowerPointToPdf => typeof(PowerPointToPdfToolViewModel),
                    PdfToolId.ExcelToPdf => typeof(ExcelToPdfToolViewModel),
                    PdfToolId.EditPdf => typeof(EditPdfToolViewModel),
                    PdfToolId.PdfToJpg => typeof(PdfToJpgToolViewModel),
                    PdfToolId.JpgToPdf => typeof(JpgToPdfToolViewModel),
                    PdfToolId.SignPdf => typeof(SignPdfToolViewModel),
                    PdfToolId.Watermark => typeof(WatermarkToolViewModel),
                    PdfToolId.RotatePdf => typeof(RotatePdfToolViewModel),
                    PdfToolId.HtmlToPdf => typeof(HtmlToPdfToolViewModel),
                    PdfToolId.UnlockPdf => typeof(UnlockPdfToolViewModel),
                    PdfToolId.ProtectPdf => typeof(ProtectPdfToolViewModel),
                    PdfToolId.OrganizePdf => typeof(OrganizePdfToolViewModel),
                    PdfToolId.PdfToPdfA => typeof(PdfToPdfAToolViewModel),
                    PdfToolId.RepairPdf => typeof(RepairPdfToolViewModel),
                    PdfToolId.PageNumbers => typeof(PageNumbersToolViewModel),
                    PdfToolId.ScanToPdf => typeof(ScanToPdfToolViewModel),
                    PdfToolId.OcrPdf => typeof(OcrPdfToolViewModel),
                    PdfToolId.ComparePdf => typeof(ComparePdfToolViewModel),
                    PdfToolId.RedactPdf => typeof(RedactPdfToolViewModel),
                    PdfToolId.CropPdf => typeof(CropPdfToolViewModel),
                    PdfToolId.PdfForms => typeof(PdfFormsToolViewModel),
                    PdfToolId.AiSummarizer => typeof(AiSummarizerToolViewModel),
                    PdfToolId.TranslatePdf => typeof(TranslatePdfToolViewModel),
                    PdfToolId.PdfToMarkdown => typeof(PdfToMarkdownToolViewModel),
                    _ => typeof(EditPdfToolViewModel)
                };

                if (ActivatorUtilities.CreateInstance(_serviceProvider, vmType, toolDef) is PdfToolViewModelBase vm)
                {
                    return vm;
                }
            }
            catch
            {
                // Fall back to direct instantiation
            }
        }

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
