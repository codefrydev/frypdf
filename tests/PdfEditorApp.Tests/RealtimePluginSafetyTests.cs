using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Settings;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Overlays;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Regressions for the host behaviours a plugin running real-time work depends on.
/// </summary>
/// <remarks>
/// The music player plugin plays audio on a CLR-attached thread (SoundFlow's miniaudio callback
/// runs managed code), so it is suspended by every GC and blocked by any lock the UI thread
/// holds. Each test here pins one host-side cause of the audio stuttering during editing.
/// </remarks>
public class RealtimePluginSafetyTests
{
    // ─── Overlay teardown ───────────────────────────────────────────────────

    /// <summary>Records disposal so a test can tell whether the host tore the plugin down.</summary>
    private sealed class DisposableContent : IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    private static OverlayDescriptor Descriptor(Func<IServiceProvider, object> factory) => new()
    {
        Id = "test.overlay.realtime",
        Title = "Realtime Overlay",
        Slot = "shell.overlay",
        ViewFactory = factory
    };

    [Fact]
    public void HideOverlay_ReusesTheSameInstance_InsteadOfBuildingASecondOne()
    {
        // Hiding used to remove the instance from the registry without disposing it, so the
        // next show fell through to the view factory and built a whole new plugin instance —
        // for the music player, another native audio engine and another open playback device
        // per toggle, none of them ever released.
        var services = new ServiceCollection().BuildServiceProvider();
        var registry = new OverlayRegistry(services);

        int factoryCalls = 0;
        using var unreg = registry.RegisterOverlay(Descriptor(_ =>
        {
            factoryCalls++;
            return new DisposableContent();
        }));

        registry.ShowOverlay("test.overlay.realtime");
        Assert.Equal(1, factoryCalls);

        for (int i = 0; i < 5; i++)
        {
            registry.HideOverlay("test.overlay.realtime");
            Assert.False(registry.IsOverlayVisible("test.overlay.realtime"));

            registry.ShowOverlay("test.overlay.realtime");
            Assert.True(registry.IsOverlayVisible("test.overlay.realtime"));
        }

        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public void HideOverlay_DoesNotDisposeContent_SoPlaybackSurvivesClosingThePanel()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var registry = new OverlayRegistry(services);
        var content = new DisposableContent();

        using var unreg = registry.RegisterOverlay(Descriptor(_ => content));

        registry.ShowOverlay("test.overlay.realtime");
        registry.HideOverlay("test.overlay.realtime");

        Assert.Equal(0, content.DisposeCount);
    }

    [Fact]
    public void UnregisterOverlay_DisposesContent_ReleasingPluginHeldResources()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var registry = new OverlayRegistry(services);
        var content = new DisposableContent();

        registry.RegisterOverlay(Descriptor(_ => content));
        registry.ShowOverlay("test.overlay.realtime");

        registry.UnregisterOverlay("test.overlay.realtime");

