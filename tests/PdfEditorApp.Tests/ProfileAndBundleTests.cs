using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Models;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Intelligence;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using Xunit;

namespace PdfEditorApp.Tests;

public class ProfileAndBundleTests
{
    private class DummyOperationsService : IPdfDocumentOperationsService
    {
        public IPdfToolRegistry ToolRegistry => null!;
        public IPdfPageService PageService => null!;
        public IPdfOptimizationService OptimizationService => null!;
        public IPdfSecurityService SecurityService => null!;
        public IPdfConversionService ConversionService => null!;
        public IPdfOcrService OcrService => null!;
        public IPdfFormService FormService => null!;
        public IAiDocumentService AiService => null!;
        public IDocumentTranslationService TranslationService => null!;
        public IPdfWorkflowEngine WorkflowEngine => null!;

        public Task<ToolExecutionResult> ExecuteToolAsync(PdfToolId toolId, object options, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            return Task.FromResult(new ToolExecutionResult { Success = true });
        }
    }

    [Fact]
    public void ProfileLoader_ParseValidJson_Succeeds()
    {
        var json = """
        {
          "name": "Custom Profile",
          "description": "Test custom profile configuration",
          "version": "1.0.0",
          "bundles": [
            "FryPdf.Bundle.Tools.Organize",
            "FryPdf.Bundle.Tools.Security"
          ],
          "enabledPlugins": [
            "frypdf.tool.merge",
            "frypdf.tool.split"
          ],
          "disabledPlugins": [
            "frypdf.tool.compress"
          ],
          "settings": {
            "maxMemoryMb": "1024",
            "enableTelemetry": "false"
          }
        }
        """;

        var profile = ProfileLoader.LoadProfileFromJson(json);

        Assert.NotNull(profile);
        Assert.Equal("Custom Profile", profile.Name);
        Assert.Equal("1.0.0", profile.Version);
        Assert.Contains("FryPdf.Bundle.Tools.Organize", profile.Bundles);
        Assert.Contains("FryPdf.Bundle.Tools.Security", profile.Bundles);
        Assert.Contains("frypdf.tool.merge", profile.EnabledPlugins);
        Assert.Contains("frypdf.tool.compress", profile.DisabledPlugins);
        Assert.True(profile.IsPluginEnabled("frypdf.tool.merge"));
        Assert.False(profile.IsPluginEnabled("frypdf.tool.compress"));
        Assert.True(profile.IsBundleEnabled("FryPdf.Bundle.Tools.Organize"));
    }

    [Fact]
    public void ProfileLoader_LoadFromFile_ReadsCorrectly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"frypdf_profile_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, """
            {
              "name": "Temp Test Profile",
              "version": "2.0.0"
            }
            """);

            var profile = ProfileLoader.LoadProfileFromFile(tempFile);
            Assert.NotNull(profile);
            Assert.Equal("Temp Test Profile", profile.Name);
            Assert.Equal("2.0.0", profile.Version);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Bundles_InstantiateAndRegisterCorrectPlugins()
    {
        var organizeBundle = new ToolsOrganizeBundle();
        Assert.Equal("FryPdf.Bundle.Tools.Organize", organizeBundle.Id);
        Assert.Equal(6, organizeBundle.Plugins.Count);
        Assert.Contains(organizeBundle.Plugins, p => p.Id == "frypdf.tool.merge");
        Assert.Contains(organizeBundle.Plugins, p => p.Id == "frypdf.tool.split");
        Assert.Contains(organizeBundle.Plugins, p => p.Id == "frypdf.tool.rotate");
        Assert.Contains(organizeBundle.Plugins, p => p.Id == "frypdf.tool.organize");
        Assert.Contains(organizeBundle.Plugins, p => p.Id == "frypdf.tool.crop");
        Assert.Contains(organizeBundle.Plugins, p => p.Id == "frypdf.tool.pagenumbers");

        var securityBundle = new ToolsSecurityBundle();
        Assert.Equal("FryPdf.Bundle.Tools.Security", securityBundle.Id);
        Assert.Equal(7, securityBundle.Plugins.Count);
        Assert.Contains(securityBundle.Plugins, p => p.Id == "frypdf.tool.compress");
        Assert.Contains(securityBundle.Plugins, p => p.Id == "frypdf.tool.protect");
        Assert.Contains(securityBundle.Plugins, p => p.Id == "frypdf.tool.unlock");
        Assert.Contains(securityBundle.Plugins, p => p.Id == "frypdf.tool.sign");

        var conversionBundle = new ToolsConversionBundle();
        Assert.Equal("FryPdf.Bundle.Tools.Conversion", conversionBundle.Id);
        Assert.Equal(12, conversionBundle.Plugins.Count);
        Assert.Contains(conversionBundle.Plugins, p => p.Id == "frypdf.tool.pdftoword");
        Assert.Contains(conversionBundle.Plugins, p => p.Id == "frypdf.tool.wordtopdf");

        var intelligenceBundle = new ToolsIntelligenceBundle();
        Assert.Equal("FryPdf.Bundle.Tools.Intelligence", intelligenceBundle.Id);
        Assert.Equal(6, intelligenceBundle.Plugins.Count);
        Assert.Contains(intelligenceBundle.Plugins, p => p.Id == "frypdf.tool.aisummarizer");
        Assert.Contains(intelligenceBundle.Plugins, p => p.Id == "frypdf.tool.ocr");

        var dataStudioBundle = new DataStudioBundle();
        Assert.Equal("FryPdf.Bundle.DataStudio", dataStudioBundle.Id);
        Assert.Equal(2, dataStudioBundle.Plugins.Count);

        // Verify mounting into PluginHost with dummy operations service
        var ctx = new FryPluginContext();
        ctx.RegisterService<IPdfDocumentOperationsService>(new DummyOperationsService());
        var host = new PluginHost(ctx);

        foreach (var plugin in organizeBundle.Plugins)
        {
            host.RegisterPlugin(plugin);
        }

        await host.StartAsync();
        Assert.Equal(6, host.LoadedPlugins.Count);

        await host.StopAsync();
        Assert.Empty(host.LoadedPlugins);
    }

    [Fact]
    public async Task ProfileLoader_ApplyToHostAsync_RespectsFilters()
    {
        var profile = new PluginProfile
        {
            Name = "Filtered Profile",
            EnabledPlugins = new() { "frypdf.tool.merge", "frypdf.tool.compress" },
            DisabledPlugins = new() { "frypdf.tool.split" }
        };

        var allAvailablePlugins = new IFryPlugin[]
        {
            new ToolsOrganizeBundle().Plugins[0], // merge
            new ToolsOrganizeBundle().Plugins[1], // split
            new ToolsSecurityBundle().Plugins[0], // compress
            new ToolsSecurityBundle().Plugins[1]  // repair
        };

        var ctx = new FryPluginContext();
        ctx.RegisterService<IPdfDocumentOperationsService>(new DummyOperationsService());
        var host = new PluginHost(ctx);

        await ProfileLoader.ApplyToHostAsync(host, profile, allAvailablePlugins);

        // Only merge and compress should be active
        Assert.Equal(2, host.LoadedPlugins.Count);
        Assert.Contains(host.LoadedPlugins, p => p.Id == "frypdf.tool.merge");
        Assert.Contains(host.LoadedPlugins, p => p.Id == "frypdf.tool.compress");
        Assert.DoesNotContain(host.LoadedPlugins, p => p.Id == "frypdf.tool.split");
        Assert.DoesNotContain(host.LoadedPlugins, p => p.Id == "frypdf.tool.repair");

        await host.StopAsync();
    }
}
