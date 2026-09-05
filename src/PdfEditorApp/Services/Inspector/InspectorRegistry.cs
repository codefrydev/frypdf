using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Inspector;

public class InspectorRegistry : IInspectorRegistry
{
    private readonly ConcurrentDictionary<string, InspectorSectionDescriptor> _sections = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public InspectorRegistry(bool seedDefaults = false)
    {
        if (seedDefaults)
        {
            RegisterBuiltInSections();
        }
    }

    private void RegisterBuiltInSections()
    {
        // Core sections can be registered dynamically if needed
    }

    public IDisposable RegisterSection(InspectorSectionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _sections[descriptor.SectionId] = descriptor;
        RegistryChanged?.Invoke();

        return new DisposableAction(() =>
        {
            _sections.TryRemove(descriptor.SectionId, out _);
            RegistryChanged?.Invoke();
        });
    }

    public IReadOnlyList<InspectorSectionDescriptor> GetSectionsForTarget(object? target)
    {
        return _sections.Values
            .Where(s => s.AppliesTo(target))
            .OrderBy(s => s.Order)
            .ToList();
    }

    public IReadOnlyList<InspectorSectionDescriptor> GetAllSections()
    {
        return _sections.Values.OrderBy(s => s.Order).ToList();
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        private Action? _action = action;
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }
}
