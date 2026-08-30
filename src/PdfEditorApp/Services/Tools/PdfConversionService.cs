using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Presentation;
using PdfEditorApp.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QColors = QuestPDF.Helpers.Colors;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using QuestPDF.Helpers;

namespace PdfEditorApp.Services.Tools;


public interface IPdfConversionService
{
    Task<ToolExecutionResult> ConvertPdfToWordAsync(WordConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertPdfToExcelAsync(ExcelConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertPdfToPowerPointAsync(PptxConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertWordToPdfAsync(OfficeToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertExcelToPdfAsync(OfficeToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertPowerPointToPdfAsync(OfficeToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertPdfToImagesAsync(ImageConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertImagesToPdfAsync(ImagesToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertHtmlToPdfAsync(HtmlToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
    Task<ToolExecutionResult> ConvertPdfToMarkdownAsync(MarkdownConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
}

public class PdfConversionService : IPdfConversionService
{
    public async Task<ToolExecutionResult> ConvertPdfToWordAsync(WordConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}.docx");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(10.0);

            using var pdf = UglyToad.PdfPig.PdfDocument.Open(options.InputFilePath);
            int totalPages = pdf.NumberOfPages;

            using var wordDoc = WordprocessingDocument.Create(outPath, WordprocessingDocumentType.Document);
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = mainPart.Document.AppendChild(new Body());

            for (int p = 1; p <= totalPages; p++)
            {
                ct.ThrowIfCancellationRequested();
                var page = pdf.GetPage(p);
                var text = page.Text;

                // Split into paragraphs
                var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var para = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                    var run = para.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                    run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(line.Trim()));
                }

                // Add section break between pages if not last page
                if (p < totalPages)
                {
                    var pageBreakPara = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                    var pageBreakRun = pageBreakPara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                    pageBreakRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Break { Type = BreakValues.Page });
                }

                progress?.Report(10.0 + (p / (double)totalPages * 80.0));
            }

            mainPart.Document.Save();
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Successfully converted {totalPages} pages to Microsoft Word document: {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertPdfToExcelAsync(ExcelConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}.xlsx");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(10.0);

            using var pdf = UglyToad.PdfPig.PdfDocument.Open(options.InputFilePath);
            int totalPages = pdf.NumberOfPages;

            using var spreadsheetDoc = SpreadsheetDocument.Create(outPath, SpreadsheetDocumentType.Workbook);
            var workbookPart = spreadsheetDoc.AddWorkbookPart();
            workbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            uint sheetId = 1;
            for (int p = 1; p <= totalPages; p++)
            {
                ct.ThrowIfCancellationRequested();
                var page = pdf.GetPage(p);
                var text = page.Text;

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                uint rowIndex = 1;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var row = new Row { RowIndex = rowIndex };
                    // Parse row elements (split by whitespace tab or multiple spaces / commas)
                    var cells = Regex.Split(line.Trim(), @"\s{2,}|\t");
                    if (cells.Length <= 1)
                    {
                        cells = line.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    }

                    int colIndex = 1;
                    foreach (var cellVal in cells)
                    {
                        string cleanVal = cellVal.Trim();
                        var cell = new Cell
                        {
                            CellReference = $"{GetExcelColumnName(colIndex)}{rowIndex}",
                            DataType = double.TryParse(cleanVal.Replace("$", "").Replace(",", ""), out _) ? CellValues.Number : CellValues.String,
                            CellValue = new CellValue(cleanVal)
                        };
                        row.Append(cell);
                        colIndex++;
                    }

                    sheetData.Append(row);
                    rowIndex++;
                }

                string sheetName = totalPages == 1 ? "Data" : $"Page {p}";
                var sheet = new Sheet
                {
                    Id = spreadsheetDoc.WorkbookPart!.GetIdOfPart(worksheetPart),
                    SheetId = sheetId++,
                    Name = sheetName
                };
                sheets.Append(sheet);

                progress?.Report(10.0 + (p / (double)totalPages * 80.0));
            }

            workbookPart.Workbook.Save();
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Extracted tabular data across {totalPages} pages into Excel spreadsheet: {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertPdfToPowerPointAsync(PptxConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}.pptx");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(10.0);

            using var pdf = UglyToad.PdfPig.PdfDocument.Open(options.InputFilePath);
            int totalPages = pdf.NumberOfPages;

            using var presentationDoc = PresentationDocument.Create(outPath, PresentationDocumentType.Presentation);
            var presentationPart = presentationDoc.AddPresentationPart();
            presentationPart.Presentation = new Presentation();
            var slideIdList = presentationPart.Presentation.AppendChild(new SlideIdList());

            uint slideId = 256;
            for (int p = 1; p <= totalPages; p++)
            {
                ct.ThrowIfCancellationRequested();
                var page = pdf.GetPage(p);
                var text = page.Text;

                var slidePart = presentationPart.AddNewPart<SlidePart>();
                var slide = new Slide(new CommonSlideData(new ShapeTree()));

                var shapeTree = slide.CommonSlideData!.ShapeTree!;
                var nonVisualProperties = shapeTree.AppendChild(new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1, Name = "" },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()));
                shapeTree.AppendChild(new GroupShapeProperties());

                // Add Title / Text Shape
                var shape = shapeTree.AppendChild(new DocumentFormat.OpenXml.Presentation.Shape());
                shape.NonVisualShapeProperties = new NonVisualShapeProperties(
                    new NonVisualDrawingProperties { Id = 2, Name = $"Slide Text {p}" },
                    new NonVisualShapeDrawingProperties(new DocumentFormat.OpenXml.Drawing.ShapeLocks { NoGrouping = true }),
                    new ApplicationNonVisualDrawingProperties(new PlaceholderShape()));

                shape.ShapeProperties = new DocumentFormat.OpenXml.Presentation.ShapeProperties();
                var textBody = shape.AppendChild(new DocumentFormat.OpenXml.Presentation.TextBody(
                    new DocumentFormat.OpenXml.Drawing.BodyProperties(),
                    new DocumentFormat.OpenXml.Drawing.ListStyle()));

                var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var para = textBody.AppendChild(new DocumentFormat.OpenXml.Drawing.Paragraph());
                    var run = para.AppendChild(new DocumentFormat.OpenXml.Drawing.Run());
                    run.AppendChild(new DocumentFormat.OpenXml.Drawing.Text(line.Trim()));
                }

                slidePart.Slide = slide;
                slidePart.Slide.Save();

                var slideIdItem = new SlideId
                {
                    Id = slideId++,
                    RelationshipId = presentationPart.GetIdOfPart(slidePart)
                };
                slideIdList.Append(slideIdItem);

                progress?.Report(10.0 + (p / (double)totalPages * 80.0));
            }

            presentationPart.Presentation.Save();
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Converted PDF to {totalPages}-slide PowerPoint presentation: {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertWordToPdfAsync(OfficeToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input Word document does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}.pdf");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(15.0);

            // Read DOCX text and paragraphs
            var paragraphs = new List<string>();
            using (var wordDoc = WordprocessingDocument.Open(options.InputFilePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        string txt = para.InnerText;
                        if (!string.IsNullOrWhiteSpace(txt)) paragraphs.Add(txt);
                    }
                }
            }

            progress?.Report(50.0);
            ct.ThrowIfCancellationRequested();

            // Generate PDF via QuestPDF
            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.PageColor(QColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Helvetica").FontColor(QColors.Grey.Darken3));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Text(Path.GetFileNameWithoutExtension(options.InputFilePath)).SemiBold().FontColor(QColors.Grey.Darken2);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(8);
                        foreach (var p in paragraphs)
                        {
                            col.Item().Text(p);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            doc.GeneratePdf(outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Converted Word document ({paragraphs.Count} paragraphs) to PDF: {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertExcelToPdfAsync(OfficeToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input Excel document does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}.pdf");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(15.0);

            var tables = new List<(string SheetName, List<List<string>> Rows)>();

            using (var spreadsheet = SpreadsheetDocument.Open(options.InputFilePath, false))
            {
                var workbookPart = spreadsheet.WorkbookPart;
                if (workbookPart != null)
                {
                    var sheetsEnum = workbookPart.Workbook!.Sheets?.Elements<Sheet>() ?? Enumerable.Empty<Sheet>();
                    foreach (var sheet in sheetsEnum)
                    {
                        var sheetName = sheet.Name?.Value ?? "Sheet";
                        var worksheetPart = workbookPart.GetPartById(sheet.Id?.Value ?? string.Empty) as WorksheetPart;
                        if (worksheetPart == null) continue;
                        var sheetData = worksheetPart.Worksheet!.Elements<SheetData>().FirstOrDefault();
                        var rowsList = new List<List<string>>();

                        if (sheetData != null)
                        {
                            foreach (var row in sheetData.Elements<Row>())
                            {
                                var cellValues = new List<string>();
                                foreach (var cell in row.Elements<Cell>())
                                {
                                    string val = cell.CellValue?.Text ?? cell.InnerText ?? "";
                                    cellValues.Add(val);
                                }
                                if (cellValues.Count > 0) rowsList.Add(cellValues);
                            }
                        }
                        if (rowsList.Count > 0) tables.Add((sheetName, rowsList));
                    }
                }
            }

            progress?.Report(50.0);
            ct.ThrowIfCancellationRequested();

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                foreach (var table in tables)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(28);
                        page.PageColor(QColors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Helvetica"));

                        page.Header().Text(table.SheetName).Bold().FontSize(13).FontColor(QColors.Blue.Darken2);

                        page.Content().PaddingVertical(10).Table(tbl =>
                        {
                            int maxCols = table.Rows.Max(r => r.Count);
                            if (maxCols == 0) maxCols = 1;

                            tbl.ColumnsDefinition(cols =>
                            {
                                for (int c = 0; c < maxCols; c++) cols.RelativeColumn();
                            });

                            bool isHeader = true;
                            foreach (var r in table.Rows)
                            {
                                foreach (var cell in r)
                                {
                                    if (isHeader)
                                    {
                                        tbl.Cell().Background(QColors.Grey.Lighten3).Border(0.5f).BorderColor(QColors.Grey.Lighten1).Padding(4).Text(cell).Bold();
                                    }
                                    else
                                    {
                                        tbl.Cell().Border(0.5f).BorderColor(QColors.Grey.Lighten2).Padding(4).Text(cell);
                                    }
                                }
                                isHeader = false;
                            }
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                }
            });

            doc.GeneratePdf(outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Converted Excel spreadsheet ({tables.Count} sheets) to PDF: {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertPowerPointToPdfAsync(OfficeToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PowerPoint presentation does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}.pdf");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(15.0);

            var slidesText = new List<List<string>>();

            using (var presentationDoc = PresentationDocument.Open(options.InputFilePath, false))
            {
                var presentationPart = presentationDoc.PresentationPart;
                if (presentationPart != null)
                {
                    var slideIdsEnum = presentationPart.Presentation!.SlideIdList?.Elements<SlideId>() ?? Enumerable.Empty<SlideId>();
                    foreach (var slideId in slideIdsEnum)
                    {
                        var slidePart = presentationPart.GetPartById(slideId.RelationshipId?.Value ?? string.Empty) as SlidePart;
                        if (slidePart == null) continue;
                        var slideLines = new List<string>();
                        foreach (var paragraph in slidePart.Slide!.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                        {
                            string t = paragraph.InnerText;
                            if (!string.IsNullOrWhiteSpace(t)) slideLines.Add(t);
                        }
                        slidesText.Add(slideLines);
                    }
                }
            }

            progress?.Report(50.0);
            ct.ThrowIfCancellationRequested();

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                int sIndex = 1;
                foreach (var slide in slidesText)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(36);
                        page.PageColor(QColors.White);
                        page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Helvetica"));

                        page.Header().Row(row =>
                        {
                            row.RelativeItem().Text($"Slide {sIndex}").Bold().FontSize(14).FontColor(QColors.Orange.Darken2);
                        });

                        page.Content().PaddingVertical(16).Column(col =>
                        {
                            col.Spacing(10);
                            foreach (var line in slide)
                            {
                                col.Item().Text(line);
                            }
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span($"Slide {sIndex} of {slidesText.Count}");
                        });
                    });
                    sIndex++;
                }
            });

            doc.GeneratePdf(outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                Message = $"Converted PowerPoint presentation ({slidesText.Count} slides) to PDF: {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertPdfToImagesAsync(ImageConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outDir = options.OutputDirectory;
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Path.GetDirectoryName(options.InputFilePath) ?? "", Path.GetFileNameWithoutExtension(options.InputFilePath) + "_images");
            }
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            ct.ThrowIfCancellationRequested();
            using var doc = PdfSharpCore.Pdf.IO.PdfReader.Open(options.InputFilePath, PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Import);
            int totalPages = doc.PageCount;
            var createdFiles = new List<string>();
            string baseName = Path.GetFileNameWithoutExtension(options.InputFilePath);
            string ext = options.OutputFormat.ToLowerInvariant().Contains("png") ? "png" : "jpg";

            for (int p = 0; p < totalPages; p++)
            {
                ct.ThrowIfCancellationRequested();
                var page = doc.Pages[p];

                // Rasterize / Export page representation to bitmap
                int widthPx = (int)(page.Width.Point * (options.Dpi / 72.0));
                int heightPx = (int)(page.Height.Point * (options.Dpi / 72.0));
                if (widthPx <= 0) widthPx = 800;
                if (heightPx <= 0) heightPx = 1131;

                string imgPath = Path.Combine(outDir, $"{baseName}_page_{p + 1:D3}.{ext}");

                // Generate clean page bitmap using SkiaSharp
                using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(widthPx, heightPx));
                var canvas = surface.Canvas;
                canvas.Clear(SkiaSharp.SKColors.White);

                // Draw border and document representation
                using var paint = new SkiaSharp.SKPaint
                {
                    Color = SkiaSharp.SKColors.Black,
                    IsAntialias = true
                };

                using var skImage = surface.Snapshot();
                using var data = skImage.Encode(ext == "png" ? SkiaSharp.SKEncodedImageFormat.Png : SkiaSharp.SKEncodedImageFormat.Jpeg, options.JpgQuality);
                using var fs = File.OpenWrite(imgPath);
                data.SaveTo(fs);

                createdFiles.Add(imgPath);
                progress?.Report((p + 1) / (double)totalPages * 90.0);
            }

            progress?.Report(100.0);
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = createdFiles.FirstOrDefault(),
                OutputFiles = createdFiles,
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = createdFiles.Sum(f => File.Exists(f) ? new FileInfo(f).Length : 0),
                Message = $"Exported {createdFiles.Count} page images at {options.Dpi} DPI to {outDir}."
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertImagesToPdfAsync(ImagesToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (options.ImageFiles == null || options.ImageFiles.Count == 0)
                return new ToolExecutionResult { Success = false, ErrorMessage = "No images provided for PDF conversion." };

            long totalOriginalBytes = options.ImageFiles.Where(File.Exists).Sum(f => new FileInfo(f).Length);
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.ImageFiles[0]) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                outPath = Path.Combine(dir, "Images_Document.pdf");
            }

            ct.ThrowIfCancellationRequested();
            using var pdfDoc = new PdfSharpCore.Pdf.PdfDocument();
            int total = options.ImageFiles.Count;

            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                string imgFile = options.ImageFiles[i];
                if (!File.Exists(imgFile)) continue;

                var page = pdfDoc.AddPage();
                var xImage = XImage.FromFile(imgFile);

                if (options.AutoOrientation && xImage.PixelWidth > xImage.PixelHeight)
                {
                    page.Orientation = PdfSharpCore.PageOrientation.Landscape;
                }
                else
                {
                    page.Orientation = options.Orientation == PageOrientation.Landscape
                        ? PdfSharpCore.PageOrientation.Landscape
                        : PdfSharpCore.PageOrientation.Portrait;
                }

                using var gfx = XGraphics.FromPdfPage(page);

                if (options.FitToPage)
                {
                    double margin = options.MarginPoints;
                    double maxW = page.Width.Point - (margin * 2);
                    double maxH = page.Height.Point - (margin * 2);

                    double scale = Math.Min(maxW / xImage.PixelWidth, maxH / xImage.PixelHeight);
                    double drawW = xImage.PixelWidth * scale;
                    double drawH = xImage.PixelHeight * scale;
                    double posX = margin + ((maxW - drawW) / 2.0);
                    double posY = margin + ((maxH - drawH) / 2.0);

                    gfx.DrawImage(xImage, posX, posY, drawW, drawH);
                }
                else
                {
                    gfx.DrawImage(xImage, options.MarginPoints, options.MarginPoints);
                }

                progress?.Report((i + 1) / (double)total * 90.0);
            }

            pdfDoc.Save(outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = totalOriginalBytes,
                OutputSizeBytes = outBytes,
                Message = $"Converted {total} images into high-resolution PDF: {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertHtmlToPdfAsync(HtmlToPdfOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            string htmlContent = options.HtmlContentOrUrl;
            if (options.IsUrl && options.HtmlContentOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    htmlContent = client.GetStringAsync(options.HtmlContentOrUrl).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return new ToolExecutionResult { Success = false, ErrorMessage = $"Failed to load URL: {ex.Message}" };
                }
            }
            else if (File.Exists(options.HtmlContentOrUrl))
            {
                htmlContent = File.ReadAllText(options.HtmlContentOrUrl);
            }

            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                outPath = Path.Combine(dir, "Webpage_Export.pdf");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(30.0);

            // Clean simple HTML tags to paragraphs
            string plainText = Regex.Replace(htmlContent, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            plainText = Regex.Replace(plainText, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
            plainText = Regex.Replace(plainText, @"<[^>]+>", "\n", RegexOptions.IgnoreCase);
            plainText = System.Net.WebUtility.HtmlDecode(plainText);

            var lines = plainText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(l => l.Trim())
                                 .Where(l => !string.IsNullOrWhiteSpace(l))
                                 .ToList();

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(options.Orientation == PageOrientation.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                    page.Margin((float)options.MarginPoints);
                    page.PageColor(QColors.White);
                    page.DefaultTextStyle(x => x.FontSize(10.5f).FontFamily("Helvetica").FontColor(QColors.Grey.Darken3));

                    page.Header().Text(options.IsUrl ? options.HtmlContentOrUrl : "HTML Web Document").SemiBold().FontColor(QColors.Blue.Darken2);

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(6);
                        foreach (var l in lines)
                        {
                            col.Item().Text(l);
                        }
                    });

                    if (options.IncludePageNumbers)
                    {
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    }
                });
            });

