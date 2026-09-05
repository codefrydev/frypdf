using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Services;
using PdfEditorApp.Services.Tools.Core;

namespace PdfEditorApp.Plugins;

/// <summary>
/// Convenient base class for standard FryPDF tool plugins.
/// Automatically registers its <see cref="PdfToolDescriptor"/> into the active context during <see cref="ApplyAsync"/>.
/// </summary>
public abstract class ToolPluginBase : IFryPlugin
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public virtual Version Version => new(1, 0, 0);
    public virtual IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();
    public virtual IReadOnlyList<Type> ProvidedServices => Array.Empty<Type>();

    protected abstract PdfToolDescriptor CreateDescriptor();

    public virtual Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterTool(CreateDescriptor());
        return Task.CompletedTask;
    }
}
