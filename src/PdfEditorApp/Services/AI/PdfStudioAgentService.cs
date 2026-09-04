using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.Services.AI;

/// <summary>
/// Autonomous document studio agent powered by Microsoft.Extensions.AI.
/// Translates natural language composition prompts into native editable FryPDF canvas elements.
/// </summary>
public class PdfStudioAgentService : IPdfStudioAgentService
{
    private readonly IAiService _aiService;
    private readonly IUndoRedoService? _undoRedoService;

    public PdfStudioAgentService(IAiService aiService, IUndoRedoService? undoRedoService = null)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _undoRedoService = undoRedoService;
    }

    /// <inheritdoc />
    public async Task<AiAgentResult> ExecutePromptAsync(
        string userPrompt,
        PageViewModel targetPage,
        AiSettingsModel settings,
        Action<string>? progressCallback = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return new AiAgentResult { Success = false, Message = "User prompt was empty." };
        }

        if (targetPage == null)
        {
            return new AiAgentResult { Success = false, Message = "Target page is required." };
        }

        var sw = Stopwatch.StartNew();
        progressCallback?.Invoke("Connecting to AI model...");

        var createdElements = new List<ElementViewModelBase>();
        var actionsTaken = new List<string>();

        // System prompt guiding design principles and canvas coordinate system
        string systemPrompt = $@"You are FryPDF Studio Agent, an expert AI document layout designer.
You create professional, beautiful, polished document elements directly on the active document canvas using available tools.

Canvas Dimensions:
- Page Width: {targetPage.Width:0} pt, Height: {targetPage.Height:0} pt.
- Coordinate origin (0, 0) is top-left.
- Safe printable margins: X: 40 to {targetPage.Width - 40:0} pt, Y: 40 to {targetPage.Height - 40:0} pt.

Design Guidelines:
1. Visual Hierarchy: Use clear contrast in font sizes (Headings 20-28pt bold, Subheadings 14-16pt, Paragraphs 10-12pt).
2. Layout Spacing: Leave generous vertical spacing between sections (15-25pt gap). Do not overlap elements unless deliberately layering a text/badge over a background card shape.
3. Cohesive Color Palettes:
   - Primary Accent: #0F6CBD (Modern Blue), #1E293B (Slate Dark), or #15803D (Emerald Green)
   - Background Tints: #F8FAFC, #F0F7FD, #F0FDF4
   - Borders/Dividers: #E2E8F0, #CBD5E1
   - Text Colors: Dark text #0F172A or #1E293B on light backgrounds.
