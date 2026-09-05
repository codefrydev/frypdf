using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Services.Ocr;

public class OcrEngineRegistry : IOcrEngineRegistry
{
    private readonly ConcurrentDictionary<string, IOcrEngine> _engines = new(StringComparer.OrdinalIgnoreCase);

    public event Action? RegistryChanged;

    public OcrEngineRegistry(ITesseractModelService? modelService = null)
    {
        RegisterBuiltInEngines(modelService);
    }

    private void RegisterBuiltInEngines(ITesseractModelService? modelService)
    {
        RegisterEngine(new AppleVisionOcrEngine());
        RegisterEngine(new WindowsMediaOcrEngine());
        RegisterEngine(new TesseractOcrEngine(modelService ?? new TesseractModelService()));
    }

    public IDisposable RegisterEngine(IOcrEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        _engines[engine.EngineName] = engine;
        RegistryChanged?.Invoke();

        return new DisposableAction(() =>
        {
            _engines.TryRemove(engine.EngineName, out _);
            RegistryChanged?.Invoke();
        });
    }

    public IOcrEngine? GetEngine(string engineName)
    {
        if (string.IsNullOrWhiteSpace(engineName)) return null;
        return _engines.GetValueOrDefault(engineName);
    }

    public IReadOnlyList<IOcrEngine> GetAllEngines()
    {
        return _engines.Values.ToList();
    }

    public IReadOnlyList<IOcrEngine> GetAvailableEngines()
    {
        return _engines.Values.Where(e => e.IsAvailable).ToList();
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        private Action? _action = action;
        public void Dispose() => Interlocked.Exchange(ref _action, null)?.Invoke();
    }
}
