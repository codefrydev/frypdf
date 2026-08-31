using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Data;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Typography;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.Tests;

public class RichTextEngineTests
{
    [Fact]
    public void PdfTextSpan_Clone_CreatesDeepCopy()
    {
        var original = new PdfTextSpan
        {
            Text = "Sample Run",
            FontFamily = "Roboto",
            FontSize = 18.5,
            IsBold = true,
            IsItalic = true,
            IsUnderline = true,
            IsStrikethrough = false,
            TextColorHex = "#0F6CBD",
            HighlightColorHex = "#FFEB3B",
            Script = TextScriptMode.Superscript,
            LinkUrl = "https://example.com"
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(original.Text, clone.Text);
        Assert.Equal(original.FontFamily, clone.FontFamily);
        Assert.Equal(original.FontSize, clone.FontSize);
        Assert.Equal(original.IsBold, clone.IsBold);
        Assert.Equal(original.IsItalic, clone.IsItalic);
        Assert.Equal(original.IsUnderline, clone.IsUnderline);
        Assert.Equal(original.IsStrikethrough, clone.IsStrikethrough);
        Assert.Equal(original.TextColorHex, clone.TextColorHex);
        Assert.Equal(original.HighlightColorHex, clone.HighlightColorHex);
        Assert.Equal(original.Script, clone.Script);
        Assert.Equal(original.LinkUrl, clone.LinkUrl);
    }

    [Fact]
    public void PdfTextElement_WithSpans_ClonesDeeplyAndSynchronizes()
    {
        var element = new PdfTextElement
        {
            Text = "Fallback Plain",
            FontFamily = "Arial",
            FontSize = 14,
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Hello ", IsBold = true },
                new() { Text = "World", TextColorHex = "#DC2626", Script = TextScriptMode.Superscript }
            }
        };

        element.SynchronizePlainTextFromSpans();
        Assert.Equal("Hello World", element.Text);

        var clone = (PdfTextElement)element.Clone();
        Assert.NotSame(element, clone);
        Assert.NotNull(clone.Spans);
        Assert.Equal(2, clone.Spans.Count);
        Assert.NotSame(element.Spans[0], clone.Spans[0]);
        Assert.Equal("Hello ", clone.Spans[0].Text);
        Assert.True(clone.Spans[0].IsBold);
        Assert.Equal("World", clone.Spans[1].Text);
        Assert.Equal("#DC2626", clone.Spans[1].TextColorHex);
        Assert.Equal(TextScriptMode.Superscript, clone.Spans[1].Script);
    }

    [Fact]
    public void PdfTextElement_BackwardCompatibility_NullSpansWorksAsPlainText()
    {
        var element = new PdfTextElement
        {
            Text = "Legacy plain text",
            FontFamily = "Segoe UI",
            FontSize = 12,
            Spans = null
        };

        Assert.Equal("Legacy plain text", element.GetEffectivePlainText());
        element.SynchronizePlainTextFromSpans();
        Assert.Equal("Legacy plain text", element.Text);

        var clone = (PdfTextElement)element.Clone();
        Assert.Null(clone.Spans);
        Assert.Equal("Legacy plain text", clone.Text);
    }

    [Fact]
    public void RichTextHelper_ParsesMarkdownTokensAccurately()
    {
        string input = "Start **Bold Text** then *Italic Text* and ~~Strikethrough~~ plus <u>Underline</u> end.";
        var spans = RichTextHelper.ParseMarkdownToSpans(input);

        Assert.NotNull(spans);
        Assert.True(spans.Count >= 5);

        Assert.Equal("Start ", spans[0].Text);
        Assert.Equal("Bold Text", spans[1].Text);
        Assert.True(spans[1].IsBold);

        Assert.Equal(" then ", spans[2].Text);
        Assert.Equal("Italic Text", spans[3].Text);
        Assert.True(spans[3].IsItalic);

        var strikeSpan = spans.FirstOrDefault(s => s.Text == "Strikethrough");
        Assert.NotNull(strikeSpan);
        Assert.True(strikeSpan.IsStrikethrough);

        var underlineSpan = spans.FirstOrDefault(s => s.Text == "Underline");
        Assert.NotNull(underlineSpan);
        Assert.True(underlineSpan.IsUnderline);
    }

