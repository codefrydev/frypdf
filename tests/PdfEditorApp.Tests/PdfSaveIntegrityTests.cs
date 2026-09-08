using System;
using System.IO;
using System.Text;
using PdfEditorApp.Core.Utils;
using PdfEditorApp.Services.Tools.Core;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Regression tests for the Tier 1.5 save-path defects: the /Producer patch wrote one byte
/// past the span it replaced, and the patch ran in place over the file that had just been
/// reported as saved.
/// </summary>
[Collection("AppLogService")]
public class PdfSaveIntegrityTests
{
    /// <summary>
    /// Builds a minimal byte buffer containing a /Producer entry with the given value.
    /// The sentinel after the closing paren is what the off-by-one used to clobber.
    /// </summary>
    private static byte[] BuildProducerBuffer(string existingProducer, out int sentinelIndex)
    {
        string prefix = "%PDF-1.4\n<< /Producer (";
        string suffix = ") /Title (Keep Me) >>\n";
        string text = prefix + existingProducer + suffix;

        // The byte right after ')' — index of ')' + 1.
        sentinelIndex = prefix.Length + existingProducer.Length + 1;
        return Encoding.ASCII.GetBytes(text);
    }

    [Fact]
    public void StringPadRight_NeverTruncates()
    {
        // The premise behind the fix: PadRight pads up to a width, it does not cut down to
        // one. "%".PadRight(0) is "%", not "", so an exact-length replacement emitted one
        // byte too many.
        Assert.Equal("%", "%".PadRight(0, ' '));
        Assert.Equal("%  ", "%".PadRight(3, ' '));
    }

    [Fact]
    public void PatchProducerInBytes_WithAnEqualLengthReplacement_DoesNotOverrunTheSpan()
    {
        // "codefrydev.in" is 13 chars, so an existing 13-char producer makes the
        // replacement exactly as long as the span. That is the case where
        // "%".PadRight(0) returned "%" and produced a one-byte overrun.
        const string producer = "codefrydev.in";
        var bytes = BuildProducerBuffer("aaaaaaaaaaaaa", out int sentinelIndex);

        Assert.Equal(13, producer.Length);
        byte sentinelBefore = bytes[sentinelIndex];

        var result = PdfFileHelper.PatchProducerInBytes(bytes, producer);

        Assert.Equal(sentinelBefore, result[sentinelIndex]);
        Assert.Contains($"({producer})", Encoding.ASCII.GetString(result), StringComparison.Ordinal);
        Assert.Contains("/Title (Keep Me)", Encoding.ASCII.GetString(result), StringComparison.Ordinal);
    }

    [Fact]
    public void PatchProducerInBytes_WithAShorterReplacement_PadsExactly()
    {
        const string producer = "codefrydev.in";
        var bytes = BuildProducerBuffer("a-much-longer-original-producer", out int sentinelIndex);
        byte sentinelBefore = bytes[sentinelIndex];
        int lengthBefore = bytes.Length;

        var result = PdfFileHelper.PatchProducerInBytes(bytes, producer);

        Assert.Equal(lengthBefore, result.Length);
        Assert.Equal(sentinelBefore, result[sentinelIndex]);
        Assert.Contains("/Title (Keep Me)", Encoding.ASCII.GetString(result), StringComparison.Ordinal);
    }

    [Fact]
    public void PdfDocumentSanitizer_PatchProducer_HasTheSameBoundsBehaviour()
    {
        // The two implementations are verbatim copies; the fix has to hold in both.
        const string producer = "codefrydev.in";
        var bytes = BuildProducerBuffer("aaaaaaaaaaaaa", out int sentinelIndex);
        byte sentinelBefore = bytes[sentinelIndex];

        var result = PdfDocumentSanitizer.PatchProducerInBytes(bytes, producer);

        Assert.Equal(sentinelBefore, result[sentinelIndex]);
        Assert.Contains("/Title (Keep Me)", Encoding.ASCII.GetString(result), StringComparison.Ordinal);
    }

    // ─── Atomic save ────────────────────────────────────────────────────────

    [Fact]
    public void SaveDocumentWithFryPdfMetadata_ProducesAReadablePdf()
    {
        string path = Path.Combine(Path.GetTempPath(), $"save_{Guid.NewGuid():N}.pdf");
        try
        {
            using (var doc = new PdfDocument())
            {
                var page = doc.AddPage();
                using var gfx = XGraphics.FromPdfPage(page);
                gfx.DrawString("Hello", new XFont("Arial", 12), XBrushes.Black, new XPoint(40, 40));
                PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, path);
            }

            Assert.True(File.Exists(path));
            using var reopened = PdfFileHelper.OpenDocumentSafely(path);
            Assert.Equal(1, reopened.PageCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void SaveDocumentWithFryPdfMetadata_LeavesNoTempFileBehind()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"savedir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "out.pdf");
        try
        {
            using (var doc = new PdfDocument())
            {
                doc.AddPage();
                PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, path);
            }

            Assert.Single(Directory.GetFiles(dir));
            Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SaveDocumentWithFryPdfMetadata_OverwritesAnExistingFileInPlace()
    {
        string path = Path.Combine(Path.GetTempPath(), $"overwrite_{Guid.NewGuid():N}.pdf");
        try
        {
            using (var first = new PdfDocument())
            {
                first.AddPage();
                PdfFileHelper.SaveDocumentWithFryPdfMetadata(first, path);
            }

            // File.Replace is used on the overwrite path; make sure it actually replaces.
            using (var second = new PdfDocument())
            {
                second.AddPage();
                second.AddPage();
                second.AddPage();
                PdfFileHelper.SaveDocumentWithFryPdfMetadata(second, path);
            }

            using var reopened = PdfFileHelper.OpenDocumentSafely(path);
            Assert.Equal(3, reopened.PageCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void SaveDocumentWithFryPdfMetadata_CreatesMissingDirectories()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"nested_{Guid.NewGuid():N}", "a", "b");
        string path = Path.Combine(dir, "out.pdf");
        try
        {
            using var doc = new PdfDocument();
            doc.AddPage();
            PdfFileHelper.SaveDocumentWithFryPdfMetadata(doc, path);
            Assert.True(File.Exists(path));
        }
        finally
        {
            var root = Path.GetDirectoryName(Path.GetDirectoryName(dir));
            if (root != null && Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
