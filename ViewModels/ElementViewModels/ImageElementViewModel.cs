using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using System.IO;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class ImageElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string? _imagePath;

    [ObservableProperty]
    private Bitmap? _previewBitmap;

    [ObservableProperty]
    private bool _keepAspectRatio = true;

    [ObservableProperty]
    private double _cornerRadius = 4;

    [ObservableProperty]
    private string _borderColorHex = "#E1DFDD";

    [ObservableProperty]
    private double _borderThickness = 1;

    [ObservableProperty]
    private string _altText = "Image Placeholder";

    public override ElementKind Kind => ElementKind.Image;
    public override string DisplayName => string.IsNullOrEmpty(ImagePath) ? "Image" : Path.GetFileName(ImagePath);

    partial void OnImagePathChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value) && File.Exists(value))
        {
            try
            {
                PreviewBitmap = new Bitmap(value);
            }
            catch
            {
                PreviewBitmap = null;
            }
        }
        else
        {
            PreviewBitmap = null;
        }
    }

    public override PdfElementBase ToModel()
    {
        return new PdfImageElement
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
            ImagePath = ImagePath,
            KeepAspectRatio = KeepAspectRatio,
            CornerRadius = CornerRadius,
            BorderColorHex = BorderColorHex,
            BorderThickness = BorderThickness,
            AltText = AltText
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfImageElement img)
        {
            Id = img.Id;
            X = img.X;
            Y = img.Y;
            Width = img.Width;
            Height = img.Height;
            ZIndex = img.ZIndex;
            Rotation = img.Rotation;
            Opacity = img.Opacity;
            IsLocked = img.IsLocked;

            ImagePath = img.ImagePath;
            KeepAspectRatio = img.KeepAspectRatio;
            CornerRadius = img.CornerRadius;
            BorderColorHex = img.BorderColorHex;
            BorderThickness = img.BorderThickness;
            AltText = img.AltText;
        }
    }
}
