using System;
using System.IO;

namespace PdfEditorApp.Core.Plugins.Manifests;

/// <summary>
/// Validates plugin identifiers that originate from untrusted sources — the <c>id</c> field
/// of a <c>plugin.json</c> inside a .fryplugin archive, or an entry in a remote marketplace
/// catalog — before they are used to build filesystem paths.
/// </summary>
/// <remarks>
/// A plugin id reaches <see cref="Path.Combine(string, string)"/> when resolving an install
/// directory. <c>Path.Combine</c> returns its second argument verbatim when that argument is
/// rooted, and it does not collapse <c>..</c> segments, so an unvalidated id such as
/// <c>"../../../Startup"</c> or <c>"/Users/someone/Documents"</c> escapes the plugins root —
/// where the install path is then recursively deleted and extracted into.
/// </remarks>
public static class PluginIdValidator
{
    /// <summary>Maximum length of a plugin id.</summary>
    public const int MaxLength = 64;

    /// <summary>
    /// True when <paramref name="pluginId"/> is safe to use as a single path segment.
    /// </summary>
    public static bool IsValid(string? pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return false;
        if (pluginId.Length > MaxLength) return false;

        // "." and ".." are valid-looking but resolve to the parent/current directory.
        if (pluginId == "." || pluginId == "..") return false;

        foreach (char c in pluginId)
        {
            bool allowed = (c >= 'a' && c <= 'z')
                        || (c >= 'A' && c <= 'Z')
                        || (c >= '0' && c <= '9')
                        || c == '.' || c == '_' || c == '-';
            if (!allowed) return false;
        }

        return true;
    }

    /// <summary>
    /// Returns <paramref name="pluginId"/> when it is valid, otherwise throws.
    /// </summary>
    /// <exception cref="ArgumentException">The id is not a safe single path segment.</exception>
    public static string Require(string? pluginId, string context)
    {
        if (!IsValid(pluginId))
        {
            throw new ArgumentException(
                $"Invalid plugin id '{pluginId}' in {context}. A plugin id must be 1-{MaxLength} " +
                "characters of letters, digits, '.', '_' or '-', and cannot be '.' or '..'.",
                nameof(pluginId));
        }

        return pluginId!;
    }

    /// <summary>
    /// Combines <paramref name="root"/> with a validated <paramref name="pluginId"/> and
    /// verifies the result still resolves inside <paramref name="root"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The id is invalid or escapes the root.</exception>
    public static string ResolveInstallDirectory(string root, string? pluginId, string context)
    {
        Require(pluginId, context);

        var fullRoot = Path.GetFullPath(root);
        var candidate = Path.GetFullPath(Path.Combine(fullRoot, pluginId!));

        if (!IsInside(candidate, fullRoot))
        {
            throw new ArgumentException(
                $"Plugin id '{pluginId}' in {context} resolves to '{candidate}', which is outside " +
                $"the plugins root '{fullRoot}'.",
                nameof(pluginId));
        }

        return candidate;
    }

    /// <summary>
    /// True when <paramref name="path"/> is <paramref name="root"/> itself or sits beneath it.
    /// Compares on a separator boundary so that "/plugins-evil" is not treated as being
    /// inside "/plugins".
    /// </summary>
    public static bool IsInside(string path, string root)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);

        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (string.Equals(fullPath, fullRoot, comparison)) return true;

        return fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, comparison);
    }
}
