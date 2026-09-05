using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PdfEditorApp.Core.Plugins.Marketplace;

/// <summary>
/// Service providing access to the curated FryPDF Plugin Store and Extensions Marketplace.
/// Enables discovery, searching, downloading, and package installation of official and community extensions.
/// </summary>
public interface IPluginMarketplaceService
{
    /// <summary>
    /// Retrieves all available extensions from the marketplace catalog.
    /// </summary>
    Task<IReadOnlyList<MarketplacePluginItem>> GetCatalogAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches the marketplace catalog by query keywords, category, or tags.
    /// </summary>
    Task<IReadOnlyList<MarketplacePluginItem>> SearchAsync(string query, string? category = null, CancellationToken ct = default);

    /// <summary>
    /// Simulates or executes downloading and installing a marketplace plugin package by ID.
    /// </summary>
    Task<bool> InstallPluginAsync(string pluginId, IProgress<double>? progress = null, Action<string>? statusCallback = null, CancellationToken ct = default);

    /// <summary>
    /// Uninstalls an installed marketplace plugin.
    /// </summary>
    Task<bool> UninstallPluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a specific plugin is installed locally.
    /// </summary>
    bool IsPluginInstalled(string pluginId);

    /// <summary>
    /// Checks for available updates across all installed marketplace plugins.
    /// </summary>
    Task<IReadOnlyList<MarketplacePluginItem>> CheckForUpdatesAsync(CancellationToken ct = default);
}
