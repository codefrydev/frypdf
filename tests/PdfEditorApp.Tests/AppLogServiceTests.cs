using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Services;
using Xunit;

namespace PdfEditorApp.Tests;

// These tests read/clear the AppLogService.Instance singleton's shared buffer, so they must
// never run concurrently with other tests that do the same (see the other classes tagged below).
[Collection("AppLogService")]
public class AppLogServiceTests
{
    [Fact]
    public void ParseRaw_TraceSourceWarning_CategorizedAsWarning()
    {
        var raw = "PdfEditorApp Warning: 0 : [QuestPDF] QuestPDF.Settings.UseEnvironmentFonts is enabled. QuestPDF may use fonts installed on the current machine in addition to fonts explicitly registered with QuestPDF.Drawing.FontManager. This is convenient during development, but it can make documents depend on fonts that are not available in production, especially in minimal Docker images, serverless functions, and other reduced runtime environments. As a result, the same document may fail to render or may use different fallback fonts after deployment. For predictable output, deploy the required fonts with your application and set QuestPDF.Settings.UseEnvironmentFonts to false. This setting remains enabled by default for backward compatibility.";

        var (category, body, level) = AppLogService.ParseRaw(raw);

        Assert.Equal("QuestPDF", category);
        Assert.Equal(AppLogLevel.Warning, level);
        Assert.Contains("UseEnvironmentFonts is enabled", body);
    }

    [Fact]
    public void ParseRaw_AvaloniaBindingNullEvaluation_CategorizedAsDebug()
    {
        var raw = "[Binding] An error occurred binding 'Text' to 'CurrentPage.HeaderLeft' at 'CurrentPage': 'Value is null.' (TextBlock #12152506)";

        var (category, body, level) = AppLogService.ParseRaw(raw);

        Assert.Equal("Binding", category);
        Assert.Equal(AppLogLevel.Debug, level);
        Assert.Contains("Value is null", body);
    }

    [Fact]
    public void ParseRaw_AvaloniaBindingConverterFailure_CategorizedAsError()
    {
        var raw = "[Binding] An error occurred binding 'Kind' to 'IconKind': 'Could not convert 'Stamp' (System.String) to 'System.Nullable`1[Material.Icons.MaterialIconKind]'.' (MaterialIcon #35138254)";

        var (category, body, level) = AppLogService.ParseRaw(raw);

        Assert.Equal("Binding", category);
        Assert.Equal(AppLogLevel.Error, level);
        Assert.Contains("Could not convert", body);
    }

    [Fact]
    public void ParseRaw_TraceSourceError_CategorizedAsError()
    {
        var raw = "PdfEditorApp Error: 0 : [Kernel] Failed to mount plugin 'custom.tool'";

        var (category, body, level) = AppLogService.ParseRaw(raw);

        Assert.Equal("Kernel", category);
        Assert.Equal(AppLogLevel.Error, level);
        Assert.Contains("Failed to mount", body);
    }

    [Fact]
    public void ParseRaw_SuccessKeyword_CategorizedAsInfo()
    {
        var raw = "[Document] Document exported successfully to output.pdf";

        var (category, body, level) = AppLogService.ParseRaw(raw);

        Assert.Equal("Document", category);
        Assert.Equal(AppLogLevel.Info, level);
    }

    [Fact]
    public void ParseRaw_FallbackKeyword_CategorizedAsWarning()
    {
        var raw = "[Font] Missing glyph, using fallback font Inter";

        var (category, body, level) = AppLogService.ParseRaw(raw);

        Assert.Equal("Font", category);
        Assert.Equal(AppLogLevel.Warning, level);
    }

    [Fact]
    public void LogError_ReflectionTypeLoadException_UnwrapsLoaderExceptions()
    {
        // ex.ToString() alone omits LoaderExceptions — the actual per-type failure reasons —
        // which is exactly what a Windows-only missing-dependency plugin load needs surfaced.
        var loaderFailure = new InvalidOperationException("Simulated-Loader-Failure-Marker-12345");
        var rtle = new System.Reflection.ReflectionTypeLoadException(
            new Type?[] { null },
            new Exception?[] { loaderFailure },
            "Some types failed to load.");

        AppLogService.Instance.LogError("TestCategory", "Reflection failed", rtle);

        var snapshot = AppLogService.Instance.GetSnapshot();
        Assert.Contains(snapshot, e =>
            e.Category == "TestCategory" &&
            e.Level == AppLogLevel.Error &&
            e.Message.Contains("Simulated-Loader-Failure-Marker-12345"));
    }

    [Fact]
    public void AppLogService_LogAndClear_MaintainsBufferAndState()
    {
        var service = AppLogService.Instance;
        service.Clear();

        service.Log(AppLogLevel.Info, "TestCategory", "Test Message 1");
        service.Log(AppLogLevel.Warning, "TestCategory", "Test Message 2");

        var snapshot = service.GetSnapshot();
        Assert.True(snapshot.Count >= 2);
        var lastTwo = snapshot.TakeLast(2).ToList();
        Assert.Equal("Test Message 1", lastTwo[0].Message);
        Assert.Equal(AppLogLevel.Info, lastTwo[0].Level);
        Assert.Equal("Test Message 2", lastTwo[1].Message);
        Assert.Equal(AppLogLevel.Warning, lastTwo[1].Level);

        service.Clear();
        var cleared = service.GetSnapshot();
        Assert.Empty(cleared);
    }

    [Fact]
    public async Task Log_PersistsToRollingFileOnDisk()
    {
        // File writes happen on a background task (see AppLogFileWriter) so the UI thread
        // never blocks — poll briefly rather than asserting immediately after Log().
        var marker = $"FileWriterTestMarker-{Guid.NewGuid():N}";
        AppLogService.Instance.Log(AppLogLevel.Info, "TestCategory", marker);

        var logFilePath = FryPdfPaths.LogFilePath;
        var deadline = DateTime.UtcNow.AddSeconds(5);
        string content = string.Empty;
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(logFilePath))
            {
                try { content = await File.ReadAllTextAsync(logFilePath); }
                catch (IOException) { /* file mid-write; retry */ }
                if (content.Contains(marker)) break;
            }
            await Task.Delay(100);
        }

        Assert.Contains(marker, content);
    }
}
