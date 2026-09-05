using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing all built-in canvas elements for FryPDF.
/// Enables profile-level enabling/disabling (e.g. minimal viewer vs full publisher).
/// </summary>
public class CanvasElementsBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.CanvasElements";
    public string Name => "Canvas Elements Bundle";
    public string Description => "Core canvas elements: typography, geometry, charts, tables, LaTeX equations, form controls, and signatures.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new TextElementPlugin(),
        new ShapeElementPlugin(),
        new MediaElementPlugin(),
        new DataVisualsElementPlugin(),
        new MathElementPlugin(),
        new FormElementsPlugin(),
        new MarkupElementsPlugin()
    };
}

public class TextElementPlugin : IFryPlugin
{
    public string Id => "frypdf.element.plugin.text";
    public string Name => "Typography Elements";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.text",
            DisplayName = "Text Block",
            ModelType = typeof(PdfTextElement),
            ViewModelType = typeof(TextElementViewModel),
            IconKind = "FormatColorText",
            DefaultWidth = 200,
            DefaultHeight = 60,
            CanInsertFromToolbar = true,
            InsertionCategory = "Basic",
            ShortcutKey = "T",
            SortOrder = 10,
            Tags = new[] { "text", "typography", "heading", "label" },
            Factory = (sp, m) => new TextElementViewModel()
        });

        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.divider",
            DisplayName = "Divider Line",
            ModelType = typeof(PdfDividerElement),
            ViewModelType = typeof(DividerElementViewModel),
            IconKind = "Minus",
            DefaultWidth = 300,
            DefaultHeight = 20,
            CanInsertFromToolbar = true,
            InsertionCategory = "Basic",
            SortOrder = 20,
            Tags = new[] { "divider", "line", "separator" },
            Factory = (sp, m) => new DividerElementViewModel()
        });

        return Task.CompletedTask;
    }
}

public class ShapeElementPlugin : IFryPlugin
{
    public string Id => "frypdf.element.plugin.shape";
    public string Name => "Geometric Shapes";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.shape",
            DisplayName = "Geometric Shape",
            ModelType = typeof(PdfShapeElement),
            ViewModelType = typeof(ShapeElementViewModel),
            IconKind = "ShapeOutline",
            DefaultWidth = 150,
            DefaultHeight = 100,
            CanInsertFromToolbar = true,
            InsertionCategory = "Basic",
            ShortcutKey = "R",
            SortOrder = 30,
            Tags = new[] { "shape", "rectangle", "circle", "arrow", "polygon" },
            Factory = (sp, m) => new ShapeElementViewModel()
        });

        return Task.CompletedTask;
    }
}

public class MediaElementPlugin : IFryPlugin
{
    public string Id => "frypdf.element.plugin.media";
    public string Name => "Media Graphics";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.image",
            DisplayName = "Image Graphic",
            ModelType = typeof(PdfImageElement),
            ViewModelType = typeof(ImageElementViewModel),
            IconKind = "ImageOutline",
            DefaultWidth = 200,
            DefaultHeight = 150,
            CanInsertFromToolbar = true,
            InsertionCategory = "Media",
            ShortcutKey = "I",
            SortOrder = 40,
            Tags = new[] { "image", "photo", "bitmap", "picture" },
            Factory = (sp, m) => new ImageElementViewModel()
        });

        return Task.CompletedTask;
    }
}

public class DataVisualsElementPlugin : IFryPlugin
{
    public string Id => "frypdf.element.plugin.datavisuals";
    public string Name => "Data Visualizations";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.table",
            DisplayName = "Data Table",
            ModelType = typeof(PdfTableElement),
            ViewModelType = typeof(TableElementViewModel),
            IconKind = "TableLarge",
            DefaultWidth = 400,
            DefaultHeight = 160,
            CanInsertFromToolbar = true,
            InsertionCategory = "Tables & Data",
            SortOrder = 50,
            Tags = new[] { "table", "grid", "data", "columns", "rows" },
            Factory = (sp, m) => new TableElementViewModel()
        });

        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.chart",
            DisplayName = "Interactive Chart",
            ModelType = typeof(PdfChartElement),
            ViewModelType = typeof(ChartElementViewModel),
            IconKind = "ChartBar",
            DefaultWidth = 400,
            DefaultHeight = 250,
            CanInsertFromToolbar = true,
            InsertionCategory = "Tables & Data",
            SortOrder = 60,
            Tags = new[] { "chart", "graph", "bar", "pie", "line", "candlestick", "radar", "waterfall" },
            Factory = (sp, m) => new ChartElementViewModel()
        });

        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.qrcode",
            DisplayName = "QR Code",
            ModelType = typeof(PdfQrCodeElement),
            ViewModelType = typeof(QrCodeElementViewModel),
            IconKind = "Qrcode",
            DefaultWidth = 120,
            DefaultHeight = 120,
            CanInsertFromToolbar = true,
            InsertionCategory = "Tables & Data",
            SortOrder = 70,
            Tags = new[] { "qrcode", "barcode", "url", "2d" },
            Factory = (sp, m) => new QrCodeElementViewModel()
        });

        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.barcode",
            DisplayName = "Barcode",
            ModelType = typeof(PdfBarcodeElement),
            ViewModelType = typeof(BarcodeElementViewModel),
            IconKind = "Barcode",
            DefaultWidth = 200,
            DefaultHeight = 80,
            CanInsertFromToolbar = true,
            InsertionCategory = "Tables & Data",
            SortOrder = 80,
            Tags = new[] { "barcode", "code128", "ean", "optical" },
            Factory = (sp, m) => new BarcodeElementViewModel()
        });

        return Task.CompletedTask;
    }
}

