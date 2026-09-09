using System;
using System.Collections.Concurrent;
using System.IO;
using Avalonia.Media;

namespace PdfEditorApp.Services;

public static class FontHelper
{
    /// <summary>
    /// Memoized results of <see cref="CreateFontFamily"/>, keyed by the requested family name.
    /// </summary>
    /// <remarks>
    /// <see cref="CreateFontFamily"/> is called from <c>Render</c>, from value converters, and
    /// once per character by the text layout engine. Each uncached call performed a
    /// <see cref="File.Exists(string)"/> disk stat and allocated a new
    /// <see cref="FontFamily"/>, so laying out a 2000-character text block issued ~2000
    /// filesystem syscalls per frame.
    /// </remarks>
    private static readonly ConcurrentDictionary<string, FontFamily> FontFamilyCache = new(StringComparer.Ordinal);

    /// <summary>Resolved once: the per-user font cache directory.</summary>
    private static readonly string UserFontDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FryPDF", "Fonts");

    /// <summary>
    /// Raised when the set of resolvable fonts changes. Downstream caches keyed on a font
    /// family name (glyph typefaces, glyph advances) must drop their entries in response.
    /// </summary>
    /// <remarks>
    /// Both subscriber and publisher are static and live for the process, so this holds no
    /// object alive that would otherwise be collected.
    /// </remarks>
    public static event Action? FontsChanged;

    /// <summary>
    /// Invalidates the cached <see cref="FontFamily"/> for <paramref name="fontName"/>.
    /// Call this after installing or importing a font so the next resolution re-probes disk.
    /// </summary>
    public static void RegisterFontFamily(string fontName)
    {
        if (!string.IsNullOrWhiteSpace(fontName))
        {
            FontFamilyCache.TryRemove(fontName, out _);
            FontsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Drops every cached <see cref="FontFamily"/>. Call this after a bulk font-library change
    /// (a font pack install, or clearing the user font cache).
    /// </summary>
    public static void InvalidateFontFamilyCache()
    {
        FontFamilyCache.Clear();
        FontsChanged?.Invoke();
    }

    /// <summary>
    /// Resolves a font family name to an Avalonia <see cref="FontFamily"/>, memoized.
    /// </summary>
    /// <remarks>
    /// Fonts only appear at runtime through the font manager, which calls
    /// <see cref="RegisterFontFamily"/> / <see cref="InvalidateFontFamilyCache"/>, so caching
    /// here is safe and removes the per-call disk probe from every render and layout pass.
    /// </remarks>
    public static FontFamily CreateFontFamily(string? fontName)
    {
        if (string.IsNullOrWhiteSpace(fontName))
            return FontFamily.Default;

        return FontFamilyCache.GetOrAdd(fontName, static name =>
        {
            // Prefer a font file already downloaded into the user cache directory.
            string cleanName = name.Replace(" ", "");
            string ttfPath = Path.Combine(UserFontDirectory, $"{cleanName}.ttf");

            if (File.Exists(ttfPath))
            {
                return new FontFamily(
                    $"file://{UserFontDirectory}#{name}, avares://PdfEditorApp/Assets/Fonts#{name}, {name}");
            }

            // Standard embedded asset resolution with system font fallback
            return new FontFamily($"avares://PdfEditorApp/Assets/Fonts#{name}, {name}");
        });
    }

    /// <summary>
    /// Cached <see cref="Typeface"/> per (family, bold, italic), memoized like
    /// <see cref="CreateFontFamily"/>.
    /// </summary>
    /// <remarks>
    /// Custom text controls built one of these inside <c>Render</c>, so it was reconstructed on
    /// every render pass of every text element — and the canvas renders each element twice,
    /// once on the page and once in the thumbnail rail. Keyed the same way as
    /// <c>TextLayoutEngine.GlyphTypefaceCache</c> and invalidated by the same
    /// <see cref="FontsChanged"/> signal.
    /// </remarks>
    private static readonly ConcurrentDictionary<(string Family, bool Bold, bool Italic), Typeface>
        TypefaceCache = new();

    static FontHelper()
    {
        // A newly installed font changes what a family name resolves to, so a typeface cached
        // against that name must not outlive the change.
        FontsChanged += () => TypefaceCache.Clear();
    }

    /// <summary>
    /// Resolves a font family name plus weight/style to an Avalonia <see cref="Typeface"/>,
    /// memoized. See <see cref="TypefaceCache"/>.
    /// </summary>
    public static Typeface CreateTypeface(string? fontName, bool isBold, bool isItalic)
    {
        var key = (fontName ?? string.Empty, isBold, isItalic);

        return TypefaceCache.GetOrAdd(key, static k => new Typeface(
            CreateFontFamily(k.Family),
            k.Italic ? FontStyle.Italic : FontStyle.Normal,
            k.Bold ? FontWeight.Bold : FontWeight.Normal));
    }

    /// <summary>
    /// Returns a safe fallback font family when the requested family cannot be resolved.
    /// </summary>
    public static string GetSafeFallback(string? requestedFamily)
    {
        if (string.IsNullOrWhiteSpace(requestedFamily)) return "Open Sans";

        // Script-specific fallbacks
        string lc = requestedFamily.ToLowerInvariant();
        if (lc.Contains("sc") || lc.Contains("hans") || lc.Contains("chinese") || lc.Contains("simsun") || lc.Contains("yahei")) return "Noto Sans SC";
        if (lc.Contains("tc") || lc.Contains("hant") || lc.Contains("mingliu")) return "Noto Sans TC";
        if (lc.Contains("jp") || lc.Contains("japanese") || lc.Contains("gothic") || lc.Contains("mincho")) return "Noto Sans JP";
        if (lc.Contains("kr") || lc.Contains("korean") || lc.Contains("hangul") || lc.Contains("malgun")) return "Noto Sans KR";
        if (lc.Contains("devanagari") || lc.Contains("hindi")) return "Noto Sans Devanagari";
        if (lc.Contains("tamil")) return "Noto Sans Tamil";
        if (lc.Contains("telugu")) return "Noto Sans Telugu";
        if (lc.Contains("arabic") || lc.Contains("urdu")) return "Noto Sans Arabic";
        if (lc.Contains("hebrew")) return "Noto Sans Hebrew";
        if (lc.Contains("thai")) return "Noto Sans Thai";
        if (lc.Contains("gujarati")) return "Noto Sans Gujarati";
        if (lc.Contains("kannada")) return "Noto Sans Kannada";
        if (lc.Contains("bengali")) return "Noto Sans Bengali";
        if (lc.Contains("malayalam")) return "Noto Sans Malayalam";

        // Serif fallbacks
        if (lc.Contains("serif") || lc.Contains("times") || lc.Contains("georgia") ||
            lc.Contains("garamond") || lc.Contains("baskerville"))
            return "PT Serif";

        // Mono fallbacks
        if (lc.Contains("mono") || lc.Contains("code") || lc.Contains("courier"))
            return "Fira Code";

        return "Open Sans";
    }
}
