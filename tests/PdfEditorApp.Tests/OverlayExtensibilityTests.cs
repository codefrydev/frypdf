using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Marketplace;
using PdfEditorApp.Plugins.Scratchpad;
using PdfEditorApp.Plugins.Snake;
using PdfEditorApp.Plugins.Telemetry;
using PdfEditorApp.Services.Overlays;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class OverlayExtensibilityTests
{
    [Fact]
    public void OverlayDescriptor_SupportsMultipleChromeModes()
    {
        var standardDesc = new OverlayDescriptor
        {
            Id = "test.standard",
            Title = "Standard Overlay",
            ChromeMode = OverlayChromeMode.StandardCard
        };
        Assert.False(standardDesc.HasCustomChrome);

        var customDesc = new OverlayDescriptor
        {
            Id = "test.custom",
            Title = "Custom Overlay",
            ChromeMode = OverlayChromeMode.CustomChrome
        };
        Assert.True(customDesc.HasCustomChrome);

        var pillDesc = new OverlayDescriptor
        {
            Id = "test.pill",
            Title = "Pill Overlay",
            ChromeMode = OverlayChromeMode.FloatingPill
        };
        Assert.Equal(OverlayChromeMode.FloatingPill, pillDesc.ChromeMode);
    }

    [Fact]
    public void BringToFront_SetsHighestZIndex_AcrossMultipleActiveOverlays()
    {
        var desc1 = new OverlayDescriptor { Id = "overlay.1", Title = "Card 1" };
        var desc2 = new OverlayDescriptor { Id = "overlay.2", Title = "Card 2" };
        var desc3 = new OverlayDescriptor { Id = "overlay.3", Title = "Card 3" };

        var inst1 = new OverlayInstanceViewModel(desc1) { ZIndex = 1 };
        var inst2 = new OverlayInstanceViewModel(desc2) { ZIndex = 2 };
        var inst3 = new OverlayInstanceViewModel(desc3) { ZIndex = 3 };

        var all = new[] { inst1, inst2, inst3 };

        // Bring inst1 to front
        inst1.BringToFront(all);
        Assert.True(inst1.ZIndex > inst2.ZIndex);
        Assert.True(inst1.ZIndex > inst3.ZIndex);
        Assert.Equal(4, inst1.ZIndex);

        // Bring inst2 to front
        inst2.BringToFront(all);
        Assert.Equal(5, inst2.ZIndex);
        Assert.True(inst2.ZIndex > inst1.ZIndex);
    }

    [Fact]
    public void FileInstalledPluginStore_PersistsAndLoadsRecords_Cleanly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_installed_{Guid.NewGuid():N}.json");
        try
        {
            var store = new FileInstalledPluginStore(tempFile);
            Assert.Empty(store.GetAll());
            Assert.False(store.IsInstalled("test.plugin"));

            var record = new InstalledPluginRecord
            {
                PluginId = "test.plugin",
                Name = "Test Plugin",
                Version = "2.1.0",
                IsEnabled = true,
                WasOverlayOpen = true
            };

            store.AddOrUpdate(record);
            Assert.True(store.IsInstalled("test.plugin"));
            Assert.Single(store.GetAll());

            // Reload from new instance pointing to same file
            var store2 = new FileInstalledPluginStore(tempFile);
            Assert.True(store2.IsInstalled("test.plugin"));
            var loaded = store2.Get("test.plugin");
            Assert.NotNull(loaded);
            Assert.Equal("Test Plugin", loaded.Name);
            Assert.Equal("2.1.0", loaded.Version);
            Assert.True(loaded.WasOverlayOpen);

            // Remove
            store2.Remove("test.plugin");
            Assert.False(store2.IsInstalled("test.plugin"));

            // Verify persistence of removal
            var store3 = new FileInstalledPluginStore(tempFile);
            Assert.False(store3.IsInstalled("test.plugin"));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task CompanionOverlays_CanInstallAndOperateSimultaneously()
    {
        var services = new ServiceCollection();
        App.ConfigureServices(services);
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_installed_{Guid.NewGuid():N}.json");
        services.AddSingleton<IInstalledPluginStore>(new FileInstalledPluginStore(tempFile));
        var sp = services.BuildServiceProvider();

        var host = sp.GetRequiredService<PluginHost>();
        var overlayReg = sp.GetRequiredService<OverlayRegistry>();
        var marketplace = sp.GetRequiredService<PdfEditorApp.Core.Plugins.Marketplace.IPluginMarketplaceService>();

        await host.StartAsync();

        // Install Scratchpad
        bool installed1 = await marketplace.InstallPluginAsync("frypdf.overlay.scratchpad");
        Assert.True(installed1);
        Assert.True(host.IsPluginActive("frypdf.overlay.scratchpad"));
        Assert.True(overlayReg.IsOverlayVisible("frypdf.overlay.scratchpad"));

        // Install Telemetry
        bool installed2 = await marketplace.InstallPluginAsync("frypdf.overlay.telemetry");
        Assert.True(installed2);
        Assert.True(host.IsPluginActive("frypdf.overlay.telemetry"));
        Assert.True(overlayReg.IsOverlayVisible("frypdf.overlay.telemetry"));

        // Both are active simultaneously
        Assert.Equal(2, overlayReg.ActiveOverlays.Count);
        Assert.Contains(overlayReg.ActiveOverlays, o => o.Id == "frypdf.overlay.scratchpad");
        Assert.Contains(overlayReg.ActiveOverlays, o => o.Id == "frypdf.overlay.telemetry");

        // Verify StandardCard chrome on both
        var scratchInst = overlayReg.ActiveOverlays.First(o => o.Id == "frypdf.overlay.scratchpad");
        var telemInst = overlayReg.ActiveOverlays.First(o => o.Id == "frypdf.overlay.telemetry");

        Assert.True(scratchInst.HasStandardChrome);
        Assert.True(telemInst.HasStandardChrome);

        // Test Pin and Minimize commands
        scratchInst.TogglePin();
        Assert.True(scratchInst.IsPinned);

        scratchInst.ToggleMinimize();
        Assert.True(scratchInst.IsMinimized);

        await host.StopAsync();
    }

    [Fact]
    public void ScratchpadViewModel_TracksCounts_AndAddsTimestamps()
    {
        var vm = new ScratchpadViewModel
        {
            NotesText = "Hello World PDF"
        };

        Assert.Equal(3, vm.WordCount);
        Assert.Equal(15, vm.CharacterCount);

        vm.AddTimestamp();
        Assert.Contains("Note (", vm.NotesText);
        Assert.True(vm.WordCount > 3);

        vm.ClearNotes();
        Assert.Equal(0, vm.WordCount);
        Assert.Equal(0, vm.CharacterCount);
    }

    [Fact]
    public void DocumentTelemetryViewModel_RefreshesMetrics_AndTrimsHeap()
    {
        var vm = new DocumentTelemetryViewModel();
        Assert.False(string.IsNullOrWhiteSpace(vm.MemoryAllocatedMb));
        Assert.True(vm.GarbageCollections >= 0);

        vm.RunGCTrim();
        Assert.False(string.IsNullOrWhiteSpace(vm.MemoryAllocatedMb));
    }
}
