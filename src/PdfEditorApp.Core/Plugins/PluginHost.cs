using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PdfEditorApp.Core.Plugins;

/// <summary>
/// Lifecycle state of a registered FryPDF plugin.
/// </summary>
public enum PluginState
{
    Unloaded,
    Registered,
    Resolving,
    Active,
    Suspended,
    Faulted
}

/// <summary>
/// Internal tracking descriptor for a plugin and its activation scope.
/// </summary>
public class PluginEntry
{
    public IFryPlugin Plugin { get; }
    public PluginState State { get; set; } = PluginState.Registered;
    public PluginScope? Scope { get; set; }
    public Exception? LastError { get; set; }

    public PluginEntry(IFryPlugin plugin)
    {
        Plugin = plugin;
    }
}

/// <summary>
/// Orchestrates plugin registration, dependency-driven topological mounting, reactive lifecycle states,
/// cascading dependency activation/deactivation, and scoped LIFO teardown.
/// </summary>
public class PluginHost : IAsyncDisposable, IDisposable
{
    private readonly object _lock = new();
    private readonly List<IFryPlugin> _registeredPlugins = new();
    private readonly Dictionary<string, PluginEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(IFryPlugin Plugin, PluginScope Scope)> _activePlugins = new();
    private readonly FryPluginContext _context;
    private bool _isRunning;

    /// <summary>
    /// Event fired when an individual plugin transitions lifecycle states (e.g. Active, Suspended, Faulted).
    /// </summary>
    public event Action<string, PluginState>? PluginStateChanged;

    /// <summary>
    /// The root plugin context.
    /// </summary>
    public IFryPluginContext Context => _context;

    /// <summary>
    /// Gets all currently active plugins in order of activation.
    /// </summary>
    public IReadOnlyList<IFryPlugin> LoadedPlugins
    {
        get
        {
            lock (_lock)
            {
                return _activePlugins.Select(p => p.Plugin).ToList();
            }
        }
    }

    /// <summary>
    /// Gets all registered plugins, regardless of state.
    /// </summary>
    public IReadOnlyList<IFryPlugin> RegisteredPlugins
    {
        get
        {
            lock (_lock)
            {
                return _registeredPlugins.ToList();
            }
        }
    }

    public PluginHost(FryPluginContext? context = null)
    {
        _context = context ?? new FryPluginContext();
    }

    /// <summary>
    /// Gets the current lifecycle state of a plugin.
    /// </summary>
    public PluginState GetPluginState(string pluginId)
    {
        lock (_lock)
        {
            return _entries.TryGetValue(pluginId, out var entry) ? entry.State : PluginState.Unloaded;
        }
    }

    /// <summary>
    /// Checks whether a plugin is currently in the Active state.
    /// </summary>
    public bool IsPluginActive(string pluginId) => GetPluginState(pluginId) == PluginState.Active;

    /// <summary>
    /// Registers a plugin for mounting.
    /// </summary>
    public void RegisterPlugin(IFryPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        lock (_lock)
        {
            if (_entries.ContainsKey(plugin.Id))
            {
                throw new InvalidOperationException($"A plugin with ID '{plugin.Id}' has already been registered.");
            }
            _registeredPlugins.Add(plugin);
            _entries[plugin.Id] = new PluginEntry(plugin);
        }
    }

    /// <summary>
    /// Registers multiple plugins for mounting.
    /// </summary>
    public void RegisterPlugins(IEnumerable<IFryPlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        foreach (var plugin in plugins)
        {
            RegisterPlugin(plugin);
        }
    }

    /// <summary>
    /// Mounts all registered plugins in dependency-resolved topological order.
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        List<IFryPlugin> pending;
        lock (_lock)
        {
            if (_isRunning) return;
            pending = new List<IFryPlugin>(_registeredPlugins.Where(p => _entries[p.Id].State != PluginState.Active));
        }

        while (pending.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var activatable = pending
                .Where(p => p.RequiredServices.All(reqType => _context.HasService(reqType)))
                .ToList();

            if (activatable.Count == 0)
            {
                var allProvided = new HashSet<Type>(pending.SelectMany(p => p.ProvidedServices));

                foreach (var p in pending)
                {
                    var missing = p.RequiredServices.FirstOrDefault(r => !_context.HasService(r) && !allProvided.Contains(r));
                    if (missing != null)
                    {
                        throw new PluginMissingDependencyException(p.Id, missing);
                    }
                }

                throw new PluginCircularDependencyException(
                    $"Circular dependency detected among plugins: {string.Join(", ", pending.Select(p => p.Id))}");
            }

            foreach (var plugin in activatable)
            {
                ct.ThrowIfCancellationRequested();
                await MountPluginCoreAsync(plugin, ct);
                pending.Remove(plugin);
            }
        }

