using System;
using System.Text.Json.Serialization;

namespace PdfEditorApp.Core.Models.Elements;

public class PdfImageElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.Image;

    public string? ImagePath { get; set; }

    /// <summary>
    /// Raw binary image bytes (PNG, JPEG, etc.).
    /// Storing bytes directly eliminates Large Object Heap (LOH) string allocation bloat during PDF deconstruction.
    /// </summary>
    [JsonIgnore]
    public byte[]? ImageData
    {
        get => _imageData;
        set
        {
            _imageData = value;
            _cachedBase64 = null;
        }
    }
    private byte[]? _imageData;

    /// <summary>
    /// Base64 encoded representation for JSON persistence and legacy interop.
    /// Evaluated lazily on demand from <see cref="ImageData"/> to avoid memory overhead.
    /// </summary>
    public string? Base64Data
    {
        get
        {
            if (_cachedBase64 != null) return _cachedBase64;
            if (_imageData != null && _imageData.Length > 0)
            {
                _cachedBase64 = Convert.ToBase64String(_imageData);
                return _cachedBase64;
            }
            return null;
        }
        set
        {
            _cachedBase64 = value;
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    _imageData = Convert.FromBase64String(value);
                }
                catch
                {
                    // Invalid base64 string
                }
            }
        }
    }
    private string? _cachedBase64;

    public bool KeepAspectRatio { get; set; } = true;
    public double CornerRadius { get; set; } = 4;
    public string BorderColorHex { get; set; } = "#E1DFDD";
    public double BorderThickness { get; set; } = 1;
    public string AltText { get; set; } = "Image";

    public override PdfElementBase Clone()
    {
        var clone = (PdfImageElement)base.Clone();
        if (_imageData != null)
        {
            clone.ImageData = (byte[])_imageData.Clone();
        }
        return clone;
    }
}
