using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfEditorApp.Core.Analysis;

/// <summary>
/// Detects the Unicode script of text content using codepoint range analysis.
/// 
/// Used as the authoritative fallback when PDF font names are ambiguous, obfuscated,
/// or use non-standard naming conventions (common in CJK, Arabic, and Indian PDFs).
/// 
/// Supports 30+ world scripts including CJK (Simplified/Traditional Chinese, Japanese, Korean),
/// Indian scripts (Devanagari, Tamil, Telugu, Bengali, Gujarati, Kannada, Malayalam, Odia, Gurmukhi),
/// RTL scripts (Arabic, Hebrew, Persian/Urdu), and Southeast/Central Asian scripts.
/// </summary>
public static class UnicodeScriptDetector
{
    /// <summary>
    /// Classifies the dominant Unicode script of a string.
    /// Returns the best-matching FontFamily name from our embedded font library.
    /// </summary>
    public static string DetectScriptFontFamily(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Open Sans";

        // Count codepoints per script block
        var scriptCounts = new Dictionary<UnicodeScript, int>();

        for (int i = 0; i < text.Length;)
        {
            int codepoint = char.ConvertToUtf32(text, i);
            i += char.IsSurrogatePair(text, i) ? 2 : 1;

            // Skip whitespace and common punctuation
            if (codepoint <= 0x0040) continue;
            if (IsBasicLatin(codepoint)) continue;

            var script = ClassifyCodepoint(codepoint);
            if (script != UnicodeScript.Unknown)
            {
                scriptCounts.TryGetValue(script, out int count);
                scriptCounts[script] = count + 1;
            }
        }

        if (scriptCounts.Count == 0)
            return "Open Sans";

        // Return the font for the dominant script
        var dominant = scriptCounts.MaxBy(kvp => kvp.Value).Key;
        return ScriptToFontFamily(dominant);
    }

    /// <summary>
    /// Quick check: does this string contain any CJK characters?
    /// </summary>
    public static bool ContainsCjk(string text)
    {
        foreach (char c in text)
        {
            int cp = c;
            if ((cp >= 0x4E00 && cp <= 0x9FFF) ||   // CJK Unified Ideographs
                (cp >= 0x3400 && cp <= 0x4DBF) ||   // CJK Extension A
                (cp >= 0x20000 && cp <= 0x2A6DF) ||  // CJK Extension B (surrogate pair range)
                (cp >= 0x3040 && cp <= 0x309F) ||   // Hiragana
                (cp >= 0x30A0 && cp <= 0x30FF) ||   // Katakana
                (cp >= 0xAC00 && cp <= 0xD7AF))     // Korean Hangul Syllables
                return true;
        }
        return false;
    }

    /// <summary>
    /// Quick check: does this string need an RTL font?
    /// </summary>
    public static bool IsRtlText(string text)
    {
        foreach (char c in text)
        {
            int cp = c;
            if ((cp >= 0x0600 && cp <= 0x06FF) ||  // Arabic
                (cp >= 0x0590 && cp <= 0x05FF) ||  // Hebrew
                (cp >= 0xFB50 && cp <= 0xFDFF) ||  // Arabic Presentation Forms-A
                (cp >= 0xFE70 && cp <= 0xFEFF))    // Arabic Presentation Forms-B
                return true;
        }
        return false;
    }

    /// <summary>
    /// Quick check: does this string contain Devanagari (Hindi/Marathi/Sanskrit)?
    /// </summary>
    public static bool ContainsDevanagari(string text)
    {
        foreach (char c in text)
        {
            int cp = c;
            if (cp >= 0x0900 && cp <= 0x097F) return true;
        }
        return false;
    }

