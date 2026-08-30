using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;

namespace PdfEditorApp.ViewModels;

public partial class PdfToolCardViewModel : ViewModelBase
{
    public PdfToolDefinition Definition { get; }

    public PdfToolId Id => Definition.Id;
    public string Name => Definition.Name;
    public string Description => Definition.Description;
    public PdfToolCategory Category => Definition.Category;
    public string CategoryDisplayName => Definition.CategoryDisplayName;
    public string IconKind => Definition.IconKind;
    public string IconColorHex => Definition.IconColorHex;
    public string BackgroundAccentHex => Definition.BackgroundAccentHex;
    public bool IsNew => Definition.IsNew;
    public bool IsWorkflowBanner => Definition.IsWorkflowBanner;
    public bool SupportsMultiFile => Definition.SupportsMultiFile;

    [ObservableProperty]
    private bool _isStarred;

    public event Action<PdfToolId>? ToolSelected;
    public event Action<PdfToolId>? StarToggled;

    public PdfToolCardViewModel(PdfToolDefinition definition)
    {
        Definition = definition;
    }

    [RelayCommand]
    public void SelectTool()
    {
        ToolSelected?.Invoke(Id);
    }

    [RelayCommand]
    public void ToggleStar()
    {
        IsStarred = !IsStarred;
        StarToggled?.Invoke(Id);
    }
}
