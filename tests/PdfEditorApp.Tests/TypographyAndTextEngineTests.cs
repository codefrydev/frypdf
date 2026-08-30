using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Typography;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.Tests;

public class TypographyAndTextEngineTests
{
    [Fact]
    public void PdfTextElement_DefaultValues_AreSensible()
    {
        var textEl = new PdfTextElement();

        Assert.Equal("Enter text here", textEl.Text);
        Assert.Equal(14.0, textEl.FontSize);
        Assert.Equal("#201F1E", textEl.TextColorHex);
        Assert.Equal(1.0, textEl.TextOpacity);
        Assert.Equal(TextAlignmentMode.Left, textEl.Alignment);
        Assert.Equal(TextVerticalAlignment.Top, textEl.VerticalAlignment);
        Assert.Equal(TextShapeMode.Normal, textEl.ShapeMode);
        Assert.False(textEl.HasStroke);
        Assert.False(textEl.HasShadow);
        Assert.False(textEl.IsDoubleUnderline);
        Assert.Equal(1.0, textEl.ScaleX);
        Assert.Equal(1.0, textEl.ScaleY);
        Assert.False(textEl.FlipX);
        Assert.False(textEl.FlipY);
    }

    [Fact]
    public void PdfTextElement_JsonSerialization_RoundTripPreservesAllAdvancedProperties()
    {
        var original = new PdfTextElement
        {
            Id = Guid.NewGuid().ToString(),
            X = 50,
            Y = 100,
            Width = 300,
            Height = 150,
            Text = "Curved Badge Typography",
            FontFamily = "Inter",
            FontSize = 24,
            IsBold = true,
            IsItalic = true,
            IsUnderline = true,
            IsDoubleUnderline = true,
            IsStrikethrough = false,
            TextColorHex = "#0F6CBD",
            TextOpacity = 0.9,
            Alignment = TextAlignmentMode.Center,
            VerticalAlignment = TextVerticalAlignment.Center,
            LineHeight = 1.4,
            CharacterSpacing = 2.5,
            WordSpacing = 5.0,
            ParagraphSpacing = 8.0,
            TextWrap = true,
            HasStroke = true,
            StrokeColorHex = "#1E293B",
            StrokeWidth = 2.0,
            HasShadow = true,
            ShadowColorHex = "#80000000",
            ShadowOffsetX = 3.0,
            ShadowOffsetY = 4.0,
            ShadowBlurRadius = 6.0,
            ShadowOpacity = 0.75,
            BackgroundColorHex = "#FEF08A",
            CornerRadius = 8.0,
            BorderColorHex = "#CA8A04",
            BorderThickness = 1.5,
            Padding = 12.0,
            ShapeMode = TextShapeMode.Curved,
            CurveRadius = 150.0,
            CurveArcAngle = 180.0,
            CurveStartAngle = -90.0,
            CurveClockwise = true,
            CurveInvert = false,
            CircularPlacement = CircularTextPlacement.TopArc,
            BaselineShift = 5.0,
            CharacterRotation = 10.0,
            ScaleX = 1.2,
            ScaleY = 0.9,
            FlipX = false,
            FlipY = false
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<PdfTextElement>(json, options);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Text, deserialized.Text);
        Assert.Equal(original.FontSize, deserialized.FontSize);
        Assert.True(deserialized.IsBold);
        Assert.True(deserialized.IsDoubleUnderline);
        Assert.Equal(TextShapeMode.Curved, deserialized.ShapeMode);
        Assert.Equal(150.0, deserialized.CurveRadius);
        Assert.Equal(180.0, deserialized.CurveArcAngle);
        Assert.True(deserialized.HasStroke);
        Assert.Equal("#1E293B", deserialized.StrokeColorHex);
        Assert.True(deserialized.HasShadow);
        Assert.Equal(3.0, deserialized.ShadowOffsetX);
        Assert.Equal(1.2, deserialized.ScaleX);
    }

