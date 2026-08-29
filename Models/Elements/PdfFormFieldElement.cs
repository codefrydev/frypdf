using System.Collections.Generic;

namespace PdfEditorApp.Models.Elements;

public class PdfFormFieldElement : PdfElementBase
{
    public override ElementKind Kind => ElementKind.FormField;

    public FormFieldType FieldType { get; set; } = FormFieldType.Text;
    public string FieldName { get; set; } = "form_field_1";
    public string Label { get; set; } = "Full Name:";
    public string Placeholder { get; set; } = "Enter your full legal name...";
    public string Value { get; set; } = "";
    public bool IsRequired { get; set; } = true;
    public bool IsChecked { get; set; } = false;
    public string BorderColorHex { get; set; } = "#0F6CBD";
    public string BackgroundColorHex { get; set; } = "#F8FAFC";
    public double FontSize { get; set; } = 12;
    public List<string> Options { get; set; } = new() { "Option 1", "Option 2", "Option 3" };

    public override PdfElementBase Clone()
    {
        var clone = (PdfFormFieldElement)base.Clone();
        clone.Options = new List<string>(Options);
        return clone;
    }
}
