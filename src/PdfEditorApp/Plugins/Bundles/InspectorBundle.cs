using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing modular properties inspector sections.
/// </summary>
public class InspectorBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.Inspector";
    public string Name => "Properties Inspector Bundle";
    public string Description => "Dynamic inspector property cards: Element Diagnostics, Table Grid Specifications, Chart Series, and Barcode Encoding Details.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new GeometryInspectorPlugin(),
        new AppearanceInspectorPlugin(),
        new TypographyInspectorPlugin(),
        new TableGridInspectorPlugin(),
        new ChartVisualizationInspectorPlugin(),
        new ElementDiagnosticsInspectorPlugin(),
        new BarcodeDetailsInspectorPlugin()
    };
}

public class GeometryInspectorPlugin : IFryPlugin
{
    public string Id => "frypdf.inspector.geometry";
    public string Name => "Geometry & Transform Inspector Card";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterInspectorSection(new InspectorSectionDescriptor
        {
            SectionId = "frypdf.inspector.geometry",
            Title = "Custom Geometry",
            IconKind = "VectorRectangle",
            Order = 10,
            AppliesTo = target => false,
            Factory = (sp, target) => "Geometry specification"
        });

        return Task.CompletedTask;
    }
}

public class AppearanceInspectorPlugin : IFryPlugin
{
    public string Id => "frypdf.inspector.appearance";
    public string Name => "Appearance & Styling Inspector Card";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterInspectorSection(new InspectorSectionDescriptor
        {
            SectionId = "frypdf.inspector.appearance",
            Title = "Custom Appearance",
            IconKind = "PaletteOutline",
            Order = 20,
            AppliesTo = target => false,
            Factory = (sp, target) => "Appearance specification"
        });

        return Task.CompletedTask;
    }
}

public class TypographyInspectorPlugin : IFryPlugin
{
    public string Id => "frypdf.inspector.typography";
    public string Name => "Typography & Font Inspector Card";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterInspectorSection(new InspectorSectionDescriptor
        {
            SectionId = "frypdf.inspector.typography",
            Title = "Custom Typography",
            IconKind = "FormatFont",
            Order = 30,
            AppliesTo = target => false,
            Factory = (sp, target) => "Typography specification"
        });

        return Task.CompletedTask;
    }
}

public class TableGridInspectorPlugin : IFryPlugin
{
    public string Id => "frypdf.inspector.table";
    public string Name => "Table Grid Inspector Card";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterInspectorSection(new InspectorSectionDescriptor
        {
            SectionId = "frypdf.inspector.table",
            Title = "Table Grid Properties",
            IconKind = "TableLarge",
            Order = 40,
            AppliesTo = target => target is TableElementViewModel,
            Factory = (sp, target) =>
            {
                if (target is TableElementViewModel table)
                {
                    return $"Columns: {table.Headers.Count} | Rows: {table.Rows.Count}\nGrid Border: {table.BorderColorHex}";
                }
                return "Table specification";
            }
        });

        return Task.CompletedTask;
    }
}

public class ChartVisualizationInspectorPlugin : IFryPlugin
{
    public string Id => "frypdf.inspector.chart";
    public string Name => "Chart Series Inspector Card";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterInspectorSection(new InspectorSectionDescriptor
        {
            SectionId = "frypdf.inspector.chart",
            Title = "Chart Series & Visualization",
            IconKind = "ChartLine",
            Order = 50,
            AppliesTo = target => target is ChartElementViewModel,
            Factory = (sp, target) =>
            {
                if (target is ChartElementViewModel chart)
                {
                    return $"Chart Type: {chart.ChartTypeDescription}\nPalette: {chart.Palette}\nLegend: {chart.LegendPosition}";
                }
                return "Chart specification";
            }
        });

        return Task.CompletedTask;
    }
}

public class ElementDiagnosticsInspectorPlugin : IFryPlugin
{
    public string Id => "frypdf.inspector.diagnostics";
    public string Name => "Element Diagnostics Inspector Card";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterInspectorSection(new InspectorSectionDescriptor
        {
            SectionId = "frypdf.inspector.diagnostics",
            Title = "Element Diagnostics",
            IconKind = "InformationOutline",
            Order = 90,
            AppliesTo = target => target is ElementViewModelBase,
            Factory = (sp, target) =>
            {
                if (target is ElementViewModelBase el)
                {
                    string elType = el.GetType().Name.Replace("ViewModel", "");
                    return $"Type: {elType}\nLayer: Z-Index {el.ZIndex}\nBounds: {el.Width:F0} × {el.Height:F0} pt (X: {el.X:F0}, Y: {el.Y:F0})";
                }
                return "No element selected";
            }
        });

        return Task.CompletedTask;
    }
}

public class BarcodeDetailsInspectorPlugin : IFryPlugin
{
    public string Id => "frypdf.inspector.barcode";
    public string Name => "Barcode Details Inspector Card";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterInspectorSection(new InspectorSectionDescriptor
        {
            SectionId = "frypdf.inspector.barcode",
            Title = "Barcode & Code Encoding",
            IconKind = "Barcode",
            Order = 45,
            AppliesTo = target => target is BarcodeElementViewModel || target is QrCodeElementViewModel,
            Factory = (sp, target) =>
            {
                if (target is QrCodeElementViewModel qr)
                {
                    return $"QR Code Payload:\n{qr.Content}";
                }
                if (target is BarcodeElementViewModel bc)
                {
                    return $"Barcode ({bc.BarcodeFormat}):\n{bc.CodeValue}";
                }
                return "Encoding details";
            }
        });

        return Task.CompletedTask;
    }
}
