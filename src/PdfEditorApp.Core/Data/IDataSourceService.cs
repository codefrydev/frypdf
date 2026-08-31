using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PdfEditorApp.Core.Data;

public interface IDataSourceService
{
    DataMatrix ParseCsv(string text, char? delimiter = null, bool firstRowIsHeader = true);
    DataMatrix ParseTsv(string text, bool firstRowIsHeader = true);
    DataMatrix ParseJson(string jsonText, string? jsonPath = null);
    DataMatrix ParseExcel(Stream stream, string? sheetName = null, bool firstRowIsHeader = true);
    List<string> GetExcelSheetNames(Stream stream);
    Task<DataMatrix> FetchFromRestApiAsync(string url, Dictionary<string, string>? headers = null, string? jsonPath = null, CancellationToken cancellationToken = default);
    string FormatAsCsv(DataMatrix matrix, char delimiter = ',');
}
