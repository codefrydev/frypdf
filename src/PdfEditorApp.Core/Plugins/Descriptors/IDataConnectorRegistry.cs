using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Data;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Contract for a data connector plugin that loads tabular datasets into a <see cref="DataMatrix"/>.
/// </summary>
public interface IDataConnector
{
    string ConnectorId { get; }
    string DisplayName { get; }
    string Description { get; }
    string IconKind { get; }
    IReadOnlyList<string> SupportedExtensions { get; }

    Task<DataMatrix> LoadDataAsync(Dictionary<string, string> parameters, CancellationToken ct = default);
}

/// <summary>
/// Registry for discovering and dispatching data connectors contributed by plugins.
/// </summary>
public interface IDataConnectorRegistry
{
    IDisposable RegisterConnector(IDataConnector connector);
    IDataConnector? GetConnector(string connectorId);
    IReadOnlyList<IDataConnector> GetAllConnectors();
    event Action? RegistryChanged;
}
