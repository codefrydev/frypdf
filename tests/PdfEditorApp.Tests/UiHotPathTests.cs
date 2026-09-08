using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Media;
using PdfEditorApp.Converters;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Typography;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Regression tests for the Tier 4 UI hot-path and allocation-churn fixes.
/// </summary>
public class UiHotPathTests
{
    // ─── 4.3 Converters must not allocate per evaluation ────────────────────

    [Fact]
    public void HexToBrushConverter_ReturnsTheSameBrushInstanceForTheSameColour()
    {
        var first = HexToBrushConverter.Instance.Convert("#0F6CBD", typeof(IBrush), null, null!);
        var second = HexToBrushConverter.Instance.Convert("#0F6CBD", typeof(IBrush), null, null!);

        // Used 37 times in DocumentCanvasView.axaml inside per-element templates; a new
        // instance per call also invalidated Avalonia's cached render brush every time.
        Assert.Same(first, second);
    }

    [Fact]
    public void HexToBrushConverter_StillDistinguishesColours()
    {
        var blue = HexToBrushConverter.Instance.Convert("#0F6CBD", typeof(IBrush), null, null!);
        var grey = HexToBrushConverter.Instance.Convert("#E2E8F0", typeof(IBrush), null, null!);

        Assert.NotSame(blue, grey);
        Assert.Equal(Color.Parse("#0F6CBD"), ((ISolidColorBrush)blue!).Color);
        Assert.Equal(Color.Parse("#E2E8F0"), ((ISolidColorBrush)grey!).Color);
    }

    [Fact]
    public void HexToBrushConverter_FallsBackForUnparseableInput()
    {
        Assert.Same(Brushes.Transparent,
            HexToBrushConverter.Instance.Convert("not a colour", typeof(IBrush), null, null!));
        Assert.Same(Brushes.Transparent,
            HexToBrushConverter.Instance.Convert(null, typeof(IBrush), null, null!));
    }

    [Fact]
    public void BooleanToBrushConverter_CachesItsParsedParameterPair()
    {
        const string param = "#0F6CBD|#CBD5E1";

        var trueFirst = BooleanToBrushConverter.Instance.Convert(true, typeof(IBrush), param, null!);
        var trueSecond = BooleanToBrushConverter.Instance.Convert(true, typeof(IBrush), param, null!);
        var falseBrush = BooleanToBrushConverter.Instance.Convert(false, typeof(IBrush), param, null!);

        Assert.Same(trueFirst, trueSecond);
        Assert.NotSame(trueFirst, falseBrush);
        Assert.Equal(Color.Parse("#0F6CBD"), ((ISolidColorBrush)trueFirst!).Color);
        Assert.Equal(Color.Parse("#CBD5E1"), ((ISolidColorBrush)falseBrush!).Color);
    }

