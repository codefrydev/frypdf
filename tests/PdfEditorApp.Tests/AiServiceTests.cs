using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.AI;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class AiServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempSettingsPath;

    public AiServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FryPdf_AiServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _tempSettingsPath = Path.Combine(_tempDir, "ui_settings.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void AiModelInfo_TierAndBadges_CalculatedCorrectly()
    {
        // 1. Free Local Model
        var localModel = new AiModelInfo
        {
            Id = "llama3.2:latest",
            DisplayName = "Llama 3.2",
            Provider = AiProviderType.OllamaLocal,
            Tier = AiModelTier.FreeLocal,
            SizeBytes = 2_000_000_000L
        };

        Assert.True(localModel.IsFree);
        Assert.False(localModel.IsCloud);
        Assert.Equal("Free / Local", localModel.TierBadgeText);
        Assert.Equal("#DCFCE7", localModel.BadgeBackgroundColor);
        Assert.Contains("[Free / Local]", localModel.FullTitleWithBadge);

        // 2. Free Cloud Tier Model (e.g. Ollama Cloud Free, Groq Free)
        var freeCloudModel = new AiModelInfo
        {
            Id = "llama-3.1-8b-instant",
            DisplayName = "Llama 3.1 8B (Groq Free Cloud)",
            Provider = AiProviderType.CustomOpenAiCompatible,
            Tier = AiModelTier.FreeCloud
        };

        Assert.True(freeCloudModel.IsFree);
        Assert.True(freeCloudModel.IsCloud);
        Assert.Equal("Free / Cloud", freeCloudModel.TierBadgeText);
        Assert.Equal("#E0F2FE", freeCloudModel.BadgeBackgroundColor);
        Assert.Contains("[Free / Cloud]", freeCloudModel.FullTitleWithBadge);

        // 3. Paid Cloud Model
        var cloudModel = new AiModelInfo
        {
            Id = "gpt-4o-mini",
            DisplayName = "GPT-4o Mini",
            Provider = AiProviderType.OpenAiCloud,
            Tier = AiModelTier.PaidCloud
        };

        Assert.False(cloudModel.IsFree);
        Assert.True(cloudModel.IsCloud);
        Assert.Equal("Paid / Cloud", cloudModel.TierBadgeText);
        Assert.Equal("#EDE9FE", cloudModel.BadgeBackgroundColor);
        Assert.Contains("[Paid / Cloud]", cloudModel.FullTitleWithBadge);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(-100, "")]
    [InlineData(500 * 1024, "500.0 KB")]
    [InlineData(100 * 1024 * 1024, "100.0 MB")]
    [InlineData(2147483648L, "2.0 GB")]
    public void AiModelInfo_FormatBytes_FormatsAsExpected(long bytes, string expected)
    {
        string actual = AiModelInfo.FormatBytes(bytes);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AiSettingsModel_DefaultsAndClone_WorkProperly()
    {
        var settings = new AiSettingsModel();

        Assert.Equal(AiProviderType.OllamaLocal, settings.SelectedProvider);
        Assert.Equal("http://localhost:11434", settings.OllamaEndpoint);
        Assert.False(settings.IsOllamaRemote);
        Assert.Equal("llama3.2", settings.SelectedModelId);
        Assert.Equal(0.7f, settings.Temperature);

        settings.OllamaApiKey = "ollama-cloud-jwt-token";
        settings.OpenAiApiKey = "sk-test-dummy-key";
        settings.CustomBaseUrl = "https://api.groq.com/openai/v1";
        settings.DiscoveredOllamaModels.Add(new AiModelInfo { Id = "test-model", DisplayName = "Test" });

        var clone = settings.Clone();

        Assert.NotNull(clone);
        Assert.Equal(settings.SelectedProvider, clone.SelectedProvider);
        Assert.Equal(settings.OllamaEndpoint, clone.OllamaEndpoint);
        Assert.Equal(settings.OllamaApiKey, clone.OllamaApiKey);
        Assert.Equal(settings.OpenAiApiKey, clone.OpenAiApiKey);
        Assert.Equal(settings.CustomBaseUrl, clone.CustomBaseUrl);
        Assert.Single(clone.DiscoveredOllamaModels);
        Assert.Equal("test-model", clone.DiscoveredOllamaModels[0].Id);

        // Remote Ollama detection
        clone.OllamaEndpoint = "https://ollama.mycompany.cloud";
        Assert.True(clone.IsOllamaRemote);

        // Verify independent instance
        clone.DiscoveredOllamaModels.Clear();
        Assert.Single(settings.DiscoveredOllamaModels);
    }

    [Fact]
    public void AiSettingsModel_JsonSerialization_RoundtripsAccurately()
    {
        var original = new AiSettingsModel
        {
            SelectedProvider = AiProviderType.CustomOpenAiCompatible,
            OllamaApiKey = "my-token",
            CustomBaseUrl = "http://localhost:8080/v1",
            CustomModelName = "mistral-small",
            Temperature = 0.5f,
            SystemInstructions = "Always use blue accent colors"
        };

        string json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AiSettingsModel>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.SelectedProvider, deserialized.SelectedProvider);
        Assert.Equal(original.OllamaApiKey, deserialized.OllamaApiKey);
        Assert.Equal(original.CustomBaseUrl, deserialized.CustomBaseUrl);
        Assert.Equal(original.CustomModelName, deserialized.CustomModelName);
        Assert.Equal(original.Temperature, deserialized.Temperature);
        Assert.Equal(original.SystemInstructions, deserialized.SystemInstructions);
    }

    [Fact]
    public void AiService_GetCloudCatalogModels_ReturnsBothFreeAndPaidTiers()
    {
        var aiService = new AiService();
        var models = aiService.GetCloudCatalogModels();

        Assert.NotEmpty(models);

        // Must contain Free Cloud tier models (e.g. Groq free tier, OpenRouter free)
        var freeCloud = models.Where(m => m.Tier == AiModelTier.FreeCloud).ToList();
        Assert.NotEmpty(freeCloud);
        Assert.All(freeCloud, m =>
        {
            Assert.True(m.IsFree);
            Assert.True(m.IsCloud);
            Assert.Equal("Free / Cloud", m.TierBadgeText);
        });

        // Must contain Paid Cloud tier models (e.g. OpenAI GPT-4o)
        var paidCloud = models.Where(m => m.Tier == AiModelTier.PaidCloud).ToList();
        Assert.NotEmpty(paidCloud);
        Assert.All(paidCloud, m =>
        {
            Assert.False(m.IsFree);
            Assert.True(m.IsCloud);
            Assert.Equal("Paid / Cloud", m.TierBadgeText);
        });

        Assert.Contains(models, m => m.Id == "gpt-4o-mini");
        Assert.Contains(models, m => m.Id == "gpt-4o");
        Assert.Contains(models, m => m.Id == "llama-3.1-8b-instant");
    }

    [Fact]
    public void AiService_GetUnifiedModelCatalog_IncludesExpectedModels()
    {
        var aiService = new AiService();
        var settings = new AiSettingsModel
        {
            SelectedProvider = AiProviderType.OllamaLocal
        };
        settings.DiscoveredOllamaModels.Add(new AiModelInfo
        {
            Id = "custom-llama:latest",
            DisplayName = "Custom Llama",
            Provider = AiProviderType.OllamaLocal,
            Tier = AiModelTier.FreeLocal
        });

        var catalog = aiService.GetUnifiedModelCatalog(settings);

        Assert.NotEmpty(catalog);
        Assert.Contains(catalog, m => m.Id == "custom-llama:latest");

        // When switching to OpenAI provider, catalog includes cloud models
        settings.SelectedProvider = AiProviderType.OpenAiCloud;
        var cloudCatalog = aiService.GetUnifiedModelCatalog(settings);
        Assert.Contains(cloudCatalog, m => m.Id == "gpt-4o-mini");
        Assert.Contains(cloudCatalog, m => m.Tier == AiModelTier.FreeCloud);
    }

    [Fact]
    public async Task AiService_DiscoverOllamaModelsAsync_HandlesUnreachableEndpointGracefully()
    {
        var aiService = new AiService();
        // Use an unassigned port that won't respond
        var models = await aiService.DiscoverOllamaModelsAsync("http://127.0.0.1:59999", CancellationToken.None);

        Assert.NotNull(models);
        Assert.Empty(models);
    }

    [Fact]
    public async Task AiService_TestConnectionAsync_ReportsFailureForEmptyCloudApiKey()
    {
        var aiService = new AiService();
        var settings = new AiSettingsModel
        {
            SelectedProvider = AiProviderType.OpenAiCloud,
            OpenAiApiKey = ""
        };

        var (success, message, _) = await aiService.TestConnectionAsync(settings, CancellationToken.None);

        Assert.False(success);
        Assert.Contains("API Key", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AiSettingsViewModel_InitializesAndUpdatesCatalog()
    {
        var uiSettings = new UiSettingsService(_tempSettingsPath);
        var aiService = new AiService();
        var vm = new AiSettingsViewModel(uiSettings, aiService);

        Assert.NotNull(vm.AvailableModels);
        Assert.NotEmpty(vm.AvailableModels);
        Assert.NotNull(vm.SelectedModel);

        // Switch to Cloud provider
        vm.SelectedProvider = AiProviderType.OpenAiCloud;
        Assert.NotEmpty(vm.AvailableModels);
        Assert.Contains(vm.AvailableModels, m => !m.IsFree);
        Assert.Contains(vm.AvailableModels, m => m.Id == "gpt-4o-mini");
        Assert.Contains(vm.AvailableModels, m => m.Tier == AiModelTier.FreeCloud);
    }

    [Fact]
    public void AiAssistantViewModel_InitialStateAndPrompts_AreValid()
    {
        var uiSettings = new UiSettingsService(_tempSettingsPath);
        var aiService = new AiService();
        var agentService = new PdfStudioAgentService(aiService);
        var vm = new AiAssistantViewModel(agentService, uiSettings, aiService);

        Assert.False(vm.IsOpen);
        Assert.False(vm.IsGenerating);
        Assert.NotEmpty(vm.SuggestedPrompts);
        Assert.Contains(vm.SuggestedPrompts, p => p.Contains("Invoice", StringComparison.OrdinalIgnoreCase));

        vm.Open();
        Assert.True(vm.IsOpen);

        vm.Close();
        Assert.False(vm.IsOpen);
    }

    [Fact]
    public async Task PdfStudioAgentService_ExecutePromptAsync_ValidatesInput()
    {
        var aiService = new AiService();
        var agentService = new PdfStudioAgentService(aiService);
        var settings = new AiSettingsModel();

        // Empty prompt test
        var emptyResult = await agentService.ExecutePromptAsync("", null!, settings);
        Assert.False(emptyResult.Success);
        Assert.Contains("empty", emptyResult.Message, StringComparison.OrdinalIgnoreCase);

        // Null target page test
        var nullPageResult = await agentService.ExecutePromptAsync("Create heading", null!, settings);
        Assert.False(nullPageResult.Success);
        Assert.Contains("required", nullPageResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseOllamaLibraryHtml_ExtractsModelsAndCloudBadgesAccurately()
    {
        string sampleHtml = """
            <li class="flex items-baseline border-b border-neutral-200 py-6">
              <a href="/library/qwen3.8-flash-next" class="group w-full">
                <div class="flex flex-col mb-1" title="qwen3.8-flash-next">
                  <h2 class="truncate text-xl font-medium underline-offset-2 group-hover:underline md:text-2xl">
                    <span>qwen3.8-flash-next</span>
                  </h2>
                  <p class="max-w-lg break-words text-neutral-800 text-md">This experimental preview of the architecture that will underpin Qwen4.</p>
                </div>
                <div class="flex flex-col">
                  <div class="flex flex-wrap space-x-2">
                    <span class="inline-flex my-1 items-center rounded-md bg-indigo-50 px-2 py-[2px] text-xs font-medium text-indigo-600 sm:text-[13px]">vision</span>
                    <span class="inline-flex my-1 items-center rounded-md bg-indigo-50 px-2 py-[2px] text-xs font-medium text-indigo-600 sm:text-[13px]">tools</span>
                    <span class="inline-flex my-1 items-center rounded-md bg-indigo-50 px-2 py-[2px] text-xs font-medium text-indigo-600 sm:text-[13px]">thinking</span>
                  </div>
                  <p class="my-1 flex space-x-5 text-[13px] font-medium text-neutral-500">
                    <span class="flex items-center"><span>69.7K</span><span class="hidden sm:flex">&nbsp;Pulls</span></span>
                  </p>
                </div>
              </a>
            </li>
            <li class="flex items-baseline border-b border-neutral-200 py-6">
              <a href="/library/glm-5.3-flash" class="group w-full">
                <div class="flex flex-col mb-1" title="glm-5.3-flash">
                  <h2 class="truncate text-xl font-medium underline-offset-2 group-hover:underline md:text-2xl">
                    <span>glm-5.3-flash</span>
                  </h2>
                  <p class="max-w-lg break-words text-neutral-800 text-md">Z.ai's first natively multimodal model with 18B active parameters.</p>
                </div>
                <div class="flex flex-col">
                  <div class="flex flex-wrap space-x-2">
                    <span class="inline-flex my-1 items-center rounded-md bg-indigo-50 px-2 py-[2px] text-xs font-medium text-indigo-600 sm:text-[13px]">vision</span>
                    <span class="inline-flex my-1 items-center rounded-md bg-indigo-50 px-2 py-[2px] text-xs font-medium text-indigo-600 sm:text-[13px]">tools</span>
                    <span class="inline-flex my-1 items-center rounded-md bg-indigo-50 px-2 py-[2px] text-xs font-medium text-indigo-600 sm:text-[13px]">thinking</span>
                    <span class="inline-flex my-1 items-center rounded-md bg-cyan-50 px-2 py-[2px] text-xs font-medium text-cyan-500 sm:text-[13px]">cloud</span>
                  </div>
                </div>
              </a>
            </li>
            """;

        var models = AiService.ParseOllamaLibraryHtml(sampleHtml);

        Assert.Equal(2, models.Count);

        // Model 1: Local
        var qwen = models.First(m => m.Id == "qwen3.8-flash-next");
        Assert.Equal(AiModelTier.FreeLocal, qwen.Tier);
        Assert.Contains("Vision", qwen.Capabilities);
        Assert.Contains("Tools", qwen.Capabilities);
        Assert.Contains("Thinking", qwen.Capabilities);
        Assert.Contains("69.7K pulls", qwen.FormattedSize);

        // Model 2: Cloud
        var glm = models.First(m => m.Id == "glm-5.3-flash");
        Assert.Equal(AiModelTier.FreeCloud, glm.Tier);
        Assert.Equal("Ollama Cloud", glm.FormattedSize);
        Assert.Equal("18B", glm.ParameterSize);
    }

    [Fact]
    public void AiSettingsViewModel_CustomModelTyping_SynthesizesNonEmptyMetadata()
    {
        var uiSettings = new UiSettingsService(_tempSettingsPath);
        var aiService = new AiService();
        var vm = new AiSettingsViewModel(uiSettings, aiService);

        // Type an unlisted custom model
        vm.SelectedModelId = "custom-nemotron-70b-preview";

        var activeInfo = vm.ActiveModelInfo;
        Assert.NotNull(activeInfo);
        Assert.Equal("custom-nemotron-70b-preview", activeInfo.Id);
        Assert.Equal("custom-nemotron-70b-preview", activeInfo.DisplayName);
        Assert.Equal(AiModelTier.FreeLocal, activeInfo.Tier);
        Assert.False(string.IsNullOrWhiteSpace(activeInfo.TierBadgeText));
    }
}
