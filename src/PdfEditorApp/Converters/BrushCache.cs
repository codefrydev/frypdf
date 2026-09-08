using System;
using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace PdfEditorApp.Converters;

/// <summary>
/// Immutable brushes keyed by hex string, shared across converters.
/// </summary>
/// <remarks>
/// Converters run on every binding evaluation. <c>HexToBrushConverter</c> alone is used 37
/// times in DocumentCanvasView.axaml, inside per-element DataTemplates, so a single colour
/// tweak in the inspector allocated a brush per binding per element — and because each was a
/// new instance, Avalonia also had to drop its cached render brush every time.
/// Brushes are frozen (<see cref="ImmutableSolidColorBrush"/>) so sharing them is safe.
/// </remarks>
public static class BrushCache
{
    private static readonly ConcurrentDictionary<string, IBrush> ByHex = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<uint, IBrush> ByColor = new();

    /// <summary>Returns a shared brush for <paramref name="hex"/>, or <paramref name="fallback"/>.</summary>
    public static IBrush Get(string? hex, IBrush fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;

        if (ByHex.TryGetValue(hex, out var cached)) return cached;
        if (!Color.TryParse(hex, out var color)) return fallback;

        var brush = Get(color);
        ByHex[hex] = brush;
        return brush;
    }

    /// <summary>Returns a shared brush for <paramref name="color"/>.</summary>
    public static IBrush Get(Color color)
        => ByColor.GetOrAdd(color.ToUInt32(), static packed => new ImmutableSolidColorBrush(Color.FromUInt32(packed)));
}
