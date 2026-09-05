using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.ViewModels;
using PdfEditorApp.Views;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing all workspace pages and sidebar navigation sections.
/// </summary>
public class WorkspacePagesBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.WorkspacePages";
    public string Name => "Workspace Pages & Navigation Bundle";
    public string Description => "Modular workspace pages for the studio: Dashboard, PDF Reader, Templates Gallery, Tools Studios, OCR Models, Fonts, and Preferences.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new HomeDashboardPagePlugin(),
        new PdfReaderLandingPagePlugin(),
        new NewDocumentPagePlugin(),
        new PdfToolsStudioPagePlugin(),
        new ToolCategoryPagesPlugin(),
        new StarredToolsPagePlugin(),
        new FontManagerPagePlugin(),
        new TesseractPagePlugin(),
        new TrashCachePagePlugin(),
        new HelpGuidePagePlugin(),
        new LicensingPagePlugin(),
        new PluginsPagePlugin(),
        new SettingsPagePlugin()
    };
}

public class HomeDashboardPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.dashboard";
    public string Name => "Overview Dashboard Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "Home",
            Title = "Home",
            Group = "Overview",
            IconKind = "HomeOutline",
            Order = 10,
            ViewFactory = null // Built-in view mounted in HomeView.axaml for 0ms instantaneous navigation
        });
        return Task.CompletedTask;
    }
}

public class PdfReaderLandingPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.reader";
    public string Name => "PDF Reader Landing Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "PdfReader",
            Title = "PDF Reader",
            Group = "Overview",
            IconKind = "BookOpenPageVariantOutline",
            BadgeText = "Read",
            BadgeColorHex = "#DC2626",
            Order = 20,
            ViewFactory = null // Built-in view mounted in HomeView.axaml for 0ms instantaneous navigation
        });
        return Task.CompletedTask;
    }
}

public class NewDocumentPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.newdocument";
    public string Name => "New Document & Template Gallery Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "NewDocument",
            Title = "New Document",
            Group = "Overview",
            IconKind = "FileDocumentPlusOutline",
            BadgeText = "19",
            BadgeColorHex = "#059669",
            Order = 30,
            ViewFactory = null // Built-in view mounted in HomeView.axaml for 0ms instantaneous navigation
        });
        return Task.CompletedTask;
    }
}

public class PdfToolsStudioPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.tools";
    public string Name => "PDF Tools Studio Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "AllTools",
            Title = "All Tools",
            Group = "Overview",
            IconKind = "ViewGridOutline",
            BadgeText = "32",
            BadgeColorHex = "#4F46E5",
            Order = 40,
            ViewFactory = null // Built-in view mounted in HomeView.axaml for 0ms instantaneous navigation
        });
        return Task.CompletedTask;
    }
}

public class ToolCategoryPagesPlugin : IFryPlugin
{
    public string Id => "frypdf.page.toolcategories";
    public string Name => "Tool Categories Pages";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "OrganizeAndPage",
            Title = "Organize & Page",
            Group = "Categories",
            IconKind = "BookOpenPageVariantOutline",
            BadgeText = "6",
            Order = 50,
            ViewFactory = null
        });

        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "OptimizeAndSecurity",
            Title = "Security & Optimize",
            Group = "Categories",
            IconKind = "ShieldLockOutline",
            BadgeText = "7",
            Order = 60,
            ViewFactory = null
        });

        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "ConvertFromPdf",
            Title = "Convert from PDF",
            Group = "Categories",
            IconKind = "ExportVariant",
            BadgeText = "5",
            Order = 70,
            ViewFactory = null
        });

        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "ConvertToPdf",
            Title = "Convert to PDF",
            Group = "Categories",
            IconKind = "Import",
            BadgeText = "6",
            Order = 80,
            ViewFactory = null
        });

        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "EditAndForms",
            Title = "Edit & Forms",
            Group = "Categories",
            IconKind = "Draw",
            BadgeText = "4",
            Order = 90,
            ViewFactory = null
        });

        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "AiAndAutomation",
            Title = "AI & Automation",
            Group = "Categories",
            IconKind = "AutoFix",
            BadgeText = "4",
            Order = 100,
            ViewFactory = null
        });

        return Task.CompletedTask;
    }
}

public class StarredToolsPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.starred";
    public string Name => "Starred Tools Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "Starred",
            Title = "Starred Tools",
            Group = "Library",
            IconKind = "StarOutline",
            Order = 110,
            ViewFactory = null
        });
        return Task.CompletedTask;
    }
}

public class FontManagerPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.fonts";
    public string Name => "Language & Fonts Studio Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "FontPackages",
            Title = "Language & Fonts",
            Group = "Library",
            IconKind = "Translate",
            BadgeText = "10",
            BadgeColorHex = "#0284C7",
            Order = 120,
            ViewFactory = null
        });
        return Task.CompletedTask;
    }
}

public class TesseractPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.tesseract";
    public string Name => "OCR Models Manager Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "TesseractData",
            Title = "OCR Models",
            Group = "Library",
            IconKind = "TextRecognition",
            Order = 130,
            ViewFactory = null
        });
        return Task.CompletedTask;
    }
}

public class TrashCachePagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.trash";
    public string Name => "Trash & Cache Cleaner Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "Trash",
            Title = "Trash & Cache",
            Group = "Library",
            IconKind = "TrashCanOutline",
            Order = 140,
            ViewFactory = null
        });
        return Task.CompletedTask;
    }
}

public class HelpGuidePagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.help";
    public string Name => "Help & Guides Studio Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "Help",
            Title = "Help & Guides",
            Group = "Library",
            IconKind = "HelpCircleOutline",
            BadgeText = "Guides",
            BadgeColorHex = "#0284C7",
            Order = 150,
            ViewFactory = null
        });
        return Task.CompletedTask;
    }
}

public class LicensingPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.licensing";
    public string Name => "Licenses & Tools Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "Licensing",
            Title = "Licenses & Tools",
            Group = "Library",
            IconKind = "CertificateOutline",
            BadgeText = "12",
            BadgeColorHex = "#4F46E5",
            Order = 160,
            ViewFactory = null
        });
        return Task.CompletedTask;
    }
}

public class SettingsPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.settings";
    public string Name => "Settings & UI Preferences Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "Settings",
            Title = "Settings & UI",
            Group = "Preferences",
            IconKind = "TuneVariant",
            BadgeText = "UI",
            BadgeColorHex = "#7C3AED",
            Order = 170,
            ViewFactory = null
        });
        return Task.CompletedTask;
    }
}

public class PluginsPagePlugin : IFryPlugin
{
    public string Id => "frypdf.page.plugins";
    public string Name => "Plugins & Extensions Studio Page";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterNavigationItem(new NavigationItemDescriptor
        {
            Id = "Plugins",
            Title = "Plugins & Extensions",
            Group = "Preferences",
            IconKind = "PuzzleOutline",
            BadgeText = "Store",
            BadgeColorHex = "#7C3AED",
            Order = 165,
            ViewFactory = sp => new PluginsManagerPageView
            {
                DataContext = (sp?.GetService(typeof(HomeViewModel)) as HomeViewModel)?.PluginsManager
                    ?? sp?.GetService(typeof(PluginsManagerViewModel)) as PluginsManagerViewModel
                    ?? new PluginsManagerViewModel(
                        sp?.GetService(typeof(PluginHost)) as PluginHost,
                        sp?.GetService(typeof(Core.Plugins.Marketplace.IPluginMarketplaceService)) as Core.Plugins.Marketplace.IPluginMarketplaceService)
            }
        });
        return Task.CompletedTask;
    }
}
