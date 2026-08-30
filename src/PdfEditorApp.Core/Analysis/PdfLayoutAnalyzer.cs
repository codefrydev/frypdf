using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Content;

namespace PdfEditorApp.Core.Analysis;

/// <summary>
/// Represents a structured text line extracted from a PDF page with accurate font and geometry metrics.
/// </summary>
public class ExtractedPdfLine
{
    public List<Word> Words { get; } = new();
    public double Left { get; set; }
    public double Right { get; set; }
    public double Top { get; set; }
    public double Bottom { get; set; }
    public double BaselineY { get; set; }
    public string Text { get; set; } = string.Empty;
    public double FontSize { get; set; } = 11.0;
    public string FontFamily { get; set; } = "Arial";
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public string ColorHex { get; set; } = "#0F172A";

    public double Width => Math.Max(1, Right - Left);
    public double Height => Math.Max(1, Top - Bottom);
    public double MidY => (Top + Bottom) / 2.0;
}

/// <summary>
/// Represents a coherent, editable multi-line paragraph block or single-line header
/// grouped using intelligent proximity and typographic analysis (similar to Adobe Acrobat / PDFelement).
/// </summary>
public class ExtractedPdfParagraph
{
    public List<ExtractedPdfLine> Lines { get; } = new();
    public double Left { get; set; }
    public double Right { get; set; }
    public double Top { get; set; }
    public double Bottom { get; set; }
    public string Text { get; set; } = string.Empty;
    public double FontSize { get; set; } = 11.0;
    public string FontFamily { get; set; } = "Arial";
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public string ColorHex { get; set; } = "#0F172A";
    public double LineHeight { get; set; } = 1.2;
    public bool IsHeading { get; set; }
    public bool IsBulletOrList { get; set; }

    public double CanvasX { get; set; }
    public double CanvasY { get; set; }
    public double CanvasWidth { get; set; }
    public double CanvasHeight { get; set; }
}

/// <summary>
/// High-precision layout analyzer for PDF documents.
/// Deconstructs raw glyphs and words into structured lines, paragraphs, headings, and column flows.
/// </summary>
public static class PdfLayoutAnalyzer
{
    /// <summary>
    /// Analyzes words on a PDF page and reconstructs structured, editable paragraph blocks.
    /// </summary>
    /// <param name="page">The PDF page to analyze.</param>
    /// <param name="pageHeight">Page height in PDF points (for coordinate flip).</param>
    /// <param name="columnGapMultiplier">Multiplier to widen column gap threshold (1.5 for landscape/ID cards).</param>
    public static List<ExtractedPdfParagraph> AnalyzeAndGroupPageText(Page page, double pageHeight, double columnGapMultiplier = 1.0)
    {
        var words = page.GetWords()
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .ToList();

        if (words.Count == 0)
        {
            return new List<ExtractedPdfParagraph>();
        }

        // 1. Group words into horizontal lines with multi-column separation
        var lines = ExtractLinesFromWords(words, columnGapMultiplier);

        if (lines.Count == 0)
        {
            return new List<ExtractedPdfParagraph>();
        }

        // 2. Cluster lines into coherent paragraphs and standalone headings
        var paragraphs = ClusterLinesIntoParagraphs(lines, pageHeight);

        return paragraphs;
    }

    private class LineBucket
    {
        public List<Word> Words { get; } = new();
        public double BaselineY { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
    }

