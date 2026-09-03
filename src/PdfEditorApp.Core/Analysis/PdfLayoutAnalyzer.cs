using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using UglyToad.PdfPig.Content;

namespace PdfEditorApp.Core.Analysis;

/// <summary>
/// Represents a structured text line extracted from a PDF page with accurate font and geometry metrics.
/// </summary>
public class ExtractedPdfLine
{
    public List<Word> Words { get; } = new();
    public List<PdfTextSpan> Spans { get; set; } = new();
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

    public double Rotation { get; set; } = 0.0;
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
    public List<PdfTextSpan> Spans { get; set; } = new();
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
    public double Rotation { get; set; } = 0.0;
    public TextAlignmentMode Alignment { get; set; } = TextAlignmentMode.Left;

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
    /// Supports multi-orientation text (horizontal, 90° and 270° vertical marginalia).
    /// </summary>
    /// <param name="page">The PDF page to analyze.</param>
    /// <param name="pageHeight">Page height in PDF points (for coordinate flip).</param>
    /// <param name="columnGapMultiplier">Multiplier to widen column gap threshold (1.5 for landscape/ID cards).</param>
    public static List<ExtractedPdfParagraph> AnalyzeAndGroupPageText(Page page, double pageHeight, double columnGapMultiplier = 1.0)
    {
        var rawWords = page.GetWords()
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .ToList();

        if (rawWords.Count == 0)
        {
            return new List<ExtractedPdfParagraph>();
        }

        // Clean null characters and non-printable noise from words
        var words = CleanWords(rawWords);
        if (words.Count == 0) return new List<ExtractedPdfParagraph>();

        var allParagraphs = new List<ExtractedPdfParagraph>();

        // Partition words by dominant orientation
        var horizontalWords = new List<Word>();
        var rotate270Words = new List<Word>();
        var rotate90Words = new List<Word>();
        var rotate180Words = new List<Word>();

        foreach (var w in words)
        {
            var firstLetter = w.Letters.FirstOrDefault();
            var orient = firstLetter?.TextOrientation ?? TextOrientation.Horizontal;

            switch (orient)
            {
                case TextOrientation.Rotate270:
                    rotate270Words.Add(w);
                    break;
                case TextOrientation.Rotate90:
                    rotate90Words.Add(w);
                    break;
                case TextOrientation.Rotate180:
                    rotate180Words.Add(w);
                    break;
                default:
                    horizontalWords.Add(w);
                    break;
            }
        }

        // 1. Process standard Horizontal text
        if (horizontalWords.Count > 0)
        {
            var hLines = ExtractLinesFromWords(horizontalWords, columnGapMultiplier);
            var hParas = ClusterLinesIntoParagraphs(hLines, pageHeight);
            allParagraphs.AddRange(hParas);
        }

        // 2. Process Rotate270 vertical text (e.g. bottom-to-top or top-to-bottom marginalia)
        if (rotate270Words.Count > 0)
        {
            var v270Paras = ExtractVerticalParagraphs(rotate270Words, pageHeight, 270.0);
            allParagraphs.AddRange(v270Paras);
        }

        // 3. Process Rotate90 vertical text
        if (rotate90Words.Count > 0)
        {
            var v90Paras = ExtractVerticalParagraphs(rotate90Words, pageHeight, 90.0);
            allParagraphs.AddRange(v90Paras);
        }

        // 4. Process Rotate180 text
        if (rotate180Words.Count > 0)
        {
            var v180Paras = ExtractVerticalParagraphs(rotate180Words, pageHeight, 180.0);
            allParagraphs.AddRange(v180Paras);
        }

        return allParagraphs;
    }

    private static List<Word> CleanWords(List<Word> rawWords)
    {
        var cleaned = new List<Word>();
        foreach (var w in rawWords)
        {
            if (string.IsNullOrWhiteSpace(w.Text)) continue;

            // Strip null chars and control codes
            string txt = w.Text.Replace("\0", "").Trim();
            if (string.IsNullOrWhiteSpace(txt)) continue;

            cleaned.Add(w);
        }
        return cleaned;
    }