    [Fact]
    public void EnumToDescriptionConverter_MemoizesAndStillSplitsCamelCase()
    {
        var first = EnumToDescriptionConverter.Instance.Convert(
            TextAlignmentMode.Justify, typeof(string), null, null!);
        var second = EnumToDescriptionConverter.Instance.Convert(
            TextAlignmentMode.Justify, typeof(string), null, null!);

        Assert.Equal(first, second);

        // Reflection + an uncompiled Regex.Replace used to run on every binding evaluation.
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 50_000; i++)
        {
            EnumToDescriptionConverter.Instance.Convert(TextAlignmentMode.Justify, typeof(string), null, null!);
        }
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 250,
            $"50k cached conversions took {sw.ElapsedMilliseconds}ms; the memo is not being hit.");
    }

    // ─── 4.4 Selection must not churn when nothing changed ──────────────────

    private static PageViewModel CreatePageWithElements(int count)
    {
        var page = new PageViewModel();
        for (int i = 0; i < count; i++)
        {
            page.Elements.Add(new TextElementViewModel { Text = $"e{i}", X = i * 10, Y = 0, Width = 8, Height = 8 });
        }
        return page;
    }

    [Fact]
    public void SelectElements_IsANoOpWhenTheSelectionIsUnchanged()
    {
        var page = CreatePageWithElements(5);
        var target = page.Elements.Take(2).ToList();

        page.SelectElements(target);

        int notifications = 0;
        foreach (var el in page.Elements)
        {
            el.PropertyChanged += (_, _) => notifications++;
        }

        // Marquee dragging calls this ~60 times a second with the same hit set.
        page.SelectElements(target);

        Assert.Equal(0, notifications);
        Assert.Equal(2, page.SelectedElements.Count);
    }

    [Fact]
    public void SelectElements_StillUpdatesWhenTheSelectionChanges()
    {
        var page = CreatePageWithElements(5);

        page.SelectElements(page.Elements.Take(2).ToList());
        Assert.Equal(2, page.SelectedElements.Count);

        page.SelectElements(page.Elements.Take(4).ToList());
        Assert.Equal(4, page.SelectedElements.Count);
        Assert.True(page.HasMultiSelection);

        page.SelectElements(Array.Empty<ElementViewModelBase>());
        Assert.Empty(page.SelectedElements);
        Assert.All(page.Elements, e => Assert.False(e.IsSelected));
    }

    [Fact]
    public void UpdateSelectionBoundingBox_MatchesTheSelectionExtent()
    {
        var page = CreatePageWithElements(4);
        page.SelectElements(page.Elements.ToList());

        var box = page.SelectionBoundingBox;

        Assert.Equal(0, box.X);
        Assert.Equal(0, box.Y);
        // Elements sit at x = 0,10,20,30 with width 8, so the extent runs 0..38.
        Assert.Equal(38, box.Width);
        Assert.Equal(8, box.Height);
    }

    // ─── 4.4 Memoized log level must not go stale ───────────────────────────

    private static AppLogEntry Entry(AppLogLevel level, string message = "m")
        => new() { Level = level, Category = "Test", Message = message };

    [Fact]
    public void LogGroup_WorstLevel_TracksTheMostSevereEntry()
    {
        var group = new LogGroupViewModel("Test");
        Assert.Equal(AppLogLevel.Debug, group.WorstLevel);

        group.Add(Entry(AppLogLevel.Info));
        Assert.Equal(AppLogLevel.Info, group.WorstLevel);

        group.Add(Entry(AppLogLevel.Warning));
        Assert.Equal(AppLogLevel.Warning, group.WorstLevel);

        // The memo must be invalidated by Add, not just computed once.
        group.Add(Entry(AppLogLevel.Error));
        Assert.Equal(AppLogLevel.Error, group.WorstLevel);
    }

    [Fact]
    public void LogGroup_WorstLevel_RecomputesAfterClear()
    {
        var group = new LogGroupViewModel("Test");
        group.Add(Entry(AppLogLevel.Error));
        Assert.Equal(AppLogLevel.Error, group.WorstLevel);

        group.Clear();
        Assert.Equal(AppLogLevel.Debug, group.WorstLevel);
    }

    [Fact]
    public void LogGroup_WorstLevel_RecomputesAfterTrim()
    {
        var group = new LogGroupViewModel("Test");
        group.Add(Entry(AppLogLevel.Error, "old error"));
        group.Add(Entry(AppLogLevel.Info, "newer"));
        group.Add(Entry(AppLogLevel.Info, "newest"));
        Assert.Equal(AppLogLevel.Error, group.WorstLevel);

        // Trimming drops the oldest entries, so the error should fall out.
        group.TrimTo(2);
        Assert.Equal(AppLogLevel.Info, group.WorstLevel);
    }

    [Fact]
    public void LogGroup_ContainsMessage_MatchesCaseInsensitively()
    {
        var group = new LogGroupViewModel("Test");
        group.Add(Entry(AppLogLevel.Info, "Plugin load FAILED"));

        Assert.True(group.ContainsMessage("failed"));
        Assert.True(group.ContainsMessage("Plugin"));
        Assert.False(group.ContainsMessage("nothing here"));
    }

    // ─── 4.6 Span merging must not be quadratic ─────────────────────────────

    [Fact]
    public void NormalizeSpans_MergesAdjacentMatchingSpans()
    {
        var spans = new List<PdfTextSpan>
        {
            new() { Text = "Hello ", FontFamily = "Arial", FontSize = 12 },
            new() { Text = "brave ", FontFamily = "Arial", FontSize = 12 },
            new() { Text = "world", FontFamily = "Arial", FontSize = 12 },
        };

        var merged = RichTextHelper.NormalizeSpans(spans);

        Assert.Single(merged);
        Assert.Equal("Hello brave world", merged[0].Text);
    }

    [Fact]
    public void NormalizeSpans_KeepsRunsWithDifferentFormattingApart()
    {
        var spans = new List<PdfTextSpan>
        {
            new() { Text = "normal ", FontFamily = "Arial", FontSize = 12, IsBold = false },
            new() { Text = "BOLD",    FontFamily = "Arial", FontSize = 12, IsBold = true },
            new() { Text = " tail",   FontFamily = "Arial", FontSize = 12, IsBold = false },
        };

        var merged = RichTextHelper.NormalizeSpans(spans);

        Assert.Equal(3, merged.Count);
        Assert.Equal("normal ", merged[0].Text);
        Assert.Equal("BOLD", merged[1].Text);
        Assert.Equal(" tail", merged[2].Text);
    }

    [Fact]
    public void NormalizeSpans_HandlesALongUniformRunQuickly()
    {
        // One span per word, so a long uniformly styled paragraph merges into one span.
        // Accumulating with "+=" was quadratic in characters.
        var spans = Enumerable.Range(0, 8000)
            .Select(i => new PdfTextSpan { Text = "word ", FontFamily = "Arial", FontSize = 12 })
            .ToList();

        var sw = Stopwatch.StartNew();
        var merged = RichTextHelper.NormalizeSpans(spans);
        sw.Stop();

        Assert.Single(merged);
        Assert.Equal(8000 * 5, merged[0].Text.Length);
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Merging 8000 spans took {sw.ElapsedMilliseconds}ms; the accumulation is still quadratic.");
    }

    [Fact]
    public void RichTextHelper_NormalizeSpans_BehavesTheSameWay()
    {
        var spans = Enumerable.Range(0, 4000)
            .Select(_ => new PdfTextSpan { Text = "ab", FontFamily = "Arial", FontSize = 11 })
            .ToList();

        var sw = Stopwatch.StartNew();
        var merged = RichTextHelper.NormalizeSpans(spans);
        sw.Stop();

        Assert.Single(merged);
        Assert.Equal(8000, merged[0].Text.Length);
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Merging 4000 spans took {sw.ElapsedMilliseconds}ms; the accumulation is still quadratic.");
    }
}
