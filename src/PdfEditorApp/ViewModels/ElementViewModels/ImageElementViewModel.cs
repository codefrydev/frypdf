using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class ImageElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string? _imagePath;

    [ObservableProperty]
    private byte[]? _imageData;

    [ObservableProperty]
    private string? _base64Data;

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
    public override string DisplayName => !string.IsNullOrEmpty(ImagePath)
        ? Path.GetFileName(ImagePath)
        : (!string.IsNullOrEmpty(AltText) ? AltText : "Image");

    partial void OnImagePathChanged(string? value)
    {
        UpdateBitmap();
    }

    partial void OnImageDataChanged(byte[]? value)
    {
        UpdateBitmap();
    }

    partial void OnBase64DataChanged(string? value)
    {
        UpdateBitmap();
    }

    public void UpdateBitmap()
    {
        if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
        {
            try
            {
                PreviewBitmap = new Bitmap(ImagePath);
                return;
            }
            catch
            {
                PreviewBitmap = null;
            }
        }

        if (ImageData != null && ImageData.Length > 0)
        {
            try
            {
                using var ms = new MemoryStream(ImageData);
                PreviewBitmap = new Bitmap(ms);
                return;
            }
            catch
            {
                PreviewBitmap = null;
            }
        }

        if (!string.IsNullOrEmpty(Base64Data))
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(Base64Data);
                using var ms = new MemoryStream(bytes);
                PreviewBitmap = new Bitmap(ms);
                return;
            }
            catch
            {
                PreviewBitmap = null;
            }
        }

        PreviewBitmap = null;
    }

    public override PdfElementBase ToModel()
    {
        var model = new PdfImageElement
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

        if (ImageData != null)
        {
            model.ImageData = ImageData;
        }
        if (!string.IsNullOrEmpty(Base64Data))
        {
            model.Base64Data = Base64Data;
        }

        return model;
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
            ImageData = img.ImageData;
            Base64Data = img.Base64Data;
            KeepAspectRatio = img.KeepAspectRatio;
            CornerRadius = img.CornerRadius;
            BorderColorHex = img.BorderColorHex;
            BorderThickness = img.BorderThickness;
            AltText = img.AltText;

            UpdateBitmap();
        }
    }
}
