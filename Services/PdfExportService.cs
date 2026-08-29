using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Services;

public class PdfExportService : IPdfExportService
{
    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdfBytes(PdfDocumentModel model)
    {
        var document = new QuestPdfDocumentWrapper(model);
        return document.GeneratePdf();
    }

    public async Task ExportToFileAsync(PdfDocumentModel model, string filePath)
    {
        var bytes = GeneratePdfBytes(model);
        await File.WriteAllBytesAsync(filePath, bytes);
    }
}

internal class QuestPdfDocumentWrapper : IDocument
{
    private readonly PdfDocumentModel _model;

    public QuestPdfDocumentWrapper(PdfDocumentModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = _model.Title,
        Author = _model.Author,
        Subject = _model.Subject,
        CreationDate = _model.CreatedDate,
        ModifiedDate = _model.ModifiedDate
    };

    public void Compose(IDocumentContainer container)
    {
        foreach (var pageModel in _model.Pages)
        {
            container.Page(page =>
            {
                var pageSize = pageModel.Format switch
                {
                    PageFormat.A4 => PageSizes.A4,
                    PageFormat.Letter => PageSizes.Letter,
                    PageFormat.Legal => PageSizes.Legal,
                    PageFormat.Executive => PageSizes.Executive,
                    _ => PageSizes.A4
                };

                if (pageModel.Orientation == PageOrientation.Landscape)
                {
                    pageSize = pageSize.Landscape();
                }

                page.Size(pageSize);
                page.Margin(36);
                page.PageColor(pageModel.BackgroundColorHex);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor("#201F1E"));

                // Header
                if (pageModel.ShowHeaderFooter && (!string.IsNullOrEmpty(pageModel.HeaderLeft) || !string.IsNullOrEmpty(pageModel.HeaderRight)))
                {
                    page.Header().PaddingBottom(12).Row(row =>
                    {
                        if (!string.IsNullOrEmpty(pageModel.HeaderLeft))
                        {
                            row.RelativeItem().Text(pageModel.HeaderLeft).FontSize(8).FontColor(Colors.Grey.Medium);
                        }
                        if (!string.IsNullOrEmpty(pageModel.HeaderRight))
                        {
                            row.RelativeItem().AlignRight().Text(pageModel.HeaderRight).FontSize(8).FontColor(Colors.Grey.Medium);
                        }
                    });
                }

                // Watermark
                if (pageModel.Watermark != null && !string.IsNullOrWhiteSpace(pageModel.Watermark.Text))
                {
                    page.Foreground().AlignCenter().AlignMiddle().Rotate((float)pageModel.Watermark.Angle)
                        .Text(pageModel.Watermark.Text)
                        .FontSize((float)pageModel.Watermark.FontSize)
                        .FontColor(pageModel.Watermark.ColorHex)
                        .Bold();
                }

                // Content Elements ordered by Y position
                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    var sortedElements = pageModel.Elements.OrderBy(e => e.Y).ThenBy(e => e.X).ToList();

                    foreach (var element in sortedElements)
                    {
                        ComposeElement(col, element);
                    }
                });

                // Footer
                if (pageModel.ShowHeaderFooter)
                {
                    page.Footer().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem().Text(pageModel.FooterLeft ?? "CONFIDENTIAL & PROPRIETARY").FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                    });
                }
            });
        }
    }

    private void ComposeElement(ColumnDescriptor col, PdfElementBase element)
    {
        switch (element)
        {
            case PdfTextElement textEl:
                col.Item().Padding((float)textEl.Padding).Element(c =>
                {
                    var container = c;
                    if (textEl.BorderThickness > 0 && textEl.BorderColorHex != "#00000000")
                    {
                        container = container.Border((float)textEl.BorderThickness).BorderColor(textEl.BorderColorHex).Padding(6);
                    }

                    if (textEl.BackgroundColorHex != "#00000000")
                    {
                        container = container.Background(textEl.BackgroundColorHex);
                    }

                    container.Text(text =>
                    {
                        if (textEl.Alignment == TextAlignmentMode.Center) text.AlignCenter();
                        else if (textEl.Alignment == TextAlignmentMode.Right) text.AlignRight();
                        else if (textEl.Alignment == TextAlignmentMode.Justify) text.Justify();

                        var span = text.Span(textEl.Text)
                            .FontSize((float)textEl.FontSize)
                            .FontColor(textEl.TextColorHex);

                        if (textEl.IsBold) span.Bold();
                        if (textEl.IsItalic) span.Italic();
                        if (textEl.IsUnderline) span.Underline();
                    });
                });
                break;

            case PdfShapeElement shapeEl:
                // Skip full-page decorative border frames from taking up vertical column height
                if (shapeEl.Height > 300 && (shapeEl.FillColorHex == "#00000000" || shapeEl.FillColorHex.StartsWith("#00")))
                {
                    break;
                }

                col.Item().Element(c =>
                {
                    var container = c;
                    if (shapeEl.Height <= 250)
                    {
                        container = container.Height((float)Math.Max(shapeEl.Height, 20));
                    }

                    container = container.Background(shapeEl.FillColorHex)
                                         .Border((float)shapeEl.StrokeThickness)
                                         .BorderColor(shapeEl.StrokeColorHex)
                                         .CornerRadius((float)shapeEl.CornerRadius);

                    if (!string.IsNullOrEmpty(shapeEl.Label))
                    {
                        container.Padding(6).AlignCenter().AlignMiddle().Text(shapeEl.Label)
                            .FontSize((float)shapeEl.LabelFontSize)
                            .FontColor(shapeEl.LabelColorHex ?? "#201F1E")
                            .Bold();
                    }
                });
                break;

            case PdfDividerElement divEl:
                col.Item().LineHorizontal((float)divEl.Thickness).LineColor(divEl.ColorHex);
                break;

            case PdfTableElement tableEl:
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (int i = 0; i < tableEl.Headers.Count; i++)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    // Headers
                    table.Header(header =>
                    {
                        foreach (var h in tableEl.Headers)
                        {
                            header.Cell().Background(tableEl.HeaderBackgroundHex).Padding(6).Text(h)
                                .FontColor(tableEl.HeaderTextHex)
                                .Bold()
                                .FontSize(9);
                        }
                    });

                    // Rows
                    int rowIndex = 0;
                    foreach (var row in tableEl.Rows)
                    {
                        var bg = (rowIndex % 2 == 1) ? tableEl.AlternateRowBackgroundHex : "#FFFFFF";
                        foreach (var cellText in row)
                        {
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(tableEl.BorderColorHex).Padding(5).Text(cellText)
                                .FontSize(9);
                        }
                        rowIndex++;
                    }
                });
                break;

            case PdfChartElement chartEl:
                col.Item().Border(1).BorderColor(chartEl.BorderColorHex).Background(chartEl.BackgroundColorHex).Padding(10).Column(chartCol =>
                {
                    chartCol.Item().AlignCenter().Text(chartEl.Title).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                    chartCol.Item().PaddingTop(8).Row(chartRow =>
                    {
                        for (int i = 0; i < chartEl.Categories.Count; i++)
                        {
                            var idx = i;
                            var cat = chartEl.Categories[idx];
                            var valLabel = idx < chartEl.ValueLabels.Count ? chartEl.ValueLabels[idx] : "";
                            var barColor = idx < chartEl.BarColorsHex.Count ? chartEl.BarColorsHex[idx] : "#0F6CBD";
                            var val = idx < chartEl.Values.Count ? (float)chartEl.Values[idx] : 1f;

                            chartRow.RelativeItem().PaddingHorizontal(4).Column(barCol =>
                            {
                                barCol.Item().AlignCenter().Text(valLabel).FontSize(8).Bold();
                                barCol.Item().Height(val * 20).Background(barColor).CornerRadius(2);
                                barCol.Item().PaddingTop(2).AlignCenter().Text(cat).FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });
                });
                break;

            case PdfImageElement imgEl:
                if (!string.IsNullOrEmpty(imgEl.ImagePath) && File.Exists(imgEl.ImagePath))
                {
                    col.Item().Image(imgEl.ImagePath).FitArea();
                }
                else
                {
                    col.Item().Height((float)imgEl.Height).Border(1).BorderColor(imgEl.BorderColorHex).Background("#F3F2F1").AlignCenter().AlignMiddle()
                        .Text(imgEl.AltText).FontSize(11).FontColor(Colors.Grey.Medium);
                }
                break;
        }
    }
}
