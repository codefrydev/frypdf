using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PdfEditorApp.Core.Plugins.Pipelines;

/// <summary>
/// Thread-safe implementation of <see cref="IPipelineManager"/>.
/// Supports Waterfall (around-middleware), Bail (first non-null return), Parallel, and Serial dispatch modes.
/// </summary>
public class PipelineManager : IPipelineManager
{
    private readonly ConcurrentDictionary<string, List<object>> _pipelines = new();
    private readonly object _syncLock = new();

    public IDisposable RegisterWaterfall<TContext>(string pipelineName, Func<TContext, Func<Task>, Task> middleware)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        ArgumentNullException.ThrowIfNull(middleware);

        var key = $"waterfall:{pipelineName}:{typeof(TContext).FullName}";
        return RegisterHandler(key, middleware);
    }

    public async Task ExecuteWaterfallAsync<TContext>(string pipelineName, TContext context, Func<Task>? terminal = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);

        var key = $"waterfall:{pipelineName}:{typeof(TContext).FullName}";
        List<Func<TContext, Func<Task>, Task>>? middlewares = null;

        lock (_syncLock)
        {
            if (!_pipelines.TryGetValue(key, out var list) || list.Count == 0)
            {
                middlewares = null;
            }
            else
            {
                middlewares = list.Cast<Func<TContext, Func<Task>, Task>>().ToList();
            }
        }

        if (middlewares == null || middlewares.Count == 0)
        {
            if (terminal != null)
            {
                await terminal();
            }
            return;
        }

        // Build the pipeline chain: last middleware calls terminal, preceding middlewares call next
        Func<Task> pipeline = terminal ?? (() => Task.CompletedTask);

        for (int i = middlewares.Count - 1; i >= 0; i--)
        {
            var currentMiddleware = middlewares[i];
            var nextDelegate = pipeline;
            pipeline = () => currentMiddleware(context, nextDelegate);
        }

        await pipeline();
    }

    public IDisposable RegisterBail<TContext, TResult>(string pipelineName, Func<TContext, Task<TResult?>> handler) where TResult : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        ArgumentNullException.ThrowIfNull(handler);

        var key = $"bail:{pipelineName}:{typeof(TContext).FullName}:{typeof(TResult).FullName}";
        return RegisterHandler(key, handler);
    }

    public async Task<TResult?> ExecuteBailAsync<TContext, TResult>(string pipelineName, TContext context) where TResult : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);

        var key = $"bail:{pipelineName}:{typeof(TContext).FullName}:{typeof(TResult).FullName}";
        List<Func<TContext, Task<TResult?>>> handlers;

        lock (_syncLock)
        {
            if (!_pipelines.TryGetValue(key, out var list) || list.Count == 0)
            {
                return default;
            }

            handlers = list.Cast<Func<TContext, Task<TResult?>>>().ToList();
        }

        foreach (var handler in handlers)
        {
            var result = await handler(context);
            if (result is not null)
            {
                return result;
            }
        }

        return default;
    }

    public IDisposable RegisterParallel<TContext>(string pipelineName, Func<TContext, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        ArgumentNullException.ThrowIfNull(handler);

        var key = $"parallel:{pipelineName}:{typeof(TContext).FullName}";
        return RegisterHandler(key, handler);
    }

    public async Task ExecuteParallelAsync<TContext>(string pipelineName, TContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);

        var key = $"parallel:{pipelineName}:{typeof(TContext).FullName}";
        List<Func<TContext, Task>> handlers;

        lock (_syncLock)
        {
            if (!_pipelines.TryGetValue(key, out var list) || list.Count == 0)
            {
                return;
            }

            handlers = list.Cast<Func<TContext, Task>>().ToList();
        }

        var tasks = handlers.Select(h => h(context)).ToArray();
        await Task.WhenAll(tasks);
    }

    public IDisposable RegisterSerial<TContext>(string pipelineName, Func<TContext, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        ArgumentNullException.ThrowIfNull(handler);

        var key = $"serial:{pipelineName}:{typeof(TContext).FullName}";
        return RegisterHandler(key, handler);
    }

    public async Task ExecuteSerialAsync<TContext>(string pipelineName, TContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);

        var key = $"serial:{pipelineName}:{typeof(TContext).FullName}";
        List<Func<TContext, Task>> handlers;

        lock (_syncLock)
        {
            if (!_pipelines.TryGetValue(key, out var list) || list.Count == 0)
            {
                return;
            }

            handlers = list.Cast<Func<TContext, Task>>().ToList();
        }

        foreach (var handler in handlers)
        {
            await handler(context);
        }
    }

    private IDisposable RegisterHandler(string key, object handler)
    {
        lock (_syncLock)
        {
            var list = _pipelines.GetOrAdd(key, _ => new List<object>());
            list.Add(handler);
        }

        return new DisposableAction(() =>
        {
            lock (_syncLock)
            {
                if (_pipelines.TryGetValue(key, out var list))
                {
                    list.Remove(handler);
                    if (list.Count == 0)
                    {
                        _pipelines.TryRemove(key, out _);
                    }
                }
            }
        });
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
