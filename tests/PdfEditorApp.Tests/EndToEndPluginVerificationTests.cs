using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Plugins.Loader;
using PdfEditorApp.Core.Plugins;
using Xunit;

namespace PdfEditorApp.Tests;

public class EndToEndPluginVerificationTests
{
    [Fact]
    public async Task HostLoader_LoadsPlugin_AndExecutesDumpTableSuccessfully()
    {
        var pluginPath = Path.Combine(
            AppContext.BaseDirectory,
            "plugins",
            "com.frypdf.plugin.csharpeditor",
            "CSharpEditorPlugin.dll");

        if (!File.Exists(pluginPath)) return;

        using var package = PluginAssemblyLoader.LoadPluginAssembly(pluginPath);
        Assert.NotNull(package);
        Assert.False(package.IsCollectible);
        Assert.False(package.Context.IsCollectible);
        Assert.NotEmpty(package.Plugins);

        var plugin = package.Plugins[0];
        Assert.Equal("com.frypdf.plugin.csharpeditor", plugin.Id);

        var asm = package.Context.Assemblies.FirstOrDefault(a => a.GetName().Name == "CSharpEditorPlugin");
        Assert.NotNull(asm);

        var kernelType = asm.GetType("PdfEditorApp.Plugins.CSharpEditor.Services.NotebookExecutionKernel");
        Assert.NotNull(kernelType);

        dynamic kernel = Activator.CreateInstance(kernelType)!;
        object? richOutputReceived = null;

        var task = (Task)kernel.ExecuteCellAsync(
            "int[] digit = [1, 2, 3, 4, 4, 0, 0, 7, 6]; digit.Dump();",
            onLiveConsole: null,
            onRichOutput: new Action<object>(output => {
                richOutputReceived = output;
            }),
            ct: System.Threading.CancellationToken.None
        );

        await task;

        dynamic result = ((dynamic)task).Result;
        Assert.True(result.Success, (string)result.ErrorMessage);
        Assert.NotNull(richOutputReceived);

        dynamic rich = richOutputReceived;
        Assert.Equal("Table", rich.Kind.ToString());

        dynamic table = rich.TableResult;
        Assert.Equal(9, table.Rows.Count);
        Assert.Equal(1, table.Columns.Count);
        Assert.Equal("Item", table.Columns[0].Header);
    }

    [Fact]
    public async Task HostLoader_LoadsPlugin_AndExecutesSkiaSharpRenderingSuccessfully()
    {
        var pluginPath = Path.Combine(
            AppContext.BaseDirectory,
            "plugins",
            "com.frypdf.plugin.csharpeditor",
            "CSharpEditorPlugin.dll");

        if (!File.Exists(pluginPath)) return;

        using var package = PluginAssemblyLoader.LoadPluginAssembly(pluginPath);
        Assert.NotNull(package);

        var asm = package.Context.Assemblies.FirstOrDefault(a => a.GetName().Name == "CSharpEditorPlugin");
        Assert.NotNull(asm);

        var kernelType = asm.GetType("PdfEditorApp.Plugins.CSharpEditor.Services.NotebookExecutionKernel");
        Assert.NotNull(kernelType);

        dynamic kernel = Activator.CreateInstance(kernelType)!;
        object? richOutputReceived = null;

        var skiaCode = @"#r ""nuget: SkiaSharp, 3.119.4""
using SkiaSharp;

var info = new SKImageInfo(400, 200);
var surface = SKSurface.Create(info);
var canvas = surface.Canvas;

var paint = new SKPaint { Color = new SKColor(100, 150, 250), IsAntialias = true };
canvas.DrawCircle(100, 100, 40, paint);

Display.Image(surface.Snapshot());
Console.WriteLine(""SkiaSharp in Host Executed!"");";

        var task = (Task)kernel.ExecuteCellAsync(
            skiaCode,
            onLiveConsole: null,
            onRichOutput: new Action<object>(output => {
                richOutputReceived = output;
            }),
            ct: System.Threading.CancellationToken.None
        );

        await task;

        dynamic result = ((dynamic)task).Result;
        Assert.True(result.Success, (string)result.ErrorMessage);
        Assert.NotNull(richOutputReceived);

        dynamic rich = richOutputReceived;
        Assert.Equal("Image", rich.Kind.ToString());
        Assert.NotNull(rich.ImageBytes);
        Assert.True(rich.ImageBytes.Length > 0);
    }
}
