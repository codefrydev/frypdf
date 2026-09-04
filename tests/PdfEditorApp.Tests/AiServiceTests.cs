using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Services.AI;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;
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

    [Theory]
    [InlineData("https://api.groq.com/openai/v1/chat/completions", "https://api.groq.com/openai/v1")]
    [InlineData("https://api.groq.com/openai/v1/chat/completions/", "https://api.groq.com/openai/v1")]
    [InlineData("https://api.groq.com/openai/v1", "https://api.groq.com/openai/v1")]
    [InlineData("https://api.groq.com/openai/v1/", "https://api.groq.com/openai/v1")]
    [InlineData("https://openrouter.ai/api/v1/chat/completions", "https://openrouter.ai/api/v1")]
    [InlineData(null, "https://api.openai.com/v1")]
    [InlineData("", "https://api.openai.com/v1")]
    [InlineData("   ", "https://api.openai.com/v1")]
    public void AiService_NormalizeCustomOpenAiBaseUrl_SanitizesEndpoints(string? input, string expected)
    {
        string actual = AiService.NormalizeCustomOpenAiBaseUrl(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AiSettingsViewModel_CustomModelName_SynchronizesWithSelectedModelId()
    {
        var uiSettings = new UiSettingsService(_tempSettingsPath);
        var aiService = new AiService();
        var vm = new AiSettingsViewModel(uiSettings, aiService)
        {
            SelectedProvider = AiProviderType.CustomOpenAiCompatible
        };

        // User types custom model into CustomModelName field
        vm.CustomModelName = "qwen/qwen3.6-27b";

        Assert.Equal("qwen/qwen3.6-27b", vm.SelectedModelId);
        Assert.Equal("qwen/qwen3.6-27b", vm.ActiveModelInfo.Id);

        // Selecting a model from the list updates CustomModelName as well
        vm.SelectedModelId = "openai/gpt-oss-120b";
        Assert.Equal("openai/gpt-oss-120b", vm.CustomModelName);
    }

    [Theory]
    [InlineData("groq", "https://api.groq.com/openai/v1", "openai/gpt-oss-120b")]
    [InlineData("openrouter", "https://openrouter.ai/api/v1", "meta-llama/llama-3.2-3b-instruct:free")]
    [InlineData("together", "https://api.together.xyz/v1", "meta-llama/Llama-3.3-70B-Instruct-Turbo")]
    [InlineData("lmstudio", "http://localhost:1234/v1", "local-model")]
    public void AiSettingsViewModel_ApplyPreset_ConfiguresProviderAndModelsCorrectly(string preset, string expectedUrl, string expectedModel)
    {
        var uiSettings = new UiSettingsService(_tempSettingsPath);
        var aiService = new AiService();
        var vm = new AiSettingsViewModel(uiSettings, aiService)
        {
            SelectedProvider = AiProviderType.CustomOpenAiCompatible
        };

        vm.ApplyPreset(preset);

        Assert.Equal(expectedUrl, vm.CustomBaseUrl);
        Assert.Equal(expectedModel, vm.SelectedModelId);
        Assert.Equal(expectedModel, vm.CustomModelName);
    }

    [Fact]
    public void AiSettingsViewModel_CustomCompatibleMode_ExcludesOllamaModelsAndProvidesDynamicHistory()
    {
        var uiSettings = new UiSettingsService(_tempSettingsPath);
        uiSettings.UpdateSettings(s =>
        {
            s.AiSettings.DiscoveredOllamaModels.Clear();
            s.AiSettings.DiscoveredOllamaModels.Add(new AiModelInfo { Id = "ollama-local-exclusive", DisplayName = "Ollama Local Only", Provider = AiProviderType.OllamaLocal });
        });

        var aiService = new AiService();
        var vm = new AiSettingsViewModel(uiSettings, aiService)
        {
            SelectedProvider = AiProviderType.CustomOpenAiCompatible,
            CustomBaseUrl = "https://api.groq.com/openai/v1"
        };

        // Assert Ollama model is NOT present in AvailableModels
        Assert.DoesNotContain(vm.AvailableModels, m => m.Id == "ollama-local-exclusive");

        // Assert Groq/cloud models are present
        Assert.Contains(vm.AvailableModels, m => m.Id == "openai/gpt-oss-120b");

        // Assert dynamic subtitle and placeholder
        Assert.Contains("custom model identifier", vm.ModelSectionSubtitle, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ANY", vm.ModelInputPlaceholder);
    }

    [Fact]
    public void AiSettingsViewModel_SaveCustomModel_FutureProofWithoutCodeChanges()
    {
        var uiSettings = new UiSettingsService(_tempSettingsPath);
        var aiService = new AiService();
        var vm = new AiSettingsViewModel(uiSettings, aiService)
        {
            SelectedProvider = AiProviderType.CustomOpenAiCompatible,
            CustomBaseUrl = "https://api.groq.com/openai/v1"
        };

        // User enters a brand new future model not known in the codebase
        string futureModelId = "meta-llama/llama-4-scintilla-400b";
        vm.SelectedModelId = futureModelId;
        vm.SaveCustomModel(futureModelId);

        // Model is immediately remembered in history
        Assert.Contains(futureModelId, vm.CustomModelHistory);

        // Model is in AvailableModels catalog
        Assert.Contains(vm.AvailableModels, m => m.Id == futureModelId);

        // ActiveModelInfo reflects the future model
        Assert.Equal(futureModelId, vm.ActiveModelInfo.Id);

        // Future model is persisted in settings storage
        Assert.Contains(futureModelId, uiSettings.Settings.AiSettings.CustomModelHistory);
    }

    [Fact]
    public async Task ModifyElementAsync_Chart_ModifiesChartTypeAndPalette()
    {
        var aiService = new AiService();
        var undoRedo = new UndoRedoService();
        var agent = new PdfStudioAgentService(aiService, undoRedo);

        var chartVm = new PdfEditorApp.ViewModels.ElementViewModels.ChartElementViewModel
        {
            Title = "FY 2026 Quarterly Revenue Trajectory ($ Billions)",
            ChartType = ChartType.BarColumn,
            Palette = ChartPalette.CorporateBlue
        };
        chartVm.Bars.Add(new PdfEditorApp.ViewModels.ElementViewModels.ChartBarItem { Category = "Q1", Value = 0.65, ValueLabel = "$0.65B" });
        chartVm.Bars.Add(new PdfEditorApp.ViewModels.ElementViewModels.ChartBarItem { Category = "Q2", Value = 0.74, ValueLabel = "$0.74B" });
        chartVm.Bars.Add(new PdfEditorApp.ViewModels.ElementViewModels.ChartBarItem { Category = "Q3", Value = 0.82, ValueLabel = "$0.82B" });
        chartVm.Bars.Add(new PdfEditorApp.ViewModels.ElementViewModels.ChartBarItem { Category = "Q4", Value = 0.89, ValueLabel = "$0.89B" });

        var settings = new AiSettingsModel();

        // Prompt requesting Line chart, emerald palette, and 2027 projection
        var result = await agent.ModifyElementAsync(
            chartVm,
            "Change to a smooth Line chart with emerald green palette and add 2027 projection",
            settings);

        Assert.True(result.Success);
        Assert.Equal(ChartType.SmoothLine, chartVm.ChartType);
        Assert.Equal(ChartPalette.EmeraldGreen, chartVm.Palette);
        Assert.True(chartVm.Bars.Count >= 5);
        Assert.Contains(chartVm.Bars, b => b.Category.Contains("2027"));
    }

    [Fact]
    public async Task ModifyElementAsync_Text_ModifiesTypographyAndContent()
    {
        var aiService = new AiService();
        var undoRedo = new UndoRedoService();
        var agent = new PdfStudioAgentService(aiService, undoRedo);

        var textVm = new PdfEditorApp.ViewModels.ElementViewModels.TextElementViewModel
        {
            Text = "Net profit margins expanded by 4.2% year-over-year.",
            FontSize = 12,
            IsBold = false,
            TextColorHex = "#201F1E"
        };

        var settings = new AiSettingsModel();

        var result = await agent.ModifyElementAsync(
            textVm,
            "Make bold, 18pt, blue color, center aligned, and executive tone",
            settings);

        Assert.True(result.Success);
        Assert.True(textVm.IsBold);
        Assert.Equal(18, textVm.FontSize);
        Assert.Equal("#0F6CBD", textVm.TextColorHex);
        Assert.Equal(TextAlignmentMode.Center, textVm.Alignment);
        Assert.Contains("Executive Summary:", textVm.Text);
    }

    [Fact]
    public async Task ModifyElementAsync_Table_AddsSummaryRow()
    {
        var aiService = new AiService();
        var undoRedo = new UndoRedoService();
        var agent = new PdfStudioAgentService(aiService, undoRedo);

        var tableVm = new PdfEditorApp.ViewModels.ElementViewModels.TableElementViewModel();
        tableVm.Headers.Clear();
        tableVm.Headers.Add(new PdfEditorApp.ViewModels.ElementViewModels.TableHeaderItem("Department"));
        tableVm.Headers.Add(new PdfEditorApp.ViewModels.ElementViewModels.TableHeaderItem("Headcount"));
        tableVm.Headers.Add(new PdfEditorApp.ViewModels.ElementViewModels.TableHeaderItem("Budget"));

        tableVm.Rows.Clear();
        tableVm.Rows.Add(new PdfEditorApp.ViewModels.ElementViewModels.TableRowItem(new[] { "Engineering", "45", "$450,000" }));
        tableVm.Rows.Add(new PdfEditorApp.ViewModels.ElementViewModels.TableRowItem(new[] { "Product", "12", "$120,000" }));

        var settings = new AiSettingsModel();

        var result = await agent.ModifyElementAsync(
            tableVm,
            "Add a total row and use emerald green header",
            settings);

        Assert.True(result.Success);
        Assert.Equal(3, tableVm.Rows.Count);
        Assert.Equal("Total", tableVm.Rows[2].Cells[0].Text);
        Assert.Equal("#047857", tableVm.HeaderBackgroundHex);
    }

    [Fact]
    public async Task ModifyElementAsync_UndoRedo_RestoresOriginalState()
    {
        var aiService = new AiService();
        var undoRedo = new UndoRedoService();
        var agent = new PdfStudioAgentService(aiService, undoRedo);

        var chartVm = new PdfEditorApp.ViewModels.ElementViewModels.ChartElementViewModel
        {
            Title = "Original Bar Chart",
            ChartType = ChartType.BarColumn,
            Palette = ChartPalette.CorporateBlue
        };

        var settings = new AiSettingsModel();

        var result = await agent.ModifyElementAsync(
            chartVm,
            "Switch to Line chart with sunset orange palette",
            settings);

        Assert.True(result.Success);
        Assert.Equal(ChartType.Line, chartVm.ChartType);
        Assert.Equal(ChartPalette.SunsetOrange, chartVm.Palette);
        Assert.True(undoRedo.CanUndo);

        // Perform Undo
        undoRedo.Undo();

        // State is completely restored to original
        Assert.Equal("Original Bar Chart", chartVm.Title);
        Assert.Equal(ChartType.BarColumn, chartVm.ChartType);
        Assert.Equal(ChartPalette.CorporateBlue, chartVm.Palette);

        // Perform Redo
        Assert.True(undoRedo.CanRedo);
        undoRedo.Redo();

        Assert.Equal(ChartType.Line, chartVm.ChartType);
        Assert.Equal(ChartPalette.SunsetOrange, chartVm.Palette);
    }

    [Fact]
    public void AiAssistantViewModel_OpenForElement_SwitchesToModifyModeAndPopulatesElementSuggestions()
    {
        var aiService = new AiService();
        var undoRedo = new UndoRedoService();
        var agent = new PdfStudioAgentService(aiService, undoRedo);
        var uiSettings = new UiSettingsService(_tempSettingsPath);

        var vm = new AiAssistantViewModel(agent, uiSettings, aiService);

        var chartVm = new PdfEditorApp.ViewModels.ElementViewModels.ChartElementViewModel
        {
            Title = "FY 2026 Revenue Trajectory",
            ChartType = ChartType.BarColumn
        };

        // Open targeting chart
        vm.OpenForElement(chartVm);

        Assert.True(vm.IsOpen);
        Assert.True(vm.IsModifyMode);
        Assert.True(vm.HasTargetElement);
        Assert.Same(chartVm, vm.TargetElement);
        Assert.Equal("FY 2026 Revenue Trajectory", vm.TargetElementTitle);
        Assert.Equal("Chart", vm.TargetElementKindBadge);

        // Verify contextual suggestions were generated for chart
        Assert.NotEmpty(vm.SuggestedPrompts);
        Assert.Contains(vm.SuggestedPrompts, p => p.Contains("Line chart", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(vm.SuggestedPrompts, p => p.Contains("Emerald Green", StringComparison.OrdinalIgnoreCase));

        // Switch to create mode
        vm.SwitchToCreateMode();
        Assert.False(vm.IsModifyMode);
        Assert.Contains(vm.SuggestedPrompts, p => p.Contains("invoice header", StringComparison.OrdinalIgnoreCase));

        // Switch back to modify mode
        vm.SwitchToModifyMode();
        Assert.True(vm.IsModifyMode);
        Assert.Contains(vm.SuggestedPrompts, p => p.Contains("Line chart", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MainViewModel_AskAiToModify_OpensAiAssistantForSelectedElement()
    {
        var mainVm = new MainViewModel();
        Assert.NotNull(mainVm.CurrentPage);

        var chartVm = new PdfEditorApp.ViewModels.ElementViewModels.ChartElementViewModel
        {
            Title = "FY 2026 Quarterly Revenue Trajectory ($ Billions)"
        };

        // Select the chart on the current page
        mainVm.CurrentPage.AddElement(chartVm);
        mainVm.CurrentPage.SelectElement(chartVm);

        // Execute AskAiToModifyCommand
        mainVm.AskAiToModifyCommand.Execute(null);

        // Assert AiAssistant opened in modify mode targeting the chart
        Assert.True(mainVm.AiAssistant.IsOpen);
        Assert.True(mainVm.AiAssistant.IsModifyMode);
        Assert.Same(chartVm, mainVm.AiAssistant.TargetElement);
        Assert.Equal("FY 2026 Quarterly Revenue Trajectory ($ Billions)", mainVm.AiAssistant.TargetElementTitle);
    }

    [Fact]
    public async Task PdfStudioAgentService_ModifyTextElement_WithBulletsOrCheckmarks_TransformsContentCorrectly()
    {
        var aiService = new AiService();
        var undoRedo = new UndoRedoService();
        var agent = new PdfStudioAgentService(aiService, undoRedo);

        var textVm = new TextElementViewModel
        {
            Text = "Acquisition of TechNova AI\nEnterprise Customer Expansion\nMultiplatform Engine Upgrade",
            FontSize = 14,
            IsBold = false,
            TextColorHex = "#201F1E"
        };

        var settings = new AiSettingsModel();
        var result = await agent.ModifyElementAsync(textVm, "add checkmarks and make bold in emerald green", settings);

        Assert.True(result.Success);
        Assert.Contains("✔", textVm.Text);
        Assert.True(textVm.IsBold);
        Assert.Equal("#16A34A", textVm.TextColorHex);
        Assert.True(undoRedo.CanUndo);

        // Verify Undo restores original text and styling
        undoRedo.Undo();
        Assert.False(textVm.IsBold);
        Assert.DoesNotContain("✔", textVm.Text);
        Assert.Equal("#201F1E", textVm.TextColorHex);

        // Verify Redo reapplies modifications
        undoRedo.Redo();
        Assert.Contains("✔", textVm.Text);
        Assert.True(textVm.IsBold);
        Assert.Equal("#16A34A", textVm.TextColorHex);
    }

    [Fact]
    public async Task PdfStudioAgentService_ModifyMathFormula_TransformsLatexAndSize()
    {
        var aiService = new AiService();
        var undoRedo = new UndoRedoService();
        var agent = new PdfStudioAgentService(aiService, undoRedo);
        var settings = new AiSettingsModel();

        var mathVm = new MathElementViewModel
        {
            Formula = "x + y = z",
            FontSize = 16,
            TextColorHex = "#333333"
        };

        var result = await agent.ModifyElementAsync(mathVm, "change to pythagorean theorem with font size 24 and navy text", settings);

        Assert.True(result.Success);
        Assert.Equal("a^2 + b^2 = c^2", mathVm.Formula);
        Assert.Equal(24, mathVm.FontSize);
        Assert.Equal("#0F172A", mathVm.TextColorHex);

        // Test Quadratic formula prompt
        var result2 = await agent.ModifyElementAsync(mathVm, "change to quadratic formula with emerald text", settings);
        Assert.True(result2.Success);
        Assert.Contains(@"\frac{-b \pm \sqrt{b^2 - 4ac}}{2a}", mathVm.Formula);
        Assert.Equal("#059669", mathVm.TextColorHex);
    }

    [Fact]
    public async Task PdfStudioAgentService_ModifyImageAndQrCodeElements_UpdatesProperties()
    {
        var aiService = new AiService();
        var undoRedo = new UndoRedoService();
        var agent = new PdfStudioAgentService(aiService, undoRedo);
        var settings = new AiSettingsModel();

        // 1. Image Element
        var imgVm = new ImageElementViewModel
        {
            Opacity = 1.0,
            CornerRadius = 0
        };

        var imgResult = await agent.ModifyElementAsync(imgVm, "set opacity to 50% and rounded corners", settings);
        Assert.True(imgResult.Success);
        Assert.Equal(0.5, imgVm.Opacity, 2);
        Assert.Equal(8, imgVm.CornerRadius);

        // 2. QR Code Element
        var qrVm = new QrCodeElementViewModel
        {
            Content = "https://example.org/old",
            DarkColorHex = "#000000"
        };

        var qrResult = await agent.ModifyElementAsync(qrVm, "change url to https://frypdf.com/verify and dark color navy", settings);
        Assert.True(qrResult.Success);
        Assert.Equal("https://frypdf.com/verify", qrVm.Content);
        Assert.Equal("#1E293B", qrVm.DarkColorHex);
    }

    [Fact]
    public void AiAssistantViewModel_OpenForDifferentElementTypes_ConfiguresIconTitleAndSuggestions()
    {
        var aiService = new AiService();
        var undoRedo = new UndoRedoService();
        var agent = new PdfStudioAgentService(aiService, undoRedo);
        var uiSettings = new UiSettingsService(_tempSettingsPath);
        var vm = new AiAssistantViewModel(agent, uiSettings, aiService);

        // Text element
        var textVm = new TextElementViewModel { Text = "Acquisition of TechNova AI" };
        vm.OpenForElement(textVm);
        Assert.True(vm.IsOpen);
        Assert.Equal("Text", vm.TargetElementKindBadge);
        Assert.Equal("Acquisition of TechNova AI", vm.TargetElementTitle);
        Assert.Contains(vm.SuggestedPrompts, p => p.Contains("checkmark", StringComparison.OrdinalIgnoreCase) || p.Contains("check", StringComparison.OrdinalIgnoreCase));

        // Math element
        var mathVm = new MathElementViewModel { Formula = "x^2 + y^2 = r^2", PresetName = "Circle Equation" };
        vm.OpenForElement(mathVm);
        Assert.Equal("Math", vm.TargetElementKindBadge);
        Assert.Contains("Circle", vm.TargetElementTitle);
        Assert.Contains(vm.SuggestedPrompts, p => p.Contains("Quadratic", StringComparison.OrdinalIgnoreCase));

        // QR Code element
        var qrVm = new QrCodeElementViewModel { Content = "https://frypdf.com/verify" };
        vm.OpenForElement(qrVm);
        Assert.Equal("QrCode", vm.TargetElementKindBadge);
        Assert.Contains("https://frypdf.com/verify", vm.TargetElementTitle);
        Assert.Contains(vm.SuggestedPrompts, p => p.Contains("URL", StringComparison.OrdinalIgnoreCase));

        // Form Field element
        var formVm = new FormFieldElementViewModel { Label = "Authorized Signatory Name" };
        vm.OpenForElement(formVm);
        Assert.Equal("FormField", vm.TargetElementKindBadge);
        Assert.Equal("Field: Authorized Signatory Name", vm.TargetElementTitle);
        Assert.Contains(vm.SuggestedPrompts, p => p.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AskAiButton_InstantiatesWithDefaults_AndSupportsVariants()
    {
        var btn = new PdfEditorApp.Views.Controls.AskAiButton();

        Assert.Equal(PdfEditorApp.Views.Controls.AskAiButtonVariant.FloatingPill, btn.Variant);
        Assert.Equal("Ask AI", btn.ButtonText);
        Assert.Equal("Ask AI to Modify (Ctrl+I / ⌘I)", btn.ToolTipText);
        Assert.Equal(22, btn.Height);
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Right, btn.HorizontalAlignment);
        Assert.Equal(Avalonia.Layout.VerticalAlignment.Top, btn.VerticalAlignment);

        // Switch to subtle variant
        btn.Variant = PdfEditorApp.Views.Controls.AskAiButtonVariant.Subtle;
        Assert.Equal(PdfEditorApp.Views.Controls.AskAiButtonVariant.Subtle, btn.Variant);
        Assert.Equal(24, btn.Height);
        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Left, btn.HorizontalAlignment);
    }
}
