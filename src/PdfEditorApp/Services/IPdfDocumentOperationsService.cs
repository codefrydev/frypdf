using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Tools;

namespace PdfEditorApp.Services;

public interface IPdfDocumentOperationsService
{
    IPdfToolRegistry ToolRegistry { get; }
    IPdfPageService PageService { get; }
    IPdfOptimizationService OptimizationService { get; }
    IPdfSecurityService SecurityService { get; }
    IPdfConversionService ConversionService { get; }
    IPdfOcrService OcrService { get; }
    IPdfFormService FormService { get; }
    IAiDocumentService AiService { get; }
    IDocumentTranslationService TranslationService { get; }
    IPdfWorkflowEngine WorkflowEngine { get; }

    Task<ToolExecutionResult> ExecuteToolAsync(PdfToolId toolId, object options, IProgress<double>? progress = null, CancellationToken ct = default);
}
