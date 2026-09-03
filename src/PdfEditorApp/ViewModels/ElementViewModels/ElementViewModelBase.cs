using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public abstract partial class ElementViewModelBase : ViewModelBase
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _width = 200;

    [ObservableProperty]
    private double _height = 100;

    [ObservableProperty]
    private int _zIndex = 0;

    [ObservableProperty]
    private double _rotation = 0;

    [ObservableProperty]
    private double _opacity = 1.0;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isInEditMode;

    partial void OnIsInEditModeChanged(bool value)
    {
        OnEditModeChanged(value);
    }

    protected virtual void OnEditModeChanged(bool isInEditMode)
    {
    }

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private string? _groupId;

    public abstract ElementKind Kind { get; }
    public abstract string DisplayName { get; }

    public abstract PdfElementBase ToModel();
    public abstract void LoadFromModel(PdfElementBase model);

    protected void CopyBasePropertiesTo(PdfElementBase model)
    {
        model.Id = Id;
        model.X = X;
        model.Y = Y;
        model.Width = Width;
        model.Height = Height;
        model.ZIndex = ZIndex;
        model.Rotation = Rotation;
        model.Opacity = Opacity;
        model.IsLocked = IsLocked;
        model.GroupId = GroupId;
    }

    protected void PopulateBaseProperties(PdfElementBase model)
    {
        Id = model.Id;
        X = model.X;
        Y = model.Y;
        Width = model.Width;
        Height = model.Height;
        ZIndex = model.ZIndex;
        Rotation = model.Rotation;
        Opacity = model.Opacity;
        IsLocked = model.IsLocked;
        GroupId = model.GroupId;
    }

    public virtual void MoveBy(double deltaX, double deltaY, double canvasWidth, double canvasHeight)
    {
        if (IsLocked) return;

        double newX = Math.Max(0, Math.Min(canvasWidth - Width, X + deltaX));
        double newY = Math.Max(0, Math.Min(canvasHeight - Height, Y + deltaY));

        X = Math.Round(newX, 1);
        Y = Math.Round(newY, 1);
    }

    public virtual void Resize(string handle, double deltaX, double deltaY, double minW = 30, double minH = 20)
    {
        if (IsLocked) return;

        switch (handle.ToLowerInvariant())
        {
            case "topleft":
                double newW = Math.Max(minW, Width - deltaX);
                double newH = Math.Max(minH, Height - deltaY);
                X += Width - newW;
                Y += Height - newH;
                Width = newW;
                Height = newH;
                break;

            case "top":
                double newTopH = Math.Max(minH, Height - deltaY);
                Y += Height - newTopH;
                Height = newTopH;
                break;

            case "topright":
                Width = Math.Max(minW, Width + deltaX);
                double newTRH = Math.Max(minH, Height - deltaY);
                Y += Height - newTRH;
                Height = newTRH;
                break;

            case "left":
                double newLeftW = Math.Max(minW, Width - deltaX);
                X += Width - newLeftW;
                Width = newLeftW;
                break;

            case "right":
                Width = Math.Max(minW, Width + deltaX);
                break;

            case "bottomleft":
                double newBLW = Math.Max(minW, Width - deltaX);
                X += Width - newBLW;
                Width = newBLW;
                Height = Math.Max(minH, Height + deltaY);
                break;

            case "bottom":
                Height = Math.Max(minH, Height + deltaY);
                break;

            case "bottomright":
                Width = Math.Max(minW, Width + deltaX);
                Height = Math.Max(minH, Height + deltaY);
                break;
        }

        X = Math.Round(X, 1);
        Y = Math.Round(Y, 1);
        Width = Math.Round(Width, 1);
        Height = Math.Round(Height, 1);
    }
}
