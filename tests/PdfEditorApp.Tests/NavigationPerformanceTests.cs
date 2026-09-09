using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Regression tests for the navigation-stall fixes.
/// </summary>
public class NavigationPerformanceTests
{
    // ─── ViewLocator type cache ─────────────────────────────────────────────

    [Fact]
    public void ViewLocator_ResolvesTheSameViewTypeAcrossCalls()
    {
        var locator = new ViewLocator();
        var vm = new SettingsViewModel();

        var first = locator.Build(vm);
        var second = locator.Build(vm);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.GetType(), second!.GetType());
    }

    [Fact]
    public void ViewLocator_ReturnsDistinctInstances()
    {
        // Only the type lookup is cached — controls cannot be shared between parents, so each
        // call must still produce its own instance.
        var locator = new ViewLocator();
        var vm = new SettingsViewModel();

        Assert.NotSame(locator.Build(vm), locator.Build(vm));
    }

    [Fact]
    public void ViewLocator_ReportsAMissingViewInsteadOfThrowing()
    {
        var locator = new ViewLocator();
        var control = locator.Build(new ViewModelWithNoView());

        var textBlock = Assert.IsType<Avalonia.Controls.TextBlock>(control);
        Assert.Contains("Not Found", textBlock.Text ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ViewLocator_MatchesOnlyViewModels()
    {
        var locator = new ViewLocator();

        Assert.True(locator.Match(new SettingsViewModel()));
        Assert.False(locator.Match("not a view model"));
        Assert.False(locator.Match(null));
    }

    private sealed class ViewModelWithNoView : ViewModelBase
    {
    }

    // ─── The installed plugin list must not wait on the network ─────────────

    /// <summary>A marketplace whose catalog fetch always fails, standing in for an outage.</summary>
    private sealed class FailingMarketplaceService : IPluginMarketplaceService
    {
        public int FetchAttempts { get; private set; }

        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<MarketplacePluginItem>> FetchRemoteCatalogAsync(
            bool forceRefresh = false, CancellationToken ct = default)
        {
            FetchAttempts++;
            throw new TimeoutException("registry unreachable");
        }

        public Task<IReadOnlyList<MarketplacePluginItem>> GetCatalogAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MarketplacePluginItem>>(Array.Empty<MarketplacePluginItem>());

        public Task<IReadOnlyList<MarketplacePluginItem>> SearchAsync(
            string query, string? category = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MarketplacePluginItem>>(Array.Empty<MarketplacePluginItem>());

        public Task<bool> InstallPluginAsync(string pluginId, IProgress<double>? progress = null,
            Action<string>? statusCallback = null, CancellationToken ct = default) => Task.FromResult(false);

        public Task<bool> UninstallPluginAsync(string pluginId, CancellationToken ct = default)
            => Task.FromResult(false);

        public bool IsPluginInstalled(string pluginId) => false;

        public Task<IReadOnlyList<MarketplacePluginItem>> CheckForUpdatesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MarketplacePluginItem>>(Array.Empty<MarketplacePluginItem>());
    }

    [Fact]
    public async Task LoadAllData_PublishesTheInstalledListEvenWhenTheCatalogFetchFails()
    {
        var context = new FryPluginContext(null);
        var host = new PluginHost(context);
        host.RegisterPlugin(new StubPlugin("test.plugin.one"));
        host.RegisterPlugin(new StubPlugin("test.plugin.two"));
        await host.StartAsync();

        var marketplace = new FailingMarketplaceService();
        var vm = new PluginsManagerViewModel(host, marketplace);

        await vm.LoadAllDataAsync();

        // ApplyFilters used to run only *after* the catalog await, so a registry outage left
        // the installed list and the detail pane empty for a purely local list.
        Assert.Equal(2, vm.FilteredInstalledPlugins.Count);
        Assert.NotNull(vm.SelectedInstalledPlugin);
        Assert.True(marketplace.FetchAttempts > 0, "the catalog fetch should still have been attempted");
    }

    private sealed class StubPlugin : IFryPlugin
    {
        public StubPlugin(string id) => Id = id;

        public string Id { get; }
        public string Name => Id;
        public Version Version => new(1, 0, 0);
        public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();
        public IReadOnlyList<Type> ProvidedServices => Array.Empty<Type>();
        public IReadOnlyDictionary<string, Core.Plugins.Manifests.PluginSettingDefinition>? SettingsSchema => null;
        public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default) => Task.CompletedTask;
    }

    // ─── Plugin card selection ──────────────────────────────────────────────

    [Fact]
    public async Task SelectingAnInstalledPlugin_MarksExactlyOneCardSelected()
    {
        var context = new FryPluginContext(null);
        var host = new PluginHost(context);
        host.RegisterPlugin(new StubPlugin("test.select.one"));
        host.RegisterPlugin(new StubPlugin("test.select.two"));
        await host.StartAsync();

        var vm = new PluginsManagerViewModel(host, new FailingMarketplaceService());
        await vm.LoadAllDataAsync();

        var target = vm.FilteredInstalledPlugins.Last();
        vm.SelectedInstalledPlugin = target;

        // The card used to compute this in XAML through ConverterParameter="{Binding}", which
        // Avalonia never evaluates — so highlighting could never turn on.
        Assert.True(target.IsSelected);
        Assert.Single(vm.FilteredInstalledPlugins, p => p.IsSelected);
    }
}