    [Fact]
    public void PdfTextElement_BackwardCompatibility_LoadsLegacyJsonWithoutError()
    {
        // Legacy JSON schema with only basic fields
        string legacyJson = """
        {
            "Id": "legacy-text-1",
            "X": 10,
            "Y": 20,
            "Width": 200,
            "Height": 50,
            "Text": "Legacy Text",
            "FontFamily": "Arial",
            "FontSize": 14,
            "TextColorHex": "#000000",
            "Alignment": 0,
            "IsBold": false,
            "IsItalic": false
        }
        """;

        var el = JsonSerializer.Deserialize<PdfTextElement>(legacyJson);
        Assert.NotNull(el);
        Assert.Equal("legacy-text-1", el.Id);
        Assert.Equal("Legacy Text", el.Text);
        Assert.Equal(TextShapeMode.Normal, el.ShapeMode);
        Assert.False(el.HasStroke);
        Assert.False(el.HasShadow);
        Assert.Equal(1.0, el.ScaleX);
    }

    [Fact]
    public void PdfTextElement_Clone_CreatesDeepCopy()
    {
        var original = new PdfTextElement
        {
            Text = "Master Copy",
            ShapeMode = TextShapeMode.Circular,
            CurveRadius = 120,
            HasStroke = true,
            StrokeWidth = 3.0
        };

        var clone = (PdfTextElement)original.Clone();
        Assert.NotEqual(original.Id, clone.Id);
        Assert.Equal(original.Text, clone.Text);
        Assert.Equal(original.ShapeMode, clone.ShapeMode);
        Assert.Equal(original.CurveRadius, clone.CurveRadius);
        Assert.Equal(original.HasStroke, clone.HasStroke);
        Assert.Equal(original.StrokeWidth, clone.StrokeWidth);

        clone.Text = "Modified Copy";
        Assert.Equal("Master Copy", original.Text);
    }

    [Fact]
    public void TextLayoutEngine_MeasureGlyphWidth_ReturnsExpectedProportions()
    {
        double widthW = TextLayoutEngine.MeasureGlyphWidth('W', "Arial", 20, isBold: false, isItalic: false);
        double widthI = TextLayoutEngine.MeasureGlyphWidth('I', "Arial", 20, isBold: false, isItalic: false);

        Assert.True(widthW > widthI, "W should be significantly wider than I in proportional fonts");
        Assert.True(widthW > 0);
        Assert.True(widthI > 0);

        double widthBold = TextLayoutEngine.MeasureGlyphWidth('W', "Arial", 20, isBold: true, isItalic: false);
        Assert.True(widthBold >= widthW, "Bold glyphs should be slightly wider or equal to regular glyphs");
    }

    [Fact]
    public void TextLayoutEngine_CalculateNormalLayout_WordWrapsCorrectly()
    {
        string text = "First line here\nSecond line is quite long and should wrap when width is small enough";
        var result = TextLayoutEngine.CalculateNormalLayout(
            text,
            "Arial",
            16,
            isBold: false,
            isItalic: false,
            availableWidth: 120,
            lineHeightMultiplier: 1.2,
            charSpacing: 0,
            wordSpacing: 0,
            paragraphSpacing: 0,
            alignment: TextAlignmentMode.Left,
            vAlign: TextVerticalAlignment.Top,
            boxHeight: 200,
            wrap: true,
            padding: 4
        );

        Assert.True(result.Lines.Count >= 2, "Expected multiple lines from newline and word wrapping");

        foreach (var line in result.Lines)
        {
            Assert.False(string.IsNullOrEmpty(line.Text));
            Assert.True(line.Width > 0);
            Assert.True(line.Height > 0);
        }
    }

    [Fact]
    public void TextLayoutEngine_CalculateCurvedGlyphs_CalculatesValidGlyphTransforms()
    {
        string text = "CURVED HEADING";
        var layoutResult = TextLayoutEngine.CalculateCurvedGlyphs(
            text,
            "Arial",
            20,
            isBold: true,
            isItalic: false,
            boxWidth: 300,
            boxHeight: 150,
            radius: 100,
            arcAngleDeg: 120,
            startAngleDeg: -60,
            clockwise: true,
            invert: false,
            charSpacing: 0,
            circularPlacement: CircularTextPlacement.TopArc,
            shapeMode: TextShapeMode.Curved,
            baselineShift: 0
        );

        Assert.Equal(text.Length, layoutResult.Glyphs.Count);

        for (int i = 0; i < layoutResult.Glyphs.Count; i++)
        {
            var g = layoutResult.Glyphs[i];
            Assert.Equal(text[i].ToString(), g.Text);
            Assert.True(!double.IsNaN(g.X) && !double.IsInfinity(g.X));
            Assert.True(!double.IsNaN(g.Y) && !double.IsInfinity(g.Y));
            Assert.True(!double.IsNaN(g.TangentAngleDeg) && !double.IsInfinity(g.TangentAngleDeg));
        }
    }

