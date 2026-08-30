using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;

namespace PdfEditorApp.Services;

public static class FontHelper
{
    private static readonly HashSet<string> KnownFontFamilies = new(StringComparer.OrdinalIgnoreCase)
    {
        // --- Core Bundled Fonts ---
        "Roboto",
        "Inter",
        "Open Sans",
        "Montserrat",
        "Source Sans 3",
        "Playfair Display",
        "Lora",
        "Merriweather",
        "Cinzel",
        "Fira Code",
        "Roboto Mono",
        "Comic Neue",
        "Pacifico",
        "Dancing Script",
        "Caveat",
        "Great Vibes",
        "Lobster",
        "Bebas Neue",
        "Oswald",
        "Orbitron",

        // --- On-Demand Web Fonts ---
        "Lato",
        "Poppins",
        "Raleway",
        "Nunito",
        "Ubuntu",
        "Noto Sans",
        "Noto Serif",
        "PT Serif",
        "Crimson Text",
        "Libre Baskerville",
        "Libre Franklin",
        "Josefin Sans",
        "Titillium Web",
        "Exo 2",
        "Cabin",

        // --- Indian Scripts (Noto) ---
        "Noto Sans Devanagari",
        "Noto Sans Tamil",
        "Noto Sans Telugu",
        "Noto Sans Arabic",
        "Noto Sans Gujarati",
        "Noto Sans Kannada",
        "Noto Sans Bengali",
        "Noto Sans Malayalam",
        "Noto Sans Sinhala",
        "Tiro Devanagari Hindi",

        // --- CJK ---
        "Noto Sans SC",           // Simplified Chinese
        "Noto Sans TC",           // Traditional Chinese
        "Noto Sans JP",           // Japanese (Hiragana + Katakana + CJK)
        "Noto Serif JP",          // Japanese Serif
        "Noto Sans KR",           // Korean (full Hangul)
        "Nanum Gothic",           // Korean (additional)

        // --- Southeast Asian ---
        "Noto Sans Thai",         // Thai
        "Sarabun",                // Thai (modern)
        "Noto Sans Myanmar",      // Burmese
        "Noto Sans Khmer",        // Cambodian
        "Noto Sans Lao",          // Laotian
        "Be Vietnam Pro",         // Vietnamese (extended Latin)

        // --- Middle Eastern / RTL ---
        "Noto Sans Hebrew",       // Hebrew / Yiddish
        "Heebo",                  // Hebrew (modern)
        "Vazirmatn",              // Persian / Farsi
        "Noto Nastaliq Urdu",     // Urdu (Nastaliq calligraphic style)

        // --- Eurasian ---
        "Noto Sans Georgian",     // Georgian
        "Noto Sans Armenian",     // Armenian
        "Noto Sans Ethiopic",     // Ethiopic (Amharic, Tigrinya)
        "Golos Text",             // Cyrillic / Russian
        "Russo One",              // Cyrillic display
        "GFS Neohellenic",        // Greek
    };

    public static void RegisterFontFamily(string fontName)
    {
        if (!string.IsNullOrWhiteSpace(fontName))
        {
            lock (KnownFontFamilies)
            {
                KnownFontFamilies.Add(fontName);
            }
        }
    }

    public static FontFamily CreateFontFamily(string? fontName)
    {
        if (string.IsNullOrWhiteSpace(fontName))
            return FontFamily.Default;

        // Check if there's a cached font file in the user cache directory
        string userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FryPDF", "Fonts");
        string cleanName = fontName.Replace(" ", "");
        string ttfPath = Path.Combine(userDir, $"{cleanName}.ttf");

        if (File.Exists(ttfPath))
        {
            try
            {
                var uri = new Uri(ttfPath);
                return new FontFamily($"file://{userDir}#{fontName}, avares://PdfEditorApp/Assets/Fonts#{fontName}, {fontName}");
            }
            catch { }
        }

        // Standard embedded asset resolution with system font fallback
        return new FontFamily($"avares://PdfEditorApp/Assets/Fonts#{fontName}, {fontName}");
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
