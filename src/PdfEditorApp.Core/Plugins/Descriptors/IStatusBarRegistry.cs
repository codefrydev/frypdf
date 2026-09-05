using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

public enum StatusBarAlignment
{
    Left,
    Right
}

/// <summary>
/// Descriptor for a status bar widget contributed by a plugin.
/// </summary>
public class StatusBarWidgetDescriptor
{
    public string WidgetId { get; init; } = "";
    public string ToolTip { get; init; } = "";
    public StatusBarAlignment Alignment { get; init; } = StatusBarAlignment.Left;
    public int Order { get; init; } = 100;
    public Func<IServiceProvider, object> Factory { get; init; } = _ => new object();
}

/// <summary>
/// Registry for discovering and dispatching status bar widgets.
/// </summary>
public interface IStatusBarRegistry
{
    IDisposable RegisterWidget(StatusBarWidgetDescriptor widget);
    IReadOnlyList<StatusBarWidgetDescriptor> GetWidgets(StatusBarAlignment alignment);
    IReadOnlyList<StatusBarWidgetDescriptor> GetAllWidgets();
    event Action? RegistryChanged;
}
