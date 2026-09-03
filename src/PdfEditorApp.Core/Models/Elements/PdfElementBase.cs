using System;
using System.Text.Json.Serialization;

namespace PdfEditorApp.Core.Models.Elements;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PdfTextElement), typeDiscriminator: "text")]
[JsonDerivedType(typeof(PdfImageElement), typeDiscriminator: "image")]
[JsonDerivedType(typeof(PdfShapeElement), typeDiscriminator: "shape")]
[JsonDerivedType(typeof(PdfDividerElement), typeDiscriminator: "divider")]
[JsonDerivedType(typeof(PdfChartElement), typeDiscriminator: "chart")]
[JsonDerivedType(typeof(PdfTableElement), typeDiscriminator: "table")]
[JsonDerivedType(typeof(PdfWatermarkElement), typeDiscriminator: "watermark")]
[JsonDerivedType(typeof(PdfFormFieldElement), typeDiscriminator: "formfield")]
[JsonDerivedType(typeof(PdfQrCodeElement), typeDiscriminator: "qrcode")]
[JsonDerivedType(typeof(PdfBarcodeElement), typeDiscriminator: "barcode")]
[JsonDerivedType(typeof(PdfRedactionElement), typeDiscriminator: "redaction")]
[JsonDerivedType(typeof(PdfInkElement), typeDiscriminator: "ink")]
[JsonDerivedType(typeof(PdfStickyNoteElement), typeDiscriminator: "stickynote")]
[JsonDerivedType(typeof(PdfMeasurementElement), typeDiscriminator: "measurement")]
[JsonDerivedType(typeof(PdfSvgElement), typeDiscriminator: "svg")]
[JsonDerivedType(typeof(PdfMathElement), typeDiscriminator: "math")]
public abstract class PdfElementBase
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public abstract ElementKind Kind { get; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 100;
    public int ZIndex { get; set; } = 0;
    public double Rotation { get; set; } = 0;
    public double Opacity { get; set; } = 1.0;
    public bool IsLocked { get; set; } = false;
    public string? GroupId { get; set; }

    public virtual PdfElementBase Clone()
    {
        var clone = (PdfElementBase)MemberwiseClone();
        clone.Id = Guid.NewGuid().ToString("N");
        return clone;
    }
}