public class MathElementPlugin : IFryPlugin
{
    public string Id => "frypdf.element.plugin.math";
    public string Name => "Math Equations (LaTeX)";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.math",
            DisplayName = "Math Equation",
            ModelType = typeof(PdfMathElement),
            ViewModelType = typeof(MathElementViewModel),
            IconKind = "Sigma",
            DefaultWidth = 240,
            DefaultHeight = 60,
            CanInsertFromToolbar = true,
            InsertionCategory = "Equations",
            ShortcutKey = "M",
            SortOrder = 90,
            Tags = new[] { "math", "formula", "equation", "latex", "integral", "matrix" },
            Factory = (sp, m) => new MathElementViewModel()
        });

        return Task.CompletedTask;
    }
}

public class FormElementsPlugin : IFryPlugin
{
    public string Id => "frypdf.element.plugin.forms";
    public string Name => "Interactive Form Controls";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.formfield",
            DisplayName = "Form Field",
            ModelType = typeof(PdfFormFieldElement),
            ViewModelType = typeof(FormFieldElementViewModel),
            IconKind = "TextBoxOutline",
            DefaultWidth = 180,
            DefaultHeight = 36,
            CanInsertFromToolbar = true,
            InsertionCategory = "Forms",
            SortOrder = 100,
            Tags = new[] { "form", "field", "input", "checkbox", "dropdown" },
            Factory = (sp, m) => new FormFieldElementViewModel()
        });

        return Task.CompletedTask;
    }
}

public class MarkupElementsPlugin : IFryPlugin
{
    public string Id => "frypdf.element.plugin.markup";
    public string Name => "Stamps, Redaction & Markup";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.watermark",
            DisplayName = "Watermark",
            ModelType = typeof(PdfWatermarkElement),
            ViewModelType = typeof(WatermarkElementViewModel),
            IconKind = "Watermark",
            DefaultWidth = 400,
            DefaultHeight = 120,
            CanInsertFromToolbar = true,
            InsertionCategory = "Annotations",
            SortOrder = 110,
            Tags = new[] { "watermark", "draft", "confidential", "diagonal" },
            Factory = (sp, m) => new WatermarkElementViewModel()
        });

        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.redaction",
            DisplayName = "Redaction Annotation",
            ModelType = typeof(PdfRedactionElement),
            ViewModelType = typeof(RedactionElementViewModel),
            IconKind = "EyeOffOutline",
            DefaultWidth = 160,
            DefaultHeight = 40,
            CanInsertFromToolbar = true,
            InsertionCategory = "Annotations",
            SortOrder = 120,
            Tags = new[] { "redact", "redaction", "blackout", "security" },
            Factory = (sp, m) => new RedactionElementViewModel()
        });

        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.stickynote",
            DisplayName = "Sticky Review Note",
            ModelType = typeof(PdfStickyNoteElement),
            ViewModelType = typeof(StickyNoteElementViewModel),
            IconKind = "NoteOutline",
            DefaultWidth = 180,
            DefaultHeight = 140,
            CanInsertFromToolbar = true,
            InsertionCategory = "Annotations",
            ShortcutKey = "N",
            SortOrder = 130,
            Tags = new[] { "note", "comment", "review", "annotation", "sticky" },
            Factory = (sp, m) => new StickyNoteElementViewModel()
        });

        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.ink",
            DisplayName = "Freehand Drawing",
            ModelType = typeof(PdfInkElement),
            ViewModelType = typeof(InkElementViewModel),
            IconKind = "DrawPen",
            DefaultWidth = 150,
            DefaultHeight = 100,
            CanInsertFromToolbar = true,
            InsertionCategory = "Annotations",
            ShortcutKey = "D",
            SortOrder = 140,
            Tags = new[] { "ink", "pen", "draw", "highlight", "marker" },
            Factory = (sp, m) => new InkElementViewModel()
        });

        ctx.RegisterCanvasElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.measurement",
            DisplayName = "Measurement Dimension",
            ModelType = typeof(PdfMeasurementElement),
            ViewModelType = typeof(MeasurementElementViewModel),
            IconKind = "Ruler",
            DefaultWidth = 200,
            DefaultHeight = 30,
            CanInsertFromToolbar = true,
            InsertionCategory = "Annotations",
            SortOrder = 150,
            Tags = new[] { "measure", "dimension", "ruler", "cad" },
            Factory = (sp, m) => new MeasurementElementViewModel()
        });

        return Task.CompletedTask;
    }
}
