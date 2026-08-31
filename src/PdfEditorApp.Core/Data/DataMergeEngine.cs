using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Core.Data;

public class DataMergeEngine : IDataMergeEngine
{
    private static readonly Regex PlaceholderRegex = new(
        @"\{\{\s*([a-zA-Z0-9_\.\-]+)\s*(?:\:\s*([^\}\?]+?)\s*)?(?:\?\?\s*([^\}]+?)\s*)?\}\}",
        RegexOptions.Compiled);

    public IReadOnlyList<string> DetectPlaceholders(PdfDocumentModel template)
    {
        if (template == null) return Array.Empty<string>();

        var foundTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void ScanString(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var matches = PlaceholderRegex.Matches(text);
            foreach (Match m in matches)
            {
                if (m.Groups[1].Success)
                {
                    string tag = m.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(tag))
                    {
                        foundTags.Add(tag);
                    }
                }
            }
        }

        // 1. Document Metadata
        ScanString(template.Title);
        ScanString(template.Author);
        ScanString(template.Subject);
        ScanString(template.Keywords);

        // 2. Pages
        foreach (var page in template.Pages)
        {
            ScanString(page.HeaderLeft);
            ScanString(page.HeaderCenter);
            ScanString(page.HeaderRight);
            ScanString(page.FooterLeft);
            ScanString(page.FooterCenter);
            ScanString(page.FooterRight);

            if (page.Watermark != null)
            {
                ScanString(page.Watermark.Text);
            }

            foreach (var element in page.Elements)
            {
                switch (element)
                {
                    case PdfTextElement textEl:
                        ScanString(textEl.Text);
                        if (textEl.Spans != null)
                        {
                            foreach (var span in textEl.Spans)
                            {
                                ScanString(span.Text);
                            }
                        }
                        ScanString(textEl.TextColorHex);
                        ScanString(textEl.BackgroundColorHex);
                        break;

                    case PdfQrCodeElement qrEl:
                        ScanString(qrEl.Content);
                        ScanString(qrEl.Label);
                        break;

                    case PdfBarcodeElement barEl:
                        ScanString(barEl.CodeValue);
                        break;

                    case PdfImageElement imgEl:
                        ScanString(imgEl.ImagePath);
                        ScanString(imgEl.AltText);
                        break;

                    case PdfTableElement tableEl:
                        foreach (var header in tableEl.Headers)
                        {
                            ScanString(header);
                        }
                        foreach (var row in tableEl.Rows)
                        {
                            foreach (var cell in row)
                            {
                                ScanString(cell);
                            }
                        }
                        break;

                    case PdfChartElement chartEl:
                        ScanString(chartEl.Title);
                        foreach (var cat in chartEl.Categories)
                        {
                            ScanString(cat);
                        }
                        break;

                    case PdfShapeElement shapeEl:
                        ScanString(shapeEl.Label);
                        break;
                }
            }
        }

