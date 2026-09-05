using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.Templates;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;
using PdfEditorApp.ViewModels.FryPdfViewer;
using Xunit;

namespace PdfEditorApp.Tests;

public class FryPdfViewerTests
{
    private readonly ITemplateService _templateService = new TemplateService();

    [Fact]
    public void FryPdfViewer_LoadFromModel_InitializesPagesAndMetadata()
    {
        var doc = _templateService.CreateAnnualReportTemplate();
        var viewer = new FryPdfViewerViewModel();

        viewer.LoadFromModel(doc, "AnnualReport2026.frypdf");

        Assert.True(viewer.HasPages);
        Assert.Equal(doc.Pages.Count, viewer.TotalPagesCount);
        Assert.Equal(1, viewer.CurrentPageNumber);
        Assert.Equal(0, viewer.CurrentPageIndex);
        Assert.NotNull(viewer.CurrentPage);
        Assert.Contains("AnnualReport2026", viewer.DocumentTitle);
    }

    [Fact]
    public void FryPdfViewer_PageNavigation_NextPreviousFirstLast()
    {
        var doc = _templateService.CreateAnnualReportTemplate();
        var viewer = new FryPdfViewerViewModel();
        viewer.LoadFromModel(doc);

        Assert.True(viewer.CanGoNextPage);
        Assert.False(viewer.CanGoPreviousPage);

        // Next page
        viewer.NextPageCommand.Execute(null);
        Assert.Equal(2, viewer.CurrentPageNumber);
        Assert.True(viewer.CanGoPreviousPage);

        // Last page
        viewer.LastPageCommand.Execute(null);
        Assert.Equal(viewer.TotalPagesCount, viewer.CurrentPageNumber);
        Assert.False(viewer.CanGoNextPage);

        // Previous page
        viewer.PreviousPageCommand.Execute(null);
        Assert.Equal(viewer.TotalPagesCount - 1, viewer.CurrentPageNumber);

        // First page
        viewer.FirstPageCommand.Execute(null);
        Assert.Equal(1, viewer.CurrentPageNumber);

        // Direct jump
        viewer.GoToPageCommand.Execute(2);
        Assert.Equal(2, viewer.CurrentPageNumber);
    }

    [Fact]
    public void FryPdfViewer_ZoomControls_UpdatesZoomLevelCorrectly()
    {
        var viewer = new FryPdfViewerViewModel();

        viewer.ZoomLevel = 1.0;

        viewer.ZoomInCommand.Execute(null);
        Assert.True(viewer.ZoomLevel > 1.0);

        viewer.ZoomOutCommand.Execute(null);
        viewer.ZoomOutCommand.Execute(null);
        Assert.True(viewer.ZoomLevel < 1.0);

        viewer.ResetZoomCommand.Execute(null);
        Assert.Equal(1.0, viewer.ZoomLevel);

        viewer.FitToWidthCommand.Execute(null);
        Assert.Equal(1.25, viewer.ZoomLevel);

        viewer.FitToPageCommand.Execute(null);
        Assert.Equal(0.85, viewer.ZoomLevel);
    }

    [Fact]
    public void InteractiveTable_Filtering_FiltersRowsAccurately()
    {
        var tableVm = new TableElementViewModel();
        tableVm.Headers.Clear();
        tableVm.Headers.Add(new TableHeaderItem("Service"));
        tableVm.Headers.Add(new TableHeaderItem("Cost"));

        tableVm.Rows.Clear();
        tableVm.Rows.Add(new TableRowItem(new[] { "Cloud Architecture", "$6,000" }));
        tableVm.Rows.Add(new TableRowItem(new[] { "Avalonia Desktop UI", "$8,400" }));
        tableVm.Rows.Add(new TableRowItem(new[] { "QuestPDF Engine", "$4,000" }));

        var interactiveTable = new InteractiveTableViewModel(tableVm);
        Assert.Equal(3, interactiveTable.TotalRowCount);
        Assert.Equal(3, interactiveTable.FilteredRowCount);

        // Filter by "Avalonia"
        interactiveTable.FilterQuery = "Avalonia";
        Assert.Equal(1, interactiveTable.FilteredRowCount);
        Assert.Equal("Avalonia Desktop UI", interactiveTable.DisplayedRows[0].GetCell(0));

        // Clear filter
        interactiveTable.ClearFilterCommand.Execute(null);
        Assert.Equal(3, interactiveTable.FilteredRowCount);
    }

