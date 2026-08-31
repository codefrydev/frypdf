using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Data;

public enum BatchOutputMode
{
    SeparateFiles,
    SingleMergedPdf,
    ZipArchive
}

public enum FieldTransformType
{
    None,
    Currency,
    Date,
    Numeric,
    Percentage,
    Uppercase,
    Lowercase,
    TitleCase
}

public class FieldMappingItem
{
    public string PlaceholderTag { get; set; } = string.Empty;
    public string DataColumnName { get; set; } = string.Empty;
    public FieldTransformType Transform { get; set; } = FieldTransformType.None;
    public string CustomFormat { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public string SampleValue { get; set; } = string.Empty;

    public FieldMappingItem() { }

    public FieldMappingItem(string placeholderTag, string dataColumnName, string defaultValue = "")
    {
        PlaceholderTag = placeholderTag;
        DataColumnName = dataColumnName;
        DefaultValue = defaultValue;
    }
}

public class BatchGenerationConfig
{
    public BatchOutputMode OutputMode { get; set; } = BatchOutputMode.SeparateFiles;
    public string OutputDirectory { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public string FilenamePattern { get; set; } = "Document_{{Index}}.pdf";
    public bool SkipEmptyRows { get; set; } = true;
    public int? MaxRecordCount { get; set; }
}

public class BatchProgressReport
{
    public int CurrentIndex { get; set; }
    public int TotalCount { get; set; }
    public string CurrentItemName { get; set; } = string.Empty;
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public double Percentage => TotalCount > 0 ? (double)CurrentIndex / TotalCount * 100.0 : 0;
}

public class BatchGenerationResult
{
    public int TotalProcessed { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> GeneratedFiles { get; set; } = new();
    public string? OutputMergedFilePath { get; set; }
    public string? OutputZipFilePath { get; set; }
    public List<(int RowIndex, string ErrorMessage)> Errors { get; set; } = new();
    public TimeSpan ElapsedTime { get; set; }
    public bool IsSuccess => FailedCount == 0 && SuccessfulCount > 0;
}
