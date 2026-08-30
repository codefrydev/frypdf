using System;
using System.IO;
using System.Text;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using UglyToad.PdfPig.Writer;

namespace PdfEditorApp.Services.Tools;

/// <summary>
/// Robust PDF file reader and sanitizer.
/// Gracefully opens modern, web-exported, linearized, or non-standard PDF files.
/// </summary>
public static class PdfFileHelper
{
    public static void SetFryPdfMetadata(
        PdfSharpCore.Pdf.PdfDocument doc,
        string? title = null,
        string? author = null,
        string? subject = null,
        string? keywords = null,
        string? creator = null,
        string? producer = null)
    {
        if (doc == null) return;
        if (!string.IsNullOrEmpty(title) && string.IsNullOrEmpty(doc.Info.Title)) doc.Info.Title = title;
        if (!string.IsNullOrEmpty(author) && string.IsNullOrEmpty(doc.Info.Author)) doc.Info.Author = author;
        if (!string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(doc.Info.Subject)) doc.Info.Subject = subject;
        if (!string.IsNullOrEmpty(keywords) && string.IsNullOrEmpty(doc.Info.Keywords)) doc.Info.Keywords = keywords;
        doc.Info.Creator = !string.IsNullOrWhiteSpace(creator) ? creator : "FryPDF";
        try
        {
            doc.Info.Elements.SetString("/Producer", !string.IsNullOrWhiteSpace(producer) ? producer : "codefrydev.in");
        }
        catch
        {
            try { doc.Info.Elements["/Producer"] = new PdfString(!string.IsNullOrWhiteSpace(producer) ? producer : "codefrydev.in"); } catch { }
        }
    }

    public static void SaveDocumentWithFryPdfMetadata(
        PdfSharpCore.Pdf.PdfDocument doc,
        string filePath,
        string? title = null,
        string? author = null,
        string? subject = null,
        string? keywords = null,
        string? creator = "FryPDF",
        string? producer = "codefrydev.in")
    {
        SetFryPdfMetadata(doc, title, author, subject, keywords, creator, producer);
        doc.Save(filePath);
        PatchProducerInFile(filePath, producer ?? "codefrydev.in");
    }

