using System;
using System.Text.RegularExpressions;

namespace PdfEditorApp.Core.Plugins.Marketplace;

public enum PluginCompatibilityStatus
{
    Compatible,
    HostUpgradeRequired,
    HostTooNew,
    IncompatibleRuntime
}

public readonly record struct PluginCompatibilityResult(
    PluginCompatibilityStatus Status,
    bool IsCompatible,
    string Message
);

/// <summary>
/// Validates compatibility between external plugin releases and the current FryPDF host environment.
/// </summary>
public static class PluginCompatibilityChecker
{
    public static readonly Version CurrentHostVersion = new(1, 0, 0);
    public const string CurrentTargetFramework = "net10.0";

    public static PluginCompatibilityResult CheckCompatibility(
        string? minHostVersionStr,
        string? maxHostVersionStr,
        string? targetFramework,
        Version? hostVersion = null,
        string? currentTfm = null)
    {
        var host = hostVersion ?? CurrentHostVersion;
        var tfm = currentTfm ?? CurrentTargetFramework;

        // 1. Target framework check
        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            var cleanTfm = targetFramework.Trim().ToLowerInvariant();
            var currentCleanTfm = tfm.Trim().ToLowerInvariant();

            // Support net10.0 running net10.0, net9.0, net8.0, netstandard2.0, netstandard2.1
            if (!IsTfmCompatible(currentCleanTfm, cleanTfm))
            {
                return new PluginCompatibilityResult(
                    PluginCompatibilityStatus.IncompatibleRuntime,
                    false,
                    $"Requires .NET runtime '{targetFramework}' (current host is '{tfm}')."
                );
            }
        }

        // 2. Minimum host version check
        if (!string.IsNullOrWhiteSpace(minHostVersionStr) && TryParseVersion(minHostVersionStr, out var minHost))
        {
            if (host < minHost)
            {
                return new PluginCompatibilityResult(
                    PluginCompatibilityStatus.HostUpgradeRequired,
                    false,
                    $"Requires FryPDF v{minHost} or newer (current is v{host})."
                );
            }
        }

        // 3. Maximum host version check
        if (!string.IsNullOrWhiteSpace(maxHostVersionStr) && TryParseVersion(maxHostVersionStr, out var maxHost))
        {
            if (host > maxHost)
            {
                return new PluginCompatibilityResult(
                    PluginCompatibilityStatus.HostTooNew,
                    false,
                    $"Compatible up to FryPDF v{maxHost} (current is v{host})."
                );
            }
        }

        return new PluginCompatibilityResult(
            PluginCompatibilityStatus.Compatible,
            true,
            "Compatible with current FryPDF version."
        );
    }

    public static PluginCompatibilityResult CheckCompatibility(MarketplacePluginVersion version, Version? hostVersion = null, string? currentTfm = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        return CheckCompatibility(version.MinHostVersion, version.MaxHostVersion, version.TargetFramework, hostVersion, currentTfm);
    }

    public static PluginCompatibilityResult CheckCompatibility(MarketplacePluginVersion version, string? hostVersionStr, string? currentTfm = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        Version? host = null;
        if (!string.IsNullOrWhiteSpace(hostVersionStr) && TryParseVersion(hostVersionStr, out var parsed))
        {
            host = parsed;
        }
        return CheckCompatibility(version, host, currentTfm);
    }

    public static bool TryParseVersion(string input, out Version version)
    {
        version = new Version(0, 0);
        if (string.IsNullOrWhiteSpace(input)) return false;

        var clean = input.Trim().TrimStart('v', 'V');
        // Handle semver prerelease tags e.g. "1.2.0-preview1" -> "1.2.0"
        var dashIdx = clean.IndexOf('-');
        if (dashIdx > 0)
        {
            clean = clean[..dashIdx];
        }

        // Ensure at least 2 components for Version.TryParse (e.g. "1" -> "1.0")
        var parts = clean.Split('.');
        if (parts.Length == 1 && int.TryParse(parts[0], out _))
        {
            clean = $"{clean}.0";
        }

        return Version.TryParse(clean, out version!);
    }

    public static bool IsTfmCompatible(string hostTfm, string pluginTfm)
    {
        if (string.Equals(hostTfm, pluginTfm, StringComparison.OrdinalIgnoreCase)) return true;

        // netstandard is universally compatible with .NET Core/.NET 5+
        if (pluginTfm.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) return true;

        // Parse netX.Y
        if (hostTfm.StartsWith("net", StringComparison.OrdinalIgnoreCase) &&
            pluginTfm.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            var hostVerStr = hostTfm[3..];
            var pluginVerStr = pluginTfm[3..];

            if (double.TryParse(hostVerStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var hostNum) &&
                double.TryParse(pluginVerStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pluginNum))
            {
                return hostNum >= pluginNum;
            }
        }

        return false;
    }
}
