using System.Collections.Generic;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Core.Data;

public class DataMergeOptions
{
    public bool CaseInsensitiveLookup { get; set; } = true;
    public bool PreserveUnmatchedPlaceholders { get; set; } = false;
    public string DefaultFallbackValue { get; set; } = string.Empty;
}

public interface IDataMergeEngine
{
    /// <summary>
    /// Scans a PDF document model and returns all unique placeholder tags found (e.g. "EmployeeName", "BasicSalary").
    /// </summary>
    IReadOnlyList<string> DetectPlaceholders(PdfDocumentModel template);

    /// <summary>
    /// Clones the template PDF document and replaces all placeholder expressions with data from the record dictionary.
    /// Supports Text, QR Codes, Barcodes, Image URLs/Base64, Table cells, Page Headers/Footers, and Document Metadata.
    /// </summary>
    PdfDocumentModel HydrateDocument(PdfDocumentModel template, IReadOnlyDictionary<string, string> record, DataMergeOptions? options = null);

    /// <summary>
    /// Evaluates placeholders and format expressions inside a single string.
    /// Syntax: {{FieldName}}, {{FieldName:C}}, {{FieldName:yyyy-MM-dd}}, {{FieldName:upper}}, {{FieldName ?? Default}}
    /// </summary>
    string EvaluateText(string templateText, IReadOnlyDictionary<string, string> record, DataMergeOptions? options = null);
}
