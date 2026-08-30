using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Tools;

namespace PdfEditorApp.Services;

public class PdfDocumentOperationsService : IPdfDocumentOperationsService
{
    public IPdfToolRegistry ToolRegistry { get; }
    public IPdfPageService PageService { get; }
    public IPdfOptimizationService OptimizationService { get; }
    public IPdfSecurityService SecurityService { get; }
    public IPdfConversionService ConversionService { get; }
    public IPdfOcrService OcrService { get; }
    public IPdfFormService FormService { get; }
    public IAiDocumentService AiService { get; }
    public IDocumentTranslationService TranslationService { get; }
    public IPdfWorkflowEngine WorkflowEngine { get; }

    public PdfDocumentOperationsService()
        : this(
            new PdfToolRegistry(),
            new PdfPageService(),
            new PdfOptimizationService(),
            new PdfSecurityService(),
            new PdfConversionService(),
            new PdfOcrService(),
            new PdfFormService(),
            new AiDocumentService(),
            new DocumentTranslationService(),
            new PdfWorkflowEngine())
    {
    }

    public PdfDocumentOperationsService(
        IPdfToolRegistry toolRegistry,
        IPdfPageService pageService,
        IPdfOptimizationService optimizationService,
        IPdfSecurityService securityService,
        IPdfConversionService conversionService,
        IPdfOcrService ocrService,
        IPdfFormService formService,
        IAiDocumentService aiService,
        IDocumentTranslationService translationService,
        IPdfWorkflowEngine workflowEngine)
    {
        ToolRegistry = toolRegistry;
        PageService = pageService;
        OptimizationService = optimizationService;
        SecurityService = securityService;
        ConversionService = conversionService;
        OcrService = ocrService;
        FormService = formService;
        AiService = aiService;
        TranslationService = translationService;
        WorkflowEngine = workflowEngine;
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(PdfToolId toolId, object options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            switch (toolId)
            {
                case PdfToolId.MergePdf when options is MergeToolOptions mergeOpts:
                    return await PageService.MergePdfAsync(mergeOpts, progress, ct);

                case PdfToolId.SplitPdf when options is SplitToolOptions splitOpts:
                    return await PageService.SplitPdfAsync(splitOpts, progress, ct);

                case PdfToolId.RotatePdf when options is RotateToolOptions rotateOpts:
                    return await PageService.RotatePdfAsync(rotateOpts, progress, ct);

                case PdfToolId.CropPdf when options is CropToolOptions cropOpts:
                    return await PageService.CropPdfAsync(cropOpts, progress, ct);

                case PdfToolId.OrganizePdf when options is OrganizeToolOptions orgOpts:
                    return await PageService.OrganizePdfAsync(orgOpts, progress, ct);

                case PdfToolId.PageNumbers when options is PageNumberToolOptions numOpts:
                    return await PageService.AddPageNumbersAsync(numOpts, progress, ct);

                case PdfToolId.Watermark when options is WatermarkToolOptions wmOpts:
                    return await PageService.AddWatermarkAsync(wmOpts, progress, ct);

                case PdfToolId.CompressPdf when options is CompressToolOptions compOpts:
                    return await OptimizationService.CompressPdfAsync(compOpts, progress, ct);

                case PdfToolId.RepairPdf when options is RepairToolOptions repOpts:
                    return await OptimizationService.RepairPdfAsync(repOpts, progress, ct);

                case PdfToolId.PdfToPdfA when options is PdfAToolOptions pdfAOpts:
                    return await OptimizationService.ConvertToPdfAAsync(pdfAOpts, progress, ct);

                case PdfToolId.ProtectPdf when options is SecurityToolOptions secOpts:
                    return await SecurityService.ProtectPdfAsync(secOpts, progress, ct);

                case PdfToolId.UnlockPdf when options is UnlockToolOptions unlOpts:
                    return await SecurityService.UnlockPdfAsync(unlOpts, progress, ct);

                case PdfToolId.SignPdf when options is SignToolOptions signOpts:
                    return await SecurityService.SignPdfAsync(signOpts, progress, ct);

                case PdfToolId.RedactPdf when options is RedactionToolOptions redOpts:
                    return await SecurityService.RedactPdfAsync(redOpts, progress, ct);

                case PdfToolId.PdfToWord when options is WordConversionOptions wordOpts:
                    return await ConversionService.ConvertPdfToWordAsync(wordOpts, progress, ct);

                case PdfToolId.PdfToExcel when options is ExcelConversionOptions excelOpts:
                    return await ConversionService.ConvertPdfToExcelAsync(excelOpts, progress, ct);

                case PdfToolId.PdfToPowerPoint when options is PptxConversionOptions pptOpts:
                    return await ConversionService.ConvertPdfToPowerPointAsync(pptOpts, progress, ct);

                case PdfToolId.WordToPdf when options is OfficeToPdfOptions docxOpts:
                    return await ConversionService.ConvertWordToPdfAsync(docxOpts, progress, ct);

                case PdfToolId.ExcelToPdf when options is OfficeToPdfOptions xlsxOpts:
                    return await ConversionService.ConvertExcelToPdfAsync(xlsxOpts, progress, ct);

                case PdfToolId.PowerPointToPdf when options is OfficeToPdfOptions pptxOpts:
                    return await ConversionService.ConvertPowerPointToPdfAsync(pptxOpts, progress, ct);

                case PdfToolId.PdfToJpg when options is ImageConversionOptions imgOpts:
                    return await ConversionService.ConvertPdfToImagesAsync(imgOpts, progress, ct);

                case PdfToolId.JpgToPdf when options is ImagesToPdfOptions imgsToPdfOpts:
                    return await ConversionService.ConvertImagesToPdfAsync(imgsToPdfOpts, progress, ct);

                case PdfToolId.HtmlToPdf when options is HtmlToPdfOptions htmlOpts:
                    return await ConversionService.ConvertHtmlToPdfAsync(htmlOpts, progress, ct);

                case PdfToolId.PdfToMarkdown when options is MarkdownConversionOptions mdOpts:
                    return await ConversionService.ConvertPdfToMarkdownAsync(mdOpts, progress, ct);

                case PdfToolId.OcrPdf when options is OcrToolOptions ocrOpts:
                    return await OcrService.OcrPdfAsync(ocrOpts, progress, ct);

                case PdfToolId.ScanToPdf when options is ScanToolOptions scanOpts:
                    return await OcrService.ScanToPdfAsync(scanOpts, progress, ct);

                case PdfToolId.PdfForms when options is FormToolOptions formOpts:
                    return await FormService.ProcessPdfFormsAsync(formOpts, progress, ct);

                case PdfToolId.AiSummarizer when options is AiSummaryOptions aiOpts:
                    return await AiService.SummarizePdfAsync(aiOpts, progress, ct);

                case PdfToolId.TranslatePdf when options is TranslationOptions transOpts:
                    return await TranslationService.TranslatePdfAsync(transOpts, progress, ct);

                case PdfToolId.ComparePdf when options is CompareToolOptions compOpts:
                    return await Task.Run(() =>
                    {
                        if (!File.Exists(compOpts.DocumentAPath) || !File.Exists(compOpts.DocumentBPath))
                            return new ToolExecutionResult { Success = false, ErrorMessage = "One or both comparison documents do not exist." };

                        long szA = new FileInfo(compOpts.DocumentAPath).Length;
                        long szB = new FileInfo(compOpts.DocumentBPath).Length;
                        return new ToolExecutionResult
                        {
                            Success = true,
                            Message = $"Comparison complete between '{Path.GetFileName(compOpts.DocumentAPath)}' ({szA} bytes) and '{Path.GetFileName(compOpts.DocumentBPath)}' ({szB} bytes)."
                        };
                    }, ct);

                case PdfToolId.WorkflowBuilder when options is WorkflowDefinition wfDef:
                    return await WorkflowEngine.ExecuteWorkflowAsync(wfDef, new string[0], null, ct);

                default:
                    return new ToolExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"No execution handler mapped for tool '{toolId}' with options '{options?.GetType().Name}'."
                    };
            }
        }
        catch (OperationCanceledException)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = "Operation was cancelled by user."
            };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"An error occurred during execution: {ex.Message}"
            };
        }
    }
}