    private static UnicodeScript ClassifyCodepoint(int cp)
    {
        // === CJK Unified Ideographs (used by Chinese, Japanese, rarely Korean) ===
        if (cp >= 0x4E00 && cp <= 0x9FFF) return UnicodeScript.CjkUnified;
        if (cp >= 0x3400 && cp <= 0x4DBF) return UnicodeScript.CjkUnified;  // Extension A
        if (cp >= 0xF900 && cp <= 0xFAFF) return UnicodeScript.CjkUnified;  // CJK Compatibility

        // === Japanese-Specific ===
        if (cp >= 0x3040 && cp <= 0x309F) return UnicodeScript.Hiragana;
        if (cp >= 0x30A0 && cp <= 0x30FF) return UnicodeScript.Katakana;
        if (cp >= 0x31F0 && cp <= 0x31FF) return UnicodeScript.Katakana; // Katakana Phonetic Extensions
        if (cp >= 0xFF65 && cp <= 0xFF9F) return UnicodeScript.Katakana; // Halfwidth Katakana

        // === Korean ===
        if (cp >= 0xAC00 && cp <= 0xD7AF) return UnicodeScript.HangulSyllables;
        if (cp >= 0x1100 && cp <= 0x11FF) return UnicodeScript.HangulJamo;
        if (cp >= 0x3130 && cp <= 0x318F) return UnicodeScript.HangulCompatJamo;
        if (cp >= 0xA960 && cp <= 0xA97F) return UnicodeScript.HangulJamoExtended;

        // === Chinese Bopomofo (Traditional Chinese phonetics) ===
        if (cp >= 0x02EA && cp <= 0x02EB) return UnicodeScript.TraditionalChinese;
        if (cp >= 0x3100 && cp <= 0x312F) return UnicodeScript.TraditionalChinese;
        if (cp >= 0x31A0 && cp <= 0x31BF) return UnicodeScript.TraditionalChinese;

        // === Chinese CJK Strokes (Traditional-specific) ===
        if (cp >= 0x31C0 && cp <= 0x31EF) return UnicodeScript.TraditionalChinese;

        // === Arabic ===
        if (cp >= 0x0600 && cp <= 0x06FF) return UnicodeScript.Arabic;
        if (cp >= 0x0750 && cp <= 0x077F) return UnicodeScript.Arabic;   // Arabic Supplement
        if (cp >= 0x08A0 && cp <= 0x08FF) return UnicodeScript.Arabic;   // Arabic Extended-A
        if (cp >= 0xFB50 && cp <= 0xFDFF) return UnicodeScript.Arabic;   // Arabic Presentation Forms-A
        if (cp >= 0xFE70 && cp <= 0xFEFF) return UnicodeScript.Arabic;   // Arabic Presentation Forms-B
        if (cp >= 0x1EE00 && cp <= 0x1EEFF) return UnicodeScript.Arabic; // Arabic Mathematical

        // === Persian / Farsi (subset of Arabic block, with specific codepoints) ===
        // Farsi uses Arabic script with some extra chars — handled under Arabic above

        // === Hebrew ===
        if (cp >= 0x0590 && cp <= 0x05FF) return UnicodeScript.Hebrew;
        if (cp >= 0xFB00 && cp <= 0xFB4F) return UnicodeScript.Hebrew; // Alphabetic Presentation Forms (Hebrew part)

        // === Devanagari (Hindi, Marathi, Sanskrit, Nepali) ===
        if (cp >= 0x0900 && cp <= 0x097F) return UnicodeScript.Devanagari;
        if (cp >= 0xA8E0 && cp <= 0xA8FF) return UnicodeScript.Devanagari; // Devanagari Extended

        // === Bengali (Bangla) ===
        if (cp >= 0x0980 && cp <= 0x09FF) return UnicodeScript.Bengali;

        // === Gurmukhi (Punjabi) ===
        if (cp >= 0x0A00 && cp <= 0x0A7F) return UnicodeScript.Gurmukhi;

        // === Gujarati ===
        if (cp >= 0x0A80 && cp <= 0x0AFF) return UnicodeScript.Gujarati;

        // === Oriya / Odia ===
        if (cp >= 0x0B00 && cp <= 0x0B7F) return UnicodeScript.Odia;

        // === Tamil ===
        if (cp >= 0x0B80 && cp <= 0x0BFF) return UnicodeScript.Tamil;

        // === Telugu ===
        if (cp >= 0x0C00 && cp <= 0x0C7F) return UnicodeScript.Telugu;

        // === Kannada ===
        if (cp >= 0x0C80 && cp <= 0x0CFF) return UnicodeScript.Kannada;

        // === Malayalam ===
        if (cp >= 0x0D00 && cp <= 0x0D7F) return UnicodeScript.Malayalam;

        // === Sinhala (Sri Lanka) ===
        if (cp >= 0x0D80 && cp <= 0x0DFF) return UnicodeScript.Sinhala;

        // === Thai ===
        if (cp >= 0x0E00 && cp <= 0x0E7F) return UnicodeScript.Thai;

        // === Lao ===
        if (cp >= 0x0E80 && cp <= 0x0EFF) return UnicodeScript.Lao;

        // === Tibetan ===
        if (cp >= 0x0F00 && cp <= 0x0FFF) return UnicodeScript.Tibetan;

        // === Myanmar / Burmese ===
        if (cp >= 0x1000 && cp <= 0x109F) return UnicodeScript.Myanmar;

        // === Georgian ===
        if (cp >= 0x10A0 && cp <= 0x10FF) return UnicodeScript.Georgian;
        if (cp >= 0x2D00 && cp <= 0x2D2F) return UnicodeScript.Georgian; // Georgian Supplement

        // === Ethiopic (Amharic, Tigrinya, etc.) ===
        if (cp >= 0x1200 && cp <= 0x137F) return UnicodeScript.Ethiopic;
        if (cp >= 0x1380 && cp <= 0x139F) return UnicodeScript.Ethiopic;

        // === Armenian ===
        if (cp >= 0x0530 && cp <= 0x058F) return UnicodeScript.Armenian;

        // === Cyrillic (Russian, Ukrainian, Bulgarian, Serbian, etc.) ===
        if (cp >= 0x0400 && cp <= 0x04FF) return UnicodeScript.Cyrillic;
        if (cp >= 0x0500 && cp <= 0x052F) return UnicodeScript.Cyrillic; // Cyrillic Supplement
        if (cp >= 0x2DE0 && cp <= 0x2DFF) return UnicodeScript.Cyrillic; // Cyrillic Extended-A

        // === Greek ===
        if (cp >= 0x0370 && cp <= 0x03FF) return UnicodeScript.Greek;
        if (cp >= 0x1F00 && cp <= 0x1FFF) return UnicodeScript.Greek;    // Greek Extended

        // === Khmer (Cambodia) ===
        if (cp >= 0x1780 && cp <= 0x17FF) return UnicodeScript.Khmer;

        // === Mongolian ===
        if (cp >= 0x1800 && cp <= 0x18AF) return UnicodeScript.Mongolian;

        return UnicodeScript.Unknown;
    }

