using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class ShapeElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private ShapeType _shapeType = ShapeType.Rectangle;

    [ObservableProperty]
    private string _fillColorHex = "#F8F9FA";

    [ObservableProperty]
    private string _strokeColorHex = "#0F6CBD";

    [ObservableProperty]
    private double _strokeThickness = 1.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _cornerRadius = 4;

    [ObservableProperty]
    private string? _label;

    [ObservableProperty]
    private string? _labelColorHex = "#201F1E";

    [ObservableProperty]
    private double _labelFontSize = 12;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private string? _customPathData;

    [ObservableProperty]
    private string? _secondaryFillColorHex;

    [ObservableProperty]
    private string? _secondaryStrokeColorHex;

    // Bézier Curves, Connectors & Line Caps
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _bezierP0X = 0.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _bezierP0Y = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _bezierP1X = 0.33;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _bezierP1Y = 0.10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _bezierP2X = 0.67;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _bezierP2Y = 0.90;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _bezierP3X = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _bezierP3Y = 0.5;

    [ObservableProperty]
    private LineEndCap _startCap = LineEndCap.None;

    [ObservableProperty]
    private LineEndCap _endCap = LineEndCap.None;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DashArrayString))]
    private LineDashStyle _dashStyle = LineDashStyle.Solid;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _waveFrequency = 2.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PathData))]
    private double _curvatureDepth = 40.0;

    public override ElementKind Kind => ElementKind.Shape;
    public override string DisplayName => string.IsNullOrEmpty(Label) ? $"Shape ({ShapeType})" : Label;

    public bool IsLineOrCurve => ShapeType is ShapeType.Line or ShapeType.Arrow or ShapeType.BezierCurve or ShapeType.CurvedArrow or ShapeType.SCurveConnector or ShapeType.WaveLine or ShapeType.ArcLine or ShapeType.CurlyBrace;

    public string PathData => SvgShapeHelper.GetVectorPath(ShapeType, Width, Height, CornerRadius, CustomPathData);
    public string? DashArrayString => SvgShapeHelper.GetDashArray(DashStyle, StrokeThickness);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(Width) or nameof(Height) or nameof(CornerRadius) or nameof(ShapeType) or nameof(CustomPathData) or nameof(StrokeThickness) or nameof(DashStyle))
        {
            OnPropertyChanged(nameof(PathData));
            OnPropertyChanged(nameof(DashArrayString));
        }
    }

    public override PdfElementBase ToModel()
    {
        return new PdfShapeElement
        {
            Id = Id,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZIndex = ZIndex,
            Rotation = Rotation,
            Opacity = Opacity,
            IsLocked = IsLocked,
            ShapeType = ShapeType,
            FillColorHex = FillColorHex,
            StrokeColorHex = StrokeColorHex,
            StrokeThickness = StrokeThickness,
            CornerRadius = CornerRadius,
            Label = Label,
            LabelColorHex = LabelColorHex,
            LabelFontSize = LabelFontSize,
            CustomPathData = CustomPathData,
            SecondaryFillColorHex = SecondaryFillColorHex,
            SecondaryStrokeColorHex = SecondaryStrokeColorHex,
            BezierP0X = BezierP0X,
            BezierP0Y = BezierP0Y,
            BezierP1X = BezierP1X,
            BezierP1Y = BezierP1Y,
            BezierP2X = BezierP2X,
            BezierP2Y = BezierP2Y,
            BezierP3X = BezierP3X,
            BezierP3Y = BezierP3Y,
            StartCap = StartCap,
            EndCap = EndCap,
            DashStyle = DashStyle,
            WaveFrequency = WaveFrequency,
            CurvatureDepth = CurvatureDepth
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfShapeElement shape)
        {
            Id = shape.Id;
            X = shape.X;
            Y = shape.Y;
            Width = shape.Width;
            Height = shape.Height;
            ZIndex = shape.ZIndex;
            Rotation = shape.Rotation;
            Opacity = shape.Opacity;
            IsLocked = shape.IsLocked;

            ShapeType = shape.ShapeType;
            FillColorHex = shape.FillColorHex;
            StrokeColorHex = shape.StrokeColorHex;
            StrokeThickness = shape.StrokeThickness;
            CornerRadius = shape.CornerRadius;
            Label = shape.Label;
            LabelColorHex = shape.LabelColorHex;
            LabelFontSize = shape.LabelFontSize;
            CustomPathData = shape.CustomPathData;
            SecondaryFillColorHex = shape.SecondaryFillColorHex;
            SecondaryStrokeColorHex = shape.SecondaryStrokeColorHex;
            BezierP0X = shape.BezierP0X;
            BezierP0Y = shape.BezierP0Y;
            BezierP1X = shape.BezierP1X;
            BezierP1Y = shape.BezierP1Y;
            BezierP2X = shape.BezierP2X;
            BezierP2Y = shape.BezierP2Y;
            BezierP3X = shape.BezierP3X;
            BezierP3Y = shape.BezierP3Y;
            StartCap = shape.StartCap;
            EndCap = shape.EndCap;
            DashStyle = shape.DashStyle;
            WaveFrequency = shape.WaveFrequency;
            CurvatureDepth = shape.CurvatureDepth;
        }
    }
}
