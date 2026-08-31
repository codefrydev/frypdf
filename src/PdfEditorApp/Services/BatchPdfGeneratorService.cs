using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Data;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services;

public class BatchPdfGeneratorService : IBatchPdfGenerator
{
    private readonly IDataMergeEngine _mergeEngine;
    private readonly IPdfExportService _exportService;

    public BatchPdfGeneratorService(IDataMergeEngine mergeEngine, IPdfExportService exportService)
    {
        _mergeEngine = mergeEngine ?? throw new ArgumentNullException(nameof(mergeEngine));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
    }

    public async Task<BatchGenerationResult> GenerateBatchAsync(
        PdfDocumentModel template,
        DataMatrix matrix,
        IReadOnlyList<FieldMappingItem>? mappings,
        BatchGenerationConfig config,
        IProgress<BatchProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));
        if (matrix == null) throw new ArgumentNullException(nameof(matrix));
        if (config == null) throw new ArgumentNullException(nameof(config));

        var stopwatch = Stopwatch.StartNew();
        var result = new BatchGenerationResult();

        int totalRows = matrix.RowCount;
        if (config.MaxRecordCount.HasValue && config.MaxRecordCount.Value > 0)
        {
            totalRows = Math.Min(totalRows, config.MaxRecordCount.Value);
        }

        if (totalRows == 0)
        {
            stopwatch.Stop();
            result.ElapsedTime = stopwatch.Elapsed;
            return result;
        }

        // Ensure output directories exist
        if (!string.IsNullOrEmpty(config.OutputDirectory) && !Directory.Exists(config.OutputDirectory))
        {
            Directory.CreateDirectory(config.OutputDirectory);
        }

        if (config.OutputMode != BatchOutputMode.SeparateFiles && !string.IsNullOrEmpty(config.OutputFilePath))
        {
            string? dir = Path.GetDirectoryName(config.OutputFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        await Task.Run(() =>
        {
            if (config.OutputMode == BatchOutputMode.SingleMergedPdf)
            {
                GenerateSingleMergedPdf(template, matrix, mappings, config, totalRows, result, progress, cancellationToken);
            }
            else if (config.OutputMode == BatchOutputMode.ZipArchive)
            {
                GenerateZipArchive(template, matrix, mappings, config, totalRows, result, progress, cancellationToken);
            }
            else
            {
                GenerateSeparateFiles(template, matrix, mappings, config, totalRows, result, progress, cancellationToken);
            }
        }, cancellationToken);

        stopwatch.Stop();
        result.ElapsedTime = stopwatch.Elapsed;
        return result;
    }

    private void GenerateSeparateFiles(
        PdfDocumentModel template,
        DataMatrix matrix,
        IReadOnlyList<FieldMappingItem>? mappings,
        BatchGenerationConfig config,
        int totalRows,
        BatchGenerationResult result,
        IProgress<BatchProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        string outDir = string.IsNullOrWhiteSpace(config.OutputDirectory) ? Directory.GetCurrentDirectory() : config.OutputDirectory;

        for (int r = 0; r < totalRows; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = BuildRecordDictionary(matrix, r, mappings);
            if (config.SkipEmptyRows && record.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            result.TotalProcessed++;

            try
            {
                // Hydrate template
                var hydratedDoc = _mergeEngine.HydrateDocument(template, record);

                // Determine dynamic filename
                string filename = ResolveFilename(config.FilenamePattern, record, r + 1);
                string filePath = Path.Combine(outDir, filename);

                // Handle unique filename collisions
                filePath = EnsureUniqueFilePath(filePath);

                // Export to PDF
                byte[] pdfBytes = _exportService.GeneratePdfBytes(hydratedDoc);
                File.WriteAllBytes(filePath, pdfBytes);

                result.GeneratedFiles.Add(filePath);
                result.SuccessfulCount++;

                progress?.Report(new BatchProgressReport
                {
                    CurrentIndex = r + 1,
                    TotalCount = totalRows,
                    CurrentItemName = Path.GetFileName(filePath),
                    SucceededCount = result.SuccessfulCount,
                    FailedCount = result.FailedCount,
                    StatusMessage = $"Generated {Path.GetFileName(filePath)} ({r + 1}/{totalRows})"
                });
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add((r, ex.Message));

                progress?.Report(new BatchProgressReport
                {
                    CurrentIndex = r + 1,
                    TotalCount = totalRows,
                    CurrentItemName = $"Row {r + 1}",
                    SucceededCount = result.SuccessfulCount,
                    FailedCount = result.FailedCount,
                    StatusMessage = $"Error on row {r + 1}: {ex.Message}"
                });
            }
        }
    }

    private void GenerateSingleMergedPdf(
        PdfDocumentModel template,
        DataMatrix matrix,
        IReadOnlyList<FieldMappingItem>? mappings,
        BatchGenerationConfig config,
        int totalRows,
        BatchGenerationResult result,
        IProgress<BatchProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        string targetFilePath = !string.IsNullOrEmpty(config.OutputFilePath)
            ? config.OutputFilePath
            : Path.Combine(string.IsNullOrWhiteSpace(config.OutputDirectory) ? Directory.GetCurrentDirectory() : config.OutputDirectory, "Merged_Batch_Output.pdf");

        var mergedDoc = template.Clone();
        mergedDoc.Pages.Clear();
        int globalPageNum = 1;

        for (int r = 0; r < totalRows; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = BuildRecordDictionary(matrix, r, mappings);
            if (config.SkipEmptyRows && record.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            result.TotalProcessed++;

            try
            {
                var hydratedDoc = _mergeEngine.HydrateDocument(template, record);
                foreach (var page in hydratedDoc.Pages)
                {
                    var pageClone = page.Clone();
                    pageClone.PageNumber = globalPageNum++;
                    mergedDoc.Pages.Add(pageClone);
                }

                result.SuccessfulCount++;

                progress?.Report(new BatchProgressReport
                {
                    CurrentIndex = r + 1,
                    TotalCount = totalRows,
                    CurrentItemName = $"Record {r + 1}",
                    SucceededCount = result.SuccessfulCount,
                    FailedCount = result.FailedCount,
                    StatusMessage = $"Prepared record {r + 1} of {totalRows}"
                });
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add((r, ex.Message));
            }
        }

        if (mergedDoc.Pages.Count > 0)
        {
            byte[] pdfBytes = _exportService.GeneratePdfBytes(mergedDoc);
            File.WriteAllBytes(targetFilePath, pdfBytes);
            result.OutputMergedFilePath = targetFilePath;
            result.GeneratedFiles.Add(targetFilePath);
        }
    }

    private void GenerateZipArchive(
        PdfDocumentModel template,
        DataMatrix matrix,
        IReadOnlyList<FieldMappingItem>? mappings,
        BatchGenerationConfig config,
        int totalRows,
        BatchGenerationResult result,
        IProgress<BatchProgressReport>? progress,
        CancellationToken cancellationToken)
    {
        string zipFilePath = !string.IsNullOrEmpty(config.OutputFilePath)
            ? config.OutputFilePath
            : Path.Combine(string.IsNullOrWhiteSpace(config.OutputDirectory) ? Directory.GetCurrentDirectory() : config.OutputDirectory, "Batch_PDFs_Archive.zip");

        using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
        {
            var addedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = 0; r < totalRows; r++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var record = BuildRecordDictionary(matrix, r, mappings);
                if (config.SkipEmptyRows && record.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                result.TotalProcessed++;

                try
                {
                    var hydratedDoc = _mergeEngine.HydrateDocument(template, record);
                    string filename = ResolveFilename(config.FilenamePattern, record, r + 1);

                    // Ensure unique name in zip
                    int counter = 1;
                    string entryName = filename;
                    string nameNoExt = Path.GetFileNameWithoutExtension(filename);
                    string ext = Path.GetExtension(filename);

                    while (addedNames.Contains(entryName))
                    {
                        entryName = $"{nameNoExt}_{counter++}{ext}";
                    }
                    addedNames.Add(entryName);

                    byte[] pdfBytes = _exportService.GeneratePdfBytes(hydratedDoc);
                    var entry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using (var entryStream = entry.Open())
                    {
                        entryStream.Write(pdfBytes, 0, pdfBytes.Length);
                    }

                    result.SuccessfulCount++;

                    progress?.Report(new BatchProgressReport
                    {
                        CurrentIndex = r + 1,
                        TotalCount = totalRows,
                        CurrentItemName = entryName,
                        SucceededCount = result.SuccessfulCount,
                        FailedCount = result.FailedCount,
                        StatusMessage = $"Archived {entryName} ({r + 1}/{totalRows})"
                    });
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.Errors.Add((r, ex.Message));
                }
            }
        }

        result.OutputZipFilePath = zipFilePath;
        result.GeneratedFiles.Add(zipFilePath);
    }

    public static Dictionary<string, string> BuildRecordDictionary(DataMatrix matrix, int rowIndex, IReadOnlyList<FieldMappingItem>? mappings)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 1. First add all direct matrix columns
        for (int c = 0; c < matrix.ColumnCount; c++)
        {
            string header = matrix.Headers[c];
            string val = matrix.GetCellValue(rowIndex, c);
            dict[header] = val;
        }

        // Add standard systemic variables
        dict["Index"] = (rowIndex + 1).ToString();
        dict["RowNumber"] = (rowIndex + 1).ToString();
        dict["TotalRecords"] = matrix.RowCount.ToString();
        dict["CurrentDate"] = DateTime.Now.ToString("yyyy-MM-dd");
        dict["CurrentYear"] = DateTime.Now.Year.ToString();
        dict["CurrentMonth"] = DateTime.Now.ToString("MMMM");

        // 2. Overlay custom mappings
        if (mappings != null)
        {
            foreach (var m in mappings)
            {
                if (string.IsNullOrWhiteSpace(m.PlaceholderTag)) continue;

                string val = string.Empty;
                if (!string.IsNullOrWhiteSpace(m.DataColumnName) && dict.TryGetValue(m.DataColumnName, out var mappedVal))
                {
                    val = mappedVal;
                }

                if (string.IsNullOrWhiteSpace(val) && !string.IsNullOrWhiteSpace(m.DefaultValue))
                {
                    val = m.DefaultValue;
                }

                // Apply transform
                val = ApplyFieldTransform(val, m.Transform, m.CustomFormat);

                dict[m.PlaceholderTag] = val;
            }
        }

        return dict;
    }

    private static string ApplyFieldTransform(string val, FieldTransformType transform, string customFormat)
    {
        if (string.IsNullOrEmpty(val)) return val;

        switch (transform)
        {
            case FieldTransformType.Uppercase:
                return val.ToUpperInvariant();

            case FieldTransformType.Lowercase:
                return val.ToLowerInvariant();

            case FieldTransformType.TitleCase:
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(val.ToLowerInvariant());

            case FieldTransformType.Currency:
                if (DataMatrix.TryParseNumeric(val, out double currNum))
                {
                    return currNum.ToString("C", CultureInfo.CurrentCulture);
                }
                break;

            case FieldTransformType.Numeric:
                if (DataMatrix.TryParseNumeric(val, out double numVal))
                {
                    string fmt = string.IsNullOrWhiteSpace(customFormat) ? "N2" : customFormat;
                    return numVal.ToString(fmt, CultureInfo.InvariantCulture);
                }
                break;

            case FieldTransformType.Percentage:
                if (DataMatrix.TryParseNumeric(val, out double pctVal))
                {
                    return (pctVal / 100.0).ToString("P1", CultureInfo.InvariantCulture);
                }
                break;

            case FieldTransformType.Date:
                if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ||
                    DateTime.TryParse(val, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
                {
                    string fmt = string.IsNullOrWhiteSpace(customFormat) ? "yyyy-MM-dd" : customFormat;
                    return dt.ToString(fmt, CultureInfo.InvariantCulture);
                }
                break;
        }

        return val;
    }

    private string ResolveFilename(string pattern, IReadOnlyDictionary<string, string> record, int index)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            pattern = "Document_{{Index}}.pdf";
        }

        string raw = _mergeEngine.EvaluateText(pattern, record);
        raw = raw.Replace("{{Index}}", index.ToString());

        if (!raw.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            raw += ".pdf";
        }

        // Sanitize invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(raw.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        return sanitized;
    }

    private static string EnsureUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath)) return filePath;

        string dir = Path.GetDirectoryName(filePath) ?? "";
        string name = Path.GetFileNameWithoutExtension(filePath);
        string ext = Path.GetExtension(filePath);

        int counter = 1;
        while (File.Exists(filePath))
        {
            filePath = Path.Combine(dir, $"{name}_{counter++}{ext}");
        }

        return filePath;
    }
}
