using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.Services.Canvas;

/// <summary>
/// Combined registry and factory interface for canvas element descriptors and ViewModels.
/// </summary>
public interface ICanvasElementService : ICanvasElementRegistry
{
    /// <summary>Creates and initializes a ViewModel matching the specified domain model.</summary>
    ElementViewModelBase CreateViewModel(PdfElementBase model);

    /// <summary>Creates a default ViewModel for the given element type ID.</summary>
    ElementViewModelBase CreateViewModel(string elementTypeId);

    /// <summary>Clones an element ViewModel by serializing to model and re-instantiating.</summary>
    ElementViewModelBase CloneViewModel(ElementViewModelBase source);
}

/// <summary>
/// Thread-safe implementation of <see cref="ICanvasElementService"/> supporting both
/// core built-in canvas elements and dynamic plugin-contributed element types.
/// </summary>
public class CanvasElementRegistry : ICanvasElementService
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly ConcurrentDictionary<string, CanvasElementDescriptor> _descriptorsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Type, CanvasElementDescriptor> _descriptorsByModelType = new();

    public CanvasElementRegistry(IServiceProvider? serviceProvider = null, bool seedBuiltIns = true)
    {
        _serviceProvider = serviceProvider;
        if (seedBuiltIns)
        {
            RegisterBuiltInElements();
        }
    }

    private void RegisterBuiltInElements()
    {
        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.text",
            DisplayName = "Text Block",
            ModelType = typeof(PdfTextElement),
            ViewModelType = typeof(TextElementViewModel),
            IconKind = "FormatText",
            DefaultWidth = 200,
            DefaultHeight = 60,
            Tags = new[] { "text", "typography", "heading", "label" },
            Factory = (sp, m) => new TextElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.image",
            DisplayName = "Image",
            ModelType = typeof(PdfImageElement),
            ViewModelType = typeof(ImageElementViewModel),
            IconKind = "ImageOutline",
            DefaultWidth = 200,
            DefaultHeight = 150,
            Tags = new[] { "image", "photo", "bitmap", "picture" },
            Factory = (sp, m) => new ImageElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.shape",
            DisplayName = "Geometric Shape",
            ModelType = typeof(PdfShapeElement),
            ViewModelType = typeof(ShapeElementViewModel),
            IconKind = "ShapeOutline",
            DefaultWidth = 150,
            DefaultHeight = 100,
            Tags = new[] { "shape", "rectangle", "circle", "arrow", "polygon" },
            Factory = (sp, m) => new ShapeElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.divider",
            DisplayName = "Divider Line",
            ModelType = typeof(PdfDividerElement),
            ViewModelType = typeof(DividerElementViewModel),
            IconKind = "Minus",
            DefaultWidth = 300,
            DefaultHeight = 20,
            Tags = new[] { "divider", "line", "separator" },
            Factory = (sp, m) => new DividerElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.table",
            DisplayName = "Data Grid / Table",
            ModelType = typeof(PdfTableElement),
            ViewModelType = typeof(TableElementViewModel),
            IconKind = "Table",
            DefaultWidth = 350,
            DefaultHeight = 180,
            Tags = new[] { "table", "grid", "rows", "columns", "data" },
            Factory = (sp, m) => new TableElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.chart",
            DisplayName = "Interactive Chart",
            ModelType = typeof(PdfChartElement),
            ViewModelType = typeof(ChartElementViewModel),
            IconKind = "ChartBar",
            DefaultWidth = 320,
            DefaultHeight = 220,
            Tags = new[] { "chart", "bar", "line", "pie", "statistics", "data" },
            Factory = (sp, m) => new ChartElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.watermark",
            DisplayName = "Watermark",
            ModelType = typeof(PdfWatermarkElement),
            ViewModelType = typeof(WatermarkElementViewModel),
            IconKind = "Watermark",
            DefaultWidth = 400,
            DefaultHeight = 120,
            Tags = new[] { "watermark", "stamp", "draft", "confidential" },
            Factory = (sp, m) => new WatermarkElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.formfield",
            DisplayName = "Form Field",
            ModelType = typeof(PdfFormFieldElement),
            ViewModelType = typeof(FormFieldElementViewModel),
            IconKind = "TextBoxOutline",
            DefaultWidth = 180,
            DefaultHeight = 36,
            Tags = new[] { "form", "field", "input", "checkbox", "dropdown" },
            Factory = (sp, m) => new FormFieldElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.qrcode",
            DisplayName = "QR Code",
            ModelType = typeof(PdfQrCodeElement),
            ViewModelType = typeof(QrCodeElementViewModel),
            IconKind = "Qrcode",
            DefaultWidth = 120,
            DefaultHeight = 120,
            Tags = new[] { "qr", "qrcode", "barcode", "link" },
            Factory = (sp, m) => new QrCodeElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.barcode",
            DisplayName = "Barcode",
            ModelType = typeof(PdfBarcodeElement),
            ViewModelType = typeof(BarcodeElementViewModel),
            IconKind = "Barcode",
            DefaultWidth = 200,
            DefaultHeight = 80,
            Tags = new[] { "barcode", "code128", "ean", "upc" },
            Factory = (sp, m) => new BarcodeElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.redaction",
            DisplayName = "Redaction Annotation",
            ModelType = typeof(PdfRedactionElement),
            ViewModelType = typeof(RedactionElementViewModel),
            IconKind = "EyeOffOutline",
            DefaultWidth = 160,
            DefaultHeight = 40,
            Tags = new[] { "redact", "redaction", "blackout", "security" },
            Factory = (sp, m) => new RedactionElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.ink",
            DisplayName = "Freehand Drawing",
            ModelType = typeof(PdfInkElement),
            ViewModelType = typeof(InkElementViewModel),
            IconKind = "Draw",
            DefaultWidth = 150,
            DefaultHeight = 100,
            Tags = new[] { "ink", "draw", "pencil", "pen", "signature" },
            Factory = (sp, m) => new InkElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.stickynote",
            DisplayName = "Sticky Note",
            ModelType = typeof(PdfStickyNoteElement),
            ViewModelType = typeof(StickyNoteElementViewModel),
            IconKind = "NoteOutline",
            DefaultWidth = 180,
            DefaultHeight = 140,
            Tags = new[] { "note", "sticky", "comment", "annotation" },
            Factory = (sp, m) => new StickyNoteElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.measurement",
            DisplayName = "Measurement Dimension",
            ModelType = typeof(PdfMeasurementElement),
            ViewModelType = typeof(MeasurementElementViewModel),
            IconKind = "Ruler",
            DefaultWidth = 200,
            DefaultHeight = 40,
            Tags = new[] { "ruler", "measure", "dimension", "distance" },
            Factory = (sp, m) => new MeasurementElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.svg",
            DisplayName = "Vector SVG Artwork",
            ModelType = typeof(PdfSvgElement),
            ViewModelType = typeof(SvgElementViewModel),
            IconKind = "VectorSquare",
            DefaultWidth = 180,
            DefaultHeight = 180,
            Tags = new[] { "svg", "vector", "graphic", "illustration" },
            Factory = (sp, m) => new SvgElementViewModel()
        });

        RegisterElement(new CanvasElementDescriptor
        {
            ElementTypeId = "frypdf.element.math",
            DisplayName = "LaTeX Math Formula",
            ModelType = typeof(PdfMathElement),
            ViewModelType = typeof(MathElementViewModel),
            IconKind = "FunctionVariant",
            DefaultWidth = 200,
            DefaultHeight = 80,
            Tags = new[] { "math", "formula", "latex", "equation" },
            Factory = (sp, m) => new MathElementViewModel()
        });
    }

    public void RegisterElement(CanvasElementDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _descriptorsById[descriptor.ElementTypeId] = descriptor;
        _descriptorsByModelType[descriptor.ModelType] = descriptor;
    }

    public bool UnregisterElement(string elementTypeId)
    {
        if (_descriptorsById.TryRemove(elementTypeId, out var descriptor))
        {
            _descriptorsByModelType.TryRemove(descriptor.ModelType, out _);
            return true;
        }
        return false;
    }

    public CanvasElementDescriptor? GetDescriptor(string elementTypeId)
    {
        return _descriptorsById.TryGetValue(elementTypeId, out var desc) ? desc : null;
    }

    public CanvasElementDescriptor? GetDescriptorByModelType(Type modelType)
    {
        if (_descriptorsByModelType.TryGetValue(modelType, out var desc))
        {
            return desc;
        }

        // Check assignable types if exact type match not found
        foreach (var kvp in _descriptorsByModelType)
        {
            if (kvp.Key.IsAssignableFrom(modelType))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    public IReadOnlyList<CanvasElementDescriptor> GetAllDescriptors()
    {
        return _descriptorsById.Values.ToList();
    }

    public IReadOnlyList<CanvasElementDescriptor> GetInsertableElements()
    {
        return _descriptorsById.Values
            .Where(d => d.CanInsertFromToolbar)
            .OrderBy(d => d.SortOrder)
            .ToList();
    }

    public ElementViewModelBase CreateViewModel(PdfElementBase model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var descriptor = GetDescriptorByModelType(model.GetType());

        ElementViewModelBase vm;
        if (descriptor?.Factory != null)
        {
            vm = (ElementViewModelBase)descriptor.Factory(_serviceProvider!, model);
        }
        else if (descriptor != null)
        {
            if (_serviceProvider != null)
            {
                vm = (ElementViewModelBase)Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(_serviceProvider, descriptor.ViewModelType);
            }
            else
            {
                vm = (ElementViewModelBase)Activator.CreateInstance(descriptor.ViewModelType)!;
            }
        }
        else
        {
            // Direct fallback to TextElementViewModel
            vm = new TextElementViewModel();
        }

        vm.LoadFromModel(model);
        return vm;
    }

    public ElementViewModelBase CreateViewModel(string elementTypeId)
    {
        var descriptor = GetDescriptor(elementTypeId);
        if (descriptor == null)
        {
            return new TextElementViewModel();
        }

        ElementViewModelBase vm;
        if (descriptor.Factory != null)
        {
            vm = (ElementViewModelBase)descriptor.Factory(_serviceProvider!, null);
        }
        else if (_serviceProvider != null)
        {
            vm = (ElementViewModelBase)Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(_serviceProvider, descriptor.ViewModelType);
        }
        else
        {
            vm = (ElementViewModelBase)Activator.CreateInstance(descriptor.ViewModelType)!;
        }

        vm.Width = descriptor.DefaultWidth;
        vm.Height = descriptor.DefaultHeight;
        return vm;
    }

    public ElementViewModelBase CloneViewModel(ElementViewModelBase source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var model = source.ToModel();
        var vm = CreateViewModel(model);
        return vm;
    }
}
