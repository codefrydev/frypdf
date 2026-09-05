using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Services.AI;
using PdfEditorApp.Services.AI.Providers;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing all built-in AI model providers.
/// </summary>
public class AiProvidersBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.AiProviders";
    public string Name => "AI Model Providers Bundle";
    public string Description => "Pluggable AI providers: local Ollama, ultra-fast Groq cloud inference, and OpenAI compatible endpoints.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new OllamaAiPlugin(),
        new GroqAiPlugin(),
        new OpenAiPlugin()
    };
}

public class OllamaAiPlugin : IFryPlugin
{
    public string Id => "frypdf.ai.ollama";
    public string Name => "Ollama Local AI Provider";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => new[] { typeof(IAiProviderRegistry) };

    public IReadOnlyDictionary<string, Core.Plugins.Manifests.PluginSettingDefinition>? SettingsSchema => new Dictionary<string, Core.Plugins.Manifests.PluginSettingDefinition>
    {
        ["Endpoint"] = new()
        {
            Type = "string",
            Label = "Ollama Endpoint URL",
            Description = "HTTP endpoint for local Ollama daemon",
            DefaultValue = "http://localhost:11434"
        },
        ["Model"] = new()
        {
            Type = "string",
            Label = "Model Tag",
            Description = "Default local model tag (e.g. llama3, mistral, phi3)",
            DefaultValue = "llama3"
        }
    };

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        var reg = ctx.GetService<IAiProviderRegistry>();
        reg.RegisterProvider(new OllamaAiProvider());
        return Task.CompletedTask;
    }
}

public class GroqAiPlugin : IFryPlugin
{
    public string Id => "frypdf.ai.groq";
    public string Name => "Groq Cloud AI Provider";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => new[] { typeof(IAiProviderRegistry) };

    public IReadOnlyDictionary<string, Core.Plugins.Manifests.PluginSettingDefinition>? SettingsSchema => new Dictionary<string, Core.Plugins.Manifests.PluginSettingDefinition>
    {
        ["ApiKey"] = new()
        {
            Type = "secret",
            Label = "Groq API Key",
            Description = "API key from console.groq.com for ultra-fast Llama-3 / Mixtral inference",
            DefaultValue = ""
        },
        ["Model"] = new()
        {
            Type = "select",
            Label = "Model",
            Description = "Inference model name",
            DefaultValue = "llama-3.3-70b-versatile",
            Options = new List<string> { "llama-3.3-70b-versatile", "llama-3.1-8b-instant", "mixtral-8x7b-32768" }
        }
    };

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        var reg = ctx.GetService<IAiProviderRegistry>();
        reg.RegisterProvider(new GroqAiProvider());
        return Task.CompletedTask;
    }
}

public class OpenAiPlugin : IFryPlugin
{
    public string Id => "frypdf.ai.openai";
    public string Name => "OpenAI / Compatible Provider";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => new[] { typeof(IAiProviderRegistry) };

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        var reg = ctx.GetService<IAiProviderRegistry>();
        reg.RegisterProvider(new OpenAiProvider());
        return Task.CompletedTask;
    }
}
