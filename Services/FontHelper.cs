using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace PdfEditorApp.Services;

public static class FontHelper
{
    private static readonly HashSet<string> EmbeddedFontFamilies = new(StringComparer.OrdinalIgnoreCase)
    {
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
        "Orbitron"
    };

    public static FontFamily CreateFontFamily(string? fontName)
    {
        if (string.IsNullOrWhiteSpace(fontName))
            return FontFamily.Default;

        if (EmbeddedFontFamilies.Contains(fontName))
        {
            return new FontFamily($"avares://PdfEditorApp/Assets/Fonts#{fontName}");
        }

        // Support embedded resource path with system font fallback
        return new FontFamily($"avares://PdfEditorApp/Assets/Fonts#{fontName}, {fontName}");
    }
}
