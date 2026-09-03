using PdfEditorApp.Services.Tools.Core;
using PdfEditorApp.Services.Tools.Organize;
using PdfEditorApp.Services.Tools.Security;
using PdfEditorApp.Services.Tools.Conversion;
using PdfEditorApp.Services.Tools.Intelligence;
using PdfEditorApp.ViewModels.Tools.Core;
using PdfEditorApp.ViewModels.Tools.Organize;
using PdfEditorApp.ViewModels.Tools.Security;
using PdfEditorApp.ViewModels.Tools.Conversion;
using PdfEditorApp.ViewModels.Tools.Intelligence;
using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using PdfEditorApp.Views;

namespace PdfEditorApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        Name = "FryPDF";
        LiveChartsCore.LiveCharts.Configure(config => LiveChartsCore.SkiaSharpView.LiveChartsSkiaSharp.UseDefaults(config));
        AvaloniaXamlLoader.Load(this);
    }

    private void AboutMenuItem_OnClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.DataContext is MainViewModel mainVm)
        {
            mainVm.OpenAboutDialogCommand.Execute(null);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        var themeService = Services.GetRequiredService<IThemeService>();
        themeService.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IPdfExportService, PdfExportService>();
        services.AddSingleton<IPdfImportService, PdfImportService>();
        services.AddSingleton<IProjectPersistenceService, ProjectPersistenceService>();
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<IDocumentAuditService, DocumentAuditService>();
        services.AddSingleton<IDocumentCompareService, DocumentCompareService>();
        services.AddSingleton<ISignatureService, SignatureService>();
        services.AddSingleton<ISmartPlacementService, SmartPlacementService>();
        services.AddSingleton<IRecentDocumentsService, RecentDocumentsService>();
        services.AddSingleton<IPageOrganizerService, PageOrganizerService>();
        services.AddSingleton<IHelpGuideService, HelpGuideService>();
        services.AddTransient<IUndoRedoService, UndoRedoService>();

        // PDF Tools Platform Services
        services.AddSingleton<IQuestPdfOperationsEngine, QuestPdfOperationsEngine>();
        services.AddSingleton<IPdfToolRegistry, PdfToolRegistry>();
        services.AddSingleton<IPdfPageService, PdfPageService>();
        services.AddSingleton<IPdfOptimizationService, PdfOptimizationService>();
        services.AddSingleton<IPdfSecurityService, PdfSecurityService>();
        services.AddSingleton<IPdfConversionService, PdfConversionService>();
        services.AddSingleton<IPdfOcrService, PdfOcrService>();
        services.AddSingleton<IPdfFormService, PdfFormService>();
        services.AddSingleton<IAiDocumentService, AiDocumentService>();
        services.AddSingleton<IDocumentTranslationService, DocumentTranslationService>();
        services.AddSingleton<IPdfWorkflowEngine, PdfWorkflowEngine>();
        services.AddSingleton<IPdfDocumentOperationsService, PdfDocumentOperationsService>();
        services.AddSingleton<IPdfToolViewModelFactory>(sp =>
            new PdfToolViewModelFactory(
                sp.GetRequiredService<IPdfDocumentOperationsService>(),
                sp.GetRequiredService<IPdfToolRegistry>(),
                sp));

        services.AddSingleton<PdfEditorApp.Core.Data.IDataSourceService, PdfEditorApp.Core.Data.DataSourceService>();
        services.AddSingleton<PdfEditorApp.Core.Data.IDataBindingService, PdfEditorApp.Core.Data.DataBindingService>();
        services.AddSingleton<PdfEditorApp.Core.Data.IDataMergeEngine, PdfEditorApp.Core.Data.DataMergeEngine>();
        services.AddSingleton<PdfEditorApp.Core.Data.IBatchPdfGenerator, PdfEditorApp.Services.BatchPdfGeneratorService>();

        // ViewModels
        services.AddTransient<PdfEditorApp.ViewModels.DataStudio.DataStudioViewModel>();
        services.AddTransient<PdfEditorApp.ViewModels.BatchGeneration.BatchGenerationViewModel>();
        services.AddTransient<InspectorViewModel>();
        services.AddTransient<PdfViewerViewModel>();
        services.AddTransient<PdfToolRunnerViewModel>();
        services.AddTransient<WorkflowBuilderViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<MainViewModel>();

        // Individual Tool ViewModels (resolvable directly or via IPdfToolViewModelFactory)
        services.AddTransient<MergePdfToolViewModel>();
        services.AddTransient<SplitPdfToolViewModel>();
        services.AddTransient<RotatePdfToolViewModel>();
        services.AddTransient<OrganizePdfToolViewModel>();
        services.AddTransient<CropPdfToolViewModel>();
        services.AddTransient<PageNumbersToolViewModel>();

        services.AddTransient<CompressPdfToolViewModel>();
        services.AddTransient<RepairPdfToolViewModel>();
        services.AddTransient<ProtectPdfToolViewModel>();
        services.AddTransient<UnlockPdfToolViewModel>();
        services.AddTransient<SignPdfToolViewModel>();
        services.AddTransient<RedactPdfToolViewModel>();
        services.AddTransient<WatermarkToolViewModel>();

        services.AddTransient<PdfToWordToolViewModel>();
        services.AddTransient<WordToPdfToolViewModel>();
        services.AddTransient<PdfToExcelToolViewModel>();
        services.AddTransient<ExcelToPdfToolViewModel>();
        services.AddTransient<PdfToPowerPointToolViewModel>();
        services.AddTransient<PowerPointToPdfToolViewModel>();
        services.AddTransient<PdfToJpgToolViewModel>();
        services.AddTransient<JpgToPdfToolViewModel>();
        services.AddTransient<HtmlToPdfToolViewModel>();
        services.AddTransient<ScanToPdfToolViewModel>();
        services.AddTransient<PdfToMarkdownToolViewModel>();
        services.AddTransient<PdfToPdfAToolViewModel>();

        services.AddTransient<OcrPdfToolViewModel>();
        services.AddTransient<ComparePdfToolViewModel>();
        services.AddTransient<EditPdfToolViewModel>();
        services.AddTransient<PdfFormsToolViewModel>();
        services.AddTransient<AiSummarizerToolViewModel>();
        services.AddTransient<TranslatePdfToolViewModel>();
    }
}