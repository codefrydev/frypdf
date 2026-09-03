using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
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
        if (IsLeftSidebarCollapsed)
        {
            IsLeftSidebarCollapsed = false;
        }
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

    public void ReorderPage(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Pages.Count || toIndex < 0 || toIndex >= Pages.Count || fromIndex == toIndex)
            return;

        Pages.Move(fromIndex, toIndex);
        RenumberPages();
        SelectPage(Pages[toIndex]);

        UndoRedo.RecordAction(
            $"Reorder Page {fromIndex + 1} to {toIndex + 1}",
            () => { Pages.Move(toIndex, fromIndex); RenumberPages(); SelectPage(Pages[fromIndex]); },
            () => { Pages.Move(fromIndex, toIndex); RenumberPages(); SelectPage(Pages[toIndex]); }
        );

        ShowToast($"Reordered Page {fromIndex + 1} to position {toIndex + 1}", "SwapVertical");
    }

    // --- PAGE NAVIGATION SHORTCUT COMMANDS ---

    [RelayCommand]
    public void NextPage()
    {
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.NextPage();
            return;
        }
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
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.PreviousPage();
            return;
        }
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
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.FirstPage();
            return;
        }
        if (Pages.Count > 0)
        {
            SelectPage(Pages[0]);
        }
    }

    [RelayCommand]
    public void LastPage()
    {
        if (IsPdfViewerVisible && PdfViewer != null)
        {
            PdfViewer.LastPage();
            return;
        }
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

    // --- HEADER & FOOTER MANAGER ---

    [RelayCommand]
    public void OpenHeaderFooterDialog()
    {
        if (CurrentPage != null)
        {
            HeaderLeftText = CurrentPage.HeaderLeft ?? "";
            HeaderRightText = CurrentPage.HeaderRight ?? "";
            FooterLeftText = CurrentPage.FooterLeft ?? "CONFIDENTIAL & PROPRIETARY";
            FooterRightText = CurrentPage.FooterRight ?? "Page {P} of {N}";
        }
        IsHeaderFooterDialogOpen = true;
    }

    [RelayCommand]
    public void CloseHeaderFooterDialog()
    {
        IsHeaderFooterDialogOpen = false;
    }

    [RelayCommand]
    public void ApplyHeaderFooterToAllPages()
    {
        for (int i = 0; i < Pages.Count; i++)
        {
            var p = Pages[i];
            p.ShowHeaderFooter = true;
            p.HeaderLeft = ExpandMacros(HeaderLeftText, i + 1, Pages.Count);
            p.HeaderRight = ExpandMacros(HeaderRightText, i + 1, Pages.Count);
            p.FooterLeft = ExpandMacros(FooterLeftText, i + 1, Pages.Count);
            p.FooterRight = ExpandMacros(FooterRightText, i + 1, Pages.Count);
        }

        CloseHeaderFooterDialog();
        ShowToast($"Updated headers & footers across {Pages.Count} pages", "PageLayoutHeaderFooter");
    }

    private string ExpandMacros(string? template, int pageNum, int totalPages)
    {
        if (string.IsNullOrEmpty(template)) return "";
        return template
            .Replace("{P}", pageNum.ToString())
            .Replace("{N}", totalPages.ToString())
            .Replace("[PageNumber]", pageNum.ToString())
            .Replace("[TotalPages]", totalPages.ToString())
            .Replace("[Title]", DocumentTitle)
            .Replace("[Author]", DocumentAuthor)
            .Replace("[Date]", DateTime.Now.ToString("yyyy-MM-dd"));
    }

    // --- DOCUMENT SECURITY & SANITIZATION ---

    [RelayCommand]
    public void OpenSecurityDialog()
    {
        IsSecurityDialogOpen = true;
    }

    [RelayCommand]
    public void CloseSecurityDialog()
    {
        IsSecurityDialogOpen = false;
        OnPropertyChanged(nameof(SecurityStatusDisplay));
    }

    [RelayCommand]
    public void SanitizeDocument()
    {
        DocumentAuthor = "Anonymous";
        DocumentSubject = "";

        int commentsRemoved = 0;
        foreach (var page in Pages)
        {
            var notes = page.Elements.OfType<StickyNoteElementViewModel>().ToList();
            foreach (var note in notes)
            {
                page.RemoveElement(note);
                commentsRemoved++;
            }
        }

        SecuritySettings.ScrubMetadataOnExport = true;
        SecuritySettings.RemoveCommentsOnExport = true;

        RefreshComments();
        ShowToast($"Sanitized document (cleared metadata & removed {commentsRemoved} internal review notes)", "ShieldCheck");
    }

    // --- PAGE SETUP & GEOMETRY COMMANDS ---

    [RelayCommand]
    public void SetPageFormat(string formatStr)
    {
        if (CurrentPage == null) return;
        if (Enum.TryParse<PageFormat>(formatStr, true, out var format))
        {
            CurrentPage.Format = format;
            double w = format switch
            {
                PageFormat.A4 => 800,
                PageFormat.Letter => 816,
                PageFormat.Legal => 816,
                PageFormat.Executive => 700,
                PageFormat.A3 => 1131,
                PageFormat.A5 => 565,
                _ => 800
            };
            double h = format switch
            {
                PageFormat.A4 => 1131,
                PageFormat.Letter => 1056,
                PageFormat.Legal => 1344,
                PageFormat.Executive => 950,
                PageFormat.A3 => 1600,
                PageFormat.A5 => 800,
                _ => 1131
            };

            if (CurrentPage.Orientation == PageOrientation.Landscape)
            {
                (w, h) = (h, w);
            }

            CurrentPage.Width = w;
            CurrentPage.Height = h;
            OnPropertyChanged(nameof(PageDimensionsDisplay));
            ShowToast($"Page format changed to {format}", "AspectRatio");
        }
    }

    [RelayCommand]
    public void SetPageOrientation(string orientationStr)
    {
        if (CurrentPage == null) return;
        if (Enum.TryParse<PageOrientation>(orientationStr, true, out var orient))
        {
            if (CurrentPage.Orientation != orient)
            {
                CurrentPage.Orientation = orient;
                (CurrentPage.Width, CurrentPage.Height) = (CurrentPage.Height, CurrentPage.Width);
                OnPropertyChanged(nameof(PageDimensionsDisplay));
                ShowToast($"Orientation changed to {orient}", "PhoneRotateLandscape");
            }
        }
    }

    // --- ORGANIZE: SPLIT, EXTRACT & BATCH ROTATION COMMANDS ---

    [RelayCommand]
    public void OpenSplitExtractDialog()
    {
        IsSplitExtractDialogOpen = true;
    }

    [RelayCommand]
    public void CloseSplitExtractDialog()
    {
        IsSplitExtractDialogOpen = false;
    }

    [RelayCommand]
    public void SetSplitExtractMode(SplitExtractMode mode)
    {
        SplitExtractMode = mode;
    }

    [RelayCommand]
    public void ExecuteSplitExtract()
    {
        IsSplitExtractDialogOpen = false;
        if (Pages.Count == 0) return;

        var currentDoc = ToDocumentModel();

        if (SplitExtractMode == SplitExtractMode.ExtractSelectedPages)
        {
            if (CurrentPage != null)
            {
                int currentIdx = Math.Clamp(Pages.IndexOf(CurrentPage), 0, Pages.Count - 1);
                var extracted = _pageOrganizerService.ExtractPages(currentDoc, new[] { currentIdx });
                LoadFromDocumentModel(extracted);
                ShowToast($"Extracted Page {currentIdx + 1} to new active project", "CallSplit");
            }
        }
        else if (SplitExtractMode == SplitExtractMode.SplitEveryNPages)
        {
            int interval = Math.Max(1, SplitPageInterval);
            var parts = _pageOrganizerService.SplitEveryNPages(currentDoc, interval);
            if (parts.Count > 0)
            {
                // Open first split part as active document
                LoadFromDocumentModel(parts[0]);
                ShowToast($"Document split into {parts.Count} parts (Loaded Part 1 of {parts.Count})", "CallSplit");
            }
        }
        else if (SplitExtractMode == SplitExtractMode.SplitByPageRanges)
        {
            var parts = _pageOrganizerService.SplitByRanges(currentDoc, SplitPageRanges);
            if (parts.Count > 0)
            {
                LoadFromDocumentModel(parts[0]);
                ShowToast($"Split document into {parts.Count} ranges (Loaded Range 1)", "CallSplit");
            }
            else
            {
                ShowToast("Invalid page ranges specified (e.g. use '1-2, 3-4')", "AlertCircleOutline");
            }
        }
    }

    [RelayCommand]
    public void BatchRotatePages(string target)
    {
        var targetEnum = target.ToLowerInvariant() switch
        {
            "all" => PageFilterTarget.All,
            "even" => PageFilterTarget.EvenPages,
            "odd" => PageFilterTarget.OddPages,
            "landscape" => PageFilterTarget.LandscapePages,
            "portrait" => PageFilterTarget.PortraitPages,
            _ => PageFilterTarget.All
        };

        var doc = ToDocumentModel();
        int rotatedCount = _pageOrganizerService.BatchRotatePages(doc, targetEnum, 90);
        if (rotatedCount > 0)
        {
            for (int i = 0; i < doc.Pages.Count && i < Pages.Count; i++)
            {
                Pages[i].RotationAngle = doc.Pages[i].RotationAngle;
            }
            ShowToast($"Rotated {rotatedCount} {target} pages by 90°", "RotateRight");
        }
        else
        {
            ShowToast($"No {target} pages matched for rotation", "InformationOutline");
        }
    }
}
