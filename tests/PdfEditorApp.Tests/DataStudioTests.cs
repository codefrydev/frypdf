using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PdfEditorApp.Core.Data;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.DataStudio;
using PdfEditorApp.ViewModels.ElementViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class DataStudioTests
{
    [Fact]
    public void DataMatrix_BasicOperations_AndTypeInference()
    {
        var headers = new List<string> { "Product", "Revenue", "Margin", "Launch Date" };
        var rows = new List<List<string>>
        {
            new() { "Laptop Pro", "$1,200.50", "25.5%", "2024-01-15" },
            new() { "Desk Monitor", "$450.00", "30.0%", "2024-02-20" },
            new() { "Mechanical Keyboard", "$120.00", "42.1%", "2024-03-05" }
        };

        var matrix = new DataMatrix(headers, rows);

        Assert.Equal(3, matrix.RowCount);
        Assert.Equal(4, matrix.ColumnCount);
        Assert.Equal("$1,200.50", matrix.GetCell(0, 1));
        Assert.Equal("Mechanical Keyboard", matrix.GetCell(2, 0));

        // Test Type Inference
        Assert.Equal(DataColumnType.Text, matrix.InferColumnType(0));
        Assert.Equal(DataColumnType.Currency, matrix.InferColumnType(1));
        Assert.Equal(DataColumnType.Percentage, matrix.InferColumnType(2));
        Assert.Equal(DataColumnType.Date, matrix.InferColumnType(3));

        // Row/Column mutations
        matrix.AddRow(new[] { "Wireless Mouse", "$60.00", "50.0%", "2024-04-10" });
        Assert.Equal(4, matrix.RowCount);

        matrix.RemoveRow(3);
        Assert.Equal(3, matrix.RowCount);

        matrix.AddColumn("Stock Units", "100");
        Assert.Equal(5, matrix.ColumnCount);
        Assert.Equal("100", matrix.GetCell(0, 4));

        matrix.RemoveColumn(4);
        Assert.Equal(4, matrix.ColumnCount);
    }

    [Fact]
    public void DataSourceService_CsvParser_HandlesRFC4180AndDelimiters()
    {
        var service = new DataSourceService();

        // 1. Standard CSV with quotes, commas, and embedded newlines
        string csvData = "Name,Description,Amount,Status\n" +
                         "\"Alpha Widget\",\"High quality, durable\",1500,Active\n" +
                         "\"Beta Device\",\"Multi-line\r\nSpecs Included\",3200,Pending";

        var matrix = service.ParseCsv(csvData, ',', true);

        Assert.Equal(2, matrix.RowCount);
        Assert.Equal(4, matrix.ColumnCount);
        Assert.Equal("Name", matrix.Headers[0]);
        Assert.Equal("Alpha Widget", matrix.GetCell(0, 0));
        Assert.Equal("High quality, durable", matrix.GetCell(0, 1));
        Assert.Equal("1500", matrix.GetCell(0, 2));
        Assert.Contains("Multi-line", matrix.GetCell(1, 1));

        // 2. Semicolon-delimited European CSV
        string semiCsv = "Category;Q1;Q2;Q3\nEurope;100;120;140\nAsia;200;220;250";
        var semiMatrix = service.ParseCsv(semiCsv, ';', true);
        Assert.Equal(2, semiMatrix.RowCount);
        Assert.Equal(4, semiMatrix.ColumnCount);
        Assert.Equal("Europe", semiMatrix.GetCell(0, 0));

        // 3. Tab-separated data (TSV)
        string tsvData = "Dept\tHeadcount\tBudget\nEngineering\t45\t$500k\nSales\t30\t$400k";
        var tsvMatrix = service.ParseTsv(tsvData, true);
        Assert.Equal(2, tsvMatrix.RowCount);
        Assert.Equal(3, tsvMatrix.ColumnCount);
        Assert.Equal("Engineering", tsvMatrix.GetCell(0, 0));
    }

    [Fact]
    public void DataSourceService_JsonParser_SupportsArraysAndObjects()
    {
        var service = new DataSourceService();

        // 1. Array of flat objects
        string jsonObjects = @"[
            { ""id"": 1, ""product"": ""Cloud Storage"", ""price"": 99.99, ""tier"": ""Standard"" },
            { ""id"": 2, ""product"": ""Dedicated VM"", ""price"": 299.00, ""tier"": ""Enterprise"" }
        ]";

        var matrix = service.ParseJson(jsonObjects);
        Assert.Equal(2, matrix.RowCount);
        Assert.True(matrix.ColumnCount >= 4);
        Assert.Contains("product", matrix.Headers);
        Assert.Equal("Cloud Storage", matrix.GetCell(0, matrix.Headers.IndexOf("product")));

        // 2. Nested JSON with path (e.g. { data: { metrics: [...] } })
        string nestedJson = @"{
            ""status"": ""success"",
            ""data"": {
                ""metrics"": [
                    { ""quarter"": ""Q1"", ""sales"": 500, ""profit"": 80 },
                    { ""quarter"": ""Q2"", ""sales"": 650, ""profit"": 110 }
                ]
            }
        }";

        var nestedMatrix = service.ParseJson(nestedJson, "data.metrics");
        Assert.Equal(2, nestedMatrix.RowCount);
        Assert.Equal("Q1", nestedMatrix.GetCell(0, nestedMatrix.Headers.IndexOf("quarter")));

        // 3. 2D array format
        string json2D = @"[
            [""Region"", ""2024"", ""2025""],
            [""North"", 100, 130],
            [""South"", 80, 95]
        ]";
        var matrix2D = service.ParseJson(json2D);
        Assert.Equal(2, matrix2D.RowCount);
        Assert.Equal(3, matrix2D.ColumnCount);
        Assert.Equal("Region", matrix2D.Headers[0]);
        Assert.Equal("North", matrix2D.GetCell(0, 0));
    }

    [Fact]
    public void DataSourceService_ExcelParser_ReadsOpenXmlSpreadsheet()
    {
        var service = new DataSourceService();

        // Generate in-memory .xlsx file using OpenXml
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();

            // Row 1: Headers
            var headerRow = new Row { RowIndex = 1 };
            headerRow.Append(
                new Cell { CellReference = "A1", DataType = CellValues.String, CellValue = new CellValue("Quarter") },
                new Cell { CellReference = "B1", DataType = CellValues.String, CellValue = new CellValue("Revenue") },
                new Cell { CellReference = "C1", DataType = CellValues.String, CellValue = new CellValue("Expenses") }
            );
            sheetData.Append(headerRow);

            // Row 2: Data
            var dataRow1 = new Row { RowIndex = 2 };
            dataRow1.Append(
                new Cell { CellReference = "A2", DataType = CellValues.String, CellValue = new CellValue("Q1") },
                new Cell { CellReference = "B2", DataType = CellValues.Number, CellValue = new CellValue("150") },
                new Cell { CellReference = "C2", DataType = CellValues.Number, CellValue = new CellValue("90") }
            );
            sheetData.Append(dataRow1);

            // Row 3: Data
            var dataRow2 = new Row { RowIndex = 3 };
            dataRow2.Append(
                new Cell { CellReference = "A3", DataType = CellValues.String, CellValue = new CellValue("Q2") },
                new Cell { CellReference = "B3", DataType = CellValues.Number, CellValue = new CellValue("220") },
                new Cell { CellReference = "C3", DataType = CellValues.Number, CellValue = new CellValue("110") }
            );
            sheetData.Append(dataRow2);

            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = doc.WorkbookPart!.Workbook!.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = doc.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "FinancialSummary"
            });

            workbookPart.Workbook.Save();
        }

        ms.Position = 0;
        var sheetNames = service.GetExcelSheetNames(ms);
        Assert.Single(sheetNames);
        Assert.Equal("FinancialSummary", sheetNames[0]);

        ms.Position = 0;
        var matrix = service.ParseExcel(ms, "FinancialSummary", true);
        Assert.Equal(2, matrix.RowCount);
        Assert.Equal(3, matrix.ColumnCount);
        Assert.Equal("Quarter", matrix.Headers[0]);
        Assert.Equal("Q1", matrix.GetCell(0, 0));
        Assert.Equal("150", matrix.GetCell(0, 1));
        Assert.Equal("Q2", matrix.GetCell(1, 0));
        Assert.Equal("220", matrix.GetCell(1, 1));
    }

    [Fact]
    public void DataBindingService_ChartAndTableMapping_AndBidirectionalConversion()
    {
        var bindingService = new DataBindingService();

        var headers = new List<string> { "Period", "Gross Sales", "Net Profit" };
        var rows = new List<List<string>>
        {
            new() { "2023", "500", "120" },
            new() { "2024", "750", "210" },
            new() { "2025", "1100", "380" }
        };
        var matrix = new DataMatrix(headers, rows);

        // 1. Apply to Chart (Multi-Series)
        var chart = new PdfChartElement { ChartType = ChartType.BarColumn };
        bindingService.ApplyToChart(matrix, chart, 0, new List<int> { 1, 2 });

        Assert.Equal(3, chart.Categories.Count);
        Assert.Equal("2023", chart.Categories[0]);
        Assert.Equal(2, chart.MultiSeries.Count);
        Assert.Equal("Gross Sales", chart.MultiSeries[0].Name);
        Assert.Equal(500, chart.MultiSeries[0].Values[0]);
        Assert.Equal("Net Profit", chart.MultiSeries[1].Name);
        Assert.Equal(120, chart.MultiSeries[1].Values[0]);

        // 2. Extract DataMatrix from Chart
        var extractedFromChart = bindingService.ExtractFromChart(chart);
        Assert.Equal(3, extractedFromChart.RowCount);
        Assert.Equal(3, extractedFromChart.ColumnCount);
        Assert.Equal("2023", extractedFromChart.GetCell(0, 0));
        Assert.Equal("500", extractedFromChart.GetCell(0, 1));
        Assert.Equal("120", extractedFromChart.GetCell(0, 2));

        // 3. Apply to Table
        var table = new PdfTableElement();
        bindingService.ApplyToTable(matrix, table);
        Assert.Equal(3, table.Headers.Count);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal("Period", table.Headers[0]);
        Assert.Equal("1100", table.Rows[2][1]);

        // 4. Convert Table to Chart
        var convertedChart = bindingService.ConvertTableToChart(table, ChartType.SmoothLine);
        Assert.NotNull(convertedChart);
        Assert.Equal(ChartType.SmoothLine, convertedChart.ChartType);
        Assert.Equal(3, convertedChart.Categories.Count);

        // 5. Convert Chart to Table
        var convertedTable = bindingService.ConvertChartToTable(convertedChart);
        Assert.NotNull(convertedTable);
        Assert.Equal(3, convertedTable.Rows.Count);
    }

    [Fact]
    public void DataStudioViewModel_ManualEditing_AndElementCreation()
    {
        var dataSourceService = new DataSourceService();
        var dataBindingService = new DataBindingService();
        var vm = new DataStudioViewModel(dataSourceService, dataBindingService);

        // Open for new Chart
        var page = new PageViewModel { PageNumber = 1, Width = 595, Height = 842 };
        vm.OpenForNew("NewChart", page);

        Assert.True(vm.IsOpen);
        Assert.True(vm.IsChartTarget);

        // Set raw text input and parse
        vm.RawTextInput = "Division\tTarget\nAmericas\t500\nEMEA\t400\nAPAC\t650";
        vm.ParseRawTextInput();

        Assert.Equal(3, vm.CurrentMatrix.RowCount);
        Assert.Equal(2, vm.CurrentMatrix.ColumnCount);
        Assert.Equal("Division", vm.MatrixHeaders[0]);
        Assert.Equal("Target", vm.MatrixHeaders[1]);

        // Add a row
        vm.AddRow();
        Assert.Equal(4, vm.CurrentMatrix.RowCount);

        // Apply Data and verify event callback
        ElementViewModelBase? createdElement = null;
        vm.OnElementCreated += (el, desc) => createdElement = el;

        vm.ApplyData();

        Assert.False(vm.IsOpen);
        Assert.NotNull(createdElement);
        Assert.IsType<ChartElementViewModel>(createdElement);

        var chartVm = (ChartElementViewModel)createdElement;
        Assert.Equal(4, chartVm.Bars.Count);
        Assert.Equal("Americas", chartVm.Bars[0].Category);
        Assert.Equal(500, chartVm.Bars[0].Value);
    }
}
