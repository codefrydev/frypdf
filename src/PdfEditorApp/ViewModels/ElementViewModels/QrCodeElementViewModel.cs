using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
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

    private Bitmap? _previousQrBitmap;

    /// <summary>
    /// Disposes the outgoing bitmap whenever a new QR code is rasterized.
    /// </summary>
    /// <remarks>
    /// Avalonia's <see cref="Bitmap"/> wraps native Skia memory the GC does not account for,
    /// and <see cref="RefreshQrBitmap"/> runs on five different property changes — including
    /// <c>Content</c>, which is bound to a TextBox, so this fired on every keystroke.
    /// </remarks>
    partial void OnQrBitmapChanged(Bitmap? value)
    {
        if (_previousQrBitmap != null && _previousQrBitmap != value)
        {
            _previousQrBitmap.Dispose();
        }
        _previousQrBitmap = value;
    }

    /// <summary>Coalesces QR regeneration; see <see cref="RefreshQrBitmap"/>.</summary>
    private readonly UiDebouncer _qrDebouncer;

    // Content is bound to a TextBox and the colour hexes to pickers, so these fired on every
    // keystroke and every picker tick — each one re-encoding the QR, re-encoding a PNG and
    // allocating a fresh native Skia bitmap. ToModel() does not read QrPngBytes (export
    // regenerates from the model), so the only thing these feed is the live canvas preview,
    // which is safe to coalesce.
    partial void OnContentChanged(string value) => _qrDebouncer.Request();
    partial void OnDarkColorHexChanged(string value) => _qrDebouncer.Request();
    partial void OnLightColorHexChanged(string value) => _qrDebouncer.Request();
    partial void OnEccLevelChanged(QrCodeEccLevel value) => _qrDebouncer.Request();
    partial void OnDrawQuietZonesChanged(bool value) => _qrDebouncer.Request();

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
                    Content = "BEGIN:VCARD\nVERSION:3.0\nN:Doe;John\nORG:CodeFryDev\nTEL:+1-555-0149\nEMAIL:john.doe@codefrydev.in\nEND:VCARD";
                    Label = "SCAN FOR DIGITAL VCARD";
                    break;
                case QrCodePresetKind.Email:
                    Content = "mailto:legal@codefrydev.in?subject=Document%20Verification";
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
        _qrDebouncer = new UiDebouncer(QrDebounceMs, RefreshQrBitmap);

        Width = 140;
        Height = 160;

        // Direct, not debounced: an element must have a preview the moment it is created, and
        // the debounced path needs a running dispatcher which headless hosts do not have.
        RefreshQrBitmap();
    }

    /// <summary>Quiet period before the QR preview is regenerated.</summary>
    private const int QrDebounceMs = 180;

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
