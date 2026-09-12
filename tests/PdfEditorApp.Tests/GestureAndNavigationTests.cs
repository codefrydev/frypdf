using System;
using System.IO;
using System.Linq;
using Avalonia.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.Tests.Mocks;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

// Shares AppLogService.Instance's buffer with AppLogServiceTests — same collection to avoid races.
[Collection("AppLogService")]
public class GestureAndNavigationTests
{
    [Fact]
    public void TrackpadMagnifyGesture_ComputesProportionalScale()
    {
        double oldZoom = 1.0;

        // Positive delta (pinch out / zoom in by 10%)
        double deltaPositive = 0.10;
        double zoomIn = Math.Clamp(Math.Round(oldZoom * (1.0 + deltaPositive), 3), 0.1, 5.0);
        Assert.Equal(1.10, zoomIn);

        // Negative delta (pinch in / zoom out by 10%)
        double deltaNegative = -0.10;
        double zoomOut = Math.Clamp(Math.Round(oldZoom * (1.0 + deltaNegative), 3), 0.1, 5.0);
        Assert.Equal(0.90, zoomOut);
    }
    [Fact]
    public void PinchZoomMath_PreventsExponentialExplosion_AndClampsProperly()
    {
        // Simulate pinch scaling logic: initial zoom is 1.0 (100%)
        double startZoom = 1.0;

        // User pinches out by 25% (scale = 1.25)
        double scale1 = 1.25;
        double calculatedZoom1 = Math.Clamp(Math.Round(startZoom * scale1, 3), 0.1, 5.0);
        Assert.Equal(1.25, calculatedZoom1);

        // Continuous pinch event 2 in the same gesture (scale = 1.50 relative to start)
        double scale2 = 1.50;
        double calculatedZoom2 = Math.Clamp(Math.Round(startZoom * scale2, 3), 0.1, 5.0);
        Assert.Equal(1.50, calculatedZoom2);

        // Extreme pinch out clamped to max zoom (5.0 / 500%)
        double scaleExtreme = 10.0;
        double calculatedZoomMax = Math.Clamp(Math.Round(startZoom * scaleExtreme, 3), 0.1, 5.0);
        Assert.Equal(5.0, calculatedZoomMax);

        // Extreme pinch in clamped to min zoom (0.1 / 10%)
        double scaleMin = 0.01;
        double calculatedZoomMin = Math.Clamp(Math.Round(startZoom * scaleMin, 3), 0.1, 5.0);
        Assert.Equal(0.1, calculatedZoomMin);
    }

    [Fact]
    public void TrackpadContinuousZoomDelta_ComputesSmoothProportionalScaling()
    {
        double oldZoom = 1.0;

        // Small trackpad delta (e.g. 0.05)
        double deltaY = 0.05;
        double zoomDeltaFactor = Math.Pow(1.002, deltaY * 100);
        double newZoom = Math.Clamp(Math.Round(oldZoom * zoomDeltaFactor, 3), 0.1, 5.0);

        // Should be approximately 1.01 (1% smooth increase)
        Assert.True(newZoom > 1.0 && newZoom < 1.05);

        // Discrete mouse wheel tick (deltaY = 1.0)
        double wheelFactor = 1.15;
        double wheelZoom = Math.Clamp(Math.Round(oldZoom * wheelFactor, 3), 0.1, 5.0);
        Assert.Equal(1.15, wheelZoom);
    }

    [Fact]
    public void MainViewModel_ZoomCommands_OperateWithinValidRanges()
    {
        var mainVm = new MainViewModel();
        mainVm.ZoomLevel = 1.0;

        // Zoom In
        mainVm.ZoomIn();
        Assert.True(mainVm.ZoomLevel > 1.0);

        // Reset Zoom
        mainVm.ResetZoom();
        Assert.Equal(1.0, mainVm.ZoomLevel);

        // Zoom Out
        mainVm.ZoomOut();
        Assert.True(mainVm.ZoomLevel < 1.0);
    }

    [Fact]
    public void PdfViewerViewModel_ZoomAndJumpPageGestures_ValidateCorrectly()
    {
        var viewerVm = new PdfViewerViewModel();
        viewerVm.ZoomLevel = 1.0;

        // Zoom In
        viewerVm.ZoomIn();
        Assert.Equal(1.25, viewerVm.ZoomLevel);

        // Zoom Out
        viewerVm.ZoomOut();
        Assert.Equal(1.0, viewerVm.ZoomLevel);

        // Reset Zoom
        viewerVm.ResetZoom();
        Assert.Equal(1.0, viewerVm.ZoomLevel);

        // Fit Presets
        viewerVm.FitToWidth();
        Assert.Equal(1.35, viewerVm.ZoomLevel);

        viewerVm.FitToPage();
        Assert.Equal(0.95, viewerVm.ZoomLevel);

        // Percentage Presets
        viewerVm.SetZoomPreset("200%");
        Assert.Equal(2.0, viewerVm.ZoomLevel);

        viewerVm.SetZoomPreset("50%");
        Assert.Equal(0.5, viewerVm.ZoomLevel);
    }

