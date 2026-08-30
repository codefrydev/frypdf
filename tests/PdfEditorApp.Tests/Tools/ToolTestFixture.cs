using System;
using System.IO;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Presentation;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using SkiaSharp;
using UglyToad.PdfPig.Writer;

namespace PdfEditorApp.Tests.Tools;

public class ToolTestFixture
{
    public IPdfToolRegistry ToolRegistry { get; }
    public IPdfDocumentOperationsService OperationsService { get; }
    public IPdfToolViewModelFactory Factory { get; }

    public ToolTestFixture()
    {
        ToolRegistry = new PdfToolRegistry();
        var pageService = new PdfPageService();
        var optService = new PdfOptimizationService();
        var secService = new PdfSecurityService();
        var convService = new PdfConversionService();
        var ocrService = new PdfOcrService();
        var formService = new PdfFormService();
        var aiService = new AiDocumentService();
        var transService = new DocumentTranslationService();
        var workflowEngine = new PdfWorkflowEngine(pageService, optService, secService, convService, ocrService);

        OperationsService = new PdfDocumentOperationsService(
            ToolRegistry, pageService, optService, secService, convService, ocrService, formService, aiService, transService, workflowEngine);

        Factory = new PdfToolViewModelFactory(OperationsService, ToolRegistry);
    }

    /// <summary>
    /// Generates a realistic, self-contained multi-page PDF with headers, formatted paragraphs, and shapes.
    /// </summary>
    public static string CreateSamplePdf(string name = "Sample", int pageCount = 2)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{name}_{Guid.NewGuid():N}.pdf");
        using var doc = new PdfDocument();
        for (int i = 0; i < pageCount; i++)
        {
            var page = doc.AddPage();
            page.Width = XUnit.FromPoint(595);
            page.Height = XUnit.FromPoint(842);

            using var gfx = XGraphics.FromPdfPage(page);
            var titleFont = new XFont("Helvetica", 18, XFontStyle.Bold);
            var bodyFont = new XFont("Helvetica", 11, XFontStyle.Regular);
            var headerFont = new XFont("Helvetica", 9, XFontStyle.Italic);

            // Header bar
            gfx.DrawRectangle(XBrushes.SteelBlue, 0, 0, page.Width.Point, 30);
            gfx.DrawString($"FryPDF Generated Test Suite • Document: {name}", headerFont, XBrushes.White, new XPoint(20, 20));

            // Document Title
            gfx.DrawString($"Test Document: {name} (Page {i + 1} of {pageCount})", titleFont, XBrushes.DarkSlateGray, new XPoint(40, 70));

            // Paragraph content
            string p1 = "This is a real, self-contained PDF document generated dynamically for unit testing.";
            string p2 = "It contains multi-page content, paragraphs, metadata, and geometry to test PDF manipulations.";
            string p3 = "CONFIDENTIAL: Internal operations verification test suite for FryPDF.";

            gfx.DrawString(p1, bodyFont, XBrushes.Black, new XPoint(40, 110));
            gfx.DrawString(p2, bodyFont, XBrushes.Black, new XPoint(40, 130));
            gfx.DrawString(p3, bodyFont, XBrushes.DarkRed, new XPoint(40, 150));

            // Simple table
            double tableY = 180;
            gfx.DrawRectangle(XPens.DarkGray, XBrushes.LightGray, 40, tableY, 500, 20);
            gfx.DrawString("Column A", bodyFont, XBrushes.Black, new XPoint(50, tableY + 14));
            gfx.DrawString("Column B", bodyFont, XBrushes.Black, new XPoint(200, tableY + 14));
            gfx.DrawString("Column C", bodyFont, XBrushes.Black, new XPoint(350, tableY + 14));

            for (int r = 1; r <= 3; r++)
            {
                double rowY = tableY + (r * 22);
                gfx.DrawRectangle(XPens.LightGray, 40, rowY, 500, 22);
                gfx.DrawString($"Data {r}-A", bodyFont, XBrushes.Black, new XPoint(50, rowY + 15));
                gfx.DrawString($"Data {r}-B", bodyFont, XBrushes.Black, new XPoint(200, rowY + 15));
                gfx.DrawString($"Data {r}-C", bodyFont, XBrushes.Black, new XPoint(350, rowY + 15));
            }

            // Footer
            gfx.DrawLine(XPens.LightGray, 40, page.Height.Point - 40, page.Width.Point - 40, page.Height.Point - 40);
            gfx.DrawString($"Page {i + 1} of {pageCount}", headerFont, XBrushes.Gray, new XPoint(page.Width.Point / 2 - 20, page.Height.Point - 25));
        }

