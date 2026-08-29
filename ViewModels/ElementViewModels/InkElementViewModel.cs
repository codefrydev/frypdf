using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class InkElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _pointsData = "10,10 50,40 100,20 180,60 220,30";

    [ObservableProperty]
    private string _strokeColorHex = "#0F6CBD";

    [ObservableProperty]
    private double _strokeThickness = 3.0;

    [ObservableProperty]
    private bool _isHighlighter;

    public override ElementKind Kind => ElementKind.Ink;
    public override string DisplayName => IsHighlighter ? "Highlighter Path" : "Freehand Ink Drawing";

    public InkElementViewModel()
    {
        Width = 240;
        Height = 80;
    }

    public override PdfElementBase ToModel()
    {
        return new PdfInkElement
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
            PointsData = PointsData,
            StrokeColorHex = StrokeColorHex,
            StrokeThickness = StrokeThickness,
            IsHighlighter = IsHighlighter
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfInkElement ink)
        {
            Id = ink.Id;
            X = ink.X;
            Y = ink.Y;
            Width = ink.Width;
            Height = ink.Height;
            ZIndex = ink.ZIndex;
            Rotation = ink.Rotation;
            Opacity = ink.Opacity;
            IsLocked = ink.IsLocked;

            PointsData = ink.PointsData;
            StrokeColorHex = ink.StrokeColorHex;
            StrokeThickness = ink.StrokeThickness;
            IsHighlighter = ink.IsHighlighter;
        }
    }
}
