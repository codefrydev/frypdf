using System;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Descriptor representing a non-modal floating overlay panel contributed by a plugin (targeting 'shell.overlay').
/// Inspired by the DeepSeek Harness client-side dynamic overlay slot architecture.
/// </summary>
public sealed class OverlayDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Slot { get; init; } = "shell.overlay";
    public double DefaultWidth { get; init; } = 320;
    public double DefaultHeight { get; init; } = 420;
    public double? InitialX { get; init; }
    public double? InitialY { get; init; }
    public bool IsDraggable { get; init; } = true;
    public bool IsMinimizable { get; init; } = true;
    public bool IsClosable { get; init; } = true;
    public string IconKind { get; init; } = "WindowRestore";
    public Type? ViewType { get; init; }
    public Type? ViewModelType { get; init; }
    public Func<IServiceProvider, object>? ViewFactory { get; init; }
    public Func<IServiceProvider, object>? ViewModelFactory { get; init; }
}