    [Fact]
    public void MainViewModel_WindowTitle_ReflectsActiveViewAndContextCorrectly()
    {
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Reset();
        // 1. Initial State: App opens on Home Dashboard
        var mainVm = new MainViewModel(
            new PdfExportService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            recentService: new MockRecentDocumentsService());
        mainVm.NavigateToHome();
        Assert.True(mainVm.IsHomePageVisible);
        Assert.False(mainVm.IsEditorVisible);
        Assert.False(mainVm.IsPdfViewerVisible);
        Assert.Equal("FryPDF - Privacy-First PDF Studio", mainVm.WindowTitle);

        // 2. Open a tool in Home page (e.g. Merge PDF)
        mainVm.OpenTool(PdfToolId.MergePdf);
        Assert.True(mainVm.IsHomePageVisible);
        Assert.True(mainVm.Home.IsToolPageActive);
        Assert.Contains("Merge", mainVm.WindowTitle);
        Assert.EndsWith("FryPDF", mainVm.WindowTitle);

        // 3. Navigate to Licensing section
        mainVm.Home.BackToTools();
        mainVm.Home.SelectNavSectionCommand.Execute("Licensing");
        Assert.Equal("Licenses & Third-Party Tools - FryPDF", mainVm.WindowTitle);

        // 4. Open an Editor template document
        mainVm.OpenEditorWithTemplate("Invoice");
        Assert.False(mainVm.IsHomePageVisible);
        Assert.True(mainVm.IsEditorVisible);
        Assert.False(mainVm.IsPdfViewerVisible);
        Assert.Contains("FryPDF", mainVm.WindowTitle);

        // 5. Navigate back to Home
        mainVm.NavigateToHome();
        Assert.True(mainVm.IsHomePageVisible);
        Assert.False(mainVm.IsEditorVisible);
        Assert.Equal("FryPDF - Privacy-First PDF Studio", mainVm.WindowTitle);

        // 6. Open in PDF Viewer Mode
        mainVm.PdfViewer.DocumentTitle = "Quarterly_Financials.pdf";
        mainVm.OpenInViewer("dummy/Quarterly_Financials.pdf");
        Assert.False(mainVm.IsHomePageVisible);
        Assert.True(mainVm.IsPdfViewerVisible);
        Assert.Equal("Quarterly_Financials.pdf - FryPDF", mainVm.WindowTitle);
    }

    [Fact]
    public async System.Threading.Tasks.Task HomeViewModel_NavigationBetweenStandardSections_MaintainsNullDynamicPageViewForInstantVisibility()
    {
        var navReg = new PdfEditorApp.Services.Navigation.NavigationRegistry();
        var context = new PdfEditorApp.Core.Plugins.FryPluginContext();
        context.RegisterService<PdfEditorApp.Core.Plugins.Descriptors.INavigationRegistry>(navReg);

        var host = new PdfEditorApp.Core.Plugins.PluginHost(context);
        var bundle = new PdfEditorApp.Plugins.Bundles.WorkspacePagesBundle();
        host.RegisterPlugins(bundle.Plugins);
        await host.StartAsync();

        var home = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry(),
            navigationRegistry: navReg);

        // Rapid navigation through all built-in sections must keep DynamicPageView null
        // so that pre-mounted, pre-compiled visual trees in HomeView.axaml are used instantaneously
        string[] sectionsToTest = new[]
        {
            "Home", "PdfReader", "NewDocument", "AllTools", "OrganizeAndPage",
            "OptimizeAndSecurity", "ConvertFromPdf", "ConvertToPdf", "EditAndForms",
            "AiAndAutomation", "Starred", "FontPackages", "TesseractData",
            "Trash", "Help", "Licensing", "Settings", "Plugins"
        };

        foreach (var section in sectionsToTest)
        {
            home.SelectNavSectionCommand.Execute(section);
            Assert.Null(home.DynamicPageView);
            Assert.False(home.IsToolPageActive);
        }