    [Fact]
    public void RichTextHelper_ParsesSubscriptAndSuperscript()
    {
        string input = "Formula: H~2~O and Exponent: x^2^";
        var spans = RichTextHelper.ParseMarkdownToSpans(input);

        Assert.NotNull(spans);
        var subSpan = spans.FirstOrDefault(s => s.Text == "2" && s.Script == TextScriptMode.Subscript);
        Assert.NotNull(subSpan);

        var superSpan = spans.FirstOrDefault(s => s.Text == "2" && s.Script == TextScriptMode.Superscript);
        Assert.NotNull(superSpan);
    }

    [Fact]
    public void RichTextHelper_ParsesColorTagsAndHyperlinks()
    {
        string input = "Here is <color=#0F6CBD>Blue Text</color> and [Click Here](https://example.com)";
        var spans = RichTextHelper.ParseMarkdownToSpans(input);

        Assert.NotNull(spans);
        var blueSpan = spans.FirstOrDefault(s => s.Text == "Blue Text");
        Assert.NotNull(blueSpan);
        Assert.Equal("#0F6CBD", blueSpan.TextColorHex);

        var linkSpan = spans.FirstOrDefault(s => s.Text == "Click Here");
        Assert.NotNull(linkSpan);
        Assert.Equal("https://example.com", linkSpan.LinkUrl);
        Assert.True(linkSpan.IsUnderline);
    }

    [Fact]
    public void RichTextHelper_SpansToMarkdown_ReconstructsMarkupLosslessly()
    {
        var spans = new List<PdfTextSpan>
        {
            new() { Text = "Normal " },
            new() { Text = "Bold", IsBold = true },
            new() { Text = " and " },
            new() { Text = "Italic", IsItalic = true },
            new() { Text = " and " },
            new() { Text = "Super", Script = TextScriptMode.Superscript }
        };

        string markdown = RichTextHelper.SpansToMarkdown(spans);
        Assert.Contains("**Bold**", markdown);
        Assert.Contains("*Italic*", markdown);
        Assert.Contains("^Super^", markdown);
    }

    [Fact]
    public void TextElementViewModel_SetMarkdownText_UpdatesSpansAndPlainText()
    {
        var vm = new TextElementViewModel();
        vm.SetMarkdownText("Total is **$100.00** <color=#16A34A>PAID</color>");

        Assert.NotNull(vm.Spans);
        Assert.True(vm.Spans.Count >= 3);
        Assert.Equal("Total is $100.00 PAID", vm.Text);

        var model = (PdfTextElement)vm.ToModel();
        Assert.NotNull(model.Spans);
        Assert.Equal(vm.Spans.Count, model.Spans.Count);

        var reloadedVm = new TextElementViewModel(model);
        Assert.NotNull(reloadedVm.Spans);
        Assert.Equal(vm.Spans.Count, reloadedVm.Spans.Count);
        Assert.Equal("Total is $100.00 PAID", reloadedVm.Text);
    }

    [Fact]
    public void DataMergeEngine_HydratesPlaceholdersWithinSpans()
    {
        var doc = new PdfDocumentModel();
        var page = new PdfPageModel();
        doc.Pages.Add(page);

        var textElement = new PdfTextElement
        {
            X = 50,
            Y = 50,
            Width = 300,
            Height = 40,
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Dear " },
                new() { Text = "{{CustomerName}}", IsBold = true, TextColorHex = "#0F6CBD" },
                new() { Text = ", your balance is " },
                new() { Text = "{{Amount}}", IsBold = true }
            }
        };
        textElement.SynchronizePlainTextFromSpans();
        page.Elements.Add(textElement);

        var engine = new DataMergeEngine();
        var record = new Dictionary<string, string>
        {
            { "CustomerName", "Jane Doe" },
            { "Amount", "$450.00" }
        };

        var hydratedDoc = engine.HydrateDocument(doc, record);
        var hydratedText = hydratedDoc.Pages[0].Elements.OfType<PdfTextElement>().First();

