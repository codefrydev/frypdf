using System;
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
