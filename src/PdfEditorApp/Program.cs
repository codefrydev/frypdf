using Avalonia;
using Avalonia.Logging;
using System;
using PdfEditorApp.Plugins.Cli;

namespace PdfEditorApp;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        if (HeadlessCliRunner.IsCliInvocation(args))
        {
            return HeadlessCliRunner.RunCliAsync(args).GetAwaiter().GetResult();
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            // Areas are listed explicitly so Avalonia's Binding area is NOT routed into Trace.
            // AppLogService installs a TraceListener, so every failed binding became a log entry:
            // InspectorSidebarView has a section per element type and all but one bind against a
            // null view-model property at any moment, so a single rebuild emitted ~180 entries and
            // flushed the diagnostic ring buffer, evicting the events we actually need to see.
            .LogToTrace(LogEventLevel.Warning, LogArea.Control, LogArea.Layout, LogArea.Visual, LogArea.Platform);
}
