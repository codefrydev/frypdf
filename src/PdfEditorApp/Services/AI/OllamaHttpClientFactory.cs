using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PdfEditorApp.Services.AI;

/// <summary>
/// Supplies pooled <see cref="HttpClient"/> instances for authenticated Ollama endpoints.
/// </summary>
/// <remarks>
/// Both <see cref="AiService"/> and <c>OllamaAiProvider</c> previously constructed a fresh
/// <see cref="HttpClient"/> on every request and never disposed it. Each one opens its own
/// connection pool, and the sockets linger in TIME_WAIT — a long chat session exhausts
/// ephemeral ports. Clients are cached per (endpoint, key) and share one handler.
/// </remarks>
internal static class OllamaHttpClientFactory
{
    private static readonly SocketsHttpHandler SharedHandler = new()
    {
        // Recycle pooled connections so DNS changes are eventually picked up.
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };

    private static readonly ConcurrentDictionary<(string Endpoint, string ApiKey), HttpClient> Clients = new();

    /// <summary>
    /// Returns a cached client bound to <paramref name="endpoint"/> and authenticated with
    /// <paramref name="apiKey"/>.
    /// </summary>
    public static HttpClient GetOrCreate(string endpoint, string apiKey)
    {
        return Clients.GetOrAdd((endpoint, apiKey), static key =>
        {
            // disposeHandler: false — the handler is shared and must outlive any one client.
            var client = new HttpClient(SharedHandler, disposeHandler: false)
            {
                BaseAddress = new Uri(key.Endpoint),
                Timeout = TimeSpan.FromSeconds(60)
            };

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", key.ApiKey);

            return client;
        });
    }
}
