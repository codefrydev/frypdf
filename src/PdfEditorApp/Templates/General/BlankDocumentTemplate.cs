using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class BlankDocumentTemplate : ITemplateDefinition
{
    public string Id => "blank";
    public string Name => "Blank Page";
    public string Description => "Start fresh with a clean customizable canvas";
    public string Category => "General";
    public string IconKind => "FileOutline";
    public string AccentColorHex => "#64748B";

    public PdfDocumentModel Create()
    {
        return Create(PageFormat.A4, PageOrientation.Portrait);
    }

    public PdfDocumentModel Create(PageFormat format = PageFormat.A4, PageOrientation orientation = PageOrientation.Portrait)
    {
        var doc = new PdfDocumentModel
        {
            Title = "Untitled_Document.pdf",
            Author = "CodeFryDev",
            Subject = "New PDF Document"
        };

        var width = format switch
        {
            PageFormat.A4 => 800.0,
            PageFormat.Letter => 800.0,
            PageFormat.Legal => 800.0,
            _ => 800.0
        };

        var height = format switch
        {
            PageFormat.A4 => 1131.0,
            PageFormat.Letter => 1035.0,
            PageFormat.Legal => 1318.0,
            _ => 1131.0
        };

        if (orientation == PageOrientation.Landscape)
        {
            (width, height) = (height, width);
        }

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = format,
            Orientation = orientation,
            Width = width,
            Height = height,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "CONFIDENTIAL",
            FooterRight = "Page 1 of 1",
            Elements = new List<PdfElementBase>
            {
                new PdfTextElement
                {
                    X = 60,
                    Y = 60,
                    Width = 680,
                    Height = 40,
                    Text = "Click to Edit Heading",
                    FontSize = 24,
                    IsBold = true,
                    TextColorHex = "#111827"
                },
                new PdfTextElement
                {
                    X = 60,
                    Y = 110,
                    Width = 680,
                    Height = 60,
                    Text = "Start typing your content here, or use the Ribbon toolbar above to insert text blocks, images, tables, shapes, and watermarks.",
                    FontSize = 13,
                    TextColorHex = "#4B5563"
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
