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

    /// <summary>
    /// Wires the inspector's text editor.
    /// </summary>
    /// <remarks>
    /// Named handlers with "-= before +=" so that re-attaching the TextBox (container
    /// recycling, selection changes) cannot accumulate subscriptions. The anonymous lambdas
    /// used here before could never be removed, and TextBox.PropertyChanged fires for every
    /// property — so each keystroke ran UpdateSidebarSelection once per past attachment.
    /// </remarks>
    private void OnSidebarTextBoxAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        textBox.PropertyChanged -= OnSidebarTextBoxPropertyChanged;
        textBox.PropertyChanged += OnSidebarTextBoxPropertyChanged;

        textBox.PointerReleased -= OnSidebarTextBoxPointerReleased;
        textBox.PointerReleased += OnSidebarTextBoxPointerReleased;

        textBox.KeyUp -= OnSidebarTextBoxKeyUp;
        textBox.KeyUp += OnSidebarTextBoxKeyUp;

        textBox.DetachedFromVisualTree -= OnSidebarTextBoxDetached;
        textBox.DetachedFromVisualTree += OnSidebarTextBoxDetached;
    }

    private void OnSidebarTextBoxDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        textBox.PropertyChanged -= OnSidebarTextBoxPropertyChanged;
        textBox.PointerReleased -= OnSidebarTextBoxPointerReleased;
        textBox.KeyUp -= OnSidebarTextBoxKeyUp;
        textBox.DetachedFromVisualTree -= OnSidebarTextBoxDetached;
    }

    private void OnSidebarTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        if (e.Property == TextBox.SelectionStartProperty ||
            e.Property == TextBox.SelectionEndProperty ||
            e.Property == TextBox.TextProperty)
        {
            UpdateSidebarSelection(textBox);
        }
    }

    private void OnSidebarTextBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is TextBox textBox) UpdateSidebarSelection(textBox);
    }

    private void OnSidebarTextBoxKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox textBox) UpdateSidebarSelection(textBox);
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
