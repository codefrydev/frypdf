using System;
using System.Threading.Tasks;

namespace PdfEditorApp.Core.Plugins.Pipelines;

/// <summary>
/// Orchestrates typed execution pipelines inspired by DeepSeek Harness / Cordis.
/// Supports 4 primary dispatch modes: Waterfall (around-middleware), Bail (first handled result),
/// Parallel (concurrent execution), and Serial (ordered execution).
/// </summary>
public interface IPipelineManager
{
    /// <summary>
    /// Registers a middleware component into a named Waterfall pipeline.
    /// Middleware components wrap subsequent handlers using the provided <c>next</c> delegate.
    /// </summary>
    IDisposable RegisterWaterfall<TContext>(string pipelineName, Func<TContext, Func<Task>, Task> middleware);

    /// <summary>
    /// Executes a named Waterfall pipeline with the given context and optional terminal delegate.
    /// </summary>
    Task ExecuteWaterfallAsync<TContext>(string pipelineName, TContext context, Func<Task>? terminal = null);

    /// <summary>
    /// Registers a handler into a named Bail pipeline.
    /// The pipeline will evaluate handlers until the first handler returns a non-null result.
    /// </summary>
    IDisposable RegisterBail<TContext, TResult>(string pipelineName, Func<TContext, Task<TResult?>> handler) where TResult : class;

    /// <summary>
    /// Executes a named Bail pipeline, returning the result of the first handler that produces a non-null value.
    /// </summary>
    Task<TResult?> ExecuteBailAsync<TContext, TResult>(string pipelineName, TContext context) where TResult : class;

    /// <summary>
    /// Registers a handler into a named Parallel pipeline.
    /// All handlers will be executed concurrently using <see cref="Task.WhenAll(Task[])"/>.
    /// </summary>
    IDisposable RegisterParallel<TContext>(string pipelineName, Func<TContext, Task> handler);

    /// <summary>
    /// Executes all handlers registered in a Parallel pipeline concurrently.
    /// </summary>
    Task ExecuteParallelAsync<TContext>(string pipelineName, TContext context);

    /// <summary>
    /// Registers a handler into a named Serial pipeline.
    /// Handlers will be awaited sequentially in order of registration.
    /// </summary>
    IDisposable RegisterSerial<TContext>(string pipelineName, Func<TContext, Task> handler);

    /// <summary>
    /// Executes all handlers registered in a Serial pipeline in sequential order.
    /// </summary>
    Task ExecuteSerialAsync<TContext>(string pipelineName, TContext context);
}
