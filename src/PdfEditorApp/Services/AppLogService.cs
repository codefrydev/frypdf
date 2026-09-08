using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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
    private readonly AppLogFileWriter _fileWriter;

    public int Capacity { get; } = DefaultCapacity;

    private AppLogService()
    {
        _listener = new FryPdfTraceListener(this);
        Trace.Listeners.Add(_listener);

        // Path resolution is deferred to the background pump (see AppLogFileWriter) rather than
        // resolved here, so that FryPdfPaths — which itself logs its redirect decisions — never
        // has to call back into this constructor before the `Instance` field is assigned.
        _fileWriter = new AppLogFileWriter(() => FryPdfPaths.LogFilePath);
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

        // Persist to disk so the trail survives a crash/restart, not just the in-memory window.
        _fileWriter.Enqueue(entry);

        // Fire-and-forget; WeakReferenceMessenger won't keep ViewModel alive
        WeakReferenceMessenger.Default.Send(new NewLogEntryMessage(entry));
    }

    internal static (string category, string body, AppLogLevel level) ParseRaw(string raw)
    {
        raw = raw?.Trim() ?? string.Empty;

        string category = "App";
        string body = raw;
        AppLogLevel? explicitLevel = null;

        // 1. Check for standard .NET TraceSource format: "Source Warning: 0 : Message"
        var traceColonIdx = raw.IndexOf(" : ", StringComparison.Ordinal);
        if (traceColonIdx > 0 && traceColonIdx < 50)
        {
            var prefix = raw[..traceColonIdx];
            if (prefix.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                explicitLevel = AppLogLevel.Warning;
            else if (prefix.Contains("Error", StringComparison.OrdinalIgnoreCase))
                explicitLevel = AppLogLevel.Error;
            else if (prefix.Contains("Information", StringComparison.OrdinalIgnoreCase))
                explicitLevel = AppLogLevel.Info;
            else if (prefix.Contains("Verbose", StringComparison.OrdinalIgnoreCase))
                explicitLevel = AppLogLevel.Debug;

            body = raw[(traceColonIdx + 3)..].TrimStart();
        }

        // 2. Extract [Category] prefix from body
        if (body.StartsWith('['))
        {
            var end = body.IndexOf(']');
            if (end > 1)
            {
                category = body[1..end].Trim();
                body = body[(end + 1)..].TrimStart();
            }
        }

        if (explicitLevel.HasValue)
        {
            return (category, body, explicitLevel.Value);
        }

        // 3. Special handling for Avalonia [Binding] messages
        if (string.Equals(category, "Binding", StringComparison.OrdinalIgnoreCase))
        {
            // If the binding evaluated to null along an optional intermediate path
            // (e.g. unselected element, document not yet loaded), Avalonia outputs:
            // "An error occurred binding ... 'Value is null.'"
            // In XAML/MVVM this is normal lifecycle behavior rather than an error.
            if (body.Contains("Value is null", StringComparison.OrdinalIgnoreCase))
            {
                return (category, body, AppLogLevel.Debug);
            }

            // Real binding failures (conversion failures, missing properties, exceptions)
            return (category, body, AppLogLevel.Error);
        }

        // 4. Heuristic classification based on keywords
        var level = AppLogLevel.Debug;
        var lower = body.ToLowerInvariant();

        // Check warning first so advisory warnings containing phrases like "may fail to render" remain warnings
        if (lower.Contains("warn") || lower.Contains("fallback") || lower.Contains("retry"))
            level = AppLogLevel.Warning;
        else if (lower.Contains("error") || lower.Contains("exception") || lower.Contains("fail") || lower.Contains("denied"))
            level = AppLogLevel.Error;
        else if (lower.Contains("success") || lower.Contains("installed") || lower.Contains("mounted") || lower.Contains("synced"))
            level = AppLogLevel.Info;

        return (category, body, level);
    }

    public void Dispose()
    {
        Trace.Listeners.Remove(_listener);
        _listener.Dispose();
        _fileWriter.Dispose();
    }
}

// ─── Background file persistence ───────────────────────────────────────────────

/// <summary>
/// Persists log entries to a rolling file on disk so the diagnostic trail survives an app
/// crash or restart (the in-memory buffer above is capped and wiped on restart). Writes happen
/// on a dedicated background task via a bounded channel so that no caller — including
/// UI-thread navigation/install code — ever blocks on disk I/O.
/// </summary>
internal sealed class AppLogFileWriter : IDisposable
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB before rotating to .bak

    private readonly Channel<string> _channel;
    private readonly Task _pumpTask;

    public AppLogFileWriter(Func<string> logFilePathProvider)
    {
        _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(2000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
        _pumpTask = Task.Run(() => PumpAsync(logFilePathProvider));
    }

    /// <summary>Non-blocking; drops the oldest queued line if the channel is full rather than stalling the caller.</summary>
    public void Enqueue(AppLogEntry entry) => _channel.Writer.TryWrite(entry.FormattedLine);

    private async Task PumpAsync(Func<string> logFilePathProvider)
    {
        string? logFilePath;
        try
        {
            logFilePath = logFilePathProvider();
        }
        catch
        {
            logFilePath = null; // No writable directory resolvable — nothing we can persist to.
        }

        await foreach (var line in _channel.Reader.ReadAllAsync())
        {
            if (logFilePath == null) continue;

            try
            {
                RotateIfNeeded(logFilePath);
                await File.AppendAllTextAsync(logFilePath, line + Environment.NewLine);
            }
            catch
            {
                // Best-effort: a locked/unwritable log file must never crash the app.
            }
        }
    }

    private static void RotateIfNeeded(string logFilePath)
    {
        var info = new FileInfo(logFilePath);
        if (!info.Exists || info.Length < MaxFileSizeBytes) return;

        try
        {
            File.Copy(logFilePath, logFilePath + ".bak", overwrite: true);
            File.WriteAllText(logFilePath, string.Empty);
        }
        catch
        {
            // Non-fatal — worst case the file keeps growing past the cap.
        }
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        try
        {
            _pumpTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Best-effort flush on shutdown; disposal must never throw.
        }
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
