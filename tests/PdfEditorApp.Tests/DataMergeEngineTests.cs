using System;
using System.Collections.Generic;
using PdfEditorApp.Core.Data;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using Xunit;

namespace PdfEditorApp.Tests;

public class DataMergeEngineTests
{
    private readonly IDataMergeEngine _engine = new DataMergeEngine();

    [Fact]
    public void EvaluateText_SimplePlaceholder_ReplacesValue()
    {
        var record = new Dictionary<string, string>
        {
            ["EmployeeName"] = "Johnathan Doe",
            ["EmployeeId"] = "EMP-0842"
        };

        string template = "Employee: {{EmployeeName}} (ID: {{EmployeeId}})";
        string result = _engine.EvaluateText(template, record);

        Assert.Equal("Employee: Johnathan Doe (ID: EMP-0842)", result);
    }

    [Fact]
    public void EvaluateText_CaseInsensitiveLookup_MatchesDifferentCasing()
    {
        var record = new Dictionary<string, string>
        {
            ["EMPLOYEE_NAME"] = "Sophia Martinez"
        };

        string template = "Hello, {{EmployeeName}}!";
        string result = _engine.EvaluateText(template, record, new DataMergeOptions { CaseInsensitiveLookup = true });

        Assert.Equal("Hello, Sophia Martinez!", result);
    }

    [Fact]
    public void EvaluateText_FallbackValue_UsedWhenKeyIsMissingOrEmpty()
    {
        var record = new Dictionary<string, string>
        {
            ["EmployeeName"] = "David Kim"
        };

        string template = "Dept: {{Department ?? Engineering}}, Level: {{Level ?? L5}}";
        string result = _engine.EvaluateText(template, record);

        Assert.Equal("Dept: Engineering, Level: L5", result);
    }

    [Fact]
    public void EvaluateText_CurrencyFormatting_FormatsCorrectly()
    {
        var record = new Dictionary<string, string>
        {
            ["BasicSalary"] = "8500",
            ["InrSalary"] = "125000",
            ["EurSalary"] = "6200"
        };

        string templateInr = "Salary: {{InrSalary:currency:INR}}";
        string templateEur = "Salary: {{EurSalary:currency:EUR}}";

        string resultInr = _engine.EvaluateText(templateInr, record);
        string resultEur = _engine.EvaluateText(templateEur, record);

        Assert.Contains("₹1,25,000.00", resultInr);
        Assert.Contains("€6,200.00", resultEur);
    }

    [Fact]
    public void EvaluateText_NumericAndDateFormatting_FormatsCorrectly()
    {
        var record = new Dictionary<string, string>
        {
            ["GrossAmount"] = "12345.6789",
            ["JoiningDate"] = "2022-04-15"
        };

        string templateNum = "Amount: {{GrossAmount:N2}}";
        string templateDate = "Joined: {{JoiningDate:yyyy/MM/dd}}";

        string resultNum = _engine.EvaluateText(templateNum, record);
        string resultDate = _engine.EvaluateText(templateDate, record);

        Assert.Equal("Amount: 12,345.68", resultNum);
        Assert.Equal("Joined: 2022/04/15", resultDate);
    }

    [Fact]
    public void EvaluateText_CaseTransforms_UppercaseLowercaseTitleCase()
    {
        var record = new Dictionary<string, string>
        {
            ["Designation"] = "senior cloud engineer"
        };

        string templateUpper = "{{Designation:upper}}";
        string templateTitle = "{{Designation:title}}";

        Assert.Equal("SENIOR CLOUD ENGINEER", _engine.EvaluateText(templateUpper, record));
        Assert.Equal("Senior Cloud Engineer", _engine.EvaluateText(templateTitle, record));
    }

    [Fact]
    public void DetectPlaceholders_ExtractsAllTagsAcrossDocumentElements()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Payslip for {{EmployeeName}}",
            Author = "{{Company}}"
        };

        var page = new PdfPageModel
        {
            HeaderLeft = "Confidential - {{Department}}",
            FooterRight = "Generated for {{EmployeeId}}",
            Watermark = new PdfWatermarkElement { Text = "{{WatermarkStatus}}" },
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement { Text = "Gross: {{GrossSalary:C}}" },
                new PdfQrCodeElement { Content = "https://verify.com?id={{EmployeeId}}" },
                new PdfBarcodeElement { CodeValue = "{{BarcodeValue}}" },
                new PdfTableElement
                {
                    Headers = new List<string> { "{{Header1}}", "Amount" },
                    Rows = new List<List<string>>
                    {
                        new() { "Basic", "{{BasicSalary}}" }
                    }
                }
            }
        };

        doc.Pages.Add(page);

        var detected = _engine.DetectPlaceholders(doc);

        Assert.Contains("EmployeeName", detected);
        Assert.Contains("Company", detected);
        Assert.Contains("Department", detected);
        Assert.Contains("EmployeeId", detected);
        Assert.Contains("WatermarkStatus", detected);
        Assert.Contains("GrossSalary", detected);
        Assert.Contains("BarcodeValue", detected);
        Assert.Contains("Header1", detected);
        Assert.Contains("BasicSalary", detected);
    }

    [Fact]
    public void HydrateDocument_PopulatesAllElementsInClonedDocument()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Payslip - {{EmployeeName}}"
        };

        var page = new PdfPageModel
        {
            HeaderLeft = "Dept: {{Department}}",
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement { Text = "Welcome {{EmployeeName}}, Salary: {{Salary:currency:INR}}" },
                new PdfQrCodeElement { Content = "https://verify.corp.com/emp/{{EmployeeId}}" },
                new PdfBarcodeElement { CodeValue = "{{EmployeeId}}" },
                new PdfTableElement
                {
                    Headers = new List<string> { "Item", "Value" },
                    Rows = new List<List<string>>
                    {
                        new() { "Base", "{{Salary:currency:INR}}" }
                    }
                }
            }
        };
        doc.Pages.Add(page);

        var record = new Dictionary<string, string>
        {
            ["EmployeeName"] = "Amina Al-Mansoor",
            ["Department"] = "Human Resources",
            ["EmployeeId"] = "EMP-1315",
            ["Salary"] = "92000"
        };

        var hydrated = _engine.HydrateDocument(doc, record);

        // Original template remains untouched
        Assert.Equal("Payslip - {{EmployeeName}}", doc.Title);

        // Hydrated clone has values replaced
        Assert.Equal("Payslip - Amina Al-Mansoor", hydrated.Title);
        Assert.Equal("Dept: Human Resources", hydrated.Pages[0].HeaderLeft);

        var textEl = (PdfTextElement)hydrated.Pages[0].Elements[0];
        Assert.Contains("Welcome Amina Al-Mansoor", textEl.Text);
        Assert.Contains("₹92,000.00", textEl.Text);

        var qrEl = (PdfQrCodeElement)hydrated.Pages[0].Elements[1];
        Assert.Equal("https://verify.corp.com/emp/EMP-1315", qrEl.Content);

        var barEl = (PdfBarcodeElement)hydrated.Pages[0].Elements[2];
        Assert.Equal("EMP-1315", barEl.CodeValue);

        var tableEl = (PdfTableElement)hydrated.Pages[0].Elements[3];
        Assert.Equal("₹92,000.00", tableEl.Rows[0][1]);
    }
}
