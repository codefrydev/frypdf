using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;

namespace PdfEditorApp.Services;

// ─── Log entry model ───────────────────────────────────────────────────────────

public enum AppLogLevel { Debug, Info, Warning, Error }

public sealed class AppLogEntry
{
    private static long _seq;

    public long     Sequence  { get; } = Interlocked.Increment(ref _seq);
    public DateTime Timestamp { get; } = DateTime.Now;
    public AppLogLevel Level  { get; init; } = AppLogLevel.Debug;
    public string   Category  { get; init; } = string.Empty;
    public string   Message   { get; init; } = string.Empty;

    /// <summary>Full single-line representation suitable for copy-paste.</summary>
    public string FormattedLine =>
        $"[{Timestamp:HH:mm:ss.fff}] [{Level,-7}] [{Category}] {Message}";
}

// ─── Message sent over WeakReferenceMessenger ──────────────────────────────────

public sealed class NewLogEntryMessage(AppLogEntry entry)
{
    public AppLogEntry Entry { get; } = entry;
}

// ─── Service interface ─────────────────────────────────────────────────────────

public interface IAppLogService
{
    /// <summary>Returns a snapshot of the current bounded log buffer.</summary>
    IReadOnlyList<AppLogEntry> GetSnapshot();

    /// <summary>Appends a log entry programmatically (e.g. from catch blocks).</summary>
    void Log(AppLogLevel level, string category, string message);

    /// <summary>Clears all buffered entries.</summary>
    void Clear();

    /// <summary>Maximum entries retained in the circular buffer.</summary>
    int Capacity { get; }
}

// ─── Singleton implementation ──────────────────────────────────────────────────

/// <summary>
/// Thread-safe in-process diagnostic log service.
/// Installs a <see cref="FryPdfTraceListener"/> so that every
/// <see cref="System.Diagnostics.Debug.WriteLine"/> call already present in the codebase
/// is automatically captured without modifying call sites.
///
/// Entries are kept in a bounded circular buffer (default 500) to avoid unbounded memory growth.
/// New entries are broadcast via <see cref="WeakReferenceMessenger"/> so the
/// <see cref="ViewModels.DiagnosticLogsViewModel"/> can update reactively with zero polling.
/// </summary>
public sealed class AppLogService : IAppLogService, IDisposable
{
    public static readonly AppLogService Instance = new();

    private const int DefaultCapacity = 500;

    private readonly object _lock = new();
    private readonly LinkedList<AppLogEntry> _buffer = new();
    private readonly FryPdfTraceListener _listener;

    public int Capacity { get; } = DefaultCapacity;

    private AppLogService()
    {
        _listener = new FryPdfTraceListener(this);
        Trace.Listeners.Add(_listener);
    }

    // ── IAppLogService ──────────────────────────────────────────────────────────

    public IReadOnlyList<AppLogEntry> GetSnapshot()
    {
        lock (_lock)
        {
            return _buffer.ToList();
        }
    }

    public void Log(AppLogLevel level, string category, string message)
    {
        var entry = new AppLogEntry { Level = level, Category = category, Message = message };
        Enqueue(entry);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }
        // Notify ViewModel that the list changed
        WeakReferenceMessenger.Default.Send(new NewLogEntryMessage(
            new AppLogEntry { Level = AppLogLevel.Info, Category = "AppLog", Message = "Log cleared." }));
    }

    // ── Internal ────────────────────────────────────────────────────────────────

    internal void Append(string rawMessage)
    {
        // Parse the [Category] prefix produced by existing Debug.WriteLine calls:
        //   "[PluginMarketplaceService] Init warning: …"
        //   "QuestPDF Merge fallback: …"
        var (category, body, level) = ParseRaw(rawMessage);
        var entry = new AppLogEntry { Level = level, Category = category, Message = body };
        Enqueue(entry);
    }

    private void Enqueue(AppLogEntry entry)
    {
        lock (_lock)
        {
            _buffer.AddLast(entry);
            while (_buffer.Count > Capacity)
                _buffer.RemoveFirst();
        }

        // Fire-and-forget; WeakReferenceMessenger won't keep ViewModel alive
        WeakReferenceMessenger.Default.Send(new NewLogEntryMessage(entry));
    }

    private static (string category, string body, AppLogLevel level) ParseRaw(string raw)
    {
        raw = raw?.Trim() ?? string.Empty;

        string category = "App";
        string body = raw;

        // "[CategoryHere] rest of message"
        if (raw.StartsWith('['))
        {
            var end = raw.IndexOf(']');
            if (end > 1)
            {
                category = raw[1..end].Trim();
                body = raw[(end + 1)..].TrimStart();
            }
        }

        var level = AppLogLevel.Debug;
        var lower = body.ToLowerInvariant();
        if (lower.Contains("error") || lower.Contains("exception") || lower.Contains("fail") || lower.Contains("denied"))
            level = AppLogLevel.Error;
        else if (lower.Contains("warn") || lower.Contains("fallback") || lower.Contains("retry"))
            level = AppLogLevel.Warning;
        else if (lower.Contains("success") || lower.Contains("installed") || lower.Contains("mounted") || lower.Contains("synced"))
            level = AppLogLevel.Info;

        return (category, body, level);
    }

    public void Dispose()
    {
        Trace.Listeners.Remove(_listener);
        _listener.Dispose();
    }
}

// ─── TraceListener that feeds AppLogService ────────────────────────────────────

/// <summary>
/// Hooks into <see cref="System.Diagnostics.Debug.WriteLine"/> / <see cref="Trace.WriteLine"/>
/// to route all existing diagnostic messages into <see cref="AppLogService"/> without
/// modifying any call site.
/// </summary>
internal sealed class FryPdfTraceListener : TraceListener
{
    private readonly AppLogService _service;
    private readonly StringBuilder _lineBuffer = new();

    public FryPdfTraceListener(AppLogService service) : base("FryPDF")
    {
        _service = service;
    }

    public override void Write(string? message)
    {
        if (message != null)
            _lineBuffer.Append(message);
    }

    public override void WriteLine(string? message)
    {
        _lineBuffer.Append(message);
        var line = _lineBuffer.ToString();
        _lineBuffer.Clear();

        if (!string.IsNullOrWhiteSpace(line))
            _service.Append(line);
    }
}
