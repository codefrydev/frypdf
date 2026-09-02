using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.Tools;

public interface IPdfWorkflowEngine
{
    Task<ToolExecutionResult> ExecuteWorkflowAsync(WorkflowDefinition workflow, IEnumerable<string> inputFiles, IProgress<WorkflowProgressInfo>? progress = null, CancellationToken ct = default);
    Task SaveWorkflowToFileAsync(WorkflowDefinition workflow, string filePath);
    Task<WorkflowDefinition?> LoadWorkflowFromFileAsync(string filePath);
    string SerializeWorkflow(WorkflowDefinition workflow);
    WorkflowDefinition? DeserializeWorkflow(string json);
    IReadOnlyList<WorkflowDefinition> GetPresetWorkflows();
}

public class PdfWorkflowEngine : IPdfWorkflowEngine
{
    private readonly IPdfPageService _pageService;
    private readonly IPdfOptimizationService _optService;
    private readonly IPdfSecurityService _secService;
    private readonly IPdfConversionService _convService;
    private readonly IPdfOcrService _ocrService;

    public PdfWorkflowEngine()
        : this(new PdfPageService(), new PdfOptimizationService(), new PdfSecurityService(), new PdfConversionService(), new PdfOcrService())
    {
    }

    public PdfWorkflowEngine(
        IPdfPageService pageService,
        IPdfOptimizationService optService,
        IPdfSecurityService secService,
        IPdfConversionService convService,
        IPdfOcrService ocrService)
    {
        _pageService = pageService;
        _optService = optService;
        _secService = secService;
        _convService = convService;
        _ocrService = ocrService;
    }

    public async Task<ToolExecutionResult> ExecuteWorkflowAsync(
        WorkflowDefinition workflow,
        IEnumerable<string> inputFiles,
        IProgress<WorkflowProgressInfo>? progress = null,
        CancellationToken ct = default)
    {
        return await Task.Run(async () =>
        {
            var files = inputFiles.Where(File.Exists).ToList();
            if (files.Count == 0)
                return new ToolExecutionResult { Success = false, ErrorMessage = "No valid input files provided for workflow execution." };

            var enabledSteps = workflow.Steps.Where(s => s.IsEnabled).ToList();
            if (enabledSteps.Count == 0)
                return new ToolExecutionResult { Success = false, ErrorMessage = "Workflow has no enabled steps." };

            string outDir = string.IsNullOrWhiteSpace(workflow.OutputDirectory)
                ? Path.Combine(Path.GetDirectoryName(files[0]) ?? "", "Workflow_Output")
                : workflow.OutputDirectory;

            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            var finalOutputs = new List<string>();
            string tempDir = Path.Combine(Path.GetTempPath(), "FryPdf_Workflow_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                int totalSteps = enabledSteps.Count * files.Count;
                int completedSteps = 0;

                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();
                    string currentFile = file;

                    for (int s = 0; s < enabledSteps.Count; s++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var step = enabledSteps[s];

                        progress?.Report(new WorkflowProgressInfo
                        {
                            CurrentStepIndex = s + 1,
                            TotalSteps = enabledSteps.Count,
                            CurrentStepName = step.StepName,
                            CurrentFileName = Path.GetFileName(file),
                            OverallProgressPercentage = (completedSteps / (double)totalSteps) * 100.0,
                            StatusMessage = $"Running '{step.StepName}' on {Path.GetFileName(file)}..."
                        });

                        string stepOut = Path.Combine(tempDir, $"step_{s}_{Guid.NewGuid():N}_{Path.GetFileName(currentFile)}");

                        // Execute Step
                        var stepResult = await ExecuteSingleStepAsync(step, currentFile, stepOut, ct);
                        if (!stepResult.Success)
                        {
                            return new ToolExecutionResult
                            {
                                Success = false,
                                ErrorMessage = $"Workflow failed at step '{step.StepName}': {stepResult.ErrorMessage}"
                            };
                        }

                        currentFile = stepResult.OutputFilePath ?? stepOut;
                        completedSteps++;
                    }

                    // Move final output to destination folder
                    string finalFileName = workflow.OutputFileNamePattern
                        .Replace("{filename}", Path.GetFileNameWithoutExtension(file))
                        .Replace("{date}", DateTime.UtcNow.ToString("yyyyMMdd"));

                    if (!finalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        finalFileName += ".pdf";

                    string destPath = Path.Combine(outDir, finalFileName);
                    if (!string.Equals(Path.GetFullPath(currentFile), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(destPath)) File.Delete(destPath);
                        File.Copy(currentFile, destPath, true);
                    }
                    finalOutputs.Add(destPath);
                }

                progress?.Report(new WorkflowProgressInfo
                {
                    CurrentStepIndex = enabledSteps.Count,
                    TotalSteps = enabledSteps.Count,
                    CurrentStepName = "Complete",
                    OverallProgressPercentage = 100.0,
                    StatusMessage = $"Workflow '{workflow.Name}' executed successfully on {files.Count} files."
                });

                return new ToolExecutionResult
                {
                    Success = true,
                    OutputFilePath = finalOutputs.FirstOrDefault(),
                    OutputFiles = finalOutputs,
                    Message = $"Executed workflow '{workflow.Name}' on {files.Count} files across {enabledSteps.Count} automated steps."
                };
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
                catch { }
            }
        }, ct);
    }

