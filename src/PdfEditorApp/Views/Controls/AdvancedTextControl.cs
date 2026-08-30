using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Typography;

namespace PdfEditorApp.Views.Controls;

public class AdvancedTextControl : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<AdvancedTextControl, string>(nameof(Text), defaultValue: "Enter text here");

    public static readonly StyledProperty<string> FontFamilyNameProperty =
        AvaloniaProperty.Register<AdvancedTextControl, string>(nameof(FontFamilyName), defaultValue: "Segoe UI");

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(FontSize), defaultValue: 14.0);

    public static readonly StyledProperty<bool> IsBoldProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(IsBold), defaultValue: false);

    public static readonly StyledProperty<bool> IsItalicProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(IsItalic), defaultValue: false);

    public static readonly StyledProperty<bool> IsUnderlineProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(IsUnderline), defaultValue: false);

    public static readonly StyledProperty<bool> IsDoubleUnderlineProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(IsDoubleUnderline), defaultValue: false);

    public static readonly StyledProperty<bool> IsStrikethroughProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(IsStrikethrough), defaultValue: false);

    public static readonly StyledProperty<IBrush?> TextBrushProperty =
        AvaloniaProperty.Register<AdvancedTextControl, IBrush?>(nameof(TextBrush), defaultValue: Brushes.Black);

    public static readonly StyledProperty<double> TextOpacityProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(TextOpacity), defaultValue: 1.0);

    public static readonly StyledProperty<TextAlignmentMode> AlignmentProperty =
        AvaloniaProperty.Register<AdvancedTextControl, TextAlignmentMode>(nameof(Alignment), defaultValue: TextAlignmentMode.Left);

    public static readonly StyledProperty<TextVerticalAlignment> TextVerticalAlignmentProperty =
        AvaloniaProperty.Register<AdvancedTextControl, TextVerticalAlignment>(nameof(TextVerticalAlignment), defaultValue: TextVerticalAlignment.Top);

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(LineHeight), defaultValue: 1.4);

    public static readonly StyledProperty<double> CharacterSpacingProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(CharacterSpacing), defaultValue: 0.0);

    public static readonly StyledProperty<double> WordSpacingProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(WordSpacing), defaultValue: 0.0);

    public static readonly StyledProperty<double> ParagraphSpacingProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(ParagraphSpacing), defaultValue: 0.0);

    public static readonly StyledProperty<bool> TextWrapProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(TextWrap), defaultValue: true);

    // Stroke / Outline
    public static readonly StyledProperty<bool> HasStrokeProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(HasStroke), defaultValue: false);

    public static readonly StyledProperty<IBrush?> StrokeBrushProperty =
        AvaloniaProperty.Register<AdvancedTextControl, IBrush?>(nameof(StrokeBrush), defaultValue: Brushes.Black);

    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(StrokeWidth), defaultValue: 1.0);

    // Shadow & Glow
    public static readonly StyledProperty<bool> HasShadowProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(HasShadow), defaultValue: false);

    public static readonly StyledProperty<IBrush?> ShadowBrushProperty =
        AvaloniaProperty.Register<AdvancedTextControl, IBrush?>(nameof(ShadowBrush), defaultValue: Brushes.DarkGray);

    public static readonly StyledProperty<double> ShadowOffsetXProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(ShadowOffsetX), defaultValue: 2.0);

    public static readonly StyledProperty<double> ShadowOffsetYProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(ShadowOffsetY), defaultValue: 2.0);

    public static readonly StyledProperty<double> ShadowBlurRadiusProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(ShadowBlurRadius), defaultValue: 4.0);

    public static readonly StyledProperty<double> ShadowOpacityProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(ShadowOpacity), defaultValue: 0.5);

    // Box Background, Border, Padding
    public static readonly StyledProperty<IBrush?> BackgroundBrushProperty =
        AvaloniaProperty.Register<AdvancedTextControl, IBrush?>(nameof(BackgroundBrush), defaultValue: Brushes.Transparent);

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<AdvancedTextControl, IBrush?>(nameof(BorderBrush), defaultValue: Brushes.Transparent);

    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BorderThickness), defaultValue: 0.0);

    public static readonly StyledProperty<double> CornerRadiusProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(CornerRadius), defaultValue: 0.0);

    public static readonly StyledProperty<double> PaddingProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(Padding), defaultValue: 0.0);

    // Shape / Curved / Circular Typography
    public static readonly StyledProperty<TextShapeMode> ShapeModeProperty =
        AvaloniaProperty.Register<AdvancedTextControl, TextShapeMode>(nameof(ShapeMode), defaultValue: TextShapeMode.Normal);

    public static readonly StyledProperty<double> CurveRadiusProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(CurveRadius), defaultValue: 120.0);

    public static readonly StyledProperty<double> CurveArcAngleProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(CurveArcAngle), defaultValue: 180.0);

    public static readonly StyledProperty<double> CurveStartAngleProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(CurveStartAngle), defaultValue: 0.0);

    public static readonly StyledProperty<bool> CurveClockwiseProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(CurveClockwise), defaultValue: true);

    public static readonly StyledProperty<bool> CurveInvertProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(CurveInvert), defaultValue: false);

    public static readonly StyledProperty<CircularTextPlacement> CircularPlacementProperty =
        AvaloniaProperty.Register<AdvancedTextControl, CircularTextPlacement>(nameof(CircularPlacement), defaultValue: CircularTextPlacement.TopArc);

    // Transformations
    public static readonly StyledProperty<double> BaselineShiftProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BaselineShift), defaultValue: 0.0);

    public static readonly StyledProperty<double> CharacterRotationProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(CharacterRotation), defaultValue: 0.0);

    public static readonly StyledProperty<double> ScaleXProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(ScaleX), defaultValue: 1.0);

    public static readonly StyledProperty<double> ScaleYProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(ScaleY), defaultValue: 1.0);

    public static readonly StyledProperty<bool> FlipXProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(FlipX), defaultValue: false);

    public static readonly StyledProperty<bool> FlipYProperty =
        AvaloniaProperty.Register<AdvancedTextControl, bool>(nameof(FlipY), defaultValue: false);

    // Bézier Curve Typography
    public static readonly StyledProperty<BezierCurvePreset> BezierPresetProperty =
        AvaloniaProperty.Register<AdvancedTextControl, BezierCurvePreset>(nameof(BezierPreset), defaultValue: BezierCurvePreset.Wave);

    public static readonly StyledProperty<double> BezierP0XProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BezierP0X), defaultValue: 0.0);

    public static readonly StyledProperty<double> BezierP0YProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BezierP0Y), defaultValue: 0.5);

    public static readonly StyledProperty<double> BezierP1XProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BezierP1X), defaultValue: 0.33);

    public static readonly StyledProperty<double> BezierP1YProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BezierP1Y), defaultValue: 0.10);

    public static readonly StyledProperty<double> BezierP2XProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BezierP2X), defaultValue: 0.67);

    public static readonly StyledProperty<double> BezierP2YProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BezierP2Y), defaultValue: 0.90);

    public static readonly StyledProperty<double> BezierP3XProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BezierP3X), defaultValue: 1.0);

    public static readonly StyledProperty<double> BezierP3YProperty =
        AvaloniaProperty.Register<AdvancedTextControl, double>(nameof(BezierP3Y), defaultValue: 0.5);

    static AdvancedTextControl()
    {
        AffectsRender<AdvancedTextControl>(
            TextProperty, FontFamilyNameProperty, FontSizeProperty, IsBoldProperty, IsItalicProperty,
            IsUnderlineProperty, IsDoubleUnderlineProperty, IsStrikethroughProperty, TextBrushProperty,
            TextOpacityProperty, AlignmentProperty, TextVerticalAlignmentProperty, LineHeightProperty,
            CharacterSpacingProperty, WordSpacingProperty, ParagraphSpacingProperty, TextWrapProperty,
            HasStrokeProperty, StrokeBrushProperty, StrokeWidthProperty, HasShadowProperty,
            ShadowBrushProperty, ShadowOffsetXProperty, ShadowOffsetYProperty, ShadowBlurRadiusProperty,
            ShadowOpacityProperty, BackgroundBrushProperty, BorderBrushProperty, BorderThicknessProperty,
            CornerRadiusProperty, PaddingProperty, ShapeModeProperty, CurveRadiusProperty,
            CurveArcAngleProperty, CurveStartAngleProperty, CurveClockwiseProperty, CurveInvertProperty,
            CircularPlacementProperty, BaselineShiftProperty, CharacterRotationProperty, ScaleXProperty,
            ScaleYProperty, FlipXProperty, FlipYProperty,
            BezierPresetProperty, BezierP0XProperty, BezierP0YProperty, BezierP1XProperty, BezierP1YProperty,
            BezierP2XProperty, BezierP2YProperty, BezierP3XProperty, BezierP3YProperty
        );
    }

    #region Properties Accessors
    public string Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string FontFamilyName { get => GetValue(FontFamilyNameProperty); set => SetValue(FontFamilyNameProperty, value); }
    public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public bool IsBold { get => GetValue(IsBoldProperty); set => SetValue(IsBoldProperty, value); }
    public bool IsItalic { get => GetValue(IsItalicProperty); set => SetValue(IsItalicProperty, value); }
    public bool IsUnderline { get => GetValue(IsUnderlineProperty); set => SetValue(IsUnderlineProperty, value); }
    public bool IsDoubleUnderline { get => GetValue(IsDoubleUnderlineProperty); set => SetValue(IsDoubleUnderlineProperty, value); }
    public bool IsStrikethrough { get => GetValue(IsStrikethroughProperty); set => SetValue(IsStrikethroughProperty, value); }
    public IBrush? TextBrush { get => GetValue(TextBrushProperty); set => SetValue(TextBrushProperty, value); }
    public double TextOpacity { get => GetValue(TextOpacityProperty); set => SetValue(TextOpacityProperty, value); }
    public TextAlignmentMode Alignment { get => GetValue(AlignmentProperty); set => SetValue(AlignmentProperty, value); }
    public TextVerticalAlignment TextVerticalAlignment { get => GetValue(TextVerticalAlignmentProperty); set => SetValue(TextVerticalAlignmentProperty, value); }
    public double LineHeight { get => GetValue(LineHeightProperty); set => SetValue(LineHeightProperty, value); }
    public double CharacterSpacing { get => GetValue(CharacterSpacingProperty); set => SetValue(CharacterSpacingProperty, value); }
    public double WordSpacing { get => GetValue(WordSpacingProperty); set => SetValue(WordSpacingProperty, value); }
    public double ParagraphSpacing { get => GetValue(ParagraphSpacingProperty); set => SetValue(ParagraphSpacingProperty, value); }
    public bool TextWrap { get => GetValue(TextWrapProperty); set => SetValue(TextWrapProperty, value); }

    public bool HasStroke { get => GetValue(HasStrokeProperty); set => SetValue(HasStrokeProperty, value); }
    public IBrush? StrokeBrush { get => GetValue(StrokeBrushProperty); set => SetValue(StrokeBrushProperty, value); }
    public double StrokeWidth { get => GetValue(StrokeWidthProperty); set => SetValue(StrokeWidthProperty, value); }

    public bool HasShadow { get => GetValue(HasShadowProperty); set => SetValue(HasShadowProperty, value); }
    public IBrush? ShadowBrush { get => GetValue(ShadowBrushProperty); set => SetValue(ShadowBrushProperty, value); }
    public double ShadowOffsetX { get => GetValue(ShadowOffsetXProperty); set => SetValue(ShadowOffsetXProperty, value); }
    public double ShadowOffsetY { get => GetValue(ShadowOffsetYProperty); set => SetValue(ShadowOffsetYProperty, value); }
    public double ShadowBlurRadius { get => GetValue(ShadowBlurRadiusProperty); set => SetValue(ShadowBlurRadiusProperty, value); }
    public double ShadowOpacity { get => GetValue(ShadowOpacityProperty); set => SetValue(ShadowOpacityProperty, value); }

    public IBrush? BackgroundBrush { get => GetValue(BackgroundBrushProperty); set => SetValue(BackgroundBrushProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public double BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public double CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    public TextShapeMode ShapeMode { get => GetValue(ShapeModeProperty); set => SetValue(ShapeModeProperty, value); }
    public double CurveRadius { get => GetValue(CurveRadiusProperty); set => SetValue(CurveRadiusProperty, value); }
    public double CurveArcAngle { get => GetValue(CurveArcAngleProperty); set => SetValue(CurveArcAngleProperty, value); }
    public double CurveStartAngle { get => GetValue(CurveStartAngleProperty); set => SetValue(CurveStartAngleProperty, value); }
    public bool CurveClockwise { get => GetValue(CurveClockwiseProperty); set => SetValue(CurveClockwiseProperty, value); }
    public bool CurveInvert { get => GetValue(CurveInvertProperty); set => SetValue(CurveInvertProperty, value); }
    public CircularTextPlacement CircularPlacement { get => GetValue(CircularPlacementProperty); set => SetValue(CircularPlacementProperty, value); }

    public double BaselineShift { get => GetValue(BaselineShiftProperty); set => SetValue(BaselineShiftProperty, value); }
    public double CharacterRotation { get => GetValue(CharacterRotationProperty); set => SetValue(CharacterRotationProperty, value); }
    public double ScaleX { get => GetValue(ScaleXProperty); set => SetValue(ScaleXProperty, value); }
    public double ScaleY { get => GetValue(ScaleYProperty); set => SetValue(ScaleYProperty, value); }
    public bool FlipX { get => GetValue(FlipXProperty); set => SetValue(FlipXProperty, value); }
    public bool FlipY { get => GetValue(FlipYProperty); set => SetValue(FlipYProperty, value); }

    public BezierCurvePreset BezierPreset { get => GetValue(BezierPresetProperty); set => SetValue(BezierPresetProperty, value); }
    public double BezierP0X { get => GetValue(BezierP0XProperty); set => SetValue(BezierP0XProperty, value); }
    public double BezierP0Y { get => GetValue(BezierP0YProperty); set => SetValue(BezierP0YProperty, value); }
    public double BezierP1X { get => GetValue(BezierP1XProperty); set => SetValue(BezierP1XProperty, value); }
    public double BezierP1Y { get => GetValue(BezierP1YProperty); set => SetValue(BezierP1YProperty, value); }
    public double BezierP2X { get => GetValue(BezierP2XProperty); set => SetValue(BezierP2XProperty, value); }
    public double BezierP2Y { get => GetValue(BezierP2YProperty); set => SetValue(BezierP2YProperty, value); }
    public double BezierP3X { get => GetValue(BezierP3XProperty); set => SetValue(BezierP3XProperty, value); }
    public double BezierP3Y { get => GetValue(BezierP3YProperty); set => SetValue(BezierP3YProperty, value); }
    #endregion

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        // Apply Opacity
        using var opacityScope = TextOpacity < 0.99 ? context.PushOpacity(TextOpacity) : default;

        // Draw Background and Border
        if (BackgroundBrush != null || (BorderBrush != null && BorderThickness > 0))
        {
            var bgPen = (BorderBrush != null && BorderThickness > 0) ? new Pen(BorderBrush, BorderThickness) : null;
            var rect = new RoundedRect(new Rect(0, 0, w, h), CornerRadius);
            context.DrawRectangle(BackgroundBrush, bgPen, rect);
        }

        // Apply Scaling & Flipping Matrix around center
        bool hasCustomTransform = FlipX || FlipY || Math.Abs(ScaleX - 1.0) > 0.001 || Math.Abs(ScaleY - 1.0) > 0.001;
        IDisposable? transformScope = null;
        if (hasCustomTransform)
        {
            double sx = (FlipX ? -1.0 : 1.0) * (ScaleX != 0 ? ScaleX : 1.0);
            double sy = (FlipY ? -1.0 : 1.0) * (ScaleY != 0 ? ScaleY : 1.0);
            var center = new Point(w / 2.0, h / 2.0);
            var transform = Matrix.CreateTranslation(-center.X, -center.Y) *
                            Matrix.CreateScale(sx, sy) *
                            Matrix.CreateTranslation(center.X, center.Y);
            transformScope = context.PushTransform(transform);
        }

        try
        {
            var avaloniaFamily = FontHelper.CreateFontFamily(FontFamilyName);
            var typeface = new Typeface(
                avaloniaFamily,
                IsItalic ? FontStyle.Italic : FontStyle.Normal,
                IsBold ? FontWeight.Bold : FontWeight.Normal);

            var fillBrush = TextBrush ?? Brushes.Black;
            Pen? strokePen = (HasStroke && StrokeWidth > 0 && StrokeBrush != null) ? new Pen(StrokeBrush, StrokeWidth) : null;
            IBrush? shadowBrush = (HasShadow && ShadowBrush != null) ? ShadowBrush : null;

            if (ShapeMode == TextShapeMode.Normal)
            {
                RenderNormalText(context, typeface, fillBrush, strokePen, shadowBrush, w, h);
            }
            else if (ShapeMode == TextShapeMode.BezierCurve)
            {
                RenderBezierText(context, typeface, fillBrush, strokePen, shadowBrush, w, h);
            }
            else
            {
                RenderCurvedText(context, typeface, fillBrush, strokePen, shadowBrush, w, h);
            }
        }
        finally
        {
            transformScope?.Dispose();
        }
    }

    private void RenderNormalText(
        DrawingContext context,
        Typeface typeface,
        IBrush fillBrush,
        Pen? strokePen,
        IBrush? shadowBrush,
        double w,
        double h)
    {
        var layout = TextLayoutEngine.CalculateNormalLayout(
            Text,
            FontFamilyName,
            FontSize,
            IsBold,
            IsItalic,
            w,
            LineHeight,
            CharacterSpacing,
            WordSpacing,
            ParagraphSpacing,
            Alignment,
            TextVerticalAlignment,
            h,
            TextWrap,
            Padding
        );

        foreach (var line in layout.Lines)
        {
            if (string.IsNullOrEmpty(line.Text)) continue;

            // Draw Shadow
            if (HasShadow && shadowBrush != null)
            {
                using (context.PushOpacity(ShadowOpacity))
                {
                    var shadowFt = new FormattedText(
                        line.Text,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        FontSize,
                        shadowBrush);
                    context.DrawText(shadowFt, new Point(line.X + ShadowOffsetX, line.Y + ShadowOffsetY));
                }
            }

            var ft = new FormattedText(
                line.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize,
                fillBrush);

            // Draw Stroke Outline
            if (strokePen != null)
            {
                var geom = ft.BuildGeometry(new Point(line.X, line.Y));
                if (geom != null)
                {
                    context.DrawGeometry(null, strokePen, geom);
                }
            }

            // Draw Fill Text
            context.DrawText(ft, new Point(line.X, line.Y));

            // Double Underline
            if (IsDoubleUnderline)
            {
                double uY1 = line.BaselineY + 2;
                double uY2 = line.BaselineY + 5;
                var uPen = new Pen(fillBrush, 1.0);
                context.DrawLine(uPen, new Point(line.X, uY1), new Point(line.X + line.Width, uY1));
                context.DrawLine(uPen, new Point(line.X, uY2), new Point(line.X + line.Width, uY2));
            }
        }
    }

    private void RenderCurvedText(
        DrawingContext context,
        Typeface typeface,
        IBrush fillBrush,
        Pen? strokePen,
        IBrush? shadowBrush,
        double w,
        double h)
    {
        var curvedLayout = TextLayoutEngine.CalculateCurvedGlyphs(
            Text,
            FontFamilyName,
            FontSize,
            IsBold,
            IsItalic,
            w,
            h,
            CurveRadius,
            CurveArcAngle,
            CurveStartAngle,
            CurveClockwise,
            CurveInvert,
            CharacterSpacing,
            CircularPlacement,
            ShapeMode,
            BaselineShift
        );

        var decorations = new TextDecorationCollection();
        if (IsUnderline) foreach (var d in TextDecorations.Underline) decorations.Add(d);
        if (IsStrikethrough) foreach (var d in TextDecorations.Strikethrough) decorations.Add(d);

        foreach (var g in curvedLayout.Glyphs)
        {
            if (char.IsWhiteSpace(g.Character)) continue;

            double angleDeg = g.TangentAngleDeg + CharacterRotation;
            double angleRad = (angleDeg * Math.PI) / 180.0;

            // Rotate around glyph center
            var glyphCenter = new Point(g.X, g.Y);
            var transform = Matrix.CreateTranslation(-glyphCenter.X, -glyphCenter.Y) *
                            Matrix.CreateRotation(angleRad) *
                            Matrix.CreateTranslation(glyphCenter.X, glyphCenter.Y);

            using (context.PushTransform(transform))
            {
                // Shadow
                if (HasShadow && shadowBrush != null)
                {
                    using (context.PushOpacity(ShadowOpacity))
                    {
                        var sFt = new FormattedText(
                            g.Text,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            FontSize,
                            shadowBrush);
                        context.DrawText(sFt, new Point(g.X - (g.Width / 2.0) + ShadowOffsetX, g.Y - g.BaselineOffset + ShadowOffsetY));
                    }
                }

                var ft = new FormattedText(
                    g.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    FontSize,
                    fillBrush);

                var origin = new Point(g.X - (g.Width / 2.0), g.Y - g.BaselineOffset);

                // Stroke Outline
                if (strokePen != null)
                {
                    var geom = ft.BuildGeometry(origin);
                    if (geom != null)
                    {
                        context.DrawGeometry(null, strokePen, geom);
                    }
                }

                // Fill
                context.DrawText(ft, origin);
            }
        }
    }

    private void RenderBezierText(
        DrawingContext context,
        Typeface typeface,
        IBrush fillBrush,
        Pen? strokePen,
        IBrush? shadowBrush,
        double w,
        double h)
    {
        var layout = TextLayoutEngine.CalculateBezierGlyphs(
            Text,
            FontFamilyName,
            FontSize,
            IsBold,
            IsItalic,
            w,
            h,
            new Point(BezierP0X, BezierP0Y),
            new Point(BezierP1X, BezierP1Y),
            new Point(BezierP2X, BezierP2Y),
            new Point(BezierP3X, BezierP3Y),
            CurveInvert,
            CharacterSpacing,
            BaselineShift
        );

        foreach (var g in layout.Glyphs)
        {
            if (string.IsNullOrEmpty(g.Text) || char.IsWhiteSpace(g.Character)) continue;

            double totalAngle = g.TangentAngleDeg + CharacterRotation;
            double angleRad = (totalAngle * Math.PI) / 180.0;

            var transform = Matrix.CreateTranslation(-g.X, -g.Y) *
                            Matrix.CreateRotation(angleRad) *
                            Matrix.CreateTranslation(g.X, g.Y);

            using (context.PushTransform(transform))
            {
                // Shadow
                if (HasShadow && shadowBrush != null)
                {
                    using (context.PushOpacity(ShadowOpacity))
                    {
                        var sFt = new FormattedText(
                            g.Text,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            FontSize,
                            shadowBrush);
                        context.DrawText(sFt, new Point(g.X - (g.Width / 2.0) + ShadowOffsetX, g.Y - g.BaselineOffset + ShadowOffsetY));
                    }
                }

                var ft = new FormattedText(
                    g.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    FontSize,
                    fillBrush);

                var origin = new Point(g.X - (g.Width / 2.0), g.Y - g.BaselineOffset);

                // Stroke Outline
                if (strokePen != null)
                {
                    var geom = ft.BuildGeometry(origin);
                    if (geom != null)
                    {
                        context.DrawGeometry(null, strokePen, geom);
                    }
                }

                // Fill Text
                context.DrawText(ft, origin);

                // Underline & Strikethrough along character local axis
                if (IsUnderline || IsDoubleUnderline)
                {
                    var uPen = new Pen(fillBrush, Math.Max(1.0, FontSize * 0.07));
                    double uY = g.Y + (FontSize * 0.45);
                    context.DrawLine(uPen, new Point(g.X - (g.Width / 2.0), uY), new Point(g.X + (g.Width / 2.0), uY));

                    if (IsDoubleUnderline)
                    {
                        double uY2 = uY + Math.Max(2.0, FontSize * 0.12);
                        context.DrawLine(uPen, new Point(g.X - (g.Width / 2.0), uY2), new Point(g.X + (g.Width / 2.0), uY2));
                    }
                }

                if (IsStrikethrough)
                {
                    var sPen = new Pen(fillBrush, Math.Max(1.0, FontSize * 0.07));
                    double sY = g.Y;
                    context.DrawLine(sPen, new Point(g.X - (g.Width / 2.0), sY), new Point(g.X + (g.Width / 2.0), sY));
                }
            }
        }
    }
}
