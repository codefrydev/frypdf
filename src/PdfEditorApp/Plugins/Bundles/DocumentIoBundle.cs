using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Export;
using PdfEditorApp.Services.Import;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing all built-in document importers and multi-format exporters.
/// </summary>
public class DocumentIoBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.DocumentIo";
    public string Name => "Document I/O (Import & Export) Bundle";
    public string Description => "Comprehensive document import/export formats: PDF, Images (PNG/JPEG/WebP), Markdown, HTML5, Plain Text, and SVG.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new PdfIoPlugin(),
        new ImageIoPlugin(),
        new TextIoPlugin(),
        new WebVectorIoPlugin()
    };
}

public class PdfIoPlugin : IFryPlugin
{
    public string Id => "frypdf.io.pdf";
    public string Name => "Standard PDF Engine I/O";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        // Importer
        ctx.RegisterImporter(new PdfDeconstructionImporter());

        // Exporter
        var exportService = ctx.GetService<IPdfExportService>();
        ctx.RegisterExporter(new PdfDocumentExporter(exportService));

        return Task.CompletedTask;
    }
}

public class ImageIoPlugin : IFryPlugin
{
    public string Id => "frypdf.io.images";
    public string Name => "Raster Image Document I/O";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterImporter(new ImageDocumentImporter());
        return Task.CompletedTask;
    }
}

public class TextIoPlugin : IFryPlugin
{
    public string Id => "frypdf.io.text";
    public string Name => "Text & Markdown Document I/O";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterImporter(new PlainTextDocumentImporter());
        ctx.RegisterExporter(new MarkdownDocumentExporter());
        ctx.RegisterExporter(new PlainTextDocumentExporter());
        return Task.CompletedTask;
    }
}

public class WebVectorIoPlugin : IFryPlugin
{
    public string Id => "frypdf.io.webvector";
    public string Name => "HTML5 & SVG Vector Document Exporter";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterExporter(new HtmlDocumentExporter());
        ctx.RegisterExporter(new SvgVectorExporter());
        return Task.CompletedTask;
    }
}
