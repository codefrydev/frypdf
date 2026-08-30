using System;
using System.Collections.Generic;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Models;

public class PdfPageModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int PageNumber { get; set; } = 1;

    public PageFormat Format { get; set; } = PageFormat.A4;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
    public int RotationAngle { get; set; } = 0; // 0, 90, 180, 270

    public double Width { get; set; } = 800; // Screen display canvas points (A4 ratio = 800 x 1131)
    public double Height { get; set; } = 1131;

    public string BackgroundColorHex { get; set; } = "#FFFFFF";

    public bool ShowHeaderFooter { get; set; } = true;
    public string? HeaderLeft { get; set; }
    public string? HeaderCenter { get; set; }
    public string? HeaderRight { get; set; }
    public string? FooterLeft { get; set; } = "CONFIDENTIAL & PROPRIETARY";
    public string? FooterCenter { get; set; }
    public string? FooterRight { get; set; } = "Page {P} of {N}";

    public PdfWatermarkElement? Watermark { get; set; }

    public List<PdfElementBase> Elements { get; set; } = new();

    public PdfPageModel Clone()
    {
        var clone = new PdfPageModel
        {
            Id = Guid.NewGuid().ToString("N"),
            PageNumber = PageNumber,
            Format = Format,
            Orientation = Orientation,
            RotationAngle = RotationAngle,
            Width = Width,
            Height = Height,
            BackgroundColorHex = BackgroundColorHex,
            ShowHeaderFooter = ShowHeaderFooter,
            HeaderLeft = HeaderLeft,
            HeaderCenter = HeaderCenter,
            HeaderRight = HeaderRight,
            FooterLeft = FooterLeft,
            FooterCenter = FooterCenter,
            FooterRight = FooterRight,
            Watermark = (PdfWatermarkElement?)Watermark?.Clone()
        };

        foreach (var element in Elements)
        {
            clone.Elements.Add(element.Clone());
        }

        return clone;
    }
}
