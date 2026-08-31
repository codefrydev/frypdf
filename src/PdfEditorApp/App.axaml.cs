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
        services.AddSingleton<PdfEditorApp.Services.Tools.IPdfToolRegistry, PdfEditorApp.Services.Tools.PdfToolRegistry>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IPdfPageService, PdfEditorApp.Services.Tools.PdfPageService>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IPdfOptimizationService, PdfEditorApp.Services.Tools.PdfOptimizationService>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IPdfSecurityService, PdfEditorApp.Services.Tools.PdfSecurityService>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IPdfConversionService, PdfEditorApp.Services.Tools.PdfConversionService>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IPdfOcrService, PdfEditorApp.Services.Tools.PdfOcrService>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IPdfFormService, PdfEditorApp.Services.Tools.PdfFormService>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IAiDocumentService, PdfEditorApp.Services.Tools.AiDocumentService>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IDocumentTranslationService, PdfEditorApp.Services.Tools.DocumentTranslationService>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IPdfWorkflowEngine, PdfEditorApp.Services.Tools.PdfWorkflowEngine>();
        services.AddSingleton<IPdfDocumentOperationsService, PdfDocumentOperationsService>();
        services.AddSingleton<PdfEditorApp.Services.Tools.IPdfToolViewModelFactory, PdfEditorApp.Services.Tools.PdfToolViewModelFactory>();

        services.AddSingleton<PdfEditorApp.Core.Data.IDataSourceService, PdfEditorApp.Core.Data.DataSourceService>();
        services.AddSingleton<PdfEditorApp.Core.Data.IDataBindingService, PdfEditorApp.Core.Data.DataBindingService>();
        services.AddSingleton<PdfEditorApp.Core.Data.IDataMergeEngine, PdfEditorApp.Core.Data.DataMergeEngine>();
        services.AddSingleton<PdfEditorApp.Core.Data.IBatchPdfGenerator, PdfEditorApp.Services.BatchPdfGeneratorService>();

        // ViewModels
        services.AddTransient<PdfEditorApp.ViewModels.DataStudio.DataStudioViewModel>();
        services.AddTransient<PdfEditorApp.ViewModels.BatchGeneration.BatchGenerationViewModel>();
        services.AddTransient<InspectorViewModel>();
        services.AddTransient<PdfToolRunnerViewModel>();
        services.AddTransient<WorkflowBuilderViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<MainViewModel>();
    }
}