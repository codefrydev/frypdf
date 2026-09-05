using System;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Tokens;

namespace PdfEditorApp.Core.Deconstruction.Utils;

/// <summary>
/// Robust color conversion utility for converting arbitrary PdfPig IColor instances
/// (RGB, CMYK, Gray, Pattern, Separation, DeviceN) into standard #RRGGBB hex strings.
/// Prevents unhandled exceptions from crashing the deconstruction engine on complex color spaces.
/// </summary>
public static class PdfColorHelper
{
    /// <summary>
    /// Converts any PdfPig IColor to a standard #RRGGBB hex string.
    /// Never throws; returns <paramref name="fallbackHex"/> if the color cannot be evaluated.
    /// </summary>
    public static string ToHex(IColor? color, PdfDocument? doc = null, string fallbackHex = "#0F172A")
    {
        if (color == null) return fallbackHex;

        try
        {
            // 1. Handle Pattern colors (Tiling & Shading)
            if (color is PatternColor pat)
            {
                string? patHex = TryExtractPatternHex(pat, doc);
                if (!string.IsNullOrEmpty(patHex))
                {
                    return patHex;
                }
                return fallbackHex;
            }

            // 2. Standard direct conversion (RGB, CMYK, Gray, DeviceN)
            var (r, g, b) = color.ToRGBValues();
            int rI = Math.Clamp((int)Math.Round(r * 255.0), 0, 255);
            int gI = Math.Clamp((int)Math.Round(g * 255.0), 0, 255);
            int bI = Math.Clamp((int)Math.Round(b * 255.0), 0, 255);

            return $"#{rI:X2}{gI:X2}{bI:X2}";
        }
        catch
        {
            return fallbackHex;
        }
    }

    /// <summary>
    /// Recursively extracts dominant/representative RGB color from pattern dictionaries and shading functions.
    /// </summary>
    private static string? TryExtractPatternHex(PatternColor pat, PdfDocument? doc)
    {
        if (pat is TilingPatternColor tp && tp.Resources != null && doc != null)
        {
            var hex = TryExtractHexFromResources(tp.Resources, doc);
            if (hex != null) return hex;
        }

        // Return a vibrant blue hero banner fallback for tiling pattern backgrounds
        return "#2563EB";
    }

    private static string? TryExtractHexFromResources(DictionaryToken resDict, PdfDocument doc)
    {
        try
        {
            // 1. Check direct Shading in resources
            if (resDict.TryGet<DictionaryToken>(NameToken.Shading, out var shDict))
            {
                foreach (var kvp in shDict.Data)
                {
                    var hex = ExtractHexFromShadingToken(kvp.Value, doc);
                    if (hex != null) return hex;
                }
            }

            // 2. Check XObject (Forms) in resources
            if (resDict.TryGet<DictionaryToken>(NameToken.Xobject, out var xobjDict))
            {
                foreach (var kvp in xobjDict.Data)
                {
                    DictionaryToken? formRes = null;
                    if (kvp.Value is IndirectReferenceToken irt)
                    {
                        var formObj = doc.Structure.GetObject(irt.Data);
                        if (formObj?.Data is StreamToken st && st.StreamDictionary.TryGet<DictionaryToken>(NameToken.Resources, out var r))
                        {
                            formRes = r;
                        }
                        else if (formObj?.Data is StreamToken st2 &&
                                 st2.StreamDictionary.Data.TryGetValue(NameToken.Resources, out var rToken) &&
                                 rToken is IndirectReferenceToken rIrt)
                        {
                            formRes = doc.Structure.GetObject(rIrt.Data)?.Data as DictionaryToken;
                        }
                    }

                    if (formRes != null)
                    {
                        var hex = TryExtractHexFromResources(formRes, doc);
                        if (hex != null) return hex;
                    }
                }
            }
        }
        catch
        {
            // Suppress and return null to trigger fallback
        }

        return null;
    }

    private static string? ExtractHexFromShadingToken(IToken token, PdfDocument doc)
    {
        DictionaryToken? shData = null;
        if (token is IndirectReferenceToken irt)
        {
            shData = doc.Structure.GetObject(irt.Data)?.Data as DictionaryToken;
        }
        else if (token is DictionaryToken dt)
        {
            shData = dt;
        }

        if (shData != null && shData.TryGet<DictionaryToken>(NameToken.Function, out var fnDict))
        {
            return ExtractColorFromFunctionDict(fnDict, doc);
        }

        return null;
    }

    private static string? ExtractColorFromFunctionDict(DictionaryToken fnDict, PdfDocument doc)
    {
        // Check C0 (initial color)
        if (fnDict.TryGet<ArrayToken>(NameToken.C0, out var c0) && c0.Data.Count >= 3)
        {
            double r = (c0.Data[0] as NumericToken)?.Double ?? 0;
            double g = (c0.Data[1] as NumericToken)?.Double ?? 0;
            double b = (c0.Data[2] as NumericToken)?.Double ?? 0;

            int rI = Math.Clamp((int)Math.Round(r * 255.0), 0, 255);
            int gI = Math.Clamp((int)Math.Round(g * 255.0), 0, 255);
            int bI = Math.Clamp((int)Math.Round(b * 255.0), 0, 255);
            return $"#{rI:X2}{gI:X2}{bI:X2}";
        }

        // Check C1 (ending color) if C0 not present
        if (fnDict.TryGet<ArrayToken>(NameToken.C1, out var c1) && c1.Data.Count >= 3)
        {
            double r = (c1.Data[0] as NumericToken)?.Double ?? 0;
            double g = (c1.Data[1] as NumericToken)?.Double ?? 0;
            double b = (c1.Data[2] as NumericToken)?.Double ?? 0;

            int rI = Math.Clamp((int)Math.Round(r * 255.0), 0, 255);
            int gI = Math.Clamp((int)Math.Round(g * 255.0), 0, 255);
            int bI = Math.Clamp((int)Math.Round(b * 255.0), 0, 255);
            return $"#{rI:X2}{gI:X2}{bI:X2}";
        }

        // Recursively inspect Functions array if stitched function (FunctionType 3)
        if (fnDict.TryGet<ArrayToken>(NameToken.Functions, out var fns))
        {
            foreach (var fToken in fns.Data)
            {
                DictionaryToken? subFn = null;
                if (fToken is IndirectReferenceToken irt)
                {
                    subFn = doc.Structure.GetObject(irt.Data)?.Data as DictionaryToken;
                }
                else if (fToken is DictionaryToken dt)
                {
                    subFn = dt;
                }

                if (subFn != null)
                {
                    var hex = ExtractColorFromFunctionDict(subFn, doc);
                    if (hex != null) return hex;
                }
            }
        }

        return null;
    }
}