    [Fact]
    public void TextLayoutEngine_CalculateCircularGlyphs_PositionsGlyphsAroundCircle()
    {
        string text = "CIRCULAR BADGE EMBLEM";
        double width = 200;
        double height = 200;
        double radius = 80;

        var layoutResult = TextLayoutEngine.CalculateCurvedGlyphs(
            text,
            "Arial",
            14,
            isBold: true,
            isItalic: false,
            boxWidth: width,
            boxHeight: height,
            radius: radius,
            arcAngleDeg: 360,
            startAngleDeg: -90,
            clockwise: true,
            invert: false,
            charSpacing: 0,
            circularPlacement: CircularTextPlacement.FullCircle,
            shapeMode: TextShapeMode.Circular,
            baselineShift: 0
        );

        Assert.Equal(text.Length, layoutResult.Glyphs.Count);

        double centerX = width / 2.0;
        double centerY = height / 2.0;

        foreach (var g in layoutResult.Glyphs)
        {
            if (string.IsNullOrWhiteSpace(g.Text)) continue;
            double dx = g.X - centerX;
            double dy = g.Y - centerY;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            // Distance should be close to radius
            Assert.InRange(dist, 40, 120);
        }
    }

    [Fact]
    public void TextLayoutEngine_GenerateSvgMarkup_ProducesValidVectorMarkup()
    {
        var textEl = new PdfTextElement
        {
            Text = "VECTOR SVG EXPORT",
            Width = 250,
            Height = 100,
            FontSize = 18,
            TextColorHex = "#2563EB",
            HasStroke = true,
            StrokeColorHex = "#1E293B",
            StrokeWidth = 1.5,
            HasShadow = true,
            ShadowColorHex = "#40000000",
            ShapeMode = TextShapeMode.Curved,
            CurveRadius = 120,
            CurveArcAngle = 90
        };

        string svg = TextLayoutEngine.GenerateSvgMarkup(textEl);
        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg);
        Assert.EndsWith("</svg>", svg);
        Assert.Contains("xmlns=\"http://www.w3.org/2000/svg\"", svg);
        Assert.Contains("<defs>", svg);
        Assert.Contains("<filter id=\"dropShadow\"", svg);
        Assert.Contains("stroke=\"#1E293B\"", svg);
        Assert.Contains("fill=\"#2563EB\"", svg);
    }

    [Fact]
    public void TextElementViewModel_Presets_ApplyCorrectValues()
    {
        var el = new PdfTextElement { Width = 200, Height = 100, FontSize = 16 };
        var vm = new TextElementViewModel(el);

        vm.ApplyTypographyPreset("archup");
        Assert.Equal(TextShapeMode.Curved, vm.ShapeMode);
        Assert.True(vm.CurveClockwise);

        vm.ApplyTypographyPreset("circlebadge");
        Assert.Equal(TextShapeMode.Circular, vm.ShapeMode);
        Assert.Equal(CircularTextPlacement.FullCircle, vm.CircularPlacement);

        vm.ApplyTypographyPreset("outlined");
        Assert.True(vm.HasStroke);
        Assert.Equal(1.5, vm.StrokeWidth);

        vm.ApplyTypographyPreset("neonglow");
        Assert.True(vm.HasShadow);
        Assert.True(vm.HasStroke);

        vm.ApplyTypographyPreset("normal");
        Assert.Equal(TextShapeMode.Normal, vm.ShapeMode);
        Assert.False(vm.HasStroke);
        Assert.False(vm.HasShadow);
    }

    [Fact]
    public void TextElementViewModel_CaseTransforms_TransformText()
    {
        var el = new PdfTextElement { Text = "hello world of PDF typography" };
        var vm = new TextElementViewModel(el);

        vm.TransformUppercase();
        Assert.Equal("HELLO WORLD OF PDF TYPOGRAPHY", vm.Text);

        vm.TransformLowercase();
        Assert.Equal("hello world of pdf typography", vm.Text);

        vm.TransformTitleCase();
        Assert.Equal("Hello World Of Pdf Typography", vm.Text);
    }

    [Fact]
    public void TextElementViewModel_ListToggles_AddAndRemovePrefixes()
    {
        var el = new PdfTextElement { Text = "First Item\nSecond Item" };
        var vm = new TextElementViewModel(el);

        vm.ToggleBulletList();
        Assert.Contains("• First Item", vm.Text);
        Assert.Contains("• Second Item", vm.Text);

        vm.ToggleBulletList();
        Assert.DoesNotContain("•", vm.Text);

        vm.ToggleNumberedList();
        Assert.Contains("1. First Item", vm.Text);
        Assert.Contains("2. Second Item", vm.Text);
    }

    [Fact]
    public async Task PdfExportService_ExportsTypographyDocumentToPdf_Successfully()
    {
        var exportService = new PdfExportService();
        var doc = new PdfDocumentModel
        {
            Title = "Typography Showcase Test",
            Author = "Automated Test Suite"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Width = 595,
            Height = 842
        };

        // 1. Standard text element
        page.Elements.Add(new PdfTextElement
        {
            X = 50,
            Y = 50,
            Width = 400,
            Height = 40,
            Text = "Standard PDF Text Heading",
            FontSize = 22,
            IsBold = true,
            TextColorHex = "#1E293B"
        });

        // 2. Curved text element
        page.Elements.Add(new PdfTextElement
        {
            X = 50,
            Y = 120,
            Width = 400,
            Height = 120,
            Text = "ARC TYPOGRAPHY VECTOR CURVE",
            FontSize = 18,
            ShapeMode = TextShapeMode.Curved,
            CurveRadius = 140,
            CurveArcAngle = 120,
            TextColorHex = "#0F6CBD",
            IsBold = true
        });

        // 3. Circular badge element
        page.Elements.Add(new PdfTextElement
        {
            X = 150,
            Y = 280,
            Width = 200,
            Height = 200,
            Text = "★ OFFICIAL SEAL & EMBLEM ★ QUALITY",
            FontSize = 13,
            ShapeMode = TextShapeMode.Circular,
            CircularPlacement = CircularTextPlacement.FullCircle,
            CurveRadius = 80,
            TextColorHex = "#D97706",
            IsBold = true
        });

        // 4. Stroked & Shadowed text element
        page.Elements.Add(new PdfTextElement
        {
            X = 50,
            Y = 520,
            Width = 400,
            Height = 60,
            Text = "OUTLINED & SHADOWED TITLE",
            FontSize = 26,
            IsBold = true,
            TextColorHex = "#FFFFFF",
            HasStroke = true,
            StrokeColorHex = "#7C3AED",
            StrokeWidth = 2.0,
            HasShadow = true,
            ShadowColorHex = "#80000000",
            ShadowOffsetX = 3,
            ShadowOffsetY = 3,
            ShadowBlurRadius = 4
        });

        doc.Pages.Add(page);

        string tempPdfPath = Path.Combine(Path.GetTempPath(), $"typography_test_{Guid.NewGuid():N}.pdf");
        try
        {
            await exportService.ExportToFileAsync(doc, tempPdfPath);
            Assert.True(File.Exists(tempPdfPath), "Exported PDF file should exist");
            var fileInfo = new FileInfo(tempPdfPath);
            Assert.True(fileInfo.Length > 500, "Exported PDF file should be non-empty and well-formed");
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
    public void InspectorViewModel_TypographyCommands_SupportUndoRedo()
    {
        var undoRedo = new UndoRedoService();
        var inspector = new InspectorViewModel
        {
            UndoRedo = undoRedo
        };

        var textEl = new PdfTextElement
        {
            Text = "Undoable Text",
            ShapeMode = TextShapeMode.Normal,
            HasStroke = false,
            HasShadow = false,
            IsDoubleUnderline = false
        };
        var vm = new TextElementViewModel(textEl);
        inspector.SelectedElement = vm;

        // 1. Toggle Double Underline
        Assert.False(vm.IsDoubleUnderline);
        inspector.ToggleDoubleUnderlineCommand.Execute(null);
        Assert.True(vm.IsDoubleUnderline);
        Assert.True(undoRedo.CanUndo);

        undoRedo.Undo();
        Assert.False(vm.IsDoubleUnderline);

        undoRedo.Redo();
        Assert.True(vm.IsDoubleUnderline);

        // 2. Change Shape Mode
        inspector.SetTextShapeModeCommand.Execute("Curved");
        Assert.Equal(TextShapeMode.Curved, vm.ShapeMode);

        undoRedo.Undo();
        Assert.Equal(TextShapeMode.Normal, vm.ShapeMode);

        // 3. Toggle Stroke
        inspector.ToggleTextStrokeCommand.Execute(null);
        Assert.True(vm.HasStroke);

        undoRedo.Undo();
        Assert.False(vm.HasStroke);

        // 4. Toggle Shadow
        inspector.ToggleTextShadowCommand.Execute(null);
        Assert.True(vm.HasShadow);

        undoRedo.Undo();
        Assert.False(vm.HasShadow);

        // 5. Apply Preset
        inspector.ApplyTypographyPresetCommand.Execute("circlebadge");
        Assert.Equal(TextShapeMode.Circular, vm.ShapeMode);

        undoRedo.Undo();
        Assert.Equal(TextShapeMode.Normal, vm.ShapeMode);
    }

    [Fact]
    public void TextLayoutEngine_CalculateNormalLayout_AlignmentsAndPadding()
    {
        string text = "Centered Heading Text";
        var resultLeft = TextLayoutEngine.CalculateNormalLayout(
            text, "Arial", 16, false, false, 300, 1.2, 0, 0, 0, TextAlignmentMode.Left, TextVerticalAlignment.Top, 100, false, 10);
        var resultCenter = TextLayoutEngine.CalculateNormalLayout(
            text, "Arial", 16, false, false, 300, 1.2, 0, 0, 0, TextAlignmentMode.Center, TextVerticalAlignment.Top, 100, false, 10);
        var resultRight = TextLayoutEngine.CalculateNormalLayout(
            text, "Arial", 16, false, false, 300, 1.2, 0, 0, 0, TextAlignmentMode.Right, TextVerticalAlignment.Top, 100, false, 10);

        Assert.Single(resultLeft.Lines);
        Assert.Single(resultCenter.Lines);
        Assert.Single(resultRight.Lines);

        Assert.Equal(10.0, resultLeft.Lines[0].X); // starts at padding
        Assert.True(resultCenter.Lines[0].X > resultLeft.Lines[0].X, "Center alignment X should be greater than Left alignment X");
        Assert.True(resultRight.Lines[0].X > resultCenter.Lines[0].X, "Right alignment X should be greater than Center alignment X");
    }

    [Fact]
    public void TextLayoutEngine_CalculateBezierGlyphs_PositionsGlyphsAlongCubicBezier()
    {
        string text = "BEZIER WAVE CURVE";
        var pts = TextLayoutEngine.GetPresetBezierControlPoints(BezierCurvePreset.Wave);

        var result = TextLayoutEngine.CalculateBezierGlyphs(
            text, "Arial", 16, false, false, 400, 150,
            pts.P0, pts.P1, pts.P2, pts.P3,
            invert: false, charSpacing: 0, baselineShift: 0);

        Assert.Equal(text.Length, result.Glyphs.Count);
        Assert.True(result.BoundingBox.Width > 100);
        Assert.True(result.BoundingBox.Height > 20);

        // Check that glyphs progress along X from left to right
        for (int i = 1; i < result.Glyphs.Count; i++)
        {
            Assert.True(result.Glyphs[i].X >= result.Glyphs[i - 1].X, "Glyphs should advance along X");
        }
    }

    [Fact]
    public void TextLayoutEngine_BezierPresets_ProvideDistinctControlPoints()
    {
        var wave = TextLayoutEngine.GetPresetBezierControlPoints(BezierCurvePreset.Wave);
        var scurve = TextLayoutEngine.GetPresetBezierControlPoints(BezierCurvePreset.SCurve);
        var bridge = TextLayoutEngine.GetPresetBezierControlPoints(BezierCurvePreset.Bridge);
        var valley = TextLayoutEngine.GetPresetBezierControlPoints(BezierCurvePreset.Valley);
        var rise = TextLayoutEngine.GetPresetBezierControlPoints(BezierCurvePreset.Rise);

        Assert.NotEqual(wave.P1.Y, bridge.P1.Y);
        Assert.NotEqual(bridge.P1.Y, valley.P1.Y);
        Assert.NotEqual(scurve.P0.Y, rise.P0.Y);
    }

    [Fact]
    public void TextLayoutEngine_GenerateSvgMarkup_SupportsBezierCurve()
    {
        var textEl = new PdfTextElement
        {
            Text = "SMOOTH BEZIER TEXT",
            FontSize = 18,
            ShapeMode = TextShapeMode.BezierCurve,
            BezierPreset = BezierCurvePreset.SCurve,
            Width = 350,
            Height = 120,
            TextColorHex = "#4F46E5",
            IsBold = true
        };

        string svg = TextLayoutEngine.GenerateSvgMarkup(textEl);

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg);
        Assert.EndsWith("</svg>", svg);
        Assert.Contains("fill=\"#4F46E5\"", svg);
        Assert.Contains("font-size=\"18.0\"", svg);
        Assert.Contains("transform=\"rotate(", svg);
    }

    [Fact]
    public void TextElementViewModel_BezierPresets_ApplyCorrectly()
    {
        var vm = new TextElementViewModel();

        vm.ApplyTypographyPreset("wave");
        Assert.Equal(TextShapeMode.BezierCurve, vm.ShapeMode);
        Assert.Equal(BezierCurvePreset.Wave, vm.BezierPreset);

        vm.ApplyTypographyPreset("scurve");
        Assert.Equal(TextShapeMode.BezierCurve, vm.ShapeMode);
        Assert.Equal(BezierCurvePreset.SCurve, vm.BezierPreset);

        vm.ApplyTypographyPreset("bridge");
        Assert.Equal(TextShapeMode.BezierCurve, vm.ShapeMode);
        Assert.Equal(BezierCurvePreset.Bridge, vm.BezierPreset);
    }

    [Fact]
    public void InspectorViewModel_SetBezierPresetCommand_SupportsUndoRedo()
    {
        var undoRedo = new UndoRedoService();
        var inspector = new InspectorViewModel { UndoRedo = undoRedo };
        var vm = new TextElementViewModel(new PdfTextElement { ShapeMode = TextShapeMode.BezierCurve });
        inspector.SelectedElement = vm;

        inspector.SetBezierPresetCommand.Execute("SCurve");
        Assert.Equal(BezierCurvePreset.SCurve, vm.BezierPreset);
        Assert.True(undoRedo.CanUndo);

        undoRedo.Undo();
        Assert.Equal(BezierCurvePreset.Wave, vm.BezierPreset);

        undoRedo.Redo();
        Assert.Equal(BezierCurvePreset.SCurve, vm.BezierPreset);
    }

    [Fact]
    public async Task PdfExportService_ExportsBezierTypographyToPdf_Successfully()
    {
        var exportService = new PdfExportService();
        var doc = new PdfDocumentModel { Title = "Bezier Export Test" };
        var page = new PdfPageModel { PageNumber = 1, Width = 595, Height = 842 };

        page.Elements.Add(new PdfTextElement
        {
            X = 50,
            Y = 100,
            Width = 400,
            Height = 150,
            Text = "ELEGANT BEZIER WAVE TYPOGRAPHY",
            FontSize = 18,
            ShapeMode = TextShapeMode.BezierCurve,
            BezierPreset = BezierCurvePreset.Wave,
            TextColorHex = "#2563EB",
            IsBold = true
        });

        doc.Pages.Add(page);

        string tempPdfPath = Path.Combine(Path.GetTempPath(), $"bezier_test_{Guid.NewGuid():N}.pdf");
        try
        {
            await exportService.ExportToFileAsync(doc, tempPdfPath);
            Assert.True(File.Exists(tempPdfPath));
            var fileInfo = new FileInfo(tempPdfPath);
            Assert.True(fileInfo.Length > 500);
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


