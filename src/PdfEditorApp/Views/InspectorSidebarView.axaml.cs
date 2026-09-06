using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Material.Icons;
using Material.Icons.Avalonia;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class InspectorSidebarView : UserControl
{
    private bool _isResizingTextEditor;
    private Point _resizeStartPoint;
    private double _resizeStartHeight;
    private const double DefaultEditorHeight = 72.0;
    private const double ExpandedEditorHeight = 180.0;
    private const double MinEditorHeight = 44.0;
    private const double MaxEditorHeight = 450.0;

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

    private void OnTextEditorResizeGripPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control grip && e.GetCurrentPoint(grip).Properties.IsLeftButtonPressed)
        {
            _isResizingTextEditor = true;
            _resizeStartPoint = e.GetPosition(this);
            var textBox = this.FindControl<TextBox>("SidebarTextEditor");
            _resizeStartHeight = textBox?.Bounds.Height > 0 ? textBox.Bounds.Height : (textBox?.Height ?? DefaultEditorHeight);
            e.Pointer.Capture(grip);
            e.Handled = true;
        }
    }

    private void OnTextEditorResizeGripPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isResizingTextEditor)
        {
            var currentPoint = e.GetPosition(this);
            double deltaY = currentPoint.Y - _resizeStartPoint.Y;
            double newHeight = Math.Clamp(_resizeStartHeight + deltaY, MinEditorHeight, MaxEditorHeight);
            var textBox = this.FindControl<TextBox>("SidebarTextEditor");
            if (textBox != null)
            {
                textBox.Height = newHeight;
            }
            e.Handled = true;
        }
    }

    private void OnTextEditorResizeGripPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizingTextEditor)
        {
            _isResizingTextEditor = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            UpdateExpandIcon();
        }
    }

    private void OnTextEditorResizeGripDoubleTapped(object? sender, TappedEventArgs e)
    {
        ToggleEditorSize();
        e.Handled = true;
    }

    private void OnToggleTextEditorExpandClicked(object? sender, RoutedEventArgs e)
    {
        ToggleEditorSize();
    }

    private void ToggleEditorSize()
    {
        var textBox = this.FindControl<TextBox>("SidebarTextEditor");
        if (textBox != null)
        {
            double current = textBox.Height;
            if (double.IsNaN(current) || current <= DefaultEditorHeight + 20)
            {
                textBox.Height = ExpandedEditorHeight;
            }
            else
            {
                textBox.Height = DefaultEditorHeight;
            }
            UpdateExpandIcon();
        }
    }

    private void UpdateExpandIcon()
    {
        var textBox = this.FindControl<TextBox>("SidebarTextEditor");
        var icon = this.FindControl<MaterialIcon>("TextEditorExpandIcon");
        if (textBox != null && icon != null)
        {
            bool isExpanded = textBox.Height > DefaultEditorHeight + 20;
            icon.Kind = isExpanded ? MaterialIconKind.ArrowCollapseVertical : MaterialIconKind.ArrowExpandVertical;
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
