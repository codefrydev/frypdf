using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Services.Typography;

/// <summary>
/// High-performance parser and serializer for rich text formatting.
/// Translates between inline Markdown/HTML tags, plain text, and structured <see cref="PdfTextSpan"/> runs.
/// </summary>
public static class RichTextHelper
{
    private static readonly Regex ColorTagRegex = new(@"<color\s*=\s*(?<col>[#\w\d]+)>(?<content>.*?)</color>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex UnderlineTagRegex = new(@"<u>(?<content>.*?)</u>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex LinkTagRegex = new(@"\[(?<label>[^\]]+)\]\((?<url>[^\)]+)\)", RegexOptions.Compiled);

    /// <summary>
    /// Parses an inline formatted string containing Markdown / HTML tag notation into structured <see cref="PdfTextSpan"/> items.
    /// Supports **bold**, *italic*, ~~strikethrough~~, <u>underline</u>, ^superscript^, ~subscript~, &lt;color=#HEX&gt;text&lt;/color&gt;, and [label](url).
    /// </summary>
    public static List<PdfTextSpan> ParseMarkdownToSpans(string? input, PdfTextElement? baseStyle = null)
    {
        var spans = new List<PdfTextSpan>();
        if (string.IsNullOrEmpty(input))
        {
            return spans;
        }

        // Fast path: If no markdown/tag tokens exist, return single span with plain text
        if (!ContainsFormattingTokens(input))
        {
            spans.Add(new PdfTextSpan
            {
                Text = input,
                FontFamily = baseStyle?.FontFamily,
                FontSize = baseStyle?.FontSize,
                IsBold = baseStyle?.IsBold,
                IsItalic = baseStyle?.IsItalic,
                IsUnderline = baseStyle?.IsUnderline,
                IsStrikethrough = baseStyle?.IsStrikethrough,
                TextColorHex = baseStyle?.TextColorHex
            });
            return spans;
        }

        // Token-based recursive/stack parser for nested rich text
        ParseFormattedString(input, spans, baseStyle, new SpanStyleState
        {
            FontFamily = baseStyle?.FontFamily,
            FontSize = baseStyle?.FontSize,
            IsBold = baseStyle?.IsBold ?? false,
            IsItalic = baseStyle?.IsItalic ?? false,
            IsUnderline = baseStyle?.IsUnderline ?? false,
            IsStrikethrough = baseStyle?.IsStrikethrough ?? false,
            TextColorHex = baseStyle?.TextColorHex,
            Script = TextScriptMode.Normal
        });

        return NormalizeSpans(spans);
    }

    private static bool ContainsFormattingTokens(string text)
    {
        return text.Contains("**") || text.Contains('*') ||
               text.Contains("~~") || text.Contains("<u>") || text.Contains("<color") ||
               text.Contains('^') || text.Contains('~') || (text.Contains('[') && text.Contains('('));
    }

    private struct SpanStyleState
    {
        public string? FontFamily;
        public double? FontSize;
        public bool IsBold;
        public bool IsItalic;
        public bool IsUnderline;
        public bool IsStrikethrough;
        public string? TextColorHex;
        public string? HighlightColorHex;
        public TextScriptMode Script;
        public string? LinkUrl;

        public SpanStyleState Clone() => (SpanStyleState)MemberwiseClone();
    }

    private static void ParseFormattedString(
        string text,
        List<PdfTextSpan> output,
        PdfTextElement? baseStyle,
        SpanStyleState currentStyle)
    {
        int pos = 0;
        int len = text.Length;
        var buffer = new StringBuilder();

        void FlushBuffer()
        {
            if (buffer.Length > 0)
            {
                output.Add(new PdfTextSpan
                {
                    Text = buffer.ToString(),
                    FontFamily = currentStyle.FontFamily ?? baseStyle?.FontFamily,
                    FontSize = currentStyle.FontSize ?? baseStyle?.FontSize,
                    IsBold = currentStyle.IsBold,
                    IsItalic = currentStyle.IsItalic,
                    IsUnderline = currentStyle.IsUnderline,
                    IsStrikethrough = currentStyle.IsStrikethrough,
                    TextColorHex = currentStyle.TextColorHex ?? baseStyle?.TextColorHex,
                    HighlightColorHex = currentStyle.HighlightColorHex,
                    Script = currentStyle.Script,
                    LinkUrl = currentStyle.LinkUrl
                });
                buffer.Clear();
            }
        }

        while (pos < len)
        {
            // 1. Color tag <color=#HEX>...</color>
            if (text.AsSpan(pos).StartsWith("<color", StringComparison.OrdinalIgnoreCase))
            {
                int tagClose = text.IndexOf('>', pos);
                if (tagClose > pos)
                {
                    string openTag = text.Substring(pos, tagClose - pos + 1);
                    int colEqual = openTag.IndexOf('=');
                    if (colEqual > 0)
                    {
                        string colorVal = openTag.Substring(colEqual + 1).Trim(' ', '>', '"', '\'');
                        int endTag = text.IndexOf("</color>", tagClose + 1, StringComparison.OrdinalIgnoreCase);
                        if (endTag > tagClose)
                        {
                            FlushBuffer();
                            string inner = text.Substring(tagClose + 1, endTag - tagClose - 1);
                            var nextStyle = currentStyle.Clone();
                            nextStyle.TextColorHex = NormalizeHexColor(colorVal);
                            ParseFormattedString(inner, output, baseStyle, nextStyle);
                            pos = endTag + "</color>".Length;
                            continue;
                        }
                    }
                }
            }

            // 1b. Highlight/Mark tag <mark=#HEX>...</mark> or <highlight=#HEX>...</highlight>
            if (text.AsSpan(pos).StartsWith("<mark", StringComparison.OrdinalIgnoreCase) || text.AsSpan(pos).StartsWith("<highlight", StringComparison.OrdinalIgnoreCase))
            {
                bool isMark = text.AsSpan(pos).StartsWith("<mark", StringComparison.OrdinalIgnoreCase);
                string closeTagName = isMark ? "</mark>" : "</highlight>";
                int tagClose = text.IndexOf('>', pos);
                if (tagClose > pos)
                {
                    string openTag = text.Substring(pos, tagClose - pos + 1);
                    int colEqual = openTag.IndexOf('=');
                    string highlightCol = colEqual > 0 ? NormalizeHexColor(openTag.Substring(colEqual + 1).Trim(' ', '>', '"', '\'')) : "#FFEB3B";
                    int endTag = text.IndexOf(closeTagName, tagClose + 1, StringComparison.OrdinalIgnoreCase);
                    if (endTag > tagClose)
                    {
                        FlushBuffer();
                        string inner = text.Substring(tagClose + 1, endTag - tagClose - 1);
                        var nextStyle = currentStyle.Clone();
                        nextStyle.HighlightColorHex = highlightCol;
                        ParseFormattedString(inner, output, baseStyle, nextStyle);
                        pos = endTag + closeTagName.Length;
                        continue;
                    }
                }
            }

            // 2. Underline tag <u>...</u>
            if (text.AsSpan(pos).StartsWith("<u>", StringComparison.OrdinalIgnoreCase))
            {
                int endTag = text.IndexOf("</u>", pos + 3, StringComparison.OrdinalIgnoreCase);
                if (endTag > pos)
                {
                    FlushBuffer();
                    string inner = text.Substring(pos + 3, endTag - pos - 3);
                    var nextStyle = currentStyle.Clone();
                    nextStyle.IsUnderline = true;
                    ParseFormattedString(inner, output, baseStyle, nextStyle);
                    pos = endTag + 4;
                    continue;
                }
            }

            // 3. Hyperlink [label](url)
            if (text[pos] == '[')
            {
                int labelEnd = text.IndexOf(']', pos + 1);
                if (labelEnd > pos && labelEnd + 1 < len && text[labelEnd + 1] == '(')
                {
                    int urlEnd = text.IndexOf(')', labelEnd + 2);
                    if (urlEnd > labelEnd)
                    {
                        FlushBuffer();
                        string label = text.Substring(pos + 1, labelEnd - pos - 1);
                        string url = text.Substring(labelEnd + 2, urlEnd - labelEnd - 2);
                        var nextStyle = currentStyle.Clone();
                        nextStyle.LinkUrl = url;
                        nextStyle.IsUnderline = true;
                        if (string.IsNullOrEmpty(nextStyle.TextColorHex))
                        {
                            nextStyle.TextColorHex = "#0F6CBD";
                        }
                        ParseFormattedString(label, output, baseStyle, nextStyle);
                        pos = urlEnd + 1;
                        continue;
                    }
                }
            }

            // 4. Bold **...**
            if (pos + 1 < len && text[pos] == '*' && text[pos + 1] == '*')
            {
                int closing = text.IndexOf("**", pos + 2, StringComparison.Ordinal);
                if (closing > pos + 1)
                {
                    FlushBuffer();
                    string inner = text.Substring(pos + 2, closing - pos - 2);
                    var nextStyle = currentStyle.Clone();
                    nextStyle.IsBold = true;
                    ParseFormattedString(inner, output, baseStyle, nextStyle);
                    pos = closing + 2;
                    continue;
                }
            }

            // 5. Strikethrough ~~...~~
            if (pos + 1 < len && text[pos] == '~' && text[pos + 1] == '~')
            {
                int closing = text.IndexOf("~~", pos + 2, StringComparison.Ordinal);
                if (closing > pos + 1)
                {
                    FlushBuffer();
                    string inner = text.Substring(pos + 2, closing - pos - 2);
                    var nextStyle = currentStyle.Clone();
                    nextStyle.IsStrikethrough = true;
                    ParseFormattedString(inner, output, baseStyle, nextStyle);
                    pos = closing + 2;
                    continue;
                }
            }

            // 6. Subscript ~...~ (single tilde)
            if (text[pos] == '~')
            {
                int closing = text.IndexOf('~', pos + 1);
                if (closing > pos)
                {
                    FlushBuffer();
                    string inner = text.Substring(pos + 1, closing - pos - 1);
                    var nextStyle = currentStyle.Clone();
                    nextStyle.Script = TextScriptMode.Subscript;
                    ParseFormattedString(inner, output, baseStyle, nextStyle);
                    pos = closing + 1;
                    continue;
                }
            }

            // 7. Superscript ^...^
            if (text[pos] == '^')
            {
                int closing = text.IndexOf('^', pos + 1);
                if (closing > pos)
                {
                    FlushBuffer();
                    string inner = text.Substring(pos + 1, closing - pos - 1);
                    var nextStyle = currentStyle.Clone();
                    nextStyle.Script = TextScriptMode.Superscript;
                    ParseFormattedString(inner, output, baseStyle, nextStyle);
                    pos = closing + 1;
                    continue;
                }
            }

            // 8. Italic *...*
            if (text[pos] == '*')
            {
                int closing = text.IndexOf('*', pos + 1);
                if (closing > pos)
                {
                    FlushBuffer();
                    string inner = text.Substring(pos + 1, closing - pos - 1);
                    var nextStyle = currentStyle.Clone();
                    nextStyle.IsItalic = true;
                    ParseFormattedString(inner, output, baseStyle, nextStyle);
                    pos = closing + 1;
                    continue;
                }
            }

            // Regular character
            buffer.Append(text[pos]);
            pos++;
        }

        FlushBuffer();
    }

    private static string NormalizeHexColor(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "#201F1E";
        input = input.Trim();
        if (input.StartsWith("#")) return input;

        // Named CSS color aliases
        return input.ToLowerInvariant() switch
        {
            "red" => "#DC2626",
            "blue" => "#0F6CBD",
            "green" => "#16A34A",
            "yellow" => "#CA8A04",
            "orange" => "#EA580C",
            "purple" => "#9333EA",
            "black" => "#000000",
            "white" => "#FFFFFF",
            "gray" or "grey" => "#6B7280",
            _ => "#" + input
        };
    }

    /// <summary>
    /// Reconstructs a clean Markdown/HTML string representation from structured spans.
    /// Useful for feeding the in-place text editor.
    /// </summary>
    public static string SpansToMarkdown(IReadOnlyList<PdfTextSpan>? spans, PdfTextElement? baseStyle = null)
    {
        if (spans == null || spans.Count == 0)
        {
            return baseStyle?.Text ?? string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var span in spans)
        {
            if (string.IsNullOrEmpty(span.Text)) continue;

            string fragment = span.Text;

            // Check script
            if (span.Script == TextScriptMode.Superscript)
            {
                fragment = $"^{fragment}^";
            }
            else if (span.Script == TextScriptMode.Subscript)
            {
                fragment = $"~{fragment}~";
            }

            // Check strikethrough
            if (span.IsStrikethrough == true && baseStyle?.IsStrikethrough != true)
            {
                fragment = $"~~{fragment}~~";
            }

            // Check underline
            if (span.IsUnderline == true && baseStyle?.IsUnderline != true && string.IsNullOrEmpty(span.LinkUrl))
            {
                fragment = $"<u>{fragment}</u>";
            }

            // Check italic
            if (span.IsItalic == true && baseStyle?.IsItalic != true)
            {
                fragment = $"*{fragment}*";
            }

            // Check bold
            if (span.IsBold == true && baseStyle?.IsBold != true)
            {
                fragment = $"**{fragment}**";
            }

            // Check custom color
            if (!string.IsNullOrEmpty(span.TextColorHex) &&
                !string.Equals(span.TextColorHex, baseStyle?.TextColorHex, StringComparison.OrdinalIgnoreCase))
            {
                fragment = $"<color={span.TextColorHex}>{fragment}</color>";
            }

            // Check hyperlink
            if (!string.IsNullOrEmpty(span.LinkUrl))
            {
                fragment = $"[{fragment}]({span.LinkUrl})";
            }

            sb.Append(fragment);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Extracts raw plain text without any markup tokens from a list of spans.
    /// </summary>
    public static string SpansToPlainText(IEnumerable<PdfTextSpan>? spans)
    {
        if (spans == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var span in spans)
        {
            sb.Append(span.Text);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Merges adjacent spans with identical typographic attributes into single unified spans.
    /// </summary>
    public static List<PdfTextSpan> NormalizeSpans(List<PdfTextSpan> spans)
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

            if (CanMergeSpans(current, s))
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

    private static bool CanMergeSpans(PdfTextSpan a, PdfTextSpan b)
    {
        return string.Equals(a.FontFamily, b.FontFamily, StringComparison.OrdinalIgnoreCase) &&
               Nullable.Equals(a.FontSize, b.FontSize) &&
               a.IsBold == b.IsBold &&
               a.IsItalic == b.IsItalic &&
               a.IsUnderline == b.IsUnderline &&
               a.IsStrikethrough == b.IsStrikethrough &&
               string.Equals(a.TextColorHex, b.TextColorHex, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(a.HighlightColorHex, b.HighlightColorHex, StringComparison.OrdinalIgnoreCase) &&
               a.Script == b.Script &&
               string.Equals(a.LinkUrl, b.LinkUrl, StringComparison.OrdinalIgnoreCase);
    }
}
