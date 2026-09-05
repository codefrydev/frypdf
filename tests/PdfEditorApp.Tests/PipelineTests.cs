using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins.Pipelines;
using Xunit;

namespace PdfEditorApp.Tests;

public class PipelineTests
{
    private class TestPipelineContext
    {
        public List<string> Trace { get; } = new();
        public int Value { get; set; }
    }

    [Fact]
    public async Task Waterfall_ExecutesMiddlewareInWrappingOrderWithTerminal()
    {
        var manager = new PipelineManager();
        var context = new TestPipelineContext();

        manager.RegisterWaterfall<TestPipelineContext>("test:export", async (ctx, next) =>
        {
            ctx.Trace.Add("M1-Before");
            ctx.Value += 10;
            await next();
            ctx.Trace.Add("M1-After");
        });

        manager.RegisterWaterfall<TestPipelineContext>("test:export", async (ctx, next) =>
        {
            ctx.Trace.Add("M2-Before");
            ctx.Value *= 2;
            await next();
            ctx.Trace.Add("M2-After");
        });

        await manager.ExecuteWaterfallAsync("test:export", context, terminal: () =>
        {
            context.Trace.Add("Terminal");
            context.Value += 5;
            return Task.CompletedTask;
        });

        // Pipeline execution order: M1-Before -> M2-Before -> Terminal -> M2-After -> M1-After
        Assert.Equal(new[] { "M1-Before", "M2-Before", "Terminal", "M2-After", "M1-After" }, context.Trace);
        // Value: (0 + 10) * 2 + 5 = 25
        Assert.Equal(25, context.Value);
    }

    [Fact]
    public async Task Bail_HaltsOnFirstNonNullResult()
    {
        var manager = new PipelineManager();
        var executionLog = new List<string>();

        manager.RegisterBail<string, string>("convert:file", async input =>
        {
            executionLog.Add("Handler1");
            if (input.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                return await Task.FromResult("DocxConverted");
            return null;
        });

        manager.RegisterBail<string, string>("convert:file", async input =>
        {
            executionLog.Add("Handler2");
            if (input.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return await Task.FromResult("ExcelConverted");
            return null;
        });

        manager.RegisterBail<string, string>("convert:file", async input =>
        {
            executionLog.Add("Handler3");
            return await Task.FromResult("FallbackConverted");
        });

        // Test 1: Handled by Handler2
        var resultXlsx = await manager.ExecuteBailAsync<string, string>("convert:file", "sheet.xlsx");
        Assert.Equal("ExcelConverted", resultXlsx);
        Assert.Equal(new[] { "Handler1", "Handler2" }, executionLog);

        executionLog.Clear();

        // Test 2: Handled by Handler1
        var resultDocx = await manager.ExecuteBailAsync<string, string>("convert:file", "doc.docx");
        Assert.Equal("DocxConverted", resultDocx);
        Assert.Equal(new[] { "Handler1" }, executionLog);
    }

    [Fact]
    public async Task Parallel_ExecutesAllHandlersConcurrently()
    {
        var manager = new PipelineManager();
        var results = new List<int>();
        var lockObj = new object();

        manager.RegisterParallel<int>("event:notify", async val =>
        {
            await Task.Delay(10);
            lock (lockObj) results.Add(val * 1);
        });

        manager.RegisterParallel<int>("event:notify", async val =>
        {
            await Task.Delay(5);
            lock (lockObj) results.Add(val * 2);
        });

        await manager.ExecuteParallelAsync("event:notify", 5);

        Assert.Equal(2, results.Count);
        Assert.Contains(5, results);
        Assert.Contains(10, results);
    }

    [Fact]
    public async Task Serial_ExecutesHandlersInDeterministicOrder()
    {
        var manager = new PipelineManager();
        var results = new List<string>();

        manager.RegisterSerial<string>("validate:doc", async name =>
        {
            await Task.Delay(15);
            results.Add($"Step1-{name}");
        });

        manager.RegisterSerial<string>("validate:doc", async name =>
        {
            await Task.Delay(5);
            results.Add($"Step2-{name}");
        });

        await manager.ExecuteSerialAsync("validate:doc", "Sample");

        Assert.Equal(new[] { "Step1-Sample", "Step2-Sample" }, results);
    }

    [Fact]
    public async Task Pipeline_DisposingRegistration_RemovesFromExecution()
    {
        var manager = new PipelineManager();
        var count = 0;

        var reg = manager.RegisterSerial<int>("counter", _ =>
        {
            count++;
            return Task.CompletedTask;
        });

        await manager.ExecuteSerialAsync("counter", 0);
        Assert.Equal(1, count);

        // Dispose the registration
        reg.Dispose();

        await manager.ExecuteSerialAsync("counter", 0);
        Assert.Equal(1, count); // Not called again
    }
}
