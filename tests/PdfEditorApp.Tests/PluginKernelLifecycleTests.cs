using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Manifests;
using PdfEditorApp.Services.Plugins;
using PdfEditorApp.Services.Tools.Conversion;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Regression tests for Tier 2 plugin-kernel and marketplace defects.
/// </summary>
public class PluginKernelLifecycleTests
{
    // ─── 2.4 Scope effect tokens must actually unregister ───────────────────

    [Fact]
    public void RegisterEffect_Token_RunsAndUnregistersTheEffect()
    {
        using var scope = new PluginScope();
        int ran = 0;

        var token = scope.RegisterEffect(() => ran++);
        token.Dispose();

        // Previously Dispose only nulled a private field: the effect neither ran nor was
        // removed, so every scoped registration handle was silently inert.
        Assert.Equal(1, ran);

        // Disposing the scope must not run it a second time.
        scope.Dispose();
        Assert.Equal(1, ran);
    }

    [Fact]
    public void RegisterEffect_Token_IsIdempotent()
    {
        using var scope = new PluginScope();
        int ran = 0;

        var token = scope.RegisterEffect(() => ran++);
        token.Dispose();
        token.Dispose();
        token.Dispose();

        Assert.Equal(1, ran);
    }

    [Fact]
    public void ScopeDispose_StillRunsEffectsNotIndividuallyDisposed()
    {
        var scope = new PluginScope();
        var order = new List<int>();

        scope.RegisterEffect(() => order.Add(1));
        var second = scope.RegisterEffect(() => order.Add(2));
        scope.RegisterEffect(() => order.Add(3));

        second.Dispose();
        scope.Dispose();

        // LIFO for the survivors, and the individually disposed one ran when its token did.
        Assert.Equal(new[] { 2, 3, 1 }, order);
    }

    [Fact]
    public void ScopeDispose_UnwindsInLifoOrder()
    {
        var scope = new PluginScope();
        var order = new List<int>();

        scope.RegisterEffect(() => order.Add(1));
        scope.RegisterEffect(() => order.Add(2));
        scope.RegisterEffect(() => order.Add(3));

        scope.Dispose();

        Assert.Equal(new[] { 3, 2, 1 }, order);
    }

    // ─── 2.4 Re-registering an id must unwind the outgoing instance ─────────

    private sealed class CountingPlugin : IFryPlugin
    {
        private readonly Action _onUnwind;
        public CountingPlugin(string id, Action onUnwind) { Id = id; _onUnwind = onUnwind; }

        public string Id { get; }
        public string Name => Id;
        public Version Version => new(1, 0, 0);
        public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();
        public IReadOnlyList<Type> ProvidedServices => Array.Empty<Type>();
        public IReadOnlyDictionary<string, PluginSettingDefinition>? SettingsSchema => null;

        public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
        {
            ctx.RegisterEffect(_onUnwind);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task RegisterPlugin_ReplacingAnId_UnwindsTheOutgoingInstance()
    {
        int unwound = 0;
        var context = new FryPluginContext(null);
        var host = new PluginHost(context);

        host.RegisterPlugin(new CountingPlugin("dup.plugin", () => unwound++));
        await host.StartAsync();
        Assert.True(host.IsPluginActive("dup.plugin"));
        Assert.Equal(0, unwound);

        // Registering a different instance under the same id previously replaced the entry
        // while leaving the old one active, so its effects could never be unwound.
        host.RegisterPlugin(new CountingPlugin("dup.plugin", () => { }));

        Assert.Equal(1, unwound);
    }

    // ─── 2.2 Marketplace download URL allow-list ────────────────────────────

    [Theory]
    [InlineData("https://evil.example.com/pkg.fryplugin")]   // wrong host
    [InlineData("http://raw.githubusercontent.com/x.fryplugin")] // plaintext
    [InlineData("file:///etc/passwd")]
    [InlineData("not a url")]
    [InlineData("")]
    public void IsAllowedDownloadUrl_RejectsUntrustedUrls(string url)
    {
        var svc = new PluginMarketplaceService(registryBaseUrl: "https://raw.githubusercontent.com/acme/registry");
        Assert.False(svc.IsAllowedDownloadUrl(url));
    }

    [Fact]
    public void IsAllowedDownloadUrl_AcceptsTheRegistryHostOverHttps()
    {
        var svc = new PluginMarketplaceService(registryBaseUrl: "https://raw.githubusercontent.com/acme/registry");
        Assert.True(svc.IsAllowedDownloadUrl("https://raw.githubusercontent.com/acme/registry/x/x.fryplugin"));
    }

    // ─── 2.2 Semver-aware update detection ──────────────────────────────────

    [Theory]
    [InlineData("1.2.1", "1.2.0", true)]
    [InlineData("1.2.0", "1.2.0", false)]
    [InlineData("1.1.0", "1.2.0", false)]
    [InlineData("1.2.0", "1.2.0-beta", true)]     // release supersedes pre-release
    [InlineData("1.3.0-beta", "1.2.0", true)]     // pre-release of a higher version
    [InlineData("1.2.0+build.5", "1.1.0", true)]  // build metadata is ignored
    public void IsNewerVersion_HandlesSemverSuffixes(string candidate, string installed, bool expected)
    {
        // Version.TryParse rejects "-beta"/"+build" outright, so those plugins silently never
        // reported an available update.
        Assert.Equal(expected, PluginMarketplaceService.IsNewerVersion(candidate, installed));
    }

    // ─── 2.9 HTML fetch must refuse internal targets ────────────────────────

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")]  // cloud instance metadata
    [InlineData("http://localhost:8080/admin")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://10.0.0.5/internal")]
    [InlineData("http://192.168.1.1/router")]
    [InlineData("http://172.16.4.4/")]
    [InlineData("file:///etc/passwd")]
    [InlineData("httpsomething")]                              // matched the old StartsWith("http")
    public void IsFetchableHtmlUrl_RefusesInternalAndNonHttpTargets(string url)
    {
        Assert.False(PdfConversionService.IsFetchableHtmlUrl(url));
    }

    [Theory]
    [InlineData("https://example.com/page.html")]
    [InlineData("http://example.com/page.html")]
    [InlineData("https://8.8.8.8/page.html")]
    public void IsFetchableHtmlUrl_AllowsPublicHttpTargets(string url)
    {
        Assert.True(PdfConversionService.IsFetchableHtmlUrl(url));
    }
}
