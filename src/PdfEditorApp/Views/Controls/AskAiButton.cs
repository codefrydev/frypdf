using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material.Icons;
using Material.Icons.Avalonia;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views.Controls;

/// <summary>
/// Display variant style for the <see cref="AskAiButton"/>.
/// </summary>
public enum AskAiButtonVariant
{
    /// <summary>
    /// Vibrant purple floating pill badge positioned above element selection borders.
    /// </summary>
    FloatingPill,

    /// <summary>
    /// Soft lilac themed badge designed for properties inspectors and toolbars.
    /// </summary>
    Subtle
}

/// <summary>
/// Reusable, context-aware "Ask AI" button control for FryPDF Studio.
/// Can be dropped anywhere in the canvas or sidebar UI via &lt;controls:AskAiButton /&gt;.
/// Automatically resolves the target element from DataContext/CommandParameter and executes
/// <see cref="MainViewModel.AskAiToModifyCommand"/>.
/// </summary>
public class AskAiButton : Button
{
    public static readonly StyledProperty<AskAiButtonVariant> VariantProperty =
        AvaloniaProperty.Register<AskAiButton, AskAiButtonVariant>(nameof(Variant), AskAiButtonVariant.FloatingPill);

    public static readonly StyledProperty<string> ButtonTextProperty =
        AvaloniaProperty.Register<AskAiButton, string>(nameof(ButtonText), "Ask AI");

    public static readonly StyledProperty<string> ToolTipTextProperty =
        AvaloniaProperty.Register<AskAiButton, string>(nameof(ToolTipText), "Ask AI to Modify (Ctrl+I / ⌘I)");

    public AskAiButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public string ButtonText
    {
        get => GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    public string ToolTipText
    {
        get => GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(Button);

    private readonly MaterialIcon _icon;
    private readonly TextBlock _textBlock;

    public AskAiButton()
    {
        try
        {
            Cursor = new Cursor(StandardCursorType.Hand);
        }
        catch
        {
            // Headless / mock test runner without platform cursor service
        }

        _icon = new MaterialIcon
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        try
        {
            _icon.Kind = MaterialIconKind.AutoFixHigh;
        }
        catch
        {
            // Headless unit test environment without Avalonia rendering interface
        }

        _textBlock = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _icon, _textBlock }
        };

        Content = stack;

        ApplyVariant(Variant);
        UpdateTextAndToolTip();
    }

    static AskAiButton()
    {
        VariantProperty.Changed.AddClassHandler<AskAiButton>((btn, e) =>
        {
            if (e.NewValue is AskAiButtonVariant v) btn.ApplyVariant(v);
        });

        ButtonTextProperty.Changed.AddClassHandler<AskAiButton>((btn, e) =>
        {
            btn.UpdateTextAndToolTip();
        });

        ToolTipTextProperty.Changed.AddClassHandler<AskAiButton>((btn, e) =>
        {
            btn.UpdateTextAndToolTip();
        });
    }

    private void UpdateTextAndToolTip()
    {
        _textBlock.Text = ButtonText;
        ToolTip.SetTip(this, ToolTipText);
    }

    private void ApplyVariant(AskAiButtonVariant variant)
    {
        if (variant == AskAiButtonVariant.FloatingPill)
        {
            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Top;
            Margin = new Thickness(0, -24, 0, 0);
            Height = 22;
            Padding = new Thickness(6, 2);
            Background = Brush.Parse("#7C3AED");
            BorderBrush = Brush.Parse("#6D28D9");
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(11);

            _icon.Width = 12;
            _icon.Height = 12;
            _icon.Foreground = Brushes.White;

            _textBlock.FontSize = 10;
            _textBlock.Foreground = Brushes.White;
        }
        else // Subtle
        {
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Center;
            Margin = new Thickness(0);
            Height = 24;
            Padding = new Thickness(6, 2);
            Background = Brush.Parse("#EDE9FE");
            BorderBrush = Brush.Parse("#C4B5FD");
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(5);

            _icon.Width = 13;
            _icon.Height = 13;
            _icon.Foreground = Brush.Parse("#7C3AED");

            _textBlock.FontSize = 10.5;
            _textBlock.Foreground = Brush.Parse("#7C3AED");
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (Variant == AskAiButtonVariant.FloatingPill)
        {
            Background = Brush.Parse("#6D28D9");
            BorderBrush = Brush.Parse("#5B21B6");
        }
        else
        {
            Background = Brush.Parse("#DDD6FE");
            BorderBrush = Brush.Parse("#A78BFA");
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (Variant == AskAiButtonVariant.FloatingPill)
        {
            Background = Brush.Parse("#7C3AED");
            BorderBrush = Brush.Parse("#6D28D9");
        }
        else
        {
            Background = Brush.Parse("#EDE9FE");
            BorderBrush = Brush.Parse("#C4B5FD");
        }
    }

    protected override void OnClick()
    {
        if (Command != null)
        {
            base.OnClick();
            return;
        }

        // Automatic smart invocation:
        // 1. Resolve target element (from CommandParameter, or from DataContext)
        var targetParam = CommandParameter ?? DataContext;

        // 2. Discover MainViewModel in visual ancestor hierarchy
        var mainVm = this.FindAncestorOfType<DocumentCanvasView>()?.DataContext as MainViewModel
                     ?? this.FindAncestorOfType<MainWindow>()?.DataContext as MainViewModel
                     ?? (TopLevel.GetTopLevel(this) as Window)?.DataContext as MainViewModel;

        if (mainVm != null)
        {
            if (mainVm.AskAiToModifyCommand.CanExecute(targetParam))
            {
                mainVm.AskAiToModifyCommand.Execute(targetParam);
            }
            return;
        }

        base.OnClick();
    }
}
