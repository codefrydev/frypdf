using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.Core.Plugins.Settings;
using PdfEditorApp.Plugins.Loader;
using PdfEditorApp.Services.Plugins;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PluginUninstallAllVersionsTests
{
    private class TestPlugin : IFryPlugin
    {
        public string Id { get; }
        public string Name { get; }
        public Version Version { get; }
        public string Description => "Test Description";
        public string Author => "Test Author";
        public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();
        public IReadOnlyList<Type> ProvidedServices => Array.Empty<Type>();
        public bool AutoOpenOverlay => false;

        public TestPlugin(string id, string name, string version)
        {
            Id = id;
            Name = name;
            Version = Version.Parse(version);
        }

        public Task ApplyAsync(IFryPluginContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task UninstallPluginAsync_CompletelyRemovesAllVersions_CacheArchives_Fallbacks_AndSettings()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"fry_uninstall_test_{Guid.NewGuid():N}");
        var pluginsDir = Path.Combine(tempRoot, "plugins");
        var dataDir = Path.Combine(tempRoot, "data");
        Directory.CreateDirectory(pluginsDir);
        Directory.CreateDirectory(dataDir);

        var pluginId = "com.frypdf.test.multi";
        var primaryDir = Path.Combine(pluginsDir, pluginId);

        // 1. Create multiple version directories on disk
        var v1Dir = Path.Combine(primaryDir, "1.0.0");
        var v2Dir = Path.Combine(primaryDir, "1.1.0");
        var v3Dir = Path.Combine(primaryDir, "2.0.0");
        Directory.CreateDirectory(v1Dir);
        Directory.CreateDirectory(v2Dir);
        Directory.CreateDirectory(v3Dir);

        File.WriteAllText(Path.Combine(v1Dir, "plugin.json"), "{\"id\":\"" + pluginId + "\",\"version\":\"1.0.0\"}");
        File.WriteAllBytes(Path.Combine(v1Dir, "TestPlugin.dll"), new byte[] { 0x4D, 0x5A });
        File.WriteAllText(Path.Combine(v2Dir, "plugin.json"), "{\"id\":\"" + pluginId + "\",\"version\":\"1.1.0\"}");
        File.WriteAllBytes(Path.Combine(v2Dir, "TestPlugin.dll"), new byte[] { 0x4D, 0x5A });
        File.WriteAllText(Path.Combine(v3Dir, "plugin.json"), "{\"id\":\"" + pluginId + "\",\"version\":\"2.0.0\"}");
        File.WriteAllBytes(Path.Combine(v3Dir, "TestPlugin.dll"), new byte[] { 0x4D, 0x5A });

        // Set one file as ReadOnly to test safe attribute stripping
        File.SetAttributes(Path.Combine(v2Dir, "TestPlugin.dll"), FileAttributes.ReadOnly);

        // 2. Create cached .fryplugin packages across versions
        var cacheDir = Path.Combine(pluginsDir, ".cache");
        Directory.CreateDirectory(cacheDir);
        var cachePkg1 = Path.Combine(cacheDir, $"{pluginId}_1.0.0.fryplugin");
        var cachePkg2 = Path.Combine(cacheDir, $"{pluginId}_1.1.0.fryplugin");
        var cachePkgOther = Path.Combine(cacheDir, "other.plugin_1.0.0.fryplugin");
        File.WriteAllBytes(cachePkg1, new byte[] { 1, 2, 3 });
        File.WriteAllBytes(cachePkg2, new byte[] { 4, 5, 6 });
        File.WriteAllBytes(cachePkgOther, new byte[] { 7, 8, 9 });

        // 3. Create a fallback locked directory
        var fallbackDir = Path.Combine(pluginsDir, $"{pluginId}_638000000000");
        Directory.CreateDirectory(fallbackDir);
        File.WriteAllText(Path.Combine(fallbackDir, "dummy.txt"), "fallback");

        // 4. Set up persistent stores and kernel
        var installedStorePath = Path.Combine(dataDir, "installed_plugins.json");
        var installedStore = new FileInstalledPluginStore(installedStorePath);
        installedStore.AddOrUpdate(new InstalledPluginRecord
        {
            PluginId = pluginId,
            Name = "Multi Version Test",
            Version = "2.0.0",
            ActiveVersion = "2.0.0",
            InstalledVersions = new List<string> { "1.0.0", "1.1.0", "2.0.0" },
            IsEnabled = true
        });

        var settingsPath = Path.Combine(dataDir, "plugins.settings.json");
        using var settingsStore = new FilePluginSettingsStore(settingsPath);
        settingsStore.SetSetting(pluginId, "Theme", "Dark");
        settingsStore.SetSetting(pluginId, "AutoSave", true);
        settingsStore.SetSetting("other.plugin", "Volume", 80);
        settingsStore.Save();

        var host = new PluginHost();
        var pluginInstance = new TestPlugin(pluginId, "Multi Version Test", "2.0.0");
        host.RegisterPlugin(pluginInstance);
        await host.StartAsync();

        var marketplace = new PluginMarketplaceService(
            pluginHost: host,
            installedPluginStore: installedStore,
            pluginSettingsStore: settingsStore,
            pluginsDirectory: pluginsDir);

        try
        {
            // Verify initial state
            Assert.True(Directory.Exists(primaryDir));
            Assert.True(Directory.Exists(v1Dir));
            Assert.True(Directory.Exists(v2Dir));
            Assert.True(Directory.Exists(v3Dir));
            Assert.True(Directory.Exists(fallbackDir));
            Assert.True(File.Exists(cachePkg1));
            Assert.True(File.Exists(cachePkg2));
            Assert.True(File.Exists(cachePkgOther));
            Assert.True(installedStore.IsInstalled(pluginId));
            Assert.True(host.IsPluginActive(pluginId));
            Assert.Equal("Dark", settingsStore.GetSetting(pluginId, "Theme", "Default"));

            var versionsBefore = PluginAssemblyLoader.GetInstalledVersionsOnDisk(pluginsDir, pluginId);
            Assert.Equal(3, versionsBefore.Count);

            // ACT: Execute complete uninstall
            bool success = await marketplace.UninstallPluginAsync(pluginId);
            Assert.True(success);

            // ASSERT 1: All versions and primary directory completely removed
            Assert.False(Directory.Exists(primaryDir), "Primary directory must be deleted.");
            Assert.False(Directory.Exists(v1Dir), "v1.0.0 directory must be deleted.");
            Assert.False(Directory.Exists(v2Dir), "v1.1.0 directory must be deleted.");
            Assert.False(Directory.Exists(v3Dir), "v2.0.0 directory must be deleted.");

            // ASSERT 2: Fallback directory completely removed
            Assert.False(Directory.Exists(fallbackDir), "Fallback directory must be deleted.");

            // ASSERT 3: Cached packages for this plugin deleted, unrelated plugin package intact
            Assert.False(File.Exists(cachePkg1), "Cached v1.0.0 package must be deleted.");
            Assert.False(File.Exists(cachePkg2), "Cached v1.1.0 package must be deleted.");
            Assert.True(File.Exists(cachePkgOther), "Unrelated plugin cache package must remain intact.");

            // ASSERT 4: Versions on disk reports empty
            var versionsAfter = PluginAssemblyLoader.GetInstalledVersionsOnDisk(pluginsDir, pluginId);
            Assert.Empty(versionsAfter);

            // ASSERT 5: Plugin unregistered from Kernel Host
            Assert.False(host.IsPluginActive(pluginId), "Plugin must not be active in host.");
            Assert.DoesNotContain(host.RegisteredPlugins, p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(host.LoadedPlugins, p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));

            // ASSERT 6: Persistent store records deleted
            Assert.False(installedStore.IsInstalled(pluginId), "Plugin must be removed from installed store.");
            Assert.Null(installedStore.Get(pluginId));

            // ASSERT 7: Plugin settings purged from settings store, unrelated settings intact
            Assert.Equal("Default", settingsStore.GetSetting(pluginId, "Theme", "Default"));
            Assert.Empty(settingsStore.GetPluginSettings(pluginId));
            Assert.Equal(80, settingsStore.GetSetting("other.plugin", "Volume", 0));
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task UninstallPluginAsync_ResetsCatalogAndDetailViewModelVersionStates()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"fry_vm_test_{Guid.NewGuid():N}");
        var pluginsDir = Path.Combine(tempRoot, "plugins");
        var dataDir = Path.Combine(tempRoot, "data");
        Directory.CreateDirectory(pluginsDir);
        Directory.CreateDirectory(dataDir);

        var pluginId = "com.frypdf.test.catalog";
        var host = new PluginHost();
        var installedStore = new FileInstalledPluginStore(Path.Combine(dataDir, "installed_plugins.json"));
        var settingsStore = new FilePluginSettingsStore(Path.Combine(dataDir, "plugins.settings.json"));

        var ver1 = new MarketplacePluginVersion { Version = "1.0.0", IsInstalled = true, IsActive = false };
        var ver2 = new MarketplacePluginVersion { Version = "1.1.0", IsInstalled = true, IsActive = true };
        var ver3 = new MarketplacePluginVersion { Version = "1.2.0", IsInstalled = false, IsActive = false };

        var item = new MarketplacePluginItem
        {
            Id = pluginId,
            Name = "Catalog Test Plugin",
            Publisher = "Test Publisher",
            Version = "1.1.0",
            Description = "Catalog Test",
            Status = MarketplacePluginStatus.Installed,
            Versions = new List<MarketplacePluginVersion> { ver3, ver2, ver1 }
        };
        item.SelectedVersion = ver2;

        var marketplace = new PluginMarketplaceService(
            pluginHost: host,
            installedPluginStore: installedStore,
            pluginSettingsStore: settingsStore,
            pluginsDirectory: pluginsDir);

        // Add item to remote extensions catalog in marketplace
        var remoteExtField = typeof(PluginMarketplaceService).GetField("_remoteExtensions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var remoteExt = (List<MarketplacePluginItem>?)remoteExtField?.GetValue(marketplace);
        remoteExt?.Add(item);

        // Create detail viewmodel
        var detailVm = PluginsManagerDetailViewModel.FromMarketplaceItem(item);
        detailVm.IsInstalled = true;
        detailVm.IsActive = true;
        detailVm.ActiveInstalledVersion = "1.1.0";
        detailVm.UpdateVersionState();

        try
        {
            // Verify initial states
            Assert.True(ver1.IsInstalled);
            Assert.True(ver2.IsInstalled);
            Assert.True(ver2.IsActive);
            Assert.True(item.IsInstalled);
            Assert.True(detailVm.IsInstalled);

            // Uninstall
            bool success = await marketplace.UninstallPluginAsync(pluginId);
            Assert.True(success);

            // Assert catalog item versions are all reset
            Assert.Equal(MarketplacePluginStatus.Available, item.Status);
            Assert.False(item.IsInstalled);
            Assert.False(ver1.IsInstalled);
            Assert.False(ver1.IsActive);
            Assert.False(ver2.IsInstalled);
            Assert.False(ver2.IsActive);
            Assert.False(ver3.IsInstalled);
            Assert.False(ver3.IsActive);
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }
    }
}
