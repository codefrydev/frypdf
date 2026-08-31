using System;
using System.Linq;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;
using PdfEditorApp.ViewModels;
using PdfEditorApp.ViewModels.ElementViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class DirectEditingTests
{
    [Fact]
    public void TextElement_EditMode_TransitionsCorrectly()
    {
        var textEl = new TextElementViewModel
        {
            Text = "Original Content",
            FontSize = 14,
            IsInEditMode = false
        };

        Assert.False(textEl.IsInEditMode);

        // Enter edit mode
        textEl.IsInEditMode = true;
        Assert.True(textEl.IsInEditMode);

        // Edit text
        textEl.Text = "Updated Direct Canvas Text";
        Assert.Equal("Updated Direct Canvas Text", textEl.Text);

        // Exit edit mode
        textEl.IsInEditMode = false;
        Assert.False(textEl.IsInEditMode);
    }

    [Fact]
    public void InspectorViewModel_FontSize_IncreaseAndDecreaseCommands()
    {
        var inspector = new InspectorViewModel();
        var page = new PageViewModel();
        var textEl = new TextElementViewModel
        {
            Text = "Headline Text",
            FontSize = 16
        };
        page.AddElement(textEl);
        inspector.UpdateSelection(textEl, page);

        Assert.True(inspector.IsTextElement);
        Assert.Equal(16, textEl.FontSize);

        // Increase font size
        inspector.IncreaseFontSizeCommand.Execute(null);
        Assert.Equal(17, textEl.FontSize);

        // Increase again
        inspector.IncreaseFontSizeCommand.Execute(null);
        Assert.Equal(18, textEl.FontSize);

        // Decrease font size
        inspector.DecreaseFontSizeCommand.Execute(null);
        Assert.Equal(17, textEl.FontSize);

        // Test bounds and finish edit mode
        textEl.IsInEditMode = false;
        Assert.False(textEl.IsInEditMode);

        inspector.StartEditModeCommand.Execute(null);
        Assert.True(textEl.IsInEditMode);

        inspector.ToggleEditModeCommand.Execute(null);
        Assert.False(textEl.IsInEditMode);

        inspector.ToggleEditModeCommand.Execute(null);
        Assert.True(textEl.IsInEditMode);

        inspector.FinishEditModeCommand.Execute(null);
        Assert.False(textEl.IsInEditMode);
    }

    [Fact]
    public void InspectorViewModel_TextFormattingCommands_WorkDirectly()
    {
        var inspector = new InspectorViewModel();
        var page = new PageViewModel();
        var textEl = new TextElementViewModel
        {
            Text = "Sample Paragraph",
            FontSize = 12,
            IsBold = false,
            IsItalic = false,
            IsUnderline = false,
            Alignment = TextAlignmentMode.Left
        };
        page.AddElement(textEl);
        inspector.UpdateSelection(textEl, page);

        // Toggle Bold
        inspector.ToggleBoldCommand.Execute(null);
        Assert.True(textEl.IsBold);

        // Toggle Italic
        inspector.ToggleItalicCommand.Execute(null);
        Assert.True(textEl.IsItalic);

        // Toggle Underline
        inspector.ToggleUnderlineCommand.Execute(null);
        Assert.True(textEl.IsUnderline);

        // Change Alignment
        inspector.SetAlignmentCommand.Execute("Center");
        Assert.Equal(TextAlignmentMode.Center, textEl.Alignment);

        // Change Color
        inspector.SetTextColorCommand.Execute("#0F6CBD");
        Assert.Equal("#0F6CBD", textEl.TextColorHex);

        // Auto-Fit Text
        inspector.AutoFitTextBothCommand.Execute(null);
        Assert.True(textEl.Width > 0);
        Assert.True(textEl.Height > 0);
    }

    [Fact]
    public void ShapeElement_InPlaceLabelEditing_WorksCorrectly()
    {
        var shape = new ShapeElementViewModel
        {
            ShapeType = ShapeType.RoundedRectangle,
            Label = "CONFIDENTIAL",
            LabelFontSize = 14,
            IsInEditMode = false
        };

        Assert.False(shape.IsInEditMode);
        Assert.Equal("CONFIDENTIAL", shape.Label);

        // Enter label edit mode
        shape.IsInEditMode = true;
        Assert.True(shape.IsInEditMode);

        // Edit label directly
        shape.Label = "APPROVED";
        Assert.Equal("APPROVED", shape.Label);

        // Exit edit mode
        shape.IsInEditMode = false;
        Assert.False(shape.IsInEditMode);
    }

    [Fact]
    public void TableElement_DirectCellAndHeaderEditing_RoundTripsModel()
    {
        var table = new TableElementViewModel();
        Assert.Equal(4, table.Headers.Count);
        Assert.Equal(3, table.Rows.Count);

        // Direct edit header
        table.Headers[0].Text = "Service Description";
        Assert.Equal("Service Description", table.Headers[0].Text);

        // Direct edit cell
        table.Rows[0].Cells[0].Text = "Enterprise Architecture Consulting";
        table.Rows[0].Cells[3].Text = "$12,500.00";

        Assert.Equal("Enterprise Architecture Consulting", table.Rows[0].Cells[0].Text);
        Assert.Equal("$12,500.00", table.Rows[0].Cells[3].Text);

        // Convert to Model and back
        var model = (PdfTableElement)table.ToModel();
        Assert.Equal("Service Description", model.Headers[0]);
        Assert.Equal("Enterprise Architecture Consulting", model.Rows[0][0]);
        Assert.Equal("$12,500.00", model.Rows[0][3]);

        var restoredTable = new TableElementViewModel();
        restoredTable.LoadFromModel(model);

        Assert.Equal("Service Description", restoredTable.Headers[0].Text);
        Assert.Equal("Enterprise Architecture Consulting", restoredTable.Rows[0].Cells[0].Text);
        Assert.Equal("$12,500.00", restoredTable.Rows[0].Cells[3].Text);
    }

    [Fact]
    public void StickyNoteElement_DirectTextEditing_WorksCorrectly()
    {
        var note = new StickyNoteElementViewModel
        {
            Author = "Reviewer",
            NoteText = "Please update section 3"
        };

        note.NoteText = "Section 3 approved with revisions.";
        var model = (PdfStickyNoteElement)note.ToModel();
        Assert.Equal("Section 3 approved with revisions.", model.NoteText);
    }

    [Fact]
    public void PageViewModel_SelectionChange_DeactivatesEditModeOnOtherElements()
    {
        var page = new PageViewModel();
        var el1 = new TextElementViewModel { Text = "Element 1", IsInEditMode = true };
        var el2 = new TextElementViewModel { Text = "Element 2", IsInEditMode = false };
        page.AddElement(el1);
        page.AddElement(el2);

        page.SelectElement(el1);
        el1.IsInEditMode = true;
        Assert.True(el1.IsSelected);
        Assert.True(el1.IsInEditMode);

        // Selecting el2 must automatically deactivate edit mode on el1
        page.SelectElement(el2);
        Assert.False(el1.IsSelected);
        Assert.False(el1.IsInEditMode);
        Assert.True(el2.IsSelected);

        // Clearing selection must deactivate edit mode on all elements
        el2.IsInEditMode = true;
        Assert.True(el2.IsInEditMode);
        page.ClearSelection();
        Assert.False(el2.IsSelected);
        Assert.False(el2.IsInEditMode);
    }
}

