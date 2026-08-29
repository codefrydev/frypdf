using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class DividerElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _colorHex = "#0F6CBD";

    [ObservableProperty]
    private double _thickness = 2;

    [ObservableProperty]
    private bool _isVertical;

    public override ElementKind Kind => ElementKind.Divider;
    public override string DisplayName => "Divider Line";

    public override PdfElementBase ToModel()
    {
        return new PdfDividerElement
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
            ColorHex = ColorHex,
            Thickness = Thickness,
            IsVertical = IsVertical
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfDividerElement div)
        {
            Id = div.Id;
            X = div.X;
            Y = div.Y;
            Width = div.Width;
            Height = div.Height;
            ZIndex = div.ZIndex;
            Rotation = div.Rotation;
            Opacity = div.Opacity;
            IsLocked = div.IsLocked;

            ColorHex = div.ColorHex;
            Thickness = div.Thickness;
            IsVertical = div.IsVertical;
        }
    }
}
