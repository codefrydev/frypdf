using System;
using System.Collections.Generic;
using System.Threading;

namespace PdfEditorApp.Core.Plugins;

/// <summary>
/// Manages the reversible effects and resources registered by a plugin during its lifetime.
/// When the scope is disposed, registered effects and unmanaged handles are unwound in reverse (LIFO) order.
/// </summary>
public sealed class PluginScope : IDisposable
{
    private readonly object _lock = new();

    // A List (unwound in reverse) rather than a Stack, so that a token returned by
    // RegisterEffect can remove its own effect without disturbing the others' LIFO order.
    private readonly List<Action> _disposers = new();
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
            _disposers.Add(onDispose);
        }

        return new ScopeEffectToken(this, onDispose);
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
            actionsToRun.Reverse();
            _disposers.Clear();
        }

        // Execute in LIFO order (most recently registered effect unwinds first)
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

    /// <summary>
    /// Removes <paramref name="onDispose"/> from this scope without running it.
    /// </summary>
    /// <returns>True when the effect was still registered.</returns>
    private bool TryRemoveEffect(Action onDispose)
    {
        lock (_lock)
        {
            if (_isDisposed) return false;
            return _disposers.Remove(onDispose);
        }
    }

    /// <summary>
    /// Handle returned by <see cref="RegisterEffect"/>. Disposing it unwinds that one effect
    /// immediately and unregisters it from the scope.
    /// </summary>
    /// <remarks>
    /// This used to only null out its own field, so it neither removed the action from the
    /// scope nor ran it — every scoped RegisterTool / RegisterCommand / RegisterOverlay handle
    /// was silently inert, and the registration survived until the whole plugin unmounted.
    /// </remarks>
    private sealed class ScopeEffectToken : IDisposable
    {
        private readonly PluginScope _scope;
        private Action? _action;

        public ScopeEffectToken(PluginScope scope, Action action)
        {
            _scope = scope;
            _action = action;
        }

        public void Dispose()
        {
            var action = Interlocked.Exchange(ref _action, null);
            if (action == null) return;

            // Only run it if it was still registered; if the scope already unwound it,
            // running it again could double-unregister.
            if (_scope.TryRemoveEffect(action))
            {
                action();
            }
        }
    }
}
