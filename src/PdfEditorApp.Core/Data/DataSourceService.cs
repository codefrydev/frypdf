using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PdfEditorApp.Core.Data;

public class DataSourceService : IDataSourceService
{
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    public DataMatrix ParseCsv(string text, char? delimiter = null, bool firstRowIsHeader = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return new DataMatrix();

        char actualDelimiter = delimiter ?? DetectDelimiter(text);
        var parsedRows = ParseDelimitedText(text, actualDelimiter);

        if (parsedRows.Count == 0) return new DataMatrix();

        var headers = new List<string>();
        var rows = new List<List<string>>();

        int startRow = 0;
        int maxCols = parsedRows.Max(r => r.Count);

        if (firstRowIsHeader && parsedRows.Count > 0)
        {
            var headerRow = parsedRows[0];
            for (int i = 0; i < maxCols; i++)
            {
                string h = (i < headerRow.Count && !string.IsNullOrWhiteSpace(headerRow[i])) ? headerRow[i].Trim() : $"Column {i + 1}";
                headers.Add(h);
            }
            startRow = 1;
        }
        else
        {
            for (int i = 0; i < maxCols; i++)
            {
                headers.Add($"Column {i + 1}");
            }
        }

        for (int r = startRow; r < parsedRows.Count; r++)
        {
            var row = parsedRows[r];
            var normalizedRow = new List<string>();
            for (int c = 0; c < maxCols; c++)
            {
                normalizedRow.Add(c < row.Count ? row[c].Trim() : string.Empty);
            }
            // Skip entirely empty rows
            if (normalizedRow.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                rows.Add(normalizedRow);
            }
        }

        return new DataMatrix(headers, rows);
    }

    public DataMatrix ParseTsv(string text, bool firstRowIsHeader = true)
    {
        return ParseCsv(text, '\t', firstRowIsHeader);
    }