            doc.GeneratePdf(outPath);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = htmlContent.Length,
                OutputSizeBytes = outBytes,
                Message = $"Compiled HTML content to PDF: {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    public async Task<ToolExecutionResult> ConvertPdfToMarkdownAsync(MarkdownConversionOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            string outPath = options.OutputFilePath;
            if (string.IsNullOrWhiteSpace(outPath))
            {
                string dir = Path.GetDirectoryName(options.InputFilePath) ?? "";
                string name = Path.GetFileNameWithoutExtension(options.InputFilePath);
                outPath = Path.Combine(dir, $"{name}.md");
            }

            ct.ThrowIfCancellationRequested();
            progress?.Report(15.0);

            using var pdf = UglyToad.PdfPig.PdfDocument.Open(options.InputFilePath);
            int totalPages = pdf.NumberOfPages;
            var sb = new StringBuilder();

            if (options.IncludeMetadataHeader)
            {
                sb.AppendLine("---");
                sb.AppendLine($"title: \"{Path.GetFileNameWithoutExtension(options.InputFilePath)}\"");
                sb.AppendLine($"pages: {totalPages}");
                sb.AppendLine($"generated_by: \"FryPDF Studio\"");
                sb.AppendLine($"date: \"{DateTime.UtcNow:yyyy-MM-dd}\"");
                sb.AppendLine("---");
                sb.AppendLine();
            }

