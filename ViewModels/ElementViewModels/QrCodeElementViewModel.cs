using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class QrCodeElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _content = "https://github.com/PrashantUnity/PDFCreator";

    [ObservableProperty]
    private string _darkColorHex = "#0F172A";

    [ObservableProperty]
    private string _lightColorHex = "#FFFFFF";

    [ObservableProperty]
    private string _label = "SCAN TO VERIFY CREDENTIAL";

    public override ElementKind Kind => ElementKind.QrCode;
    public override string DisplayName => $"QR Code ({Content})";

    public QrCodeElementViewModel()
    {
        Width = 140;
        Height = 160;
    }

    public override PdfElementBase ToModel()
    {
        return new PdfQrCodeElement
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
            Content = Content,
            DarkColorHex = DarkColorHex,
            LightColorHex = LightColorHex,
            Label = Label
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfQrCodeElement qr)
        {
            Id = qr.Id;
            X = qr.X;
            Y = qr.Y;
            Width = qr.Width;
            Height = qr.Height;
            ZIndex = qr.ZIndex;
            Rotation = qr.Rotation;
            Opacity = qr.Opacity;
            IsLocked = qr.IsLocked;

            Content = qr.Content;
            DarkColorHex = qr.DarkColorHex;
            LightColorHex = qr.LightColorHex;
            Label = qr.Label;
        }
    }
}
