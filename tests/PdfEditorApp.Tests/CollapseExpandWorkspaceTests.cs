using System.Linq;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class CollapseExpandWorkspaceTests
{
    [Fact]
    public void MainViewModel_ToggleRibbonCollapse_TogglesStateCorrectly()
    {
        var vm = new MainViewModel();
        Assert.False(vm.IsRibbonCollapsed);

        vm.ToggleRibbonCollapseCommand.Execute(null);
        Assert.True(vm.IsRibbonCollapsed);

        vm.ToggleRibbonCollapseCommand.Execute(null);
        Assert.False(vm.IsRibbonCollapsed);
    }

    [Fact]
    public void MainViewModel_SelectRibbonTab_AutoExpandsWhenCollapsed()
    {
        var vm = new MainViewModel();
        vm.IsRibbonCollapsed = true;
        Assert.True(vm.IsRibbonCollapsed);

        // Selecting a ribbon tab should automatically expand the ribbon
        vm.SelectRibbonTabCommand.Execute(RibbonTabKind.Insert);

        Assert.Equal(RibbonTabKind.Insert, vm.ActiveRibbonTab);
        Assert.False(vm.IsRibbonCollapsed);
    }

    [Fact]
    public void MainViewModel_ToggleLeftSidebar_TogglesStateCorrectly()
    {
        var vm = new MainViewModel();
        Assert.False(vm.IsLeftSidebarCollapsed);

        vm.ToggleLeftSidebarCommand.Execute(null);
        Assert.True(vm.IsLeftSidebarCollapsed);

        vm.ToggleLeftSidebarCommand.Execute(null);
        Assert.False(vm.IsLeftSidebarCollapsed);
    }

    [Fact]
    public void MainViewModel_SelectSidebarTab_AutoExpandsWhenCollapsed()
    {
        var vm = new MainViewModel();
        vm.IsLeftSidebarCollapsed = true;
        Assert.True(vm.IsLeftSidebarCollapsed);

        // Selecting a sidebar tab should automatically expand the sidebar
        vm.SelectSidebarTabCommand.Execute(SidebarTabKind.Outline);

        Assert.Equal(SidebarTabKind.Outline, vm.ActiveSidebarTab);
        Assert.False(vm.IsLeftSidebarCollapsed);
    }

    [Fact]
    public void MainViewModel_ToggleInspectorCollapse_TogglesStateCorrectly()
    {
        var vm = new MainViewModel();
        Assert.False(vm.IsInspectorCollapsed);

        vm.ToggleInspectorCollapseCommand.Execute(null);
        Assert.True(vm.IsInspectorCollapsed);

        vm.ToggleInspectorCollapseCommand.Execute(null);
        Assert.False(vm.IsInspectorCollapsed);
    }

    [Fact]
    public void MainViewModel_ExpandAllPanels_ExpandsAllThreePanels()
    {
        var vm = new MainViewModel();
        vm.IsRibbonCollapsed = true;
        vm.IsLeftSidebarCollapsed = true;
        vm.IsInspectorCollapsed = true;

        vm.ExpandAllPanelsCommand.Execute(null);

        Assert.False(vm.IsRibbonCollapsed);
        Assert.False(vm.IsLeftSidebarCollapsed);
        Assert.False(vm.IsInspectorCollapsed);
    }

    [Fact]
    public void MainViewModel_CommandPalette_ContainsCollapseAndExpandEntries()
    {
        var vm = new MainViewModel();
        var palette = vm.AllPaletteCommands;

        Assert.Contains(palette, c => c.Title.Contains("Toggle Ribbon Toolbar"));
        Assert.Contains(palette, c => c.Title.Contains("Toggle Pages Sidebar"));
        Assert.Contains(palette, c => c.Title.Contains("Toggle Properties Inspector"));
        Assert.Contains(palette, c => c.Title.Contains("Expand All Panels"));
    }

    [Fact]
    public void InspectorViewModel_AccordionSections_ToggleIndividually()
    {
        var inspector = new InspectorViewModel();

        // Default state: all expanded
        Assert.True(inspector.IsPresetGeometryExpanded);
        Assert.True(inspector.IsLineStrokePatternExpanded);
        Assert.True(inspector.IsColorsExpanded);
        Assert.True(inspector.IsDimensionsExpanded);
        Assert.True(inspector.IsTypographyExpanded);
        Assert.True(inspector.IsParagraphExpanded);
        Assert.True(inspector.IsTransformExpanded);
        Assert.True(inspector.IsShadowExpanded);

        // Toggle Preset Geometry
        inspector.TogglePresetGeometryExpandedCommand.Execute(null);
        Assert.False(inspector.IsPresetGeometryExpanded);
        inspector.TogglePresetGeometryExpandedCommand.Execute(null);
        Assert.True(inspector.IsPresetGeometryExpanded);

        // Toggle Line Stroke Pattern
        inspector.ToggleLineStrokePatternExpandedCommand.Execute(null);
        Assert.False(inspector.IsLineStrokePatternExpanded);
        inspector.ToggleLineStrokePatternExpandedCommand.Execute(null);
        Assert.True(inspector.IsLineStrokePatternExpanded);

        // Toggle Colors
        inspector.ToggleColorsExpandedCommand.Execute(null);
        Assert.False(inspector.IsColorsExpanded);
        inspector.ToggleColorsExpandedCommand.Execute(null);
        Assert.True(inspector.IsColorsExpanded);

        // Toggle Dimensions
        inspector.ToggleDimensionsExpandedCommand.Execute(null);
        Assert.False(inspector.IsDimensionsExpanded);
        inspector.ToggleDimensionsExpandedCommand.Execute(null);
        Assert.True(inspector.IsDimensionsExpanded);

        // Toggle Typography
        inspector.ToggleTypographyExpandedCommand.Execute(null);
        Assert.False(inspector.IsTypographyExpanded);
        inspector.ToggleTypographyExpandedCommand.Execute(null);
        Assert.True(inspector.IsTypographyExpanded);

        // Toggle Paragraph
        inspector.ToggleParagraphExpandedCommand.Execute(null);
        Assert.False(inspector.IsParagraphExpanded);
        inspector.ToggleParagraphExpandedCommand.Execute(null);
        Assert.True(inspector.IsParagraphExpanded);

        // Toggle Transform
        inspector.ToggleTransformExpandedCommand.Execute(null);
        Assert.False(inspector.IsTransformExpanded);
        inspector.ToggleTransformExpandedCommand.Execute(null);
        Assert.True(inspector.IsTransformExpanded);

        // Toggle Shadow
        inspector.ToggleShadowExpandedCommand.Execute(null);
        Assert.False(inspector.IsShadowExpanded);
        inspector.ToggleShadowExpandedCommand.Execute(null);
        Assert.True(inspector.IsShadowExpanded);

        // Toggle Page Setup
        inspector.TogglePageSetupExpandedCommand.Execute(null);
        Assert.False(inspector.IsPageSetupExpanded);
        inspector.TogglePageSetupExpandedCommand.Execute(null);
        Assert.True(inspector.IsPageSetupExpanded);

        // Toggle Image Adjustments
        inspector.ToggleImageAdjustmentsExpandedCommand.Execute(null);
        Assert.False(inspector.IsImageAdjustmentsExpanded);
        inspector.ToggleImageAdjustmentsExpandedCommand.Execute(null);
        Assert.True(inspector.IsImageAdjustmentsExpanded);

        // Toggle Table Properties
        inspector.ToggleTablePropertiesExpandedCommand.Execute(null);
        Assert.False(inspector.IsTablePropertiesExpanded);
        inspector.ToggleTablePropertiesExpandedCommand.Execute(null);
        Assert.True(inspector.IsTablePropertiesExpanded);

        // Toggle Math Formula
        inspector.ToggleMathFormulaExpandedCommand.Execute(null);
        Assert.False(inspector.IsMathFormulaExpanded);
        inspector.ToggleMathFormulaExpandedCommand.Execute(null);
        Assert.True(inspector.IsMathFormulaExpanded);
    }

    [Fact]
    public void InspectorViewModel_ExpandAndCollapseAllSections_ControlsAllSubSections()
    {
        var inspector = new InspectorViewModel();

        inspector.CollapseAllSectionsCommand.Execute(null);

        Assert.False(inspector.IsPageSetupExpanded);
        Assert.False(inspector.IsPresetGeometryExpanded);
        Assert.False(inspector.IsLineStrokePatternExpanded);
        Assert.False(inspector.IsColorsExpanded);
        Assert.False(inspector.IsDimensionsExpanded);
        Assert.False(inspector.IsTypographyExpanded);
        Assert.False(inspector.IsParagraphExpanded);
        Assert.False(inspector.IsTransformExpanded);
        Assert.False(inspector.IsShadowExpanded);
        Assert.False(inspector.IsImageAdjustmentsExpanded);
        Assert.False(inspector.IsTablePropertiesExpanded);
        Assert.False(inspector.IsMathFormulaExpanded);

        inspector.ExpandAllSectionsCommand.Execute(null);

        Assert.True(inspector.IsPageSetupExpanded);
        Assert.True(inspector.IsPresetGeometryExpanded);
        Assert.True(inspector.IsLineStrokePatternExpanded);
        Assert.True(inspector.IsColorsExpanded);
        Assert.True(inspector.IsDimensionsExpanded);
        Assert.True(inspector.IsTypographyExpanded);
        Assert.True(inspector.IsParagraphExpanded);
        Assert.True(inspector.IsTransformExpanded);
        Assert.True(inspector.IsShadowExpanded);
        Assert.True(inspector.IsImageAdjustmentsExpanded);
        Assert.True(inspector.IsTablePropertiesExpanded);
        Assert.True(inspector.IsMathFormulaExpanded);
    }

    [Fact]
    public void MainViewModel_TogglePanels_ShowsToastMessage()
    {
        var vm = new MainViewModel();

        vm.ToggleRibbonCollapseCommand.Execute(null);
        Assert.True(vm.IsToastVisible);
        Assert.Contains("Ribbon", vm.ToastMessage);

        vm.ToggleLeftSidebarCommand.Execute(null);
        Assert.True(vm.IsToastVisible);
        Assert.Contains("Sidebar", vm.ToastMessage);

        vm.ToggleInspectorCollapseCommand.Execute(null);
        Assert.True(vm.IsToastVisible);
        Assert.Contains("Inspector", vm.ToastMessage);

        vm.ExpandAllPanelsCommand.Execute(null);
        Assert.True(vm.IsToastVisible);
        Assert.Contains("All Workspace Panels Expanded", vm.ToastMessage);
    }
}

