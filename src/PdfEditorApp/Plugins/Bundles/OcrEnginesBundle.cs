using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Services.Ocr;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing all built-in OCR engines (Apple Vision, Windows Media, Tesseract).
/// </summary>
public class OcrEnginesBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.OcrEngines";
    public string Name => "OCR Engines Bundle";
    public string Description => "Hardware-accelerated optical character recognition: macOS Apple Vision, Windows Media OCR, and Tesseract neural network models.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new AppleVisionOcrPlugin(),
        new WindowsMediaOcrPlugin(),
        new TesseractOcrPlugin()
    };
}

public class AppleVisionOcrPlugin : IFryPlugin
{
    public string Id => "frypdf.ocr.applevision";
    public string Name => "Apple Vision Framework OCR (macOS)";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterOcrEngine(new AppleVisionOcrEngine());
        return Task.CompletedTask;
    }
}

public class WindowsMediaOcrPlugin : IFryPlugin
{
    public string Id => "frypdf.ocr.windowsmedia";
    public string Name => "Windows Media OCR Engine (Windows)";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterOcrEngine(new WindowsMediaOcrEngine());
        return Task.CompletedTask;
    }
}

public class TesseractOcrPlugin : IFryPlugin
{
    public string Id => "frypdf.ocr.tesseract";
    public string Name => "Tesseract Neural Network OCR";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        var modelService = ctx.TryGetService<ITesseractModelService>(out var ms) ? ms : new TesseractModelService();
        ctx.RegisterOcrEngine(new TesseractOcrEngine(modelService));
        return Task.CompletedTask;
    }
}
