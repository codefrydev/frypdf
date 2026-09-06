using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Overlays;
using PdfEditorApp.Services.Plugins;
using Xunit;

namespace PdfEditorApp.Tests;

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
            Assert.NotNull(ticTacToe);
            Assert.Equal("Tic-Tac-Toe", ticTacToe.Name);
            Assert.Equal("Code Fry Dev", ticTacToe.Publisher);
            Assert.Equal("UI & Extensions", ticTacToe.Category);
            Assert.StartsWith("https://raw.githubusercontent.com/codefrydev/PDFCreator-resources/", ticTacToe.DownloadUrl);
            Assert.Contains("tictactoe", ticTacToe.Tags);

            // Verify merging with curated extensions
            var fullCatalog = await marketplace.GetCatalogAsync();
            Assert.Contains(fullCatalog, i => i.Id == "frypdf.overlay.snake");
            Assert.Contains(fullCatalog, i => i.Id == "com.frypdf.plugin.tictactoe");
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

        // Curated local extensions must remain accessible
        var catalog = await marketplace.GetCatalogAsync();
        Assert.NotEmpty(catalog);
        Assert.Contains(catalog, i => i.Id == "frypdf.overlay.snake");
    }

    [Fact]
    public async Task InstallPluginAsync_SupportsRemotePluginPackage_WithMockHttp()
    {
        var tempDir = Path.Combine(AppContext.BaseDirectory, $"frypdf_remote_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Use pre-packaged TicTacToe.fryplugin from examples
            var tictactoePkg = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../docs/examples/TicTacToePlugin/bin/Release/net10.0/TicTacToe.fryplugin"));
            byte[] pkgBytes;
            if (File.Exists(tictactoePkg))
            {
                pkgBytes = await File.ReadAllBytesAsync(tictactoePkg);
            }
            else
            {
                // Fallback: build staging directory
                var stagingDir = Path.Combine(tempDir, "staging");
                Directory.CreateDirectory(stagingDir);
                File.WriteAllText(Path.Combine(stagingDir, "plugin.json"), @"{""id"":""com.frypdf.plugin.tictactoe"",""name"":""Tic-Tac-Toe"",""version"":""1.0.0"",""entryPoint"":""TicTacToePlugin.dll""}");
                var tttDll = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../docs/examples/TicTacToePlugin/bin/Release/net10.0/TicTacToePlugin.dll"));
                if (File.Exists(tttDll))
                {
                    File.Copy(tttDll, Path.Combine(stagingDir, "TicTacToePlugin.dll"), true);
                }
                var mockPkgPath = Path.Combine(tempDir, "TicTacToe.fryplugin");
                System.IO.Compression.ZipFile.CreateFromDirectory(stagingDir, mockPkgPath);
                pkgBytes = await File.ReadAllBytesAsync(mockPkgPath);
            }

            // Mock HttpMessageHandler returning mock catalog and real package
            var mockHandler = new MockHttpMessageHandler(pkgBytes);
            using var httpClient = new HttpClient(mockHandler) { Timeout = TimeSpan.FromSeconds(5) };

            var sp = CreateTestServices(testDir: tempDir, httpClient: httpClient, registryBaseUrl: "https://mock.frypdf.dev/plugins");
            var marketplace = sp.GetRequiredService<IPluginMarketplaceService>();
            var host = sp.GetRequiredService<PluginHost>();

            // Fetch catalog
            var remoteItems = await marketplace.FetchRemoteCatalogAsync(forceRefresh: true);
            Assert.Contains(remoteItems, i => i.Id == "com.frypdf.plugin.tictactoe");
            var tttItem = remoteItems.First(i => i.Id == "com.frypdf.plugin.tictactoe");
            Assert.Equal("com.frypdf.plugin.tictactoe", tttItem.Id);

            // Install TicTacToe plugin
            string lastStatus = "";
            bool installed = await marketplace.InstallPluginAsync("com.frypdf.plugin.tictactoe", statusCallback: s => lastStatus = s);
            Assert.True(installed, $"Install failed with status: {lastStatus}");
            Assert.True(marketplace.IsPluginInstalled("com.frypdf.plugin.tictactoe"));

            // Uninstall
            bool uninstalled = await marketplace.UninstallPluginAsync("com.frypdf.plugin.tictactoe");
            Assert.True(uninstalled);
            Assert.False(marketplace.IsPluginInstalled("com.frypdf.plugin.tictactoe"));
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
                // Install from real CDN
                bool installed = await marketplace.InstallPluginAsync("com.frypdf.plugin.tictactoe");
                Assert.True(installed);
                Assert.True(marketplace.IsPluginInstalled("com.frypdf.plugin.tictactoe"));
                Assert.True(host.IsPluginActive("com.frypdf.plugin.tictactoe"));

                // Verify overlay opened
                Assert.True(overlayReg.IsOverlayVisible("com.frypdf.plugin.tictactoe"));

                // Clean uninstall
                bool uninstalled = await marketplace.UninstallPluginAsync("com.frypdf.plugin.tictactoe");
                Assert.True(uninstalled);
                Assert.False(marketplace.IsPluginInstalled("com.frypdf.plugin.tictactoe"));
                Assert.False(host.IsPluginActive("com.frypdf.plugin.tictactoe"));
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
        var sp = CreateTestServices();
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
    ""id"": ""com.frypdf.plugin.tictactoe"",
    ""name"": ""Tic-Tac-Toe"",
    ""publisher"": ""Code Fry Dev"",
    ""version"": ""1.0.0"",
    ""category"": ""UI & Extensions"",
    ""description"": ""Interactive floating Tic-Tac-Toe mini-game with local 2-Player mode and intelligent AI opponent."",
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
