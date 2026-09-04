using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using CommunityToolkit.Mvvm.Input;

namespace PdfEditorApp.ViewModels;

public partial class PdfViewerViewModel
{
    // --- Interactive Annotations & Review Markups ---

    [RelayCommand]
    public void SetHighlightColor(string? colorHex)
    {
        if (!string.IsNullOrWhiteSpace(colorHex))
        {
            SelectedHighlightColorHex = colorHex;
        }
    }

    [RelayCommand]
    public void AddHighlightAnnotation(string? customColorHex = null)
    {
        var page = Pages.FirstOrDefault(p => p.PageNumber == ActiveSelectedPageNumber) ?? SelectedPage;
        if (page == null) return;
        string color = string.IsNullOrWhiteSpace(customColorHex) ? SelectedHighlightColorHex : customColorHex;
        string textToHighlight = !string.IsNullOrWhiteSpace(ActiveSelectedText) ? ActiveSelectedText : page.SelectedText;

        var highlightRects = new List<Rect>(page.SelectionRects);

        var ann = new PdfViewerAnnotationItem
        {
            Type = "Highlight",
            PageNumber = page.PageNumber,
            Author = "Reader Reviewer",
            Content = !string.IsNullOrWhiteSpace(textToHighlight) ? textToHighlight : $"Highlighted text passage on Page {page.PageNumber}",
            ColorHex = color,
            IconKind = "FormatColorHighlight",
            HighlightRects = highlightRects
        };
        Annotations.Add(ann);
        page.PageAnnotations.Add(ann);
        OnPropertyChanged(nameof(HasAnnotations));
        page.ClearSelection();
        ClearSelection();
        SelectedSidebarTab = PdfViewerSidebarTab.Annotations;
        ShowToastRequested?.Invoke($"Added Highlight on Page {page.PageNumber}");
    }

    [RelayCommand]
    public void HighlightSelectedText(string? customColorHex = null)
    {
        AddHighlightAnnotation(customColorHex);
    }

    [RelayCommand]
    public void OpenAddNoteDialog()
    {
        NewNoteText = string.Empty;
        IsAddNoteOpen = true;
    }

    [RelayCommand]
    public void AddNoteFromSelection()
    {
        if (!string.IsNullOrWhiteSpace(ActiveSelectedText))
        {
            NewNoteText = $"Re: \"{ActiveSelectedText}\"\n\n";
        }
        else
        {
            NewNoteText = string.Empty;
        }
        IsAddNoteOpen = true;
    }

    [RelayCommand]
    public void ConfirmAddNote()
    {
        if (SelectedPage == null || string.IsNullOrWhiteSpace(NewNoteText))
        {
            IsAddNoteOpen = false;
            return;
        }

        var ann = new PdfViewerAnnotationItem
        {
            Type = "StickyNote",
            PageNumber = SelectedPage.PageNumber,
            Author = "Reader Note",
            Content = NewNoteText.Trim(),
            ColorHex = "#38BDF8",
            IconKind = "NoteTextOutline"
        };
        Annotations.Add(ann);
        SelectedPage.PageAnnotations.Add(ann);
        OnPropertyChanged(nameof(HasAnnotations));
        IsAddNoteOpen = false;
        SelectedSidebarTab = PdfViewerSidebarTab.Annotations;
        ShowToastRequested?.Invoke($"Added Sticky Note on Page {SelectedPage.PageNumber}");
    }

    [RelayCommand]
    public void AddStamp(string? stampText)
    {
        if (SelectedPage == null) return;
        string text = string.IsNullOrWhiteSpace(stampText) ? "APPROVED" : stampText;
        string color = text switch
        {
            "REJECTED" => "#EF4444",
            "CONFIDENTIAL" => "#DC2626",
            "DRAFT" => "#F59E0B",
            "FINAL" => "#8B5CF6",
            "REVIEWED" => "#3B82F6",
            _ => "#10B981" // APPROVED
        };

        var ann = new PdfViewerAnnotationItem
        {
            Type = "Stamp",
            PageNumber = SelectedPage.PageNumber,
            Author = "Auditor",
            Content = $"Stamp: {text}",
            ColorHex = color,
            IconKind = "Stamp"
        };
        Annotations.Add(ann);
        SelectedPage.PageAnnotations.Add(ann);
        OnPropertyChanged(nameof(HasAnnotations));
        IsAddStampOpen = false;
        SelectedSidebarTab = PdfViewerSidebarTab.Annotations;
        ShowToastRequested?.Invoke($"Applied '{text}' stamp on Page {SelectedPage.PageNumber}");
    }

    [RelayCommand]
    public void DeleteAnnotation(PdfViewerAnnotationItem? ann)
    {
        if (ann == null) return;
        Annotations.Remove(ann);
        foreach (var p in Pages)
        {
            p.PageAnnotations.Remove(ann);
            p.NotifySelectionChanged();
        }
        OnPropertyChanged(nameof(HasAnnotations));
    }

}