    private static bool IsBasicLatin(int cp)
    {
        return cp < 0x0250; // Basic Latin + Latin-1 Supplement + Latin Extended-A+B
    }

    private static string ScriptToFontFamily(UnicodeScript script) => script switch
    {
        // === CJK ===
        UnicodeScript.Hiragana       => "Noto Sans JP",        // Japanese hiragana → JP font
        UnicodeScript.Katakana       => "Noto Sans JP",        // Japanese katakana → JP font
        UnicodeScript.CjkUnified     => "Noto Sans SC",        // Default CJK → Simplified Chinese (superset)
        UnicodeScript.TraditionalChinese => "Noto Sans TC",    // Traditional Chinese specific
        UnicodeScript.HangulSyllables    => "Noto Sans KR",    // Korean
        UnicodeScript.HangulJamo         => "Noto Sans KR",
        UnicodeScript.HangulCompatJamo   => "Noto Sans KR",
        UnicodeScript.HangulJamoExtended => "Noto Sans KR",

        // === RTL ===
        UnicodeScript.Arabic         => "Noto Sans Arabic",
        UnicodeScript.Hebrew         => "Noto Sans Hebrew",

        // === Indian Scripts ===
        UnicodeScript.Devanagari     => "Noto Sans Devanagari",
        UnicodeScript.Bengali        => "Noto Sans Bengali",
        UnicodeScript.Gurmukhi       => "Noto Sans",           // Gurmukhi → general Noto Sans
        UnicodeScript.Gujarati       => "Noto Sans Gujarati",
        UnicodeScript.Odia           => "Noto Sans",
        UnicodeScript.Tamil          => "Noto Sans Tamil",
        UnicodeScript.Telugu         => "Noto Sans Telugu",
        UnicodeScript.Kannada        => "Noto Sans Kannada",
        UnicodeScript.Malayalam      => "Noto Sans Malayalam",
        UnicodeScript.Sinhala        => "Noto Sans Sinhala",
        UnicodeScript.Tibetan        => "Noto Sans",

        // === Southeast Asian ===
        UnicodeScript.Thai           => "Noto Sans Thai",
        UnicodeScript.Lao            => "Noto Sans Lao",
        UnicodeScript.Myanmar        => "Noto Sans Myanmar",
        UnicodeScript.Khmer          => "Noto Sans Khmer",

        // === Eurasian ===
        UnicodeScript.Georgian       => "Noto Sans Georgian",
        UnicodeScript.Armenian       => "Noto Sans Armenian",
        UnicodeScript.Ethiopic       => "Noto Sans Ethiopic",
        UnicodeScript.Cyrillic       => "Noto Sans",           // Cyrillic covered by Noto Sans Latin
        UnicodeScript.Greek          => "GFS Neohellenic",
        UnicodeScript.Mongolian      => "Noto Sans",

        _                            => "Open Sans"
    };
}

/// <summary>
/// Unicode script classification enum.
/// </summary>
public enum UnicodeScript
{
    Unknown,

    // CJK
    CjkUnified,
    TraditionalChinese,
    Hiragana,
    Katakana,
    HangulSyllables,
    HangulJamo,
    HangulCompatJamo,
    HangulJamoExtended,

    // RTL
    Arabic,
    Hebrew,

    // Indian
    Devanagari,
    Bengali,
    Gurmukhi,
    Gujarati,
    Odia,
    Tamil,
    Telugu,
    Kannada,
    Malayalam,
    Sinhala,
    Tibetan,

    // Southeast Asian
    Thai,
    Lao,
    Myanmar,
    Khmer,

    // Eurasian
    Georgian,
    Armenian,
    Ethiopic,
    Cyrillic,
    Greek,
    Mongolian,
}