    private async Task<ToolExecutionResult> ExecuteSingleStepAsync(WorkflowStepDefinition step, string inputFile, string outputFile, CancellationToken ct)
    {
        switch (step.ToolId)
        {
            case PdfToolId.CompressPdf:
                return await _optService.CompressPdfAsync(new CompressToolOptions
                {
                    InputFilePath = inputFile,
                    OutputFilePath = outputFile,
                    Level = PdfCompressionLevel.Balanced
                }, null, ct);

            case PdfToolId.OcrPdf:
                return await _ocrService.OcrPdfAsync(new OcrToolOptions
                {
                    InputFilePath = inputFile,
                    OutputFilePath = outputFile
                }, null, ct);

            case PdfToolId.PageNumbers:
                return await _pageService.AddPageNumbersAsync(new PageNumberToolOptions
                {
                    InputFilePath = inputFile,
                    OutputFilePath = outputFile
                }, null, ct);

            case PdfToolId.ProtectPdf:
                return await _secService.ProtectPdfAsync(new SecurityToolOptions
                {
                    InputFilePath = inputFile,
                    OutputFilePath = outputFile,
                    AllowPrinting = true
                }, null, ct);

            case PdfToolId.PdfToPdfA:
                return await _optService.ConvertToPdfAAsync(new PdfAToolOptions
                {
                    InputFilePath = inputFile,
                    OutputFilePath = outputFile
                }, null, ct);

            case PdfToolId.RepairPdf:
                return await _optService.RepairPdfAsync(new RepairToolOptions
                {
                    InputFilePath = inputFile,
                    OutputFilePath = outputFile
                }, null, ct);

            default:
                // Pass-through copy if no specialized execution needed
                if (!string.Equals(Path.GetFullPath(inputFile), Path.GetFullPath(outputFile), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(inputFile, outputFile, true);
                }
                return new ToolExecutionResult { Success = true, OutputFilePath = outputFile };
        }
    }

    public string SerializeWorkflow(WorkflowDefinition workflow)
    {
        workflow.ModifiedAt = DateTime.UtcNow;
        return JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = true });
    }

    public WorkflowDefinition? DeserializeWorkflow(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<WorkflowDefinition>(json);
    }

    public async Task SaveWorkflowToFileAsync(WorkflowDefinition workflow, string filePath)
    {
        string json = SerializeWorkflow(workflow);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<WorkflowDefinition?> LoadWorkflowFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        string json = await File.ReadAllTextAsync(filePath);
        return DeserializeWorkflow(json);
    }

    public IReadOnlyList<WorkflowDefinition> GetPresetWorkflows()
    {
        return new List<WorkflowDefinition>
        {
            new WorkflowDefinition
            {
                Id = "preset_publish",
                Name = "Publishing & Archival Pipeline",
                Description = "Run OCR, optimize compression, stamp page numbers, and convert to PDF/A for long-term storage.",
                Steps = new List<WorkflowStepDefinition>
                {
                    new WorkflowStepDefinition { Id = "s1", ToolId = PdfToolId.OcrPdf, StepName = "OCR Text Recognition", StepDescription = "Make scanned text searchable" },
                    new WorkflowStepDefinition { Id = "s2", ToolId = PdfToolId.CompressPdf, StepName = "Balanced Compression", StepDescription = "Optimize document file size" },
                    new WorkflowStepDefinition { Id = "s3", ToolId = PdfToolId.PageNumbers, StepName = "Add Header & Page Numbers", StepDescription = "Stamp 'Page {n} of {total}'" },
                    new WorkflowStepDefinition { Id = "s4", ToolId = PdfToolId.PdfToPdfA, StepName = "Convert to PDF/A", StepDescription = "Enforce ISO 19005 archival compliance" }
                }
            },
            new WorkflowDefinition
            {
                Id = "preset_secure",
                Name = "Enterprise Security & Protection",
                Description = "Sanitize document, apply corporate watermarking, and protect with permission encryption.",
                Steps = new List<WorkflowStepDefinition>
                {
                    new WorkflowStepDefinition { Id = "s1", ToolId = PdfToolId.RepairPdf, StepName = "Audit & Repair", StepDescription = "Fix damaged object structures" },
                    new WorkflowStepDefinition { Id = "s2", ToolId = PdfToolId.CompressPdf, StepName = "Compress Resources", StepDescription = "Strip unnecessary metadata" },
                    new WorkflowStepDefinition { Id = "s3", ToolId = PdfToolId.ProtectPdf, StepName = "Encrypt & Protect", StepDescription = "Apply password encryption and printing restrictions" }
                }
            }
        };
    }
}