    private static List<ExtractedPdfParagraph> ExtractVerticalParagraphs(List<Word> words, double pageHeight, double rotationAngle)
    {
        var paragraphs = new List<ExtractedPdfParagraph>();
        if (words == null || words.Count == 0) return paragraphs;

        // Group words by vertical column baseline (matching X coordinate).
        // For Rotate270 text each glyph is a single character stacked bottom-to-top,
        // so we use a narrow X-band threshold to bucket same-column glyphs together.
        var colBuckets = new List<List<Word>>();
        var sortedByX = words.OrderBy(w => w.BoundingBox.Left).ToList();

        foreach (var word in sortedByX)
        {
            double wordLeft = word.BoundingBox.Left;
            double wordRight = word.BoundingBox.Right;
            double wordMidX = (wordLeft + wordRight) / 2.0;

            List<Word>? matchingCol = null;
            double bestDist = double.MaxValue;

            foreach (var col in colBuckets)
            {
                double colMidX = (col.Min(w => w.BoundingBox.Left) + col.Max(w => w.BoundingBox.Right)) / 2.0;
                double dist = Math.Abs(wordMidX - colMidX);
                // Use a tight 12pt X-band tolerance so left/right card text is never merged
                if (dist <= 12.0 && dist < bestDist)
                {
                    bestDist = dist;
                    matchingCol = col;
                }
            }

            if (matchingCol != null)
            {
                matchingCol.Add(word);
            }
            else
            {
                colBuckets.Add(new List<Word> { word });
            }
        }

        foreach (var col in colBuckets)
        {
            // For Rotate270: PDF text matrix is rotated 270°, meaning each glyph's Y-axis
            // points leftward. Reading order runs bottom→top in PDF coordinates.
            // For Rotate90: reading order runs top→bottom in PDF coordinates.
            //
            // IMPORTANT: PdfPig may return inverted BoundingBox.Bottom/Top for rotated glyphs
            // (Bottom > Top is possible). Use GeoMinY/GeoMaxY helpers for reliable geometric bounds.
            static double GeoMinY(Word w) => Math.Min(w.BoundingBox.Bottom, w.BoundingBox.Top);
            static double GeoMaxY(Word w) => Math.Max(w.BoundingBox.Bottom, w.BoundingBox.Top);

            // Sort by geometric minimum Y (bottom of glyph on page). For Rotate270 text,
            // glyphs with the smallest Y value are at the visual bottom of the card.
            var orderedWords = rotationAngle is 270.0 or 90.0
                ? col.OrderBy(GeoMinY).ToList()
                : col.OrderByDescending(GeoMinY).ToList();

            if (orderedWords.Count == 0) continue;

            var firstLetter = orderedWords[0].Letters.FirstOrDefault();
            double fontSize = firstLetter != null ? Math.Max(6.0, firstLetter.PointSize) : 10.0;

            // For Rotate270/90 text, gaps are computed geometrically (GeoMinY(cur) - GeoMaxY(prev)).
            // Adjacent glyphs within the same word overlap slightly (gap ≈ 0 to -0.5pt).
            // A word-space character produces a gap of approximately fontSize * 0.15 to 0.25.
            // Use a threshold just above 0 to catch spaces without false-positives within a word.
            double spaceThreshold = Math.Max(fontSize * 0.2, 0.8);

            var sb = new StringBuilder();
            for (int i = 0; i < orderedWords.Count; i++)
            {
                var cur = orderedWords[i];
                string txt = cur.Text.Replace("\0", "").Trim();
                if (string.IsNullOrWhiteSpace(txt)) continue;

                if (i > 0 && sb.Length > 0)
                {
                    var prev = orderedWords[i - 1];
                    // Gap between the geometric top of prev glyph and geometric bottom of cur glyph.
                    // Using geometric bounds ensures correctness for inverted PdfPig Rotate270 boxes.
                    double prevGeoMaxY = GeoMaxY(prev);
                    double curGeoMinY  = GeoMinY(cur);
                    double verticalGap = curGeoMinY - prevGeoMaxY;

                    // Add a space whenever the geometric gap between successive glyphs exceeds
                    // the word-space threshold. With correct geometric bounds:
                    //   - Adjacent glyphs within the same word: gap ≤ 0 (overlap or touching)
                    //   - Word-space boundary: gap ≈ fontSize * 0.15-0.25 (positive gap)
                    // The threshold of fontSize*0.2 (≥0.8pt) cleanly separates these cases.
                    if (verticalGap >= spaceThreshold)
                    {
                        sb.Append(' ');
                    }
                }
                sb.Append(txt);
            }

            string fullText = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(fullText)) continue;
            string fontFamily = firstLetter != null ? NormalizeFontFamily(firstLetter.FontName) : "Open Sans";
            string colorHex = "#0F172A";

            if (firstLetter?.Color != null)
            {
                var (r, g, b) = firstLetter.Color.ToRGBValues();
                colorHex = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
            }

            double minLeft  = orderedWords.Min(w => w.BoundingBox.Left);
            double maxRight = orderedWords.Max(w => w.BoundingBox.Right);
            // Use geometric bounds: span all geometric Y extents of glyphs in the column.
            double geoMaxY  = orderedWords.Max(GeoMaxY);
            double geoMinY  = orderedWords.Min(GeoMinY);

            // In PDF space for Rotate270/90 text:
            //   pdfSpanLength  = full Y-axis span of the glyph column  → TEXT LENGTH (how long the text run is)
            //   pdfSpanThick   = X-axis width of the glyph column      → FONT THICKNESS (cap height in points)
            double pdfSpanLength = geoMaxY - geoMinY;
            double pdfSpanThick  = maxRight - minLeft;

            // Unrotated element dimensions:
            //   Width  = text run length (how long the string is when rendered horizontally)
            //   Height = line height / font cap-height
            // Add generous padding so glyphs don't clip at the bounding box edge.
            double unrotatedW = Math.Max(30.0, pdfSpanLength + 12.0);
            double unrotatedH = Math.Max(14.0, Math.Max(pdfSpanThick * 2.2, fontSize * 1.6));

            // Center of the glyph cluster in PDF coordinates → Canvas coordinates.
            // Use geometric center Y of the column.
            double centerPdfX    = (minLeft + maxRight) / 2.0;
            double centerPdfY    = (geoMaxY + geoMinY) / 2.0;
            double centerCanvasX = centerPdfX;
            double centerCanvasY = pageHeight - centerPdfY;

            // ── Rotation-aware placement ────────────────────────────────────────────────
            // Avalonia's RotateTransform pivots around the element's own center (RenderTransformOrigin=50%,50%).
            // After a 90° or 270° rotation the element's visual footprint swaps Width↔Height:
            //   Visual width  = unrotatedH  (the thin font-height dimension)
            //   Visual height = unrotatedW  (the long text-run dimension)
            //
            // Canvas.Left/Top (top-left of the UNROTATED box) must satisfy:
            //   pivot_X = canvasX + unrotatedW/2  →  canvasX = centerCanvasX - unrotatedW/2
            //   pivot_Y = canvasY + unrotatedH/2  →  canvasY = centerCanvasY - unrotatedH/2
            //
            // This places the rotation pivot exactly at (centerCanvasX, centerCanvasY),
            // so the rotated text visually appears centred over the original PDF glyph cluster.
            double canvasX = Math.Max(0, centerCanvasX - (unrotatedW / 2.0));
            double canvasY = Math.Max(0, centerCanvasY - (unrotatedH / 2.0));

            paragraphs.Add(new ExtractedPdfParagraph
            {
                Left         = minLeft,
                Right        = maxRight,
                Top          = geoMaxY,
                Bottom       = geoMinY,
                Text         = fullText,
                FontSize     = Math.Round(fontSize, 1),
                FontFamily   = fontFamily,
                ColorHex     = colorHex,
                Rotation     = rotationAngle,
                CanvasX      = Math.Round(canvasX, 1),
                CanvasY      = Math.Round(canvasY, 1),
                CanvasWidth  = Math.Round(unrotatedW, 1),
                CanvasHeight = Math.Round(unrotatedH, 1)
            });
        }

