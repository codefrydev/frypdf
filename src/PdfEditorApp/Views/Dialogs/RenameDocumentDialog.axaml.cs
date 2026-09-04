using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views.Dialogs;

public partial class RenameDocumentDialog : UserControl
{
    public RenameDocumentDialog()
    {
        InitializeComponent();
        PropertyChanged += OnDialogPropertyChanged;
    }

    private void OnDialogPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty && e.NewValue is true)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var textBox = this.FindControl<TextBox>("RenameTextBox");
                if (textBox != null)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is HomeViewModel vm)
        {
            vm.CancelRenameCommand.Execute(null);
        }
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Prevent backdrop click from dismissing when clicking inside card
        e.Handled = true;
    }

    private void OnRenameTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not HomeViewModel vm) return;

        if (e.Key == Key.Enter)
        {
            vm.ConfirmRenameCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.CancelRenameCommand.Execute(null);
            e.Handled = true;
        }
    }
}