    public DataMatrix ParseJson(string jsonText, string? jsonPath = null)
    {
        if (string.IsNullOrWhiteSpace(jsonText)) return new DataMatrix();

        using var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        // Navigate jsonPath if supplied (e.g. "data.items" or "results")
        if (!string.IsNullOrWhiteSpace(jsonPath))
        {
            var segments = jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var segment in segments)
            {
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(segment, out var child))
                {
                    root = child;
                }
                else
                {
                    break;
                }
            }
        }

        // If root is an Object containing an array property (like {"data": [...]}), auto-unwrap the first array
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    root = prop.Value;
                    break;
                }
            }
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            // If single object, treat as 1-row matrix
            if (root.ValueKind == JsonValueKind.Object)
            {
                var headers = new List<string>();
                var row = new List<string>();
                foreach (var prop in root.EnumerateObject())
                {
                    headers.Add(prop.Name);
                    row.Add(prop.Value.ToString());
                }
                return new DataMatrix(headers, new[] { row });
            }
            return new DataMatrix();
        }

        var arrayItems = root.EnumerateArray().ToList();
        if (arrayItems.Count == 0) return new DataMatrix();

        // Case 1: Array of Objects: [{"colA": 1, "colB": 2}, ...]
        if (arrayItems[0].ValueKind == JsonValueKind.Object)
        {
            var headerSet = new HashSet<string>();
            var headers = new List<string>();

            // Collect all unique property names across all items
            foreach (var item in arrayItems)
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in item.EnumerateObject())
                    {
                        if (headerSet.Add(prop.Name))
                        {
                            headers.Add(prop.Name);
                        }
                    }
                }
            }

            var rows = new List<List<string>>();
            foreach (var item in arrayItems)
            {
                var row = new List<string>();
                if (item.ValueKind == JsonValueKind.Object)
                {
                    foreach (var h in headers)
                    {
                        if (item.TryGetProperty(h, out var val))
                        {
                            row.Add(val.ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }
                    }
                }
                rows.Add(row);
            }

            return new DataMatrix(headers, rows);
        }

        // Case 2: 2D Array: [["Header1", "Header2"], ["Val1", "Val2"]]
        if (arrayItems[0].ValueKind == JsonValueKind.Array)
        {
            var headers = new List<string>();
            var rows = new List<List<string>>();

            var firstRow = arrayItems[0].EnumerateArray().Select(e => e.ToString()).ToList();
            int maxCols = arrayItems.Max(item => item.ValueKind == JsonValueKind.Array ? item.GetArrayLength() : 0);

            for (int i = 0; i < maxCols; i++)
            {
                headers.Add(i < firstRow.Count && !string.IsNullOrWhiteSpace(firstRow[i]) ? firstRow[i] : $"Column {i + 1}");
            }

            for (int r = 1; r < arrayItems.Count; r++)
            {
                var item = arrayItems[r];
                var row = new List<string>();
                if (item.ValueKind == JsonValueKind.Array)
                {
                    var cells = item.EnumerateArray().Select(e => e.ToString()).ToList();
                    for (int c = 0; c < maxCols; c++)
                    {
                        row.Add(c < cells.Count ? cells[c] : string.Empty);
                    }
                }
                rows.Add(row);
            }

            return new DataMatrix(headers, rows);
        }

        // Case 3: 1D Primitive Array: [10, 20, 30, 40]
        {
            var headers = new List<string> { "Value" };
            var rows = arrayItems.Select(item => new List<string> { item.ToString() }).ToList();
            return new DataMatrix(headers, rows);
        }
    }

    public List<string> GetExcelSheetNames(Stream stream)
    {
        var names = new List<string>();
        try
        {
            using var doc = SpreadsheetDocument.Open(stream, false);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart?.Workbook?.Sheets != null)
            {
                foreach (Sheet sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
                {
                    if (!string.IsNullOrEmpty(sheet.Name))
                    {
                        names.Add(sheet.Name!);
                    }
                }
            }
        }
        catch
        {
            // Ignore format errors
        }
        return names;
    }

    public DataMatrix ParseExcel(Stream stream, string? sheetName = null, bool firstRowIsHeader = true)
    {
        try
        {
            using var doc = SpreadsheetDocument.Open(stream, false);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart == null) return new DataMatrix();

            var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

            Sheet? targetSheet = null;
            if (!string.IsNullOrEmpty(sheetName))
            {
                targetSheet = workbookPart.Workbook?.Sheets?.Elements<Sheet>()
                    .FirstOrDefault(s => string.Equals(s.Name, sheetName, StringComparison.OrdinalIgnoreCase));
            }
            targetSheet ??= workbookPart.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault();

            if (targetSheet == null || targetSheet.Id == null) return new DataMatrix();

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(targetSheet.Id!);
            var sheetData = worksheetPart.Worksheet?.Elements<SheetData>().FirstOrDefault();
            if (sheetData == null) return new DataMatrix();

            var parsedRows = new List<List<string>>();
            int maxColIndex = 0;

            foreach (Row row in sheetData.Elements<Row>())
            {
                var rowCells = new Dictionary<int, string>();
                foreach (Cell cell in row.Elements<Cell>())
                {
                    int colIndex = GetColumnIndexFromCellReference(cell.CellReference?.Value);
                    string cellValue = GetCellTextValue(cell, sharedStringTable);
                    rowCells[colIndex] = cellValue;
                    if (colIndex > maxColIndex) maxColIndex = colIndex;
                }

                var rowList = new List<string>();
                for (int c = 0; c <= maxColIndex; c++)
                {
                    rowList.Add(rowCells.TryGetValue(c, out var v) ? v : string.Empty);
                }
                parsedRows.Add(rowList);
            }

            if (parsedRows.Count == 0) return new DataMatrix();

            // Re-normalize all rows to have uniform column count
            int totalCols = maxColIndex + 1;
            var headers = new List<string>();
            var rows = new List<List<string>>();
            int startRow = 0;

            if (firstRowIsHeader && parsedRows.Count > 0)
            {
                var first = parsedRows[0];
                for (int c = 0; c < totalCols; c++)
                {
                    string h = (c < first.Count && !string.IsNullOrWhiteSpace(first[c])) ? first[c].Trim() : $"Column {c + 1}";
                    headers.Add(h);
                }
                startRow = 1;
            }
            else
            {
                for (int c = 0; c < totalCols; c++)
                {
                    headers.Add($"Column {c + 1}");
                }
            }

            for (int r = startRow; r < parsedRows.Count; r++)
            {
                var row = parsedRows[r];
                var normalizedRow = new List<string>();
                for (int c = 0; c < totalCols; c++)
                {
                    normalizedRow.Add(c < row.Count ? row[c] : string.Empty);
                }
                if (normalizedRow.Any(v => !string.IsNullOrWhiteSpace(v)))
                {
                    rows.Add(normalizedRow);
                }
            }

            return new DataMatrix(headers, rows);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse Excel workbook: {ex.Message}", ex);
        }
    }

    public async Task<DataMatrix> FetchFromRestApiAsync(
        string url,
        Dictionary<string, string>? headers = null,
        string? jsonPath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("REST API URL cannot be empty.", nameof(url));
        }

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "FryPDF-Studio/1.0");

        if (headers != null)
        {
            foreach (var kvp in headers)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    request.Headers.TryAddWithoutValidation(kvp.Key.Trim(), kvp.Value.Trim());
                }
            }
        }

        var response = await SharedHttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseJson(jsonContent, jsonPath);
    }

    public string FormatAsCsv(DataMatrix matrix, char delimiter = ',')
    {
        var sb = new StringBuilder();

        // Headers
        sb.AppendLine(string.Join(delimiter.ToString(), matrix.Headers.Select(h => EscapeCsvField(h, delimiter))));

        // Rows
        foreach (var row in matrix.Rows)
        {
            var escapedCells = new List<string>();
            for (int c = 0; c < matrix.ColumnCount; c++)
            {
                string cell = c < row.Count ? row[c] : string.Empty;
                escapedCells.Add(EscapeCsvField(cell, delimiter));
            }
            sb.AppendLine(string.Join(delimiter.ToString(), escapedCells));
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(string? field, char delimiter)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        if (field.Contains(delimiter) || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static char DetectDelimiter(string text)
    {
        var firstFewLines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Take(5).ToList();
        if (firstFewLines.Count == 0) return ',';

        int commas = firstFewLines.Sum(l => l.Count(c => c == ','));
        int tabs = firstFewLines.Sum(l => l.Count(c => c == '\t'));
        int semicolons = firstFewLines.Sum(l => l.Count(c => c == ';'));
        int pipes = firstFewLines.Sum(l => l.Count(c => c == '|'));

        if (tabs > commas && tabs > semicolons && tabs > pipes) return '\t';
        if (semicolons > commas && semicolons > tabs && semicolons > pipes) return ';';
        if (pipes > commas && pipes > tabs && pipes > semicolons) return '|';

        return ',';
    }

    private static List<List<string>> ParseDelimitedText(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var currentCell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        currentCell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentCell.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                }
                else if (c == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                }
                else if (c == '\n')
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                }
                else
                {
                    currentCell.Append(c);
                }
            }
        }

        if (currentCell.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentCell.ToString());
            rows.Add(currentRow);
        }

        return rows;
    }

    private static int GetColumnIndexFromCellReference(string? cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return 0;
        int colIndex = 0;
        foreach (char c in cellRef)
        {
            if (char.IsLetter(c))
            {
                colIndex = (colIndex * 26) + (char.ToUpperInvariant(c) - 'A' + 1);
            }
            else break;
        }
        return Math.Max(0, colIndex - 1);
    }

    private static string GetCellTextValue(Cell cell, SharedStringTable? sharedTable)
    {
        if (cell.CellValue == null)
        {
            return cell.InnerText ?? string.Empty;
        }

        string value = cell.CellValue.Text;
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            if (int.TryParse(value, out int sstIndex) && sharedTable != null)
            {
                var sstItem = sharedTable.Elements<SharedStringItem>().ElementAtOrDefault(sstIndex);
                return sstItem?.Text?.Text ?? sstItem?.InnerText ?? value;
            }
        }

        return value;
    }
}
