using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing status bar widgets across the footer of the desktop window.
/// </summary>
public class StatusBarBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.StatusBar";
    public string Name => "Status Bar Widgets Bundle";
    public string Description => "Status bar footer indicators: document page statistics, system operational status, and graphics engine memory monitor.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new PageStatsWidgetPlugin(),
        new SystemStatusWidgetPlugin(),
        new MemoryMonitorWidgetPlugin()
    };
}

public class PageStatsWidgetPlugin : IFryPlugin
{
    public string Id => "frypdf.status.pagestats";
    public string Name => "Document Page Stats Widget";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterStatusBarWidget(new StatusBarWidgetDescriptor
        {
            WidgetId = "frypdf.status.pagestats",
            Alignment = StatusBarAlignment.Left,
            Order = 10,
            ToolTip = "Current Page Statistics",
            Factory = sp => new ViewModels.StatusBarWidgetViewModel
            {
                WidgetId = "frypdf.status.pagestats",
                Label = "Pages",
                IconKind = "FileDocumentOutline",
                ToolTip = "Active Document Page Statistics",
                IsActive = true
            }
        });

        return Task.CompletedTask;
    }
}

public class SystemStatusWidgetPlugin : IFryPlugin
{
    public string Id => "frypdf.status.ready";
    public string Name => "System Operational Status Widget";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterStatusBarWidget(new StatusBarWidgetDescriptor
        {
            WidgetId = "frypdf.status.ready",
            Alignment = StatusBarAlignment.Left,
            Order = 20,
            ToolTip = "System Operational & Idle",
            Factory = sp => new ViewModels.StatusBarWidgetViewModel
            {
                WidgetId = "frypdf.status.ready",
                Label = "Ready",
                IconKind = "CheckCircleOutline",
                ToolTip = "System Operational & Idle",
                IsActive = true
            }
        });

        return Task.CompletedTask;
    }
}

public class MemoryMonitorWidgetPlugin : IFryPlugin
{
    public string Id => "frypdf.status.memory";
    public string Name => "Skia Engine Memory Widget";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterStatusBarWidget(new StatusBarWidgetDescriptor
        {
            WidgetId = "frypdf.status.memory",
            Alignment = StatusBarAlignment.Right,
            Order = 90,
            ToolTip = "SkiaSharp 64-Bit Graphics Engine",
            Factory = sp => new ViewModels.StatusBarWidgetViewModel
            {
                WidgetId = "frypdf.status.memory",
                Label = "Skia 64-bit",
                IconKind = "Memory",
                ToolTip = "SkiaSharp 64-Bit Graphics Engine",
                IsActive = true
            }
        });

        return Task.CompletedTask;
    }
}