        lock (_lock)
        {
            _isRunning = true;
        }
    }

    /// <summary>
    /// Dynamically enables an individual plugin at runtime, mounting its capabilities and
    /// reactively cascading activation to any downstream plugins waiting on its provided services.
    /// </summary>
    public async Task EnablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        PluginEntry entry;
        lock (_lock)
        {
            if (!_entries.TryGetValue(pluginId, out var e))
            {
                throw new KeyNotFoundException($"Plugin with ID '{pluginId}' is not registered.");
            }
            if (e.State == PluginState.Active) return;
            entry = e;
        }

        // Validate dependencies
        var missing = entry.Plugin.RequiredServices.FirstOrDefault(req => !_context.HasService(req));
        if (missing != null)
        {
            throw new PluginMissingDependencyException(entry.Plugin.Id, missing);
        }

        await MountPluginCoreAsync(entry.Plugin, ct);

        // Cascading auto-mount for any suspended or registered plugins that can now activate
        await AutoMountSatisfiedPluginsAsync(ct);
    }

    /// <summary>
    /// Dynamically disables an individual plugin at runtime, cleanly tearing down its effects
    /// and cascading suspension to any dependent plugins that require services provided by this plugin.
    /// </summary>
    public async Task DisablePluginAsync(string pluginId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        PluginEntry entry;
        lock (_lock)
        {
            if (!_entries.TryGetValue(pluginId, out var e) || e.State != PluginState.Active)
            {
                return;
            }
            entry = e;
        }

        // Unmount the target plugin first
        await UnmountPluginCoreAsync(entry, PluginState.Suspended);

        // Cascading deactivation: iteratively suspend any active plugins whose RequiredServices are no longer satisfied in Context!
        bool progress = true;
        while (progress)
        {
            progress = false;
            PluginEntry? nextToSuspend = null;

            lock (_lock)
            {
                nextToSuspend = _activePlugins
                    .Select(p => _entries[p.Plugin.Id])
                    .FirstOrDefault(p => p.Plugin.RequiredServices.Any(req => !_context.HasService(req)));
            }

            if (nextToSuspend != null)
            {
                await UnmountPluginCoreAsync(nextToSuspend, PluginState.Suspended);
                progress = true;
            }
        }
    }

    /// <summary>
    /// Reloads a plugin by disabling it and re-enabling it.
    /// </summary>
    public async Task ReloadPluginAsync(string pluginId, CancellationToken ct = default)
    {
        await DisablePluginAsync(pluginId, ct);
        await EnablePluginAsync(pluginId, ct);
    }

    private async Task MountPluginCoreAsync(IFryPlugin plugin, CancellationToken ct)
    {
        PluginScope scope = new();
        var scopedContext = _context.CreateScopedContext(scope);

        lock (_lock)
        {
            if (_entries.TryGetValue(plugin.Id, out var entry))
            {
                entry.State = PluginState.Resolving;
                entry.Scope = scope;
                entry.LastError = null;
            }
        }

        try
        {
            await plugin.ApplyAsync(scopedContext, ct);

            lock (_lock)
            {
                _activePlugins.Add((plugin, scope));
                if (_entries.TryGetValue(plugin.Id, out var entry))
                {
                    entry.State = PluginState.Active;
                }
            }

            PluginStateChanged?.Invoke(plugin.Id, PluginState.Active);
        }
        catch (Exception ex)
        {
            scope.Dispose();

            lock (_lock)
            {
                if (_entries.TryGetValue(plugin.Id, out var entry))
                {
                    entry.State = PluginState.Faulted;
                    entry.Scope = null;
                    entry.LastError = ex;
                }
            }

            PluginStateChanged?.Invoke(plugin.Id, PluginState.Faulted);
            throw;
        }
    }

    private Task UnmountPluginCoreAsync(PluginEntry entry, PluginState nextState)
    {
        PluginScope? scopeToDispose = null;

        lock (_lock)
        {
            var idx = _activePlugins.FindIndex(p => string.Equals(p.Plugin.Id, entry.Plugin.Id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                scopeToDispose = _activePlugins[idx].Scope;
                _activePlugins.RemoveAt(idx);
            }

            entry.State = nextState;
            entry.Scope = null;
        }

        if (scopeToDispose != null)
        {
            try
            {
                scopeToDispose.Dispose();
            }
            catch (Exception ex)
            {
                entry.LastError = ex;
            }
        }

        PluginStateChanged?.Invoke(entry.Plugin.Id, nextState);
        return Task.CompletedTask;
    }

    private async Task AutoMountSatisfiedPluginsAsync(CancellationToken ct)
    {
        bool progress = true;
        while (progress)
        {
            progress = false;
            List<PluginEntry> candidates;

            lock (_lock)
            {
                candidates = _entries.Values
                    .Where(e => e.State is PluginState.Suspended or PluginState.Registered)
                    .Where(e => e.Plugin.RequiredServices.All(req => _context.HasService(req)))
                    .ToList();
            }

            foreach (var candidate in candidates)
            {
                ct.ThrowIfCancellationRequested();
                await MountPluginCoreAsync(candidate.Plugin, ct);
                progress = true;
            }
        }
    }

    /// <summary>
    /// Unmounts all active plugins in reverse (LIFO) order and disposes their scopes.
    /// </summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        List<(IFryPlugin Plugin, PluginScope Scope)> toUnload;
        lock (_lock)
        {
            if (!_isRunning && _activePlugins.Count == 0) return;
            _isRunning = false;

            toUnload = new List<(IFryPlugin Plugin, PluginScope Scope)>(_activePlugins);
            _activePlugins.Clear();

            foreach (var entry in _entries.Values)
            {
                entry.State = PluginState.Unloaded;
                entry.Scope = null;
            }
        }

        toUnload.Reverse();

        List<Exception>? exceptions = null;
        foreach (var (plugin, scope) in toUnload)
        {
            try
            {
                scope.Dispose();
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(new PluginException($"Error while unloading plugin '{plugin.Id}': {ex.Message}", ex));
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException("One or more errors occurred while stopping plugins.", exceptions);
        }

        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
