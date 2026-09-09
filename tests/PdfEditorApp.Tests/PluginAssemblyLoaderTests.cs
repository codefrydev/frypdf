using System;
using System.IO;
using System.Reflection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Plugins.Loader;
using PdfEditorApp.Services;
using Xunit;

namespace PdfEditorApp.Tests;

// Shares AppLogService.Instance's buffer with AppLogServiceTests — same collection to avoid races.
[Collection("AppLogService")]
public class PluginAssemblyLoaderTests
{
    [Fact]
    public void LoadPluginAssembly_ThrowsWhenFileNotFound()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.dll");
        Assert.Throws<FileNotFoundException>(() => PluginAssemblyLoader.LoadPluginAssembly(nonExistentPath));

        // Same control flow (still throws) — now also visible in the diagnostic log.
        var snapshot = AppLogService.Instance.GetSnapshot();
        Assert.Contains(snapshot, e =>
            e.Category == "PluginLoader" &&
            e.Level == AppLogLevel.Error &&
            e.Message.Contains(nonExistentPath));
    }

    [Fact]
    public void DiscoverAndLoadDirectory_HandlesNonExistentDirectoryGracefully()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"dir_{Guid.NewGuid():N}");
        var results = PluginAssemblyLoader.DiscoverAndLoadDirectory(nonExistentDir);
        Assert.Empty(results);
    }

    [Fact]
    public void DiscoverAndLoadDirectory_HandlesEmptyDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"plugins_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var results = PluginAssemblyLoader.DiscoverAndLoadDirectory(tempDir);
            Assert.Empty(results);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    // ─── Host contract resolution ───────────────────────────────────────────
    //
    // Plugins compile against PdfEditorApp.Core but never ship it, so it has to come from the
    // host. Release builds used to renumber the contract assembly (the pipeline's global
    // -p:AssemblyVersion reached it through the ProjectReference), which left installed builds
    // carrying e.g. 0.0.5.0 while every plugin asked for 1.0.0.0 — the runtime refuses to
    // substitute a lower version and reports it as "cannot find the file specified".

    /// <summary>
    /// The plugin ABI version is deliberately decoupled from the application's release version.
    /// If this fails, a build-configuration change has started floating the contract identity
    /// again and every already-published plugin is about to stop loading.
    /// </summary>
    [Fact]
    public void ContractAssembly_HasStableAbiVersion()
    {
        Assert.Equal(new Version(1, 0, 0, 0), typeof(IFryPlugin).Assembly.GetName().Version);
    }

    [Fact]
    public void PluginLoadContext_ResolvesHostContractToTheHostInstance()
    {
        var alc = new CollectiblePluginLoadContext(typeof(PluginAssemblyLoaderTests).Assembly.Location);

        var resolved = alc.LoadFromAssemblyName(new AssemblyName("PdfEditorApp.Core"));

        // Same instance, not merely the same name: a second copy would give IFryPlugin a distinct
        // type identity, and LoadPluginAssembly would silently discover zero plugins.
        Assert.Same(typeof(IFryPlugin).Assembly, resolved);
    }

    [Fact]
    public void PluginLoadContext_ResolvesHostContractDespiteVersionMismatch()
    {
        var alc = new CollectiblePluginLoadContext(typeof(PluginAssemblyLoaderTests).Assembly.Location);

        // A version the host certainly does not have — stands in for a plugin compiled against a
        // different build of the app, which is exactly the reported failure.
        var resolved = alc.LoadFromAssemblyName(new AssemblyName("PdfEditorApp.Core, Version=99.0.0.0"));

        Assert.Same(typeof(IFryPlugin).Assembly, resolved);
    }

    [Fact]
    public void PluginLoadContext_PrefersHostContractOverACopyInThePluginFolder()
    {
        // The test assembly's own folder contains a PdfEditorApp.Core.dll, so this covers the
        // case of a plugin package that wrongly bundles the contract: the host copy must still
        // win, which only holds while the host-contract check runs *before* the folder probe.
        string pluginPath = typeof(PluginAssemblyLoaderTests).Assembly.Location;
        string bundled = Path.Combine(Path.GetDirectoryName(pluginPath)!, "PdfEditorApp.Core.dll");
        Assert.True(File.Exists(bundled), "Expected a sibling PdfEditorApp.Core.dll to shadow.");

        var alc = new CollectiblePluginLoadContext(pluginPath);
        var resolved = alc.LoadFromAssemblyName(new AssemblyName("PdfEditorApp.Core"));

        Assert.Same(typeof(IFryPlugin).Assembly, resolved);
    }

    [Fact]
    public void PluginLoadContext_ResolvesHostProvidedDependencyDespiteVersionMismatch()
    {
        // Plugins reference the host's UI/MVVM stack with PrivateAssets="all" and never ship it,
        // so those requests hit the same version-drift wall as the contract assemblies.
        var alc = new CollectiblePluginLoadContext(typeof(PluginAssemblyLoaderTests).Assembly.Location);

        var resolved = alc.LoadFromAssemblyName(new AssemblyName("CommunityToolkit.Mvvm, Version=99.0.0.0"));

        Assert.NotNull(resolved);
        Assert.Equal("CommunityToolkit.Mvvm", resolved.GetName().Name);
    }

    [Fact]
    public void PluginAbiMismatchException_NamesBothVersionsAndTheRemedy()
    {
        var ex = new PluginAbiMismatchException(
            "PdfEditorApp.Core",
            new Version(1, 0, 0, 0),
            new Version(0, 0, 5, 0),
            new FileNotFoundException("Could not load file or assembly"));

        // The raw runtime error says "cannot find the file specified", which sends people looking
        // for a DLL that is present — the message has to carry both versions and what to do.
        Assert.Contains("PdfEditorApp.Core", ex.Message);
        Assert.Contains("1.0.0.0", ex.Message);
        Assert.Contains("0.0.5.0", ex.Message);
        Assert.Contains("Rebuild the plugin", ex.Message);
        Assert.IsType<FileNotFoundException>(ex.InnerException);
    }
}
