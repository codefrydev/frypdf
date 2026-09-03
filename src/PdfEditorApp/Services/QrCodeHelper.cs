using System;
using System.Globalization;
using System.IO;
using Avalonia.Media.Imaging;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using QRCoder;

namespace PdfEditorApp.Services;

/// <summary>
/// High-performance QR Code generation helper supporting live Avalonia bitmap previews,
/// custom brand colors, error correction levels, and vector/raster export bytes.
/// </summary>
public static class QrCodeHelper
{
    public static byte[] HexToRgba(string? hex, byte defaultR, byte defaultG, byte defaultB, byte defaultA = 255)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return new byte[] { defaultR, defaultG, defaultB, defaultA };
        }

        string clean = hex.Trim().TrimStart('#');
        try
        {
            if (clean.Length == 6)
            {
                byte r = byte.Parse(clean.Substring(0, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(clean.Substring(2, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(clean.Substring(4, 2), NumberStyles.HexNumber);
                return new byte[] { r, g, b, 255 };
            }
            if (clean.Length == 8)
            {
                byte a = byte.Parse(clean.Substring(0, 2), NumberStyles.HexNumber);
                byte r = byte.Parse(clean.Substring(2, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(clean.Substring(4, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(clean.Substring(6, 2), NumberStyles.HexNumber);
                return new byte[] { r, g, b, a };
            }
            if (clean.Length == 3)
            {
                byte r = (byte)(Convert.ToByte(clean[0].ToString(), 16) * 17);
                byte g = (byte)(Convert.ToByte(clean[1].ToString(), 16) * 17);
                byte b = (byte)(Convert.ToByte(clean[2].ToString(), 16) * 17);
                return new byte[] { r, g, b, 255 };
            }
        }
        catch
        {
            // fallback on format error
        }

        return new byte[] { defaultR, defaultG, defaultB, defaultA };
    }

    public static QRCodeGenerator.ECCLevel ToQRCoderEcc(QrCodeEccLevel level)
    {
        return level switch
        {
            QrCodeEccLevel.L => QRCodeGenerator.ECCLevel.L,
            QrCodeEccLevel.M => QRCodeGenerator.ECCLevel.M,
            QrCodeEccLevel.Q => QRCodeGenerator.ECCLevel.Q,
            QrCodeEccLevel.H => QRCodeGenerator.ECCLevel.H,
            _ => QRCodeGenerator.ECCLevel.M
        };
    }

    public static byte[] GeneratePngBytes(
        string? content,
        string? darkHex = "#0F172A",
        string? lightHex = "#FFFFFF",
        QrCodeEccLevel ecc = QrCodeEccLevel.M,
        int pixelsPerModule = 10,
        bool drawQuietZones = true)
    {
        string payload = string.IsNullOrWhiteSpace(content) ? "https://codefrydev.in" : content;
        byte[] darkRgba = HexToRgba(darkHex, 15, 23, 42); // #0F172A
        byte[] lightRgba = HexToRgba(lightHex, 255, 255, 255); // #FFFFFF

        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(payload, ToQRCoderEcc(ecc));
        using var qrCode = new PngByteQRCode(qrData);

        return qrCode.GetGraphic(pixelsPerModule, darkRgba, lightRgba, drawQuietZones);
    }

    public static Bitmap? GenerateAvaloniaBitmap(
        string? content,
        string? darkHex = "#0F172A",
        string? lightHex = "#FFFFFF",
        QrCodeEccLevel ecc = QrCodeEccLevel.M,
        int pixelsPerModule = 8,
        bool drawQuietZones = true)
    {
        try
        {
            byte[] bytes = GeneratePngBytes(content, darkHex, lightHex, ecc, pixelsPerModule, drawQuietZones);
            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }
}
