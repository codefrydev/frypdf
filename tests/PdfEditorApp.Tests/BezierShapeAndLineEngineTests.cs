using System;
using System.IO;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Templates.Events;
using PdfEditorApp.ViewModels.ElementViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class BezierShapeAndLineEngineTests
{
    [Theory]
    [InlineData(ShapeType.BezierCurve)]
    [InlineData(ShapeType.CurvedArrow)]
    [InlineData(ShapeType.SCurveConnector)]
    [InlineData(ShapeType.WaveLine)]
    [InlineData(ShapeType.ArcLine)]
    [InlineData(ShapeType.CurlyBrace)]
    [InlineData(ShapeType.CurvedCallout)]
    [InlineData(ShapeType.Teardrop)]
    [InlineData(ShapeType.WaveRibbon)]
    [InlineData(ShapeType.OrganicBlob)]
    public void GetVectorPath_GeneratesValidSvgPath_ForBezierShapes(ShapeType shapeType)
    {
        string path = SvgShapeHelper.GetVectorPath(shapeType, 200, 100, 4);

        Assert.False(string.IsNullOrWhiteSpace(path));
        Assert.StartsWith("M ", path);
        Assert.True(path.Contains("C ") || path.Contains("Q ") || path.Contains("L ") || path.Contains("A "));
    }

    [Theory]
    [InlineData(LineDashStyle.Solid, null)]
    [InlineData(LineDashStyle.Dashed, "7.0,4.0")]
    [InlineData(LineDashStyle.Dotted, "2.0,4.0")]
    [InlineData(LineDashStyle.DashDot, "8.0,4.0,2.0,4.0")]
    public void GetDashArray_ReturnsCorrectDashString(LineDashStyle style, string? expectedDash)
    {
        string? result = SvgShapeHelper.GetDashArray(style, 2.0);
        Assert.Equal(expectedDash, result);
    }

    [Theory]
    [InlineData(DividerStyle.Straight)]
    [InlineData(DividerStyle.Wave)]
    [InlineData(DividerStyle.SCurve)]
    [InlineData(DividerStyle.Arch)]
    [InlineData(DividerStyle.DoubleWave)]
    [InlineData(DividerStyle.CalligraphicFlourish)]
    public void GenerateDividerSvgPath_HorizontalAndVertical_ProducesValidPaths(DividerStyle style)
    {
        string hPath = SvgShapeHelper.GenerateDividerSvgPath(style, 400, 24, 6.0, 4.0, false);
        Assert.False(string.IsNullOrWhiteSpace(hPath));
        Assert.StartsWith("M 0,", hPath);

        string vPath = SvgShapeHelper.GenerateDividerSvgPath(style, 24, 400, 6.0, 4.0, true);
        Assert.False(string.IsNullOrWhiteSpace(vPath));
        Assert.StartsWith("M ", vPath);
    }

    [Fact]
    public void GenerateDividerSvgMarkup_GeneratesFullValidSvgDocument()
    {
        var divEl = new PdfDividerElement
        {
            Width = 300,
            Height = 20,
            Thickness = 2.5,
            ColorHex = "#2563EB",
            Style = DividerStyle.Wave,
            WaveAmplitude = 8.0,
            WaveFrequency = 5.0,
            DashStyle = LineDashStyle.Dashed
        };

        string svg = SvgShapeHelper.GenerateDividerSvgMarkup(divEl);

        Assert.Contains("<svg ", svg);
        Assert.Contains("viewBox=\"0 0 300.0 20.0\"", svg);
        Assert.Contains("stroke=\"#2563EB\"", svg);
        Assert.Contains("stroke-width=\"2.5\"", svg);
        Assert.Contains("stroke-dasharray=", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void GenerateSmoothInkSvgPath_ConvertsRawPointsToCubicBezierSpline()
    {
        string points = "10,10 50,40 100,20 180,60 220,30";

        string smoothPath = SvgShapeHelper.GenerateSmoothInkSvgPath(points, isSmoothSpline: true);
        Assert.StartsWith("M 10.0,10.0", smoothPath);
        Assert.Contains(" C ", smoothPath);

        string linearPath = SvgShapeHelper.GenerateSmoothInkSvgPath(points, isSmoothSpline: false);
        Assert.StartsWith("M 10.0,10.0", linearPath);
        Assert.Contains(" L ", linearPath);
        Assert.DoesNotContain(" C ", linearPath);
    }

    [Fact]
    public void GenerateSmoothInkSvgPath_GracefullyHandlesDegeneratePoints()
    {
        Assert.Equal("M 0,0", SvgShapeHelper.GenerateSmoothInkSvgPath(""));
        Assert.StartsWith("M 10.0,20.0", SvgShapeHelper.GenerateSmoothInkSvgPath("10,20"));
        Assert.StartsWith("M 10.0,20.0 L 30.0,40.0", SvgShapeHelper.GenerateSmoothInkSvgPath("10,20 30,40"));
    }

    [Fact]
    public void GenerateInkSvgMarkup_GeneratesValidSvgWithOpacityAndSmoothGeometry()
    {
        var ink = new PdfInkElement
        {
            Width = 200,
            Height = 60,
            StrokeColorHex = "#7C3AED",
            StrokeThickness = 3.5,
            Opacity = 0.85,
            PointsData = "10,10 40,30 80,15 140,50",
            IsSmoothSpline = true
        };

        string svg = SvgShapeHelper.GenerateInkSvgMarkup(ink);

        Assert.Contains("<svg ", svg);
        Assert.Contains("stroke=\"#7C3AED\"", svg);
        Assert.Contains("stroke-width=\"3.5\"", svg);
        Assert.Contains("opacity=\"0.85\"", svg);
        Assert.Contains(" C ", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void ViewModels_ToModelAndLoadFromModel_PreservesBezierProperties()
    {
        // 1. ShapeElementViewModel
        var shapeVm = new ShapeElementViewModel
        {
            ShapeType = ShapeType.BezierCurve,
            StrokeColorHex = "#D97706",
            StrokeThickness = 3.0,
            DashStyle = LineDashStyle.DashDot,
            StartCap = LineEndCap.Circle,
            EndCap = LineEndCap.Arrow,
            WaveFrequency = 3.5,
            CurvatureDepth = 55.0
        };

        var shapeModel = (PdfShapeElement)shapeVm.ToModel();
        Assert.Equal(ShapeType.BezierCurve, shapeModel.ShapeType);
        Assert.Equal(LineDashStyle.DashDot, shapeModel.DashStyle);
        Assert.Equal(LineEndCap.Circle, shapeModel.StartCap);
        Assert.Equal(LineEndCap.Arrow, shapeModel.EndCap);

        var restoredShapeVm = new ShapeElementViewModel();
        restoredShapeVm.LoadFromModel(shapeModel);
        Assert.Equal(ShapeType.BezierCurve, restoredShapeVm.ShapeType);
        Assert.Equal(LineDashStyle.DashDot, restoredShapeVm.DashStyle);
        Assert.Equal(LineEndCap.Circle, restoredShapeVm.StartCap);
        Assert.Equal(LineEndCap.Arrow, restoredShapeVm.EndCap);

        // 2. DividerElementViewModel
        var divVm = new DividerElementViewModel
        {
            Style = DividerStyle.CalligraphicFlourish,
            WaveAmplitude = 9.0,
            WaveFrequency = 6.0,
            DashStyle = LineDashStyle.Dotted,
            Thickness = 2.5
        };

        var divModel = (PdfDividerElement)divVm.ToModel();
        Assert.Equal(DividerStyle.CalligraphicFlourish, divModel.Style);
        Assert.Equal(9.0, divModel.WaveAmplitude);
        Assert.Equal(LineDashStyle.Dotted, divModel.DashStyle);

        var restoredDivVm = new DividerElementViewModel();
        restoredDivVm.LoadFromModel(divModel);
        Assert.Equal(DividerStyle.CalligraphicFlourish, restoredDivVm.Style);
        Assert.Equal(9.0, restoredDivVm.WaveAmplitude);
        Assert.Equal(LineDashStyle.Dotted, restoredDivVm.DashStyle);

        // 3. InkElementViewModel
        var inkVm = new InkElementViewModel
        {
            PointsData = "5,5 25,15 60,35",
            IsSmoothSpline = true,
            StrokeThickness = 4.0
        };

        var inkModel = (PdfInkElement)inkVm.ToModel();
        Assert.True(inkModel.IsSmoothSpline);

        var restoredInkVm = new InkElementViewModel();
        restoredInkVm.LoadFromModel(inkModel);
        Assert.True(restoredInkVm.IsSmoothSpline);
    }

    [Fact]
    public async Task PdfExportService_RendersDocumentWithBezierShapesDividersAndInk_Successfully()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Bézier Vector Features Verification Document",
            Author = "Professional PDF Creator Engine"
        };

        var page = new PdfPageModel
        {
            Width = 595.28,
            Height = 841.89,
            PageNumber = 1
        };

        // Add Bézier curve shape
        page.Elements.Add(new PdfShapeElement
        {
            X = 50,
            Y = 50,
            Width = 300,
            Height = 100,
            ShapeType = ShapeType.BezierCurve,
            FillColorHex = "#00000000",
            StrokeColorHex = "#2563EB",
            StrokeThickness = 3.0,
            DashStyle = LineDashStyle.DashDot
        });

        // Add Curved Arrow
        page.Elements.Add(new PdfShapeElement
        {
            X = 50,
            Y = 170,
            Width = 250,
            Height = 80,
            ShapeType = ShapeType.CurvedArrow,
            FillColorHex = "#00000000",
            StrokeColorHex = "#7C3AED",
            StrokeThickness = 2.5
        });

        // Add Decorative Wave Divider
        page.Elements.Add(new PdfDividerElement
        {
            X = 50,
            Y = 270,
            Width = 495,
            Height = 24,
            Style = DividerStyle.Wave,
            WaveAmplitude = 8.0,
            WaveFrequency = 5.0,
            Thickness = 2.0,
            ColorHex = "#059669"
        });

        // Add Calligraphic Flourish Divider
        page.Elements.Add(new PdfDividerElement
        {
            X = 50,
            Y = 320,
            Width = 495,
            Height = 24,
            Style = DividerStyle.CalligraphicFlourish,
            WaveAmplitude = 7.0,
            Thickness = 2.0,
            ColorHex = "#D97706"
        });

        // Add Freehand Ink Drawing with Catmull-Rom to Cubic Bézier Spline
        page.Elements.Add(new PdfInkElement
        {
            X = 50,
            Y = 380,
            Width = 350,
            Height = 90,
            PointsData = "10,20 60,70 120,30 200,80 280,40 340,75",
            StrokeColorHex = "#DC2626",
            StrokeThickness = 3.0,
            Opacity = 0.9,
            IsSmoothSpline = true
        });

        // Add Curved Callout and Curly Brace
        page.Elements.Add(new PdfShapeElement
        {
            X = 50,
            Y = 500,
            Width = 200,
            Height = 100,
            ShapeType = ShapeType.CurvedCallout,
            FillColorHex = "#EFF6FF",
            StrokeColorHex = "#3B82F6",
            StrokeThickness = 2.0,
            Label = "Bézier Callout"
        });

        page.Elements.Add(new PdfShapeElement
        {
            X = 300,
            Y = 500,
            Width = 40,
            Height = 150,
            ShapeType = ShapeType.CurlyBrace,
            FillColorHex = "#00000000",
            StrokeColorHex = "#475569",
            StrokeThickness = 2.0
        });

        doc.Pages.Add(page);

        var exportService = new PdfExportService();
        string tempPdfPath = Path.Combine(Path.GetTempPath(), $"bezier_shapes_test_{Guid.NewGuid():N}.pdf");

        try
        {
            await exportService.ExportToFileAsync(doc, tempPdfPath);
            Assert.True(File.Exists(tempPdfPath));
            var fileInfo = new FileInfo(tempPdfPath);
            Assert.True(fileInfo.Length > 1000);

            byte[] pdfBytes = await File.ReadAllBytesAsync(tempPdfPath);
            // PDF header magic bytes %PDF-
            Assert.Equal(0x25, pdfBytes[0]); // %
            Assert.Equal(0x50, pdfBytes[1]); // P
            Assert.Equal(0x44, pdfBytes[2]); // D
            Assert.Equal(0x46, pdfBytes[3]); // F
        }
        finally
        {
            if (File.Exists(tempPdfPath))
            {
                File.Delete(tempPdfPath);
            }
        }
    }

    [Fact]
    public void CreativeTypographyShowcaseTemplate_GeneratesValidDocumentWithAllBezierAndTextFeatures()
    {
        var template = new CreativeTypographyShowcaseTemplate();
        Assert.Equal("typographyshowcase", template.Id);
        Assert.Equal("Design & Creative", template.Category);

        var doc = template.Create();
        Assert.NotNull(doc);
        Assert.Single(doc.Pages);

        var page = doc.Pages[0];
        Assert.True(page.Elements.Count >= 20, "Showcase template should have comprehensive elements");

        // Verify Bézier curve text elements exist
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.ShapeMode == TextShapeMode.BezierCurve && text.BezierPreset == BezierCurvePreset.Wave);
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.ShapeMode == TextShapeMode.BezierCurve && text.BezierPreset == BezierCurvePreset.Bridge);

        // Verify Text Outline, Drop Shadow, and Background badges exist
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.HasShadow && text.ShadowBlurRadius > 0);
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.HasStroke && text.StrokeWidth > 0);
        Assert.Contains(page.Elements, e => e is PdfTextElement text && text.BorderColorHex != "#00000000");

        // Verify Decorative Dividers exist
        Assert.Contains(page.Elements, e => e is PdfDividerElement div && div.Style == DividerStyle.CalligraphicFlourish);
        Assert.Contains(page.Elements, e => e is PdfDividerElement div && div.Style == DividerStyle.DoubleWave);
        Assert.Contains(page.Elements, e => e is PdfDividerElement div && div.Style == DividerStyle.Arch);

        // Verify Bézier Shapes & Connectors exist
        Assert.Contains(page.Elements, e => e is PdfShapeElement shape && shape.ShapeType == ShapeType.MedalRibbonBadge);
        Assert.Contains(page.Elements, e => e is PdfShapeElement shape && shape.ShapeType == ShapeType.CurvedCallout);
        Assert.Contains(page.Elements, e => e is PdfShapeElement shape && shape.ShapeType == ShapeType.CurvedArrow);
        Assert.Contains(page.Elements, e => e is PdfShapeElement shape && shape.ShapeType == ShapeType.SCurveConnector);
        Assert.Contains(page.Elements, e => e is PdfShapeElement shape && shape.ShapeType == ShapeType.CurlyBrace);

        // Verify Smooth Bézier Ink Signatures exist
        Assert.Contains(page.Elements, e => e is PdfInkElement ink && ink.IsSmoothSpline && ink.StrokeThickness > 0);
    }

    [Fact]
    public async Task CreativeTypographyShowcaseTemplate_ExportsToValidPdfFile()
    {
        var templateService = new TemplateService();
        var doc = templateService.CreateTypographyShowcaseTemplate();
        Assert.NotNull(doc);

        var exportService = new PdfExportService();
        string tempPdfPath = Path.Combine(Path.GetTempPath(), $"showcase_template_test_{Guid.NewGuid():N}.pdf");

        try
        {
            await exportService.ExportToFileAsync(doc, tempPdfPath);
            Assert.True(File.Exists(tempPdfPath));
            var fileInfo = new FileInfo(tempPdfPath);
            Assert.True(fileInfo.Length > 2000, "Exported PDF with rich vector assets should be substantial in size");

            byte[] pdfBytes = await File.ReadAllBytesAsync(tempPdfPath);
            Assert.Equal(0x25, pdfBytes[0]); // %
            Assert.Equal(0x50, pdfBytes[1]); // P
            Assert.Equal(0x44, pdfBytes[2]); // D
            Assert.Equal(0x46, pdfBytes[3]); // F
        }
        finally
        {
            if (File.Exists(tempPdfPath))
            {
                File.Delete(tempPdfPath);
            }
        }
    }
}
