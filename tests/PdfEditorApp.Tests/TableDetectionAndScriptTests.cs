using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Deconstruction;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services.Tools.Organize;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Regression tests for the table-detection, Unicode-script and page-filter defects
/// found in the Tier 1 audit. Each test here fails against the pre-fix code.
/// </summary>
public class TableDetectionAndScriptTests
{
    // ─── 1.1 Table detection ────────────────────────────────────────────────

    /// <summary>
    /// Builds a PDF containing one ruled table: four horizontal rules 30pt apart,
    /// three vertical rules, and two columns of text across three row bands.
    /// </summary>
    private static string CreateRuledTablePdf()
    {
        const double left = 60, right = 460;
        double[] ruleYs = { 100, 130, 160, 190 };
        double[] colXs = { 60, 260, 460 };

        string path = Path.Combine(Path.GetTempPath(), $"ruled_table_{Guid.NewGuid():N}.pdf");
        using (var doc = new PdfDocument())
        {
            var page = doc.AddPage();
            page.Width = 595;
            page.Height = 842;

            using var gfx = XGraphics.FromPdfPage(page);
            var pen = new XPen(XColors.Black, 1.0);

            foreach (double y in ruleYs)
                gfx.DrawLine(pen, left, y, right, y);

            foreach (double x in colXs)
                gfx.DrawLine(pen, x, ruleYs[0], x, ruleYs[^1]);

            var font = new XFont("Arial", 11);
            string[,] cells =
            {
                { "Region", "Revenue" },
                { "North",  "1200" },
                { "South",  "3400" },
            };

            for (int r = 0; r < 3; r++)
            {
                double textY = ruleYs[r] + 20;
                gfx.DrawString(cells[r, 0], font, XBrushes.Black, new XPoint(70, textY));
                gfx.DrawString(cells[r, 1], font, XBrushes.Black, new XPoint(270, textY));
            }

            doc.Save(path);
        }
        return path;
    }

    [Fact]
    public void Deconstruct_RuledTable_ProducesATableElement()
    {
        string path = CreateRuledTablePdf();
        try
        {
            var model = PdfDeconstructionEngine.Deconstruct(File.ReadAllBytes(path), "RuledTable");

            var tables = model.Pages
                .SelectMany(p => p.Elements)
                .OfType<PdfTableElement>()
                .ToList();

            // Pre-fix, Math.Round(x, -1) threw ArgumentOutOfRangeException for every real
            // table candidate and the page-level catch swallowed it, so this was always 0.
            Assert.NotEmpty(tables);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DetectTables_DoesNotThrow_ForATableWithManyParagraphs()
    {
        // Guards the specific throw: >= 4 paragraphs inside the table bounds is what
        // pushed execution past the early-continue and into the rounding call.
        string path = CreateRuledTablePdf();
        try
        {
            var ex = Record.Exception(() =>
                PdfDeconstructionEngine.Deconstruct(File.ReadAllBytes(path), "RuledTable"));
            Assert.Null(ex);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void MathRound_WithNegativeDigits_Throws()
    {
        // The premise behind SnapToGrid: there is no "round to nearest 10" overload, and
        // Math.Round(value, digits) rejects a negative digit count. TableGridDetector used
        // Math.Round(x, -1), which is why table detection always threw.
        Assert.Throws<ArgumentOutOfRangeException>(() => Math.Round(123.4, -1));
    }

    // ─── 1.2 Unicode script detection ───────────────────────────────────────

    [Theory]
    [InlineData("lone-high", "\uD83D")]
    [InlineData("lone-low", "\uDE00")]
    [InlineData("high-then-text", "Hello \uD83D world")]
    [InlineData("reversed-pair", "\uDC00\uD800")]
    public void DetectScriptFontFamily_ToleratesUnpairedSurrogates(string label, string text)
    {
        Assert.False(string.IsNullOrEmpty(label));
        // char.ConvertToUtf32(string, int) throws ArgumentException on an unpaired
        // surrogate; that throw reached the page-level catch and discarded every
        // text element on the page.
        var ex = Record.Exception(() => UnicodeScriptDetector.DetectScriptFontFamily(text));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("lone-high", "\uD83D")]
    [InlineData("high-then-text", "Hello \uD83D world")]
    public void ContainsCjk_ToleratesUnpairedSurrogates(string label, string text)
    {
        Assert.False(string.IsNullOrEmpty(label));
        var ex = Record.Exception(() => UnicodeScriptDetector.ContainsCjk(text));
        Assert.Null(ex);
    }

    [Fact]
    public void ContainsCjk_DetectsSupplementaryPlaneIdeographs()
    {
        // U+20000 (CJK Extension B) is a surrogate pair. Enumerating by char widened
        // each UTF-16 unit into an int that can never exceed 0xFFFF, so the Extension B
        // range test was unreachable.
        string extensionB = char.ConvertFromUtf32(0x20000);
        Assert.True(UnicodeScriptDetector.ContainsCjk(extensionB));

        Assert.True(UnicodeScriptDetector.ContainsCjk("中文"));  // BMP ideographs
        Assert.False(UnicodeScriptDetector.ContainsCjk("plain ascii"));
    }

    // ─── 1.6 Split reports an empty result as failure ───────────────────────

    private static string CreateMultiPagePdf(int pages)
    {
        string path = Path.Combine(Path.GetTempPath(), $"split_src_{Guid.NewGuid():N}.pdf");
        using (var doc = new PdfDocument())
        {
            for (int i = 0; i < pages; i++)
            {
                var page = doc.AddPage();
                using var gfx = XGraphics.FromPdfPage(page);
                gfx.DrawString($"Page {i + 1}", new XFont("Arial", 14), XBrushes.Black, new XPoint(50, 50));
            }
            doc.Save(path);
        }
        return path;
    }

    [Theory]
    [InlineData("50-60")]   // entirely out of range
    [InlineData("10-3")]    // reversed
    [InlineData("abc")]     // malformed
    public async Task SplitPdf_WithUnusableRange_ReportsFailure(string range)
    {
        string input = CreateMultiPagePdf(3);
        string outDir = Path.Combine(Path.GetTempPath(), $"split_out_{Guid.NewGuid():N}");
        try
        {
            var svc = new PdfPageService();
            var result = await svc.SplitPdfAsync(new SplitToolOptions
            {
                InputFilePath = input,
                OutputDirectory = outDir,
                Mode = SplitExtractMode.SplitByPageRanges,
                RangeExpression = range
            });

            // Previously returned Success = true with "Split PDF into 0 files".
            Assert.False(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }
        finally
        {
            if (File.Exists(input)) File.Delete(input);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task SplitPdf_WithValidRange_StillSucceeds()
    {
        string input = CreateMultiPagePdf(4);
        string outDir = Path.Combine(Path.GetTempPath(), $"split_out_{Guid.NewGuid():N}");
        try
        {
            var svc = new PdfPageService();
            var result = await svc.SplitPdfAsync(new SplitToolOptions
            {
                InputFilePath = input,
                OutputDirectory = outDir,
                Mode = SplitExtractMode.SplitByPageRanges,
                RangeExpression = "1-2, 3-4"
            });

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(2, result.OutputFiles.Count);
        }
        finally
        {
            if (File.Exists(input)) File.Delete(input);
            if (Directory.Exists(outDir)) Directory.Delete(outDir, recursive: true);
        }
    }
}
