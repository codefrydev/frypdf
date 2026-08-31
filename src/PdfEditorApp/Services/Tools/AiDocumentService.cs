using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Models;
using UglyToad.PdfPig;

namespace PdfEditorApp.Services.Tools;

public interface IAiDocumentService
{
    Task<ToolExecutionResult> SummarizePdfAsync(AiSummaryOptions options, IProgress<double>? progress = null, CancellationToken ct = default);
}

public class AiDocumentService : IAiDocumentService
{
    public async Task<ToolExecutionResult> SummarizePdfAsync(AiSummaryOptions options, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(options.InputFilePath))
                return new ToolExecutionResult { Success = false, ErrorMessage = "Input PDF file does not exist." };

            long origBytes = new FileInfo(options.InputFilePath).Length;
            progress?.Report(15.0);

            ct.ThrowIfCancellationRequested();
            var allText = new StringBuilder();

            using (var pdf = UglyToad.PdfPig.PdfDocument.Open(options.InputFilePath))
            {
                int totalPages = pdf.NumberOfPages;
                for (int p = 1; p <= totalPages; p++)
                {
                    ct.ThrowIfCancellationRequested();
                    var page = pdf.GetPage(p);
                    allText.AppendLine(page.Text);
                    progress?.Report(15.0 + (p / (double)totalPages * 40.0));
                }
            }

            string fullDocText = allText.ToString();
            if (string.IsNullOrWhiteSpace(fullDocText))
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Unable to extract readable text for summarization. The document may be scanned or image-based."
                };
            }

            progress?.Report(65.0);
            ct.ThrowIfCancellationRequested();

            // Run Extractive NLP & Statistical Summarizer
            var summary = GenerateExtractiveSummary(fullDocText, options.MaxBulletPoints, options.IncludeExecutiveSummary, options.IncludeActionItems);
            progress?.Report(95.0);

            string outDir = Path.GetDirectoryName(options.InputFilePath) ?? Path.GetTempPath();
            string outPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(options.InputFilePath)}_Summary.md");
            File.WriteAllText(outPath, summary, Encoding.UTF8);

            progress?.Report(100.0);

            long outBytes = File.Exists(outPath) ? new FileInfo(outPath).Length : 0;
            return new ToolExecutionResult
            {
                Success = true,
                OutputFilePath = outPath,
                OutputFiles = new List<string> { outPath },
                OriginalSizeBytes = origBytes,
                OutputSizeBytes = outBytes,
                ExtraData = new Dictionary<string, object> { ["SummaryText"] = summary },
                Message = $"Generated executive summary and key points to {Path.GetFileName(outPath)}"
            };
        }, ct);
    }

    private static string GenerateExtractiveSummary(string text, int maxBullets, bool includeExec, bool includeActions)
    {
        var sentences = SplitIntoSentences(text);
        if (sentences.Count == 0) return "No content available to summarize.";

        // Word frequency map (excluding common stop words)
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with",
            "by", "from", "as", "is", "was", "are", "were", "be", "been", "have", "has", "had",
            "this", "that", "it", "they", "we", "you", "which", "will", "would", "can", "could",
            "should", "not", "also", "more", "their", "all", "its", "into"
        };

        var wordFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sentences)
        {
            var words = Regex.Matches(s, @"\b[A-Za-z]{3,}\b");
            foreach (Match m in words)
            {
                string w = m.Value;
                if (!stopWords.Contains(w))
                {
                    wordFreq[w] = wordFreq.GetValueOrDefault(w, 0) + 1;
                }
            }
        }

        // Score sentences based on word frequencies and position
        var scored = new List<(string Sentence, double Score, int Index)>();
        for (int i = 0; i < sentences.Count; i++)
        {
            string s = sentences[i];
            double score = 0;
            var words = Regex.Matches(s, @"\b[A-Za-z]{3,}\b");
            foreach (Match m in words)
            {
                if (wordFreq.TryGetValue(m.Value, out int count))
                {
                    score += count;
                }
            }

            // Normalization by sentence length (penalize extremely short or long sentences)
            if (words.Count > 0)
            {
                score /= Math.Sqrt(words.Count);
            }

            // Boost early sentences (introductions/abstracts)
            if (i < 5) score *= 1.3;

            scored.Add((s, score, i));
        }

        var topSentences = scored.OrderByDescending(x => x.Score)
                                 .Take(maxBullets)
                                 .OrderBy(x => x.Index)
                                 .Select(x => x.Sentence)
                                 .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("# AI Executive Document Summary");
        sb.AppendLine($"*Generated by FryPDF Intelligence Engine · {DateTime.UtcNow:yyyy-MM-dd}*");
        sb.AppendLine();

        if (includeExec && topSentences.Count > 0)
        {
            sb.AppendLine("## Executive Overview");
            sb.AppendLine(string.Join(" ", topSentences.Take(2)));
            sb.AppendLine();
        }

        sb.AppendLine("## Key Takeaways & Core Findings");
        foreach (var s in topSentences)
        {
            sb.AppendLine($"- **Key Point**: {s.Trim()}");
        }
        sb.AppendLine();

        if (includeActions)
        {
            var actionKeywords = new[] { "must", "should", "recommend", "require", "next steps", "action", "deadline", "implement", "review", "ensure" };
            var actionSentences = sentences.Where(s => actionKeywords.Any(k => s.Contains(k, StringComparison.OrdinalIgnoreCase)))
                                           .Take(4)
                                           .ToList();

            if (actionSentences.Count > 0)
            {
                sb.AppendLine("## Strategic Action Items");
                foreach (var a in actionSentences)
                {
                    sb.AppendLine($"1. {a.Trim()}");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static List<string> SplitIntoSentences(string text)
    {
        var list = new List<string>();
        var matches = Regex.Matches(text, @"(?<=[\.\!\?])\s+(?=[A-Z])");
        int lastIndex = 0;

        foreach (Match m in matches)
        {
            int length = m.Index - lastIndex;
            string s = text.Substring(lastIndex, length).Trim();
            if (s.Length > 20 && s.Length < 350)
            {
                list.Add(s);
            }
            lastIndex = m.Index + m.Length;
        }

        if (lastIndex < text.Length)
        {
            string s = text.Substring(lastIndex).Trim();
            if (s.Length > 20 && s.Length < 350) list.Add(s);
        }

        return list;
    }
}