4. Completeness: When asked for a composition (e.g. invoice, report header, certificate, certificate border, notice, card), invoke multiple tools to build the complete, ready-to-use section.
5. If the tool calling mechanism is unavailable or you prefer structured output, output a JSON array of actions with keys:
   action ('addHeading'|'addParagraph'|'addShape'|'addTable'|'addDivider'|'addBadge'|'addCard'|'addSvg'|'addQrCode'|'addBarcode'|'addChart').";

        if (!string.IsNullOrWhiteSpace(settings.SystemInstructions))
        {
            systemPrompt += "\n\nUser Custom Instructions:\n" + settings.SystemInstructions;
        }

        // Define tools using Microsoft.Extensions.AI AIFunctionFactory
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(
                ([Description("Heading or title text")] string text,
                 [Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 40,
                 [Description("Width in pt")] double width = 500,
                 [Description("Height in pt")] double height = 45,
                 [Description("Font size in pt (default 24)")] double fontSize = 24,
                 [Description("Font family name")] string fontFamily = "Georgia",
                 [Description("Hex text color (e.g. #111827)")] string textColorHex = "#111827",
                 [Description("Bold typeface")] bool isBold = true,
                 [Description("Text alignment: Left, Center, or Right")] string alignment = "Left") =>
                {
                    var el = CreateHeadingElement(text, x, y, width, height, fontSize, fontFamily, textColorHex, isBold, alignment);
                    targetPage.AddElement(el);
                    createdElements.Add(el);
                    string desc = $"Heading: \"{Truncate(text, 25)}\" at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created heading element ID {el.Id}";
                },
                "addHeading",
                "Adds a prominent section title or document heading text element."),

            AIFunctionFactory.Create(
                ([Description("Paragraph body text")] string text,
                 [Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 90,
                 [Description("Width in pt")] double width = 500,
                 [Description("Height in pt")] double height = 60,
                 [Description("Font size in pt (default 12)")] double fontSize = 12,
                 [Description("Font family name")] string fontFamily = "Inter",
                 [Description("Hex text color (e.g. #374151)")] string textColorHex = "#374151",
                 [Description("Text alignment: Left, Center, or Right")] string alignment = "Left") =>
                {
                    var el = CreateParagraphElement(text, x, y, width, height, fontSize, fontFamily, textColorHex, alignment);
                    targetPage.AddElement(el);
                    createdElements.Add(el);
                    string desc = $"Paragraph: \"{Truncate(text, 25)}\" at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created paragraph element ID {el.Id}";
                },
                "addParagraph",
                "Adds a multi-line paragraph or body text block."),

            AIFunctionFactory.Create(
                ([Description("Shape type: Rectangle, RoundedRectangle, Circle, PillBadge, Star, Triangle")] string shapeType = "RoundedRectangle",
                 [Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 40,
                 [Description("Width in pt")] double width = 200,
                 [Description("Height in pt")] double height = 100,
                 [Description("Fill background hex color")] string fillColorHex = "#F0F7FD",
                 [Description("Border stroke hex color")] string strokeColorHex = "#0F6CBD",
                 [Description("Stroke thickness in pt")] double strokeThickness = 1.0,
                 [Description("Corner radius for rounded rectangle")] double cornerRadius = 8.0) =>
                {
                    var el = CreateShapeElement(shapeType, x, y, width, height, fillColorHex, strokeColorHex, strokeThickness, cornerRadius);
                    targetPage.AddElement(el);
                    createdElements.Add(el);
                    string desc = $"Shape: {shapeType} at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created shape element ID {el.Id}";
                },
                "addShape",
                "Adds a geometric container shape, background card, accent bar, or banner."),

            AIFunctionFactory.Create(
                ([Description("List of column header titles")] string[] headers,
                 [Description("Matrix of row cell string values")] string[][] rows,
                 [Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 160,
                 [Description("Width in pt")] double width = 520,
                 [Description("Height in pt")] double height = 180,
                 [Description("Header background color hex")] string headerBgColorHex = "#0F6CBD",
                 [Description("Header text color hex")] string headerTextColorHex = "#FFFFFF") =>
                {
                    var el = CreateTableElement(headers, rows, x, y, width, height, headerBgColorHex, headerTextColorHex);
                    targetPage.AddElement(el);
                    createdElements.Add(el);
                    string desc = $"Table: {headers?.Length ?? 0} cols × {rows?.Length ?? 0} rows at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created table element ID {el.Id}";
                },
                "addTable",
                "Adds a structured multi-column data grid or financial table with formatted headers and rows."),

            AIFunctionFactory.Create(
                ([Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 150,
                 [Description("Width in pt")] double width = 520,
                 [Description("Height in pt")] double height = 2,
                 [Description("Divider line color hex")] string colorHex = "#CBD5E1",
                 [Description("Line thickness in pt")] double thickness = 1.0,
                 [Description("Orientation: Horizontal or Vertical")] string orientation = "Horizontal") =>
                {
                    var el = CreateDividerElement(x, y, width, height, colorHex, thickness, orientation);
                    targetPage.AddElement(el);
                    createdElements.Add(el);
                    string desc = $"Divider: {orientation} at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created divider element ID {el.Id}";
                },
                "addDivider",
                "Adds a decorative horizontal or vertical divider line to separate sections."),

            AIFunctionFactory.Create(
                ([Description("Short badge text label (e.g. PAID, CONFIDENTIAL, APPROVED)")] string text,
                 [Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 40,
                 [Description("Width in pt")] double width = 120,
                 [Description("Height in pt")] double height = 28,
                 [Description("Badge background fill color hex")] string bgColorHex = "#EFF6FF",
                 [Description("Badge text color hex")] string textColorHex = "#1D4ED8") =>
                {
                    var el = CreateBadgeElement(text, x, y, width, height, bgColorHex, textColorHex);
                    targetPage.AddElement(el);
                    createdElements.Add(el);
                    string desc = $"Badge: \"{text}\" at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created badge element ID {el.Id}";
                },
                "addBadge",
                "Adds a stylish pill badge or status tag (e.g. PAID, DRAFT, CONFIDENTIAL)."),

            AIFunctionFactory.Create(
                ([Description("Card header title")] string title,
                 [Description("Card subheader or date")] string subtitle,
                 [Description("Card body text")] string body,
                 [Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 100,
                 [Description("Width in pt")] double width = 520,
                 [Description("Height in pt")] double height = 130,
                 [Description("Theme accent color hex")] string themeColorHex = "#0F6CBD") =>
                {
                    var elements = CreateCardElements(title, subtitle, body, x, y, width, height, themeColorHex);
                    foreach (var el in elements)
                    {
                        targetPage.AddElement(el);
                        createdElements.Add(el);
                    }
                    string desc = $"Card: \"{Truncate(title, 20)}\" (container + text) at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created card composed of {elements.Count} elements";
                },
                "addCard",
                "Adds a composed UI callout card with container background, title, subtitle, and body text."),

            AIFunctionFactory.Create(
                ([Description("Preset ornament name from SvgOrnamentLibrary (e.g. ArtDecoFrame, BotanicalWreath, GaneshaCrest, OmCrest)")] string ornamentName,
                 [Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 40,
                 [Description("Width in pt")] double width = 80,
                 [Description("Height in pt")] double height = 80,
                 [Description("Tint color hex")] string colorHex = "#0F6CBD") =>
                {
                    var el = CreateSvgOrnamentElement(ornamentName, x, y, width, height, colorHex);
                    targetPage.AddElement(el);
                    createdElements.Add(el);
                    string desc = $"SVG Ornament: {ornamentName} at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created SVG ornament element ID {el.Id}";
                },
                "addSvgOrnament",
                "Adds a vector ornament, crest, emblem, or flourish from FryPDF's vector library."),

            AIFunctionFactory.Create(
                ([Description("URL, text, or payment payload")] string payload,
                 [Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 40,
                 [Description("Size in pt")] double size = 80) =>
                {
                    var el = CreateQrCodeElement(payload, x, y, size);
                    targetPage.AddElement(el);
                    createdElements.Add(el);
                    string desc = $"QR Code at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created QR code element ID {el.Id}";
                },
                "addQrCode",
                "Adds a scannable QR code element for verification, links, or contact details."),

            AIFunctionFactory.Create(
                ([Description("Barcode data value")] string payload,
                 [Description("Format: Code128, Ean13, etc.")] string format = "Code128",
                 [Description("X position in pt")] double x = 40,
                 [Description("Y position in pt")] double y = 40,
                 [Description("Width in pt")] double width = 180,
                 [Description("Height in pt")] double height = 55) =>
                {
                    var el = CreateBarcodeElement(payload, format, x, y, width, height);
                    targetPage.AddElement(el);
                    createdElements.Add(el);
                    string desc = $"Barcode: {payload} at ({x:0},{y:0})";
                    actionsTaken.Add(desc);
                    progressCallback?.Invoke($"Created {desc}");
                    return $"Created barcode element ID {el.Id}";
                },
                "addBarcode",
                "Adds a standard 1D barcode element.")
        };

        try
        {
            var baseChatClient = _aiService.CreateChatClient(settings);

            // Wrap in FunctionInvokingChatClient from Microsoft.Extensions.AI for automatic tool loop
            var chatClient = new ChatClientBuilder(baseChatClient)
                .UseFunctionInvocation()
                .Build();

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, userPrompt)
            };

            var chatOptions = new ChatOptions
            {
                Tools = tools,
                Temperature = settings.Temperature,
                MaxOutputTokens = 1500
            };

            progressCallback?.Invoke("Generating elements on canvas...");
            var response = await chatClient.GetResponseAsync(messages, chatOptions, ct);

            string replyText = response?.Text ?? string.Empty;

            // FALLBACK: If model returned structured JSON actions in text instead of native function calls
            if (createdElements.Count == 0 && !string.IsNullOrWhiteSpace(replyText))
            {
                progressCallback?.Invoke("Parsing structured element instructions...");
                ParseAndExecuteJsonFallback(replyText, targetPage, createdElements, actionsTaken, progressCallback);
            }

            // ATOMIC UNDO/REDO RECORDING: Wrap all created elements in a single atomic undo transaction
            if (createdElements.Count > 0 && _undoRedoService != null)
            {
                var elementsSnapshot = createdElements.ToList();
                string undoDesc = $"AI Studio: {Truncate(userPrompt, 30)} ({elementsSnapshot.Count} items)";

                _undoRedoService.RecordAction(
                    undoDesc,
                    () =>
                    {
                        foreach (var el in elementsSnapshot) targetPage.RemoveElement(el);
                    },
                    () =>
                    {
                        foreach (var el in elementsSnapshot) targetPage.AddElement(el);
                    });
            }

            sw.Stop();

            if (createdElements.Count > 0)
            {
                return new AiAgentResult
                {
                    Success = true,
                    Message = $"Successfully created {createdElements.Count} elements in {sw.Elapsed.TotalSeconds:0.1}s.",
                    ElementsCreatedCount = createdElements.Count,
                    ActionDescriptions = actionsTaken,
                    RawOutput = replyText,
                    Duration = sw.Elapsed
                };
            }
            else
            {
                return new AiAgentResult
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(replyText)
                        ? "The AI model did not generate any elements. Please verify your prompt."
                        : $"AI Response: {replyText}",
                    ElementsCreatedCount = 0,
                    RawOutput = replyText,
                    Duration = sw.Elapsed
                };
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new AiAgentResult
            {
                Success = false,
                Message = $"AI Generation Error: {ex.Message}",
                Duration = sw.Elapsed
            };
        }
    }

    #region Element Factory Methods

    private static TextElementViewModel CreateHeadingElement(
        string text, double x, double y, double width, double height,
        double fontSize, string fontFamily, string textColorHex, bool isBold, string alignment)
    {
        return new TextElementViewModel
        {
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = Math.Max(60, width),
            Height = Math.Max(24, height),
            Text = text,
            FontSize = fontSize > 0 ? fontSize : 24,
            FontFamily = string.IsNullOrWhiteSpace(fontFamily) ? "Georgia" : fontFamily,
            TextColorHex = string.IsNullOrWhiteSpace(textColorHex) ? "#111827" : textColorHex,
            IsBold = isBold,
            ZIndex = 1000
        };
    }

    private static TextElementViewModel CreateParagraphElement(
        string text, double x, double y, double width, double height,
        double fontSize, string fontFamily, string textColorHex, string alignment)
    {
        return new TextElementViewModel
        {
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = Math.Max(60, width),
            Height = Math.Max(30, height),
            Text = text,
            FontSize = fontSize > 0 ? fontSize : 12,
            FontFamily = string.IsNullOrWhiteSpace(fontFamily) ? "Inter" : fontFamily,
            TextColorHex = string.IsNullOrWhiteSpace(textColorHex) ? "#374151" : textColorHex,
            ZIndex = 1000
        };
    }

    private static ShapeElementViewModel CreateShapeElement(
        string shapeTypeStr, double x, double y, double width, double height,
        string fillColorHex, string strokeColorHex, double strokeThickness, double cornerRadius)
    {
        var shapeType = ShapeType.RoundedRectangle;
        if (Enum.TryParse<ShapeType>(shapeTypeStr, true, out var parsed))
        {
            shapeType = parsed;
        }

        return new ShapeElementViewModel
        {
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = Math.Max(20, width),
            Height = Math.Max(10, height),
            ShapeType = shapeType,
            FillColorHex = string.IsNullOrWhiteSpace(fillColorHex) ? "#F0F7FD" : fillColorHex,
            StrokeColorHex = string.IsNullOrWhiteSpace(strokeColorHex) ? "#0F6CBD" : strokeColorHex,
            StrokeThickness = strokeThickness >= 0 ? strokeThickness : 1.0,
            CornerRadius = cornerRadius >= 0 ? cornerRadius : 6.0,
            ZIndex = 100 // Background shape layer
        };
    }

    private static TableElementViewModel CreateTableElement(
        string[]? headers, string[][]? rows, double x, double y, double width, double height,
        string headerBgColorHex, string headerTextColorHex)
    {
        var table = new TableElementViewModel
        {
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = Math.Max(200, width),
            Height = Math.Max(80, height),
            HeaderBackgroundHex = string.IsNullOrWhiteSpace(headerBgColorHex) ? "#0F6CBD" : headerBgColorHex,
            HeaderTextHex = string.IsNullOrWhiteSpace(headerTextColorHex) ? "#FFFFFF" : headerTextColorHex,
            ZIndex = 500
        };

        if (headers != null && headers.Length > 0)
        {
            table.Headers.Clear();
            foreach (var h in headers)
            {
                table.Headers.Add(new TableHeaderItem(h));
            }
        }

        if (rows != null && rows.Length > 0)
        {
            table.Rows.Clear();
            foreach (var row in rows)
            {
                table.Rows.Add(new TableRowItem(row));
            }
        }

        return table;
    }

    private static DividerElementViewModel CreateDividerElement(
        double x, double y, double width, double height,
        string colorHex, double thickness, string orientation)
    {
        bool isVertical = orientation.Equals("Vertical", StringComparison.OrdinalIgnoreCase);

        return new DividerElementViewModel
        {
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = Math.Max(10, width),
            Height = isVertical ? Math.Max(10, height) : 2,
            ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#CBD5E1" : colorHex,
            Thickness = thickness > 0 ? thickness : 1.0,
            IsVertical = isVertical,
            ZIndex = 600
        };
    }

    private static ShapeElementViewModel CreateBadgeElement(
        string text, double x, double y, double width, double height,
        string bgColorHex, string textColorHex)
    {
        return new ShapeElementViewModel
        {
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = Math.Max(60, width),
            Height = Math.Max(22, height),
            ShapeType = ShapeType.RoundedRectangle,
            FillColorHex = string.IsNullOrWhiteSpace(bgColorHex) ? "#EFF6FF" : bgColorHex,
            StrokeColorHex = string.IsNullOrWhiteSpace(textColorHex) ? "#1D4ED8" : textColorHex,
            StrokeThickness = 1.0,
            CornerRadius = height / 2.0, // Pill badge
            Label = text,
            LabelColorHex = textColorHex,
            LabelFontSize = 11,
            ZIndex = 800
        };
    }

    private static List<ElementViewModelBase> CreateCardElements(
        string title, string subtitle, string body,
        double x, double y, double width, double height, string themeColorHex)
    {
        var list = new List<ElementViewModelBase>();

        // 1. Background Card Shape (ZIndex = 50)
        var cardShape = new ShapeElementViewModel
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            ShapeType = ShapeType.RoundedRectangle,
            FillColorHex = "#F8FAFC",
            StrokeColorHex = string.IsNullOrWhiteSpace(themeColorHex) ? "#0F6CBD" : themeColorHex,
            StrokeThickness = 1.2,
            CornerRadius = 10,
            ZIndex = 50
        };
        list.Add(cardShape);

        // 2. Title Text
        var titleEl = new TextElementViewModel
        {
            X = x + 16,
            Y = y + 14,
            Width = width - 32,
            Height = 26,
            Text = title,
            FontSize = 15,
            FontFamily = "Georgia",
            IsBold = true,
            TextColorHex = "#0F172A",
            ZIndex = 1000
        };
        list.Add(titleEl);

        double contentY = y + 42;

        // 3. Subtitle Text (if present)
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            var subEl = new TextElementViewModel
            {
                X = x + 16,
                Y = contentY,
                Width = width - 32,
                Height = 20,
                Text = subtitle,
                FontSize = 11,
                FontFamily = "Inter",
                TextColorHex = string.IsNullOrWhiteSpace(themeColorHex) ? "#0F6CBD" : themeColorHex,
                ZIndex = 1000
            };
            list.Add(subEl);
            contentY += 24;
        }

        // 4. Body Text
        if (!string.IsNullOrWhiteSpace(body))
        {
            var bodyEl = new TextElementViewModel
            {
                X = x + 16,
                Y = contentY,
                Width = width - 32,
                Height = Math.Max(30, (y + height) - contentY - 14),
                Text = body,
                FontSize = 11.5,
                FontFamily = "Inter",
                TextColorHex = "#334155",
                ZIndex = 1000
            };
            list.Add(bodyEl);
        }

        return list;
    }

    private static SvgElementViewModel CreateSvgOrnamentElement(
        string ornamentName, double x, double y, double width, double height, string colorHex)
    {
        string svg = SvgOrnamentLibrary.GetSvg(ornamentName, colorHex);
        return new SvgElementViewModel
        {
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = Math.Max(20, width),
            Height = Math.Max(20, height),
            SvgSource = svg,
            PresetName = ornamentName,
            TintColorHex = colorHex,
            ZIndex = 700
        };
    }

    private static QrCodeElementViewModel CreateQrCodeElement(string payload, double x, double y, double size)
    {
        double dim = Math.Max(40, size);
        return new QrCodeElementViewModel
        {
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = dim,
            Height = dim,
            Content = string.IsNullOrWhiteSpace(payload) ? "https://github.com/PrashantUnity/PDFCreator" : payload,
            ZIndex = 800
        };
    }

    private static BarcodeElementViewModel CreateBarcodeElement(string payload, string format, double x, double y, double width, double height)
    {
        return new BarcodeElementViewModel
        {
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = Math.Max(100, width),
            Height = Math.Max(30, height),
            CodeValue = string.IsNullOrWhiteSpace(payload) ? "DOC-2026-001" : payload,
            BarcodeFormat = string.IsNullOrWhiteSpace(format) ? "Code128" : format,
            ZIndex = 800
        };
    }

    #endregion

    #region JSON Fallback Parsing

    private void ParseAndExecuteJsonFallback(
        string text,
        PageViewModel page,
        List<ElementViewModelBase> created,
        List<string> actions,
        Action<string>? progress)
    {
        var match = Regex.Match(text, @"\[[\s\S]*\]");
        if (!match.Success) return;

        try
        {
            using var doc = JsonDocument.Parse(match.Value);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("action", out var actionProp)) continue;
                string action = actionProp.GetString() ?? "";

                double x = GetDouble(item, "x", 40);
                double y = GetDouble(item, "y", 40);
                double w = GetDouble(item, "width", 300);
                double h = GetDouble(item, "height", 50);

                switch (action.ToLowerInvariant())
                {
                    case "addheading":
                    {
                        string txt = GetString(item, "text", "Heading");
                        double fontSize = GetDouble(item, "fontSize", 24);
                        string color = GetString(item, "textColorHex", "#111827");
                        var el = CreateHeadingElement(txt, x, y, w, h, fontSize, "Georgia", color, true, "Left");
                        page.AddElement(el);
                        created.Add(el);
                        actions.Add($"Heading: \"{Truncate(txt, 20)}\"");
                        progress?.Invoke($"Created Heading: \"{Truncate(txt, 20)}\"");
                        break;
                    }
                    case "addparagraph":
                    {
                        string txt = GetString(item, "text", "Paragraph text");
                        double fontSize = GetDouble(item, "fontSize", 12);
                        string color = GetString(item, "textColorHex", "#374151");
                        var el = CreateParagraphElement(txt, x, y, w, h, fontSize, "Inter", color, "Left");
                        page.AddElement(el);
                        created.Add(el);
                        actions.Add($"Paragraph: \"{Truncate(txt, 20)}\"");
                        progress?.Invoke($"Created Paragraph: \"{Truncate(txt, 20)}\"");
                        break;
                    }
                    case "addshape":
                    {
                        string shape = GetString(item, "shapeType", "RoundedRectangle");
                        string fill = GetString(item, "fillColorHex", "#F0F7FD");
                        string stroke = GetString(item, "strokeColorHex", "#0F6CBD");
                        double radius = GetDouble(item, "cornerRadius", 6);
                        var el = CreateShapeElement(shape, x, y, w, h, fill, stroke, 1.0, radius);
                        page.AddElement(el);
                        created.Add(el);
                        actions.Add($"Shape: {shape}");
                        progress?.Invoke($"Created Shape: {shape}");
                        break;
                    }
                    case "adddivider":
                    {
                        string color = GetString(item, "colorHex", "#CBD5E1");
                        var el = CreateDividerElement(x, y, w, h, color, 1.0, "Horizontal");
                        page.AddElement(el);
                        created.Add(el);
                        actions.Add("Divider line");
                        progress?.Invoke("Created Divider");
                        break;
                    }
                    case "addbadge":
                    {
                        string txt = GetString(item, "text", "STATUS");
                        string bg = GetString(item, "bgColorHex", "#EFF6FF");
                        string textCol = GetString(item, "textColorHex", "#1D4ED8");
                        var el = CreateBadgeElement(txt, x, y, w, h, bg, textCol);
                        page.AddElement(el);
                        created.Add(el);
                        actions.Add($"Badge: \"{txt}\"");
                        progress?.Invoke($"Created Badge: \"{txt}\"");
                        break;
                    }
                }
            }
        }
        catch
        {
            // JSON fallback parsing failed; proceed without crash
        }
    }

    private static double GetDouble(JsonElement el, string prop, double fallback)
    {
        if (el.TryGetProperty(prop, out var p) && p.TryGetDouble(out var v)) return v;
        return fallback;
    }

    private static string GetString(JsonElement el, string prop, string fallback)
    {
        if (el.TryGetProperty(prop, out var p)) return p.GetString() ?? fallback;
        return fallback;
    }

    private static string Truncate(string str, int maxLen)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Length <= maxLen ? str : str[..maxLen] + "...";
    }

    #endregion
}
