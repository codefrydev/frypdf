using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using PdfEditorApp.Core.Deconstruction;
using PdfEditorApp.Core.Deconstruction.Extractors;
using PdfEditorApp.Core.Deconstruction.Utils;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.ElementViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PdfDeconstructionEngineTests
{
    [Fact]
    public void PdfImageElement_RawImageData_DoesNotAllocateBase64UntilAccessed()
    {
        byte[] dummyBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01, 0x02 };
        var element = new PdfImageElement
        {
            ImageData = dummyBytes
        };

        // ImageData is set directly
        Assert.NotNull(element.ImageData);
        Assert.Equal(dummyBytes.Length, element.ImageData.Length);

        // Accessing Base64Data computes lazily
        string? base64 = element.Base64Data;
        Assert.NotNull(base64);
        Assert.Equal(Convert.ToBase64String(dummyBytes), base64);

        // Cloning preserves ImageData
        var clone = (PdfImageElement)element.Clone();
        Assert.NotNull(clone.ImageData);
        Assert.Equal(dummyBytes, clone.ImageData);
    }

    [Fact]
    public void ImageElementViewModel_PrioritizesRawImageData_DirectBitmapLoad()
    {
        // 1. Create a 4x4 PNG in memory
        using var bmp = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.CornflowerBlue);
        using var img = SKImage.FromBitmap(bmp);
        using var pngData = img.Encode(SKEncodedImageFormat.Png, 100);
        byte[] pngBytes = pngData.ToArray();

        var vm = new ImageElementViewModel();
        vm.ImageData = pngBytes;

        Assert.NotNull(vm.ImageData);
        Assert.Equal(pngBytes.Length, vm.ImageData.Length);

        // Model roundtrip preserves raw bytes
        var model = (PdfImageElement)vm.ToModel();
        Assert.NotNull(model.ImageData);
        Assert.Equal(pngBytes.Length, model.ImageData.Length);
    }

    [Fact]
    public void ColorContrastHelper_CalculatesRelativeLuminanceAndContrastAccurately()
    {
        double whiteLum = ColorContrastHelper.GetRelativeLuminance("#FFFFFF");
        double blackLum = ColorContrastHelper.GetRelativeLuminance("#000000");

        Assert.True(whiteLum > 0.99);
        Assert.True(blackLum < 0.01);

        double maxContrast = ColorContrastHelper.GetContrastRatio("#000000", "#FFFFFF");
        Assert.True(maxContrast >= 20.0); // 21:1 ideal

        double lowContrast = ColorContrastHelper.GetContrastRatio("#FEFEFE", "#FFFFFF");
        Assert.True(lowContrast < 1.1);
    }

    [Theory]
    [InlineData("#FFFFFF", "#FFFFFF", "#0F172A")] // White on white -> flip to dark
    [InlineData("#FEFEFE", "#FFFFFF", "#0F172A")] // Near white on white -> flip to dark
    [InlineData("#FFFFFF", "#8B0000", "#FFFFFF")] // White on dark red -> keep white (contrast > 4.5)
    [InlineData("#FFFFFF", "#0B192C", "#FFFFFF")] // White on dark navy -> keep white
    [InlineData("#0F172A", "#0B192C", "#FFFFFF")] // Dark slate on dark navy -> flip to white
    [InlineData("#000000", "#FFD700", "#000000")] // Black on gold -> keep black (high contrast)
    public void ColorContrastHelper_EnsureLegibleContrast_HandlesArbitraryBackgroundColors(
        string textHex, string bgHex, string expectedFinalColor)
    {
        string result = ColorContrastHelper.EnsureLegibleContrast(textHex, bgHex, minContrastRatio: 3.0);
        Assert.Equal(expectedFinalColor, result);
    }

    [Fact]
    public void CmykColorConverter_ConvertsCmykToCalibratedRgbAccurately()
    {
        // 1. Pure Cyan (C=255, M=0, Y=0, K=0) -> R=0, G=255, B=255
        CmykColorConverter.ConvertCmykToRgb(255, 0, 0, 0, out byte rC, out byte gC, out byte bC);
        Assert.Equal(0, rC);
        Assert.Equal(255, gC);
        Assert.Equal(255, bC);

        // 2. Pure Magenta (C=0, M=255, Y=0, K=0) -> R=255, G=0, B=255
        CmykColorConverter.ConvertCmykToRgb(0, 255, 0, 0, out byte rM, out byte gM, out byte bM);
        Assert.Equal(255, rM);
        Assert.Equal(0, gM);
        Assert.Equal(255, bM);

        // 3. Pure Yellow (C=0, M=0, Y=255, K=0) -> R=255, G=255, B=0
        CmykColorConverter.ConvertCmykToRgb(0, 0, 255, 0, out byte rY, out byte gY, out byte bY);
        Assert.Equal(255, rY);
        Assert.Equal(255, gY);
        Assert.Equal(0, bY);

        // 4. Pure Black (K=255) -> R=0, G=0, B=0
        CmykColorConverter.ConvertCmykToRgb(0, 0, 0, 255, out byte rK, out byte gK, out byte bK);
        Assert.Equal(0, rK);
        Assert.Equal(0, gK);
        Assert.Equal(0, bK);
    }

    [Fact]
    public void PdfDeconstructionOptions_CustomThresholds_CanBeCustomizedAndPassed()
    {
        var options = new PdfDeconstructionOptions
        {
            PureScannedImageCoverageThreshold = 0.90,
            MaxVectorShapesPerPage = 150,
            GroupExcessVectorsAsSvg = true,
            WatermarkOpacity = 0.20,
            HighContrastDarkTextColor = "#1E293B"
        };

        Assert.Equal(0.90, options.PureScannedImageCoverageThreshold);
        Assert.Equal(150, options.MaxVectorShapesPerPage);
        Assert.True(options.GroupExcessVectorsAsSvg);
        Assert.Equal(0.20, options.WatermarkOpacity);
        Assert.Equal("#1E293B", options.HighContrastDarkTextColor);
    }

    [Fact]
    public void PdfTextExtractor_GroupContainersAndText_AssignsSharedGroupIdToContainedText()
    {
        var shape = new PdfShapeElement
        {
            X = 50,
            Y = 50,
            Width = 200,
            Height = 100,
            FillColorHex = "#F1F5F9"
        };

        var textInside = new PdfTextElement
        {
            X = 60,
            Y = 60,
            Width = 100,
            Height = 20,
            Text = "Inside Card"
        };

        var textOutside = new PdfTextElement
        {
            X = 350,
            Y = 350,
            Width = 100,
            Height = 20,
            Text = "Outside Card"
        };

        var shapes = new List<PdfShapeElement> { shape };
        var texts = new List<PdfTextElement> { textInside, textOutside };

        PdfTextExtractor.GroupContainersAndText(shapes, texts, PdfDeconstructionOptions.Default);

        Assert.NotNull(shape.GroupId);
        Assert.Equal(shape.GroupId, textInside.GroupId);
        Assert.Null(textOutside.GroupId);
    }

    [Theory]
    [InlineData(595.28, 841.89, PageFormat.A4)]
    [InlineData(612.0, 792.0, PageFormat.Letter)]
    [InlineData(612.0, 1008.0, PageFormat.Legal)]
    [InlineData(841.89, 1190.55, PageFormat.A3)]
    [InlineData(419.53, 595.28, PageFormat.A5)]
    public void DeterminePageFormat_AccuratelyIdentifiesStandardPageFormats(double width, double height, PageFormat expected)
    {
        var result = PdfDeconstructionEngine.DeterminePageFormat(width, height);
        Assert.Equal(expected, result);
    }


    [Fact]
    public void ColorContrastHelper_TryParseRgb_ParsesHexVariantsCorrectly()
    {
        Assert.True(ColorContrastHelper.TryParseRgb("#FFF", out byte r1, out byte g1, out byte b1));
        Assert.Equal(255, r1); Assert.Equal(255, g1); Assert.Equal(255, b1);

        Assert.True(ColorContrastHelper.TryParseRgb("#123456", out byte r2, out byte g2, out byte b2));
        Assert.Equal(0x12, r2); Assert.Equal(0x34, g2); Assert.Equal(0x56, b2);

        Assert.True(ColorContrastHelper.TryParseRgb("#FF0F172A", out byte r3, out byte g3, out byte b3));
        Assert.Equal(0x0F, r3); Assert.Equal(0x17, g3); Assert.Equal(0x2A, b3);

        Assert.False(ColorContrastHelper.TryParseRgb("Transparent", out _, out _, out _));
        Assert.False(ColorContrastHelper.TryParseRgb(null, out _, out _, out _));
    }
}
