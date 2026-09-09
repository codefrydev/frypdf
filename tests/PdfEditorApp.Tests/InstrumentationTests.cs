using System;
using System.Linq;
using System.Threading;
using PdfEditorApp.Services;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Tests for the navigation/duration instrumentation added to make multi-second UI stalls
/// visible in the diagnostic log.
/// </summary>
[Collection("AppLogService")]
public class InstrumentationTests
{
    /// <summary>Captures entries in memory so assertions don't depend on the shared singleton.</summary>
    private sealed class RecordingLog : IAppLogService
    {
        public System.Collections.Generic.List<AppLogEntry> Entries { get; } = new();

        public void Log(AppLogLevel level, string category, string message)
            => Entries.Add(new AppLogEntry { Level = level, Category = category, Message = message });

        public System.Collections.Generic.IReadOnlyList<AppLogEntry> GetSnapshot() => Entries;
        public void Clear() => Entries.Clear();
        public int Capacity => int.MaxValue;
    }

    // ─── LogDuration level escalation ───────────────────────────────────────

    [Fact]
    public void LogDuration_EscalatesToWarningAtOrAboveTheThreshold()
    {
        var log = new RecordingLog();

        log.LogDuration("Navigation", "fast", elapsedMs: 10, warnAboveMs: 250);
        log.LogDuration("Navigation", "at threshold", elapsedMs: 250, warnAboveMs: 250);
        log.LogDuration("Navigation", "slow", elapsedMs: 4200, warnAboveMs: 250);

        Assert.Equal(AppLogLevel.Info, log.Entries[0].Level);
        Assert.Equal(AppLogLevel.Warning, log.Entries[1].Level);
        Assert.Equal(AppLogLevel.Warning, log.Entries[2].Level);
    }

    [Fact]
    public void LogDuration_SetsTheLevelExplicitlyRatherThanByKeyword()
    {
        // AppLogService.ParseRaw classifies free-text by keyword; "took 4200ms" contains none
        // of its warning keywords, so a slow navigation would otherwise be filed as Debug.
        var (_, _, parsedLevel) = AppLogService.ParseRaw("[Navigation] Home -> Plugins took 4200ms.");
        Assert.Equal(AppLogLevel.Debug, parsedLevel);

        var log = new RecordingLog();
        log.LogDuration("Navigation", "Home -> Plugins took 4200ms.", 4200);
        Assert.Equal(AppLogLevel.Warning, log.Entries.Single().Level);
    }

    // ─── TimeOperation ──────────────────────────────────────────────────────

    [Fact]
    public void TimeOperation_LogsOnceOnDisposal()
    {
        var log = new RecordingLog();

        using (log.TimeOperation("Navigation", "OpenPage"))
        {
            Assert.Empty(log.Entries);
        }

        var entry = Assert.Single(log.Entries);
        Assert.Equal("Navigation", entry.Category);
        Assert.Contains("OpenPage took", entry.Message, StringComparison.Ordinal);
        Assert.Contains("ms", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TimeOperation_IsIdempotentAcrossRepeatedDisposal()
    {
        var log = new RecordingLog();

        var timer = log.TimeOperation("Navigation", "OpenPage");
        timer.Dispose();
        timer.Dispose();
        timer.Dispose();

        Assert.Single(log.Entries);
    }

    [Fact]
    public void TimeOperation_EscalatesWhenTheOperationExceedsTheThreshold()
    {
        var log = new RecordingLog();

        using (log.TimeOperation("Navigation", "SlowPage", warnAboveMs: 20))
        {
            Thread.Sleep(60);
        }

        Assert.Equal(AppLogLevel.Warning, log.Entries.Single().Level);
    }

    [Fact]
    public void TimeOperation_StaysInfoForAFastOperation()
    {
        var log = new RecordingLog();

        using (log.TimeOperation("Navigation", "FastPage", warnAboveMs: 5000))
        {
        }

        Assert.Equal(AppLogLevel.Info, log.Entries.Single().Level);
    }

    // ─── UI-thread watchdog ─────────────────────────────────────────────────

    [Fact]
    public void UiThreadWatchdog_IsInertWithoutAnApplication()
    {
        // No Avalonia Application in a unit-test host, so there is no dispatcher loop to probe.
        // The watchdog must stay quiet rather than throw or report phantom stalls.
        var log = new RecordingLog();
        using var watchdog = new UiThreadWatchdog(log);

        Thread.Sleep(120);

        Assert.Empty(log.Entries);
        Assert.Equal(0, watchdog.WorstStallMs);
    }

    [Fact]
    public void UiThreadWatchdog_DisposesCleanlyAndIsIdempotent()
    {
        var watchdog = new UiThreadWatchdog(new RecordingLog());
        watchdog.Dispose();
        watchdog.Dispose();
    }
}
