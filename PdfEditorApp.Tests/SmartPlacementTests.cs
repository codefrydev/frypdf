using System;
using System.Linq;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class SmartPlacementTests
{
    [Fact]
    public void ViewportCenter_Placement_CalculatesAccurateCoordinates()
    {
        var service = new SmartPlacementService();
        var page = new PageViewModel { Width = 800, Height = 1131 };

        // User is scrolled down looking at center (400, 700)
        service.UpdateViewport(400, 700, 800, 600);

        // Add 400x80 text box
        var (x, y) = service.GetPlacementPosition(page, 400, 80);

        // Expected centered at (400 - 200 = 200, 700 - 40 = 660)
        Assert.Equal(200.0, x);
        Assert.Equal(660.0, y);
    }

    [Fact]
    public void ViewportCenter_Clamping_SafelyKeepsElementsInsidePageMargins()
    {
        var service = new SmartPlacementService();
        var page = new PageViewModel { Width = 800, Height = 1131 };

        // Viewport is scrolled way past the bottom right (1000, 1500)
        service.UpdateViewport(1000, 1500, 800, 600);

        var (x, y) = service.GetPlacementPosition(page, 300, 100);

        // Max X = 800 - 300 - 28 = 472
        // Max Y = 1131 - 100 - 28 = 1003
        Assert.Equal(472.0, x);
        Assert.Equal(1003.0, y);
    }

    [Fact]
    public void ContextMenu_Placement_PositionsNearClickPointer()
    {
        var service = new SmartPlacementService();
        var page = new PageViewModel { Width = 800, Height = 1131 };

        service.UpdateViewport(400, 300, 800, 600);
        service.SetContextMenuPointer(350, 480);

        // Context menu placed 200x150 sticky note
        var (x, y) = service.GetPlacementPosition(page, 200, 150);

        Assert.Equal(320.0, x); // 350 - 30
        Assert.Equal(470.0, y); // 480 - 10

        // After placement, context pointer is consumed and returns to viewport center
        var (nextX, nextY) = service.GetPlacementPosition(page, 200, 150);
        Assert.Equal(300.0, nextX); // Viewport center X (400 - 100)
        Assert.Equal(225.0, nextY); // Viewport center Y (300 - 75)
    }

    [Fact]
    public void ConsecutiveInsertions_ApplyCascadingOffset()
    {
        var service = new SmartPlacementService();
        var page = new PageViewModel { Width = 800, Height = 1131 };

        service.UpdateViewport(400, 500, 800, 600);

        var (x1, y1) = service.GetPlacementPosition(page, 100, 100);
        var (x2, y2) = service.GetPlacementPosition(page, 100, 100);
        var (x3, y3) = service.GetPlacementPosition(page, 100, 100);

        Assert.Equal(350.0, x1);
        Assert.Equal(450.0, y1);

        // Second should be offset by 22pt
        Assert.Equal(372.0, x2);
        Assert.Equal(472.0, y2);

        // Third should be offset by 44pt
        Assert.Equal(394.0, x3);
        Assert.Equal(494.0, y3);

        // Reset cascade
        service.ResetCascade();
        var (x4, y4) = service.GetPlacementPosition(page, 100, 100);
        Assert.Equal(350.0, x4);
        Assert.Equal(450.0, y4);
    }

    [Fact]
    public void MainViewModel_AddTextElement_UsesViewportPosition()
    {
        var mainVm = new MainViewModel();
        Assert.NotNull(mainVm.CurrentPage);

        // Update viewport center to lower half of page
        mainVm.SmartPlacement.UpdateViewport(400, 800, 800, 600);

        int initialCount = mainVm.CurrentPage.Elements.Count;
        mainVm.AddTextElement();

        Assert.Equal(initialCount + 1, mainVm.CurrentPage.Elements.Count);
        var addedText = mainVm.CurrentPage.Elements.Last() as TextElementViewModel;
        Assert.NotNull(addedText);

        // 400x80 text box centered at (400, 800) -> X=200, Y=760
        Assert.Equal(200.0, addedText.X);
        Assert.Equal(760.0, addedText.Y);
        Assert.Same(addedText, mainVm.CurrentPage.SelectedElement);
    }

    [Fact]
    public void MainViewModel_PasteAndDuplicate_UsesSmartPlacement()
    {
        var mainVm = new MainViewModel();
        Assert.NotNull(mainVm.CurrentPage);

        var firstEl = mainVm.CurrentPage.Elements.First();
        mainVm.CurrentPage.SelectElement(firstEl);

        // Duplicate
        mainVm.SmartPlacement.UpdateViewport(400, 600, 800, 600);
        mainVm.Duplicate();

        var duplicated = mainVm.CurrentPage.SelectedElement;
        Assert.NotNull(duplicated);
        Assert.NotSame(firstEl, duplicated);

        // Copy and paste
        mainVm.Copy();
        mainVm.SmartPlacement.UpdateViewport(400, 900, 800, 600);
        mainVm.Paste();

        var pasted = mainVm.CurrentPage.SelectedElement;
        Assert.NotNull(pasted);
        Assert.NotSame(duplicated, pasted);
        Assert.True(pasted.Y > 700); // Placed in the scrolled-down viewport
    }
}
