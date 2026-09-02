using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Services.Ocr;

public interface ICompositeOcrProvider : IOcrEngine
{
    OcrEngineType PreferredEngine { get; set; }
    ITesseractModelService ModelService { get; }
    IReadOnlyList<IOcrEngine> AvailableEngines { get; }
    IOcrEngine ActiveEngine { get; }
}

public class CompositeOcrProvider : ICompositeOcrProvider
{
    private static CompositeOcrProvider? _instance;
    public static CompositeOcrProvider Default => _instance ??= new CompositeOcrProvider();

    private readonly List<IOcrEngine> _engines = new();
    private readonly ITesseractModelService _modelService;
    private readonly AppleVisionOcrEngine _appleEngine;
    private readonly WindowsMediaOcrEngine _windowsEngine;
    private readonly TesseractOcrEngine _tesseractEngine;

    public string EngineName => ActiveEngine.EngineName;
    public OcrEngineType EngineType => ActiveEngine.EngineType;
    public bool IsAvailable => ActiveEngine.IsAvailable;
    public OcrEngineType PreferredEngine { get; set; } = OcrEngineType.Auto;
    public ITesseractModelService ModelService => _modelService;
    public IReadOnlyList<IOcrEngine> AvailableEngines => _engines;

    public IOcrEngine ActiveEngine
    {
        get
        {
            if (PreferredEngine == OcrEngineType.OsNative)
            {
                if (OperatingSystem.IsMacOS()) return _appleEngine;
                if (OperatingSystem.IsWindows()) return _windowsEngine;
            }
            else if (PreferredEngine == OcrEngineType.Tesseract)
            {
                return _tesseractEngine;
            }

            // Auto mode: prioritize OS Native for zero-download instant execution
            if (OperatingSystem.IsMacOS() && _appleEngine.IsAvailable) return _appleEngine;
            if (OperatingSystem.IsWindows() && _windowsEngine.IsAvailable) return _windowsEngine;
            return _tesseractEngine;
        }
    }

    public CompositeOcrProvider(ITesseractModelService? modelService = null)
    {
        _modelService = modelService ?? new TesseractModelService();
        _appleEngine = new AppleVisionOcrEngine();
        _windowsEngine = new WindowsMediaOcrEngine();
        _tesseractEngine = new TesseractOcrEngine(_modelService);

        if (_appleEngine.IsAvailable) _engines.Add(_appleEngine);
        if (_windowsEngine.IsAvailable) _engines.Add(_windowsEngine);
        _engines.Add(_tesseractEngine);
    }

    public async Task<OcrResult> RecognizeTextAsync(byte[] imageBytes, string language = "eng", CancellationToken ct = default)
    {
        var primary = ActiveEngine;
        var result = await primary.RecognizeTextAsync(imageBytes, language, ct);

        if (result.Success && (result.Words.Count > 0 || !string.IsNullOrWhiteSpace(result.FullText)))
        {
            return result;
        }

        // If primary engine produced nothing or failed and we're in Auto mode, try fallback
        if (PreferredEngine == OcrEngineType.Auto && primary != _tesseractEngine && _tesseractEngine.IsAvailable)
        {
            var fallback = await _tesseractEngine.RecognizeTextAsync(imageBytes, language, ct);
            if (fallback.Success)
            {
                return fallback;
            }
        }

        return result;
    }
}
