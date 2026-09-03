using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Core.Utils;

/// <summary>
/// Embeds and extracts lossless native FryPDF document models (including tables, charts,
/// math formulas, barcodes, QR codes, layers, and vector shapes) directly inside standard PDF files.
/// </summary>
public static class FryPdfEmbeddingHelper
{
    public const string StartMarker = "%FRYPDF_MODEL_V1_START%";
    public const string EndMarker = "%FRYPDF_MODEL_V1_END%";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Embeds the serialized, compressed PdfDocumentModel into the PDF bytes as a valid PDF comment stream.
    /// The resulting PDF remains 100% compliant with standard PDF viewers (Acrobat, Chrome, Preview).
    /// </summary>
    public static byte[] EmbedModelInPdfBytes(byte[] pdfBytes, PdfDocumentModel model)
    {
        if (pdfBytes == null || pdfBytes.Length == 0 || model == null)
            return pdfBytes ?? Array.Empty<byte>();

        try
        {
            string json = JsonSerializer.Serialize(model, JsonOptions);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            byte[] compressedBytes;
            using (var ms = new MemoryStream())
            {
                using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                {
                    deflate.Write(jsonBytes, 0, jsonBytes.Length);
                }
                compressedBytes = ms.ToArray();
            }

            string base64 = Convert.ToBase64String(compressedBytes);

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(StartMarker);
            
            // Chunk base64 into 76-char lines formatted as valid PDF comments
            for (int i = 0; i < base64.Length; i += 76)
            {
                int len = Math.Min(76, base64.Length - i);
                sb.Append('%');
                sb.AppendLine(base64.Substring(i, len));
            }

            sb.AppendLine(EndMarker);
            sb.AppendLine("%%EOF");

            byte[] payloadBytes = Encoding.ASCII.GetBytes(sb.ToString());

            byte[] result = new byte[pdfBytes.Length + payloadBytes.Length];
            Buffer.BlockCopy(pdfBytes, 0, result, 0, pdfBytes.Length);
            Buffer.BlockCopy(payloadBytes, 0, result, pdfBytes.Length, payloadBytes.Length);

            return result;
        }
        catch
        {
            // If serialization fails, return unmodified PDF bytes
            return pdfBytes;
        }
    }

    /// <summary>
    /// Attempts to extract and deserialize an embedded FryPdfModel from the PDF bytes.
    /// Returns true if a valid, lossless model was successfully retrieved.
    /// </summary>
    public static bool TryExtractEmbeddedModel(byte[] pdfBytes, out PdfDocumentModel? model)
    {
        model = null;
        if (pdfBytes == null || pdfBytes.Length < 64) return false;

        try
        {
            // Look for StartMarker and EndMarker in the byte stream
            string content = Encoding.ASCII.GetString(pdfBytes);
            int startIdx = content.IndexOf(StartMarker, StringComparison.Ordinal);
            if (startIdx < 0) return false;

            int dataStart = startIdx + StartMarker.Length;
            int endIdx = content.IndexOf(EndMarker, dataStart, StringComparison.Ordinal);
            if (endIdx < 0) return false;

            string rawPayload = content.Substring(dataStart, endIdx - dataStart);

            // Clean lines and strip leading '%' comment characters
            var base64Sb = new StringBuilder(rawPayload.Length);
            using (var reader = new StringReader(rawPayload))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("%"))
                    {
                        trimmed = trimmed.Substring(1).Trim();
                    }
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        base64Sb.Append(trimmed);
                    }
                }
            }

            string base64 = base64Sb.ToString();
            if (string.IsNullOrWhiteSpace(base64)) return false;

            byte[] compressedBytes = Convert.FromBase64String(base64);

            byte[] jsonBytes;
            using (var inputMs = new MemoryStream(compressedBytes))
            using (var deflate = new DeflateStream(inputMs, CompressionMode.Decompress))
            using (var outputMs = new MemoryStream())
            {
                deflate.CopyTo(outputMs);
                jsonBytes = outputMs.ToArray();
            }

            string json = Encoding.UTF8.GetString(jsonBytes);
            var deserialized = JsonSerializer.Deserialize<PdfDocumentModel>(json, JsonOptions);

            if (deserialized != null && deserialized.Pages.Count > 0)
            {
                model = deserialized;
                return true;
            }
        }
        catch
        {
            // Ignore extraction errors and fallback to deconstruction
        }

        return false;
    }
}
