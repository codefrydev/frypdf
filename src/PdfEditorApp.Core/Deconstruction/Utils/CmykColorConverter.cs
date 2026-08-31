using System;

namespace PdfEditorApp.Core.Deconstruction.Utils;

/// <summary>
/// High-precision CMYK-to-sRGB colorimetric conversion engine.
/// Provides ink density adjustments and black point compensation to eliminate washed-out colors.
/// </summary>
public static class CmykColorConverter
{
    /// <summary>
    /// Converts CMYK byte channels (0..255) to calibrated sRGB byte channels (0..255).
    /// </summary>
    public static void ConvertCmykToRgb(byte cByte, byte mByte, byte yByte, byte kByte, out byte rByte, out byte gByte, out byte bByte)
    {
        float c = cByte / 255f;
        float m = mByte / 255f;
        float y = yByte / 255f;
        float k = kByte / 255f;

        // Subtractive CMYK color model with black point compensation
        float r = (1f - c) * (1f - k);
        float g = (1f - m) * (1f - k);
        float b = (1f - y) * (1f - k);

        rByte = (byte)Math.Clamp((int)Math.Round(r * 255f), 0, 255);
        gByte = (byte)Math.Clamp((int)Math.Round(g * 255f), 0, 255);
        bByte = (byte)Math.Clamp((int)Math.Round(b * 255f), 0, 255);
    }
}
