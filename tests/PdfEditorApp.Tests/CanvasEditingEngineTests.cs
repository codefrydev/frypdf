using System;
using System.Linq;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class CanvasEditingEngineTests
{
    [Fact]
    public void PageViewModel_MultiSelection_TogglesAndCalculatesBoundingBoxCorrectly()
    {
        // Arrange
        var page = new PageViewModel { Width = 595.28, Height = 841.89 };
        var el1 = new TextElementViewModel { X = 50, Y = 100, Width = 150, Height = 40, Text = "Title 1" };
        var el2 = new TextElementViewModel { X = 250, Y = 200, Width = 100, Height = 60, Text = "Title 2" };
        page.AddElement(el1);
        page.AddElement(el2);

        // Act - Single select
        page.SelectElement(el1);
        Assert.Single(page.SelectedElements);
        Assert.Same(el1, page.SelectedElement);
        Assert.False(page.HasMultiSelection);
        Assert.Equal(1, page.SelectionCount);

        // Act - Multi select (toggle second)
        page.ToggleElementSelection(el2);
        Assert.Equal(2, page.SelectedElements.Count);
        Assert.True(page.HasMultiSelection);
        Assert.Equal(2, page.SelectionCount);

        // Assert - Bounding box spans both elements
        // Min X = 50, Min Y = 100, Max X = 250+100=350, Max Y = 200+60=260
        // Width = 350-50 = 300, Height = 260-100 = 160
        var bbox = page.SelectionBoundingBox;
        Assert.Equal(50, bbox.X);
        Assert.Equal(100, bbox.Y);
        Assert.Equal(300, bbox.Width);
        Assert.Equal(160, bbox.Height);

        // Act - Select All
        page.SelectAll();
        Assert.Equal(2, page.SelectedElements.Count);

        // Act - Clear Selection
        page.ClearSelection();
        Assert.Empty(page.SelectedElements);
        Assert.Null(page.SelectedElement);
        Assert.False(page.HasMultiSelection);
    }

    [Fact]
    public void InspectorViewModel_MathematicalAlignment_AlignsMultiSelectionCorrectly()
    {
        // Arrange
        var undoRedo = new UndoRedoService();
        var inspector = new InspectorViewModel { UndoRedo = undoRedo };
        var page = new PageViewModel { Width = 600, Height = 800 };

        var el1 = new TextElementViewModel { X = 100, Y = 100, Width = 100, Height = 50 };
        var el2 = new TextElementViewModel { X = 200, Y = 200, Width = 120, Height = 40 };
        var el3 = new TextElementViewModel { X = 50, Y = 300, Width = 80, Height = 60 };

        page.AddElement(el1);
        page.AddElement(el2);
        page.AddElement(el3);
        page.SelectElements(new[] { el1, el2, el3 });
        inspector.UpdateSelection(el1, page);

        // Act - Align Left (should all align to min X = 50)
        inspector.AlignLeft();
        Assert.Equal(50, el1.X);
        Assert.Equal(50, el2.X);
        Assert.Equal(50, el3.X);

        // Undo Align Left
        undoRedo.Undo();
        Assert.Equal(100, el1.X);
        Assert.Equal(200, el2.X);
        Assert.Equal(50, el3.X);

        // Act - Align Right (Max right edge is max(100+100, 200+120, 50+80) = 320)
        inspector.AlignRight();
        Assert.Equal(320 - 100, el1.X); // 220
        Assert.Equal(320 - 120, el2.X); // 200
        Assert.Equal(320 - 80, el3.X);  // 240

        // Act - Align Top (Min Y = 100)
        inspector.AlignTop();
        Assert.Equal(100, el1.Y);
        Assert.Equal(100, el2.Y);
        Assert.Equal(100, el3.Y);
    }

    [Fact]
    public void InspectorViewModel_DistributeHorizontallyAndVertically_SpacesElementsEqually()
    {
        // Arrange
        var undoRedo = new UndoRedoService();
        var inspector = new InspectorViewModel { UndoRedo = undoRedo };
        var page = new PageViewModel { Width = 1000, Height = 1000 };

        // 3 elements of width 50
        // Leftmost X = 100, Rightmost X = 500 (Right edge = 550)
        // Middle element initially at X = 200
        // Span = 550 - 100 = 450. Total element widths = 150. Total gaps = 300.
        // Gap per space (2 spaces) = 300 / 2 = 150.
        // Expected X positions: El1: 100, El2: 100 + 50 + 150 = 300, El3: 500
        var el1 = new TextElementViewModel { X = 100, Y = 50, Width = 50, Height = 30 };
        var el2 = new TextElementViewModel { X = 200, Y = 50, Width = 50, Height = 30 };
        var el3 = new TextElementViewModel { X = 500, Y = 50, Width = 50, Height = 30 };

        page.AddElement(el1);
        page.AddElement(el2);
        page.AddElement(el3);
        page.SelectElements(new[] { el1, el2, el3 });
        inspector.UpdateSelection(el1, page);

        // Act - Distribute Horizontally
        inspector.DistributeHorizontally();

        Assert.Equal(100, el1.X);
        Assert.Equal(300, el2.X);
        Assert.Equal(500, el3.X);

        // Act - Undo
        undoRedo.Undo();
        Assert.Equal(200, el2.X);

        // Act - Redo
        undoRedo.Redo();
        Assert.Equal(300, el2.X);
    }

    [Fact]
    public void InspectorViewModel_BatchDeleteAndDuplicate_MaintainsAtomicUndoRedo()
    {
        // Arrange
        var undoRedo = new UndoRedoService();
        var inspector = new InspectorViewModel { UndoRedo = undoRedo };
        var page = new PageViewModel { Width = 600, Height = 800 };

        var el1 = new TextElementViewModel { X = 50, Y = 50, Width = 100, Height = 30, Text = "Elem 1" };
        var el2 = new TextElementViewModel { X = 50, Y = 100, Width = 100, Height = 30, Text = "Elem 2" };

        page.AddElement(el1);
        page.AddElement(el2);
        page.SelectElements(new[] { el1, el2 });
        inspector.UpdateSelection(el1, page);

        // Act - Duplicate Selected Elements (Batch)
        inspector.DuplicateSelectedElement();
        Assert.Equal(4, page.Elements.Count);
        Assert.Equal(2, page.SelectedElements.Count); // Newly duplicated are selected

        // Undo duplication
        undoRedo.Undo();
        Assert.Equal(2, page.Elements.Count);

        // Redo duplication
        undoRedo.Redo();
        Assert.Equal(4, page.Elements.Count);

        // Act - Delete All 4 elements
        page.SelectAll();
        inspector.UpdateSelection(page.SelectedElement, page);
        inspector.DeleteSelectedElement();
        Assert.Empty(page.Elements);

        // Undo deletion
        undoRedo.Undo();
        Assert.Equal(4, page.Elements.Count);
    }

    [Fact]
    public void InspectorViewModel_SingleElementAlignment_AlignsRelativeToPage()
    {
        // Arrange
        var undoRedo = new UndoRedoService();
        var inspector = new InspectorViewModel { UndoRedo = undoRedo };
        var page = new PageViewModel { Width = 600, Height = 800 };
        var el = new TextElementViewModel { X = 250, Y = 350, Width = 100, Height = 50 };
        page.AddElement(el);
        page.SelectElement(el);
        inspector.UpdateSelection(el, page);

        // Align Left (60 margin)
        inspector.AlignLeft();
        Assert.Equal(60, el.X);

        // Align Right (600 - 100 - 60 = 440)
        inspector.AlignRight();
        Assert.Equal(440, el.X);

        // Align Center ((600 - 100) / 2 = 250)
        inspector.AlignCenter();
        Assert.Equal(250, el.X);

        // Align Top (60 margin)
        inspector.AlignTop();
        Assert.Equal(60, el.Y);

        // Align Bottom (800 - 50 - 60 = 690)
        inspector.AlignBottom();
        Assert.Equal(690, el.Y);

        // Align Middle ((800 - 50) / 2 = 375)
        inspector.AlignMiddle();
        Assert.Equal(375, el.Y);
    }

    [Fact]
    public void InspectorViewModel_BatchLockToggle_LocksAndUnlocksAllSelected()
    {
        // Arrange
        var undoRedo = new UndoRedoService();
        var inspector = new InspectorViewModel { UndoRedo = undoRedo };
        var page = new PageViewModel { Width = 600, Height = 800 };
        var el1 = new TextElementViewModel { X = 10, Y = 10, Width = 50, Height = 20, IsLocked = false };
        var el2 = new TextElementViewModel { X = 20, Y = 20, Width = 50, Height = 20, IsLocked = false };
        page.AddElement(el1);
        page.AddElement(el2);
        page.SelectElements(new[] { el1, el2 });
        inspector.UpdateSelection(el1, page);

        // Act - Lock All
        inspector.ToggleLock();
        Assert.True(el1.IsLocked);
        Assert.True(el2.IsLocked);

        // Undo
        undoRedo.Undo();
        Assert.False(el1.IsLocked);
        Assert.False(el2.IsLocked);

        // Redo
        undoRedo.Redo();
        Assert.True(el1.IsLocked);
        Assert.True(el2.IsLocked);
    }

    [Fact]
    public void MainViewModel_DynamicZoomAndFit_CalculatesAccurately()
    {
        // Arrange
        var vm = new MainViewModel();

        // Standard A4 page width = 595.28 pt, height = 841.89 pt
        Assert.NotNull(vm.CurrentPage);

        // Act - Zoom in and out
        vm.ZoomLevel = 1.0;
        vm.ZoomIn();
        Assert.True(vm.ZoomLevel > 1.0);

        vm.ZoomOut();
        Assert.Equal(1.0, vm.ZoomLevel);

        // Act - Fit to Width with viewport width = 2.0x
        double targetWidth = vm.CurrentPage.Width * 2.0 + 64.0;
        vm.FitToWidthDynamic(targetWidth);
        Assert.Equal(2.0, vm.ZoomLevel);

        // Act - Fit to Page with viewport = 1.0x
        double targetPageW = vm.CurrentPage.Width * 1.0 + 64.0;
        double targetPageH = vm.CurrentPage.Height * 1.0 + 64.0;
        vm.FitToPageDynamic(targetPageW, targetPageH);
        Assert.Equal(1.0, vm.ZoomLevel);

        // Act - Reset zoom
        vm.ResetZoom();
        Assert.Equal(1.0, vm.ZoomLevel);
    }

    [Fact]
    public void PdfTextElement_And_ViewModel_DefaultPadding_IsZero()
    {
        var model = new PdfTextElement();
        Assert.Equal(0.0, model.Padding);

        var vm = new TextElementViewModel();
        Assert.Equal(0.0, vm.Padding);
    }

    [Fact]
    public void TextElementViewModel_CalculateRequiredHeight_ProvidesAdequateHeight()
    {
        var vm = new TextElementViewModel
        {
            Text = "I. INTRODUCTION & EXPERIMENTAL SETUP",
            FontSize = 11,
            FontFamily = "Segoe UI",
            Width = 330,
            Height = 15 // Deliberately tight/too small
        };

        double requiredHeight = vm.CalculateRequiredHeight();
        Assert.True(requiredHeight >= 18.0, $"Required height should be >= 18 for 11pt font, was {requiredHeight}");

        vm.AutoFitHeight();
        Assert.True(vm.Height >= 18.0);
    }

    [Fact]
    public void InspectorViewModel_AutoFitTextHeight_ExpandsSelectedElementsWithUndoRedo()
    {
        var undoRedo = new UndoRedoService();
        var inspector = new InspectorViewModel { UndoRedo = undoRedo };
        var page = new PageViewModel { Width = 600, Height = 800 };
        var textEl = new TextElementViewModel
        {
            Text = "Multi-Line\nAffiliation and Department\nUniversity of Science",
            FontSize = 10,
            Width = 200,
            Height = 15 // Clipped
        };
        page.AddElement(textEl);
        page.SelectElement(textEl);
        inspector.UpdateSelection(textEl, page);

        inspector.AutoFitTextHeight();
        Assert.True(textEl.Height > 30.0, $"Height should expand for 3 lines of text, but was {textEl.Height}");

        // Test Undo
        undoRedo.Undo();
        Assert.Equal(15.0, textEl.Height);

        // Test Redo
        undoRedo.Redo();
        Assert.True(textEl.Height > 30.0);
    }

    [Fact]
    public void AllTemplates_CreateValidDocuments_WithZeroPaddingAndValidTextBounds()
    {
        var templateService = new TemplateService();
        var templates = templateService.GetAllTemplates();

        Assert.NotEmpty(templates);
        Assert.Equal(18, templates.Count());

        foreach (var def in templates)
        {
            var doc = def.Create();
            Assert.NotNull(doc);
            Assert.NotEmpty(doc.Pages);

            foreach (var p in doc.Pages)
            {
                Assert.True(p.Width > 0);
                Assert.True(p.Height > 0);
                Assert.NotEmpty(p.Elements);

                foreach (var el in p.Elements.OfType<PdfTextElement>())
                {
                    Assert.True(el.Width > 0, $"Text element '{el.Text}' in template '{def.Name}' has non-positive width");
                    Assert.True(el.Height >= 18, $"Text element '{el.Text}' in template '{def.Name}' has height {el.Height} which is too small and might clip");
                }
            }
        }
    }
}
