using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Manifests;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.Plugins.Loader;
using PdfEditorApp.Services;  // FryPdfPaths — writable-path resolver (MSIX-safe); AppLogService — diagnostic logging


namespace PdfEditorApp.Services.Plugins;

/// <summary>
/// Service providing access to the curated FryPDF Plugin Store and Marketplace.
/// Features real, functional extension packages with persistent history and 1-click mounting into the isolated plugin kernel.
/// </summary>
public class PluginMarketplaceService : IPluginMarketplaceService, IDisposable
{
    public const string DefaultRegistryBaseUrl = "https://raw.githubusercontent.com/codefrydev/PDFCreator-resources/refs/heads/main/plugins";

    private readonly PluginHost? _pluginHost;
    private readonly IOverlayRegistry? _overlayRegistry;
    private readonly IInstalledPluginStore _installedPluginStore;
    private readonly string _pluginsDirectory;
    private readonly string _registryBaseUrl;
    private readonly HttpClient _httpClient;
    private readonly HashSet<string> _installedMarketplaceIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Hard ceiling on a downloaded .fryplugin archive (64 MB).</summary>
    private const long MaxPackageBytes = 64L * 1024 * 1024;
    private readonly List<MarketplacePluginItem> _remoteExtensions = new();
    private readonly object _catalogLock = new();

    /// <summary>
    /// All extensions and plugins are discovered and downloaded dynamically from the online catalog.json registry.
    /// </summary>
    private static readonly List<MarketplacePluginItem> CuratedExtensions = new();

    public PluginMarketplaceService(
        PluginHost? pluginHost = null,
        IOverlayRegistry? overlayRegistry = null,
        IInstalledPluginStore? installedPluginStore = null,
        HttpClient? httpClient = null,
        string? registryBaseUrl = null,
        string? pluginsDirectory = null)
    {
        _pluginHost = pluginHost;
        _overlayRegistry = overlayRegistry;
        // Use FryPdfPaths so that on MSIX installs (read-only WindowsApps dir) the
        // plugins and data files land in %LocalAppData%\FryPDF\ instead.
        _pluginsDirectory = string.IsNullOrWhiteSpace(pluginsDirectory)
            ? FryPdfPaths.PluginsDirectory
            : pluginsDirectory;
        _installedPluginStore = installedPluginStore
            ?? new FileInstalledPluginStore(FryPdfPaths.InstalledPluginsJsonPath);
        _registryBaseUrl = string.IsNullOrWhiteSpace(registryBaseUrl) ? DefaultRegistryBaseUrl : registryBaseUrl.TrimEnd('/');
        // 15 seconds: GitHub CDN round-trip on a cold Windows boot (DNS + TLS handshake)
        // can easily exceed the old 6s limit, causing false "0 extensions" readings.
        _ownsHttpClient = httpClient == null;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        try
        {
            Directory.CreateDirectory(_pluginsDirectory);
            LoadDiskCatalogCache();
            ScanInstalledMarketplacePlugins();
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogWarning("PluginInstall", "Marketplace service initialization warning", ex);
        }
    }

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    /// <summary>
    /// Restores and activates previously installed plugins. Idempotent.
    /// </summary>
    /// <remarks>
    /// This used to run from the constructor, blocking on <c>EnablePluginAsync</c> with
    /// <c>GetAwaiter().GetResult()</c>. Because this is a DI singleton it was typically first
    /// resolved on the UI thread during startup, and enabling a plugin registers overlays and
    /// ribbon items that post back to the dispatcher — a self-deadlock. Call it after the main
    /// window exists and await it.
    /// </remarks>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            await RestorePersistedPluginsAsync(ct);
            _initialized = true;
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogWarning("PluginInstall", "Marketplace plugin restore warning", ex);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private bool _hasFetchedRemote;

