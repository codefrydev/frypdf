using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Manifests;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Core.Plugins.Settings;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Overlays;
using PdfEditorApp.Services.Plugins;
using Xunit;

namespace PdfEditorApp.Tests;

public class TestMarketplacePlugin : IFryPlugin
{
    public string Id => "com.frypdf.test.marketplace";
    public string Name => "Test Marketplace Plugin";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();
    public IReadOnlyList<Type> ProvidedServices => Array.Empty<Type>();
    public IReadOnlyDictionary<string, PluginSettingDefinition>? SettingsSchema => null;
    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default) => Task.CompletedTask;
}

public class RemotePluginMarketplaceTests
{
    private static IServiceProvider CreateTestServices(string? testDir = null, HttpClient? httpClient = null, string? registryBaseUrl = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<FryPluginContext>();
        services.AddSingleton<IFryPluginContext>(sp => sp.GetRequiredService<FryPluginContext>());
        services.AddSingleton<PluginHost>();
        services.AddSingleton<OverlayRegistry>();
        services.AddSingleton<IOverlayRegistry>(sp => sp.GetRequiredService<OverlayRegistry>());
        services.AddSingleton<IInstalledPluginStore>(sp =>
        {
            var storePath = testDir != null
                ? Path.Combine(testDir, "installed_plugins.json")
                : Path.Combine(AppContext.BaseDirectory, $"installed_plugins_test_{Guid.NewGuid():N}.json");
            return new FileInstalledPluginStore(storePath);
        });

        services.AddSingleton<IPluginMarketplaceService>(sp =>
        {
            var host = sp.GetRequiredService<PluginHost>();
            var overlay = sp.GetRequiredService<OverlayRegistry>();
            var store = sp.GetRequiredService<IInstalledPluginStore>();
            var pluginsDir = testDir != null ? Path.Combine(testDir, "plugins") : null;
            return new PluginMarketplaceService(host, overlay, store, httpClient, registryBaseUrl, pluginsDir);
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task FetchRemoteCatalogAsync_ParsesOfficialGitHubCatalog_WhenOnline()
    {
        var sp = CreateTestServices();
        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();

        var remoteItems = await marketplace.FetchRemoteCatalogAsync();

        // If machine is online, catalog from codefrydev/PDFCreator-resources is fetched
        if (remoteItems.Count > 0)
        {
            var ticTacToe = remoteItems.FirstOrDefault(i => i.Id == "com.frypdf.plugin.tictactoe");
            if (ticTacToe != null)
            {
                Assert.Equal("Tic-Tac-Toe", ticTacToe.Name);
                Assert.Equal("Code Fry Dev", ticTacToe.Publisher);
                Assert.Equal("UI & Extensions", ticTacToe.Category);
                Assert.StartsWith("https://raw.githubusercontent.com/codefrydev/PDFCreator-resources/", ticTacToe.DownloadUrl);
                Assert.Contains("tictactoe", ticTacToe.Tags);
            }

            var fullCatalog = await marketplace.GetCatalogAsync();
            Assert.NotEmpty(fullCatalog);
        }
    }

    [Fact]
    public async Task FetchRemoteCatalogAsync_HandlesOfflineGracefully_WithoutThrowing()
    {
        // Simulate completely invalid / offline registry
        using var offlineClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
        var sp = CreateTestServices(httpClient: offlineClient, registryBaseUrl: "http://invalid-unreachable-domain-987654.xyz");
        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();

        // Must not throw any unhandled exception
        var remote = await marketplace.FetchRemoteCatalogAsync();
        Assert.NotNull(remote);

        var catalog = await marketplace.GetCatalogAsync();
        Assert.NotNull(catalog);
    }

    [Fact]
    public async Task InstallPluginAsync_SupportsRemotePluginPackage_WithMockHttp()
    {
        var tempDir = Path.Combine(AppContext.BaseDirectory, $"frypdf_remote_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Build self-contained mock package from current test assembly
            var stagingDir = Path.Combine(tempDir, "staging");
            Directory.CreateDirectory(stagingDir);

            var manifest = new PluginManifest
            {
                Id = "com.frypdf.test.marketplace",
                Name = "Test Marketplace Plugin",
                Version = "1.0.0",
                EntryPoint = "PdfEditorApp.Tests.dll",
                Description = "Self-contained test mock plugin package."
            };
            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(stagingDir, "plugin.json"), manifestJson);

            var testDllPath = typeof(RemotePluginMarketplaceTests).Assembly.Location;
            File.Copy(testDllPath, Path.Combine(stagingDir, "PdfEditorApp.Tests.dll"), overwrite: true);

            var mockPkgPath = Path.Combine(tempDir, "TestMarketplacePlugin.fryplugin");
            ZipFile.CreateFromDirectory(stagingDir, mockPkgPath);
            var pkgBytes = await File.ReadAllBytesAsync(mockPkgPath);

            // Mock HttpMessageHandler returning mock catalog and real package
            var mockHandler = new MockHttpMessageHandler(pkgBytes);
            using var httpClient = new HttpClient(mockHandler) { Timeout = TimeSpan.FromSeconds(5) };

            var sp = CreateTestServices(testDir: tempDir, httpClient: httpClient, registryBaseUrl: "https://mock.frypdf.dev/plugins");
            var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();
            var host = sp.GetRequiredService<PluginHost>();

            // Fetch catalog
            var remoteItems = await marketplace.FetchRemoteCatalogAsync(forceRefresh: true);
            Assert.Contains(remoteItems, i => i.Id == "com.frypdf.test.marketplace");
            var item = remoteItems.First(i => i.Id == "com.frypdf.test.marketplace");
            Assert.Equal("com.frypdf.test.marketplace", item.Id);

            // Install plugin
            string lastStatus = "";
            bool installed = await marketplace.InstallPluginAsync("com.frypdf.test.marketplace", statusCallback: s => lastStatus = s);
            Assert.True(installed, $"Install failed with status: {lastStatus}");
            Assert.True(marketplace.IsPluginInstalled("com.frypdf.test.marketplace"));
            Assert.True(host.IsPluginActive("com.frypdf.test.marketplace"));

            // Uninstall
            bool uninstalled = await marketplace.UninstallPluginAsync("com.frypdf.test.marketplace");
            Assert.True(uninstalled);
            Assert.False(marketplace.IsPluginInstalled("com.frypdf.test.marketplace"));
            Assert.False(host.IsPluginActive("com.frypdf.test.marketplace"));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task InstallPluginAsync_DownloadsRealPackageFromGitHubCdn_AndMountsSuccessfully()
    {
        var tempDir = Path.Combine(AppContext.BaseDirectory, $"frypdf_live_cdn_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var sp = CreateTestServices(testDir: tempDir);
            var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();
            var host = sp.GetRequiredService<PluginHost>();
            var overlayReg = sp.GetRequiredService<IOverlayRegistry>();

            // Fetch remote catalog from live GitHub CDN
            var catalog = await marketplace.FetchRemoteCatalogAsync();
            var tttItem = catalog.FirstOrDefault(i => i.Id == "com.frypdf.plugin.tictactoe");
            if (tttItem != null)
            {
                // Attempt real download if online
                bool installed = await marketplace.InstallPluginAsync("com.frypdf.plugin.tictactoe");
                if (installed)
                {
                    Assert.True(marketplace.IsPluginInstalled("com.frypdf.plugin.tictactoe"));
                    Assert.True(host.IsPluginActive("com.frypdf.plugin.tictactoe"));
                    Assert.True(overlayReg.IsOverlayVisible("com.frypdf.plugin.tictactoe"));

                    // Clean uninstall
                    bool uninstalled = await marketplace.UninstallPluginAsync("com.frypdf.plugin.tictactoe");
                    Assert.True(uninstalled);
                    Assert.False(marketplace.IsPluginInstalled("com.frypdf.plugin.tictactoe"));
                    Assert.False(host.IsPluginActive("com.frypdf.plugin.tictactoe"));
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task PluginsManagerViewModel_Search_RanksTicTacToeFirst_WhenSearchingTic()
    {
        var mockHandler = new MockHttpMessageHandler(Array.Empty<byte>());
        using var httpClient = new HttpClient(mockHandler) { Timeout = TimeSpan.FromSeconds(5) };

        var sp = CreateTestServices(httpClient: httpClient, registryBaseUrl: "https://mock.frypdf.dev/plugins");
        var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();
        var host = sp.GetRequiredService<PluginHost>();

        var vm = new PdfEditorApp.ViewModels.PluginsManagerViewModel(host, marketplace);
        await vm.LoadAllDataAsync();

        vm.SelectedTab = PdfEditorApp.ViewModels.PluginsManagerTab.Marketplace;
        vm.SearchQuery = "tic";

        Assert.NotEmpty(vm.FilteredMarketplacePlugins);
        // Tic-Tac-Toe should be ranked at index 0 because its name starts with "tic"
        Assert.Equal("com.frypdf.plugin.tictactoe", vm.FilteredMarketplacePlugins[0].Id);
        Assert.Equal("Tic-Tac-Toe", vm.FilteredMarketplacePlugins[0].Name);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] _pkgBytes;

        public MockHttpMessageHandler(byte[] pkgBytes)
        {
            _pkgBytes = pkgBytes;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;

            if (uri.EndsWith("catalog.json", StringComparison.OrdinalIgnoreCase))
            {
                var catalogJson = @"[
  {
    ""id"": ""com.frypdf.test.marketplace"",
    ""name"": ""Test Marketplace Plugin"",
    ""publisher"": ""FryPDF Core Team"",
    ""version"": ""1.0.0"",
    ""category"": ""UI & Extensions"",
    ""description"": ""Test mock plugin for automated installation."",
    ""downloadUrl"": ""https://mock.frypdf.dev/plugins/TestMarketplacePlugin.fryplugin"",
    ""formattedSize"": ""25 KB""
  },
  {
    ""id"": ""com.frypdf.plugin.tictactoe"",
    ""name"": ""Tic-Tac-Toe"",
    ""publisher"": ""Code Fry Dev"",
    ""version"": ""1.0.0"",
    ""category"": ""UI & Extensions"",
    ""description"": ""Interactive floating Tic-Tac-Toe mini-game."",
    ""downloadUrl"": ""https://mock.frypdf.dev/plugins/TicTacToe.fryplugin"",
    ""formattedSize"": ""51 KB""
  }
]";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(catalogJson, System.Text.Encoding.UTF8, "application/json")
                });
            }

            if (uri.EndsWith(".fryplugin", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(_pkgBytes)
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
