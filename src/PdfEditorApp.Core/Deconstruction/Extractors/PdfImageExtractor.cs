using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using UglyToad.PdfPig.Content;
using PdfEditorApp.Core.Deconstruction.Utils;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Core.Deconstruction.Extractors;

/// <summary>
/// High-performance SkiaSharp &amp; PdfPig Image Extraction Subsystem.
/// Decodes DCT/JPEG, JPEG2000, uncompressed CMYK, Grayscale, RGB, and 1-bit monochrome bitmaps.
/// Stores raw binary image bytes directly to avoid Large Object Heap (LOH) string fragmentation.
/// </summary>
public static class PdfImageExtractor
{
    /// <summary>
    /// Extracts images from a pure scanned PDF page (where an image covers most of the page canvas).
    /// </summary>
    public static List<PdfImageElement> ExtractScannedCanvasImages(
        IReadOnlyList<IPdfImage> images,
        int pageNumber,
        double pageHeight,
        ref int bgZIndex,
        PdfDeconstructionOptions options,
        ILogger? logger = null)
    {
        var elements = new List<PdfImageElement>();

        foreach (var img in images)
        {
            try
            {
                byte[]? imgBytes = ExtractImageBytes(img, logger);
                if (imgBytes != null && imgBytes.Length > 0)
                {
                    double imgX = Math.Max(0, img.BoundingBox.Left);
                    double imgY = Math.Max(0, pageHeight - img.BoundingBox.Top);
                    double imgW = Math.Max(10, img.BoundingBox.Width);
                    double imgH = Math.Max(10, img.BoundingBox.Height);

                    var scannedImgElement = new PdfImageElement
                    {
                        X = Math.Round(imgX, 1),
                        Y = Math.Round(imgY, 1),
                        Width = Math.Round(imgW, 1),
                        Height = Math.Round(imgH, 1),
                        ImageData = imgBytes,
                        ZIndex = bgZIndex++,
                        IsLocked = false,
                        CornerRadius = 0,
                        BorderThickness = 0,
                        BorderColorHex = "Transparent",
                        AltText = $"Scanned Page {pageNumber} Canvas"
                    };
                    elements.Add(scannedImgElement);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to extract scanned canvas image on page {PageNumber}", pageNumber);
            }
        }

        return elements;
    }

    /// <summary>
    /// Extracts embedded images (photos, logos, figures, icons, watermarks) from a born-digital or mixed PDF page.
    /// </summary>
    public static List<PdfImageElement> ExtractEmbeddedImages(
        IReadOnlyList<IPdfImage> images,
        int pageNumber,
        double pageWidth,
        double pageHeight,
        bool hasSufficientText,
        ref int bgZIndex,
        ref int imgZIndex,
        PdfDeconstructionOptions options,
        ILogger? logger = null)
    {
        var elements = new List<PdfImageElement>();

        foreach (var img in images)
        {
            try
            {
                double imgW = Math.Max(10, img.BoundingBox.Width);
                double imgH = Math.Max(10, img.BoundingBox.Height);

                byte[]? imgBytes = ExtractImageBytes(img, logger);
                if (imgBytes != null && imgBytes.Length > 0)
                {
                    double imgX = Math.Max(0, img.BoundingBox.Left);
                    double imgY = Math.Max(0, pageHeight - img.BoundingBox.Top);

                    // Classify full-page background or centered document watermark
                    bool isFullPageBg = imgW >= pageWidth * options.FullPageBgRatio &&
                                        imgH >= pageHeight * options.FullPageBgRatio &&
                                        hasSufficientText;

                    bool isWatermark = (imgW >= pageWidth * options.WatermarkWidthRatio &&
                                        imgH >= pageHeight * options.WatermarkHeightRatio) &&
                                       hasSufficientText;

                    var imgElement = new PdfImageElement
                    {
                        X = Math.Round(imgX, 1),
                        Y = Math.Round(imgY, 1),
                        Width = Math.Round(imgW, 1),
                        Height = Math.Round(imgH, 1),
                        ImageData = imgBytes,
                        ZIndex = (isFullPageBg || isWatermark) ? bgZIndex++ : imgZIndex++,
                        IsLocked = isFullPageBg || isWatermark,
                        Opacity = isWatermark ? options.WatermarkOpacity : 1.0,
                        CornerRadius = 0,
                        BorderThickness = 0,
                        BorderColorHex = "Transparent",
                        AltText = (isFullPageBg || isWatermark)
                            ? $"Watermark Background ({Math.Round(imgW):F0}x{Math.Round(imgH):F0})"
                            : $"Embedded Image ({Math.Round(imgW):F0}x{Math.Round(imgH):F0})"
                    };
                    elements.Add(imgElement);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to extract embedded image on page {PageNumber}", pageNumber);
            }
        }

        return elements;
    }

    /// <summary>
    /// Decodes raw PDF image samples into a clean, standardized PNG byte array using SkiaSharp with unsafe pointer optimizations.
    /// </summary>
    public static byte[]? ExtractImageBytes(IPdfImage img, ILogger? logger = null)
    {
        // 1. Try native PNG extraction from PdfPig
        try
        {
            if (img.TryGetPng(out var pngBytes) && pngBytes != null && pngBytes.Length >= 8)
            {
                // Verify standard PNG magic header (0x89, 'P', 'N', 'G')
                if (pngBytes[0] == 0x89 && pngBytes[1] == 0x50 && pngBytes[2] == 0x4E && pngBytes[3] == 0x47)
                {
                    return pngBytes;
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "PdfPig native TryGetPng failed; proceeding to SkiaSharp multi-format fallback");
        }

        // 2. Try decoding raw image bytes (JPEG / JPEG2000 / WEBP / BMP) via SkiaSharp
        try
        {
            if (!img.RawBytes.IsEmpty && img.RawBytes.Length > 0)
            {
                var rawArray = img.RawBytes.ToArray();
                using var skData = SKData.CreateCopy(rawArray);
                using var skImg = SKImage.FromEncodedData(skData);
                if (skImg != null)
                {
                    using var encoded = skImg.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }

                // Check for JPEG direct magic header
                if (rawArray.Length > 3 && rawArray[0] == 0xFF && rawArray[1] == 0xD8)
                {
                    using var skBmp = SKBitmap.Decode(rawArray);
                    if (skBmp != null)
                    {
                        using var imgFromBmp = SKImage.FromBitmap(skBmp);
                        using var encoded = imgFromBmp.Encode(SKEncodedImageFormat.Png, 100);
                        if (encoded != null && encoded.Size > 0)
                        {
                            return encoded.ToArray();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "SkiaSharp encoded stream decode failed; proceeding to raw pixel sample extraction");
        }

        // 3. Raw pixel samples conversion via SkiaSharp using unsafe pointer loops (zero bounds-checking overhead)
        try
        {
            int w = img.WidthInSamples;
            int h = img.HeightInSamples;

            if (w > 0 && h > 0 && img.TryGetBytesAsMemory(out var pixelMem) && pixelMem.Length > 0)
            {
                var rawPixels = pixelMem.ToArray();

                // Case A: 24-bit RGB (3 bytes per pixel)
                if (rawPixels.Length >= w * h * 3)
                {
                    using var bitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
                    unsafe
                    {
                        byte* dstPtr = (byte*)bitmap.GetPixels().ToPointer();
                        fixed (byte* srcPtr = rawPixels)
                        {
                            int pixelCount = w * h;
                            byte* src = srcPtr;
                            byte* dst = dstPtr;
                            for (int i = 0; i < pixelCount; i++)
                            {
                                dst[0] = src[0]; // R
                                dst[1] = src[1]; // G
                                dst[2] = src[2]; // B
                                dst[3] = 255;    // A
                                src += 3;
                                dst += 4;
                            }
                        }
                    }

                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }
                // Case B: 8-bit Grayscale (1 byte per pixel)
                else if (rawPixels.Length >= w * h)
                {
                    using var bitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
                    unsafe
                    {
                        byte* dstPtr = (byte*)bitmap.GetPixels().ToPointer();
                        fixed (byte* srcPtr = rawPixels)
                        {
                            int pixelCount = w * h;
                            byte* src = srcPtr;
                            byte* dst = dstPtr;
                            for (int i = 0; i < pixelCount; i++)
                            {
                                byte g = *src++;
                                dst[0] = g;
                                dst[1] = g;
                                dst[2] = g;
                                dst[3] = 255;
                                dst += 4;
                            }
                        }
                    }

                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }
                // Case C: 32-bit CMYK (4 bytes per pixel)
                else if (rawPixels.Length >= w * h * 4)
                {
                    using var bitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
                    unsafe
                    {
                        byte* dstPtr = (byte*)bitmap.GetPixels().ToPointer();
                        fixed (byte* srcPtr = rawPixels)
                        {
                            int pixelCount = w * h;
                            byte* src = srcPtr;
                            byte* dst = dstPtr;
                            for (int i = 0; i < pixelCount; i++)
                            {
                                CmykColorConverter.ConvertCmykToRgb(src[0], src[1], src[2], src[3], out byte r, out byte g, out byte b);
                                dst[0] = r;
                                dst[1] = g;
                                dst[2] = b;
                                dst[3] = 255;
                                src += 4;
                                dst += 4;
                            }
                        }
                    }

                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }
                // Case D: 1-bit Monochrome (e.g. stamps, fax/signatures)
                else if (img.BitsPerComponent == 1)
                {
                    using var bitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque);
                    unsafe
                    {
                        byte* dstPtr = (byte*)bitmap.GetPixels().ToPointer();
                        fixed (byte* srcPtr = rawPixels)
                        {
                            int rowStride = (w + 7) / 8;
                            for (int y = 0; y < h; y++)
                            {
                                byte* rowSrc = srcPtr + (y * rowStride);
                                byte* rowDst = dstPtr + (y * w * 4);
                                for (int x = 0; x < w; x++)
                                {
                                    int byteIdx = x >> 3;
                                    int bitIdx = 7 - (x & 7);
                                    bool isWhite = ((rowSrc[byteIdx] >> bitIdx) & 1) != 0;
                                    byte val = isWhite ? (byte)255 : (byte)0;
                                    rowDst[0] = val;
                                    rowDst[1] = val;
                                    rowDst[2] = val;
                                    rowDst[3] = 255;
                                    rowDst += 4;
                                }
                            }
                        }
                    }

                    using var image = SKImage.FromBitmap(bitmap);
                    using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                    if (encoded != null && encoded.Size > 0)
                    {
                        return encoded.ToArray();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to decode raw pixel sample memory");
        }

        // 4. Raw bytes fallback
        try
        {
            if (!img.RawBytes.IsEmpty && img.RawBytes.Length > 0)
            {
                return img.RawBytes.ToArray();
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed raw bytes fallback for image");
        }

        return null;
    }
}