        Assert.NotNull(hydratedText.Spans);
        Assert.Equal(4, hydratedText.Spans.Count);
        Assert.Equal("Jane Doe", hydratedText.Spans[1].Text);
        Assert.True(hydratedText.Spans[1].IsBold);
        Assert.Equal("#0F6CBD", hydratedText.Spans[1].TextColorHex);
        Assert.Equal("$450.00", hydratedText.Spans[3].Text);
        Assert.Equal("Dear Jane Doe, your balance is $450.00", hydratedText.Text);
    }

    [Fact]
    public void PdfExportService_ExportsMultiSpanTextElement_GeneratesValidPdfBytes()
    {
        var doc = new PdfDocumentModel();
        var page = new PdfPageModel { Width = 595, Height = 842 };
        doc.Pages.Add(page);

        var textElement = new PdfTextElement
        {
            X = 50,
            Y = 50,
            Width = 400,
            Height = 60,
            FontFamily = "Arial",
            FontSize = 14,
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Invoice Number: " },
                new() { Text = "INV-2026-001", IsBold = true, TextColorHex = "#0F6CBD" },
                new() { Text = " (Status: " },
                new() { Text = "APPROVED", IsBold = true, TextColorHex = "#16A34A" },
                new() { Text = ", Formula: H" },
                new() { Text = "2", Script = TextScriptMode.Subscript },
                new() { Text = "O)" }
            }
        };
        textElement.SynchronizePlainTextFromSpans();
        page.Elements.Add(textElement);

        var exporter = new PdfExportService();
        byte[] pdfBytes = exporter.GeneratePdfBytes(doc);

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 500);

        // Verify standard PDF header magic bytes %PDF-
        string header = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void TextLayoutEngine_GenerateSvgMarkup_EmitsTspanElementsForSpans()
    {
        var element = new PdfTextElement
        {
            X = 10,
            Y = 10,
            Width = 250,
            Height = 50,
            FontFamily = "Segoe UI",
            FontSize = 14,
            Spans = new List<PdfTextSpan>
            {
                new() { Text = "Prefix ", IsBold = false },
                new() { Text = "BoldRed", IsBold = true, TextColorHex = "#DC2626" },
                new() { Text = "2", Script = TextScriptMode.Superscript }
            }
        };
        element.SynchronizePlainTextFromSpans();

        string svg = TextLayoutEngine.GenerateSvgMarkup(element);
        Assert.NotNull(svg);
        Assert.Contains("<tspan", svg);
        Assert.Contains("BoldRed", svg);
        Assert.Contains("fill=\"#DC2626\"", svg);
        Assert.Contains("baseline-shift=\"super\"", svg);
    }

    [Fact]
    public void TemplateService_CreatesRichTextShowcaseTemplate_ValidModelAndSpans()
    {
        var templateService = new TemplateService();
        var doc = templateService.CreateRichTextShowcaseTemplate();

        Assert.NotNull(doc);
        Assert.Single(doc.Pages);
        var page = doc.Pages[0];

        var textElements = page.Elements.OfType<PdfTextElement>().ToList();
        Assert.NotEmpty(textElements);

        // Verify that multi-span elements exist with rich formatting
        var multiSpanElements = textElements.Where(t => t.Spans != null && t.Spans.Count > 1).ToList();
        Assert.True(multiSpanElements.Count >= 5);

        // Verify subscripts and superscripts in the specimen
        bool hasSubscript = multiSpanElements.Any(t => t.Spans!.Any(s => s.Script == TextScriptMode.Subscript));
        bool hasSuperscript = multiSpanElements.Any(t => t.Spans!.Any(s => s.Script == TextScriptMode.Superscript));
        bool hasColors = multiSpanElements.Any(t => t.Spans!.Any(s => !string.IsNullOrEmpty(s.TextColorHex)));
        bool hasLinks = multiSpanElements.Any(t => t.Spans!.Any(s => !string.IsNullOrEmpty(s.LinkUrl)));

        Assert.True(hasSubscript);
        Assert.True(hasSuperscript);
        Assert.True(hasColors);
        Assert.True(hasLinks);

        // Verify QuestPDF export
        var exporter = new PdfExportService();
        byte[] pdfBytes = exporter.GeneratePdfBytes(doc);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);
    }

    [Fact]
    public void TemplateService_CreatesOcrAnalysisReportTemplate_ValidModelAndSpans()
    {
        var templateService = new TemplateService();
        var doc = templateService.CreateOcrAnalysisReportTemplate();

        Assert.NotNull(doc);
        Assert.Single(doc.Pages);
        var page = doc.Pages[0];

        // Verify table, QR code, and text elements
        var tables = page.Elements.OfType<PdfTableElement>().ToList();
        var qrCodes = page.Elements.OfType<PdfQrCodeElement>().ToList();
        var textElements = page.Elements.OfType<PdfTextElement>().ToList();

        Assert.Single(tables);
        Assert.Single(qrCodes);
        Assert.NotEmpty(textElements);

        // Verify multi-span OCR annotations
        var multiSpanElements = textElements.Where(t => t.Spans != null && t.Spans.Count > 1).ToList();
        Assert.True(multiSpanElements.Count >= 4);

        // Verify QuestPDF export
        var exporter = new PdfExportService();
        byte[] pdfBytes = exporter.GeneratePdfBytes(doc);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);
    }
}
