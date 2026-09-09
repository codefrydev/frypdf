using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace PdfEditorApp.Services;

/// <summary>
/// Convenience helpers for logging exceptions through <see cref="IAppLogService"/> with full
/// detail (type, message, stack trace via <see cref="Exception.ToString"/>) plus an explicit
/// unwrap of <see cref="ReflectionTypeLoadException.LoaderExceptions"/>, which is the one thing
/// <see cref="Exception.ToString"/> omits and which plugin assembly loading can throw.
/// </summary>
public static class AppLogServiceExtensions
{
    public static void LogError(this IAppLogService log, string category, string message, Exception ex)
    {
        log.Log(AppLogLevel.Error, category, $"{message}: {BuildDetail(ex)}");
    }

    public static void LogWarning(this IAppLogService log, string category, string message, Exception? ex = null)
    {
        log.Log(AppLogLevel.Warning, category, ex == null ? message : $"{message}: {BuildDetail(ex)}");
    }

    /// <summary>
    /// Times an operation and logs its duration when the returned handle is disposed.
    /// </summary>
    /// <param name="warnAboveMs">
    /// Durations at or above this log at <see cref="AppLogLevel.Warning"/>; anything faster logs
    /// at <see cref="AppLogLevel.Info"/>.
    /// </param>
    /// <remarks>
    /// The level is chosen explicitly rather than left to <c>AppLogService.ParseRaw</c>, which
    /// classifies by keyword — a message like "took 4200ms" contains none of its warning
    /// keywords and would be filed as Debug and buried. It also matters for the diagnostics UI,
    /// which sorts and filters groups by their worst level, so a slow operation logged at Info
    /// sinks to the bottom of the list.
    /// </remarks>
    public static IDisposable TimeOperation(
        this IAppLogService log, string category, string operation, int warnAboveMs = 250)
    {
        return new OperationTimer(log, category, operation, warnAboveMs);
    }

    /// <summary>
    /// Logs <paramref name="message"/> at Warning when <paramref name="elapsedMs"/> is at or
    /// above <paramref name="warnAboveMs"/>, otherwise at Info.
    /// </summary>
    public static void LogDuration(
        this IAppLogService log, string category, string message, long elapsedMs, int warnAboveMs = 250)
    {
        log.Log(elapsedMs >= warnAboveMs ? AppLogLevel.Warning : AppLogLevel.Info, category, message);
    }

    private sealed class OperationTimer : IDisposable
    {
        private readonly IAppLogService _log;
        private readonly string _category;
        private readonly string _operation;
        private readonly int _warnAboveMs;
        private readonly Stopwatch _stopwatch;
        private bool _isDisposed;

        public OperationTimer(IAppLogService log, string category, string operation, int warnAboveMs)
        {
            _log = log;
            _category = category;
            _operation = operation;
            _warnAboveMs = warnAboveMs;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _stopwatch.Stop();
            _log.LogDuration(
                _category,
                $"{_operation} took {_stopwatch.ElapsedMilliseconds}ms.",
                _stopwatch.ElapsedMilliseconds,
                _warnAboveMs);
        }
    }

    private static string BuildDetail(Exception ex, int depth = 0)
    {
        if (depth > 5) return ex.ToString();

        var sb = new StringBuilder();
        sb.Append(ex);

        if (ex is ReflectionTypeLoadException { LoaderExceptions.Length: > 0 } rtle)
        {
            sb.Append(" | LoaderExceptions: ");
            foreach (var loaderEx in rtle.LoaderExceptions)
            {
                if (loaderEx == null) continue;
                sb.Append('[').Append(BuildDetail(loaderEx, depth + 1)).Append(']');
            }
        }

        return sb.ToString();
    }
}