    /// <summary>
    /// Groups words into horizontal lines respecting baseline alignment, reading order, and column bounds.
    /// </summary>
    /// <param name="words">Words extracted from a PDF page.</param>
    /// <param name="columnGapMultiplier">Adjusts column gap sensitivity (>1 = more aggressive column splitting).</param>
    public static List<ExtractedPdfLine> ExtractLinesFromWords(List<Word> words, double columnGapMultiplier = 1.0)
    {
        var resultLines = new List<ExtractedPdfLine>();
        if (words == null || words.Count == 0) return resultLines;

        // Step 1: Bucket words into baseline lines
        var buckets = new List<LineBucket>();

        // Sort roughly by vertical position descending
        var sortedByY = words
            .OrderByDescending(w => w.BoundingBox.Bottom)
            .ToList();

        foreach (var word in sortedByY)
        {
            double wordBottom = word.BoundingBox.Bottom;
            double wordTop = word.BoundingBox.Top;
            double wordHeight = Math.Max(4.0, word.BoundingBox.Height);
            double wordMidY = (wordTop + wordBottom) / 2.0;

            // For scripts with tall ascenders (Devanagari, Arabic, CJK), use a tighter midY-only match
            // so characters in different rows are never bucketed together.
            // Heuristic: if any letter in the word has a font height > 1.5x its em-size, be more strict.
            double threshold = Math.Max(3.0, wordHeight * 0.40);

            // Find matching bucket
            LineBucket? matchingBucket = null;
            double bestDist = double.MaxValue;

            foreach (var b in buckets)
            {
                double bucketMidY = (b.Top + b.Bottom) / 2.0;
                double dist = Math.Abs(wordMidY - bucketMidY);
                double baseDist = Math.Abs(wordBottom - b.BaselineY);

                if (dist <= threshold || baseDist <= threshold)
                {
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        matchingBucket = b;
                    }
                }
            }

            if (matchingBucket != null)
            {
                matchingBucket.Words.Add(word);
                matchingBucket.Top = Math.Max(matchingBucket.Top, wordTop);
                matchingBucket.Bottom = Math.Min(matchingBucket.Bottom, wordBottom);
                matchingBucket.BaselineY = (matchingBucket.BaselineY * (matchingBucket.Words.Count - 1) + wordBottom) / matchingBucket.Words.Count;
            }
            else
            {
                var newBucket = new LineBucket
                {
                    BaselineY = wordBottom,
                    Top = wordTop,
                    Bottom = wordBottom
                };
                newBucket.Words.Add(word);
                buckets.Add(newBucket);
            }
        }

        // Step 2: For each bucket, sort words left-to-right and split on column gaps
        foreach (var bucket in buckets)
        {
            var orderedWords = bucket.Words.OrderBy(w => w.BoundingBox.Left).ToList();
            if (orderedWords.Count == 0) continue;

            var currentSegment = new List<Word>();

            foreach (var word in orderedWords)
            {
                if (currentSegment.Count == 0)
                {
                    currentSegment.Add(word);
                    continue;
                }

                double prevRight = currentSegment.Max(w => w.BoundingBox.Right);
                double gap = word.BoundingBox.Left - prevRight;
                double wordHeight = Math.Max(6.0, word.BoundingBox.Height);

                // Column gap threshold: adjusted by columnGapMultiplier for landscape/ID cards.
                // For landscape PDFs (like Aadhaar cards with left+right columns), we split more aggressively.
                double baseGap = Math.Max(20.0, wordHeight * 2.2);
                double maxColGap = baseGap * Math.Max(0.5, columnGapMultiplier);

                if (gap > maxColGap)
                {
                    // Split segment
                    var line = BuildLine(currentSegment);
                    if (line != null) resultLines.Add(line);

                    currentSegment = new List<Word> { word };
                }
                else
                {
                    currentSegment.Add(word);
                }
            }

            if (currentSegment.Count > 0)
            {
                var line = BuildLine(currentSegment);
                if (line != null) resultLines.Add(line);
            }
        }

        // Sort all lines in natural reading order: top-to-bottom, left-to-right
        return resultLines
            .OrderByDescending(l => l.Top)
            .ThenBy(l => l.Left)
            .ToList();
    }

    private static ExtractedPdfLine? BuildLine(List<Word> words)
    {
        if (words == null || words.Count == 0) return null;

        var ordered = words.OrderBy(w => w.BoundingBox.Left).ToList();
        var sb = new StringBuilder();

        double minLeft = ordered.Min(w => w.BoundingBox.Left);
        double maxRight = ordered.Max(w => w.BoundingBox.Right);
        double maxTop = ordered.Max(w => w.BoundingBox.Top);
        double minBottom = ordered.Min(w => w.BoundingBox.Bottom);

        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(ordered[i].Text);
        }

