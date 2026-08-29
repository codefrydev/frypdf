using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels.ElementViewModels;

namespace PdfEditorApp.ViewModels;

public partial class MainViewModel
{
    // --- PAGE COLLECTION & SELECTION COMMANDS ---

    [RelayCommand]
    public void SelectPage(PageViewModel? page)
    {
        if (page == null) return;

        foreach (var p in Pages)
        {
            p.IsSelected = (p == page);
        }

        CurrentPage = page;
        OnPropertyChanged(nameof(CurrentPageNumber));
        OnPropertyChanged(nameof(PageDimensionsDisplay));

        Inspector.UpdateSelection(page.SelectedElement, page);
        UpdateStatus($"Viewing Page {page.PageNumber} of {Pages.Count}");
    }

    [RelayCommand]
    public void SelectSidebarTab(SidebarTabKind tab)
    {
        ActiveSidebarTab = tab;
        if (tab == SidebarTabKind.Outline) RefreshOutline();
        if (tab == SidebarTabKind.Comments) RefreshComments();
    }

    [RelayCommand]
    public void AddPage()
    {
        var newPage = new PageViewModel
        {
            PageNumber = Pages.Count + 1,
            Format = CurrentPage?.Format ?? PageFormat.A4,
            Orientation = CurrentPage?.Orientation ?? PageOrientation.Portrait,
            Width = CurrentPage?.Width ?? 800,
            Height = CurrentPage?.Height ?? 1131,
            FooterRight = $"Page {Pages.Count + 1} of {Pages.Count + 1}"
        };

        newPage.SelectionChanged += OnElementSelectionChanged;
        Pages.Add(newPage);
        SelectPage(newPage);

        int addedIndex = Pages.IndexOf(newPage);
        UndoRedo.RecordAction(
            $"Add Page {newPage.PageNumber}",
            () =>
            {
                if (Pages.Contains(newPage))
                {
                    Pages.Remove(newPage);
                    RenumberPages();
                    if (Pages.Count > 0) SelectPage(Pages[Math.Max(0, addedIndex - 1)]);
                }
            },
            () =>
            {
                if (!Pages.Contains(newPage))
                {
                    Pages.Insert(Math.Min(addedIndex, Pages.Count), newPage);
                    RenumberPages();
                    SelectPage(newPage);
                }
            }
        );

        ShowToast($"Added Page {newPage.PageNumber}", "FilePlusOutline");
    }

    [RelayCommand]
    public void DuplicateCurrentPage()
    {
        if (CurrentPage == null) return;
        var model = CurrentPage.ToModel();
        var clonedPage = new PageViewModel();
        clonedPage.LoadFromModel(model);
        clonedPage.PageNumber = Pages.Count + 1;
        clonedPage.SelectionChanged += OnElementSelectionChanged;
        Pages.Add(clonedPage);
        SelectPage(clonedPage);

        int addedIndex = Pages.IndexOf(clonedPage);
        UndoRedo.RecordAction(
            $"Duplicate Page {CurrentPage.PageNumber}",
            () =>
            {
                if (Pages.Contains(clonedPage))
                {
                    Pages.Remove(clonedPage);
                    RenumberPages();
                    if (Pages.Count > 0) SelectPage(Pages[Math.Max(0, addedIndex - 1)]);
                }
            },
            () =>
            {
                if (!Pages.Contains(clonedPage))
                {
                    Pages.Insert(Math.Min(addedIndex, Pages.Count), clonedPage);
                    RenumberPages();
                    SelectPage(clonedPage);
                }
            }
        );

        ShowToast($"Duplicated Page {CurrentPage.PageNumber}", "FileMultipleOutline");
    }

    [RelayCommand]
    public void DeleteCurrentPage()
    {
        if (Pages.Count <= 1 || CurrentPage == null)
        {
            ShowToast("Cannot delete the only page in the document", "AlertCircleOutline");
            return;
        }

        int index = Pages.IndexOf(CurrentPage);
        var removedPage = CurrentPage;
        Pages.Remove(removedPage);
        RenumberPages();

        int newIndex = Math.Min(index, Pages.Count - 1);
        SelectPage(Pages[newIndex]);

        UndoRedo.RecordAction(
            $"Delete Page {removedPage.PageNumber}",
            () =>
            {
                Pages.Insert(Math.Min(index, Pages.Count), removedPage);
                RenumberPages();
                SelectPage(removedPage);
            },
            () =>
            {
                if (Pages.Contains(removedPage))
                {
                    Pages.Remove(removedPage);
                    RenumberPages();
                    SelectPage(Pages[Math.Min(index, Pages.Count - 1)]);
                }
            }
        );

        ShowToast($"Deleted Page {index + 1}", "DeleteOutline");
    }

    [RelayCommand]
    public void RotateCurrentPage()
    {
        if (CurrentPage == null) return;
        var targetPage = CurrentPage;
        int oldAngle = targetPage.RotationAngle;
        targetPage.RotateClockwise();
        int newAngle = targetPage.RotationAngle;

        UndoRedo.RecordAction(
            $"Rotate Page {targetPage.PageNumber}",
            () => targetPage.RotationAngle = oldAngle,
            () => targetPage.RotationAngle = newAngle
        );

        ShowToast($"Page rotated to {newAngle}°", "RotateRight");
    }

