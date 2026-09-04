using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views.Dialogs;

public partial class DeleteConfirmationDialog : UserControl
{
    public DeleteConfirmationDialog()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is HomeViewModel vm)
        {
            vm.CancelDeleteCommand.Execute(null);
        }
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not HomeViewModel vm) return;

        if (e.Key == Key.Escape)
        {
            vm.CancelDeleteCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            vm.ConfirmDeleteCommand.Execute(null);
            e.Handled = true;
        }
    }
}
