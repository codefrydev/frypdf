using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

public enum StatusBarAlignment
{
    Left,
    Right
}

/// <summary>
/// Defines the target scope or surface where a status bar widget is intended to be displayed.
/// </summary>
public enum StatusBarScope
{
    /// <summary>
    /// Core document editor status bar (e.g. graphics engine telemetry, active document metrics).
    /// </summary>
    DocumentEditor,

    /// <summary>
    /// General application shell or background extension widgets.
    /// Not displayed in the document canvas editor footer.
    /// </summary>
    Global
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

    /// <summary>
    /// The target surface/scope where this widget should be displayed.
    /// Defaults to <see cref="StatusBarScope.Global"/>.
    /// </summary>
    public StatusBarScope Scope { get; init; } = StatusBarScope.Global;

    /// <summary>
    /// True if the contributing plugin is an external (3rd-party/user-installed) plugin.
    /// </summary>
    public bool IsExternal { get; set; }
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
