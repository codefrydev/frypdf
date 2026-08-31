using System;
using System.Collections.Generic;
using PdfEditorApp.Models;

namespace PdfEditorApp.Models;

/// <summary>
/// Model representing a comprehensive help guide topic, tool walkthrough, or troubleshooting article.
/// </summary>
public class HelpGuideItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Steps { get; set; } = new();
    public List<string> KeyFeatures { get; set; } = new();
    public List<string> ProTips { get; set; } = new();
    public string SupportedFormats { get; set; } = "";
    public PdfToolId? RelatedToolId { get; set; }
    public string? KeyboardShortcut { get; set; }
    public string IconKind { get; set; } = "HelpCircleOutline";
    public string IconColorHex { get; set; } = "#0284C7";
    public string BackgroundAccentHex { get; set; } = "#E0F2FE";
    public string Badge { get; set; } = "Guide";
    public string Keywords { get; set; } = "";
    public bool IsFeatured { get; set; }

    public bool HasRelatedTool => RelatedToolId.HasValue;
    public bool HasShortcut => !string.IsNullOrWhiteSpace(KeyboardShortcut);
    public bool HasFormats => !string.IsNullOrWhiteSpace(SupportedFormats);
    public bool HasSteps => Steps != null && Steps.Count > 0;
    public bool HasKeyFeatures => KeyFeatures != null && KeyFeatures.Count > 0;
    public bool HasProTips => ProTips != null && ProTips.Count > 0;
}
