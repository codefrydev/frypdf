using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [RelayCommand]
    public void ApplyPresetType(string presetStr)
    {
        if (Enum.TryParse<QrCodePresetKind>(presetStr, true, out var kind))
        {
            switch (kind)
            {
                case QrCodePresetKind.Url:
                    Content = "https://github.com/PrashantUnity/PDFCreator";
                    Label = "SCAN TO VISIT WEBSITE";
                    break;
                case QrCodePresetKind.Wifi:
                    Content = "WIFI:S:Enterprise-Secure;T:WPA2;P:Passcode2026;;";
                    Label = "SCAN TO CONNECT WI-FI";
                    break;
                case QrCodePresetKind.VCard:
                    Content = "BEGIN:VCARD\nVERSION:3.0\nN:Smith;John\nORG:Acme Corp\nTEL:+1-555-0149\nEMAIL:john@acmecorp.com\nEND:VCARD";
                    Label = "SCAN FOR DIGITAL VCARD";
                    break;
                case QrCodePresetKind.Email:
                    Content = "mailto:legal@acmecorp.com?subject=Document%20Verification";
                    Label = "SCAN TO SEND EMAIL";
                    break;
                case QrCodePresetKind.PhoneCall:
                    Content = "tel:+18005550199";
                    Label = "SCAN TO DIAL DIRECT";
                    break;
                case QrCodePresetKind.GeoLocation:
                    Content = "geo:37.7749,-122.4194";
                    Label = "SCAN FOR GPS LOCATION";
                    break;
                case QrCodePresetKind.PlainText:
                    Content = "AUTHENTICATED-DOCUMENT-HASH-892401924";
                    Label = "SCAN TO VERIFY TOKEN";
                    break;
            }
        }
    }

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
