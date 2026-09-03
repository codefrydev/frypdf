using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using UglyToad.PdfPig;
using UglyToad.PdfPig.AcroForms;
using UglyToad.PdfPig.AcroForms.Fields;

namespace PdfEditorApp.Core.Deconstruction.Extractors;

/// <summary>
/// Strongly-typed, reflection-free AcroForms interactive form field extractor.
/// Maps native PdfPig AcroField hierarchy into editable <see cref="PdfFormFieldElement"/> models.
/// </summary>
public static class PdfFormExtractor
{
    /// <summary>
    /// Extracts all AcroForms on the given document/page using strongly-typed patterns without reflection.
    /// </summary>
    public static List<PdfFormFieldElement> ExtractFormFields(
        PdfDocument? doc,
        int pageNumber,
        double pageHeight,
        ref int formZIndex,
        PdfDeconstructionOptions options,
        ILogger? logger = null)
    {
        var formElements = new List<PdfFormFieldElement>();
        if (doc == null) return formElements;

        try
        {
            if (doc.TryGetForm(out var form) && form != null && form.Fields != null)
            {
                foreach (var field in form.Fields)
                {
                    if (!field.Bounds.HasValue || field.Bounds.Value.Width <= 0 || field.Bounds.Value.Height <= 0)
                        continue;

                    var b = field.Bounds.Value;
                    double fX = Math.Max(0, b.Left);
                    double fY = Math.Max(0, pageHeight - b.Top);
                    double fW = Math.Max(20, b.Width);
                    double fH = Math.Max(14, b.Height);

                    string fieldName = field.Information?.PartialName
                        ?? field.Information?.AlternateName
                        ?? field.Information?.MappingName
                        ?? "FormField";

                    string fieldValue = ExtractFieldValue(field);
                    FormFieldType fieldType = MapFieldType(field);

                    // PDF spec 32000-1: bit 1 of FieldFlags is ReadOnly (0x1)
                    bool isReadOnly = (field.FieldFlags & 1) != 0;

                    var formElement = new PdfFormFieldElement
                    {
                        X = Math.Round(fX, 1),
                        Y = Math.Round(fY, 1),
                        Width = Math.Round(fW, 1),
                        Height = Math.Round(fH, 1),
                        FieldName = fieldName,
                        Value = fieldValue,
                        DefaultValue = fieldValue,
                        FieldType = fieldType,
                        IsReadOnly = isReadOnly,
                        ZIndex = formZIndex++
                    };
                    formElements.Add(formElement);
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to extract AcroForms on page {PageNumber}", pageNumber);
        }

        return formElements;
    }

    private static string ExtractFieldValue(AcroFieldBase field)
    {
        return field switch
        {
            AcroTextField tf => tf.Value ?? string.Empty,
            AcroCheckboxField cb => cb.IsChecked ? "true" : "false",
            AcroRadioButtonField rb => rb.IsSelected ? "true" : "false",
            AcroComboBoxField combo => combo.SelectedOptions.FirstOrDefault() ?? combo.Options.FirstOrDefault()?.ToString() ?? string.Empty,
            AcroListBoxField list => list.SelectedOptions.FirstOrDefault() ?? list.Options.FirstOrDefault()?.ToString() ?? string.Empty,
            _ => string.Empty
        };
    }

    private static FormFieldType MapFieldType(AcroFieldBase field)
    {
        return field.FieldType switch
        {
            AcroFieldType.Checkbox => FormFieldType.Checkbox,
            AcroFieldType.RadioButton => FormFieldType.Radio,
            AcroFieldType.ComboBox or AcroFieldType.ListBox => FormFieldType.Dropdown,
            AcroFieldType.Signature => FormFieldType.Signature,
            _ => FormFieldType.Text
        };
    }
}
