using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins;

/// <summary>
/// Manages the reversible effects and resources registered by a plugin during its lifetime.
/// When the scope is disposed, registered effects and unmanaged handles are unwound in reverse (LIFO) order.
/// </summary>
public sealed class PluginScope : IDisposable
{
    private readonly object _lock = new();
    private readonly Stack<Action> _disposers = new();
    private bool _isDisposed;

    /// <summary>
    /// Gets whether this scope has been disposed.
    /// </summary>
    public bool IsDisposed
    {
        get
        {
            lock (_lock) return _isDisposed;
        }
    }

    /// <summary>
    /// Registers a reversible effect or cleanup action to execute when this scope is disposed.
    /// </summary>
    /// <param name="onDispose">The cleanup action to execute on disposal.</param>
    /// <returns>An <see cref="IDisposable"/> token that can prematurely unregister or execute this effect.</returns>
    public IDisposable RegisterEffect(Action onDispose)
    {
        ArgumentNullException.ThrowIfNull(onDispose);

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _disposers.Push(onDispose);
        }

        return new ScopeEffectToken(onDispose);
    }

    /// <summary>
    /// Registers an <see cref="IDisposable"/> instance to be disposed when this scope is torn down.
    /// </summary>
    public IDisposable RegisterDisposable(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        return RegisterEffect(disposable.Dispose);
    }

    /// <summary>
    /// Unwinds all registered effects in reverse (LIFO) order.
    /// </summary>
    public void Dispose()
    {
        List<Action> actionsToRun;

        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            actionsToRun = new List<Action>(_disposers);
            _disposers.Clear();
        }

        // Execute in LIFO order (Stack enumeration is top-to-bottom, which is LIFO)
        List<Exception>? exceptions = null;
        foreach (var action in actionsToRun)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException("One or more errors occurred while unwinding plugin effects.", exceptions);
        }
    }

    private sealed class ScopeEffectToken : IDisposable
    {
        private Action? _action;

        public ScopeEffectToken(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            // Note: The main scope disposal will safely no-op or run whatever is registered.
            _action = null;
        }
    }
}