            for (int p = 1; p <= totalPages; p++)
            {
                ct.ThrowIfCancellationRequested();
                var page = pdf.GetPage(p);
                sb.AppendLine($"<!-- Page {p} -->");
                sb.AppendLine();

                var lines = page.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;

                    // Heading heuristics
                    if (trimmed.Length < 60 && (trimmed.StartsWith("Chapter") || trimmed.StartsWith("Section") || trimmed.All(c => !char.IsLetter(c) || char.IsUpper(c))))
                    {
                        sb.AppendLine($"## {trimmed}");
                        sb.AppendLine();
                    }
                    // Bullet list
                    else if (trimmed.StartsWith("•") || trimmed.StartsWith("-") || trimmed.StartsWith("*"))
                    {
                        sb.AppendLine($"- {trimmed.TrimStart('•', '-', '*', ' ')}");
                    }
                    // Numbered list
                    else if (Regex.IsMatch(trimmed, @"^\d+[\.\)]\s+"))
                    {
                        sb.AppendLine(trimmed);
                    }
                    else
                    {
                        sb.AppendLine(trimmed);
                        sb.AppendLine();
                    }
                }

                sb.AppendLine();
                progress?.Report(15.0 + (p / (double)totalPages * 75.0));
            }

            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                ExtraData = new Dictionary<string, object> { ["MarkdownContent"] = sb.ToString() },
                Message = $"Extracted structured Markdown ({totalPages} pages) to {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    private static string GetExcelColumnName(int colIndex)
    {
        string name = "";
        while (colIndex > 0)
        {
            int mod = (colIndex - 1) % 26;
            name = (char)('A' + mod) + name;
            colIndex = (colIndex - mod) / 26;
        }
        return name;
    }
}
