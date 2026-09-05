using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Palette;

/// <summary>
/// Thread-safe in-memory registry for discovering, searching, and dispatching Command Palette commands.
/// </summary>
public class CommandPaletteRegistry : ICommandPaletteRegistry
{
    private readonly ConcurrentDictionary<string, CommandPaletteDescriptor> _commands = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public IDisposable RegisterCommand(CommandPaletteDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.Id);

        _commands[descriptor.Id] = descriptor;
        RegistryChanged?.Invoke();

        return new DisposableAction(() =>
        {
            _commands.TryRemove(descriptor.Id, out _);
            RegistryChanged?.Invoke();
        });
    }

    public bool UnregisterCommand(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId)) return false;
        var removed = _commands.TryRemove(commandId, out _);
        if (removed)
        {
            RegistryChanged?.Invoke();
        }
        return removed;
    }

    public IReadOnlyList<CommandPaletteDescriptor> GetAllCommands()
    {
        return _commands.Values.OrderBy(c => c.Order).ThenBy(c => c.Title).ToList();
    }

    private sealed class DisposableAction : IDisposable
    {
        private Action? _action;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            var act = System.Threading.Interlocked.Exchange(ref _action, null);
            act?.Invoke();
        }
    }
}
