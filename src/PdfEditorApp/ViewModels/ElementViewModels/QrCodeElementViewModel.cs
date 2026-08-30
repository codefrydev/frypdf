using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;

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

    [ObservableProperty]
    private QrCodeEccLevel _eccLevel = QrCodeEccLevel.M;

    [ObservableProperty]
    private bool _drawQuietZones = true;

    [ObservableProperty]
    private byte[]? _qrPngBytes;

    [ObservableProperty]
    private Bitmap? _qrBitmap;

    partial void OnContentChanged(string value) => RefreshQrBitmap();
    partial void OnDarkColorHexChanged(string value) => RefreshQrBitmap();
    partial void OnLightColorHexChanged(string value) => RefreshQrBitmap();
    partial void OnEccLevelChanged(QrCodeEccLevel value) => RefreshQrBitmap();
    partial void OnDrawQuietZonesChanged(bool value) => RefreshQrBitmap();

    public void RefreshQrBitmap()
    {
        QrPngBytes = QrCodeHelper.GeneratePngBytes(Content, DarkColorHex, LightColorHex, EccLevel, pixelsPerModule: 8, drawQuietZones: DrawQuietZones);
        try
        {
            if (QrPngBytes != null && QrPngBytes.Length > 0)
            {
                using var ms = new MemoryStream(QrPngBytes);
                QrBitmap = new Bitmap(ms);
            }
            else
            {
                QrBitmap = null;
            }
        }
        catch
        {
            // In headless/mock test runner without Skia rendering context
            QrBitmap = null;
        }
    }

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
                case QrCodePresetKind.Sms:
                    Content = "SMSTO:+18005550199:Verified Document Request";
                    Label = "SCAN TO SEND SMS";
                    break;
                case QrCodePresetKind.GeoLocation:
                    Content = "geo:37.7749,-122.4194";
                    Label = "SCAN FOR GPS LOCATION";
                    break;
                case QrCodePresetKind.CryptoAddress:
                    Content = "bitcoin:bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh?amount=0.005";
                    Label = "SCAN TO PAY CRYPTO";
                    break;
                case QrCodePresetKind.EventCalendar:
                    Content = "BEGIN:VEVENT\nSUMMARY:Global Tech Summit 2026\nLOCATION:Moscone Center, SF\nDESCRIPTION:Official PDF Session\nEND:VEVENT";
                    Label = "SCAN TO ADD CALENDAR EVENT";
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
        RefreshQrBitmap();
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
            Label = Label,
            EccLevel = EccLevel,
            DrawQuietZones = DrawQuietZones
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
            EccLevel = qr.EccLevel;
            DrawQuietZones = qr.DrawQuietZones;

            RefreshQrBitmap();
        }
    }
}