        Assert.Equal(1, content.DisposeCount);
    }

    [Fact]
    public void DisposingTheRegistry_TearsDownEveryCachedInstance()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var content = new DisposableContent();

        using (var registry = new OverlayRegistry(services))
        {
            registry.RegisterOverlay(Descriptor(_ => content));
            registry.ShowOverlay("test.overlay.realtime");
            registry.HideOverlay("test.overlay.realtime");
        }

        Assert.Equal(1, content.DisposeCount);
    }

    // ─── Plugin settings store ──────────────────────────────────────────────

    [Fact]
    public void SetSetting_DoesNotWriteToDiskSynchronously()
    {
        // SetSetting used to call Save() inline, so every set re-serialized the whole
        // dictionary and did a blocking File.WriteAllText. The music player writes four keys
        // per volume change, so dragging the slider issued hundreds of whole-file writes per
        // second on the UI thread, contending with the audio thread's own reads.
        var path = Path.Combine(Path.GetTempPath(), $"plugin_settings_{Guid.NewGuid():N}.json");
        try
        {
            using var store = new FilePluginSettingsStore(path);

            store.SetSetting("test.plugin", "Volume", 80);

            Assert.False(File.Exists(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Save_FlushesImmediately_ForCallersThatNeedADurabilityPoint()
    {
        var path = Path.Combine(Path.GetTempPath(), $"plugin_settings_{Guid.NewGuid():N}.json");
        try
        {
            using var store = new FilePluginSettingsStore(path);

            store.SetSetting("test.plugin", "Volume", 80);
            store.Save();

            Assert.True(File.Exists(path));
            Assert.Equal(80, new FilePluginSettingsStore(path).GetSetting("test.plugin", "Volume", 0));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Dispose_FlushesAPendingWrite_SoTheLastChangeIsNotLost()
    {
        var path = Path.Combine(Path.GetTempPath(), $"plugin_settings_{Guid.NewGuid():N}.json");
        try
        {
            using (var store = new FilePluginSettingsStore(path))
            {
                store.SetSetting("test.plugin", "RepeatMode", "RepeatAll");
                Assert.False(File.Exists(path));
            }

            Assert.True(File.Exists(path));
            Assert.Equal("RepeatAll",
                new FilePluginSettingsStore(path).GetSetting("test.plugin", "RepeatMode", ""));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void PendingWrite_IsFlushedByTheCoalescingTimer()
    {
        var path = Path.Combine(Path.GetTempPath(), $"plugin_settings_{Guid.NewGuid():N}.json");
        try
        {
            using var store = new FilePluginSettingsStore(path);
            store.SetSetting("test.plugin", "Volume", 55);

            // The store coalesces on a 500ms quiet period; allow generous slack for CI.
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!File.Exists(path) && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(50);
            }

            Assert.True(File.Exists(path), "coalesced write never reached disk");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ─── Trace listener ─────────────────────────────────────────────────────

    [Fact]
    public void TraceListener_DeclaresItselfThreadSafe_SoTraceTakesNoGlobalLock()
    {
        // TraceListener.IsThreadSafe defaults to false and Trace.UseGlobalLock defaults to
        // true, which serialized every Debug.WriteLine in the process on one lock. A trace call
        // from a plugin's real-time thread could then block behind UI-thread log traffic.
        var listener = new FryPdfTraceListener(AppLogService.Instance);

        Assert.True(listener.IsThreadSafe);
    }

    // ─── Real-time work coordinator ─────────────────────────────────────────

    [Fact]
    public void BeginRealtimeWork_RaisesGcLatencyMode_AndRestoresItOnTheLastRelease()
    {
        var original = GCSettings.LatencyMode;
        if (original == GCLatencyMode.NoGCRegion)
        {
            return; // Cannot leave a NoGCRegion via LatencyMode; nothing to assert.
        }

        var coordinator = new RealtimeWorkCoordinator(new NullLog());
        Assert.False(coordinator.IsRealtimeWorkActive);

        var outer = coordinator.BeginRealtimeWork("outer");
        Assert.True(coordinator.IsRealtimeWorkActive);
        Assert.Equal(GCLatencyMode.SustainedLowLatency, GCSettings.LatencyMode);

        var inner = coordinator.BeginRealtimeWork("inner");
        inner.Dispose();

        // Still one declaration outstanding, so the mode must not have been restored.
        Assert.True(coordinator.IsRealtimeWorkActive);
        Assert.Equal(GCLatencyMode.SustainedLowLatency, GCSettings.LatencyMode);

        outer.Dispose();
        Assert.False(coordinator.IsRealtimeWorkActive);
        Assert.Equal(original, GCSettings.LatencyMode);

        // Disposing twice must not underflow the count and re-raise the mode.
        outer.Dispose();
        Assert.False(coordinator.IsRealtimeWorkActive);
        Assert.Equal(original, GCSettings.LatencyMode);
    }

    private sealed class NullLog : IAppLogService
    {
        public System.Collections.Generic.IReadOnlyList<AppLogEntry> GetSnapshot()
            => Array.Empty<AppLogEntry>();
        public void Log(AppLogLevel level, string category, string message) { }
        public void Clear() { }
        public int Capacity => 0;
    }
}
