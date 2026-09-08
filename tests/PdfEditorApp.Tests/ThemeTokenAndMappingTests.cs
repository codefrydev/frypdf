using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PdfEditorApp.Core.Analysis;
using Xunit;

namespace PdfEditorApp.Tests;

/// <summary>
/// Regression tests for the Tier 5 theme-token and font-mapping fixes.
/// </summary>
public class ThemeTokenAndMappingTests
{
    private readonly string _projectRoot;

    public ThemeTokenAndMappingTests()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src", "PdfEditorApp")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        _projectRoot = dir!.FullName;
    }

    // ─── Hardcoded neutral surface/text colours ─────────────────────────────

    /// <summary>
    /// Neutral greys and whites used as a surface, text or border colour. A saturated accent
    /// (an icon tint, a category colour) is not covered here — only neutrals, which are what
    /// the light/dark theme is actually responsible for.
    /// </summary>
    private static readonly Regex HardcodedNeutral = new(
        "(Background|Foreground|BorderBrush)=\"#(FFFFFF|F8FAFC|F1F5F9|E2E8F0|CBD5E1|94A3B8|64748B|475569|334155|1E293B|0F172A)\"",
        RegexOptions.Compiled);

    /// <summary>
    /// Sites where a literal neutral is the correct choice, with the reason.
    /// </summary>
    private static readonly Dictionary<string, string> AllowedLiteralNeutrals = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Views/MainWindow.axaml"] = "logo mark: the FryPDF artwork needs a white disc in both themes",
        ["Views/LicensingPageView.axaml"] = "logo mark",
        ["Views/HomeView.axaml"] = "logo mark",
        ["Views/InspectorSidebarView.axaml"] = "colour swatch buttons: the literal IS the value the button applies",
        ["Views/SettingsPageView.axaml"] = "notification style preview: the literal IS the style being previewed",
        ["Views/FryPdfViewerView.axaml"] = "presentation mode is a fixed dark chrome, not theme-driven",
    };

    [Fact]
    public void Axaml_DoesNotHardcodeNeutralThemeColours()
    {
        string viewsRoot = Path.Combine(_projectRoot, "src", "PdfEditorApp");
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(viewsRoot, "*.axaml", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(viewsRoot, file).Replace('\\', '/');
            if (AllowedLiteralNeutrals.ContainsKey(relative)) continue;

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (HardcodedNeutral.IsMatch(lines[i]))
                {
                    offenders.Add($"{relative}:{i + 1}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Neutral surface/text colours must come from theme tokens (M3*Brush or the Win* aliases), " +
            "otherwise they do not follow the light/dark theme. Offending lines:\n  " +
            string.Join("\n  ", offenders));
    }

    [Fact]
    public void AllowList_OnlyCoversFilesThatStillNeedIt()
    {
        // Keeps the allow-list honest: if a file is cleaned up, its entry should be removed
        // rather than silently masking future regressions.
        string viewsRoot = Path.Combine(_projectRoot, "src", "PdfEditorApp");

        foreach (var (relative, reason) in AllowedLiteralNeutrals)
        {
            string full = Path.Combine(viewsRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(full), $"Allow-listed file no longer exists: {relative}");
            Assert.True(HardcodedNeutral.IsMatch(File.ReadAllText(full)),
                $"'{relative}' no longer contains a literal neutral, so its allow-list entry " +
                $"({reason}) should be removed.");
        }
    }

    // ─── Font family mapping: previously unreachable branches ───────────────

    [Fact]
    public void NormalizeFontFamily_MapsAakarToGujaratiNotDevanagari()
    {
        // "aakar" was listed in the earlier Devanagari rule, making the Gujarati branch
        // unreachable for it.
        Assert.Equal("Noto Sans Gujarati", PdfLayoutAnalyzer.NormalizeFontFamily("Aakar"));
        Assert.Equal("Noto Sans Gujarati", PdfLayoutAnalyzer.NormalizeFontFamily("ABCDEE+Aakar-Bold"));
    }

    [Fact]
    public void NormalizeFontFamily_MapsUrduToNastaliq()
    {
        // The general Arabic rule matched "urdu" first, so this branch was dead.
        Assert.Equal("Noto Nastaliq Urdu", PdfLayoutAnalyzer.NormalizeFontFamily("Noto Nastaliq Urdu"));
        Assert.Equal("Noto Nastaliq Urdu", PdfLayoutAnalyzer.NormalizeFontFamily("Jameel Noori Nastaleeq"));
    }

    [Fact]
    public void NormalizeFontFamily_MapsFarsiToVazirmatn()
    {
        // Likewise "farsi" was claimed by the Arabic rule.
        Assert.Equal("Vazirmatn", PdfLayoutAnalyzer.NormalizeFontFamily("B-Farsi"));
        Assert.Equal("Vazirmatn", PdfLayoutAnalyzer.NormalizeFontFamily("IRANSans"));
    }

    [Fact]
    public void NormalizeFontFamily_StillMapsGenericArabic()
    {
        Assert.Equal("Noto Sans Arabic", PdfLayoutAnalyzer.NormalizeFontFamily("Arabic-Regular"));
        Assert.Equal("Noto Sans Arabic", PdfLayoutAnalyzer.NormalizeFontFamily("Scheherazade"));
        Assert.Equal("Noto Sans Arabic", PdfLayoutAnalyzer.NormalizeFontFamily("Amiri-Bold"));
    }

    [Fact]
    public void NormalizeFontFamily_DoesNotTreatMonotypeAsMonospace()
    {
        // "Monotype" is a foundry name; MonotypeCorsiva is a script face and was rendered
        // monospaced by the fn.Contains("mono") rule.
        Assert.NotEqual("Fira Code", PdfLayoutAnalyzer.NormalizeFontFamily("MonotypeCorsiva"));
        Assert.Equal("Dancing Script", PdfLayoutAnalyzer.NormalizeFontFamily("MonotypeCorsiva"));
    }

    [Fact]
    public void NormalizeFontFamily_StillMapsGenuineMonospaceFaces()
    {
        Assert.Equal("Fira Code", PdfLayoutAnalyzer.NormalizeFontFamily("CourierNewPSMT"));
        Assert.Equal("Fira Code", PdfLayoutAnalyzer.NormalizeFontFamily("RobotoMono-Regular"));
        Assert.Equal("Fira Code", PdfLayoutAnalyzer.NormalizeFontFamily("DejaVuSansMono"));
    }

    [Fact]
    public void NormalizeFontFamily_StillMapsDevanagariFaces()
    {
        Assert.Equal("Noto Sans Devanagari", PdfLayoutAnalyzer.NormalizeFontFamily("Mangal"));
        Assert.Equal("Noto Sans Devanagari", PdfLayoutAnalyzer.NormalizeFontFamily("NirmalaUI-Bold"));
    }
}
