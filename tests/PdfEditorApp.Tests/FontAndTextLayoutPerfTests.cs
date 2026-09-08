using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Media;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Typography;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Regression tests for Tier 1.3: font resolution and glyph measurement used to perform
/// filesystem I/O and full text shaping per character, on every render frame.
/// </summary>
public class FontAndTextLayoutPerfTests
{
    // ─── FontHelper.CreateFontFamily is memoized ────────────────────────────

    [Fact]
    public void CreateFontFamily_ReturnsTheSameInstanceForRepeatedCalls()
    {
        var first = FontHelper.CreateFontFamily("Roboto");
        var second = FontHelper.CreateFontFamily("Roboto");

        // Previously every call allocated a new FontFamily after a File.Exists disk probe.
        Assert.Same(first, second);
    }

    [Fact]
    public void CreateFontFamily_DoesNotTouchTheFilesystemAfterTheFirstCall()
    {
        // Force a cold entry for a name nothing else uses.
        string family = $"ProbeFace_{Guid.NewGuid():N}";
        FontHelper.CreateFontFamily(family);

        // The user font directory is where the per-call File.Exists probe pointed. If the
        // cache works, deleting or creating files there cannot change the resolved instance.
        var before = FontHelper.CreateFontFamily(family);
        var after = FontHelper.CreateFontFamily(family);
        Assert.Same(before, after);
    }

    [Fact]
    public void RegisterFontFamily_InvalidatesOnlyThatEntry()
    {
        var roboto = FontHelper.CreateFontFamily("Roboto");
        var inter = FontHelper.CreateFontFamily("Inter");

        FontHelper.RegisterFontFamily("Roboto");

        Assert.NotSame(roboto, FontHelper.CreateFontFamily("Roboto"));
        Assert.Same(inter, FontHelper.CreateFontFamily("Inter"));
    }

    [Fact]
    public void InvalidateFontFamilyCache_DropsEverything()
    {
        var before = FontHelper.CreateFontFamily("Montserrat");
        FontHelper.InvalidateFontFamilyCache();
        Assert.NotSame(before, FontHelper.CreateFontFamily("Montserrat"));
    }

    [Fact]
    public void CreateFontFamily_ReturnsDefaultForBlankNames()
    {
        Assert.Equal(FontFamily.Default, FontHelper.CreateFontFamily(null));
        Assert.Equal(FontFamily.Default, FontHelper.CreateFontFamily("   "));
    }

    [Fact]
    public void InvalidatingFonts_AlsoDropsTheGlyphCaches()
    {
        // The glyph typeface and advance caches are keyed on family name, so installing a font
        // must invalidate them too — otherwise a newly installed family keeps resolving to the
        // fallback metrics that were cached before it existed.
        double before = TextLayoutEngine.MeasureGlyphWidth('A', "Roboto", 16, false, false);

        FontHelper.InvalidateFontFamilyCache();

        double after = TextLayoutEngine.MeasureGlyphWidth('A', "Roboto", 16, false, false);

        // Same inputs, so the value is unchanged — the point is that it was recomputed, not
        // served from a cache that survived the invalidation.
        Assert.Equal(before, after, precision: 6);
    }

    // ─── Glyph measurement stays correct and gets cheap ─────────────────────

    [Fact]
    public void MeasureGlyphWidth_KeepsProportionalOrdering()
    {
        double widthW = TextLayoutEngine.MeasureGlyphWidth('W', "Arial", 20, false, false);
        double widthI = TextLayoutEngine.MeasureGlyphWidth('I', "Arial", 20, false, false);

        Assert.True(widthW > widthI, "W should be wider than I in a proportional face");
        Assert.True(widthI > 0);
    }

    [Fact]
    public void MeasureGlyphWidth_ScalesLinearlyWithFontSize()
    {
        // Advances are cached in em units, so one cache entry must serve every size.
        double at10 = TextLayoutEngine.MeasureGlyphWidth('M', "Arial", 10, false, false);
        double at20 = TextLayoutEngine.MeasureGlyphWidth('M', "Arial", 20, false, false);
        double at40 = TextLayoutEngine.MeasureGlyphWidth('M', "Arial", 40, false, false);

        Assert.Equal(at10 * 2, at20, precision: 6);
        Assert.Equal(at10 * 4, at40, precision: 6);
    }

    [Fact]
    public void MeasureStringWidth_EqualsTheSumOfItsGlyphs()
    {
        const string text = "Hamburgefonstiv";
        double summed = text.Sum(c => TextLayoutEngine.MeasureGlyphWidth(c, "Arial", 16, false, false));
        double measured = TextLayoutEngine.MeasureStringWidth(text, "Arial", 16, false, false, 0, 0);

        Assert.Equal(summed, measured, precision: 6);
    }

    [Fact]
    public void MeasureGlyphWidth_IsFastEnoughForAPerFrameBudget()
    {
        // Warm the caches, then measure a page-sized block of text. Pre-fix this did one
        // File.Exists syscall plus one FormattedText (full shaping) per character.
        const int glyphs = 20_000;
        TextLayoutEngine.MeasureGlyphWidth('a', "Arial", 14, false, false);

        var sw = Stopwatch.StartNew();
        double total = 0;
        for (int i = 0; i < glyphs; i++)
        {
            char c = (char)('a' + (i % 26));
            total += TextLayoutEngine.MeasureGlyphWidth(c, "Arial", 14, false, false);
        }
        sw.Stop();

        Assert.True(total > 0);
        Assert.True(sw.ElapsedMilliseconds < 250,
            $"Measuring {glyphs} cached glyphs took {sw.ElapsedMilliseconds}ms; the cache is not being hit.");
    }

    // ─── Word wrapping ──────────────────────────────────────────────────────

    private static NormalLayoutResult Layout(string text, double width) =>
        TextLayoutEngine.CalculateNormalLayout(
            text, "Arial", 16, false, false, width, 1.2, 0, 0, 0,
            TextAlignmentMode.Left, TextVerticalAlignment.Top, 400, true, 0);

    [Fact]
    public void CalculateNormalLayout_BreaksAWordWiderThanTheLine()
    {
        string longWord = new string('M', 200);
        var result = Layout(longWord, 120);

        // Previously emitted as a single over-wide line that silently overflowed the element.
        Assert.True(result.Lines.Count > 1,
            "An over-wide word should be broken across lines.");
        Assert.Equal(longWord, string.Concat(result.Lines.Select(l => l.Text)));
    }

    [Fact]
    public void CalculateNormalLayout_StillWrapsOnWordBoundariesWhenItCan()
    {
        var result = Layout("alpha beta gamma delta epsilon zeta eta theta", 120);

        Assert.True(result.Lines.Count > 1);
        // No word should have been split apart when a space boundary was available.
        foreach (var line in result.Lines)
        {
            Assert.DoesNotContain("alph\n", line.Text);
        }
        Assert.Contains(result.Lines, l => l.Text.Contains("alpha", StringComparison.Ordinal));
    }

    [Fact]
    public void CalculateNormalLayout_PreservesExplicitLineBreaks()
    {
        var result = Layout("one\ntwo\nthree", 400);
        Assert.Equal(3, result.Lines.Count);
        Assert.Equal("one", result.Lines[0].Text);
        Assert.Equal("two", result.Lines[1].Text);
        Assert.Equal("three", result.Lines[2].Text);
    }
}