    private void LoadDiskCatalogCache()
    {
        try
        {
            // Primary cache is in the writable plugins directory (FryPdfPaths-resolved).
            // No AppContext.BaseDirectory fallback — that path may be read-only on MSIX.
            var cacheFile = Path.Combine(_pluginsDirectory, "catalog_cache.json");

            if (File.Exists(cacheFile))
            {
                var json = File.ReadAllText(cacheFile);
                var cached = JsonSerializer.Deserialize<List<MarketplacePluginItem>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (cached != null && cached.Count > 0)
                {
                    lock (_catalogLock)
                    {
                        _remoteExtensions.Clear();
                        _remoteExtensions.AddRange(cached.Where(c => c != null && !string.IsNullOrEmpty(c.Id)));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogWarning("PluginInstall", "Failed to load disk catalog cache", ex);
        }
    }

    private static IFryPlugin? InstantiatePlugin(string pluginId)
    {
        return null;
    }

    private async Task RestorePersistedPluginsAsync(CancellationToken ct = default)
    {
        if (_pluginHost == null) return;

        var records = _installedPluginStore.GetAll();
        foreach (var rec in records)
        {
            if (!rec.IsEnabled) continue;

            var plugin = InstantiatePlugin(rec.PluginId);
            if (plugin != null)
            {
                if (_pluginHost.GetPluginState(rec.PluginId) == PluginState.Unloaded)
                {
                    _pluginHost.RegisterPlugin(plugin);
                }

                if (!_pluginHost.IsPluginActive(rec.PluginId))
                {
                    try
                    {
                        await _pluginHost.EnablePluginAsync(rec.PluginId, ct);
                    }
                    catch (Exception ex)
                    {
                        AppLogService.Instance.LogWarning("PluginInstall", $"Restore error for '{rec.PluginId}'", ex);
                    }
                }

                lock (_catalogLock) { _installedMarketplaceIds.Add(rec.PluginId); }

                if (rec.WasOverlayOpen)
                {
                    var overlayReg = _overlayRegistry ?? _pluginHost.Context.GetService<IOverlayRegistry>();
                    overlayReg?.ShowOverlay(rec.PluginId);
                }
            }
            else
            {
                // The startup directory scan (App.InitializePluginSystem) already discovers,
                // loads and mounts everything under the plugins directory. Reloading here
                // pulled a second copy of the assembly into a fresh collectible ALC and
                // registered a second plugin instance — so an external plugin was mounted
                // twice per launch, and anything it owned existed twice. For the music player
                // that meant two native audio engines and two open playback devices competing
                // for the same output. The IsEnabled branch above already guards on plugin
                // state; this branch did not.
                if (_pluginHost.GetPluginState(rec.PluginId) != PluginState.Unloaded)
                {
                    lock (_catalogLock) { _installedMarketplaceIds.Add(rec.PluginId); }

                    if (rec.WasOverlayOpen)
                    {
                        var alreadyLoadedOverlayReg = _overlayRegistry ?? _pluginHost.Context.GetService<IOverlayRegistry>();
                        alreadyLoadedOverlayReg?.ShowOverlay(rec.PluginId);
                    }

                    continue;
                }

                // Try restoring external downloaded plugin from plugins/{pluginId}/
                var pluginDir = Path.Combine(_pluginsDirectory, rec.PluginId);
                if (Directory.Exists(pluginDir))
                {
                    try
                    {
                        string? entryDll = null;
                        var manifestFile = Path.Combine(pluginDir, "plugin.json");
                        if (File.Exists(manifestFile))
                        {
                            var json = File.ReadAllText(manifestFile);
                            var manifest = JsonSerializer.Deserialize<PdfEditorApp.Core.Plugins.Manifests.PluginManifest>(json);
                            if (!string.IsNullOrWhiteSpace(manifest?.EntryPoint))
                            {
                                var candidate = Path.Combine(pluginDir, manifest.EntryPoint);
                                if (File.Exists(candidate)) entryDll = candidate;
                            }
                        }

                        entryDll ??= Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly).FirstOrDefault();
                        if (entryDll != null)
                        {
                            var pkg = PluginAssemblyLoader.LoadPluginAssembly(entryDll);
                            if (pkg.Plugins.Count > 0)
                            {
                                _pluginHost.RegisterPlugins(pkg.Plugins);
                                foreach (var p in pkg.Plugins)
                                {
                                    if (!_pluginHost.IsPluginActive(p.Id))
                                    {
                                        await _pluginHost.EnablePluginAsync(p.Id, ct);
                                    }
                                }

                                lock (_catalogLock) { _installedMarketplaceIds.Add(rec.PluginId); }

                                if (rec.WasOverlayOpen)
                                {
                                    var overlayReg = _overlayRegistry ?? _pluginHost.Context.GetService<IOverlayRegistry>();
                                    overlayReg?.ShowOverlay(rec.PluginId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogService.Instance.LogWarning("PluginInstall", $"Error restoring external plugin '{rec.PluginId}'", ex);
                    }
                }
            }
        }
    }

    private void ScanInstalledMarketplacePlugins()
    {
        // Build into a local set and swap it in under one lock, so a concurrent
        // IsPluginInstalled never observes a half-cleared collection. Mutating the shared
        // HashSet in place while UI plugin cards read it is undefined behavior, not just
        // a stale answer.
        var rebuilt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rec in _installedPluginStore.GetAll())
        {
            if (rec != null && rec.IsEnabled && !string.IsNullOrEmpty(rec.PluginId))
            {
                rebuilt.Add(rec.PluginId);
            }
        }

        if (_pluginHost != null)
        {
            foreach (var item in CuratedExtensions)
            {
                if (item != null && !string.IsNullOrEmpty(item.Id) && _pluginHost.IsPluginActive(item.Id))
                {
                    rebuilt.Add(item.Id);
                }
            }

            List<MarketplacePluginItem> snapshot;
            lock (_catalogLock)
            {
                snapshot = _remoteExtensions.Where(i => i != null && !string.IsNullOrEmpty(i.Id)).ToList();
            }

            foreach (var item in snapshot)
            {
                if (_pluginHost.IsPluginActive(item.Id))
                {
                    rebuilt.Add(item.Id);
                }
            }
        }

        lock (_catalogLock)
        {
            _installedMarketplaceIds.Clear();
            foreach (var id in rebuilt) _installedMarketplaceIds.Add(id);
        }
    }

    public async Task<IReadOnlyList<MarketplacePluginItem>> FetchRemoteCatalogAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (_hasFetchedRemote && !forceRefresh)
        {
            lock (_catalogLock)
            {
                return _remoteExtensions.Where(i => i != null && !string.IsNullOrEmpty(i.Id)).ToList();
            }
        }

        var catalogUrl = $"{_registryBaseUrl}/catalog.json";

        // Attempt the HTTP fetch with 1 automatic retry (2 s back-off) to handle cold-boot
        // DNS / TLS latency on Windows that can push the round-trip above 6 s on first open.
        const int maxAttempts = 2;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(catalogUrl, ct);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    var items = JsonSerializer.Deserialize<List<MarketplacePluginItem>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (items != null && items.Count > 0)
                    {
                        lock (_catalogLock)
                        {
                            foreach (var item in items.Where(i => i != null && !string.IsNullOrEmpty(i.Id)))
                            {
                                var existing = _remoteExtensions.FirstOrDefault(e => string.Equals(e?.Id, item.Id, StringComparison.OrdinalIgnoreCase));
                                if (existing != null)
                                    _remoteExtensions.Remove(existing);
                                _remoteExtensions.Add(item);
                            }
                        }

                        // Persist to local disk cache for fast/offline resilience
                        try
                        {
                            string mergedJson;
                            lock (_catalogLock)
                            {
                                mergedJson = JsonSerializer.Serialize(_remoteExtensions, new JsonSerializerOptions { WriteIndented = true });
                            }
                            // _pluginsDirectory is already FryPdfPaths-resolved (writable on MSIX)
                            var cacheFile = Path.Combine(_pluginsDirectory, "catalog_cache.json");
                            Directory.CreateDirectory(_pluginsDirectory);
                            File.WriteAllText(cacheFile, mergedJson);
                        }
                        catch { }
                    }
                    break; // Successful — no retry needed
                }
                else
                {
                    lock (_catalogLock)
                    {
                        if (_remoteExtensions.Count == 0)
                            LoadDiskCatalogCache();
                    }
                    break; // HTTP error (e.g. 404) — retrying won't help
                }
            }
            catch (Exception ex) when (attempt < maxAttempts &&
                                       !ct.IsCancellationRequested &&
                                       ex is HttpRequestException or TaskCanceledException)
            {
                // Transient network failure — wait 2 s then retry once
                AppLogService.Instance.LogWarning("PluginInstall", $"Catalog fetch attempt {attempt} failed; retrying", ex);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
            catch (Exception ex)
            {
                AppLogService.Instance.LogError("PluginInstall", "Remote catalog fetch failed", ex);
                lock (_catalogLock)
                {
                    if (_remoteExtensions.Count == 0)
                        LoadDiskCatalogCache();
                }
                break;
            }
        }

        _hasFetchedRemote = true;
        ScanInstalledMarketplacePlugins();
        List<MarketplacePluginItem> result;
        lock (_catalogLock)
        {
            foreach (var item in _remoteExtensions)
            {
                if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                item.Status = IsPluginInstalled(item.Id)
                    ? MarketplacePluginStatus.Installed
                    : MarketplacePluginStatus.Available;
            }
            result = _remoteExtensions.Where(i => i != null && !string.IsNullOrEmpty(i.Id)).ToList();
        }

        return result;
    }

    public Task<IReadOnlyList<MarketplacePluginItem>> GetCatalogAsync(CancellationToken ct = default)
    {
        ScanInstalledMarketplacePlugins();
        var allItems = new List<MarketplacePluginItem>(CuratedExtensions);
        List<MarketplacePluginItem> snapshot;
        lock (_catalogLock)
        {
            snapshot = _remoteExtensions.Where(i => i != null && !string.IsNullOrEmpty(i.Id)).ToList();
        }

        foreach (var remote in snapshot)
        {
            if (!allItems.Any(c => string.Equals(c?.Id, remote.Id, StringComparison.OrdinalIgnoreCase)))
            {
                allItems.Add(remote);
            }
        }

        foreach (var item in allItems)
        {
            item.Status = IsPluginInstalled(item.Id)
                ? MarketplacePluginStatus.Installed
                : MarketplacePluginStatus.Available;
        }
        return Task.FromResult<IReadOnlyList<MarketplacePluginItem>>(allItems);
    }

    public Task<IReadOnlyList<MarketplacePluginItem>> SearchAsync(string query, string? category = null, CancellationToken ct = default)
    {
        ScanInstalledMarketplacePlugins();
        List<MarketplacePluginItem> allItems;
        lock (_catalogLock)
        {
            allItems = _remoteExtensions.Where(i => i != null && !string.IsNullOrEmpty(i.Id)).ToList();
        }

        var q = query.Trim().ToLowerInvariant();
        var results = allItems.Where(item =>
        {
            if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (string.IsNullOrWhiteSpace(q)) return true;

            return (item.Name?.ToLowerInvariant().Contains(q) ?? false) ||
                   (item.Id?.ToLowerInvariant().Contains(q) ?? false) ||
                   (item.Publisher?.ToLowerInvariant().Contains(q) ?? false) ||
                   (item.Description?.ToLowerInvariant().Contains(q) ?? false) ||
                   (item.Tags?.Any(t => t.ToLowerInvariant().Contains(q)) ?? false);
        }).ToList();

        foreach (var item in results)
        {
            item.Status = IsPluginInstalled(item.Id)
                ? MarketplacePluginStatus.Installed
                : MarketplacePluginStatus.Available;
        }

        return Task.FromResult<IReadOnlyList<MarketplacePluginItem>>(results);
    }

    public async Task<bool> InstallPluginAsync(string pluginId, IProgress<double>? progress = null, Action<string>? statusCallback = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        MarketplacePluginItem? item;
        lock (_catalogLock)
        {
            item = CuratedExtensions.FirstOrDefault(e => string.Equals(e?.Id, pluginId, StringComparison.OrdinalIgnoreCase))
                   ?? _remoteExtensions.FirstOrDefault(e => string.Equals(e?.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        }

        // If not found in memory, try fetching remote catalog or fallback to known TicTacToe plugin
        if (item == null)
        {
            await FetchRemoteCatalogAsync(forceRefresh: false, ct);
            lock (_catalogLock)
            {
                item = _remoteExtensions.FirstOrDefault(e => string.Equals(e?.Id, pluginId, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (item == null)
        {
            var shortName = pluginId.Split('.').Last();
            item = new MarketplacePluginItem
            {
                Id = pluginId,
                Name = shortName,
                Publisher = "Community",
                Version = "1.0.0",
                Description = $"{shortName} plugin.",
                DownloadUrl = $"{_registryBaseUrl}/{pluginId}/{shortName}.fryplugin"
            };
        }

        // The id comes from remote catalog JSON and is used below to build the temp package
        // path, the install directory, and the uninstall directory that gets recursively deleted.
        if (!PluginIdValidator.IsValid(item.Id))
        {
            AppLogService.Instance.Log(AppLogLevel.Warning, "PluginInstall",
                $"Refused to install '{item.Id}': the catalog id is not a safe path segment.");
            statusCallback?.Invoke($"Refused to install '{item.Name}': unsafe plugin id.");
            item.Status = MarketplacePluginStatus.Available;
            return false;
        }

        item.Status = MarketplacePluginStatus.Installing;
        statusCallback?.Invoke($"Connecting to FryPDF Marketplace registry for '{item.Name}'...");
        progress?.Report(0.1);

        // Case A: Remote package download from registry
        if (!string.IsNullOrWhiteSpace(item.DownloadUrl))
        {
            statusCallback?.Invoke($"Downloading {item.FormattedSize} package archive from registry...");
            progress?.Report(0.2);

            var tempDir = Path.Combine(_pluginsDirectory, ".cache");
            Directory.CreateDirectory(tempDir);
            var tempPackagePath = Path.Combine(tempDir, $"{item.Id}.fryplugin");

            if (!IsAllowedDownloadUrl(item.DownloadUrl))
            {
                AppLogService.Instance.Log(AppLogLevel.Warning, "PluginInstall",
                    $"Refused to download '{item.Id}': '{item.DownloadUrl}' is not on the registry host.");
                statusCallback?.Invoke($"Refused to download '{item.Name}': untrusted download URL.");
                item.Status = MarketplacePluginStatus.Available;
                return false;
            }

            try
            {
                bool downloaded = false;
                try
                {
                    using (var response = await _httpClient.GetAsync(item.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            // Content-Length of 0 is not null, so "?? 1" did not guard it and the
                            // progress division produced Infinity.
                            long declaredLength = response.Content.Headers.ContentLength ?? 0;
                            if (declaredLength > MaxPackageBytes)
                            {
                                throw new InvalidOperationException(
                                    $"Package is {declaredLength} bytes, above the {MaxPackageBytes} byte limit.");
                            }

                            await using var stream = await response.Content.ReadAsStreamAsync(ct);
                            await using var fileStream = File.Create(tempPackagePath);

                            var buffer = new byte[8192];
                            long totalBytesRead = 0;
                            int bytesRead;

                            var reportStopwatch = Stopwatch.StartNew();
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                            {
                                totalBytesRead += bytesRead;
                                if (totalBytesRead > MaxPackageBytes)
                                {
                                    // A server that under-declares Content-Length must not be able
                                    // to stream unbounded data into the plugins directory.
                                    throw new InvalidOperationException(
                                        $"Package exceeded the {MaxPackageBytes} byte download limit.");
                                }

                                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);

                                if (declaredLength > 0)
                                {
                                    double fraction = (double)totalBytesRead / declaredLength;
                                    progress?.Report(0.2 + 0.5 * fraction);
                                    if (reportStopwatch.ElapsedMilliseconds >= 100 || totalBytesRead == declaredLength)
                                    {
                                        reportStopwatch.Restart();
                                        var dlStr = MarketplacePluginItem.FormatBytes(totalBytesRead);
                                        var totalStr = MarketplacePluginItem.FormatBytes(declaredLength);
                                        var pct = Math.Clamp((int)(fraction * 100), 0, 100);
                                        statusCallback?.Invoke($"Downloading {item.Name}: {dlStr} / {totalStr} ({pct}%)...");
                                    }
                                }
                                else
                                {
                                    if (reportStopwatch.ElapsedMilliseconds >= 100)
                                    {
                                        reportStopwatch.Restart();
                                        var dlStr = MarketplacePluginItem.FormatBytes(totalBytesRead);
                                        statusCallback?.Invoke($"Downloading {item.Name}: {dlStr} downloaded...");
                                    }
                                }
                            }
                            downloaded = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogService.Instance.LogWarning("PluginInstall", $"Remote package download failed for '{item.Id}'", ex);
                }

                if (!downloaded)
                {
                    item.Status = MarketplacePluginStatus.Available;
                    statusCallback?.Invoke($"Failed to download '{item.Name}' package from registry.");
                    AppLogService.Instance.Log(AppLogLevel.Warning, "PluginInstall",
                        $"Install aborted for '{item.Id}': download failed after {sw.ElapsedMilliseconds}ms.");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(item.Sha256))
                {
                    statusCallback?.Invoke("Verifying package SHA-256...");
                    var actual = await ComputeSha256Async(tempPackagePath, ct);
                    if (!string.Equals(actual, item.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        AppLogService.Instance.Log(AppLogLevel.Error, "PluginInstall",
                            $"SHA-256 mismatch for '{item.Id}': catalog declared {item.Sha256}, download was {actual}. Install aborted.");
                        statusCallback?.Invoke($"'{item.Name}' failed integrity verification and was not installed.");
                        item.Status = MarketplacePluginStatus.Available;
                        return false;
                    }
                }
                else
                {
                    // Say what is actually true. The catalog carries no digest for this entry,
                    // so unpacking below will load and execute unverified code.
                    AppLogService.Instance.Log(AppLogLevel.Warning, "PluginInstall",
                        $"Catalog entry '{item.Id}' carries no sha256; installing without integrity verification.");
                    statusCallback?.Invoke("No checksum published for this package — installing unverified.");
                }

                statusCallback?.Invoke("Unpacking package archive...");
                progress?.Report(0.75);

                var pkgResult = FryPluginPackageLoader.UnpackAndLoad(tempPackagePath, _pluginsDirectory);

                statusCallback?.Invoke("Mounting extension into isolated plugin kernel...");
                progress?.Report(0.85);

                if (_pluginHost != null)
                {
                    _pluginHost.RegisterPlugins(pkgResult.AssemblyPackage.Plugins);
                    foreach (var pkgPlugin in pkgResult.AssemblyPackage.Plugins)
                    {
                        if (!_pluginHost.IsPluginActive(pkgPlugin.Id))
                        {
                            await _pluginHost.EnablePluginAsync(pkgPlugin.Id, ct);
                        }
                    }

                    var overlayReg = _overlayRegistry ?? _pluginHost.Context.GetService<IOverlayRegistry>();
                    overlayReg?.ShowOverlay(item.Id);
                }

                lock (_catalogLock) { _installedMarketplaceIds.Add(item.Id); }
                _installedPluginStore.AddOrUpdate(new InstalledPluginRecord
                {
                    PluginId = item.Id,
                    Name = item.Name,
                    Version = item.Version,
                    InstalledAt = DateTime.UtcNow,
                    IsEnabled = true,
                    WasOverlayOpen = true
                });

                item.Status = MarketplacePluginStatus.Installed;
                statusCallback?.Invoke($"'{item.Name}' installed and activated successfully!");
                progress?.Report(1.0);
                AppLogService.Instance.Log(AppLogLevel.Info, "PluginInstall",
                    $"Installed '{item.Id}' ({pkgResult.AssemblyPackage.Plugins.Count} plugin(s)) via remote package in {sw.ElapsedMilliseconds}ms.");
                return true;
            }
            catch (Exception ex)
            {
                AppLogService.Instance.LogError("PluginInstall", $"Install failed for '{item.Name}' after {sw.ElapsedMilliseconds}ms", ex);
                item.Status = MarketplacePluginStatus.Available;
                statusCallback?.Invoke($"Failed to install '{item.Name}': {ex.Message}");
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPackagePath))
                    {
                        File.Delete(tempPackagePath);
                    }
                }
                catch (Exception ex)
                {
                    AppLogService.Instance.LogWarning("PluginInstall", $"Failed to delete temp package '{tempPackagePath}'", ex);
                }
            }
        }

        // Case B: Local/simulated installation for built-in plugins (Snake, Scratchpad, Telemetry)
        await Task.Delay(100, ct);

        statusCallback?.Invoke($"Preparing {item.FormattedSize} package components...");
        progress?.Report(0.35);
        await Task.Delay(150, ct);

        // Local/built-in components ship with the app; there is no download to verify.
        statusCallback?.Invoke("Preparing built-in extension components...");
        progress?.Report(0.65);
        await Task.Delay(100, ct);

        // Create installation folder in plugins/
        var targetDir = Path.Combine(_pluginsDirectory, item.Id);
        Directory.CreateDirectory(targetDir);

        var manifestPath = Path.Combine(targetDir, "plugin.json");
        // Serialize rather than interpolate: a catalog Name or Description containing a quote
        // or a backslash previously produced malformed (or attacker-shaped) JSON.
        var manifestContent = JsonSerializer.Serialize(
            new PluginManifest
            {
                Id = item.Id,
                Name = item.Name,
                Version = item.Version,
                Description = item.Description,
                Author = item.Publisher,
                EntryPoint = $"{item.Id}.dll",
                Icon = item.IconKind
            },
            new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(manifestPath, manifestContent, ct);

        statusCallback?.Invoke("Mounting extension into isolated plugin kernel...");
        progress?.Report(0.85);

        // Mount and activate real plugin into host if available
        var plugin = InstantiatePlugin(item.Id);
        if (plugin != null && _pluginHost != null)
        {
            if (_pluginHost.GetPluginState(item.Id) == PluginState.Unloaded)
            {
                _pluginHost.RegisterPlugin(plugin);
            }

            if (!_pluginHost.IsPluginActive(item.Id))
            {
                await _pluginHost.EnablePluginAsync(item.Id, ct);
            }

            var overlayReg = _overlayRegistry ?? _pluginHost.Context.GetService<IOverlayRegistry>();
            overlayReg?.ShowOverlay(item.Id);
        }

        lock (_catalogLock) { _installedMarketplaceIds.Add(item.Id); }
        _installedPluginStore.AddOrUpdate(new InstalledPluginRecord
        {
            PluginId = item.Id,
            Name = item.Name,
            Version = item.Version,
            InstalledAt = DateTime.UtcNow,
            IsEnabled = true,
            WasOverlayOpen = true
        });

        item.Status = MarketplacePluginStatus.Installed;

        statusCallback?.Invoke($"'{item.Name}' installed and activated successfully!");
        progress?.Report(1.0);
        AppLogService.Instance.Log(AppLogLevel.Info, "PluginInstall",
            $"Installed '{item.Id}' via local/simulated path in {sw.ElapsedMilliseconds}ms.");
        return true;
    }

    public async Task<bool> UninstallPluginAsync(string pluginId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        MarketplacePluginItem? item;
        lock (_catalogLock)
        {
            item = CuratedExtensions.FirstOrDefault(e => string.Equals(e?.Id, pluginId, StringComparison.OrdinalIgnoreCase))
                   ?? _remoteExtensions.FirstOrDefault(e => string.Equals(e?.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        }
        if (item != null)
        {
            item.Status = MarketplacePluginStatus.Available;
        }

        lock (_catalogLock) { _installedMarketplaceIds.Remove(pluginId); }
        _installedPluginStore.Remove(pluginId);

        if (_pluginHost != null)
        {
            var overlayReg = _overlayRegistry ?? _pluginHost.Context.GetService<IOverlayRegistry>();
            overlayReg?.HideOverlay(pluginId);

            if (_pluginHost.IsPluginActive(pluginId))
            {
                await _pluginHost.DisablePluginAsync(pluginId, ct);
            }
        }

        var targetDir = Path.Combine(_pluginsDirectory, pluginId);
        if (Directory.Exists(targetDir))
        {
            try
            {
                Directory.Delete(targetDir, recursive: true);
            }
            catch (Exception ex)
            {
                AppLogService.Instance.LogError("PluginInstall", $"Uninstall delete error for '{pluginId}'", ex);
            }
        }

        AppLogService.Instance.Log(AppLogLevel.Info, "PluginInstall",
            $"Uninstalled '{pluginId}' in {sw.ElapsedMilliseconds}ms.");
        return true;
    }

    public bool IsPluginInstalled(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return false;

        if (_installedPluginStore.IsInstalled(pluginId))
            return true;

        if (_pluginHost != null && _pluginHost.IsPluginActive(pluginId))
            return true;

        lock (_catalogLock) { return _installedMarketplaceIds.Contains(pluginId); }
    }

    public async Task<IReadOnlyList<MarketplacePluginItem>> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        var remote = await FetchRemoteCatalogAsync(forceRefresh: true, ct);
        var updateAvailable = new List<MarketplacePluginItem>();

        foreach (var remoteItem in remote)
        {
            var installed = _installedPluginStore.Get(remoteItem.Id);
            if (installed != null)
            {
                if (IsNewerVersion(remoteItem.Version, installed.Version))
                {
                    remoteItem.Status = MarketplacePluginStatus.UpdateAvailable;
                    updateAvailable.Add(remoteItem);
                }
            }
        }

        return updateAvailable;
    }

    /// <summary>
    /// True when <paramref name="downloadUrl"/> is an absolute HTTPS URL on the same host as
    /// the configured registry.
    /// </summary>
    /// <remarks>
    /// The URL arrives from catalog JSON. Without this check a compromised or mirrored catalog
    /// could point the installer — which downloads and then executes code — at any host.
    /// </remarks>
    internal bool IsAllowedDownloadUrl(string? downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl)) return false;
        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri)) return false;
        if (!Uri.TryCreate(_registryBaseUrl, UriKind.Absolute, out var registryUri)) return false;

        // Plaintext HTTP would let a network attacker swap the package for their own.
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        return string.Equals(uri.Host, registryUri.Host, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Computes the lowercase hex SHA-256 of a file.</summary>
    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Compares two version strings, tolerating semver pre-release and build metadata
    /// ("1.2.0-beta", "1.2.0+build.5") which <see cref="Version.TryParse(string, out Version)"/>
    /// rejects outright — previously any such plugin silently never reported an update.
    /// </summary>
    internal static bool IsNewerVersion(string? candidate, string? installed)
    {
        var candidateCore = ParseVersionCore(candidate);
        var installedCore = ParseVersionCore(installed);
        if (candidateCore == null || installedCore == null) return false;

        int coreComparison = candidateCore.CompareTo(installedCore);
        if (coreComparison != 0) return coreComparison > 0;

        // Same numeric core: a release supersedes a pre-release of the same version.
        bool candidatePre = HasPreRelease(candidate);
        bool installedPre = HasPreRelease(installed);
        return installedPre && !candidatePre;
    }

    private static bool HasPreRelease(string? version)
        => !string.IsNullOrWhiteSpace(version) && version.Contains('-');

    private static Version? ParseVersionCore(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;

        var core = version.Trim();
        int cut = core.IndexOfAny(new[] { '-', '+' });
        if (cut >= 0) core = core[..cut];

        return Version.TryParse(core, out var parsed) ? parsed : null;
    }

    private readonly bool _ownsHttpClient;
    private bool _isDisposed;

    /// <summary>
    /// Disposes the <see cref="HttpClient"/> this service created for itself.
    /// </summary>
    /// <remarks>
    /// The class owned an HttpClient but did not implement IDisposable, so its handler was
    /// never released. An injected client belongs to the caller and is left alone.
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }

        _initLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
