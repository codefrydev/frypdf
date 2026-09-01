using System;
using Avalonia;
using Avalonia.Controls;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class InspectorSidebarView : UserControl
{
    public InspectorSidebarView()
    {
        InitializeComponent();
    }

    private void OnSidebarTextBoxAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.PropertyChanged += (s, args) =>
            {
                if (args.Property == TextBox.SelectionStartProperty || args.Property == TextBox.SelectionEndProperty || args.Property == TextBox.TextProperty)
                {
                    UpdateSidebarSelection(textBox);
                }
            };

            textBox.PointerReleased += (s, args) => UpdateSidebarSelection(textBox);
            textBox.KeyUp += (s, args) => UpdateSidebarSelection(textBox);
        }
    }

    private void UpdateSidebarSelection(TextBox textBox)
    {
        if (DataContext is InspectorViewModel inspector && inspector.TextElement != null)
        {
            int start = Math.Min(textBox.SelectionStart, textBox.SelectionEnd);
            int end = Math.Max(textBox.SelectionStart, textBox.SelectionEnd);
            int len = end - start;

            if (!textBox.IsFocused && len == 0 && inspector.TextElement.HasTextSelection)
            {
                return;
            }

            string text = textBox.Text ?? "";
            string sel = len > 0 && start + len <= text.Length ? text.Substring(start, len) : string.Empty;
            inspector.TextElement.UpdateTextSelection(start, len, sel);
        }
    }
}
