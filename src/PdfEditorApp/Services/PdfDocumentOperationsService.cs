using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Intelligence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        IPdfWorkflowEngine workflowEngine,
        PdfEditorApp.Core.Plugins.IFryPluginContext? pluginContext = null)
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
        _pluginContext = pluginContext;
    }

    private readonly PdfEditorApp.Core.Plugins.IFryPluginContext? _pluginContext;

    public async Task<ToolExecutionResult> ExecuteToolAsync(PdfToolId toolId, object options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (_pluginContext != null)
        {
            var pipelineContext = new PdfEditorApp.Core.Plugins.Pipelines.PdfToolExecutionPipelineContext(toolId.ToString(), options, progress, ct);
            await _pluginContext.ExecuteWaterfallAsync("tool:execute", pipelineContext, async () =>
            {
                pipelineContext.Result = await ExecuteToolCoreAsync(toolId, pipelineContext.Options, progress, ct);
            });

            return pipelineContext.Result as ToolExecutionResult ?? new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = "Tool execution pipeline did not produce a result."
            };
        }

        return await ExecuteToolCoreAsync(toolId, options, progress, ct);
    }

    private async Task<ToolExecutionResult> ExecuteToolCoreAsync(PdfToolId toolId, object options, IProgress<double>? progress = null, CancellationToken ct = default)
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
                {
                    if (!File.Exists(compOpts.DocumentAPath) || !File.Exists(compOpts.DocumentBPath))
                        return new ToolExecutionResult { Success = false, ErrorMessage = "One or both comparison documents do not exist." };

                    var importService = new PdfImportService();
                    var compareService = new DocumentCompareService();

                    var docA = await importService.ImportPdfAsync(compOpts.DocumentAPath);
                    ct.ThrowIfCancellationRequested();
                    progress?.Report(40.0);
                    var docB = await importService.ImportPdfAsync(compOpts.DocumentBPath);
                    ct.ThrowIfCancellationRequested();
                    progress?.Report(70.0);

                    var report = await compareService.CompareDocumentsAsync(docA, docB, ct);

                    string outDir = Path.GetDirectoryName(compOpts.DocumentBPath) ?? Path.GetTempPath();
                    string reportPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(compOpts.DocumentBPath)}_comparison_report.txt");

                    var sb = new StringBuilder();
                    sb.AppendLine("Document Comparison Report");
                    sb.AppendLine($"Base document:     {report.BaseDocumentTitle} ({report.BasePageCount} pages)");
                    sb.AppendLine($"Compared document: {report.ComparedDocumentTitle} ({report.ComparedPageCount} pages)");
                    sb.AppendLine($"Generated:         {report.ComparisonTimestamp:u}");
                    sb.AppendLine();
                    sb.AppendLine($"Total differences: {report.TotalDifferencesCount} (Added: {report.AdditionsCount}, Removed: {report.DeletionsCount}, Modified: {report.ModificationsCount})");
                    sb.AppendLine();
                    foreach (var diff in report.Differences)
                    {
                        sb.AppendLine($"[Page {diff.PageNumber}] {diff.DiffType}: {diff.Description}");
                        if (!string.IsNullOrEmpty(diff.OldValue) || !string.IsNullOrEmpty(diff.NewValue))
                        {
                            sb.AppendLine($"    - Was: {diff.OldValue}");
                            sb.AppendLine($"    + Now: {diff.NewValue}");
                        }
                    }

                    await File.WriteAllTextAsync(reportPath, sb.ToString(), ct);
                    progress?.Report(100.0);

                    return new ToolExecutionResult
                    {
                        Success = true,
                        OutputFilePath = reportPath,
                        OutputFiles = new List<string> { reportPath },
                        Message = report.TotalDifferencesCount == 0
                            ? $"No differences found between '{Path.GetFileName(compOpts.DocumentAPath)}' and '{Path.GetFileName(compOpts.DocumentBPath)}'."
                            : $"Found {report.TotalDifferencesCount} difference(s) between the two documents ({report.AdditionsCount} added, {report.DeletionsCount} removed, {report.ModificationsCount} modified). Report saved to {Path.GetFileName(reportPath)}.",
                        ExtraData = new Dictionary<string, object> { ["ComparisonReport"] = report }
                    };
                }

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
            // Let cancellation propagate so PdfToolViewModelBase's dedicated
            // OperationCanceledException handler can show a neutral "cancelled" state
            // instead of the generic failure path.
            throw;
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
