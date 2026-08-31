using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Core.Data;
using PdfEditorApp.Services;
using PdfEditorApp.Templates;
using Xunit;

namespace PdfEditorApp.Tests;

public class EmployeePayslipBatchTests
{
    private readonly IDataSourceService _dataSourceService = new DataSourceService();
    private readonly IDataMergeEngine _mergeEngine = new DataMergeEngine();
    private readonly IPdfExportService _exportService = new PdfExportService();
    private readonly ITemplateService _templateService = new TemplateService();

    [Fact]
    public void EmployeePayslipTemplate_ContainsExpectedMergeTags()
    {
        var template = _templateService.CreateEmployeePayslipTemplate();
        Assert.NotNull(template);
        Assert.Single(template.Pages);

        var detected = _mergeEngine.DetectPlaceholders(template);

        Assert.Contains("EmployeeName", detected);
        Assert.Contains("EmployeeId", detected);
        Assert.Contains("Designation", detected);
        Assert.Contains("Department", detected);
        Assert.Contains("BasicSalary", detected);
        Assert.Contains("HRA", detected);
        Assert.Contains("GrossEarnings", detected);
        Assert.Contains("ProvidentFund", detected);
        Assert.Contains("TotalDeductions", detected);
        Assert.Contains("NetSalary", detected);
        Assert.Contains("PayPeriod", detected);
    }

    [Fact]
    public async Task EmployeePayslip_FullBatchGeneration_ProducesValidPdfs()
    {
        var template = _templateService.CreateEmployeePayslipTemplate();

        string sampleCsv =
@"EmployeeId,EmployeeName,Designation,Department,JoiningDate,BankName,AccountNumber,TaxId,WorkingDays,BasicSalary,HRA,SpecialAllowance,Bonus,MedicalAllowance,GrossEarnings,ProvidentFund,IncomeTax,ProfessionalTax,Insurance,OtherDeductions,TotalDeductions,NetSalary,NetSalaryInWords,PayPeriod,CompanyName,AuthHash
EMP-2026-0842,Johnathan Doe,Senior Software Architect,Cloud Infrastructure,2021-03-15,JPMorgan Chase,****8492,US-SSN-8429,30,8500,3400,1800,1200,600,15500,950,1850,200,350,0,3350,12150,Twelve Thousand One Hundred Fifty US Dollars,August 2026,Apex Global Technologies Inc.,9A4F-8201-B732
EMP-2026-0914,Sophia Martinez,Principal UI/UX Designer,Product Experience,2022-06-01,Bank of America,****3198,US-SSN-1942,30,7800,3120,1500,900,500,13820,850,1550,200,300,0,2900,10920,Ten Thousand Nine Hundred Twenty US Dollars,August 2026,Apex Global Technologies Inc.,7E2B-9410-C311
EMP-2026-1052,David Kim,Lead DevOps Engineer,Cloud Infrastructure,2020-11-10,Wells Fargo,****9041,US-SSN-7731,30,8200,3280,1650,1100,550,14780,900,1720,200,320,0,3140,11640,Eleven Thousand Six Hundred Forty US Dollars,August 2026,Apex Global Technologies Inc.,4D1C-8822-A904";

        var matrix = _dataSourceService.ParseCsv(sampleCsv, ',', true);
        Assert.Equal(3, matrix.RowCount);

        var generator = new BatchPdfGeneratorService(_mergeEngine, _exportService);
        string tempDir = Path.Combine(Path.GetTempPath(), "FryPdf_Test_Payslips_" + Guid.NewGuid().ToString("N"));

        try
        {
            var config = new BatchGenerationConfig
            {
                OutputMode = BatchOutputMode.SeparateFiles,
                OutputDirectory = tempDir,
                FilenamePattern = "Payslip_{{EmployeeId}}_{{EmployeeName}}_{{PayPeriod}}.pdf"
            };

            var result = await generator.GenerateBatchAsync(template, matrix, null, config);

            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.SuccessfulCount);
            Assert.Equal(0, result.FailedCount);

            // Check that all 3 files exist and are valid non-empty PDFs
            var files = Directory.GetFiles(tempDir, "*.pdf");
            Assert.Equal(3, files.Length);

            foreach (var file in files)
            {
                byte[] bytes = await File.ReadAllBytesAsync(file);
                Assert.True(bytes.Length > 500);

                // Verify PDF signature (%PDF-)
                string header = System.Text.Encoding.ASCII.GetString(bytes.Take(5).ToArray());
                Assert.Equal("%PDF-", header);
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
}
