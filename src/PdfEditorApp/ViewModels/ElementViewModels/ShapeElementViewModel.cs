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

    public override ElementKind Kind => ElementKind.Shape;
    public override string DisplayName => string.IsNullOrEmpty(Label) ? $"Shape ({ShapeType})" : Label;

    public string PathData => SvgShapeHelper.GetVectorPath(ShapeType, Width, Height, CornerRadius, CustomPathData);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(Width) or nameof(Height) or nameof(CornerRadius) or nameof(ShapeType) or nameof(CustomPathData))
        {
            OnPropertyChanged(nameof(PathData));
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
            SecondaryStrokeColorHex = SecondaryStrokeColorHex
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
        }
    }
}