        doc.Save(path);
        return path;
    }

    /// <summary>
    /// Generates a PDF with web-appended whitespace or trailing comments to test resilience.
    /// </summary>
    public static string CreatePaddedWebPdf(string name = "WebPaddedSample", int pageCount = 3)
    {
        string basePdf = CreateSamplePdf(name, pageCount);
        byte[] bytes = File.ReadAllBytes(basePdf);
        using var ms = new MemoryStream();
        ms.Write(bytes, 0, bytes.Length);
        byte[] extra = Encoding.ASCII.GetBytes("\r\n% Trailing web analytics comment\r\n\r\n   \r\n");
        ms.Write(extra, 0, extra.Length);

        string paddedPath = Path.Combine(Path.GetTempPath(), $"{name}_padded_{Guid.NewGuid():N}.pdf");
        File.WriteAllBytes(paddedPath, ms.ToArray());

        if (File.Exists(basePdf)) File.Delete(basePdf);
        return paddedPath;
    }

    public static string CreateSampleDocx(string name = "SampleDocx")
    {
        string path = Path.Combine(Path.GetTempPath(), $"{name}_{Guid.NewGuid():N}.docx");
        using var wordDoc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
        var body = mainPart.Document.AppendChild(new Body());
        var para = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
        var run = para.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("Sample Word Document Paragraph for Testing"));
        mainPart.Document.Save();
        return path;
    }

    public static string CreateSampleXlsx(string name = "SampleXlsx")
    {
        string path = Path.Combine(Path.GetTempPath(), $"{name}_{Guid.NewGuid():N}.xlsx");
        using var spreadsheetDoc = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
        var wbPart = spreadsheetDoc.AddWorkbookPart();
        wbPart.Workbook = new Workbook();
        var wsPart = wbPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        var row = new Row();
        var cell = new Cell { DataType = CellValues.String, CellValue = new CellValue("Header 1") };
        row.Append(cell);
        sheetData.Append(row);
        wsPart.Worksheet = new Worksheet(sheetData);
        var sheets = wbPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = 1, Name = "Sheet1" });
        wbPart.Workbook.Save();
        return path;
    }

    public static string CreateSamplePptx(string name = "SamplePptx")
    {
        string path = Path.Combine(Path.GetTempPath(), $"{name}_{Guid.NewGuid():N}.pptx");
        using var presentationDoc = PresentationDocument.Create(path, PresentationDocumentType.Presentation);
        var pPart = presentationDoc.AddPresentationPart();
        pPart.Presentation = new Presentation();
        var slideIdList = pPart.Presentation.AppendChild(new SlideIdList());
        var slidePart = pPart.AddNewPart<SlidePart>();
        var slide = new Slide(new CommonSlideData(new ShapeTree()));
        slidePart.Slide = slide;
        slidePart.Slide.Save();
        slideIdList.Append(new SlideId { Id = 256, RelationshipId = pPart.GetIdOfPart(slidePart) });
        pPart.Presentation.Save();
        return path;
    }

    public static string CreateSampleImage(string name = "SampleImage")
    {
        string path = Path.Combine(Path.GetTempPath(), $"{name}_{Guid.NewGuid():N}.png");
        using var bitmap = new SKBitmap(300, 300);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.AliceBlue);
            using var paint = new SKPaint { Color = SKColors.Navy, IsAntialias = true };
            canvas.DrawRect(30, 30, 240, 240, paint);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
        return path;
    }
}
