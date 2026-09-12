using System;

namespace PdfEditorApp.Core.Plugins.Loading;

/// <summary>
/// Extension methods for registering and resolving <see cref="ILoadingProgressService"/> from <see cref="IFryPluginContext"/>.
/// </summary>
public static class LoadingPluginContextExtensions
{
    public static void RegisterLoadingProgressService(this IFryPluginContext ctx, ILoadingProgressService service)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(service);

        ctx.RegisterService<ILoadingProgressService>(service);
        ctx.RegisterEffect(() =>
        {
            // Clean unregister effect
        });
    }

    public static bool TryGetLoadingProgressService(this IFryPluginContext ctx, out ILoadingProgressService? service)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return ctx.TryGetService<ILoadingProgressService>(out service);
    }

    public static ILoadingProgressService GetLoadingProgressService(this IFryPluginContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return ctx.GetService<ILoadingProgressService>();
    }
}