    [Fact]
    public void InteractiveTable_Sorting_SortsAlphabeticallyAndNumerically()
    {
        var tableVm = new TableElementViewModel();
        tableVm.Headers.Clear();
        tableVm.Headers.Add(new TableHeaderItem("Task"));
        tableVm.Headers.Add(new TableHeaderItem("Hours"));

        tableVm.Rows.Clear();
        tableVm.Rows.Add(new TableRowItem(new[] { "Zeppelin Design", "10" }));
        tableVm.Rows.Add(new TableRowItem(new[] { "Alpha Review", "40" }));
        tableVm.Rows.Add(new TableRowItem(new[] { "Beta Optimization", "20" }));

        var interactiveTable = new InteractiveTableViewModel(tableVm);

        // Sort column 0 ascending (Alpha, Beta, Zeppelin)
        interactiveTable.SortByColumnCommand.Execute(0);
        Assert.Equal("Alpha Review", interactiveTable.DisplayedRows[0].GetCell(0));
        Assert.Equal("Zeppelin Design", interactiveTable.DisplayedRows[2].GetCell(0));

        // Sort column 0 descending (Zeppelin, Beta, Alpha)
        interactiveTable.SortByColumnCommand.Execute(0);
        Assert.Equal("Zeppelin Design", interactiveTable.DisplayedRows[0].GetCell(0));
        Assert.Equal("Alpha Review", interactiveTable.DisplayedRows[2].GetCell(0));
    }

    [Fact]
    public void InteractiveTable_GenerateCsv_ExportsValidCsv()
    {
        var tableVm = new TableElementViewModel();
        tableVm.Headers.Clear();
        tableVm.Headers.Add(new TableHeaderItem("Item"));
        tableVm.Headers.Add(new TableHeaderItem("Amount"));

        tableVm.Rows.Clear();
        tableVm.Rows.Add(new TableRowItem(new[] { "Consulting", "$100" }));
        tableVm.Rows.Add(new TableRowItem(new[] { "Support", "$50" }));

        var interactiveTable = new InteractiveTableViewModel(tableVm);
        string csv = interactiveTable.GenerateCsv();

        Assert.Contains("Item,Amount", csv);
        Assert.Contains("Consulting,$100", csv);
        Assert.Contains("Support,$50", csv);
    }

    [Fact]
    public void InteractiveChart_PopulateAndAnimation_CalculatesValuesAndTooltips()
    {
        var chartVm = new ChartElementViewModel();
        chartVm.Title = "Sales Q1-Q4";
        chartVm.Bars.Clear();
        chartVm.Bars.Add(new ChartBarItem { Category = "Q1", Value = 10, ValueLabel = "$10M", ColorHex = "#0F6CBD" });
        chartVm.Bars.Add(new ChartBarItem { Category = "Q2", Value = 20, ValueLabel = "$20M", ColorHex = "#107C41" });
        chartVm.Bars.Add(new ChartBarItem { Category = "Q3", Value = 30, ValueLabel = "$30M", ColorHex = "#D83B01" });

        var interactiveChart = new InteractiveChartViewModel(chartVm);

        Assert.Equal(3, interactiveChart.Items.Count);
        Assert.Equal(60, interactiveChart.TotalSum);
        Assert.Equal(30, interactiveChart.MaxItemValue);

        // Tooltip checks
        var q2Item = interactiveChart.Items[1];
        Assert.Equal("Q2: $20M (33.3%)", q2Item.TooltipText);

        // Toggle data table view
        Assert.False(interactiveChart.IsShowingDataTable);
        interactiveChart.ToggleDataTableCommand.Execute(null);
        Assert.True(interactiveChart.IsShowingDataTable);
    }

