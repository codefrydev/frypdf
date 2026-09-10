using System;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> that is skipped unless <c>FRYPDF_LIVE_NETWORK_TESTS=1</c>
/// is set in the environment.
/// </summary>
/// <remarks>
/// Tests that reach real hosts cannot be part of the default <c>dotnet test</c> run: they
/// fail behind a proxy or in a sandbox, they fail offline, and they fail whenever the
/// remote content changes - none of which says anything about this codebase. Keeping them
/// opt-in preserves them as a deliberate smoke test:
/// <code>FRYPDF_LIVE_NETWORK_TESTS=1 dotnet test</code>
/// xUnit v2 has no conditional-skip support (<c>SkipUnless</c> arrived in v3), so the
/// condition is evaluated here at discovery time instead.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class LiveNetworkFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "FRYPDF_LIVE_NETWORK_TESTS";

    public LiveNetworkFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(EnvironmentVariable), "1", StringComparison.Ordinal))
        {
            Skip = $"Live-network smoke test. Set {EnvironmentVariable}=1 to run.";
        }
    }
}
