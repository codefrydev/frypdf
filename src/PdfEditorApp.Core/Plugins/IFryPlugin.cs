using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PdfEditorApp.Core.Plugins;

/// <summary>
/// Contract implemented by all FryPDF plugins.
/// Inspired by Cordis and DeepSeek Harness: every capability mounts into a shared context.
/// </summary>
public interface IFryPlugin
{
    /// <summary>
    /// Unique identifier for this plugin, e.g. "frypdf.tool.merge" or "frypdf.engine.questpdf".
    /// </summary>
    string Id { get; }

    /// <summary>
    /// User-friendly name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Service contracts required by this plugin before it can mount (declarative injection graph).
    /// </summary>
    IReadOnlyList<Type> RequiredServices { get; }

    /// <summary>
    /// Service contracts provided by this plugin (used for dependency graph cycle detection and validation).
    /// </summary>
    IReadOnlyList<Type> ProvidedServices => Array.Empty<Type>();

    /// <summary>
    /// Optional declarative configuration schema describing configurable user settings for this plugin.
    /// </summary>
    IReadOnlyDictionary<string, Manifests.PluginSettingDefinition>? SettingsSchema => null;

    /// <summary>
    /// Mounts the plugin into the active context, registering services, pipelines, tools, and effects.
    /// </summary>
    /// <param name="ctx">The active plugin context.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default);
}
