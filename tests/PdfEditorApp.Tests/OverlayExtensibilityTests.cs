using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Marketplace;
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
    public void OverlayRegistry_Registers_Shows_Toggles_And_Hides_Overlay()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var registry = new OverlayRegistry(services);

        bool registryChangedFired = false;
        registry.RegistryChanged += () => registryChangedFired = true;

        var desc = new OverlayDescriptor
        {
            Id = "test.overlay.custom",
            Title = "Custom Overlay",
            Slot = "shell.overlay",
            DefaultWidth = 300,
            DefaultHeight = 400,
            ViewFactory = _ => "MockView"
        };

        using var unreg = registry.RegisterOverlay(desc);
        Assert.True(registryChangedFired);
        Assert.NotNull(registry.GetOverlay("test.overlay.custom"));
        Assert.Single(registry.GetAllOverlays());

        // Initially not visible
        Assert.False(registry.IsOverlayVisible("test.overlay.custom"));

        // Show
        registry.ShowOverlay("test.overlay.custom");
        Assert.True(registry.IsOverlayVisible("test.overlay.custom"));
        Assert.Single(registry.ActiveOverlays);
        Assert.Equal("Custom Overlay", registry.ActiveOverlays[0].Title);

        // Toggle (hides)
        registry.ToggleOverlay("test.overlay.custom");
        Assert.False(registry.IsOverlayVisible("test.overlay.custom"));
        Assert.Empty(registry.ActiveOverlays);

        // Toggle (shows again)
        registry.ToggleOverlay("test.overlay.custom");
        Assert.True(registry.IsOverlayVisible("test.overlay.custom"));
        Assert.Single(registry.ActiveOverlays);

        // Hide
        registry.HideOverlay("test.overlay.custom");
        Assert.False(registry.IsOverlayVisible("test.overlay.custom"));
        Assert.Empty(registry.ActiveOverlays);
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
    public void CompanionOverlays_CanOperateSimultaneously_WithChromePinAndMinimize()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var overlayReg = new OverlayRegistry(services);

        var desc1 = new OverlayDescriptor
        {
            Id = "test.overlay.card1",
            Title = "Test Card 1",
            ChromeMode = OverlayChromeMode.StandardCard,
            ViewFactory = _ => "CardView1"
        };
        var desc2 = new OverlayDescriptor
        {
            Id = "test.overlay.card2",
            Title = "Test Card 2",
            ChromeMode = OverlayChromeMode.StandardCard,
            ViewFactory = _ => "CardView2"
        };

        using var reg1 = overlayReg.RegisterOverlay(desc1);
        using var reg2 = overlayReg.RegisterOverlay(desc2);

        overlayReg.ShowOverlay("test.overlay.card1");
        overlayReg.ShowOverlay("test.overlay.card2");

        Assert.Equal(2, overlayReg.ActiveOverlays.Count);
        Assert.Contains(overlayReg.ActiveOverlays, o => o.Id == "test.overlay.card1");
        Assert.Contains(overlayReg.ActiveOverlays, o => o.Id == "test.overlay.card2");

        var inst1 = overlayReg.ActiveOverlays.First(o => o.Id == "test.overlay.card1");
        var inst2 = overlayReg.ActiveOverlays.First(o => o.Id == "test.overlay.card2");

        Assert.True(inst1.HasStandardChrome);
        Assert.True(inst2.HasStandardChrome);

        inst1.TogglePin();
        Assert.True(inst1.IsPinned);

        inst1.ToggleMinimize();
        Assert.True(inst1.IsMinimized);
    }
}
