using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Core.Data;

public interface IDataBindingService
{
    void ApplyToChart(DataMatrix matrix, PdfChartElement chart, int categoryColIndex = 0, List<int>? valueColIndices = null);
    void ApplyToTable(DataMatrix matrix, PdfTableElement table);
    DataMatrix ExtractFromTable(PdfTableElement table);
    DataMatrix ExtractFromChart(PdfChartElement chart);
    PdfChartElement ConvertTableToChart(PdfTableElement table, ChartType chartType = ChartType.BarColumn);
    PdfTableElement ConvertChartToTable(PdfChartElement chart);
}
