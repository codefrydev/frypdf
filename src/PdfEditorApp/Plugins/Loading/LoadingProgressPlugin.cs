using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Loading;

namespace PdfEditorApp.Plugins.Loading;

/// <summary>
/// Core plugin contributing the application-wide full-screen loading progress service.
/// </summary>
public class LoadingProgressPlugin : IFryPlugin
{
    public string Id => "frypdf.shell.loadingprogress";
    public string Name => "Universal Full-Screen Loading Progress Service";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    private readonly ILoadingProgressService _service;

    public LoadingProgressPlugin(ILoadingProgressService? service = null)
    {
        _service = service ?? new LoadingProgressService();
    }

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        var instance = _service;
        if (instance == null && ctx.TryGetService<ILoadingProgressService>(out var resolved))
        {
            instance = resolved;
        }
        instance ??= new LoadingProgressService();

        ctx.RegisterLoadingProgressService(instance);

        ctx.RegisterEffect(() =>
        {
            instance.Hide();
        });

        return Task.CompletedTask;
    }
}