        return foundTags.OrderBy(t => t).ToList();
    }

    public PdfDocumentModel HydrateDocument(PdfDocumentModel template, IReadOnlyDictionary<string, string> record, DataMergeOptions? options = null)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        record ??= new Dictionary<string, string>();
        options ??= new DataMergeOptions();

        // 1. Deep clone template
        var doc = template.Clone();

        // 2. Hydrate Document Metadata
        doc.Title = EvaluateText(doc.Title, record, options);
        doc.Author = EvaluateText(doc.Author, record, options);
        doc.Subject = EvaluateText(doc.Subject, record, options);
        doc.Keywords = EvaluateText(doc.Keywords, record, options);
        doc.ModifiedDate = DateTime.Now;

        // 3. Hydrate Pages
        foreach (var page in doc.Pages)
        {
            page.HeaderLeft = EvaluateText(page.HeaderLeft, record, options);
            page.HeaderCenter = EvaluateText(page.HeaderCenter, record, options);
            page.HeaderRight = EvaluateText(page.HeaderRight, record, options);
            page.FooterLeft = EvaluateText(page.FooterLeft, record, options);
            page.FooterCenter = EvaluateText(page.FooterCenter, record, options);
            page.FooterRight = EvaluateText(page.FooterRight, record, options);

            if (page.Watermark != null)
            {
                page.Watermark.Text = EvaluateText(page.Watermark.Text, record, options);
            }

            foreach (var element in page.Elements)
            {
                HydrateElement(element, record, options);
            }
        }

        return doc;
    }

    private void HydrateElement(PdfElementBase element, IReadOnlyDictionary<string, string> record, DataMergeOptions options)
    {
        switch (element)
        {
            case PdfTextElement textEl:
                textEl.Text = EvaluateText(textEl.Text, record, options);
                if (textEl.Spans != null && textEl.Spans.Count > 0)
                {
                    foreach (var span in textEl.Spans)
                    {
                        span.Text = EvaluateText(span.Text, record, options);
                    }
                    textEl.SynchronizePlainTextFromSpans();
                }
                if (textEl.TextColorHex.Contains("{{"))
                {
                    string evaluatedColor = EvaluateText(textEl.TextColorHex, record, options);
                    if (evaluatedColor.StartsWith("#") && (evaluatedColor.Length == 7 || evaluatedColor.Length == 9))
                    {
                        textEl.TextColorHex = evaluatedColor;
                    }
                }
                if (textEl.BackgroundColorHex.Contains("{{"))
                {
                    string evaluatedBg = EvaluateText(textEl.BackgroundColorHex, record, options);
                    if (evaluatedBg.StartsWith("#") && (evaluatedBg.Length == 7 || evaluatedBg.Length == 9))
                    {
                        textEl.BackgroundColorHex = evaluatedBg;
                    }
                }
                break;

            case PdfQrCodeElement qrEl:
                qrEl.Content = EvaluateText(qrEl.Content, record, options);
                qrEl.Label = EvaluateText(qrEl.Label, record, options);
                break;

            case PdfBarcodeElement barEl:
                barEl.CodeValue = EvaluateText(barEl.CodeValue, record, options);
                break;

            case PdfImageElement imgEl:
                if (!string.IsNullOrEmpty(imgEl.ImagePath) && imgEl.ImagePath.Contains("{{"))
                {
                    string resolvedPath = EvaluateText(imgEl.ImagePath, record, options);
                    imgEl.ImagePath = resolvedPath;
                }
                if (!string.IsNullOrEmpty(imgEl.Base64Data) && imgEl.Base64Data.Contains("{{"))
                {
                    string resolvedBase64 = EvaluateText(imgEl.Base64Data, record, options);
                    imgEl.Base64Data = resolvedBase64;
                }
                imgEl.AltText = EvaluateText(imgEl.AltText, record, options);
                break;

            case PdfTableElement tableEl:
                for (int h = 0; h < tableEl.Headers.Count; h++)
                {
                    tableEl.Headers[h] = EvaluateText(tableEl.Headers[h], record, options);
                }
                for (int r = 0; r < tableEl.Rows.Count; r++)
                {
                    var row = tableEl.Rows[r];
                    for (int c = 0; c < row.Count; c++)
                    {
                        row[c] = EvaluateText(row[c], record, options);
                    }
                }
                break;

            case PdfChartElement chartEl:
                chartEl.Title = EvaluateText(chartEl.Title, record, options);
                for (int i = 0; i < chartEl.Categories.Count; i++)
                {
                    chartEl.Categories[i] = EvaluateText(chartEl.Categories[i], record, options);
                }
                break;

            case PdfShapeElement shapeEl:
                if (!string.IsNullOrEmpty(shapeEl.Label))
                {
                    shapeEl.Label = EvaluateText(shapeEl.Label, record, options);
                }
                break;
        }
    }

    public string EvaluateText(string? templateText, IReadOnlyDictionary<string, string> record, DataMergeOptions? options = null)
    {
        if (string.IsNullOrEmpty(templateText)) return string.Empty;
        if (!templateText.Contains("{{")) return templateText;

        options ??= new DataMergeOptions();

        return PlaceholderRegex.Replace(templateText, match =>
        {
            string fieldName = match.Groups[1].Value.Trim();
            string format = match.Groups[2].Success ? match.Groups[2].Value.Trim() : string.Empty;
            string fallback = match.Groups[3].Success ? match.Groups[3].Value.Trim() : options.DefaultFallbackValue;

            string? rawValue = FindRecordValue(record, fieldName, options.CaseInsensitiveLookup);

            if (string.IsNullOrEmpty(rawValue))
            {
                return !string.IsNullOrEmpty(fallback)
                    ? fallback
                    : (options.PreserveUnmatchedPlaceholders ? match.Value : string.Empty);
            }

            return FormatValue(rawValue, format);
        });
    }

    private static string? FindRecordValue(IReadOnlyDictionary<string, string> record, string fieldName, bool caseInsensitive)
    {
        if (record.TryGetValue(fieldName, out var directVal))
        {
            return directVal;
        }

        if (caseInsensitive)
        {
            foreach (var kvp in record)
            {
                if (string.Equals(kvp.Key, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }

                // Also check normalized name (stripping underscores, spaces, dashes)
                if (NormalizeFieldName(kvp.Key) == NormalizeFieldName(fieldName))
                {
                    return kvp.Value;
                }
            }
        }

        return null;
    }

    private static string NormalizeFieldName(string name)
    {
        return Regex.Replace(name.Trim().ToLowerInvariant(), @"[\s_\-\.]", "");
    }

    private static string FormatValue(string rawValue, string format)
    {
        if (string.IsNullOrEmpty(format)) return rawValue;

        // 1. Case Transforms
        switch (format.ToLowerInvariant())
        {
            case "upper":
            case "uppercase":
                return rawValue.ToUpperInvariant();

            case "lower":
            case "lowercase":
                return rawValue.ToLowerInvariant();

            case "title":
            case "titlecase":
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawValue.ToLowerInvariant());

            case "cap":
            case "capitalize":
                if (rawValue.Length == 0) return rawValue;
                return char.ToUpperInvariant(rawValue[0]) + (rawValue.Length > 1 ? rawValue.Substring(1) : "");
        }

        // 2. Currency Formatting: "C", "currency", "currency:INR", "currency:EUR", "$"
        if (format.StartsWith("currency", StringComparison.OrdinalIgnoreCase) ||
            format.Equals("C", StringComparison.OrdinalIgnoreCase) ||
            format.Equals("$", StringComparison.OrdinalIgnoreCase))
        {
            if (DataMatrix.TryParseNumeric(rawValue, out double amount))
            {
                if (format.Contains(':'))
                {
                    string currencyCode = format.Split(':')[1].Trim().ToUpperInvariant();
                    return currencyCode switch
                    {
                        "INR" or "RS" => $"₹{amount:N2}",
                        "EUR" => $"€{amount:N2}",
                        "GBP" => $"£{amount:N2}",
                        "JPY" => $"¥{amount:N0}",
                        _ => $"${amount:N2}"
                    };
                }
                return amount.ToString("C", CultureInfo.CurrentCulture);
            }
            return rawValue;
        }

        // 3. Numeric Formatting: "N0", "N2", "F2", "P1", etc.
        if (DataMatrix.TryParseNumeric(rawValue, out double numVal))
        {
            try
            {
                return numVal.ToString(format, CultureInfo.InvariantCulture);
            }
            catch
            {
                // Fallback
            }
        }

        // 4. Date Formatting: "yyyy-MM-dd", "MMM dd, yyyy", "dd/MM/yyyy", etc.
        if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
            DateTime.TryParse(rawValue, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
        {
            try
            {
                return dt.ToString(format, CultureInfo.InvariantCulture);
            }
            catch
            {
                // Fallback
            }
        }

        return rawValue;
    }
}
