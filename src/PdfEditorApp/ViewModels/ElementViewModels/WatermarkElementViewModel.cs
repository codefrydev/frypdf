using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class WatermarkElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _text = "CONFIDENTIAL";

    [ObservableProperty]
    private double _fontSize = 48;

    [ObservableProperty]
    private string _colorHex = "#CC0000";

    [ObservableProperty]
    private double _angle = -35;

    public override ElementKind Kind => ElementKind.Watermark;
    public override string DisplayName => $"Watermark ({Text})";

    public WatermarkElementViewModel()
    {
        Opacity = 0.15;
        Width = 600;
        Height = 300;
    }

    public override PdfElementBase ToModel()
    {
        return new PdfWatermarkElement
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
            Text = Text,
            FontSize = FontSize,
            ColorHex = ColorHex,
            Angle = Angle
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfWatermarkElement wm)
        {
            Id = wm.Id;
            X = wm.X;
            Y = wm.Y;
            Width = wm.Width;
            Height = wm.Height;
            ZIndex = wm.ZIndex;
            Rotation = wm.Rotation;
            Opacity = wm.Opacity;
            IsLocked = wm.IsLocked;

            Text = wm.Text;
            FontSize = wm.FontSize;
            ColorHex = wm.ColorHex;
            Angle = wm.Angle;
        }
    }
}
