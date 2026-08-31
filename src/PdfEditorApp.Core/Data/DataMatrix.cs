using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfEditorApp.Core.Data;

public enum DataColumnType
{
    Text,
    Number,
    Currency,
    Percentage,
    Date
}

public class DataMatrix
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();

    public int ColumnCount => Headers.Count;
    public int RowCount => Rows.Count;

    public DataMatrix()
    {
    }

    public DataMatrix(IEnumerable<string> headers, IEnumerable<IEnumerable<string>>? rows = null)
    {
        Headers = new List<string>(headers);
        if (rows != null)
        {
            foreach (var row in rows)
            {
                Rows.Add(new List<string>(row));
            }
        }
    }

    public string GetCellValue(int rowIndex, int colIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count) return string.Empty;
        var row = Rows[rowIndex];
        if (colIndex < 0 || colIndex >= row.Count) return string.Empty;
        return row[colIndex] ?? string.Empty;
    }

    public string GetCell(int rowIndex, int colIndex) => GetCellValue(rowIndex, colIndex);
    public void SetCell(int rowIndex, int colIndex, string value) => SetCellValue(rowIndex, colIndex, value);

    public void SetCellValue(int rowIndex, int colIndex, string value)
    {
        while (rowIndex >= Rows.Count)
        {
            var newRow = new List<string>();
            while (newRow.Count < ColumnCount) newRow.Add(string.Empty);
            Rows.Add(newRow);
        }

        var targetRow = Rows[rowIndex];
        while (colIndex >= targetRow.Count)
        {
            targetRow.Add(string.Empty);
        }

        targetRow[colIndex] = value;
    }

    public List<string> GetColumnValues(int colIndex)
    {
        var result = new List<string>(RowCount);
        for (int r = 0; r < RowCount; r++)
        {
            result.Add(GetCellValue(r, colIndex));
        }
        return result;
    }

    public void AddRow(IEnumerable<string> cells)
    {
        var rowList = new List<string>(cells);
        while (rowList.Count < ColumnCount)
        {
            rowList.Add(string.Empty);
        }
        Rows.Add(rowList);
    }

    public void AddColumn(string header, IEnumerable<string>? initialValues = null)
    {
        Headers.Add(string.IsNullOrWhiteSpace(header) ? $"Col {ColumnCount + 1}" : header);
        var valuesList = initialValues?.ToList();

        for (int r = 0; r < RowCount; r++)
        {
            string val = (valuesList != null && r < valuesList.Count) ? valuesList[r] : string.Empty;
            Rows[r].Add(val);
        }
    }

    public void AddColumn(string header, string defaultValue)
    {
        Headers.Add(string.IsNullOrWhiteSpace(header) ? $"Col {ColumnCount + 1}" : header);
        for (int r = 0; r < RowCount; r++)
        {
            Rows[r].Add(defaultValue ?? string.Empty);
        }
    }

    public void RemoveRow(int rowIndex)
    {
        if (rowIndex >= 0 && rowIndex < Rows.Count)
        {
            Rows.RemoveAt(rowIndex);
        }
    }

    public void RemoveColumn(int colIndex)
    {
        if (colIndex >= 0 && colIndex < Headers.Count)
        {
            Headers.RemoveAt(colIndex);
            foreach (var row in Rows)
            {
                if (colIndex < row.Count)
                {
                    row.RemoveAt(colIndex);
                }
            }
        }
    }

    public DataColumnType InferColumnType(int colIndex)
    {
        if (RowCount == 0) return DataColumnType.Text;

        int numCount = 0;
        int currencyCount = 0;
        int pctCount = 0;
        int dateCount = 0;
        int sampleCount = Math.Min(RowCount, 50);

        for (int r = 0; r < sampleCount; r++)
        {
            string val = GetCellValue(r, colIndex).Trim();
            if (string.IsNullOrEmpty(val)) continue;

            if (val.EndsWith("%") && TryParseNumeric(val.TrimEnd('%'), out _))
            {
                pctCount++;
            }
            else if ((val.StartsWith("$") || val.StartsWith("€") || val.StartsWith("£") || val.StartsWith("¥") || val.StartsWith("₹")) &&
                     TryParseNumeric(val.Substring(1), out _))
            {
                currencyCount++;
            }
            else if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                dateCount++;
            }
            else if (TryParseNumeric(val, out _))
            {
                numCount++;
            }
        }

        if (pctCount > sampleCount * 0.6) return DataColumnType.Percentage;
        if (currencyCount > sampleCount * 0.6) return DataColumnType.Currency;
        if (dateCount > sampleCount * 0.6) return DataColumnType.Date;
        if (numCount > sampleCount * 0.6) return DataColumnType.Number;

        return DataColumnType.Text;
    }

    public static bool TryParseNumeric(string? raw, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        string sanitized = Regex.Replace(raw.Trim(), @"[$,€£¥₹\s]", "");
        if (sanitized.EndsWith("%"))
        {
            sanitized = sanitized.TrimEnd('%');
            if (double.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
            {
                result = pct;
                return true;
            }
        }

        return double.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ||
               double.TryParse(sanitized, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
    }

    public DataMatrix Clone()
    {
        var clone = new DataMatrix(Headers);
        foreach (var r in Rows)
        {
            clone.Rows.Add(new List<string>(r));
        }
        return clone;
    }
}
