using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.ViewModels.ElementViewModels;

public partial class FormFieldElementViewModel : ElementViewModelBase
{
    [ObservableProperty]
    private FormFieldType _fieldType = FormFieldType.Text;

    [ObservableProperty]
    private string _fieldName = "form_field_1";

    [ObservableProperty]
    private string _label = "Full Legal Name:";

    [ObservableProperty]
    private string _placeholder = "Type text here...";

    [ObservableProperty]
    private string _value = "";

    [ObservableProperty]
    private bool _isRequired = true;

    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private string _borderColorHex = "#0F6CBD";

    [ObservableProperty]
    private string _backgroundColorHex = "#F8FAFC";

    [ObservableProperty]
    private double _fontSize = 12;

    public ObservableCollection<string> Options { get; } = new();

    public override ElementKind Kind => ElementKind.FormField;
    public override string DisplayName => $"Form: {Label} ({FieldType})";

    public FormFieldElementViewModel()
    {
        Options.Add("Approved");
        Options.Add("Under Review");
        Options.Add("Declined");
    }

    [RelayCommand]
    public void ToggleChecked()
    {
        IsChecked = !IsChecked;
    }

    public override PdfElementBase ToModel()
    {
        var model = new PdfFormFieldElement
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
            FieldType = FieldType,
            FieldName = FieldName,
            Label = Label,
            Placeholder = Placeholder,
            Value = Value,
            IsRequired = IsRequired,
            IsChecked = IsChecked,
            BorderColorHex = BorderColorHex,
            BackgroundColorHex = BackgroundColorHex,
            FontSize = FontSize,
            Options = new System.Collections.Generic.List<string>(Options)
        };
        return model;
    }

    public override void LoadFromModel(PdfElementBase model)
    {
        if (model is PdfFormFieldElement form)
        {
            Id = form.Id;
            X = form.X;
            Y = form.Y;
            Width = form.Width;
            Height = form.Height;
            ZIndex = form.ZIndex;
            Rotation = form.Rotation;
            Opacity = form.Opacity;
            IsLocked = form.IsLocked;

            FieldType = form.FieldType;
            FieldName = form.FieldName;
            Label = form.Label;
            Placeholder = form.Placeholder;
            Value = form.Value;
            IsRequired = form.IsRequired;
            IsChecked = form.IsChecked;
            BorderColorHex = form.BorderColorHex;
            BackgroundColorHex = form.BackgroundColorHex;
            FontSize = form.FontSize;

            Options.Clear();
            foreach (var opt in form.Options) Options.Add(opt);
        }
    }
}
