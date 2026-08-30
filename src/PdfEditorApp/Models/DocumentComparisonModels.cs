using System;
using System.Collections.Generic;

namespace PdfEditorApp.Models;

public class DocumentComparisonReport
{
    public string BaseDocumentTitle { get; set; } = "";
    public string ComparedDocumentTitle { get; set; } = "";
    public DateTime ComparisonTimestamp { get; set; } = DateTime.UtcNow;

    public int BasePageCount { get; set; }
    public int ComparedPageCount { get; set; }

    public int TotalDifferencesCount => Differences.Count;
    public int AdditionsCount { get; set; }
    public int DeletionsCount { get; set; }
    public int ModificationsCount { get; set; }

    public List<DocumentDifferenceItem> Differences { get; set; } = new();
}

public class DocumentDifferenceItem
{
    public int PageNumber { get; set; }
    public CompareDiffType DiffType { get; set; }
    public string Description { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public string ElementKindDisplay { get; set; } = "";
    public string BadgeColorHex => DiffType switch
    {
        CompareDiffType.ElementAdded => "#16A34A",
        CompareDiffType.ElementRemoved => "#DC2626",
        CompareDiffType.ElementModified => "#0F6CBD",
        CompareDiffType.TextModified => "#D97706",
        CompareDiffType.FormattingModified => "#7C3AED",
        CompareDiffType.SecurityModified => "#E11D48",
        CompareDiffType.MetadataModified => "#2563EB",
        _ => "#475569"
    };
    public string BadgeIcon => DiffType switch
    {
        CompareDiffType.ElementAdded => "PlusCircleOutline",
        CompareDiffType.ElementRemoved => "MinusCircleOutline",
        CompareDiffType.ElementModified => "PencilOutline",
        CompareDiffType.TextModified => "FormatColorText",
        CompareDiffType.FormattingModified => "FormatFont",
        CompareDiffType.SecurityModified => "ShieldAlertOutline",
        CompareDiffType.MetadataModified => "TagOutline",
        _ => "InformationOutline"
    };
}
