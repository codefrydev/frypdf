using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Data;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Data;

public class DataConnectorRegistry : IDataConnectorRegistry
{
    private readonly ConcurrentDictionary<string, IDataConnector> _connectors = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public DataConnectorRegistry(IDataSourceService? dataSourceService = null)
    {
        RegisterBuiltInConnectors(dataSourceService ?? new DataSourceService());
    }

    private void RegisterBuiltInConnectors(IDataSourceService ds)
    {
        RegisterConnector(new CsvDataConnector(ds));
        RegisterConnector(new JsonDataConnector(ds));
        RegisterConnector(new ExcelDataConnector(ds));
        RegisterConnector(new RestApiDataConnector(ds));
    }

    public IDisposable RegisterConnector(IDataConnector connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        _connectors[connector.ConnectorId] = connector;
        RegistryChanged?.Invoke();

        return new DisposableAction(() =>
        {
            _connectors.TryRemove(connector.ConnectorId, out _);
            RegistryChanged?.Invoke();
        });
    }

    public IDataConnector? GetConnector(string connectorId)
    {
        if (string.IsNullOrWhiteSpace(connectorId)) return null;
        return _connectors.GetValueOrDefault(connectorId);
    }

    public IReadOnlyList<IDataConnector> GetAllConnectors()
    {
        return _connectors.Values.ToList();
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        private Action? _action = action;
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }
}

public class CsvDataConnector(IDataSourceService ds) : IDataConnector
{
    public string ConnectorId => "frypdf.connector.csv";
    public string DisplayName => "CSV / TSV Flat Files";
    public string Description => "Ingests comma-separated or tab-separated text files.";
    public string IconKind => "FileDelimitedOutline";
    public IReadOnlyList<string> SupportedExtensions => new[] { ".csv", ".tsv", ".txt" };

    public async Task<DataMatrix> LoadDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        if (parameters.TryGetValue("filePath", out var path) && File.Exists(path))
        {
            var text = await File.ReadAllTextAsync(path, ct);
            return ds.ParseCsv(text);
        }
        return new DataMatrix();
    }
}

public class JsonDataConnector(IDataSourceService ds) : IDataConnector
{
    public string ConnectorId => "frypdf.connector.json";
    public string DisplayName => "JSON Hierarchical Array";
    public string Description => "Extracts tabular matrices from JSON object arrays or nested JSON paths.";
    public string IconKind => "CodeJson";
    public IReadOnlyList<string> SupportedExtensions => new[] { ".json" };

    public async Task<DataMatrix> LoadDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        if (parameters.TryGetValue("filePath", out var path) && File.Exists(path))
        {
            var text = await File.ReadAllTextAsync(path, ct);
            parameters.TryGetValue("jsonPath", out var jsonPath);
            return ds.ParseJson(text, jsonPath);
        }
        return new DataMatrix();
    }
}

public class ExcelDataConnector(IDataSourceService ds) : IDataConnector
{
    public string ConnectorId => "frypdf.connector.excel";
    public string DisplayName => "Microsoft Excel Workbook";
    public string Description => "Imports sheets from modern .xlsx workbooks.";
    public string IconKind => "FileExcelOutline";
    public IReadOnlyList<string> SupportedExtensions => new[] { ".xlsx", ".xlsm" };

    public async Task<DataMatrix> LoadDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        if (parameters.TryGetValue("filePath", out var path) && File.Exists(path))
        {
            await using var stream = File.OpenRead(path);
            parameters.TryGetValue("sheetName", out var sheetName);
            return ds.ParseExcel(stream, sheetName);
        }
        return new DataMatrix();
    }
}

public class RestApiDataConnector(IDataSourceService ds) : IDataConnector
{
    public string ConnectorId => "frypdf.connector.rest";
    public string DisplayName => "REST API Endpoint";
    public string Description => "Fetches live data records from HTTP/HTTPS endpoints returning JSON arrays.";
    public string IconKind => "Api";
    public IReadOnlyList<string> SupportedExtensions => Array.Empty<string>();

    public Task<DataMatrix> LoadDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        if (parameters.TryGetValue("url", out var url) && !string.IsNullOrWhiteSpace(url))
        {
            parameters.TryGetValue("jsonPath", out var jsonPath);
            return ds.FetchFromRestApiAsync(url, null, jsonPath, ct);
        }
        return Task.FromResult(new DataMatrix());
    }
}
