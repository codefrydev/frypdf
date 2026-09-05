using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Dialogs;

/// <summary>
/// Thread-safe registry for modal dialogs and floating studio overlays.
/// </summary>
public sealed class DialogRegistry : IDialogRegistry
{
    private readonly ConcurrentDictionary<string, DialogDescriptor> _dialogs = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public IDisposable RegisterDialog(DialogDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _dialogs[descriptor.Id] = descriptor;
        RegistryChanged?.Invoke();

        return new UnregisterDisposable(() =>
        {
            UnregisterDialog(descriptor.Id);
        });
    }

    public bool UnregisterDialog(string dialogId)
    {
        if (_dialogs.TryRemove(dialogId, out _))
        {
            RegistryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public DialogDescriptor? GetDialog(string dialogId)
    {
        if (_dialogs.TryGetValue(dialogId, out var descriptor))
        {
            return descriptor;
        }

        if (!dialogId.StartsWith("frypdf.dialog.", StringComparison.OrdinalIgnoreCase))
        {
            if (_dialogs.TryGetValue($"frypdf.dialog.{dialogId}", out descriptor))
            {
                return descriptor;
            }
        }

        return null;
    }

    public IReadOnlyList<DialogDescriptor> GetAllDialogs()
    {
        return _dialogs.Values.OrderBy(d => d.Title).ToList();
    }

    private sealed class UnregisterDisposable : IDisposable
    {
        private Action? _action;

        public UnregisterDisposable(Action action)
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
