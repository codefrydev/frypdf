using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Services;
using PdfEditorApp.Services.AI;
using PdfEditorApp.Services.Canvas;
using PdfEditorApp.Services.Export;
using PdfEditorApp.Services.Import;
using PdfEditorApp.Services.Inspector;
using PdfEditorApp.Services.Ocr;
using PdfEditorApp.Services.Palette;
using PdfEditorApp.Services.StatusBar;
using Xunit;

namespace PdfEditorApp.Tests;

public class MicrokernelPillarsTests
{
    private (PluginHost host, FryPluginContext context, ServiceProvider sp) CreateTestEnvironment()
    {
        var services = new ServiceCollection();

        // Registries
        services.AddSingleton<CanvasElementRegistry>();
        services.AddSingleton<ICanvasElementRegistry>(sp => sp.GetRequiredService<CanvasElementRegistry>());
        services.AddSingleton<ICanvasElementService>(sp => sp.GetRequiredService<CanvasElementRegistry>());

        services.AddSingleton<DocumentImporterRegistry>();
        services.AddSingleton<IDocumentImporterRegistry>(sp => sp.GetRequiredService<DocumentImporterRegistry>());

        services.AddSingleton<DocumentExporterRegistry>();
        services.AddSingleton<IDocumentExporterRegistry>(sp => sp.GetRequiredService<DocumentExporterRegistry>());

        services.AddSingleton<AiProviderRegistry>();
        services.AddSingleton<IAiProviderRegistry>(sp => sp.GetRequiredService<AiProviderRegistry>());

        services.AddSingleton<OcrEngineRegistry>();
        services.AddSingleton<IOcrEngineRegistry>(sp => sp.GetRequiredService<OcrEngineRegistry>());

        services.AddSingleton<TemplateService>();
        services.AddSingleton<ITemplateService>(sp => sp.GetRequiredService<TemplateService>());
        services.AddSingleton<ITemplateRegistry>(sp => sp.GetRequiredService<TemplateService>());

        services.AddSingleton<StatusBarRegistry>();
        services.AddSingleton<IStatusBarRegistry>(sp => sp.GetRequiredService<StatusBarRegistry>());

        services.AddSingleton<InspectorRegistry>();
        services.AddSingleton<IInspectorRegistry>(sp => sp.GetRequiredService<InspectorRegistry>());

        services.AddSingleton<CommandPaletteRegistry>();
        services.AddSingleton<ICommandPaletteRegistry>(sp => sp.GetRequiredService<CommandPaletteRegistry>());

        services.AddSingleton<IPdfExportService, PdfExportService>();

        var sp = services.BuildServiceProvider();
        var context = new FryPluginContext(sp);
        var host = new PluginHost(context);

        return (host, context, sp);
    }

