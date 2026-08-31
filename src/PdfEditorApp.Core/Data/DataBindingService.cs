using System;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Core.Data;

public class DataBindingService : IDataBindingService
{
    public void ApplyToChart(DataMatrix matrix, PdfChartElement chart, int categoryColIndex = 0, List<int>? valueColIndices = null)
    {
        if (matrix.RowCount == 0 || matrix.ColumnCount == 0) return;

        categoryColIndex = Math.Clamp(categoryColIndex, 0, matrix.ColumnCount - 1);
        valueColIndices ??= Enumerable.Range(0, matrix.ColumnCount).Where(i => i != categoryColIndex).ToList();
        if (valueColIndices.Count == 0)
        {
            valueColIndices.Add(categoryColIndex == 0 ? Math.Min(1, matrix.ColumnCount - 1) : 0);
        }

        var categories = matrix.GetColumnValues(categoryColIndex);
        var palette = LiveChartsRenderer.GetPaletteHexColors(chart.Palette);

        chart.Categories = categories;

        if (valueColIndices.Count == 1)
        {
            // Single Series mapping
            int valCol = valueColIndices[0];
            var rawValues = matrix.GetColumnValues(valCol);
            var values = new List<double>();
            var valueLabels = new List<string>();
            var barColors = new List<string>();

            for (int r = 0; r < rawValues.Count; r++)
            {
                string raw = rawValues[r];
                if (DataMatrix.TryParseNumeric(raw, out double num))
                {
                    values.Add(num);
                }
                else
                {
                    values.Add(0.0);
                }
                valueLabels.Add(raw);
                barColors.Add(palette[r % palette.Count]);
            }

            chart.Values = values;
            chart.ValueLabels = valueLabels;
            chart.BarColorsHex = barColors;
            chart.MultiSeries = new List<ChartSeriesItem>();

            if (string.IsNullOrEmpty(chart.Title) || chart.Title.Contains("Chart Analysis"))
            {
                chart.Title = matrix.Headers.ElementAtOrDefault(valCol) ?? "Chart Analysis";
            }
        }
        else
        {
            // Multi-Series mapping (e.g. Sales, Target, Expense)
            var multiSeriesList = new List<ChartSeriesItem>();
            var firstColValues = new List<double>();
            var firstColLabels = new List<string>();
            var barColors = new List<string>();

            for (int s = 0; s < valueColIndices.Count; s++)
            {
                int valCol = valueColIndices[s];
                string seriesName = matrix.Headers.ElementAtOrDefault(valCol) ?? $"Series {s + 1}";
                string seriesColor = palette[s % palette.Count];
                var rawValues = matrix.GetColumnValues(valCol);
                var numValues = new List<double>();

                for (int r = 0; r < rawValues.Count; r++)
                {
                    if (DataMatrix.TryParseNumeric(rawValues[r], out double num))
                    {
                        numValues.Add(num);
                    }
                    else
                    {
                        numValues.Add(0.0);
                    }

                    if (s == 0)
                    {
                        firstColValues.Add(numValues[r]);
                        firstColLabels.Add(rawValues[r]);
                        barColors.Add(palette[r % palette.Count]);
                    }
                }

                multiSeriesList.Add(new ChartSeriesItem
                {
                    Name = seriesName,
                    Values = numValues,
                    ColorHex = seriesColor
                });
            }

            chart.MultiSeries = multiSeriesList;
            chart.Values = firstColValues;
            chart.ValueLabels = firstColLabels;
            chart.BarColorsHex = barColors;
        }
    }

    public void ApplyToTable(DataMatrix matrix, PdfTableElement table)
    {
        if (matrix.ColumnCount == 0) return;

        table.Headers = new List<string>(matrix.Headers);
        table.Rows = new List<List<string>>();

        foreach (var r in matrix.Rows)
        {
            table.Rows.Add(new List<string>(r));
        }

        // Auto-scale height
        double calculatedHeight = Math.Max(table.Height, 35 + (matrix.RowCount * 26));
        table.Height = calculatedHeight;
    }

    public DataMatrix ExtractFromTable(PdfTableElement table)
    {
        var headers = new List<string>(table.Headers);
        var rows = new List<List<string>>();

        foreach (var r in table.Rows)
        {
            rows.Add(new List<string>(r));
        }

        return new DataMatrix(headers, rows);
    }

    public DataMatrix ExtractFromChart(PdfChartElement chart)
    {
        var headers = new List<string> { "Category" };
        var rows = new List<List<string>>();

        if (chart.MultiSeries != null && chart.MultiSeries.Count > 0)
        {
            foreach (var series in chart.MultiSeries)
            {
                headers.Add(series.Name);
            }

            for (int r = 0; r < chart.Categories.Count; r++)
            {
                var row = new List<string> { chart.Categories[r] };
                foreach (var series in chart.MultiSeries)
                {
                    row.Add(r < series.Values.Count ? series.Values[r].ToString("G") : "0");
                }
                rows.Add(row);
            }
        }
        else
        {
            headers.Add(string.IsNullOrWhiteSpace(chart.Title) ? "Value" : chart.Title);
            for (int r = 0; r < chart.Categories.Count; r++)
            {
                string cat = chart.Categories[r];
                string val = r < chart.ValueLabels.Count && !string.IsNullOrWhiteSpace(chart.ValueLabels[r])
                    ? chart.ValueLabels[r]
                    : (r < chart.Values.Count ? chart.Values[r].ToString("G") : "0");

                rows.Add(new List<string> { cat, val });
            }
        }

        return new DataMatrix(headers, rows);
    }

    public PdfChartElement ConvertTableToChart(PdfTableElement table, ChartType chartType = ChartType.BarColumn)
    {
        var matrix = ExtractFromTable(table);
        var chart = new PdfChartElement
        {
            X = table.X,
            Y = table.Y,
            Width = Math.Max(table.Width, 380),
            Height = Math.Max(table.Height, 220),
            Title = matrix.Headers.Count > 1 ? $"{matrix.Headers[1]} Analysis" : "Table Data Chart",
            ChartType = chartType,
            Palette = ChartPalette.CorporateBlue
        };

        // Determine category column (first text column) and numeric columns
        int catCol = 0;
        var valCols = new List<int>();

        for (int c = 0; c < matrix.ColumnCount; c++)
        {
            var type = matrix.InferColumnType(c);
            if (type == DataColumnType.Number || type == DataColumnType.Currency || type == DataColumnType.Percentage)
            {
                valCols.Add(c);
            }
        }

        if (valCols.Count == 0)
        {
            valCols = Enumerable.Range(1, Math.Max(0, matrix.ColumnCount - 1)).ToList();
        }

        ApplyToChart(matrix, chart, catCol, valCols);
        return chart;
    }

    public PdfTableElement ConvertChartToTable(PdfChartElement chart)
    {
        var matrix = ExtractFromChart(chart);
        var table = new PdfTableElement
        {
            X = chart.X,
            Y = chart.Y,
            Width = Math.Max(chart.Width, 350),
            Height = Math.Max(chart.Height, 35 + (matrix.RowCount * 26)),
            HeaderBackgroundHex = "#0F6CBD",
            HeaderTextHex = "#FFFFFF",
            AlternateRowBackgroundHex = "#F8F9FA",
            BorderColorHex = "#E1DFDD"
        };

        ApplyToTable(matrix, table);
        return table;
    }
}