    [RelayCommand]
    public void RotateCurrentPageCounterClockwise()
    {
        if (CurrentPage == null) return;
        var targetPage = CurrentPage;
        int oldAngle = targetPage.RotationAngle;
        targetPage.RotationAngle = (targetPage.RotationAngle + 270) % 360;
        int newAngle = targetPage.RotationAngle;

        UndoRedo.RecordAction(
            $"Rotate Page {targetPage.PageNumber}",
            () => targetPage.RotationAngle = oldAngle,
            () => targetPage.RotationAngle = newAngle
        );

        ShowToast($"Page rotated to {newAngle}°", "RotateLeft");
    }

    [RelayCommand]
    public void MovePageUp()
    {
        if (CurrentPage == null) return;
        int idx = Pages.IndexOf(CurrentPage);
        if (idx > 0)
        {
            Pages.Move(idx, idx - 1);
            RenumberPages();
            SelectPage(Pages[idx - 1]);

            UndoRedo.RecordAction(
                $"Move Page {idx + 1} Up",
                () => { Pages.Move(idx - 1, idx); RenumberPages(); SelectPage(Pages[idx]); },
                () => { Pages.Move(idx, idx - 1); RenumberPages(); SelectPage(Pages[idx - 1]); }
            );

            ShowToast($"Moved Page {idx + 1} to position {idx}", "ChevronUp");
        }
    }

    [RelayCommand]
    public void MovePageDown()
    {
        if (CurrentPage == null) return;
        int idx = Pages.IndexOf(CurrentPage);
        if (idx < Pages.Count - 1)
        {
            Pages.Move(idx, idx + 1);
            RenumberPages();
            SelectPage(Pages[idx + 1]);

            UndoRedo.RecordAction(
                $"Move Page {idx + 1} Down",
                () => { Pages.Move(idx + 1, idx); RenumberPages(); SelectPage(Pages[idx]); },
                () => { Pages.Move(idx, idx + 1); RenumberPages(); SelectPage(Pages[idx + 1]); }
            );

            ShowToast($"Moved Page {idx + 1} to position {idx + 2}", "ChevronDown");
        }
    }

    // --- PAGE NAVIGATION SHORTCUT COMMANDS ---

    [RelayCommand]
    public void NextPage()
    {
        if (CurrentPage == null || Pages.Count == 0) return;
        int idx = Pages.IndexOf(CurrentPage);
        if (idx < Pages.Count - 1)
        {
            SelectPage(Pages[idx + 1]);
        }
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if (CurrentPage == null || Pages.Count == 0) return;
        int idx = Pages.IndexOf(CurrentPage);
        if (idx > 0)
        {
            SelectPage(Pages[idx - 1]);
        }
    }

    [RelayCommand]
    public void FirstPage()
    {
        if (Pages.Count > 0)
        {
            SelectPage(Pages[0]);
        }
    }

    [RelayCommand]
    public void LastPage()
    {
        if (Pages.Count > 0)
        {
            SelectPage(Pages[^1]);
        }
    }

    private void RenumberPages()
    {
        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].PageNumber = i + 1;
        }
        OnPropertyChanged(nameof(CurrentPageNumber));
        OnPropertyChanged(nameof(TotalPagesCount));
    }

    // --- OUTLINE & COMMENTS STREAM SYNC ---

    public void RefreshOutline()
    {
        OutlineItems.Clear();
        for (int i = 0; i < Pages.Count; i++)
        {
            var p = Pages[i];
            var headings = p.Elements.OfType<TextElementViewModel>()
                .Where(t => t.IsBold || t.FontSize >= 16)
                .ToList();

            if (headings.Count > 0)
            {
                foreach (var h in headings)
                {
                    OutlineItems.Add(new OutlineItem
                    {
                        Title = string.IsNullOrWhiteSpace(h.Text) ? "Section Header" : h.Text.Trim(),
                        PageIndex = i + 1,
                        Kind = h.FontSize >= 20 ? "Heading 1" : "Heading 2"
                    });
                }
            }
            else
            {
                OutlineItems.Add(new OutlineItem
                {
                    Title = $"Page {i + 1}",
                    PageIndex = i + 1,
                    Kind = "Page"
                });
            }
        }
    }

    public void RefreshComments()
    {
        CommentItems.Clear();
        for (int i = 0; i < Pages.Count; i++)
        {
            var p = Pages[i];
            var stickies = p.Elements.OfType<StickyNoteElementViewModel>().ToList();
            foreach (var note in stickies)
            {
                CommentItems.Add(new CommentItem
                {
                    Author = note.Author,
                    Timestamp = "Today, 10:30 AM",
                    Text = note.NoteText,
                    Status = note.Status,
                    PageIndex = i + 1
                });
            }
        }
    }

    [RelayCommand]
    public void JumpToOutlineItem(OutlineItem? item)
    {
        if (item != null && item.PageIndex >= 1 && item.PageIndex <= Pages.Count)
        {
            SelectPage(Pages[item.PageIndex - 1]);
        }
    }

    [RelayCommand]
    public void JumpToCommentItem(CommentItem? item)
    {
        if (item != null && item.PageIndex >= 1 && item.PageIndex <= Pages.Count)
        {
            SelectPage(Pages[item.PageIndex - 1]);
        }
    }
}
