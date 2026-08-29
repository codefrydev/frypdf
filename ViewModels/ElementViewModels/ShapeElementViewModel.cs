using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class ShapeElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
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
