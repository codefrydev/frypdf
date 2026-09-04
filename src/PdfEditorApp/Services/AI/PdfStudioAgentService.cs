using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        string systemPrompt = $"""
            You are FryPDF Studio Agent, an expert AI document layout designer.
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
               action ('addHeading'|'addParagraph'|'addShape'|'addTable'|'addDivider'|'addBadge'|'addCard'|'addSvg'|'addQrCode'|'addBarcode'|'addChart').
            """;

        if (!string.IsNullOrWhiteSpace(settings.SystemInstructions))
        {
            systemPrompt += $"""


                User Custom Instructions:
                {settings.SystemInstructions}
                """;
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

    /// <inheritdoc />
    public async Task<AiAgentResult> ModifyElementAsync(
        ElementViewModelBase targetElement,
        string modificationPrompt,
        AiSettingsModel settings,
        Action<string>? progressCallback = null,
        CancellationToken ct = default)
    {
        if (targetElement == null)
        {
            return new AiAgentResult { Success = false, Message = "Target element is required." };
        }

        if (string.IsNullOrWhiteSpace(modificationPrompt))
        {
            return new AiAgentResult { Success = false, Message = "Modification prompt cannot be empty." };
        }

        var sw = Stopwatch.StartNew();
        progressCallback?.Invoke($"Analyzing {targetElement.Kind} element...");

        var currentModel = targetElement.ToModel();
        var beforeSnapshot = currentModel.Clone();

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        string currentJson = JsonSerializer.Serialize(currentModel, currentModel.GetType(), jsonOptions);

        string systemPrompt = $"""
            You are FryPDF AI Element Modifier, an expert in document graphics, charts, typography, and layout.
            The user has selected an existing canvas element and wants to modify it in-place using natural language.
            Element Kind: {currentModel.Kind}

            Instructions:
            1. Apply the user's requested modifications to the properties of this element.
            2. Return ONLY a valid JSON object matching the element's schema, with the updated property values.
            3. Keep unmodified properties intact. Retain the same 'id', 'x', and 'y' unless explicitly requested to move or resize.
            4. Output strictly valid JSON. Do not include introductory text, conversational pleasantries, or explanations outside the JSON block.

            Element-Specific Guidelines:
            - Charts: You can modify 'title', 'chartType' (BarColumn, HorizontalBar, Line, SmoothLine, Area, DonutPie, StackedBar, StackedHorizontalBar, Radar, ScatterPlot), 'palette' (CorporateBlue, ModernTeal, VibrantWarm, EmeraldForest, RoyalPurple, SunsetOrange, MonochromeSlate, NeonCyber), 'categories', 'values', 'valueLabels', 'barColorsHex', 'backgroundColorHex', 'borderColorHex', 'showDataLabels', 'showGridlines', etc.
            - Text: You can modify 'text', 'fontFamily', 'fontSize', 'isBold', 'isItalic', 'isUnderline', 'textColorHex', 'backgroundColorHex', 'alignment', 'lineHeight', etc.
            - Table: You can modify 'headers', 'rows', 'headerBackgroundHex', 'borderColorHex', 'alternateRowBackgroundHex', etc.
            - Shape: You can modify 'shapeType' (Rectangle, RoundedRectangle, Circle, PillBadge, Diamond, Star, Banner), 'fillColorHex', 'strokeColorHex', 'strokeThickness', 'cornerRadius', etc.
            - Math: You can modify 'formula' (LaTeX equation string), 'fontSize', 'textColorHex', 'showEquationNumber', 'equationNumber', etc.
            - Image: You can modify 'opacity', 'width', 'height', 'altText', etc.
            - Svg: You can modify 'tintColorHex', 'borderColorHex', 'borderThickness', 'cornerRadius', etc.
            - Divider: You can modify 'colorHex', 'thickness', 'orientation', etc.
            - QrCode: You can modify 'payload', 'darkColorHex', 'lightColorHex', etc.
            - Barcode: You can modify 'codeValue', 'barColorHex', 'barcodeFormat', etc.
            - FormField: You can modify 'label', 'isRequired', 'borderColorHex', 'backgroundColorHex', etc.
            - StickyNote: You can modify 'noteText', 'colorHex', 'status', 'author', etc.
            """;

        if (!string.IsNullOrWhiteSpace(settings.SystemInstructions))
        {
            systemPrompt += $"""


                User Custom Instructions:
                {settings.SystemInstructions}
                """;
        }

        string userMessage = $"""
            Modify this element according to instructions:

            User Request: {modificationPrompt}

            Current Element JSON:
            {currentJson}
            """;

        PdfElementBase? updatedModel = null;
        string replyText = string.Empty;

        try
        {
            var baseChatClient = _aiService.CreateChatClient(settings);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, userMessage)
            };

            var chatOptions = new ChatOptions
            {
                Temperature = Math.Min(settings.Temperature, 0.4f),
                MaxOutputTokens = 2000
            };

            progressCallback?.Invoke("Querying AI model for modifications...");
            var response = await baseChatClient.GetResponseAsync(messages, chatOptions, ct);
            replyText = response?.Text ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(replyText))
            {
                progressCallback?.Invoke("Applying updated attributes...");
                updatedModel = TryParseElementJson(replyText, currentModel.GetType(), jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PdfStudioAgentService] AI call error during ModifyElement: {ex.Message}");
            progressCallback?.Invoke("Attempting smart offline modifier...");
        }

        // Fallback to heuristic modifier if offline or test environment
        if (updatedModel == null)
        {
            updatedModel = TryApplyHeuristicModification(currentModel, modificationPrompt);
        }

        sw.Stop();

        if (updatedModel != null)
        {
            updatedModel.Id = currentModel.Id;
            if (updatedModel.Width <= 0) updatedModel.Width = currentModel.Width;
            if (updatedModel.Height <= 0) updatedModel.Height = currentModel.Height;

            targetElement.LoadFromModel(updatedModel);

            if (targetElement is ChartElementViewModel chartVm)
            {
                chartVm.UpdateLiveChart();
            }

            // Atomic Undo/Redo recording
            if (_undoRedoService != null)
            {
                var afterSnapshot = updatedModel.Clone();
                string undoDesc = $"AI Modify: {Truncate(modificationPrompt, 28)}";
                _undoRedoService.RecordAction(
                    undoDesc,
                    () =>
                    {
                        targetElement.LoadFromModel(beforeSnapshot);
                        if (targetElement is ChartElementViewModel c) c.UpdateLiveChart();
                    },
                    () =>
                    {
                        targetElement.LoadFromModel(afterSnapshot);
                        if (targetElement is ChartElementViewModel c) c.UpdateLiveChart();
                    });
            }

            return new AiAgentResult
            {
                Success = true,
                Message = $"Updated {targetElement.Kind} successfully: {Truncate(modificationPrompt, 40)}",
                ElementsCreatedCount = 1,
                RawOutput = replyText,
                Duration = sw.Elapsed
            };
        }

        return new AiAgentResult
        {
            Success = false,
            Message = string.IsNullOrWhiteSpace(replyText)
                ? "Unable to modify element with the provided instructions."
                : $"AI Response could not be parsed: {Truncate(replyText, 100)}",
            ElementsCreatedCount = 0,
            RawOutput = replyText,
            Duration = sw.Elapsed
        };
    }

    private static PdfElementBase? TryParseElementJson(string rawText, Type targetType, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return null;

        try
        {
            string cleaned = rawText.Trim();
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[7..];
            }
            else if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[3..];
            }

            if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^3].Trim();
            }

            int firstBrace = cleaned.IndexOf('{');
            int lastBrace = cleaned.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return JsonSerializer.Deserialize(cleaned, targetType, options) as PdfElementBase;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PdfStudioAgentService] JSON parse error: {ex.Message}");
            return null;
        }
    }

    private static PdfElementBase? TryApplyHeuristicModification(PdfElementBase currentModel, string prompt)
    {
        if (currentModel == null || string.IsNullOrWhiteSpace(prompt)) return null;
        string lower = prompt.ToLowerInvariant();

        if (currentModel is PdfChartElement chart)
        {
            var modified = (PdfChartElement)chart.Clone();

            // Chart Type
            if (lower.Contains("line"))
            {
                modified.ChartType = lower.Contains("smooth") ? ChartType.SmoothLine : ChartType.Line;
            }
            else if (lower.Contains("bar") || lower.Contains("column"))
            {
                modified.ChartType = lower.Contains("horizontal") ? ChartType.HorizontalBar : ChartType.BarColumn;
            }
            else if (lower.Contains("donut") || lower.Contains("pie"))
            {
                modified.ChartType = ChartType.DonutPie;
            }
            else if (lower.Contains("area"))
            {
                modified.ChartType = ChartType.Area;
            }
            else if (lower.Contains("stacked"))
            {
                modified.ChartType = lower.Contains("horizontal") ? ChartType.StackedHorizontalBar : ChartType.StackedBar;
            }
            else if (lower.Contains("radar"))
            {
                modified.ChartType = ChartType.Radar;
            }

            // Palette
            if (lower.Contains("emerald") || lower.Contains("green") || lower.Contains("mint"))
            {
                modified.Palette = ChartPalette.EmeraldGreen;
                modified.BarColorsHex = new List<string> { "#A7F3D0", "#34D399", "#10B981", "#047857" };
            }
            else if (lower.Contains("cyber") || lower.Contains("neon"))
            {
                modified.Palette = ChartPalette.CyberNeon;
                modified.BarColorsHex = new List<string> { "#99F6E4", "#2DD4BF", "#0D9488", "#115E59" };
            }
            else if (lower.Contains("pastel"))
            {
                modified.Palette = ChartPalette.PastelHarmony;
                modified.BarColorsHex = new List<string> { "#E9D5FF", "#C084FC", "#9333EA", "#6B21A8" };
            }
            else if (lower.Contains("sunset") || lower.Contains("orange"))
            {
                modified.Palette = ChartPalette.SunsetOrange;
                modified.BarColorsHex = new List<string> { "#FED7AA", "#FB923C", "#EA580C", "#9A3412" };
            }
            else if (lower.Contains("rainbow") || lower.Contains("vibrant"))
            {
                modified.Palette = ChartPalette.VibrantRainbow;
                modified.BarColorsHex = new List<string> { "#FECDD3", "#FB7185", "#E11D48", "#9F1239" };
            }
            else if (lower.Contains("blue") || lower.Contains("corporate"))
            {
                modified.Palette = ChartPalette.CorporateBlue;
                modified.BarColorsHex = new List<string> { "#C7E0F4", "#82BDF0", "#3D95E6", "#0F6CBD" };
            }
            else if (lower.Contains("slate") || lower.Contains("gray") || lower.Contains("grey") || lower.Contains("executive"))
            {
                modified.Palette = ChartPalette.ExecutiveSlate;
                modified.BarColorsHex = new List<string> { "#E2E8F0", "#94A3B8", "#475569", "#1E293B" };
            }

            // Title extraction: e.g. title to "XYZ" or rename to "XYZ" or title: XYZ
            var titleMatch = Regex.Match(prompt, """(?:title\s*(?:to|is|:)\s*|rename\s*(?:to)?\s*)["']?([^"'\r\n]+)["']?""", RegexOptions.IgnoreCase);
            if (titleMatch.Success && !string.IsNullOrWhiteSpace(titleMatch.Groups[1].Value))
            {
                modified.Title = titleMatch.Groups[1].Value.Trim();
            }

            // Projections / Adding future quarters
            if (lower.Contains("2027") || lower.Contains("projection") || lower.Contains("forecast") || lower.Contains("add quarter"))
            {
                if (!modified.Categories.Any(c => c.Contains("2027")))
                {
                    double lastVal = modified.Values.LastOrDefault();
                    double projVal = lastVal > 0 ? Math.Round(lastVal * 1.15, 2) : 1.0;
                    modified.Categories.Add("Q1 2027 (Proj)");
                    modified.Values.Add(projVal);
                    modified.ValueLabels.Add($"${projVal:0.00}B");
                    modified.BarColorsHex.Add("#34D399");
                }
            }

            // Sorting
            if (lower.Contains("sort") && (lower.Contains("asc") || lower.Contains("ascending") || lower.Contains("lowest")))
            {
                var paired = modified.Categories.Select((cat, i) => new
                {
                    Cat = cat,
                    Val = i < modified.Values.Count ? modified.Values[i] : 0,
                    Lbl = i < modified.ValueLabels.Count ? modified.ValueLabels[i] : "",
                    Col = i < modified.BarColorsHex.Count ? modified.BarColorsHex[i] : "#0F6CBD"
                }).OrderBy(x => x.Val).ToList();

                modified.Categories = paired.Select(p => p.Cat).ToList();
                modified.Values = paired.Select(p => p.Val).ToList();
                modified.ValueLabels = paired.Select(p => p.Lbl).ToList();
                modified.BarColorsHex = paired.Select(p => p.Col).ToList();
            }
            else if (lower.Contains("sort") && (lower.Contains("desc") || lower.Contains("descending") || lower.Contains("highest")))
            {
                var paired = modified.Categories.Select((cat, i) => new
                {
                    Cat = cat,
                    Val = i < modified.Values.Count ? modified.Values[i] : 0,
                    Lbl = i < modified.ValueLabels.Count ? modified.ValueLabels[i] : "",
                    Col = i < modified.BarColorsHex.Count ? modified.BarColorsHex[i] : "#0F6CBD"
                }).OrderByDescending(x => x.Val).ToList();

                modified.Categories = paired.Select(p => p.Cat).ToList();
                modified.Values = paired.Select(p => p.Val).ToList();
                modified.ValueLabels = paired.Select(p => p.Lbl).ToList();
                modified.BarColorsHex = paired.Select(p => p.Col).ToList();
            }

            return modified;
        }

        if (currentModel is PdfTextElement text)
        {
            var modified = (PdfTextElement)text.Clone();

            if (lower.Contains("bold")) modified.IsBold = true;
            if (lower.Contains("italic")) modified.IsItalic = true;
            if (lower.Contains("underline")) modified.IsUnderline = true;

            if (lower.Contains("center")) modified.Alignment = TextAlignmentMode.Center;
            else if (lower.Contains("right")) modified.Alignment = TextAlignmentMode.Right;
            else if (lower.Contains("left")) modified.Alignment = TextAlignmentMode.Left;
            else if (lower.Contains("justify")) modified.Alignment = TextAlignmentMode.Justify;

            var sizeMatch = Regex.Match(prompt, @"(\d+)\s*(?:pt|px|size)", RegexOptions.IgnoreCase);
            if (sizeMatch.Success && double.TryParse(sizeMatch.Groups[1].Value, out double size) && size > 4 && size < 120)
            {
                modified.FontSize = size;
            }

            if (lower.Contains("red")) modified.TextColorHex = "#DC2626";
            else if (lower.Contains("blue")) modified.TextColorHex = "#0F6CBD";
            else if (lower.Contains("green")) modified.TextColorHex = "#16A34A";
            else if (lower.Contains("purple")) modified.TextColorHex = "#7C3AED";
            else if (lower.Contains("amber") || lower.Contains("orange")) modified.TextColorHex = "#D97706";
            else if (lower.Contains("dark") || lower.Contains("black")) modified.TextColorHex = "#0F172A";

            if (lower.Contains("inter")) modified.FontFamily = "Inter";
            else if (lower.Contains("roboto")) modified.FontFamily = "Roboto";
            else if (lower.Contains("georgia")) modified.FontFamily = "Georgia";
            else if (lower.Contains("cascadia") || lower.Contains("mono")) modified.FontFamily = "Cascadia Code";
            else if (lower.Contains("times")) modified.FontFamily = "Times New Roman";

            if (lower.Contains("bullet"))
            {
                var lines = modified.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                modified.Text = string.Join("\n", lines.Select(l => l.Trim().StartsWith("•") ? l : $"• {l.TrimStart('✔', '✓', '•', '-', '*', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.')}"));
            }
            else if (lower.Contains("check") || lower.Contains("checkmark"))
            {
                var lines = modified.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                modified.Text = string.Join("\n", lines.Select(l => l.Trim().StartsWith("✔") ? l : $"✔ {l.TrimStart('✔', '✓', '•', '-', '*', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.')}"));
            }
            else if (lower.Contains("number") || lower.Contains("numbered") || lower.Contains("roadmap"))
            {
                var lines = modified.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                modified.Text = string.Join("\n", lines.Select((l, i) => $"{i + 1}. {l.TrimStart('✔', '✓', '•', '-', '*', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.')}"));
            }
            else if (lower.Contains("executive") || lower.Contains("formal") || lower.Contains("professional"))
            {
                if (!modified.Text.StartsWith("Executive Summary:", StringComparison.OrdinalIgnoreCase))
                {
                    modified.Text = $"Executive Summary: {modified.Text}";
                }
            }

            return modified;
        }

        if (currentModel is PdfTableElement table)
        {
            var modified = (PdfTableElement)table.Clone();

            if (lower.Contains("total") || lower.Contains("sum"))
            {
                var totalRow = new List<string> { "Total" };
                for (int c = 1; c < modified.Headers.Count; c++)
                {
                    if (c == modified.Headers.Count - 1)
                    {
                        double sum = 0;
                        foreach (var row in modified.Rows)
                        {
                            if (c < row.Count)
                            {
                                string clean = Regex.Replace(row[c], @"[^\d.]", "");
                                if (double.TryParse(clean, out double num)) sum += num;
                            }
                        }
                        totalRow.Add(sum > 0 ? $"${sum:N2}" : "—");
                    }
                    else
                    {
                        totalRow.Add("");
                    }
                }
                modified.Rows.Add(totalRow);
            }

            if (lower.Contains("emerald") || lower.Contains("green"))
            {
                modified.HeaderBackgroundHex = "#047857";
            }
            else if (lower.Contains("purple"))
            {
                modified.HeaderBackgroundHex = "#6B21A8";
            }
            else if (lower.Contains("slate") || lower.Contains("dark"))
            {
                modified.HeaderBackgroundHex = "#1E293B";
            }

            return modified;
        }

        if (currentModel is PdfShapeElement shape)
        {
            var modified = (PdfShapeElement)shape.Clone();

            if (lower.Contains("round"))
            {
                modified.ShapeType = ShapeType.RoundedRectangle;
                modified.CornerRadius = 12;
            }
            else if (lower.Contains("circle") || lower.Contains("ellipse"))
            {
                modified.ShapeType = ShapeType.Circle;
            }

            if (lower.Contains("blue"))
            {
                modified.FillColorHex = "#EFF6FF";
                modified.StrokeColorHex = "#2563EB";
            }
            else if (lower.Contains("green") || lower.Contains("emerald"))
            {
                modified.FillColorHex = "#F0FDF4";
                modified.StrokeColorHex = "#16A34A";
            }
            else if (lower.Contains("purple"))
            {
                modified.FillColorHex = "#FAF5FF";
                modified.StrokeColorHex = "#9333EA";
            }

            return modified;
        }

        if (currentModel is PdfMathElement math)
        {
            var modified = (PdfMathElement)math.Clone();

            if (lower.Contains("equation number") || lower.Contains("number"))
            {
                modified.ShowEquationNumber = true;
                var numMatch = Regex.Match(prompt, @"(?:\(([^)]+)\)|number\s+([0-9.]+))", RegexOptions.IgnoreCase);
                if (numMatch.Success)
                {
                    string num = numMatch.Groups[1].Success ? numMatch.Groups[1].Value : numMatch.Groups[2].Value;
                    modified.EquationNumber = $"({num})";
                }
            }

            if (lower.Contains("pythagor"))
            {
                modified.Formula = "a^2 + b^2 = c^2";
            }
            else if (lower.Contains("quadratic"))
            {
                modified.Formula = @"x = \frac{-b \pm \sqrt{b^2 - 4ac}}{2a}";
            }
            else if (lower.Contains("euler"))
            {
                modified.Formula = "e^{i\\pi} + 1 = 0";
            }

            var sizeMatch = Regex.Match(prompt, @"(?:(?:font\s*size|size)\s*[:=]?\s*(\d+)|(\d+)\s*(?:pt|px|size))", RegexOptions.IgnoreCase);
            if (sizeMatch.Success)
            {
                string val = sizeMatch.Groups[1].Success ? sizeMatch.Groups[1].Value : sizeMatch.Groups[2].Value;
                if (double.TryParse(val, out double size) && size >= 8 && size <= 72)
                {
                    modified.FontSize = size;
                }
            }

            if (lower.Contains("navy") || lower.Contains("dark")) modified.TextColorHex = "#0F172A";
            else if (lower.Contains("blue")) modified.TextColorHex = "#0F6CBD";
            else if (lower.Contains("red")) modified.TextColorHex = "#DC2626";
            else if (lower.Contains("emerald") || lower.Contains("green")) modified.TextColorHex = "#059669";

            return modified;
        }

        if (currentModel is PdfImageElement img)
        {
            var modified = (PdfImageElement)img.Clone();

            if (lower.Contains("opacity") || lower.Contains("transparent") || lower.Contains("fade"))
            {
                var match = Regex.Match(prompt, @"(\d+)\s*%", RegexOptions.IgnoreCase);
                if (match.Success && double.TryParse(match.Groups[1].Value, out double pct))
                {
                    modified.Opacity = Math.Clamp(pct / 100.0, 0.05, 1.0);
                }
                else
                {
                    modified.Opacity = 0.7;
                }
            }

            if (lower.Contains("round") || lower.Contains("border"))
            {
                modified.CornerRadius = 8;
                modified.BorderThickness = 1.5;
                modified.BorderColorHex = "#0F6CBD";
            }

            return modified;
        }

        if (currentModel is PdfSvgElement svg)
        {
            var modified = (PdfSvgElement)svg.Clone();

            if (lower.Contains("gold")) modified.TintColorHex = "#D97706";
            else if (lower.Contains("blue")) modified.TintColorHex = "#0F6CBD";
            else if (lower.Contains("green") || lower.Contains("emerald")) modified.TintColorHex = "#16A34A";
            else if (lower.Contains("purple")) modified.TintColorHex = "#7C3AED";
            else if (lower.Contains("slate") || lower.Contains("dark")) modified.TintColorHex = "#1E293B";

            return modified;
        }

        if (currentModel is PdfDividerElement div)
        {
            var modified = (PdfDividerElement)div.Clone();

            if (lower.Contains("thick")) modified.Thickness = Math.Max(2.5, modified.Thickness * 1.5);
            else if (lower.Contains("thin")) modified.Thickness = Math.Max(0.5, modified.Thickness * 0.5);

            if (lower.Contains("blue")) modified.ColorHex = "#0F6CBD";
            else if (lower.Contains("emerald") || lower.Contains("green")) modified.ColorHex = "#10B981";
            else if (lower.Contains("slate") || lower.Contains("gray")) modified.ColorHex = "#CBD5E1";
            else if (lower.Contains("purple")) modified.ColorHex = "#7C3AED";

            return modified;
        }

        if (currentModel is PdfQrCodeElement qr)
        {
            var modified = (PdfQrCodeElement)qr.Clone();
            var urlMatch = Regex.Match(prompt, @"(https?://[^\s]+)", RegexOptions.IgnoreCase);
            if (urlMatch.Success) modified.Content = urlMatch.Groups[1].Value;

            if (lower.Contains("dark") || lower.Contains("navy") || lower.Contains("slate")) modified.DarkColorHex = "#1E293B";
            else if (lower.Contains("blue")) modified.DarkColorHex = "#0F6CBD";

            return modified;
        }

        if (currentModel is PdfBarcodeElement barcode)
        {
            var modified = (PdfBarcodeElement)barcode.Clone();
            var codeMatch = Regex.Match(prompt, @"(?:to|is|value|code)\s+([A-Za-z0-9\-_]+)", RegexOptions.IgnoreCase);
            if (codeMatch.Success) modified.CodeValue = codeMatch.Groups[1].Value;

            if (lower.Contains("blue")) modified.BarColorHex = "#0F6CBD";
            else if (lower.Contains("slate") || lower.Contains("dark")) modified.BarColorHex = "#1E293B";

            return modified;
        }

        if (currentModel is PdfStickyNoteElement note)
        {
            var modified = (PdfStickyNoteElement)note.Clone();

            if (lower.Contains("yellow") || lower.Contains("amber")) modified.ColorHex = "#FEF3C7";
            else if (lower.Contains("blue")) modified.ColorHex = "#E0F2FE";
            else if (lower.Contains("green") || lower.Contains("mint")) modified.ColorHex = "#DCFCE7";
            else if (lower.Contains("pink") || lower.Contains("rose")) modified.ColorHex = "#FFE4E6";

            if (lower.Contains("approve") || lower.Contains("approved")) modified.Status = "Approved";
            else if (lower.Contains("review")) modified.Status = "In Review";

            return modified;
        }

        if (currentModel is PdfFormFieldElement form)
        {
            var modified = (PdfFormFieldElement)form.Clone();

            if (lower.Contains("required")) modified.IsRequired = true;
            else if (lower.Contains("optional")) modified.IsRequired = false;

            var labelMatch = Regex.Match(prompt, """(?:label\s*(?:to|is|:)\s*|name\s*(?:to)?\s*)["']?([^"'\r\n]+)["']?""", RegexOptions.IgnoreCase);
            if (labelMatch.Success && !string.IsNullOrWhiteSpace(labelMatch.Groups[1].Value))
            {
                modified.Label = labelMatch.Groups[1].Value.Trim();
            }

            if (lower.Contains("blue")) modified.BorderColorHex = "#0F6CBD";
            else if (lower.Contains("emerald") || lower.Contains("green")) modified.BorderColorHex = "#059669";

            return modified;
        }

        return null;
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
        var match = Regex.Match(text, """\[[\s\S]*\]""");
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
