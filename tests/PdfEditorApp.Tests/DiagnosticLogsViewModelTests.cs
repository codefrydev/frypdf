using System;
using System.Linq;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

// Shares AppLogService.Instance's buffer with AppLogServiceTests — same collection to avoid races.
[Collection("AppLogService")]
public class DiagnosticLogsViewModelTests
{
    [Fact]
    public void ResyncAndActivate_CatchesUpOnEntriesLoggedWhileInactive()
    {
        var category = $"ResyncTest-{Guid.NewGuid():N}";
        var vm = new DiagnosticLogsViewModel(AppLogService.Instance);

        // Simulate the cached page being navigated away from — DiagnosticLogsDialog's
        // DetachedFromVisualTree handler sets this false to unregister from the messenger.
        vm.IsActive = false;

        // Logged elsewhere in the app while this page is detached/inactive — before the fix,
        // entries logged during this window were silently lost forever, even after navigating
        // back, because the ViewModel never re-registered with the messenger on reattach.
        AppLogService.Instance.Log(AppLogLevel.Info, category, "Missed while detached");

        var groupBeforeResync = vm.FilteredGroups.FirstOrDefault(g => g.Category == category);
        Assert.True(groupBeforeResync == null || !groupBeforeResync.Entries.Any(e => e.Message == "Missed while detached"));

        // Simulate navigating back to the page — DiagnosticLogsDialog's AttachedToVisualTree
        // handler calls this on every reattach, not just on first construction.
        vm.ResyncAndActivate();

        var groupAfterResync = vm.FilteredGroups.FirstOrDefault(g => g.Category == category);
        Assert.NotNull(groupAfterResync);
        Assert.Contains(groupAfterResync!.Entries, e => e.Message == "Missed while detached");
        Assert.True(vm.IsActive);
    }

    [Fact]
    public void LogGroupViewModel_Entries_ReturnsImmutableSnapshot_NotLiveView()
    {
        // List.AsReadOnly() wraps the same mutable list by reference — a later Add() would be
        // visible through an earlier-returned "snapshot" too, which is exactly what let a burst
        // of rapid Add() calls (e.g. ResyncAndActivate catching up on many missed entries) mutate
        // the list out from under Avalonia's ItemsControl mid-diff and crash with
        // ArgumentOutOfRangeException. Entries must hand out an independent copy every time.
        var group = new LogGroupViewModel("TestCat");
        group.Add(new AppLogEntry { Level = AppLogLevel.Info, Category = "TestCat", Message = "first" });

        var snapshot = group.Entries;
        Assert.Single(snapshot);

        group.Add(new AppLogEntry { Level = AppLogLevel.Info, Category = "TestCat", Message = "second" });

        Assert.Single(snapshot); // previously-returned snapshot must be unaffected by the later Add
        Assert.Equal(2, group.Entries.Count); // a fresh read reflects the new state
    }
}