        string text = sb.ToString().Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Sample dominant font characteristics from words
        var firstWord = ordered[0];
        var firstLetter = firstWord.Letters.FirstOrDefault();

        double fontSize = 11.0;
        string fontFamily = "Open Sans";
        bool isBold = false;
        bool isItalic = false;
        string colorHex = "#0F172A";

        if (firstLetter != null)
        {
            fontSize = Math.Max(6.0, firstLetter.PointSize);
            fontFamily = NormalizeFontFamily(firstLetter.FontName);

            // Unicode Script Detection: When font name normalization falls back to a generic
            // Latin font (Open Sans) but the actual text contains non-Latin codepoints, use
            // UnicodeScriptDetector to pick the correct world-script font from our library.
            // This handles CJK, Arabic, Hebrew, Devanagari, Thai, etc. from ANY PDF source.
            if (fontFamily == "Open Sans" && !string.IsNullOrEmpty(text))
            {
                string detectedFont = UnicodeScriptDetector.DetectScriptFontFamily(text);
                if (detectedFont != "Open Sans")
                {
                    fontFamily = detectedFont;
                }
            }

            if (!string.IsNullOrEmpty(firstLetter.FontName))
            {
                string fn = firstLetter.FontName.ToLowerInvariant();
                isBold = fn.Contains("bold") || fn.Contains("black") || fn.Contains("heavy") || fn.Contains("semibold") || fn.Contains("extrabold");
                isItalic = fn.Contains("italic") || fn.Contains("oblique");
            }

            if (firstLetter.Color != null)
            {
                var (r, g, b) = firstLetter.Color.ToRGBValues();
                colorHex = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
            }
        }

