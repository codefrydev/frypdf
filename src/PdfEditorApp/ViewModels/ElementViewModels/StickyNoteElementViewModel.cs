using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class StickyNoteElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private string _author = "Reviewer";

    [ObservableProperty]
    private string _timestamp = DateTime.Now.ToString("MMM dd, yyyy HH:mm");

    [ObservableProperty]
    private string _noteText = "Please verify the audit figures with the legal compliance team prior to final PDF release.";

    [ObservableProperty]
    private string _status = "Pending Review";

    [ObservableProperty]
    private string _colorHex = "#FEF3C7";

    [ObservableProperty]
    private string _borderColorHex = "#F59E0B";

    [ObservableProperty]
    private bool _isExpanded = true;

    public override ElementKind Kind => ElementKind.StickyNote;
    public override string DisplayName => $"Sticky Note ({Author})";

    public StickyNoteElementViewModel()
    {
        Width = 200;
        Height = 150;
    }

    [RelayCommand]
    public void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        Height = IsExpanded ? 150 : 36;
    }

    public override PdfElementBase ToModel()
    {
        return new PdfStickyNoteElement
        {
            Id = Id,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZIndex = ZIndex,
            Rotation = Rotation,
            Opacity = Opacity,
            IsLocked = IsLocked,
            Author = Author,
            Timestamp = Timestamp,
            NoteText = NoteText,
            Status = Status,
            ColorHex = ColorHex,
            BorderColorHex = BorderColorHex,
            IsExpanded = IsExpanded
        };
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfStickyNoteElement note)
        {
            Id = note.Id;
            X = note.X;
            Y = note.Y;
            Width = note.Width;
            Height = note.Height;
            ZIndex = note.ZIndex;
            Rotation = note.Rotation;
            Opacity = note.Opacity;
            IsLocked = note.IsLocked;

            Author = note.Author;
            Timestamp = note.Timestamp;
            NoteText = note.NoteText;
            Status = note.Status;
            ColorHex = note.ColorHex;
            BorderColorHex = note.BorderColorHex;
            IsExpanded = note.IsExpanded;
        }
    }
}