        return paragraphs;
    }

    /// <summary>
    /// Computes accurate geometric and typographic bounds for a word.
    /// Replaces collapsed bounding boxes (e.g. Type3 fonts, OCR lines, or glyphs with zero height)
    /// with proper font ascender/descender bounds relative to the actual baseline.
    /// </summary>
    public static (double Left, double Right, double Top, double Bottom, double BaselineY, double Height) GetWordEffectiveBounds(Word word)
    {
        var firstLetter = word.Letters.FirstOrDefault();
        double ptSize = firstLetter != null && firstLetter.PointSize > 0 ? firstLetter.PointSize : Math.Max(word.BoundingBox.Height, 10.0);

        double bboxH = word.BoundingBox.Height;
        double rawBottom = word.BoundingBox.Bottom;
        double rawTop = word.BoundingBox.Top;
        double rawLeft = word.BoundingBox.Left;
        double rawRight = word.BoundingBox.Right;

        // Baseline: prefer StartBaseLine.Y if non-zero, otherwise rawBottom
        double baselineY = rawBottom;
        if (firstLetter != null && Math.Abs(firstLetter.StartBaseLine.Y) > 0.001)
        {
            baselineY = firstLetter.StartBaseLine.Y;
        }

        // If the bounding box height is collapsed or abnormally small (< 60% of point size)
        // (common in Type3 fonts, OCR text overlays, or stripped font metrics):
        if (bboxH < ptSize * 0.60 || (firstLetter != null && string.Equals(firstLetter.FontName, "Type3", StringComparison.OrdinalIgnoreCase)))
        {
            double ascent = ptSize * 0.78;
            double descent = ptSize * 0.22;
            double effTop = baselineY + ascent;
            double effBottom = baselineY - descent;
            return (rawLeft, rawRight, effTop, effBottom, baselineY, ptSize);
        }

        return (rawLeft, rawRight, rawTop, rawBottom, baselineY, Math.Max(ptSize * 0.8, bboxH));
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

        // Sort roughly by vertical baseline position descending
        var sortedByY = words
            .OrderByDescending(w => GetWordEffectiveBounds(w).BaselineY)
            .ToList();

        foreach (var word in sortedByY)
        {
            var (wLeft, wRight, wTop, wBottom, wBase, wHeight) = GetWordEffectiveBounds(word);
            double wordMidY = (wTop + wBottom) / 2.0;

            // For scripts with tall ascenders (Devanagari, Arabic, CJK), use a tighter midY-only match
            // so characters in different rows are never bucketed together.
            double threshold = Math.Max(3.0, wHeight * 0.40);

            // Find matching bucket
            LineBucket? matchingBucket = null;
            double bestDist = double.MaxValue;

            foreach (var b in buckets)
            {
                double bucketMidY = (b.Top + b.Bottom) / 2.0;
                double dist = Math.Abs(wordMidY - bucketMidY);
                double baseDist = Math.Abs(wBase - b.BaselineY);

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
                matchingBucket.Top = Math.Max(matchingBucket.Top, wTop);
                matchingBucket.Bottom = Math.Min(matchingBucket.Bottom, wBottom);
                matchingBucket.BaselineY = (matchingBucket.BaselineY * (matchingBucket.Words.Count - 1) + wBase) / matchingBucket.Words.Count;
            }
            else
            {
                var newBucket = new LineBucket
                {
                    BaselineY = wBase,
                    Top = wTop,
                    Bottom = wBottom
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

                double prevRight = currentSegment.Max(w => GetWordEffectiveBounds(w).Right);
                double gap = GetWordEffectiveBounds(word).Left - prevRight;
                var wordBounds = GetWordEffectiveBounds(word);
                double wordHeight = Math.Max(6.0, wordBounds.Height);

                // Column / element gap threshold: in typography, gaps > 25pt or > 1.1x font size represent separate columns/badges.
                double baseGap = Math.Max(16.0, Math.Min(30.0, wordHeight * 1.1));
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

        var ordered = words.OrderBy(w => GetWordEffectiveBounds(w).Left).ToList();
        var sb = new StringBuilder();

        double minLeft = ordered.Min(w => GetWordEffectiveBounds(w).Left);
        double maxRight = ordered.Max(w => GetWordEffectiveBounds(w).Right);
        double maxTop = ordered.Max(w => GetWordEffectiveBounds(w).Top);
        double minBottom = ordered.Min(w => GetWordEffectiveBounds(w).Bottom);
        double avgBaseline = ordered.Average(w => GetWordEffectiveBounds(w).BaselineY);

        for (int i = 0; i < ordered.Count; i++)
        {
            if (i > 0)
            {
                var prev = ordered[i - 1];
                var cur = ordered[i];
                double gap = GetWordEffectiveBounds(cur).Left - GetWordEffectiveBounds(prev).Right;
                double ptSize = cur.Letters.FirstOrDefault()?.PointSize ?? 10.0;
                double spaceThreshold = Math.Max(1.6, ptSize * 0.18);

                bool isComplexScript = UnicodeScriptDetector.ContainsDevanagari(cur.Text) ||
                                      UnicodeScriptDetector.ContainsDevanagari(prev.Text) ||
                                      UnicodeScriptDetector.ContainsCjk(cur.Text) ||
                                      UnicodeScriptDetector.IsRtlText(cur.Text);

                if (isComplexScript)
                {
                    // For complex scripts with split glyphs/matras, only space if gap is genuine word spacing
                    if (gap >= spaceThreshold)
                    {
                        sb.Append(' ');
                    }
                }
                else
                {
                    sb.Append(' ');
                }
            }
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

                // Type3 Identification sequence formatting (12-digit ID numbers)
                if (fn.Contains("type3") && text.Length >= 12 && text.Count(char.IsDigit) >= 10)
                {
                    isBold = true;
                }
            }

            if (firstLetter.Color != null)
            {
                var (r, g, b) = firstLetter.Color.ToRGBValues();
                colorHex = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
            }
        }

        var rawLineSpans = new List<PdfTextSpan>();
        for (int i = 0; i < ordered.Count; i++)
        {
            var w = ordered[i];
            var let = w.Letters.FirstOrDefault();
            double wSize = let != null ? Math.Max(6.0, let.PointSize) : fontSize;
            string wFam = let != null ? NormalizeFontFamily(let.FontName) : fontFamily;
            bool wBold = false;
            bool wItalic = false;
            string wCol = colorHex;

            if (let != null && !string.IsNullOrEmpty(let.FontName))
            {
                string fn = let.FontName.ToLowerInvariant();
                wBold = fn.Contains("bold") || fn.Contains("black") || fn.Contains("heavy") || fn.Contains("semibold") || fn.Contains("extrabold");
                wItalic = fn.Contains("italic") || fn.Contains("oblique");
            }
            if (let?.Color != null)
            {
                var (r, g, b) = let.Color.ToRGBValues();
                wCol = $"#{(int)(r * 255):X2}{(int)(g * 255):X2}{(int)(b * 255):X2}";
            }

            string wText = (i > 0 ? " " : "") + w.Text;
            rawLineSpans.Add(new PdfTextSpan
            {
                Text = wText,
                FontFamily = wFam,
                FontSize = wSize,
                IsBold = wBold,
                IsItalic = wItalic,
                TextColorHex = wCol
            });
        }

        var normalizedLineSpans = NormalizeSpans(rawLineSpans);

        return new ExtractedPdfLine
        {
            Left = minLeft,
            Right = maxRight,
            Top = maxTop,
            Bottom = minBottom,
            BaselineY = avgBaseline,
            Text = text,
            FontSize = fontSize,
            FontFamily = fontFamily,
            IsBold = isBold,
            IsItalic = isItalic,
            ColorHex = colorHex,
            Spans = normalizedLineSpans
        };
    }

    /// <summary>
    /// Clusters lines into coherent paragraphs and standalone headings.
    /// </summary>
    public static List<ExtractedPdfParagraph> ClusterLinesIntoParagraphs(List<ExtractedPdfLine> lines, double pageHeight)
    {
        var paragraphs = new List<ExtractedPdfParagraph>();
        if (lines == null || lines.Count == 0) return paragraphs;

        // Sort lines in natural multi-column reading order (top-to-bottom of Column 1, then Column 2)
        var sortedLines = SortLinesInNaturalReadingOrder(lines);

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

    private static List<ExtractedPdfLine> SortLinesInNaturalReadingOrder(List<ExtractedPdfLine> lines)
    {
        if (lines.Count <= 3) return lines.OrderByDescending(l => l.Top).ThenBy(l => l.Left).ToList();

        double minX = lines.Min(l => l.Left);
        double maxX = lines.Max(l => l.Right);
        double spanW = maxX - minX;

        // Detect multi-column or card layouts (2, 3, 4+ columns) on wide pages or cheat sheets
        if (spanW > 240.0)
        {
            // Full-width headers spanning > 60% of total content width
            var fullSpanHeaders = lines.Where(l => (l.Right - l.Left) >= spanW * 0.60).ToList();
            var nonSpanLines = lines.Except(fullSpanHeaders).ToList();

            if (nonSpanLines.Count >= 4)
            {
                var columnGroups = new List<List<ExtractedPdfLine>>();
                var sortedByX = nonSpanLines.OrderBy(l => l.Left).ToList();

                foreach (var line in sortedByX)
                {
                    // Find a column group where line overlaps horizontally or is closely aligned with column bounds
                    var matchingCol = columnGroups.FirstOrDefault(col =>
                    {
                        double colMinX = col.Min(cl => cl.Left);
                        double colMaxX = col.Max(cl => cl.Right);
                        return (line.Left >= colMinX - 15.0 && line.Left <= colMaxX + 15.0) ||
                               (line.Right >= colMinX - 15.0 && line.Right <= colMaxX + 15.0);
                    });

                    if (matchingCol != null)
                    {
                        matchingCol.Add(line);
                    }
                    else
                    {
                        columnGroups.Add(new List<ExtractedPdfLine> { line });
                    }
                }

                // If multiple distinct columns were identified
                if (columnGroups.Count >= 2 && columnGroups.All(c => c.Count >= 2))
                {
                    var ordered = new List<ExtractedPdfLine>();

                    double topContentY = nonSpanLines.Max(l => l.Top);
                    var topHeaders = fullSpanHeaders.Where(h => h.Bottom >= topContentY - 10.0).OrderByDescending(h => h.Top).ToList();
                    var bottomFooters = fullSpanHeaders.Where(h => h.Bottom < topContentY - 10.0).OrderByDescending(h => h.Top).ToList();

                    ordered.AddRange(topHeaders);

                    // Sort column groups from left to right, and lines inside each column top-to-bottom
                    var sortedColumns = columnGroups.OrderBy(c => c.Min(l => l.Left)).ToList();
                    foreach (var col in sortedColumns)
                    {
                        ordered.AddRange(col.OrderByDescending(l => l.Top).ThenBy(l => l.Left));
                    }

                    ordered.AddRange(bottomFooters);
                    return ordered;
                }
            }
        }

        return lines.OrderByDescending(l => l.Top).ThenBy(l => l.Left).ToList();
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
        double expectedLineGap = Math.Max(7.0, prev.FontSize * 0.95);

        // If line gap is negative (overlapping) or greater than standard paragraph spacing, break
        if (verticalPitch < -2.0 || verticalPitch > expectedLineGap) return false;

        // 6. Paragraph Indentation: if next line starts indented to the right (>= 8pt), it marks a new paragraph
        if (next.Left > prev.Left + 8.0) return false;

        // 7. Horizontal column & margin check
        bool horizontalOverlap = (next.Left < prev.Right + 10) && (next.Right > prev.Left - 10);
        if (!horizontalOverlap) return false;

        // Check left margin alignment (within 14pt for justified text variations)
        double leftIndentDiff = Math.Abs(prev.Left - next.Left);
        if (leftIndentDiff > 14.0) return false;

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
        bool isRtl = UnicodeScriptDetector.IsRtlText(fullText);
        var alignment = isRtl ? TextAlignmentMode.Right : TextAlignmentMode.Left;

        // Convert PDF coordinates (origin bottom-left, Y goes up) to Canvas coordinates (origin top-left, Y goes down)
        double canvasX = Math.Max(0, minLeft);
        double canvasY = Math.Max(0, pageHeight - maxTop);
        // Add a small safety padding (4-6pt) to width & height so Avalonia text controls render without unexpected line-clipping
        double canvasWidth = Math.Max(30.0, (maxRight - minLeft) + 6.0);
        double canvasHeight = Math.Max(16.0, (maxTop - minBottom) + 4.0);

        var paraSpans = new List<PdfTextSpan>();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0 && paraSpans.Count > 0)
            {
                paraSpans.Last().Text += "\n";
            }
            if (lines[i].Spans != null && lines[i].Spans.Count > 0)
            {
                foreach (var s in lines[i].Spans)
                {
                    paraSpans.Add(s.Clone());
                }
            }
        }
        var normalizedParaSpans = NormalizeSpans(paraSpans);

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
            Alignment = alignment,
            CanvasX = Math.Round(canvasX, 1),
            CanvasY = Math.Round(canvasY, 1),
            CanvasWidth = Math.Round(canvasWidth, 1),
            CanvasHeight = Math.Round(canvasHeight, 1),
            Spans = normalizedParaSpans
        };
    }

    private static List<PdfTextSpan> NormalizeSpans(List<PdfTextSpan> spans)
    {
        if (spans.Count <= 1) return spans;

        var merged = new List<PdfTextSpan>(spans.Count);
        PdfTextSpan? current = null;

        foreach (var s in spans)
        {
            if (string.IsNullOrEmpty(s.Text)) continue;

            if (current == null)
            {
                current = s.Clone();
                continue;
            }

            if (string.Equals(current.FontFamily, s.FontFamily, StringComparison.OrdinalIgnoreCase) &&
                Nullable.Equals(current.FontSize, s.FontSize) &&
                current.IsBold == s.IsBold &&
                current.IsItalic == s.IsItalic &&
                string.Equals(current.TextColorHex, s.TextColorHex, StringComparison.OrdinalIgnoreCase))
            {
                current.Text += s.Text;
            }
            else
            {
                merged.Add(current);
                current = s.Clone();
            }
        }

        if (current != null)
        {
            merged.Add(current);
        }

        return merged;
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
        if (fn.Contains("luckiest") || fn.Contains("bungee") || fn.Contains("fredoka")) return "Poppins";
        if (fn.Contains("impact")) return "Oswald";
        if (fn.Contains("alfaslab") || fn.Contains("alfa slab")) return "Montserrat";

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