        return new ExtractedPdfLine
        {
            Left = minLeft,
            Right = maxRight,
            Top = maxTop,
            Bottom = minBottom,
            BaselineY = minBottom,
            Text = text,
            FontSize = fontSize,
            FontFamily = fontFamily,
            IsBold = isBold,
            IsItalic = isItalic,
            ColorHex = colorHex
        };
    }

    /// <summary>
    /// Clusters lines into coherent paragraphs and standalone headings.
    /// </summary>
    public static List<ExtractedPdfParagraph> ClusterLinesIntoParagraphs(List<ExtractedPdfLine> lines, double pageHeight)
    {
        var paragraphs = new List<ExtractedPdfParagraph>();
        if (lines == null || lines.Count == 0) return paragraphs;

        // Sort lines top-to-bottom, left-to-right
        var sortedLines = lines
            .OrderByDescending(l => l.Top)
            .ThenBy(l => l.Left)
            .ToList();

        var currentCluster = new List<ExtractedPdfLine>();

        foreach (var line in sortedLines)
        {
            if (currentCluster.Count == 0)
            {
                currentCluster.Add(line);
                continue;
            }

            var prevLine = currentCluster.Last();

            // Evaluate if this line belongs to the same paragraph
            bool canCluster = ShouldClusterLines(prevLine, line);

            if (canCluster)
            {
                currentCluster.Add(line);
            }
            else
            {
                var para = BuildParagraphFromLines(currentCluster, pageHeight);
                if (para != null) paragraphs.Add(para);

                currentCluster = new List<ExtractedPdfLine> { line };
            }
        }

        if (currentCluster.Count > 0)
        {
            var para = BuildParagraphFromLines(currentCluster, pageHeight);
            if (para != null) paragraphs.Add(para);
        }

        return paragraphs;
    }

    private static bool ShouldClusterLines(ExtractedPdfLine prev, ExtractedPdfLine next)
    {
        // 1. Font Size compatibility (within 1.5pt difference)
        double sizeDiff = Math.Abs(prev.FontSize - next.FontSize);
        if (sizeDiff > 1.5) return false;

        // 2. Bold / Weight compatibility (headings are distinct from body paragraphs)
        if (prev.IsBold != next.IsBold) return false;

        // 3. Font Family compatibility
        if (!string.Equals(prev.FontFamily, next.FontFamily, StringComparison.OrdinalIgnoreCase)) return false;

        // 4. Text Color compatibility
        if (!string.Equals(prev.ColorHex, next.ColorHex, StringComparison.OrdinalIgnoreCase)) return false;

        // 5. Vertical line pitch calculation (distance from prev bottom to next top)
        double verticalPitch = prev.Bottom - next.Top; // in PDF points, descending Y
        double expectedLineGap = prev.FontSize * 1.6;

        // If line gap is negative (overlapping) or greater than standard paragraph spacing, break
        if (verticalPitch < -4.0 || verticalPitch > expectedLineGap) return false;

        // 6. Horizontal column & margin check
        // If lines are completely separated horizontally (e.g. side-by-side columns)
        bool horizontalOverlap = (next.Left < prev.Right + 10) && (next.Right > prev.Left - 10);
        if (!horizontalOverlap) return false;

        // Check left margin alignment (within 24pt for indented paragraphs)
        double leftIndentDiff = Math.Abs(prev.Left - next.Left);
        if (leftIndentDiff > 24.0) return false;

        // 7. Bullet / List Item start detection
        string nextText = next.Text.Trim();
        if (nextText.StartsWith("•") || nextText.StartsWith("-") || nextText.StartsWith("—") ||
            (nextText.Length >= 2 && char.IsDigit(nextText[0]) && (nextText[1] == '.' || nextText[1] == ')')))
        {
            // If next line starts with a list marker, break to a new paragraph/list block
            return false;
        }

        return true;
    }

    private static ExtractedPdfParagraph? BuildParagraphFromLines(List<ExtractedPdfLine> lines, double pageHeight)
    {
        if (lines == null || lines.Count == 0) return null;

        double minLeft = lines.Min(l => l.Left);
        double maxRight = lines.Max(l => l.Right);
        double maxTop = lines.Max(l => l.Top);
        double minBottom = lines.Min(l => l.Bottom);

        var firstLine = lines[0];
        double dominantFontSize = lines.GroupBy(l => Math.Round(l.FontSize, 1)).OrderByDescending(g => g.Count()).First().Key;
        string dominantFontFamily = firstLine.FontFamily;
        string dominantColor = firstLine.ColorHex;
        bool isBold = lines.Count(l => l.IsBold) >= (lines.Count / 2.0);
        bool isItalic = lines.Count(l => l.IsItalic) >= (lines.Count / 2.0);

        var sb = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0) sb.Append(Environment.NewLine);
            sb.Append(lines[i].Text);
        }

        string fullText = sb.ToString().Trim();
        if (string.IsNullOrWhiteSpace(fullText)) return null;

        // Calculate average line pitch / height ratio
        double lineHeight = 1.2;
        if (lines.Count > 1)
        {
            double totalSpan = maxTop - minBottom;
            double avgLineSpan = totalSpan / lines.Count;
            if (dominantFontSize > 0)
            {
                lineHeight = Math.Max(1.0, Math.Min(2.0, avgLineSpan / dominantFontSize));
            }
        }

        bool isHeading = (dominantFontSize >= 14.0 || (isBold && dominantFontSize >= 12.5) || (lines.Count == 1 && isBold));
        bool isBullet = fullText.StartsWith("•") || fullText.StartsWith("-") || fullText.StartsWith("—");

        // Convert PDF coordinates (origin bottom-left, Y goes up) to Canvas coordinates (origin top-left, Y goes down)
        double canvasX = Math.Max(0, minLeft);
        double canvasY = Math.Max(0, pageHeight - maxTop);
        // Add a small safety padding (4-6pt) to width & height so Avalonia text controls render without unexpected line-clipping
        double canvasWidth = Math.Max(30.0, (maxRight - minLeft) + 6.0);
        double canvasHeight = Math.Max(16.0, (maxTop - minBottom) + 4.0);

        return new ExtractedPdfParagraph
        {
            Left = minLeft,
            Right = maxRight,
            Top = maxTop,
            Bottom = minBottom,
            Text = fullText,
            FontSize = Math.Round(dominantFontSize, 1),
            FontFamily = dominantFontFamily,
            IsBold = isBold,
            IsItalic = isItalic,
            ColorHex = dominantColor,
            LineHeight = Math.Round(lineHeight, 2),
            IsHeading = isHeading,
            IsBulletOrList = isBullet,
            CanvasX = Math.Round(canvasX, 1),
            CanvasY = Math.Round(canvasY, 1),
            CanvasWidth = Math.Round(canvasWidth, 1),
            CanvasHeight = Math.Round(canvasHeight, 1)
        };
    }

    /// <summary>
    /// Normalizes PDF font names (e.g., 'ABCDEF+Arial-BoldMT' or 'TimesNewRomanPS') to clean cross-platform font families.
    /// Handles: Latin, Devanagari (Hindi/Marathi), Tamil, Telugu, Arabic, CJK (Chinese/Japanese/Korean).
    /// </summary>
    public static string NormalizeFontFamily(string? fontName)
    {
        if (string.IsNullOrWhiteSpace(fontName)) return "Open Sans";

        string fn = fontName.ToLowerInvariant();

        // Strip PDF subset prefixes like 'ABCDEF+'
        int plusIdx = fn.IndexOf('+');
        if (plusIdx >= 0 && plusIdx < fn.Length - 1)
        {
            fn = fn.Substring(plusIdx + 1);
        }

        // --- Latin & Common Fonts ---
        if (fn.Contains("arial")) return "Arial";
        if (fn.Contains("times") || fn.Contains("libertine")) return "PT Serif";
        if (fn.Contains("cambria") || fn.Contains("crimson") || fn.Contains("garamond") || fn.Contains("palatino")) return "Crimson Text";
        if (fn.Contains("courier") || fn.Contains("firacode") || fn.Contains("robotomono")) return "Fira Code";
        if (fn.Contains("consolas") || fn.Contains("lucidasans")) return "Fira Code";
        if (fn.Contains("segoe")) return "Open Sans"; // Fallback to Open Sans for cross-platform
        if (fn.Contains("roboto")) return "Roboto";
        if (fn.Contains("inter")) return "Inter";
        if (fn.Contains("calibri") || fn.Contains("cabin")) return "Cabin";
        if (fn.Contains("georgia") || fn.Contains("ptserif") || fn.Contains("pt serif")) return "PT Serif";
        if (fn.Contains("verdana")) return "Verdana";
        if (fn.Contains("helvetica")) return "Arial"; // Helvetica → Arial cross-platform
        if (fn.Contains("lato")) return "Lato";
        if (fn.Contains("opensans") || fn.Contains("open sans")) return "Open Sans";
        if (fn.Contains("poppins")) return "Poppins";
        if (fn.Contains("sourcesans") || fn.Contains("source sans")) return "Source Sans 3";
        if (fn.Contains("notosans") && !fn.Contains("devanagari") && !fn.Contains("tamil") &&
            !fn.Contains("telugu") && !fn.Contains("arabic") && !fn.Contains("gujarati") &&
            !fn.Contains("kannada") && !fn.Contains("bengali") && !fn.Contains("malayalam"))
            return "Noto Sans";
        if (fn.Contains("notoserif")) return "Noto Serif";
        if (fn.Contains("mono") || fn.Contains("courier")) return "Fira Code";
        if (fn.Contains("raleway")) return "Raleway";
        if (fn.Contains("nunito")) return "Nunito";
        if (fn.Contains("ubuntu")) return "Ubuntu";
        if (fn.Contains("merriweather")) return "Merriweather";
        if (fn.Contains("oswald")) return "Oswald";
        if (fn.Contains("montserrat")) return "Montserrat";
        if (fn.Contains("playfair")) return "Playfair Display";
        if (fn.Contains("josefin")) return "Josefin Sans";
        if (fn.Contains("titillium")) return "Titillium Web";
        if (fn.Contains("exo")) return "Exo 2";
        if (fn.Contains("librebaskerville") || fn.Contains("libre baskerville")) return "Libre Baskerville";
        if (fn.Contains("librefranklin") || fn.Contains("libre franklin")) return "Libre Franklin";
        if (fn.Contains("lora")) return "Lora";
        if (fn.Contains("bebas")) return "Bebas Neue";
        if (fn.Contains("cinzel")) return "Cinzel";
        if (fn.Contains("orbitron")) return "Orbitron";
        if (fn.Contains("lobster")) return "Lobster";
        if (fn.Contains("pacifico")) return "Pacifico";
        if (fn.Contains("dancingscript") || fn.Contains("dancing script")) return "Dancing Script";
        if (fn.Contains("caveat")) return "Caveat";
        if (fn.Contains("greatvibes") || fn.Contains("great vibes")) return "Great Vibes";
        if (fn.Contains("comicneue") || fn.Contains("comic")) return "Comic Neue";

        // --- Indian Scripts → mapped to Noto variants we have on disk ---
        if (fn.Contains("nirmala") || fn.Contains("mangal") || fn.Contains("devanagari") ||
            fn.Contains("kruti") || fn.Contains("shree") || fn.Contains("aakar") ||
            fn.Contains("lohit") || fn.Contains("samyak") || fn.Contains("tiro") ||
            fn.Contains("hindi") || fn.Contains("marathi") || fn.Contains("sanskrit"))
            return "Noto Sans Devanagari";
        if (fn.Contains("tamil") || fn.Contains("latha") || fn.Contains("vijaya"))
            return "Noto Sans Tamil";
        if (fn.Contains("telugu") || fn.Contains("gautami") || fn.Contains("vani"))
            return "Noto Sans Telugu";
        if (fn.Contains("kannada") || fn.Contains("tunga") || fn.Contains("kedage"))
            return "Noto Sans Kannada";
        if (fn.Contains("malayalam") || fn.Contains("kartika") || fn.Contains("rachana"))
            return "Noto Sans Malayalam";
        if (fn.Contains("gujarati") || fn.Contains("shruti") || fn.Contains("aakar"))
            return "Noto Sans Gujarati";
        if (fn.Contains("punjabi") || fn.Contains("gurmukhi") || fn.Contains("raavi"))
            return "Noto Sans";
        if (fn.Contains("bengali") || fn.Contains("vrinda") || fn.Contains("shonar"))
            return "Noto Sans Bengali";
        if (fn.Contains("oriya") || fn.Contains("odia") || fn.Contains("kalinga"))
            return "Noto Sans";

        // --- Arabic / Urdu / Farsi ---
        if (fn.Contains("arabic") || fn.Contains("urdu") || fn.Contains("farsi") ||
            fn.Contains("naskh") || fn.Contains("scheherazade") || fn.Contains("amiri") ||
            fn.Contains("traditional arabic") || fn.Contains("simplified arabic"))
            return "Noto Sans Arabic";

        // --- CJK (Chinese / Japanese / Korean) --- with proper script distinction ---
        // Chinese Simplified: SimSun, SimHei, Microsoft YaHei, WenQuanYi, Noto SC
        if (fn.Contains("simsun") || fn.Contains("simhei") || fn.Contains("wqy") ||
            fn.Contains("notosanssc") || fn.Contains("noto sans sc") ||
            fn.Contains("microsoftyahei") || fn.Contains("yahei") ||
            fn.Contains("songti") || fn.Contains("heiti") || fn.Contains("fangsong") ||
            fn.Contains("source han sans sc") || fn.Contains("source han sans cn"))
            return "Noto Sans SC";
        // Chinese Traditional: MingLiU, PMingLiU, Microsoft JhengHei, Noto TC
        if (fn.Contains("mingliu") || fn.Contains("pmingliou") || fn.Contains("kaiti") ||
            fn.Contains("notosanstc") || fn.Contains("noto sans tc") ||
            fn.Contains("microsoftjhenghei") || fn.Contains("jhenghei") ||
            fn.Contains("source han sans tw") || fn.Contains("source han sans hk"))
            return "Noto Sans TC";
        // Japanese: MS Gothic, MS Mincho, IPAex, Meiryo, Hiragino, Noto JP
        if (fn.Contains("msgothic") || fn.Contains("msmincho") || fn.Contains("ipaex") ||
            fn.Contains("meiryo") || fn.Contains("hiragino") || fn.Contains("yu gothic") ||
            fn.Contains("yugothic") || fn.Contains("yumincho") || fn.Contains("noto sans jp") ||
            fn.Contains("notosansjp") || fn.Contains("source han sans jp") ||
            fn.Contains("kozuka") || fn.Contains("morisawa"))
            return "Noto Sans JP";
        // Korean: Malgun, Batang, Gulim, Dotum, Nanum, Noto KR
        if (fn.Contains("malgun") || fn.Contains("batang") || fn.Contains("gulim") ||
            fn.Contains("dotum") || fn.Contains("nanum") || fn.Contains("nanumgothic") ||
            fn.Contains("notosanskr") || fn.Contains("noto sans kr") ||
            fn.Contains("source han sans k") || fn.Contains("apple sd gothic"))
            return "Noto Sans KR";
        // General CJK fallback (unidentified CJK font)
        if (fn.Contains("cjk") || fn.Contains("chinese") || fn.Contains("japanese") || fn.Contains("korean"))
            return "Noto Sans SC";

        // --- Thai ---
        if (fn.Contains("thai") || fn.Contains("sarabun") || fn.Contains("thsarabun") ||
            fn.Contains("browallia") || fn.Contains("cordia") || fn.Contains("angsana"))
            return "Noto Sans Thai";

        // --- Hebrew ---
        if (fn.Contains("hebrew") || fn.Contains("heebo") || fn.Contains("david") ||
            fn.Contains("miriam") || fn.Contains("frank ruehl") || fn.Contains("frankruehl"))
            return "Noto Sans Hebrew";

        // --- Greek ---
        if (fn.Contains("greek") || fn.Contains("gfs") || fn.Contains("paleologos") ||
            fn.Contains("didot") || fn.Contains("helvetica greek"))
            return "GFS Neohellenic";

        // --- Cyrillic (Russian, Ukrainian, Bulgarian, Serbian, Macedonian) ---
        if (fn.Contains("cyrillic") || fn.Contains("golos") || fn.Contains("russo") ||
            fn.Contains("pragmatica") || fn.Contains("paratypesans") ||
            fn.Contains("ptastra") || fn.Contains("ptsans"))
            return "Golos Text";

        // --- Georgian ---
        if (fn.Contains("georgian") || fn.Contains("sylfaen") || fn.Contains("alk") ||
            fn.Contains("bpg") || fn.Contains("dejavu georgian"))
            return "Noto Sans Georgian";

        // --- Armenian ---
        if (fn.Contains("armenian") || fn.Contains("mshtakan") || fn.Contains("euphemia"))
            return "Noto Sans Armenian";

        // --- Persian / Farsi ---
        if (fn.Contains("persian") || fn.Contains("farsi") || fn.Contains("vazir") ||
            fn.Contains("iran") || fn.Contains("yekan") || fn.Contains("nazanin"))
            return "Vazirmatn";

        // --- Urdu (Nastaliq) ---
        if (fn.Contains("urdu") || fn.Contains("nastaliq") || fn.Contains("noori") || fn.Contains("nafees"))
            return "Noto Nastaliq Urdu";

        // --- Myanmar ---
        if (fn.Contains("myanmar") || fn.Contains("burmese") || fn.Contains("zawgyi") || fn.Contains("mon"))
            return "Noto Sans Myanmar";

        // --- Khmer ---
        if (fn.Contains("khmer") || fn.Contains("cambodian") || fn.Contains("battambang"))
            return "Noto Sans Khmer";

        // --- Ethiopic ---
        if (fn.Contains("ethiopic") || fn.Contains("amharic") || fn.Contains("geez") ||
            fn.Contains("abyssinica") || fn.Contains("nyala"))
            return "Noto Sans Ethiopic";

        return "Open Sans"; // Default: clean, widely compatible sans-serif
    }
}
