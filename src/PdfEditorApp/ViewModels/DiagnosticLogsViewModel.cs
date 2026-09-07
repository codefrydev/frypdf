using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels;

// ─── Per-category group shown in the log list ──────────────────────────────────

/// <summary>
/// Represents a collapsed group of <see cref="AppLogEntry"/> items sharing the same
/// <see cref="Category"/>. Displayed as a single expandable row in the log list.
/// </summary>
public sealed partial class LogGroupViewModel : ObservableObject
{
    public string Category { get; }

    // Raw entries — newest appended last
    private readonly List<AppLogEntry> _entries = [];

    [ObservableProperty]
    private bool _isExpanded;

    public LogGroupViewModel(string category, bool expandedByDefault = false)
    {
        Category = category;
        _isExpanded = expandedByDefault;
    }

    // ── Derived stats ─────────────────────────────────────────────────────────

    public int Count => _entries.Count;

    public AppLogLevel WorstLevel
    {
        get
        {
            if (_entries.Any(e => e.Level == AppLogLevel.Error))   return AppLogLevel.Error;
            if (_entries.Any(e => e.Level == AppLogLevel.Warning)) return AppLogLevel.Warning;
            if (_entries.Any(e => e.Level == AppLogLevel.Info))    return AppLogLevel.Info;
            return AppLogLevel.Debug;
        }
    }

    public string LastMessage  => _entries.Count > 0 ? _entries[^1].Message  : string.Empty;
    public DateTime LastTime   => _entries.Count > 0 ? _entries[^1].Timestamp : DateTime.MinValue;

    /// <summary>Entries to display in the expanded detail panel (newest first).</summary>
    public IReadOnlyList<AppLogEntry> Entries => _entries.AsReadOnly();

    // ── Mutation ──────────────────────────────────────────────────────────────

    public void Add(AppLogEntry entry)
    {
        _entries.Add(entry);
        // Auto-expand groups that receive errors so they are visible immediately
        if (entry.Level == AppLogLevel.Error && !IsExpanded)
            IsExpanded = true;
        Refresh();
    }

    public void TrimTo(int maxPerGroup)
    {
        if (_entries.Count > maxPerGroup)
            _entries.RemoveRange(0, _entries.Count - maxPerGroup);
        Refresh();
    }

    public void Clear()
    {
        _entries.Clear();
        Refresh();
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(WorstLevel));
        OnPropertyChanged(nameof(LastMessage));
        OnPropertyChanged(nameof(LastTime));
        OnPropertyChanged(nameof(Entries));
    }

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;
}

// ─── Main ViewModel ────────────────────────────────────────────────────────────

/// <summary>
/// ViewModel for the Diagnostic Logs page.
/// Log entries from <see cref="AppLogService"/> are grouped by category so that
/// high-frequency debug output (plugin loading, CDN sync, etc.) does not flood
/// the list — each category collapses to a single row showing count + worst level.
/// </summary>
public sealed partial class DiagnosticLogsViewModel : ObservableRecipient,
    IRecipient<NewLogEntryMessage>
{
    // Max individual entries kept per category group before old ones are trimmed
    private const int MaxPerGroup = 200;

    private readonly IAppLogService _logService;

    // Category → group (insertion-ordered via LinkedList)
    private readonly Dictionary<string, LogGroupViewModel> _groups = new(StringComparer.OrdinalIgnoreCase);

    // Observable collection of group VMs that the AXAML binds to
    private readonly ObservableCollection<LogGroupViewModel> _allGroups = [];

    // ── Filter state ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredGroups))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredGroups))]
    private bool _showDebug = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredGroups))]
    private bool _showInfo = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredGroups))]
    private bool _showWarning = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredGroups))]
    private bool _showError = true;

    [ObservableProperty]
    private string _copyButtonLabel = "Copy All";

    // ── Derived ───────────────────────────────────────────────────────────────

    public ObservableCollection<LogGroupViewModel> FilteredGroups
    {
        get
        {
            var query = _allGroups.AsEnumerable();

            // Level filter: only show groups whose worst level is enabled
            if (!ShowDebug)   query = query.Where(g => g.WorstLevel != AppLogLevel.Debug);
            if (!ShowInfo)    query = query.Where(g => g.WorstLevel != AppLogLevel.Info   || g.Count == 0);
            if (!ShowWarning) query = query.Where(g => g.WorstLevel != AppLogLevel.Warning);
            if (!ShowError)   query = query.Where(g => g.WorstLevel != AppLogLevel.Error);

            // Text search: show groups whose category or any message matches
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(g =>
                    g.Category.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    g.Entries.Any(e => e.Message.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            // Sort: errors first, then warnings, then by entry count descending
            return new ObservableCollection<LogGroupViewModel>(
                query.OrderByDescending(g => (int)g.WorstLevel)
                     .ThenByDescending(g => g.Count));
        }
    }

    public int TotalCount   => _allGroups.Sum(g => g.Count);
    public int ErrorCount   => _allGroups.Count(g => g.WorstLevel == AppLogLevel.Error);
    public int WarningCount => _allGroups.Count(g => g.WorstLevel == AppLogLevel.Warning);
    public int GroupCount   => _allGroups.Count;

    // ── Clipboard helper (injected from code-behind) ──────────────────────────
    public Func<string, System.Threading.Tasks.Task>? SetClipboardText { get; set; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public DiagnosticLogsViewModel(IAppLogService logService)
    {
        _logService = logService;
        IsActive = true;

        // Seed from the existing circular buffer
        foreach (var entry in logService.GetSnapshot())
            AddToGroup(entry);

        NotifyCounts();
    }

    // ── WeakReferenceMessenger recipient ──────────────────────────────────────

    public void Receive(NewLogEntryMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AddToGroup(message.Entry);
            OnPropertyChanged(nameof(FilteredGroups));
            NotifyCounts();
        }, DispatcherPriority.Background);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async System.Threading.Tasks.Task CopyAllAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"FryPDF Diagnostic Log — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('─', 80));

        // Chronological: all groups, all entries, sorted by timestamp
        var all = _allGroups
            .SelectMany(g => g.Entries)
            .OrderBy(e => e.Timestamp);

        foreach (var e in all)
            sb.AppendLine(e.FormattedLine);

        if (SetClipboardText != null)
            await SetClipboardText(sb.ToString());

        CopyButtonLabel = "✓ Copied!";
        await System.Threading.Tasks.Task.Delay(2000);
        CopyButtonLabel = "Copy All";
    }

    [RelayCommand]
    private void ClearLog()
    {
        _logService.Clear();
        foreach (var g in _allGroups) g.Clear();
        _groups.Clear();
        _allGroups.Clear();
        OnPropertyChanged(nameof(FilteredGroups));
        NotifyCounts();
    }

    [RelayCommand]
    private void ExpandAll()
    {
        foreach (var g in _allGroups) g.IsExpanded = true;
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var g in _allGroups) g.IsExpanded = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddToGroup(AppLogEntry entry)
    {
        if (!_groups.TryGetValue(entry.Category, out var group))
        {
            // Auto-expand error groups by default
            group = new LogGroupViewModel(entry.Category,
                expandedByDefault: entry.Level == AppLogLevel.Error);
            _groups[entry.Category] = group;
            _allGroups.Add(group);
        }

        group.Add(entry);
        group.TrimTo(MaxPerGroup);
    }

    private void NotifyCounts()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(GroupCount));
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
    }
}
