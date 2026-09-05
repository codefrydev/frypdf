using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Services.AI.Providers;

namespace PdfEditorApp.Services.AI;

public class AiProviderRegistry : IAiProviderRegistry
{
    private readonly ConcurrentDictionary<string, IAiProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public AiProviderRegistry()
    {
        RegisterBuiltInProviders();
    }

    private void RegisterBuiltInProviders()
    {
        RegisterProvider(new OllamaAiProvider());
        RegisterProvider(new GroqAiProvider());
        RegisterProvider(new OpenAiProvider());
    }

    public IDisposable RegisterProvider(IAiProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _providers[provider.ProviderId] = provider;
        RegistryChanged?.Invoke();

        return new DisposableAction(() =>
        {
            _providers.TryRemove(provider.ProviderId, out _);
            RegistryChanged?.Invoke();
        });
    }

    public IAiProvider? GetProvider(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId)) return null;
        return _providers.GetValueOrDefault(providerId);
    }

    public IReadOnlyList<IAiProvider> GetAllProviders()
    {
        return _providers.Values.ToList();
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        private Action? _action = action;
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }
}
