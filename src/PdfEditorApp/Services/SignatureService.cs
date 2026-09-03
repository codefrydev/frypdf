using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public class SignaturePresetStyle
{
    public string Name { get; set; } = "Cursive Elegance";
    public SignatureStyle Style { get; set; } = SignatureStyle.CursiveElegance;
    public string FontFamily { get; set; } = "Georgia";
    public bool IsItalic { get; set; } = true;
    public string ColorHex { get; set; } = "#002B49"; // Traditional dark fountain blue
}

public interface ISignatureService
{
    List<SignaturePresetStyle> GetAvailableSignatureStyles();
    PdfTextElement CreateCursiveSignatureElement(string signerName, SignatureStyle style, double x = 100, double y = 200);
    PdfShapeElement CreateSignatureBoxElement(string signerName, string title = "Authorized Signatory", double x = 100, double y = 200);
    PdfShapeElement CreateCryptographicSignatureSeal(string signerName, string organization, string reason, string sha256Fingerprint, double x = 100, double y = 200);
    PdfTextElement CreateDateStampElement(double x = 100, double y = 200);
    PdfShapeElement CreateInitialsElement(string initials, double x = 100, double y = 200);
    PdfShapeElement CreateMarkupBadge(string symbol, string colorHex, double x = 100, double y = 200);
    PdfFormFieldElement CreateFormFieldElement(FormFieldType fieldType, string fieldName, double x = 100, double y = 200, double width = 180, double height = 32);
    string ComputeDocumentSha256(PdfDocumentModel document);
}

public class SignatureService : ISignatureService
{
    public List<SignaturePresetStyle> GetAvailableSignatureStyles()
    {
        return new List<SignaturePresetStyle>
        {
            new() { Name = "Executive Elegance", Style = SignatureStyle.CursiveElegance, FontFamily = "Georgia", IsItalic = true, ColorHex = "#0F2942" },
            new() { Name = "Classic Script", Style = SignatureStyle.ClassicScript, FontFamily = "Times New Roman", IsItalic = true, ColorHex = "#0F6CBD" },
            new() { Name = "Modern Signature", Style = SignatureStyle.SignatureCasual, FontFamily = "Segoe UI", IsItalic = true, ColorHex = "#1E293B" },
            new() { Name = "Handwritten Legal Blue", Style = SignatureStyle.ModernHandwriting, FontFamily = "Arial", IsItalic = true, ColorHex = "#003366" }
        };
    }

    public PdfTextElement CreateCursiveSignatureElement(string signerName, SignatureStyle style, double x = 100, double y = 200)
    {
        string font = style switch
        {
            SignatureStyle.CursiveElegance => "Georgia",
            SignatureStyle.ClassicScript => "Times New Roman",
            SignatureStyle.SignatureCasual => "Segoe UI",
            SignatureStyle.ModernHandwriting => "Arial",
            _ => "Georgia"
        };

        string color = style switch
        {
            SignatureStyle.ClassicScript => "#0F6CBD",
            SignatureStyle.ModernHandwriting => "#003366",
            _ => "#0F2942"
        };

        return new PdfTextElement
        {
            X = x,
            Y = y,
            Width = 260,
            Height = 55,
            Text = signerName,
            FontFamily = font,
            FontSize = 26,
            IsItalic = true,
            TextColorHex = color,
            Alignment = TextAlignmentMode.Left
        };
    }

    public PdfShapeElement CreateSignatureBoxElement(string signerName, string title = "Authorized Signatory", double x = 100, double y = 200)
    {
        return new PdfShapeElement
        {
            X = x,
            Y = y,
            Width = 280,
            Height = 90,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#F8FAFC",
            StrokeColorHex = "#0F6CBD",
            StrokeThickness = 1.5,
            Label = $"DIGITALLY SIGNED & VERIFIED\n{signerName} ({title})\n{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            LabelColorHex = "#0F6CBD",
            LabelFontSize = 10
        };
    }

    public PdfShapeElement CreateCryptographicSignatureSeal(string signerName, string organization, string reason, string sha256Fingerprint, double x = 100, double y = 200)
    {
        string shortFingerprint = sha256Fingerprint.Length > 16 ? sha256Fingerprint.Substring(0, 16) : sha256Fingerprint;
        return new PdfShapeElement
        {
            X = x,
            Y = y,
            Width = 320,
            Height = 100,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 8,
            FillColorHex = "#F0FDF4", // Light emerald tint
            StrokeColorHex = "#16A34A", // Emerald green border
            StrokeThickness = 1.5,
            Label = $"CRYPTOGRAPHIC X.509 DIGITAL SEAL\nSigner: {signerName} • {organization}\nReason: {reason}\nDigest: SHA256:{shortFingerprint}...\nVerified: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            LabelColorHex = "#15803D",
            LabelFontSize = 9.5
        };
    }

    public PdfTextElement CreateDateStampElement(double x = 100, double y = 200)
    {
        return new PdfTextElement
        {
            X = x,
            Y = y,
            Width = 180,
            Height = 32,
            Text = $"Date: {DateTime.Now:MMM dd, yyyy}",
            FontFamily = "Segoe UI",
            FontSize = 12,
            TextColorHex = "#334155",
            Alignment = TextAlignmentMode.Left
        };
    }

    public PdfShapeElement CreateInitialsElement(string initials, double x = 100, double y = 200)
    {
        string init = string.IsNullOrWhiteSpace(initials) ? "JD" : initials.Trim().ToUpper();
        return new PdfShapeElement
        {
            X = x,
            Y = y,
            Width = 60,
            Height = 60,
            ShapeType = ShapeType.Circle,
            FillColorHex = "#EFF6FF",
            StrokeColorHex = "#0F6CBD",
            StrokeThickness = 2.0,
            Label = init,
            LabelColorHex = "#0F6CBD",
            LabelFontSize = 18
        };
    }

    public PdfShapeElement CreateMarkupBadge(string symbol, string colorHex, double x = 100, double y = 200)
    {
        return new PdfShapeElement
        {
            X = x,
            Y = y,
            Width = 36,
            Height = 36,
            ShapeType = ShapeType.RoundedRectangle,
            CornerRadius = 6,
            FillColorHex = "#FFFFFF",
            StrokeColorHex = colorHex,
            StrokeThickness = 1.5,
            Label = symbol,
            LabelColorHex = colorHex,
            LabelFontSize = 18
        };
    }

    public PdfFormFieldElement CreateFormFieldElement(FormFieldType fieldType, string fieldName, double x = 100, double y = 200, double width = 180, double height = 32)
    {
        return new PdfFormFieldElement
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            FieldType = fieldType,
            FieldName = fieldName,
            DefaultValue = fieldType == FormFieldType.Checkbox ? "false" : ""
        };
    }

    public string ComputeDocumentSha256(PdfDocumentModel document)
    {
        var sb = new StringBuilder();
        sb.Append(document.Title).Append('|');
        sb.Append(document.Author).Append('|');
        sb.Append(document.Subject).Append('|');
        sb.Append(document.Pages.Count).Append('|');

        foreach (var page in document.Pages)
        {
            sb.Append(page.PageNumber).Append(':').Append(page.Elements.Count).Append('|');
            foreach (var el in page.Elements)
            {
                sb.Append(el.Kind).Append('@').Append(el.X).Append(',').Append(el.Y).Append(';');
            }
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash);
    }
}
