using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Descriptors;

/// <summary>
/// Descriptor for a command or action searchable in the Command Palette (⌘K / Ctrl+K),
/// contributed dynamically by plugins.
/// </summary>
public class CommandPaletteDescriptor
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string Category { get; init; } = "General";
    public string IconKind { get; init; } = "LightningBolt";
    public string? Shortcut { get; init; }
    public int Order { get; init; } = 100;
    public Action<IServiceProvider>? Action { get; init; }
}

/// <summary>
/// Registry for discovering and dispatching Command Palette commands across plugins.
/// </summary>
public interface ICommandPaletteRegistry
{
    /// <summary>
    /// Registers a command into the palette.
    /// </summary>
    IDisposable RegisterCommand(CommandPaletteDescriptor descriptor);

    /// <summary>
    /// Unregisters a command by its unique ID.
    /// </summary>
    bool UnregisterCommand(string commandId);

    /// <summary>
    /// Gets all registered command palette descriptors.
    /// </summary>
    IReadOnlyList<CommandPaletteDescriptor> GetAllCommands();

    /// <summary>
    /// Triggered when commands are registered or unregistered.
    /// </summary>
    event Action? RegistryChanged;
}
