using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class BarcodeElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _codeValue = "DOC-2026-984210";

    [ObservableProperty]
    private string _barcodeFormat = "Code128";

    [ObservableProperty]
    private string _barColorHex = "#0F172A";

    [ObservableProperty]
    private string _backgroundColorHex = "#FFFFFF";

    [ObservableProperty]
    private bool _showText = true;

    [RelayCommand]
    public void SetFormat(string formatStr)
    {
        BarcodeFormat = formatStr;
    }

    public override ElementKind Kind => ElementKind.Barcode;
    public override string DisplayName => $"Barcode ({CodeValue})";

    public BarcodeElementViewModel()
    {
        Width = 220;
        Height = 70;
    }

    public override PdfElementBase ToModel()
    {
        return new PdfBarcodeElement
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
            CodeValue = CodeValue,
            BarcodeFormat = BarcodeFormat,
            BarColorHex = BarColorHex,
            BackgroundColorHex = BackgroundColorHex,
            ShowText = ShowText
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfBarcodeElement barcode)
        {
            Id = barcode.Id;
            X = barcode.X;
            Y = barcode.Y;
            Width = barcode.Width;
            Height = barcode.Height;
            ZIndex = barcode.ZIndex;
            Rotation = barcode.Rotation;
            Opacity = barcode.Opacity;
            IsLocked = barcode.IsLocked;

            CodeValue = barcode.CodeValue;
            BarcodeFormat = barcode.BarcodeFormat;
            BarColorHex = barcode.BarColorHex;
            BackgroundColorHex = barcode.BackgroundColorHex;
            ShowText = barcode.ShowText;
        }
    }
}