    [Fact]
    public void FryPdfViewer_Search_FindsTextAcrossPages()
    {
        var doc = new PdfDocumentModel { Title = "Searchable Presentation" };
        var page1 = new PdfPageModel { PageNumber = 1 };
        page1.Elements.Add(new PdfTextElement { Text = "First Chapter Introduction", X = 10, Y = 10, Width = 200, Height = 30 });
        var page2 = new PdfPageModel { PageNumber = 2 };
        page2.Elements.Add(new PdfTextElement { Text = "Second Chapter Financials", X = 10, Y = 10, Width = 200, Height = 30 });

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);

        var viewer = new FryPdfViewerViewModel();
        viewer.LoadFromModel(doc);

        viewer.SearchQuery = "Financials";
        Assert.Single(viewer.SearchResults);
        Assert.Equal(2, viewer.SearchResults[0].PageNumber);
    }

    [Fact]
    public void MainViewModel_OpenCurrentInInteractiveReader_TransitionsSeamlessly()
    {
        var mainVm = new MainViewModel();

        // Initially on Home
        Assert.True(mainVm.IsHomePageVisible);
        Assert.False(mainVm.IsFryPdfViewerVisible);

        // Switch to Interactive Reader
        mainVm.OpenCurrentInInteractiveReaderCommand.Execute(null);

        Assert.True(mainVm.IsFryPdfViewerVisible);
        Assert.False(mainVm.IsEditorVisible);
        Assert.False(mainVm.IsHomePageVisible);
        Assert.Contains("Interactive Reader", mainVm.WindowTitle);

        // Return to Home
        mainVm.NavigateToHomeCommand.Execute(null);
        Assert.False(mainVm.IsFryPdfViewerVisible);
        Assert.True(mainVm.IsHomePageVisible);
    }

    [Fact]
    public void PresentationMode_TogglesState()
    {
        var viewer = new FryPdfViewerViewModel();
        Assert.False(viewer.IsPresentationMode);

        viewer.TogglePresentationModeCommand.Execute(null);
        Assert.True(viewer.IsPresentationMode);

        viewer.TogglePresentationModeCommand.Execute(null);
        Assert.False(viewer.IsPresentationMode);
    }

    [Fact]
    public void FryPdfViewer_LoadsAllElements_IntoUnifiedElementsCollection()
    {
        var doc = _templateService.CreateAnnualReportTemplate();
        var viewer = new FryPdfViewerViewModel();
        viewer.LoadFromModel(doc);

        Assert.NotEmpty(viewer.Pages);
        foreach (var page in viewer.Pages)
        {
            Assert.NotEmpty(page.Elements);
            foreach (var el in page.Elements)
            {
                Assert.True(el.Width > 0, $"Element {el.DisplayName} should have positive width");
                Assert.True(el.Height > 0, $"Element {el.DisplayName} should have positive height");
            }
        }
    }

    [Fact]
    public void InteractiveTable_InheritsFromElementViewModelBase_AndPreservesProperties()
    {
        var tableVm = new TableElementViewModel
        {
            X = 50,
            Y = 120,
            Width = 500,
            Height = 300,
            ZIndex = 500
        };
        tableVm.Headers.Add(new TableHeaderItem("Metric"));
        tableVm.Rows.Add(new TableRowItem(new[] { "Revenue" }));

        var interactiveTable = new InteractiveTableViewModel(tableVm);
        Assert.Equal(ElementKind.Table, interactiveTable.Kind);
        Assert.Equal(50, interactiveTable.X);
        Assert.Equal(120, interactiveTable.Y);
        Assert.Equal(500, interactiveTable.Width);
        Assert.Equal(300, interactiveTable.Height);
        Assert.Equal(500, interactiveTable.ZIndex);
        Assert.NotNull(interactiveTable.CopyToClipboardCommand);
    }

    [Fact]
    public void InteractiveChart_InheritsFromElementViewModelBase_AndPreservesProperties()
    {
        var chartVm = new ChartElementViewModel
        {
            X = 60,
            Y = 200,
            Width = 450,
            Height = 250,
            ZIndex = 400,
            Title = "Quarterly Growth"
        };
        chartVm.Bars.Add(new ChartBarItem { Category = "Q1", Value = 15, ValueLabel = "$15k" });

        var interactiveChart = new InteractiveChartViewModel(chartVm);
        Assert.Equal(ElementKind.Chart, interactiveChart.Kind);
        Assert.Equal(60, interactiveChart.X);
        Assert.Equal(200, interactiveChart.Y);
        Assert.Equal(450, interactiveChart.Width);
        Assert.Equal(250, interactiveChart.Height);
        Assert.Equal(400, interactiveChart.ZIndex);
    }

    [Fact]
    public void InteractiveExecutiveBriefTemplate_CreatesLandscapeDocumentWithRichElements()
    {
        var doc = _templateService.CreateInteractiveExecutiveBriefTemplate();

        Assert.NotNull(doc);
        Assert.Equal(3, doc.Pages.Count);
        Assert.Contains("QuantumScale", doc.Title);

        foreach (var page in doc.Pages)
        {
            Assert.Equal(PageOrientation.Landscape, page.Orientation);
            Assert.Equal(1131, page.Width);
            Assert.Equal(800, page.Height);
            Assert.NotEmpty(page.Elements);
        }

        // Page 1: Revenue Bar Chart, Donut Pillar Chart & Sticky Note
        var page1 = doc.Pages[0];
        var page1Charts = page1.Elements.OfType<PdfChartElement>().ToList();
        Assert.Equal(2, page1Charts.Count);
        Assert.Contains(page1Charts, c => c.ChartType == ChartType.BarColumn);
        Assert.Contains(page1Charts, c => c.ChartType == ChartType.DonutPie);
        Assert.Contains(page1.Elements, e => e is PdfStickyNoteElement);

        // Page 2: Operational Data Table & Regional Capacity Horizontal Bar Chart
        var page2 = doc.Pages[1];
        var table = page2.Elements.OfType<PdfTableElement>().FirstOrDefault();
        Assert.NotNull(table);
        Assert.Equal(7, table.Headers.Count);
        Assert.Equal(7, table.Rows.Count);
        var page2Chart = page2.Elements.OfType<PdfChartElement>().FirstOrDefault();
        Assert.NotNull(page2Chart);
        Assert.Equal(ChartType.HorizontalBar, page2Chart.ChartType);

        // Page 3: Compliance Checkboxes, Signatures, Redaction, Barcode, Area Chart, QR Code
        var page3 = doc.Pages[2];
        var formFields = page3.Elements.OfType<PdfFormFieldElement>().ToList();
        Assert.True(formFields.Count >= 5);
        Assert.Contains(formFields, f => f.FieldType == FormFieldType.Checkbox);
        Assert.Contains(formFields, f => f.FieldType == FormFieldType.Signature);
        Assert.Contains(page3.Elements, e => e is PdfRedactionElement);
        Assert.Contains(page3.Elements, e => e is PdfBarcodeElement);
        Assert.Contains(page3.Elements, e => e is PdfQrCodeElement);
        var page3Chart = page3.Elements.OfType<PdfChartElement>().FirstOrDefault();
        Assert.NotNull(page3Chart);
        Assert.Equal(ChartType.Area, page3Chart.ChartType);
    }

    [Fact]
    public void FryPdfViewer_LoadsLandscapeInteractiveExecutiveDeck_AndOperatesInteractively()
    {
        var doc = _templateService.CreateInteractiveExecutiveBriefTemplate();
        var viewer = new FryPdfViewerViewModel();
        viewer.LoadFromModel(doc, "QuantumScale_Executive_Brief.frypdf");

        Assert.Equal(3, viewer.TotalPagesCount);
        Assert.NotNull(viewer.CurrentPage);
        Assert.Equal(1131, viewer.CurrentPage.Width);
        Assert.Equal(800, viewer.CurrentPage.Height);

        // Navigate to Page 2 (with interactive table)
        viewer.GoToPageCommand.Execute(2);
        Assert.Equal(2, viewer.CurrentPageNumber);

        var tableVm = viewer.CurrentPage.Elements.OfType<InteractiveTableViewModel>().FirstOrDefault();
        Assert.NotNull(tableVm);
        Assert.Equal(7, tableVm.TotalRowCount);
        Assert.Equal(7, tableVm.FilteredRowCount);

        // Real-time table filter
        tableVm.FilterQuery = "Agent";
        Assert.Equal(1, tableVm.FilteredRowCount);
        Assert.Contains("Autonomous AI Agent", tableVm.DisplayedRows[0].GetCell(0));

        // Clear filter
        tableVm.ClearFilterCommand.Execute(null);
        Assert.Equal(7, tableVm.FilteredRowCount);

        // Sort by FY26 ARR descending
        tableVm.SortByColumnCommand.Execute(3);

        // CSV export
        string csv = tableVm.GenerateCsv();
        Assert.Contains("Business Unit & Segment", csv);
        Assert.Contains("Autonomous AI Agent Mesh", csv);

        // Check interactive horizontal bar chart on page 2
        var chartVm = viewer.CurrentPage.Elements.OfType<InteractiveChartViewModel>().FirstOrDefault();
        Assert.NotNull(chartVm);
        Assert.True(chartVm.IsHorizontalBar);
        Assert.Equal(5, chartVm.Items.Count);
        Assert.True(chartVm.TotalSum > 100);
    }

    [Fact]
    public void FryPdfViewer_SlideDeckNavigation_NextAndPreviousButtonsWorkAcrossAllSlides_AndWrapInPresentationMode()
    {
        var doc = _templateService.CreateInteractiveExecutiveBriefTemplate();
        var viewer = new FryPdfViewerViewModel();

        // Before document is loaded, commands should not execute
        Assert.False(viewer.CanGoNextPage);
        Assert.False(viewer.CanGoPreviousPage);
        Assert.False(viewer.NextPageCommand.CanExecute(null));
        Assert.False(viewer.PreviousPageCommand.CanExecute(null));

        // Load document
        viewer.LoadFromModel(doc, "QuantumScale_Executive_Brief.frypdf");

        // Slide 1 of 3: Next is enabled, Previous is disabled
        Assert.Equal(1, viewer.CurrentPageNumber);
        Assert.True(viewer.CanGoNextPage);
        Assert.False(viewer.CanGoPreviousPage);
        Assert.True(viewer.NextPageCommand.CanExecute(null));
        Assert.False(viewer.PreviousPageCommand.CanExecute(null));

        // Advance to Slide 2
        viewer.NextPageCommand.Execute(null);
        Assert.Equal(2, viewer.CurrentPageNumber);
        Assert.True(viewer.CanGoNextPage);
        Assert.True(viewer.CanGoPreviousPage);
        Assert.True(viewer.NextPageCommand.CanExecute(null));
        Assert.True(viewer.PreviousPageCommand.CanExecute(null));

        // Advance to Slide 3 (Slide 3 of 3 - User's Reported Screen)
        viewer.NextPageCommand.Execute(null);
        Assert.Equal(3, viewer.CurrentPageNumber);
        Assert.False(viewer.CanGoNextPage);
        Assert.True(viewer.CanGoPreviousPage);
        Assert.False(viewer.NextPageCommand.CanExecute(null));
        Assert.True(viewer.PreviousPageCommand.CanExecute(null));

        // Left button (<) MUST successfully navigate back to Slide 2
        viewer.PreviousPageCommand.Execute(null);
        Assert.Equal(2, viewer.CurrentPageNumber);

        // Left button (<) again back to Slide 1
        viewer.PreviousPageCommand.Execute(null);
        Assert.Equal(1, viewer.CurrentPageNumber);

        // Test Presentation Mode: Seamless Wrap-Around
        viewer.TogglePresentationModeCommand.Execute(null);
        Assert.True(viewer.IsPresentationMode);

        // In presentation mode on Slide 1: Previous (<) loops around to Slide 3
        Assert.True(viewer.CanGoPreviousPage);
        Assert.True(viewer.PreviousPageCommand.CanExecute(null));
        viewer.PreviousPageCommand.Execute(null);
        Assert.Equal(3, viewer.CurrentPageNumber);

        // In presentation mode on Slide 3: Next (>) loops around to Slide 1
        Assert.True(viewer.CanGoNextPage);
        Assert.True(viewer.NextPageCommand.CanExecute(null));
        viewer.NextPageCommand.Execute(null);
        Assert.Equal(1, viewer.CurrentPageNumber);
    }

    [Fact]
    public void FryPdfViewer_PresentationMode_SuppressesDistractions_AndCalculatesFitToViewport()
    {
        var doc = _templateService.CreateInteractiveExecutiveBriefTemplate();
        var viewer = new FryPdfViewerViewModel();
        viewer.LoadFromModel(doc);

        // Standard reading mode state: sidebar visible, padding 36, borders enabled, headers visible
        Assert.False(viewer.IsPresentationMode);
        Assert.True(viewer.IsSidebarVisibleInView);
        Assert.Equal("Transparent", viewer.ViewportBackgroundBrush);
        Assert.Equal(new Avalonia.Thickness(36), viewer.ViewportPadding);
        Assert.Equal(1.0, viewer.PageBorderThickness);
        Assert.True(viewer.ShowHeaderFooterInView);

        // Enter presentation mode: everything suppressed, black background, zero padding, zero borders
        viewer.TogglePresentationModeCommand.Execute(null);
        Assert.True(viewer.IsPresentationMode);
        Assert.False(viewer.IsSidebarVisibleInView);
        Assert.Equal("#000000", viewer.ViewportBackgroundBrush);
        Assert.Equal(new Avalonia.Thickness(0), viewer.ViewportPadding);
        Assert.Equal(0.0, viewer.PageBorderThickness);
        Assert.False(viewer.ShowHeaderFooterInView);

        // Test aspect-fit calculation (1131 x 800 slide in 1920 x 1080 display)
        viewer.FitToViewport(1920, 1080);
        double expectedFit = Math.Round(Math.Min(1920.0 / 1131.0, 1080.0 / 800.0), 3);
        Assert.Equal(expectedFit, viewer.ZoomLevel);

        // Exit presentation mode: normal reading mode restored
        viewer.TogglePresentationModeCommand.Execute(null);
        Assert.False(viewer.IsPresentationMode);
        Assert.True(viewer.IsSidebarVisibleInView);
        Assert.Equal("Transparent", viewer.ViewportBackgroundBrush);
        Assert.Equal(new Avalonia.Thickness(36), viewer.ViewportPadding);
        Assert.Equal(1.0, viewer.PageBorderThickness);
        Assert.True(viewer.ShowHeaderFooterInView);
    }

    [Fact]
    public void InteractiveChart_SupportsMultipleChartTypes_AndCenterSummary()
    {
        var chartVm = new ChartElementViewModel
        {
            Title = "ARR by Pillar",
            ChartType = ChartType.DonutPie
        };
        chartVm.Bars.Clear();
        chartVm.Bars.Add(new ChartBarItem { Category = "Mesh", Value = 50, ValueLabel = "$50M", ColorHex = "#4F46E5" });
        chartVm.Bars.Add(new ChartBarItem { Category = "Enclaves", Value = 50, ValueLabel = "$50M", ColorHex = "#0284C7" });

        var interactiveDonut = new InteractiveChartViewModel(chartVm);
        Assert.True(interactiveDonut.IsDonutPie);
        Assert.False(interactiveDonut.IsBarColumn);
        Assert.False(interactiveDonut.IsHorizontalBar);
        Assert.Equal(100, interactiveDonut.TotalSum);
        Assert.Equal("$100.0M", interactiveDonut.CenterSummaryValue);
        Assert.Equal("Total ARR", interactiveDonut.CenterSummaryLabel);

        // Hover over second item
        interactiveDonut.HoveredItem = interactiveDonut.Items[1];
        Assert.Equal("$50M", interactiveDonut.CenterSummaryValue);
        Assert.Equal("Enclaves", interactiveDonut.CenterSummaryLabel);

        // Test Horizontal Bar
        chartVm.ChartType = ChartType.HorizontalBar;
        var interactiveHorizontal = new InteractiveChartViewModel(chartVm);
        Assert.True(interactiveHorizontal.IsHorizontalBar);
        Assert.False(interactiveHorizontal.IsDonutPie);
        Assert.False(interactiveHorizontal.IsBarColumn);
        Assert.True(interactiveHorizontal.Items[0].AnimatedWidthPx > 0);
        Assert.Equal("50.0%", interactiveHorizontal.Items[0].ProgressPercentageString);
    }
}