        await host.StopAsync();
    }

    [Fact]
    public void HomeViewModel_NavigationCachesContributedPluginView_AndReusesInstance()
    {
        var navReg = new PdfEditorApp.Services.Navigation.NavigationRegistry();
        int factoryInvocationCount = 0;
        navReg.RegisterNavigationItem(new PdfEditorApp.Core.Plugins.Descriptors.NavigationItemDescriptor
        {
            Id = "CustomAnalyticsExtension",
            Title = "Analytics Extension",
            Group = "Extensions",
            ViewFactory = sp =>
            {
                factoryInvocationCount++;
                return new object();
            }
        });

        var home = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry(),
            navigationRegistry: navReg);

        // First navigation invokes factory and caches view
        home.SelectNavSectionCommand.Execute("CustomAnalyticsExtension");
        Assert.Equal(1, factoryInvocationCount);
        var firstView = home.DynamicPageView;
        Assert.NotNull(firstView);

        // Navigate away to Home (standard section)
        home.SelectNavSectionCommand.Execute("Home");
        Assert.Null(home.DynamicPageView);

        // Navigate back to extension: MUST reuse cached instance without re-invoking factory
        home.SelectNavSectionCommand.Execute("CustomAnalyticsExtension");
        Assert.Equal(1, factoryInvocationCount);
        Assert.Same(firstView, home.DynamicPageView);
    }

    [Fact]
    public void HomeViewModel_SelectNavSection_LogsNavigationTimingEntry()
    {
        var navReg = new PdfEditorApp.Services.Navigation.NavigationRegistry();
        var home = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry(),
            navigationRegistry: navReg);

        home.SelectNavSectionCommand.Execute("AllTools");

        var navEntry = AppLogService.Instance.GetSnapshot().LastOrDefault(e =>
            e.Category == "Navigation" &&
            e.Level == AppLogLevel.Info &&
            e.Message.Contains("-> AllTools"));

        Assert.NotNull(navEntry);
        Assert.Matches(@"in \d+ms", navEntry!.Message);
    }

    [Fact]
    public void HomeViewModel_SelectNavSection_DistinguishesCacheHitFromColdFactory()
    {
        var navReg = new PdfEditorApp.Services.Navigation.NavigationRegistry();
        navReg.RegisterNavigationItem(new PdfEditorApp.Core.Plugins.Descriptors.NavigationItemDescriptor
        {
            Id = "NavTimingTestExtension",
            Title = "Nav Timing Test Extension",
            Group = "Extensions",
            ViewFactory = sp => new object()
        });

        var home = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry(),
            navigationRegistry: navReg);

        home.SelectNavSectionCommand.Execute("NavTimingTestExtension");
        var firstEntry = AppLogService.Instance.GetSnapshot().LastOrDefault(e =>
            e.Category == "Navigation" && e.Message.Contains("-> NavTimingTestExtension"));
        Assert.NotNull(firstEntry);
        Assert.Contains("cold-factory", firstEntry!.Message);

        home.SelectNavSectionCommand.Execute("Home");
        home.SelectNavSectionCommand.Execute("NavTimingTestExtension");
        var secondEntry = AppLogService.Instance.GetSnapshot().LastOrDefault(e =>
            e.Category == "Navigation" && e.Message.Contains("-> NavTimingTestExtension"));
        Assert.NotNull(secondEntry);
        Assert.Contains("cache-hit", secondEntry!.Message);
    }

    [Fact]
    public void HomeViewModel_NavigatingFromPluginsToDiagnosticLogs_HidesPluginsWorkspace_AndShowsDiagnosticLogsPage()
    {
        var navReg = new PdfEditorApp.Services.Navigation.NavigationRegistry();
        navReg.RegisterNavigationItem(new PdfEditorApp.Core.Plugins.Descriptors.NavigationItemDescriptor
        {
            Id = "DiagnosticLogs",
            Title = "Diagnostic Logs",
            Group = "Library",
            // DisplayMode defaults to ScrollableDocument, matching the real DiagnosticLogsPagePlugin.
            ViewFactory = sp => new object()
        });

        var home = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry(),
            navigationRegistry: navReg);

        home.SelectNavSectionCommand.Execute("Plugins");
        Assert.True(home.IsPluginsWorkspaceActive);

        home.SelectNavSectionCommand.Execute("DiagnosticLogs");

        // Before HomeNavSection gained a DiagnosticLogs member, Enum.TryParse("DiagnosticLogs")
        // failed here, so SelectedNavSection stayed stuck at Plugins — IsPluginsWorkspaceActive
        // and IsFullViewportActive (both keyed off SelectedNavSection == Plugins) never cleared,
        // so the Plugins panel never hid and neither ContentControl ever picked up the new page:
        // navigating away from Plugins to any non-HomeNavSection page appeared to hang.
        Assert.False(home.IsPluginsWorkspaceActive);
        Assert.False(home.IsFullViewportActive);
        Assert.True(home.IsStandardScrollableContentActive);
        Assert.Null(home.DynamicFullViewportPageView);
        Assert.NotNull(home.DynamicScrollablePageView);
    }

    [Fact]
    public void HomeViewModel_NavigationWhileToolActive_AutomaticallyClosesTool()
    {
        var home = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry());

        home.OpenToolPage(PdfToolId.MergePdf);
        Assert.True(home.IsToolPageActive);
        Assert.NotNull(home.ActiveToolCard);

        // Navigating to Settings must clean up the tool
        home.SelectNavSectionCommand.Execute("Settings");
        Assert.False(home.IsToolPageActive);
        Assert.Null(home.ActiveToolCard);
        Assert.Null(home.ActiveToolViewModel);
        Assert.True(home.IsSettingsSection);
    }

    [Fact]
    public void HomeViewModel_FullViewportPluginNavigation_ActivatesFullViewportAndSuppressesOuterScroll()
    {
        var navReg = new PdfEditorApp.Services.Navigation.NavigationRegistry();
        navReg.RegisterNavigationItem(new PdfEditorApp.Core.Plugins.Descriptors.NavigationItemDescriptor
        {
            Id = "CanvaImageEditor",
            Title = "Image Studio",
            Group = "Creative Studios",
            DisplayMode = PdfEditorApp.Core.Plugins.Descriptors.NavigationDisplayMode.FullViewport,
            HideTopSearchBar = false,
            ViewFactory = sp => new object()
        });

        var home = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry(),
            navigationRegistry: navReg);

        // Before navigation: Home dashboard is active (scrollable, search bar hidden)
        Assert.False(home.IsFullViewportActive);
        Assert.True(home.IsStandardScrollableContentActive);
        Assert.False(home.IsTopSearchBarVisible);

        // Navigate to full-viewport tool (descriptor has HideTopSearchBar = false)
        home.SelectNavSectionCommand.Execute("CanvaImageEditor");

        Assert.True(home.IsFullViewportActive);
        Assert.False(home.IsStandardScrollableContentActive);
        Assert.True(home.IsTopSearchBarVisible);
        Assert.False(home.IsSidebarCompactRail);
        Assert.NotNull(home.DynamicPageView);
    }

    [Fact]
    public void HomeViewModel_ImmersiveStudioPluginNavigation_ActivatesFullViewportHidesSearchBarAndEnablesCompactRail()
    {
        var navReg = new PdfEditorApp.Services.Navigation.NavigationRegistry();
        navReg.RegisterNavigationItem(new PdfEditorApp.Core.Plugins.Descriptors.NavigationItemDescriptor
        {
            Id = "ImmersiveFormDesigner",
            Title = "Form Studio",
            Group = "Design",
            DisplayMode = PdfEditorApp.Core.Plugins.Descriptors.NavigationDisplayMode.ImmersiveStudio,
            HideTopSearchBar = true,
            ViewFactory = sp => new object()
        });

        var home = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry(),
            navigationRegistry: navReg);

        // Navigate to immersive studio
        home.SelectNavSectionCommand.Execute("ImmersiveFormDesigner");

        Assert.True(home.IsFullViewportActive);
        Assert.False(home.IsStandardScrollableContentActive);
        Assert.False(home.IsTopSearchBarVisible);
        Assert.True(home.IsSidebarCompactRail);
        Assert.NotNull(home.DynamicPageView);

        // Navigate back to standard section (Home) restores all standard layout flags
        home.SelectNavSectionCommand.Execute("Home");

        Assert.False(home.IsFullViewportActive);
        Assert.True(home.IsStandardScrollableContentActive);
        Assert.False(home.IsTopSearchBarVisible);
        Assert.False(home.IsSidebarCompactRail);
        Assert.Null(home.DynamicPageView);

        // Navigating to NewDocument keeps top search bar hidden
        home.SelectNavSectionCommand.Execute("NewDocument");
        Assert.False(home.IsTopSearchBarVisible);
    }

    [Fact]
    public void HomeViewModel_DynamicPageView_MutualExclusionBetweenFullViewportAndScrollable()
    {
        var navReg = new PdfEditorApp.Services.Navigation.NavigationRegistry();
        var fullViewportControl = new Avalonia.Controls.Panel();
        var scrollableControl = new Avalonia.Controls.Panel();

        navReg.RegisterNavigationItem(new PdfEditorApp.Core.Plugins.Descriptors.NavigationItemDescriptor
        {
            Id = "FullViewportStudio",
            Title = "Full Viewport Studio",
            Group = "Creative Studios",
            DisplayMode = PdfEditorApp.Core.Plugins.Descriptors.NavigationDisplayMode.FullViewport,
            ViewFactory = _ => fullViewportControl
        });

        navReg.RegisterNavigationItem(new PdfEditorApp.Core.Plugins.Descriptors.NavigationItemDescriptor
        {
            Id = "ScrollableDocPage",
            Title = "Scrollable Doc Page",
            Group = "Documents",
            DisplayMode = PdfEditorApp.Core.Plugins.Descriptors.NavigationDisplayMode.ScrollableDocument,
            ViewFactory = _ => scrollableControl
        });

        var home = new HomeViewModel(
            new RecentDocumentsService(),
            new TemplateService(),
            new ProjectPersistenceService(),
            new PdfEditorApp.Services.Tools.Core.PdfToolRegistry(),
            navigationRegistry: navReg);

        // Initially on Home: both dynamic views must be null
        Assert.Null(home.DynamicPageView);
        Assert.Null(home.DynamicFullViewportPageView);
        Assert.Null(home.DynamicScrollablePageView);

        // 1. Navigate to FullViewportStudio
        home.SelectNavSectionCommand.Execute("FullViewportStudio");
        Assert.Same(fullViewportControl, home.DynamicPageView);
        Assert.Same(fullViewportControl, home.DynamicFullViewportPageView);
        Assert.Null(home.DynamicScrollablePageView); // MUST be null to prevent visual parent clash!

        // 2. Navigate to ScrollableDocPage
        home.SelectNavSectionCommand.Execute("ScrollableDocPage");
        Assert.Same(scrollableControl, home.DynamicPageView);
        Assert.Null(home.DynamicFullViewportPageView); // MUST be null to prevent visual parent clash!
        Assert.Same(scrollableControl, home.DynamicScrollablePageView);

        // 3. Navigate back to Home
        home.SelectNavSectionCommand.Execute("Home");
        Assert.Null(home.DynamicPageView);
        Assert.Null(home.DynamicFullViewportPageView);
        Assert.Null(home.DynamicScrollablePageView);
    }

    [Fact]
    public void HomeView_SidebarLayout_DockPanelOrdersBottomFooterBeforeScrollViewerForVerticalScrolling()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "src", "PdfEditorApp", "Views", "HomeView.axaml")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var homeViewPath = Path.Combine(dir!.FullName, "src", "PdfEditorApp", "Views", "HomeView.axaml");
        var xaml = File.ReadAllText(homeViewPath);

        // Footer must be docked to Bottom
        Assert.Contains("DockPanel.Dock=\"Bottom\"", xaml);

        var footerIndex = xaml.IndexOf("<!-- Sidebar Footer with Theme Toggle & Version (Docked to Bottom FIRST so it stays pinned) -->", StringComparison.Ordinal);
        var scrollViewerIndex = xaml.IndexOf("<!-- Navigation Items (Scrollable Center filling remaining height between Top and Bottom) -->", StringComparison.Ordinal);

        Assert.True(footerIndex > 0, "Footer comment marker must be present in HomeView.axaml");
        Assert.True(scrollViewerIndex > 0, "ScrollViewer comment marker must be present in HomeView.axaml");
        Assert.True(footerIndex < scrollViewerIndex, "In DockPanel, child with DockPanel.Dock='Bottom' must appear BEFORE the fill ScrollViewer so that the center list can vertically scroll without pushing the footer off-screen.");

        // ScrollViewer must have auto vertical and disabled horizontal scrollbars, and MUST NOT dock to Top
        var scrollViewerTagStart = xaml.IndexOf("<ScrollViewer", scrollViewerIndex, StringComparison.Ordinal);
        var scrollViewerTagEnd = xaml.IndexOf(">", scrollViewerTagStart, StringComparison.Ordinal);
        var scrollViewerTag = xaml.Substring(scrollViewerTagStart, scrollViewerTagEnd - scrollViewerTagStart + 1);

        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", scrollViewerTag);
        Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", scrollViewerTag);
        Assert.DoesNotContain("DockPanel.Dock=\"Top\"", scrollViewerTag);
    }
}
