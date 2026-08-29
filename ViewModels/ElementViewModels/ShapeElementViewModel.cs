using System;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

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
    private double _cornerRadius = 4;

    [ObservableProperty]
    private string? _label;

    [ObservableProperty]
    private string? _labelColorHex = "#201F1E";

    [ObservableProperty]
    private double _labelFontSize = 12;

    public override ElementKind Kind => ElementKind.Shape;
    public override string DisplayName => string.IsNullOrEmpty(Label) ? $"Shape ({ShapeType})" : Label;

    public string PathData => GetVectorPath();

    private string GetVectorPath()
    {
        double w = Math.Max(20, Width);
        double h = Math.Max(20, Height);

        return ShapeType switch
        {
            ShapeType.Circle => $"M {w / 2:F1},0 A {w / 2:F1},{h / 2:F1} 0 1 1 {w / 2:F1},{h:F1} A {w / 2:F1},{h / 2:F1} 0 1 1 {w / 2:F1},0 Z",
            ShapeType.Triangle => $"M {w / 2:F1},0 L {w:F1},{h:F1} L 0,{h:F1} Z",
            ShapeType.RightTriangle => $"M 0,0 L {w:F1},{h:F1} L 0,{h:F1} Z",
            ShapeType.Diamond => $"M {w / 2:F1},0 L {w:F1},{h / 2:F1} L {w / 2:F1},{h:F1} L 0,{h / 2:F1} Z",
            ShapeType.Pentagon => $"M {w / 2:F1},0 L {w:F1},{h * 0.38:F1} L {w * 0.81:F1},{h:F1} L {w * 0.19:F1},{h:F1} L 0,{h * 0.38:F1} Z",
            ShapeType.Hexagon => $"M {w * 0.25:F1},0 L {w * 0.75:F1},0 L {w:F1},{h / 2:F1} L {w * 0.75:F1},{h:F1} L {w * 0.25:F1},{h:F1} L 0,{h / 2:F1} Z",
            ShapeType.Octagon => $"M {w * 0.3:F1},0 L {w * 0.7:F1},0 L {w:F1},{h * 0.3:F1} L {w:F1},{h * 0.7:F1} L {w * 0.7:F1},{h:F1} L {w * 0.3:F1},{h:F1} L 0,{h * 0.7:F1} L 0,{h * 0.3:F1} Z",
            ShapeType.Star5 => $"M {w * 0.5:F1},0 L {w * 0.62:F1},{h * 0.35:F1} L {w:F1},{h * 0.35:F1} L {w * 0.69:F1},{h * 0.57:F1} L {w * 0.81:F1},{h:F1} L {w * 0.5:F1},{h * 0.75:F1} L {w * 0.19:F1},{h:F1} L {w * 0.31:F1},{h * 0.57:F1} L 0,{h * 0.35:F1} L {w * 0.38:F1},{h * 0.35:F1} Z",
            ShapeType.Star4Badge => $"M {w * 0.5:F1},0 L {w * 0.65:F1},{h * 0.35:F1} L {w:F1},{h * 0.5:F1} L {w * 0.65:F1},{h * 0.65:F1} L {w * 0.5:F1},{h:F1} L {w * 0.35:F1},{h * 0.65:F1} L 0,{h * 0.5:F1} L {w * 0.35:F1},{h * 0.35:F1} Z",
            ShapeType.ArrowRight => $"M 0,{h * 0.3:F1} L {w * 0.6:F1},{h * 0.3:F1} L {w * 0.6:F1},0 L {w:F1},{h * 0.5:F1} L {w * 0.6:F1},{h:F1} L {w * 0.6:F1},{h * 0.7:F1} L 0,{h * 0.7:F1} Z",
            ShapeType.ArrowLeft => $"M {w:F1},{h * 0.3:F1} L {w * 0.4:F1},{h * 0.3:F1} L {w * 0.4:F1},0 L 0,{h * 0.5:F1} L {w * 0.4:F1},{h:F1} L {w * 0.4:F1},{h * 0.7:F1} L {w:F1},{h * 0.7:F1} Z",
            ShapeType.Callout => $"M 0,0 L {w:F1},0 L {w:F1},{h * 0.75:F1} L {w * 0.55:F1},{h * 0.75:F1} L {w * 0.3:F1},{h:F1} L {w * 0.35:F1},{h * 0.75:F1} L 0,{h * 0.75:F1} Z",
            ShapeType.Heart => $"M {w * 0.5:F1},{h * 0.25:F1} C {w * 0.3:F1},0 0,{h * 0.2:F1} 0,{h * 0.45:F1} C 0,{h * 0.7:F1} {w * 0.3:F1},{h * 0.85:F1} {w * 0.5:F1},{h:F1} C {w * 0.7:F1},{h * 0.85:F1} {w:F1},{h * 0.7:F1} {w:F1},{h * 0.45:F1} C {w:F1},{h * 0.2:F1} {w * 0.7:F1},0 {w * 0.5:F1},{h * 0.25:F1} Z",
            ShapeType.Cloud => $"M {w * 0.2:F1},{h * 0.7:F1} A {w * 0.15:F1},{h * 0.2:F1} 0 0 1 {w * 0.35:F1},{h * 0.3:F1} A {w * 0.25:F1},{h * 0.3:F1} 0 0 1 {w * 0.75:F1},{h * 0.35:F1} A {w * 0.18:F1},{h * 0.22:F1} 0 0 1 {w * 0.9:F1},{h * 0.7:F1} Z",
            _ => $"M 0,0 L {w:F1},0 L {w:F1},{h:F1} L 0,{h:F1} Z"
        };
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
            LabelFontSize = LabelFontSize
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
        }
    }
}
