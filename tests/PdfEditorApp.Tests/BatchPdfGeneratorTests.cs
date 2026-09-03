using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Data;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using Xunit;

namespace PdfEditorApp.Tests;

public class BatchPdfGeneratorTests
{
    private readonly IDataMergeEngine _mergeEngine = new DataMergeEngine();
    private readonly IPdfExportService _exportService = new PdfExportService();

    private PdfDocumentModel CreateSampleTemplate()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Payslip_{{EmployeeId}}.pdf"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Width = 600,
            Height = 800,
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement
                {
                    X = 50,
                    Y = 50,
                    Width = 400,
                    Height = 30,
                    Text = "Payslip for {{EmployeeName}}",
                    FontSize = 14
                },
                new PdfTextElement
                {
                    X = 50,
                    Y = 90,
                    Width = 400,
                    Height = 25,
                    Text = "Net Salary: {{NetSalary:currency:INR}}",
                    FontSize = 12
                },
                new PdfQrCodeElement
                {
                    X = 50,
                    Y = 130,
                    Width = 80,
                    Height = 80,
                    Content = "https://verify.codefrydev.in?emp={{EmployeeId}}"
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }

    private DataMatrix CreateSampleMatrix()
    {
        var headers = new List<string> { "EmployeeId", "EmployeeName", "NetSalary" };
        var rows = new List<List<string>>
        {
            new() { "EMP-101", "John Doe", "75000" },
            new() { "EMP-102", "Jane Doe", "82000" },
            new() { "EMP-103", "Alex Doe", "68000" }
        };

        return new DataMatrix(headers, rows);
    }

    [Fact]
    public async Task GenerateBatch_SeparateFilesMode_CreatesIndividualPdfs()
    {
        var generator = new BatchPdfGeneratorService(_mergeEngine, _exportService);
        var template = CreateSampleTemplate();
        var matrix = CreateSampleMatrix();

        string tempDir = Path.Combine(Path.GetTempPath(), "FryPdf_Test_BatchSeparate_" + Guid.NewGuid().ToString("N"));

        try
        {
            var config = new BatchGenerationConfig
            {
                OutputMode = BatchOutputMode.SeparateFiles,
                OutputDirectory = tempDir,
                FilenamePattern = "Payslip_{{EmployeeId}}_{{EmployeeName}}.pdf"
            };

            var progressReports = new List<BatchProgressReport>();
            var progress = new Progress<BatchProgressReport>(p => progressReports.Add(p));

            var result = await generator.GenerateBatchAsync(template, matrix, null, config, progress);

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.SuccessfulCount);
            Assert.Equal(3, result.GeneratedFiles.Count);

            foreach (var file in result.GeneratedFiles)
            {
                Assert.True(File.Exists(file));
                byte[] bytes = File.ReadAllBytes(file);
                Assert.True(bytes.Length > 100);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task GenerateBatch_SingleMergedPdfMode_CreatesMultiPagePdf()
    {
        var generator = new BatchPdfGeneratorService(_mergeEngine, _exportService);
        var template = CreateSampleTemplate();
        var matrix = CreateSampleMatrix();

        string tempFile = Path.Combine(Path.GetTempPath(), "FryPdf_Test_Merged_" + Guid.NewGuid().ToString("N") + ".pdf");

        try
        {
            var config = new BatchGenerationConfig
            {
                OutputMode = BatchOutputMode.SingleMergedPdf,
                OutputFilePath = tempFile
            };

            var result = await generator.GenerateBatchAsync(template, matrix, null, config);

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.SuccessfulCount);
            Assert.True(File.Exists(tempFile));

            byte[] bytes = File.ReadAllBytes(tempFile);
            Assert.True(bytes.Length > 200);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task GenerateBatch_ZipArchiveMode_CreatesZipWithAllPdfs()
    {
        var generator = new BatchPdfGeneratorService(_mergeEngine, _exportService);
        var template = CreateSampleTemplate();
        var matrix = CreateSampleMatrix();

        string tempZip = Path.Combine(Path.GetTempPath(), "FryPdf_Test_Zip_" + Guid.NewGuid().ToString("N") + ".zip");

        try
        {
            var config = new BatchGenerationConfig
            {
                OutputMode = BatchOutputMode.ZipArchive,
                OutputFilePath = tempZip,
                FilenamePattern = "Payslip_{{EmployeeId}}.pdf"
            };

            var result = await generator.GenerateBatchAsync(template, matrix, null, config);

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.SuccessfulCount);
            Assert.True(File.Exists(tempZip));

            // Verify Zip Entries
            using (var zip = ZipFile.OpenRead(tempZip))
            {
                Assert.Equal(3, zip.Entries.Count);
                Assert.NotNull(zip.GetEntry("Payslip_EMP-101.pdf"));
                Assert.NotNull(zip.GetEntry("Payslip_EMP-102.pdf"));
                Assert.NotNull(zip.GetEntry("Payslip_EMP-103.pdf"));
            }
        }
        finally
        {
            if (File.Exists(tempZip))
            {
                File.Delete(tempZip);
            }
        }
    }

    [Fact]
    public async Task GenerateBatch_Cancellation_StopsExecutionGracefully()
    {
        var generator = new BatchPdfGeneratorService(_mergeEngine, _exportService);
        var template = CreateSampleTemplate();
        var matrix = CreateSampleMatrix();

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var config = new BatchGenerationConfig
        {
            OutputMode = BatchOutputMode.SeparateFiles,
            OutputDirectory = Path.GetTempPath()
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await generator.GenerateBatchAsync(template, matrix, null, config, null, cts.Token);
        });
    }
}
