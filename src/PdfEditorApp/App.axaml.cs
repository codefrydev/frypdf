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
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

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
        services.AddSingleton<IPdfExportService, PdfExportService>();
        services.AddSingleton<IProjectPersistenceService, ProjectPersistenceService>();
        services.AddSingleton<ITemplateService, TemplateService>();
        services.AddSingleton<IDocumentAuditService, DocumentAuditService>();
        services.AddSingleton<IDocumentCompareService, DocumentCompareService>();
        services.AddSingleton<ISignatureService, SignatureService>();
        services.AddSingleton<ISmartPlacementService, SmartPlacementService>();
        services.AddSingleton<IRecentDocumentsService, RecentDocumentsService>();
        services.AddSingleton<IPageOrganizerService, PageOrganizerService>();
        services.AddTransient<IUndoRedoService, UndoRedoService>();

        // ViewModels
        services.AddTransient<InspectorViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<MainViewModel>();
    }
}