    public static void PatchProducerInFile(string filePath, string producer = "codefrydev.in")
    {
        if (!File.Exists(filePath)) return;
        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            byte[] updated = PatchProducerInBytes(bytes, producer);
            if (updated != null && updated.Length > 0)
            {
                File.WriteAllBytes(filePath, updated);
            }
        }
        catch { }
    }

    public static byte[] PatchProducerInBytes(byte[] bytes, string producer = "codefrydev.in")
    {
        if (bytes == null || bytes.Length < 20) return bytes ?? Array.Empty<byte>();

        try
        {
            string text = Encoding.ASCII.GetString(bytes);
            if (text.Contains("/Encrypt"))
            {
                // Never binary patch encrypted PDF streams
                return bytes;
            }

            int searchIdx = 0;
            while ((searchIdx = text.IndexOf("/Producer", searchIdx, StringComparison.Ordinal)) >= 0)
            {
                int openParen = text.IndexOf('(', searchIdx);
                if (openParen > searchIdx && openParen < searchIdx + 25)
                {
                    int closeParen = text.IndexOf(')', openParen);
                    if (closeParen > openParen)
                    {
                        int origSpanLen = closeParen - openParen + 1;
                        string replacement = $"({producer})";
                        if (replacement.Length <= origSpanLen)
                        {
                            string filler = "%".PadRight(origSpanLen - replacement.Length, ' ');
                            string fullPatch = replacement + filler;
                            byte[] patchBytes = Encoding.ASCII.GetBytes(fullPatch);
                            Array.Copy(patchBytes, 0, bytes, openParen, patchBytes.Length);
                        }
                    }
                }
                searchIdx += 9;
            }
        }
        catch { }

        return bytes;
    }

    public static PdfSharpCore.Pdf.PdfDocument OpenDocumentSafely(string filePath, PdfDocumentOpenMode mode = PdfDocumentOpenMode.Import, string? password = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        // 1. Direct open attempt
        try
        {
            if (string.IsNullOrEmpty(password))
                return PdfReader.Open(filePath, mode);
            else
                return PdfReader.Open(filePath, password, mode);
        }
        catch
        {
            // 2. Read bytes and sanitize trailing garbage or whitespace
            byte[] rawBytes = File.ReadAllBytes(filePath);
            byte[] sanitized = SanitizePdfBytes(rawBytes);

            try
            {
                var ms = new MemoryStream(sanitized);
                if (string.IsNullOrEmpty(password))
                    return PdfReader.Open(ms, mode);
                else
                    return PdfReader.Open(ms, password, mode);
            }
            catch
            {
                // 3. Fallback: Reconstruct using pure C# PdfPig builder (handles modern cross-reference streams and non-standard xrefs)
                try
                {
                    byte[] rebuilt = ReconstructCleanPdfWithPdfPig(filePath);
                    if (rebuilt.Length > 0)
                    {
                        var msRebuilt = new MemoryStream(rebuilt);
                        if (string.IsNullOrEmpty(password))
                            return PdfReader.Open(msRebuilt, mode);
                        else
                            return PdfReader.Open(msRebuilt, password, mode);
                    }
                }
                catch
                {
                    // Continue to next fallback
                }

                // 4. Fallback: synthesize trailer
                byte[] repaired = SalvageAndRepairPdfBytes(sanitized);
                var ms = new MemoryStream(repaired);
                if (string.IsNullOrEmpty(password))
                    return PdfReader.Open(ms, mode);
                else
                    return PdfReader.Open(ms, password, mode);
            }
        }
    }

    public static int InspectPageCountSafely(string filePath)
    {
        if (!File.Exists(filePath)) return 0;

        // 1. Try modern PdfPig parser (handles all PDF versions & formats)
        try
        {
            using var pig = UglyToad.PdfPig.PdfDocument.Open(filePath);
            return pig.NumberOfPages;
        }
        catch
        {
            // 2. Try OpenDocumentSafely
            try
            {
                using var doc = OpenDocumentSafely(filePath, PdfDocumentOpenMode.Import);
                return doc.PageCount;
            }
            catch
            {
                // 3. Regex token counting fallback
                try
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    string text = Encoding.ASCII.GetString(bytes);
                    int count = 0;
                    int idx = 0;
                    while ((idx = text.IndexOf("/Type /Page", idx, StringComparison.Ordinal)) != -1)
                    {
                        if (idx + 11 >= text.Length || text[idx + 11] != 's')
                        {
                            count++;
                        }
                        idx += 11;
                    }
                    return Math.Max(1, count);
                }
                catch
                {
                    return 1;
                }
            }
        }
    }

    public static byte[] ReconstructCleanPdfWithPdfPig(string filePath)
    {
        var builder = new PdfDocumentBuilder();
        using (var pigDoc = UglyToad.PdfPig.PdfDocument.Open(filePath))
        {
            for (int i = 1; i <= pigDoc.NumberOfPages; i++)
            {
                builder.AddPage(pigDoc, i);
            }
        }
        return builder.Build();
    }

    public static byte[] SanitizePdfBytes(byte[] rawBytes)
    {
        if (rawBytes == null || rawBytes.Length < 10) return rawBytes ?? Array.Empty<byte>();

        // Look for the last %%EOF token in the file
        string rawText = Encoding.ASCII.GetString(rawBytes, Math.Max(0, rawBytes.Length - 8192), Math.Min(rawBytes.Length, 8192));
        int eofIdx = rawText.LastIndexOf("%%EOF", StringComparison.OrdinalIgnoreCase);

        if (eofIdx >= 0)
        {
            int startOffset = Math.Max(0, rawBytes.Length - 8192);
            int eofAbsoluteEnd = startOffset + eofIdx + 5;

            if (eofAbsoluteEnd < rawBytes.Length)
            {
                var trimmed = new byte[eofAbsoluteEnd + 2];
                Array.Copy(rawBytes, 0, trimmed, 0, eofAbsoluteEnd);
                trimmed[eofAbsoluteEnd] = (byte)'\r';
                trimmed[eofAbsoluteEnd + 1] = (byte)'\n';
                return trimmed;
            }
        }
        else
        {
            // Missing %%EOF, append standard trailer terminator
            using var ms = new MemoryStream();
            ms.Write(rawBytes, 0, rawBytes.Length);
            byte[] terminator = Encoding.ASCII.GetBytes("\r\n%%EOF\r\n");
            ms.Write(terminator, 0, terminator.Length);
            return ms.ToArray();
        }

        return rawBytes;
    }

    public static byte[] SalvageAndRepairPdfBytes(byte[] rawBytes)
    {
        string fullText = Encoding.ASCII.GetString(rawBytes);
        int rootIdx = fullText.IndexOf("/Root", StringComparison.OrdinalIgnoreCase);
        if (rootIdx >= 0 && !fullText.Contains("trailer"))
        {
            int spaceIdx = fullText.IndexOf(" ", rootIdx + 5);
            string rootRef = "1 0 R";
            if (spaceIdx > 0 && spaceIdx + 10 < fullText.Length)
            {
                string snippet = fullText.Substring(rootIdx + 5, Math.Min(20, fullText.Length - (rootIdx + 5))).Trim();
                var parts = snippet.Split(new[] { ' ', '\r', '\n', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && parts[2] == "R")
                {
                    rootRef = $"{parts[0]} {parts[1]} R";
                }
            }

            using var ms = new MemoryStream();
            ms.Write(rawBytes, 0, rawBytes.Length);
            string synthTrailer = $"\r\ntrailer\r\n<<\r\n/Root {rootRef}\r\n>>\r\nstartxref\r\n0\r\n%%EOF\r\n";
            byte[] tBytes = Encoding.ASCII.GetBytes(synthTrailer);
            ms.Write(tBytes, 0, tBytes.Length);
            return ms.ToArray();
        }
        return rawBytes;
    }
}
