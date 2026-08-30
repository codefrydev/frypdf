using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Comprehensive tests for all PDF Tool services: page operations, optimization,
/// security, conversion, OCR, forms, AI, translation, workflow engine, and tool registry.
/// </summary>
public class PdfToolsServiceTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Helper: create a minimal valid PDF binary in memory using PdfSharpCore
    // ──────────────────────────────────────────────────────────────────────────

    private static string CreateTempPdf(int pages = 2, string? prefix = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix ?? "test"}_{Guid.NewGuid():N}.pdf");
        using var doc = new PdfSharpCore.Pdf.PdfDocument();
        doc.Info.Title = "Test Document";
        for (int i = 0; i < pages; i++)
        {
            var page = doc.AddPage();
            page.Width = PdfSharpCore.Drawing.XUnit.FromPoint(595);
            page.Height = PdfSharpCore.Drawing.XUnit.FromPoint(842);
            using var gfx = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page);
            var font = new PdfSharpCore.Drawing.XFont("Arial", 12);
            gfx.DrawString($"Test Page {i + 1}", font, PdfSharpCore.Drawing.XBrushes.Black,
                new PdfSharpCore.Drawing.XRect(50, 50, 495, 30), PdfSharpCore.Drawing.XStringFormats.TopLeft);
        }
        doc.Save(path);
        return path;
    }

    private static void CleanupFiles(params string[] paths)
    {
        foreach (var p in paths)
            if (p != null && File.Exists(p)) try { File.Delete(p); } catch { }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PdfPageService Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PdfPageService_Merge_CombinesMultipleFiles()
    {
        var file1 = CreateTempPdf(2, "merge1");
        var file2 = CreateTempPdf(3, "merge2");
        var outPath = Path.Combine(Path.GetTempPath(), $"merged_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfPageService();
            var opts = new MergeToolOptions
            {
                InputFiles = new List<string> { file1, file2 },
                OutputFilePath = outPath
            };
            var result = await svc.MergePdfAsync(opts);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
            // 2 + 3 = 5 pages
            using var merged = UglyToad.PdfPig.PdfDocument.Open(outPath);
            Assert.Equal(5, merged.NumberOfPages);
        }
        finally { CleanupFiles(file1, file2, outPath); }
    }

    [Fact]
    public async Task PdfPageService_Merge_FailsOnMissingFile()
    {
        var svc = new PdfPageService();
        var result = await svc.MergePdfAsync(new MergeToolOptions
        {
            InputFiles = new List<string> { "/nonexistent/file.pdf" },
            OutputFilePath = "/tmp/out.pdf"
        });
        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task PdfPageService_Split_ProducesOnePdfPerPage()
    {
        var inputPath = CreateTempPdf(4, "split");
        var outDir = Path.Combine(Path.GetTempPath(), $"split_{Guid.NewGuid():N}");
        try
        {
            var svc = new PdfPageService();
            var opts = new SplitToolOptions
            {
                InputFilePath = inputPath,
                OutputDirectory = outDir,
                Mode = SplitExtractMode.SplitEveryNPages,
                PagesPerSplit = 1
            };
            var result = await svc.SplitPdfAsync(opts);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.OutputFiles);
            Assert.Equal(4, result.OutputFiles!.Count);
            foreach (var f in result.OutputFiles) Assert.True(File.Exists(f));
        }
        finally
        {
            CleanupFiles(inputPath);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        }
    }

    [Fact]
    public async Task PdfPageService_Split_ByRanges_ProducesCorrectCount()
    {
        var inputPath = CreateTempPdf(6, "splitrange");
        var outDir = Path.Combine(Path.GetTempPath(), $"splitrange_{Guid.NewGuid():N}");
        try
        {
            var svc = new PdfPageService();
            var opts = new SplitToolOptions
            {
                InputFilePath = inputPath,
                OutputDirectory = outDir,
                Mode = SplitExtractMode.SplitByPageRanges,
                RangeExpression = "1-2, 3-4, 5-6"
            };
            var result = await svc.SplitPdfAsync(opts);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(3, result.OutputFiles!.Count);
        }
        finally
        {
            CleanupFiles(inputPath);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        }
    }

    [Fact]
    public async Task PdfPageService_Rotate_ProducesOutputFile()
    {
        var inputPath = CreateTempPdf(2, "rotate");
        var outPath = Path.Combine(Path.GetTempPath(), $"rotated_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfPageService();
            var opts = new RotateToolOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath,
                RotationDegrees = 90,
                TargetFilter = PageFilterTarget.All
            };
            var result = await svc.RotatePdfAsync(opts);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
            Assert.True(new FileInfo(outPath).Length > 0);
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    [Fact]
    public async Task PdfPageService_AddPageNumbers_ProducesValidPdf()
    {
        var inputPath = CreateTempPdf(3, "pagenums");
        var outPath = Path.Combine(Path.GetTempPath(), $"pagenums_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfPageService();
            var opts = new PageNumberToolOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath,
                Position = PageNumberPosition.BottomCenter,
                Template = "Page {n} of {total}",
                FontSize = 10
            };
            var result = await svc.AddPageNumbersAsync(opts);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PdfOptimizationService Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PdfOptimizationService_Compress_ProducesOutputFile()
    {
        var inputPath = CreateTempPdf(3, "compress");
        var outPath = Path.Combine(Path.GetTempPath(), $"compressed_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfOptimizationService();
            var opts = new CompressToolOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath,
                Level = PdfCompressionLevel.MaximumCompression
            };
            var result = await svc.CompressPdfAsync(opts);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    [Fact]
    public async Task PdfOptimizationService_Compress_FailsOnMissingFile()
    {
        var svc = new PdfOptimizationService();
        var result = await svc.CompressPdfAsync(new CompressToolOptions
        {
            InputFilePath = "/nonexistent/file.pdf",
            OutputFilePath = "/tmp/out.pdf"
        });
        Assert.False(result.Success);
    }

    [Fact]
    public async Task PdfOptimizationService_Repair_ProducesOutputFile()
    {
        var inputPath = CreateTempPdf(2, "repair");
        var outPath = Path.Combine(Path.GetTempPath(), $"repaired_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfOptimizationService();
            var opts = new RepairToolOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath
            };
            var result = await svc.RepairPdfAsync(opts);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    [Fact]
    public async Task PdfOptimizationService_ConvertToPdfA_ProducesOutputFile()
    {
        var inputPath = CreateTempPdf(1, "pdfa");
        var outPath = Path.Combine(Path.GetTempPath(), $"pdfa_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfOptimizationService();
            var opts = new PdfAToolOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath,
                Standard = PdfAStandard.PdfA1b
            };
            var result = await svc.ConvertToPdfAAsync(opts);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PdfSecurityService Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PdfSecurityService_ProtectAndUnlock_RoundTrip()
    {
        var inputPath = CreateTempPdf(2, "protect");
        var protectedPath = Path.Combine(Path.GetTempPath(), $"protected_{Guid.NewGuid():N}.pdf");
        var unlockedPath = Path.Combine(Path.GetTempPath(), $"unlocked_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfSecurityService();

            // Protect
            var protectResult = await svc.ProtectPdfAsync(new SecurityToolOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = protectedPath,
                UserPassword = "user123",
                OwnerPassword = "owner456",
                AllowPrinting = true,
                AllowCopying = false
            });
            Assert.True(protectResult.Success, protectResult.ErrorMessage);
            Assert.True(File.Exists(protectedPath));

            // Unlock
            var unlockResult = await svc.UnlockPdfAsync(new UnlockToolOptions
            {
                InputFilePath = protectedPath,
                OutputFilePath = unlockedPath,
                Password = "owner456"
            });
            Assert.True(unlockResult.Success, unlockResult.ErrorMessage);
            Assert.True(File.Exists(unlockedPath));
        }
        finally { CleanupFiles(inputPath, protectedPath, unlockedPath); }
    }

    [Fact]
    public async Task PdfSecurityService_Protect_FailsOnMissingFile()
    {
        var svc = new PdfSecurityService();
        var result = await svc.ProtectPdfAsync(new SecurityToolOptions
        {
            InputFilePath = "/nonexistent/file.pdf",
            OutputFilePath = "/tmp/protected.pdf",
            UserPassword = "pass"
        });
        Assert.False(result.Success);
    }

    [Fact]
    public async Task PdfSecurityService_AddWatermark_ProducesOutputFile()
    {
        var inputPath = CreateTempPdf(2, "watermark");
        var outPath = Path.Combine(Path.GetTempPath(), $"watermarked_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfSecurityService();
            var result = await svc.AddWatermarkAsync(new WatermarkToolOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath,
                Text = "CONFIDENTIAL",
                Opacity = 0.3,
                FontSize = 48,
                RotationAngle = -45
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PdfConversionService Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PdfConversionService_PdfToWord_ProducesDocxFile()
    {
        var inputPath = CreateTempPdf(2, "toword");
        var outPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.docx");
        try
        {
            var svc = new PdfConversionService();
            var result = await svc.ConvertPdfToWordAsync(new WordConversionOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
            Assert.True(new FileInfo(outPath).Length > 0);
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    [Fact]
    public async Task PdfConversionService_PdfToExcel_ProducesXlsxFile()
    {
        var inputPath = CreateTempPdf(1, "toexcel");
        var outPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.xlsx");
        try
        {
            var svc = new PdfConversionService();
            var result = await svc.ConvertPdfToExcelAsync(new ExcelConversionOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    [Fact]
    public async Task PdfConversionService_PdfToImages_ProducesImageFiles()
    {
        var inputPath = CreateTempPdf(2, "toimages");
        var outDir = Path.Combine(Path.GetTempPath(), $"images_{Guid.NewGuid():N}");
        try
        {
            var svc = new PdfConversionService();
            var result = await svc.ConvertPdfToImagesAsync(new ImageConversionOptions
            {
                InputFilePath = inputPath,
                OutputDirectory = outDir,
                OutputFormat = "png",
                Dpi = 72
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.OutputFiles);
            Assert.Equal(2, result.OutputFiles!.Count);
            foreach (var f in result.OutputFiles) Assert.True(File.Exists(f));
        }
        finally
        {
            CleanupFiles(inputPath);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        }
    }

    [Fact]
    public async Task PdfConversionService_ImagesToPdf_ProducesValidPdf()
    {
        // Create a temp PNG
        string imgPath = Path.Combine(Path.GetTempPath(), $"img_{Guid.NewGuid():N}.png");
        using (var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(400, 600)))
        {
            surface.Canvas.Clear(SkiaSharp.SKColors.LightBlue);
            using var img = surface.Snapshot();
            using var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 90);
            using var fs = File.OpenWrite(imgPath);
            data.SaveTo(fs);
        }
        var outPath = Path.Combine(Path.GetTempPath(), $"fromimages_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfConversionService();
            var result = await svc.ConvertImagesToPdfAsync(new ImagesToPdfOptions
            {
                ImageFiles = new List<string> { imgPath },
                OutputFilePath = outPath
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(imgPath, outPath); }
    }

    [Fact]
    public async Task PdfConversionService_HtmlToPdf_ProducesValidPdf()
    {
        var outPath = Path.Combine(Path.GetTempPath(), $"fromhtml_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfConversionService();
            var result = await svc.ConvertHtmlToPdfAsync(new HtmlToPdfOptions
            {
                HtmlContentOrUrl = "<h1>Test</h1><p>Hello PDF world</p>",
                IsUrl = false,
                OutputFilePath = outPath
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(outPath); }
    }

    [Fact]
    public async Task PdfConversionService_PdfToMarkdown_ProducesMarkdownFile()
    {
        var inputPath = CreateTempPdf(2, "tomd");
        var outPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.md");
        try
        {
            var svc = new PdfConversionService();
            var result = await svc.ConvertPdfToMarkdownAsync(new MarkdownConversionOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath,
                IncludeMetadataHeader = true
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
            var content = await File.ReadAllTextAsync(outPath);
            Assert.Contains("---", content); // YAML frontmatter
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    [Fact]
    public async Task PdfConversionService_PdfToWord_SupportsCancellation()
    {
        var inputPath = CreateTempPdf(2, "cancelword");
        var outPath = Path.Combine(Path.GetTempPath(), $"cancelword_{Guid.NewGuid():N}.docx");
        try
        {
            var svc = new PdfConversionService();
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // cancel immediately
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                svc.ConvertPdfToWordAsync(new WordConversionOptions
                {
                    InputFilePath = inputPath,
                    OutputFilePath = outPath
                }, ct: cts.Token));
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PdfOcrService Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PdfOcrService_OcrPdf_ProducesOutputFile()
    {
        var inputPath = CreateTempPdf(2, "ocr");
        var outPath = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfOcrService();
            var result = await svc.OcrPdfAsync(new OcrToolOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PdfFormService Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PdfFormService_ExtractFields_ReturnsResult()
    {
        var inputPath = CreateTempPdf(1, "forms");
        try
        {
            var svc = new PdfFormService();
            var fields = await svc.ExtractFormFieldsAsync(inputPath);
            // A simple text PDF has no AcroForm fields - should succeed with empty dictionary
            Assert.NotNull(fields);
        }
        finally { CleanupFiles(inputPath); }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AiDocumentService Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AiDocumentService_Summarize_ProducesNonEmptySummary()
    {
        var inputPath = CreateTempPdf(3, "ai");
        try
        {
            var svc = new AiDocumentService();
            var result = await svc.SummarizePdfAsync(new AiSummaryOptions
            {
                InputFilePath = inputPath,
                MaxBulletPoints = 3
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotEmpty(result.Message ?? "");
        }
        finally { CleanupFiles(inputPath); }
    }

    [Fact]
    public async Task AiDocumentService_Summarize_FailsOnMissingFile()
    {
        var svc = new AiDocumentService();
        var result = await svc.SummarizePdfAsync(new AiSummaryOptions
        {
            InputFilePath = "/nonexistent/ghost.pdf"
        });
        Assert.False(result.Success);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DocumentTranslationService Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DocumentTranslationService_Translate_ProducesOutputFile()
    {
        var inputPath = CreateTempPdf(2, "translate");
        var outPath = Path.Combine(Path.GetTempPath(), $"translated_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new DocumentTranslationService();
            var result = await svc.TranslatePdfAsync(new TranslationOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outPath,
                TargetLanguage = "Spanish"
            });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.True(File.Exists(outPath));
        }
        finally { CleanupFiles(inputPath, outPath); }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PdfWorkflowEngine Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PdfWorkflowEngine_ExecuteWorkflow_RunsAllSteps()
    {
        var inputPath = CreateTempPdf(3, "workflow");
        var outDir = Path.Combine(Path.GetTempPath(), $"workflow_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outDir);
        try
        {
            var engine = new PdfWorkflowEngine();

            var workflow = new WorkflowDefinition
            {
                Name = "Test Workflow",
                OutputDirectory = outDir,
                Steps = new List<WorkflowStepDefinition>
                {
                    new() { ToolId = PdfToolId.CompressPdf, IsEnabled = true },
                    new() { ToolId = PdfToolId.RotatePdf, IsEnabled = true, ParametersJson = "{\"RotationDegrees\":90}" }
                }
            };

            var result = await engine.ExecuteWorkflowAsync(workflow, new List<string> { inputPath });
            Assert.True(result.Success, result.ErrorMessage);
            Assert.NotNull(result.OutputFiles);
            Assert.True(result.OutputFiles!.Count > 0);
        }
        finally
        {
            CleanupFiles(inputPath);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        }
    }

    [Fact]
    public void PdfWorkflowEngine_SaveAndLoad_WorkflowDefinition()
    {
        var engine = new PdfWorkflowEngine();

        var workflow = new WorkflowDefinition
        {
            Name = "Save/Load Test",
            Description = "Tests serialization roundtrip",
            Steps = new List<WorkflowStepDefinition>
            {
                new() { ToolId = PdfToolId.MergePdf },
                new() { ToolId = PdfToolId.CompressPdf }
            }
        };

        string json = engine.SerializeWorkflow(workflow);
        Assert.NotEmpty(json);

        var loaded = engine.DeserializeWorkflow(json);
        Assert.NotNull(loaded);
        Assert.Equal("Save/Load Test", loaded!.Name);
        Assert.Equal(2, loaded.Steps.Count);
        Assert.Equal(PdfToolId.MergePdf, loaded.Steps[0].ToolId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PdfToolRegistry Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PdfToolRegistry_GetAllTools_Returns32Tools()
    {
        var registry = new PdfToolRegistry();
        var tools = registry.GetAllTools();
        Assert.NotEmpty(tools);
        Assert.True(tools.Count >= 32, $"Expected 32+ tool definitions, got {tools.Count}");
    }

    [Fact]
    public void PdfToolRegistry_GetTool_ReturnsCorrectDefinition()
    {
        var registry = new PdfToolRegistry();
        var merge = registry.GetTool(PdfToolId.MergePdf);
        Assert.NotNull(merge);
        Assert.Equal(PdfToolId.MergePdf, merge!.Id);
        Assert.NotEmpty(merge.Name);
        Assert.NotEmpty(merge.Description);
    }

    [Fact]
    public void PdfToolRegistry_GetByCategory_ReturnsSubset()
    {
        var registry = new PdfToolRegistry();
        var organizeTools = registry.GetToolsByCategory(PdfToolCategory.OrganizeAndPage);
        Assert.NotEmpty(organizeTools);
        Assert.All(organizeTools, t => Assert.Equal(PdfToolCategory.OrganizeAndPage, t.Category));
    }

    [Fact]
    public void PdfToolRegistry_AllTools_HaveUniqueIds()
    {
        var registry = new PdfToolRegistry();
        var tools = registry.GetAllTools();
        var ids = tools.Select(t => t.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void PdfToolRegistry_AllTools_HaveValidMetadata()
    {
        var registry = new PdfToolRegistry();
        foreach (var tool in registry.GetAllTools())
        {
            Assert.NotEmpty(tool.Name);
            Assert.NotEmpty(tool.Description);
            Assert.NotEmpty(tool.IconColorHex);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PdfDocumentOperationsService Integration Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PdfDocumentOperationsService_Merge_ReturnsSuccess()
    {
        var file1 = CreateTempPdf(2, "opsmerge1");
        var file2 = CreateTempPdf(2, "opsmerge2");
        var outPath = Path.Combine(Path.GetTempPath(), $"opsmerge_{Guid.NewGuid():N}.pdf");
        try
        {
            var svc = new PdfDocumentOperationsService();
            var result = await svc.ExecuteToolAsync(PdfToolId.MergePdf,
                new MergeToolOptions
                {
                    InputFiles = new List<string> { file1, file2 },
                    OutputFilePath = outPath
                });
            Assert.True(result.Success, result.ErrorMessage);
        }
        finally { CleanupFiles(file1, file2, outPath); }
    }

    [Fact]
    public async Task PdfDocumentOperationsService_ProgressReporting_Fires()
    {
        var inputPath = CreateTempPdf(3, "progress");
        var outDir = Path.Combine(Path.GetTempPath(), $"progressdir_{Guid.NewGuid():N}");
        try
        {
            var svc = new PdfDocumentOperationsService();
            var progressValues = new List<double>();
            var progress = new Progress<double>(v => progressValues.Add(v));

            await svc.ExecuteToolAsync(PdfToolId.SplitPdf,
                new SplitToolOptions
                {
                    InputFilePath = inputPath,
                    OutputDirectory = outDir,
                    Mode = SplitExtractMode.SplitEveryNPages,
                    PagesPerSplit = 1
                },
                progress: progress);

            // Give progress callbacks a moment to fire
            await Task.Delay(50);
            Assert.True(progressValues.Count > 0, "Progress should have reported at least once");
        }
        finally
        {
            CleanupFiles(inputPath);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        }
    }
}
