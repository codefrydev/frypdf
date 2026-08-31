using System;
using System.Globalization;

namespace PdfEditorApp.Core.Deconstruction.Utils;

/// <summary>
/// WCAG 2.1 compliant color luminance and contrast calculation helper.
/// Used to dynamically protect text readability against arbitrary background shapes and cards.
/// </summary>
public static class ColorContrastHelper
{
    /// <summary>
    /// Calculates the relative luminance of a given hex color according to WCAG 2.1 guidelines.
    /// Returns a value between 0.0 (darkest black) and 1.0 (lightest white).
    /// </summary>
    public static double GetRelativeLuminance(string hexColor)
    {
        if (!TryParseRgb(hexColor, out byte r, out byte g, out byte b))
        {
            // Default to white canvas luminance
            return 1.0;
        }

        double rLin = Linearize(r / 255.0);
        double gLin = Linearize(g / 255.0);
        double bLin = Linearize(b / 255.0);

        return 0.2126 * rLin + 0.7152 * gLin + 0.0722 * bLin;
    }

    /// <summary>
    /// Calculates the WCAG contrast ratio between two colors.
    /// Result ranges from 1:1 (no contrast) to 21:1 (maximum contrast, e.g. pure black on pure white).
    /// </summary>
    public static double GetContrastRatio(string color1Hex, string color2Hex)
    {
        double l1 = GetRelativeLuminance(color1Hex);
        double l2 = GetRelativeLuminance(color2Hex);

        double lighter = Math.Max(l1, l2);
        double darker = Math.Min(l1, l2);

        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Evaluates the contrast of text color against an underlying background color.
    /// If the contrast ratio is below <paramref name="minContrastRatio"/>, dynamically adjusts the text color
    /// to either <paramref name="darkFallback"/> or <paramref name="lightFallback"/> depending on background luminance.
    /// </summary>
    public static string EnsureLegibleContrast(
        string textColorHex,
        string backgroundColorHex,
        double minContrastRatio = 3.0,
        string darkFallback = "#0F172A",
        string lightFallback = "#FFFFFF")
    {
        if (string.IsNullOrWhiteSpace(backgroundColorHex) ||
            string.Equals(backgroundColorHex, "Transparent", StringComparison.OrdinalIgnoreCase))
        {
            backgroundColorHex = "#FFFFFF";
        }

        double contrast = GetContrastRatio(textColorHex, backgroundColorHex);
        if (contrast >= minContrastRatio)
        {
            return textColorHex;
        }

        double bgLuminance = GetRelativeLuminance(backgroundColorHex);
        return bgLuminance > 0.5 ? darkFallback : lightFallback;
    }

    public static bool TryParseRgb(string? hex, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex)) return false;

        string clean = hex.Trim().TrimStart('#');
        if (string.Equals(clean, "Transparent", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (clean.Length == 3)
        {
            if (byte.TryParse(new string(clean[0], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) &&
                byte.TryParse(new string(clean[1], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) &&
                byte.TryParse(new string(clean[2], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
            {
                return true;
            }
        }
        else if (clean.Length == 6)
        {
            if (byte.TryParse(clean.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) &&
                byte.TryParse(clean.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) &&
                byte.TryParse(clean.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
            {
                return true;
            }
        }
        else if (clean.Length == 8)
        {
            // AARRGGBB or RRGGBBAA -> treat as AARRGGBB standard
            if (byte.TryParse(clean.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) &&
                byte.TryParse(clean.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) &&
                byte.TryParse(clean.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
            {
                return true;
            }
        }

        return false;
    }

    private static double Linearize(double val)
    {
        return val <= 0.04045 ? val / 12.92 : Math.Pow((val + 0.055) / 1.055, 2.4);
    }
}