    [Fact]
    public async Task CanvasElementsBundle_RegistersAllCanvasElements()
    {
        var (host, context, sp) = CreateTestEnvironment();
        var bundle = new CanvasElementsBundle();

        host.RegisterPlugins(bundle.Plugins);
        await host.StartAsync();

        var elements = context.GetRegisteredCanvasElements();
        Assert.NotEmpty(elements);

        var elementIds = elements.Select(e => e.ElementTypeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("frypdf.element.text", elementIds);
        Assert.Contains("frypdf.element.shape", elementIds);
        Assert.Contains("frypdf.element.image", elementIds);
        Assert.Contains("frypdf.element.table", elementIds);
        Assert.Contains("frypdf.element.chart", elementIds);
        Assert.Contains("frypdf.element.math", elementIds);
        Assert.Contains("frypdf.element.formfield", elementIds);
        Assert.Contains("frypdf.element.watermark", elementIds);
        Assert.Contains("frypdf.element.redaction", elementIds);
        Assert.Contains("frypdf.element.stickynote", elementIds);
        Assert.Contains("frypdf.element.ink", elementIds);
        Assert.Contains("frypdf.element.measurement", elementIds);
    }

    [Fact]
    public async Task DocumentIoBundle_RegistersImportersAndExporters()
    {
        var (host, context, sp) = CreateTestEnvironment();
        var bundle = new DocumentIoBundle();

        host.RegisterPlugins(bundle.Plugins);
        await host.StartAsync();

        var importers = context.GetRegisteredImporters();
        Assert.NotEmpty(importers);

        var importerExtensions = importers.SelectMany(i => i.SupportedExtensions).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains(".pdf", importerExtensions);
        Assert.Contains(".png", importerExtensions);
        Assert.Contains(".md", importerExtensions);

        var exporters = context.GetRegisteredExporters();
        Assert.NotEmpty(exporters);

        var exporterIds = exporters.Select(e => e.ExporterId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("frypdf.exporter.pdf", exporterIds);
        Assert.Contains("frypdf.exporter.markdown", exporterIds);
        Assert.Contains("frypdf.exporter.html", exporterIds);
        Assert.Contains("frypdf.exporter.text", exporterIds);
        Assert.Contains("frypdf.exporter.svg", exporterIds);
    }

    [Fact]
    public async Task AiProvidersBundle_RegistersAllProviders()
    {
        var (host, context, sp) = CreateTestEnvironment();
        var bundle = new AiProvidersBundle();

        host.RegisterPlugins(bundle.Plugins);
        await host.StartAsync();

        var aiRegistry = sp.GetRequiredService<IAiProviderRegistry>();
        var providers = aiRegistry.GetAllProviders();
        Assert.NotEmpty(providers);

        var providerIds = providers.Select(p => p.ProviderId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("ollama", providerIds);
        Assert.Contains("groq", providerIds);
        Assert.Contains("openai", providerIds);
    }

    [Fact]
    public async Task OcrEnginesBundle_RegistersAllOcrEngines()
    {
        var (host, context, sp) = CreateTestEnvironment();
        var bundle = new OcrEnginesBundle();

        host.RegisterPlugins(bundle.Plugins);
        await host.StartAsync();

        var ocrRegistry = sp.GetRequiredService<IOcrEngineRegistry>();
        var engines = ocrRegistry.GetAllEngines();
        Assert.NotEmpty(engines);

        var engineNames = engines.Select(e => e.EngineName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Apple Vision OCR (macOS)", engineNames);
        Assert.Contains("Windows Media OCR", engineNames);
        Assert.Contains("Tesseract OCR", engineNames);
    }

    [Fact]
    public async Task StandardTemplatesBundle_RegistersAll28Templates()
    {
        var (host, context, sp) = CreateTestEnvironment();
        var bundle = new StandardTemplatesBundle();

        host.RegisterPlugins(bundle.Plugins);
        await host.StartAsync();

        var templateService = sp.GetRequiredService<ITemplateService>();
        var templates = templateService.GetAllTemplates();
        Assert.True(templates.Count >= 28, $"Expected >= 28 templates, got {templates.Count}");

        var categories = templates.Select(t => t.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Contains("Corporate", categories);
        Assert.Contains("Finance", categories);
        Assert.Contains("Career", categories);
        Assert.Contains("Academic", categories);
        Assert.Contains("Certificates", categories);
        Assert.Contains("Education", categories);
    }

    [Fact]
    public async Task StatusBarBundle_RegistersWidgets()
    {
        var (host, context, sp) = CreateTestEnvironment();
        var bundle = new StatusBarBundle();

        host.RegisterPlugins(bundle.Plugins);
        await host.StartAsync();

        var widgets = context.GetRegisteredStatusBarWidgets();
        Assert.NotEmpty(widgets);

        var widgetIds = widgets.Select(w => w.WidgetId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("frypdf.status.pagestats", widgetIds);
        Assert.Contains("frypdf.status.ready", widgetIds);
        Assert.Contains("frypdf.status.memory", widgetIds);
    }

    [Fact]
    public async Task InspectorBundle_RegistersInspectorSections()
    {
        var (host, context, sp) = CreateTestEnvironment();
        var bundle = new InspectorBundle();

        host.RegisterPlugins(bundle.Plugins);
        await host.StartAsync();

        var sections = context.GetRegisteredInspectorSections();
        Assert.NotEmpty(sections);

        var sectionIds = sections.Select(s => s.SectionId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("frypdf.inspector.geometry", sectionIds);
        Assert.Contains("frypdf.inspector.appearance", sectionIds);
        Assert.Contains("frypdf.inspector.typography", sectionIds);
        Assert.Contains("frypdf.inspector.table", sectionIds);
        Assert.Contains("frypdf.inspector.chart", sectionIds);
    }

    [Fact]
    public async Task CommandPaletteBundle_RegistersCommands()
    {
        var (host, context, sp) = CreateTestEnvironment();
        var bundle = new CommandPaletteBundle();

        host.RegisterPlugins(bundle.Plugins);
        await host.StartAsync();

        var commands = context.GetRegisteredCommands();
        Assert.NotEmpty(commands);

        var categories = commands.Select(c => c.Category).Distinct().ToList();
        Assert.Contains("File", categories);
        Assert.Contains("Edit", categories);
        Assert.Contains("Insert", categories);
        Assert.Contains("View", categories);
        Assert.Contains("Security", categories);
    }

    [Fact]
    public void DesktopProfile_ContainsAll16Bundles()
    {
        var profilePath = Path.Combine(AppContext.BaseDirectory, "profiles", "desktop.profile.json");
        if (!File.Exists(profilePath))
        {
            // Fallback to source tree path if not copied to bin
            profilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "profiles", "desktop.profile.json"));
        }

        Assert.True(File.Exists(profilePath), $"Profile not found at {profilePath}");
        var profile = ProfileLoader.LoadFromFile(profilePath);

        Assert.Equal("desktop", profile.ProfileName);
        Assert.Equal(16, profile.Bundles.Count);
        Assert.Contains("FryPdf.Bundle.CanvasElements", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.DocumentIo", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.AiProviders", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.OcrEngines", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.Templates", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.StatusBar", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.Inspector", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.CommandPalette", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.WorkspacePages", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.Dialogs", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.EditorSidebars", profile.Bundles);
    }

    [Fact]
    public void HeadlessProfile_LoadsHeadlessBundles()
    {
        var profilePath = Path.Combine(AppContext.BaseDirectory, "profiles", "headless.profile.json");
        if (!File.Exists(profilePath))
        {
            profilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "profiles", "headless.profile.json"));
        }

        Assert.True(File.Exists(profilePath), $"Profile not found at {profilePath}");
        var profile = ProfileLoader.LoadFromFile(profilePath);

        Assert.Equal("headless", profile.ProfileName);
        Assert.Contains("FryPdf.Bundle.Tools.Organize", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.DocumentIo", profile.Bundles);
        Assert.DoesNotContain("FryPdf.Bundle.StatusBar", profile.Bundles);
    }
}
