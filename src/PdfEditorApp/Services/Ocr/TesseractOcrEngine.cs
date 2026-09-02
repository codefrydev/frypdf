using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Services.Ocr;

/// <summary>
/// Cross-platform Tesseract OCR engine that utilizes downloaded .traineddata language packs.
/// Supports execution via native Tesseract CLI/runtime with TSV/HOCR parsing for exact word bounds.
/// </summary>
public class TesseractOcrEngine : IOcrEngine
{
    private readonly ITesseractModelService _modelService;

    public string EngineName => "Tesseract OCR";
    public OcrEngineType EngineType => OcrEngineType.Tesseract;
    public bool IsAvailable => true;

    public TesseractOcrEngine(ITesseractModelService modelService)
    {
        _modelService = modelService;
    }

    public async Task<OcrResult> RecognizeTextAsync(byte[] imageBytes, string language = "eng", CancellationToken ct = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            return new OcrResult { Success = false, ErrorMessage = "Empty image data provided." };
        }

        // Ensure the requested language pack is downloaded
        if (!_modelService.IsLanguageInstalled(language))
        {
            bool downloaded = await _modelService.DownloadLanguageAsync(language, null, null, ct);
            if (!downloaded)
            {
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = $"Language pack for '{language}' could not be downloaded."
                };
            }
        }

        string tessDataDir = _modelService.TessDataDirectory;
        return await Task.Run(() => RunTesseractProcess(imageBytes, language, tessDataDir, ct), ct);
    }

    private static OcrResult RunTesseractProcess(byte[] imageBytes, string language, string tessDataDir, CancellationToken ct)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "frypdf_ocr_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        string imagePath = Path.Combine(tempDir, "input.png");
        string outputBase = Path.Combine(tempDir, "output");
        string tsvPath = outputBase + ".tsv";

        try
        {
            File.WriteAllBytes(imagePath, imageBytes);

            // Locate tesseract executable
            string? tesseractExe = FindTesseractExecutable();
            if (string.IsNullOrEmpty(tesseractExe))
            {
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = "Tesseract executable was not found on the system PATH. Use OS Native OCR (Apple Vision / Windows Media) for zero-dependency OCR.",
                    EngineUsed = "Tesseract"
                };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = tesseractExe,
                Arguments = $"\"{imagePath}\" \"{outputBase}\" -l {language} --tessdata-dir \"{tessDataDir}\" tsv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new OcrResult { Success = false, ErrorMessage = "Failed to launch Tesseract process." };
            }

            process.WaitForExit(30000);

            if (!File.Exists(tsvPath))
            {
                string err = process.StandardError.ReadToEnd();
                return new OcrResult { Success = false, ErrorMessage = $"Tesseract did not produce output: {err}" };
            }

            return ParseTsvOutput(tsvPath, imageBytes);
        }
        catch (Exception ex)
        {
            return new OcrResult { Success = false, ErrorMessage = $"Tesseract execution error: {ex.Message}" };
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private static string? FindTesseractExecutable()
    {
        string[] candidates = OperatingSystem.IsWindows()
            ? new[] { "tesseract.exe", @"C:\Program Files\Tesseract-OCR\tesseract.exe", @"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe" }
            : new[] { "tesseract", "/opt/homebrew/bin/tesseract", "/usr/local/bin/tesseract", "/usr/bin/tesseract" };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate)) return candidate;
        }

        // Try 'which' / 'where'
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "where" : "which",
                Arguments = "tesseract",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string path = p.StandardOutput.ReadLine()?.Trim() ?? "";
                p.WaitForExit(2000);
                if (File.Exists(path)) return path;
            }
        }
        catch { }

        return null;
    }

    private static OcrResult ParseTsvOutput(string tsvPath, byte[] imageBytes)
    {
        var lines = new List<OcrLineItem>();
        var words = new List<OcrWordItem>();
        var fullText = new StringBuilder();

        // Approximate image dimensions or default 1000x1000
        double imgWidth = 1000;
        double imgHeight = 1000;

        string[] tsvLines = File.ReadAllLines(tsvPath);
        if (tsvLines.Length <= 1)
        {
            return new OcrResult { Success = true, FullText = string.Empty, Lines = lines, Words = words };
        }

        // TSV format: level, page_num, block_num, par_num, line_num, word_num, left, top, width, height, conf, text
        // Header is line 0
        OcrLineItem? currentLine = null;
        int lastLineNum = -1;

        for (int i = 1; i < tsvLines.Length; i++)
        {
            string line = tsvLines[i];
            string[] cols = line.Split('\t');
            if (cols.Length < 12) continue;

            int level = int.TryParse(cols[0], out var lv) ? lv : 0;
            if (level == 1) // Page level
            {
                if (double.TryParse(cols[8], NumberStyles.Any, CultureInfo.InvariantCulture, out var pw) && pw > 0)
                    imgWidth = pw;
                if (double.TryParse(cols[9], NumberStyles.Any, CultureInfo.InvariantCulture, out var ph) && ph > 0)
                    imgHeight = ph;
                continue;
            }

            if (level != 5) continue; // Word level

            string text = cols[11].Trim();
            if (string.IsNullOrEmpty(text)) continue;

            double left = double.TryParse(cols[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var l) ? l : 0;
            double top = double.TryParse(cols[7], NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0;
            double width = double.TryParse(cols[8], NumberStyles.Any, CultureInfo.InvariantCulture, out var w) ? w : 0;
            double height = double.TryParse(cols[9], NumberStyles.Any, CultureInfo.InvariantCulture, out var h) ? h : 0;
            float conf = float.TryParse(cols[10], NumberStyles.Any, CultureInfo.InvariantCulture, out var c) ? c / 100f : 1.0f;
            int lineNum = int.TryParse(cols[4], out var ln) ? ln : 0;

            var normBox = new OcrBoundingBox(left / imgWidth, top / imgHeight, width / imgWidth, height / imgHeight);
            var wordItem = new OcrWordItem
            {
                Text = text,
                NormalizedBounds = normBox,
                Confidence = conf
            };

            words.Add(wordItem);

            if (currentLine == null || lineNum != lastLineNum)
            {
                currentLine = new OcrLineItem
                {
                    Text = text,
                    NormalizedBounds = normBox
                };
                currentLine.Words.Add(wordItem);
                lines.Add(currentLine);
                lastLineNum = lineNum;
            }
            else
            {
                currentLine.Text += " " + text;
                currentLine.Words.Add(wordItem);
                // Union bounding box
                double nx = Math.Min(currentLine.NormalizedBounds.X, normBox.X);
                double ny = Math.Min(currentLine.NormalizedBounds.Y, normBox.Y);
                double nr = Math.Max(currentLine.NormalizedBounds.Right, normBox.Right);
                double nb = Math.Max(currentLine.NormalizedBounds.Bottom, normBox.Bottom);
                currentLine.NormalizedBounds = new OcrBoundingBox(nx, ny, nr - nx, nb - ny);
            }

            fullText.Append(text).Append(' ');
        }

        return new OcrResult
        {
            Success = true,
            FullText = fullText.ToString().Trim(),
            Lines = lines,
            Words = words,
            EngineUsed = "Tesseract OCR"
        };
    }
}
